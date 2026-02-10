using Animancer;
using DG.Tweening;
using KinematicCharacterController;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRM;
using static DataManager;
using static SingletonManager;
using static UnityEngine.InputSystem.InputAction;

namespace Battle
{
	public partial class Character : MonoBehaviour, ICharacterController
	{
		public VRMBlendShapeProxy _BlendShapeProxy;
		public AttackCollider _MeleeAttackCollider;
		public GameObject _HitEffectPrefab;
		public GameObject _GuardEffectPrefab;
		public ParticleSystem _JustGuardEffect;
		public GameObject _CancelModel;
		public Material _WhiteMaterial;
		public Transform _CooltimeJitter;

		CapsuleCollider _Collider;
		Collider[] _MeleeAttackResults;
		RaycastHit[] _RaycastResults;
		[HideInInspector] public int _AttackIndex;
		[HideInInspector] public bool _NextAttackAvailable;
		[HideInInspector] public bool _NextAttackInput;
		float _AttackMovePercent;
		float _GuardUpTime, _GuardDownTime;
		float _HitStunTimer;
		Vector3 _HitStunPrevVelocity;
		bool _IsRunning;
		Coroutine _JustGuardCoroutine;
		bool _JustGuardCancelTrigger;
		[HideInInspector] public float _LastSkill1Time, _LastSkill2Time, _LastUltimateTime;

		const float TimeDefault = -10000f;
		const float AttackPreDelay = 0.1f;
		const float MoveGraceDuration = 0.1f;
		const float WallJumpAngleThreshold = 60f;

		public Vector3 Center => transform.position + _Collider.center;
		public Vector3 Bottom => transform.position + _Motor.CharacterTransformToCapsuleBottom;

		void Start()
		{
			_MeleeAttackResults = new Collider[100];
			_RaycastResults = new RaycastHit[10];
			_Collider = GetComponent<CapsuleCollider>();

			_GuardUpTime = TimeDefault;
			_GuardDownTime = TimeDefault;
			_LastRequestTime = TimeDefault;
			_LastCanJumpTime = TimeDefault;
			_LastDashTime = TimeDefault;
			_LastSkill1Time = TimeDefault;
			_LastSkill2Time = TimeDefault;
			_LastUltimateTime = TimeDefault;

			InitMovement();
			InitFSM();
			_AttackIndex = -1;
			_JustGuardEffect.gameObject.SetActive(false);

			//StartCoroutine(Internal());
			//IEnumerator Internal()
			//{
			//	yield return new WaitForSeconds(0.1f);
			//	_BlendShapeProxy.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Neutral), 1f);
			//}
		}

		void Update()
		{
			// 애니메이션
			UpdateFSM();

			// 경직
			_BaseLayer.Speed = IsHitStun() ? 0f : 1f;
		}

		public void Move(CallbackContext obj)
		{
			_MoveInput = obj.ReadValue<Vector2>().Vector2ToXZ();
		}

		public void Dash(Direction4 dir)
		{
			_MoveRequest = dir switch
			{
				Direction4.Up => MoveRequest.DashFwd,
				Direction4.Down => MoveRequest.DashBwd,
				Direction4.Left => MoveRequest.DashLeft,
				_ => MoveRequest.DashRight
			};
			_LastRequestTime = Time.time;
		}

		public void Jump(CallbackContext obj)
		{
			_MoveRequest = MoveRequest.Jump;
			_LastRequestTime = Time.time;
		}

		public void NormalAttack(CallbackContext obj)
		{
			bool canAttack = _FSM.CurrentState._CanAttack;
			canAttack &= !IsGuarding();
			canAttack &= !(_FSM.CurrentState.IsAttack && _FSM.CurrentState._AttackData._Type > AttackType.Normal);
			if (!canAttack) return;

			// 저스트 가드
			if (IsJustGuard())
			{
				_FSM.TrySetState(_GuardAttack);
				GiveDamage();
				_JustGuardCancelTrigger = true;
			}
			// 대쉬 
			else if (IsDashing())
			{
				_FadeOutDeaccelTimer = _DashDuration;
				DashCancel();
				_FSM.TrySetState(_DashAttack);
				GiveDamage();
			}
			// 지상
			else if (_Motor.GroundingStatus.IsStableOnGround && !_Motor.MustUnground())
			{
				// 1타 공격
				if (_AttackIndex < 0)
				{
					_AttackIndex = 0;
					_FSM.TrySetState(_NormalAttacks[_AttackIndex]);
					GiveDamage();
				}
				// 2~타 공격
				else
				{
					_NextAttackInput = true;
				}
			}
			// 공중
			else
			{
				_FSM.TrySetState(_JumpAttack);
				GiveDamage();
			}
		}

		public void SpecialAttack(CallbackContext obj)
		{
			bool canAttack = _FSM.CurrentState._CanAttack;
			canAttack &= !IsGuarding();
			if (!canAttack) return;

			// 지상
			if (_Motor.GroundingStatus.IsStableOnGround && !_Motor.MustUnground())
			{
				if (_FSM.CurrentState.IsAttack)
				{
					if (_FSM.CurrentState._AttackData._Type < AttackType.Special)
					{
						Play_Canceling(_SpecialAttack, true);
					}
					else return;
				}
				else
				{
					_FSM.TrySetState(_SpecialAttack);
				}
				GiveDamage();
			}
			// 공중
			else
			{
				_FSM.TrySetState(_JumpSpecialAttack);
				GiveDamage();
			}
		}

		public void Skill1(CallbackContext obj)
		{
			bool canAttack = _FSM.CurrentState._CanAttack;
			canAttack &= !IsGuarding();
			if (!canAttack) return;

			// 쿨타임
			if (Time.time - _LastSkill1Time < _Skill1._AttackData._Cooltime)
			{
				CooltimeJitter();
				return;
			}

			if (_FSM.CurrentState.IsAttack)
			{
				if (_FSM.CurrentState._AttackData._Type < AttackType.Skill)
				{
					Play_Canceling(_Skill1, true);
				}
				else return;
			}
			else
			{
				_FSM.TrySetState(_Skill1);
			}
			GiveDamage();
			_LastSkill1Time = Time.time;
		}

		public void Skill2(CallbackContext obj)
		{
			bool canAttack = _FSM.CurrentState._CanAttack;
			canAttack &= !IsGuarding();
			if (!canAttack) return;

			// 쿨타임
			if (Time.time - _LastSkill2Time < _Skill2._AttackData._Cooltime)
			{
				CooltimeJitter();
				return;
			}

			if (_FSM.CurrentState.IsAttack)
			{
				if (_FSM.CurrentState._AttackData._Type < AttackType.Skill)
				{
					Play_Canceling(_Skill2, true);
				}
				else return;
			}
			else
			{
				_FSM.TrySetState(_Skill2);
			}
			GiveDamage();
			_LastSkill2Time = Time.time;
		}

		public void Ultimate(CallbackContext obj)
		{
			bool canAttack = _FSM.CurrentState._CanAttack;
			canAttack &= !IsGuarding();
			if (!canAttack) return;

			// 쿨타임
			if (Time.time - _LastUltimateTime < _Ultimate._AttackData._Cooltime)
			{
				CooltimeJitter();
				return;
			}

			if (_FSM.CurrentState.IsAttack)
			{
				if (_FSM.CurrentState._AttackData._Type < AttackType.Ultimate)
				{
					Play_Canceling(_Ultimate, true);
				}
				else return;
			}
			else
			{
				_FSM.TrySetState(_Ultimate);
			}
			GiveDamage();
			_LastUltimateTime = Time.time;
		}

		public void Guard(CallbackContext obj)
		{
			if (!_FSM.CurrentState._CanGuard) return;

			// 가드 시작
			if (Inputs.Guard.WasPressedThisFrame() && !IsGuarding())
			{
				if (_FSM.CurrentState.IsAttack)
				{
					Play_Canceling(_Idle, false);
				}
				_UpperBodyLayer.SetWeight(1f);
				AnimancerState state = _UpperBodyLayer.Play(_GuardUpAsset);
				state.Time = 0f;
				_GuardUpTime = Time.time;
			}

			// 가드 해제
			if (!Inputs.Guard.IsPressed() && IsGuarding())
			{
				AnimancerState state = _UpperBodyLayer.Play(_GuardDownAsset);
				state.Events(this).OnEnd ??= GuardCancel;
				_GuardDownTime = Time.time;
			}
		}

		public void GiveDamage()
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				AttackCollider attack = Instantiate(_MeleeAttackCollider, transform);
				attack._Owner = this;
				attack._StateInfo = _FSM.CurrentState;

				// 공격 최소 딜레이
				yield return new WaitForSeconds(AttackPreDelay);
				if (_FSM.CurrentState != attack._StateInfo)
				{
					Destroy(attack.gameObject);
					yield break;
				}

				// 공중 공격으로 점프
				RaycastHit[] raycastResults = new RaycastHit[10];
				int raycastCount = Raycast();
				List<RaycastHit> hits = raycastResults.ArrayToList(raycastCount);
				RaycastHit nearest = hits.MinBy(x => x.distance);
				if (raycastCount > 0 && attack._StateInfo._AttackData._Type == AttackType.Normal)
				{
					float angle = Vector3.Angle(_Motor.CharacterForward, -nearest.normal);
					if (angle < WallJumpAngleThreshold && !_Motor.GroundingStatus.IsStableOnGround)
					{
						_AttackJumpDirection = -_Motor.CharacterForward;
					}
				}

				// 공격 판정 딜레이
				yield return new WaitForSeconds(attack._StateInfo._EffectData._Delay - AttackPreDelay);

				// 히트 판정
				int overlapCount = Physics.OverlapBoxNonAlloc(
					center: attack._Collider.GetCenter(),
					halfExtents: attack._Collider.size / 2f,
					results: _MeleeAttackResults,
					orientation: attack.transform.rotation,
					mask: GetOppositeLayerMask());

				// 공격 적중
				if (overlapCount > 0)
				{
					List<Collider> overlaps = _MeleeAttackResults.ArrayToList(overlapCount);
					overlaps.Sort((a, b) =>
					{
						float aDistance = (a.GetComponent<Character>().Center - Center).sqrMagnitude;
						float bDistance = (b.GetComponent<Character>().Center - Center).sqrMagnitude;
						float v = (aDistance - bDistance) * 100f;
						return (int)v;
					});
					foreach (Collider col in overlaps)
					{
						Character c = col.GetComponent<Character>();
						c.TakeDamage(this, attack);
						yield return new WaitForSeconds(attack._StateInfo._AttackData._AttackerHitStunDuration);
					}
				}
				// 벽에 적중
				else if (raycastCount > 0) 
				{
					PlayEffect123123(_GuardEffectPrefab, this, nearest.point, Quaternion.identity);
				}

				Destroy(attack.gameObject);

				int Raycast()
				{
					return Physics.RaycastNonAlloc(
						origin: Center,
						direction: _Motor.CharacterForward,
						results: raycastResults,
						maxDistance: attack._Collider.size.z,
						layerMask: Layer.TerrainLayerMask);
				}
			}
		}

		public void TakeDamage(Character attacker, AttackCollider attack)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				Vector3 attackDir = Center - attacker.Center;
				attackDir.Normalize();
				int count = Physics.RaycastNonAlloc(
					origin: attacker.Center,
					direction: attackDir,
					results: _RaycastResults,
					maxDistance: 100f,
					layerMask: GetLayerMask());
				RaycastHit hit = default;
				for (int i = 0; i < count; i++)
				{
					RaycastHit iter = _RaycastResults[i];
					if (_Collider == iter.collider)
					{
						hit = iter;
						break;
					}
				}

				// 공격 판정 딜레이
				Effect effectData = attack._StateInfo._EffectData;
				if (effectData == null) yield break;
				Attack attackData = attack._StateInfo._AttackData;
				if (attackData == null) yield break;
				float delay = attackData._HitDelay - effectData._Delay - AttackPreDelay;
				yield return new WaitForSeconds(delay);

				// 경직
				AddHitStunTimer(attackData._AttackerHitStunDuration);
				attacker.AddHitStunTimer(attackData._AttackerHitStunDuration);

				// 가드 판정
				bool guard = IsGuardingEffective();
				float angle = Vector3.Angle(transform.forward, new(-attackDir.x, 0f, -attackDir.z));
				guard &= angle < 90f;
				if (guard)
				{
					// 저스트 가드
					if (Time.time - _GuardUpTime < 0.3f && Inputs.Guard.IsPressed()) 
					{
						JustGuard();
					}
					// 일반 가드
					else
					{
						_FadeInDeaccelTimer = 0.6f;
					}
					PlayEffect123123(_GuardEffectPrefab, this, hit.point, Quaternion.LookRotation(attackDir));
				}
				else
				{
					// 일반 경직
					if (attackData._ForceForward == 0f && attackData._ForceUp == 0f)
					{
						_Damage._Duration = attackData._DamageDuration;
						_FadeInDeaccelTimer = attackData._DamageDuration;
						_Damage._Asset = _DamageAssets.PickOne();
						_FSM.TrySetState(_Damage);
					}
					// 밀어내기
					else if (attackData._ForceForward > 0f && attackData._ForceUp == 0f)
					{
						_Impulse = new(0f, 0f, attackData._ForceForward);
						_Impulse = attacker.transform.TransformDirection(_Impulse);
						_FadeOutDeaccelTimer = attackData._DamageDuration;
						_Damage._Asset = _DamageAssets.PickOne();
						_FSM.TrySetState(_Damage);
					}
					// 날리기
					else 
					{
						_Impulse = new(0f, attackData._ForceUp, attackData._ForceForward);
						_Impulse = attacker.transform.TransformDirection(_Impulse);
						_FSM.TrySetState(_GetDown);
					}
					PlayEffect123123(attackData._HitEffectPrefab, attacker, hit.point, Quaternion.LookRotation(attackDir));
					PlayEffect123123(_HitEffectPrefab, this, hit.point, Quaternion.LookRotation(attackDir));
				}

				// 쓰러짐
				if (_HP <= 0)
				{
					_FSM.TrySetState(_Die);
					_Collider.enabled = false;
				}

				// 특수 공격으로 점프
				bool wallJump = attack._StateInfo == attacker._JumpSpecialAttack;
				wallJump &= !attack._AlreadyWallJump;
				if (wallJump)
				{
					attack._AlreadyWallJump = true;
					Vector3 jumpDir = attacker.Center - Center;
					jumpDir.y = 0f;
					attacker._AttackJumpDirection = jumpDir.normalized;
				}
			}
		}

		public void PlayEffect(Effect info, Character owner)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				yield return new WaitForSeconds(info._Delay);
				GameObject effect = Instantiate(info._EffectPrefab);
				BattleEffect e = effect.AddComponent<BattleEffect>();
				e._Owner = owner;
				e.Init();
				Data.SetupEffectPosition(effect, info, transform);
				ParticleSystem.MainModule main = effect.GetComponent<ParticleSystem>().main;
				yield return new WaitForSeconds(main.duration);
				Destroy(effect);
			}
		}

		public LayerMask GetLayerMask()
		{
			if (gameObject.layer == Layer.PlayerLayer) return Layer.PlayerLayerMask;
			else return Layer.EnemyLayerMask;
		}

		public LayerMask GetOppositeLayerMask()
		{
			if (gameObject.layer == Layer.PlayerLayer) return Layer.EnemyLayerMask;
			else return Layer.PlayerLayerMask;
		}

		bool IsMovable()
		{
			bool movable = true;
			movable &= _FSM.CurrentState != _Damage;
			movable &= _FSM.CurrentState != _GetDown;
			movable &= _FSM.CurrentState != _GetUp;
			movable &= _FSM.CurrentState != _Die;
			return movable;
		}

		bool IsDashing()
		{
			return Time.time - _LastDashTime < _DashDuration;
		}

		void DashCancel()
		{
			_LastDashTime = TimeDefault;
		}

		bool IsGuarding()
		{
			return _UpperBodyLayer.Weight > 0f;
		}

		void GuardCancel()
		{
			_UpperBodyLayer.SetWeight(0f);
		}

		bool IsGuardingEffective()
		{
			bool guard = _UpperBodyLayer.Weight > 0f;
			AnimancerState guardDownState = _UpperBodyLayer.GetOrCreateState(_GuardDownAsset.Transition);
			guard &= _UpperBodyLayer.CurrentState != guardDownState;

			// 가드 해제 그레이스 타임
			guard |= Time.time - _GuardDownTime < 0.1f;

			return guard;
		}

		public bool IsHitStun()
		{
			return _HitStunTimer > 0f;
		}

		public void AddHitStunTimer(float t)
		{
			if (_HitStunTimer < 0f)
			{
				_HitStunPrevVelocity = _Motor.Velocity;
			}
			_HitStunTimer += t;
		}

		void JustGuard()
		{
			if (_JustGuardCoroutine != null)
			{
				StopCoroutine(_JustGuardCoroutine);
				_JustGuardCoroutine = null;
			}
			_JustGuardCoroutine = StartCoroutine(Internal());

			IEnumerator Internal()
			{
				_JustGuardEffect.gameObject.SetActive(true);
				_JustGuardEffect.Play(true);
				float start = Time.time;
				_JustGuardCancelTrigger = false;
				yield return new WaitUntil(() => Time.time - start > 1.2f || _JustGuardCancelTrigger);
				_JustGuardCancelTrigger = false;
				_JustGuardEffect.gameObject.SetActive(false);
				_JustGuardCoroutine = null;
			}
		}

		bool IsJustGuard()
		{
			return _JustGuardCoroutine != null;
		}

		void CooltimeJitter()
		{
			_CooltimeJitter.DOShakePosition(0.3f, strength: 0.06f, vibrato: 100, fadeOut: false).SetEase(Ease.Flash)
				.OnKill(() => _CooltimeJitter.localPosition = Vector3.zero);
		}

		public void PlayEffect123123(GameObject prefab, Character owner, Vector3 pos, Quaternion rot)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				GameObject hitEffect = Instantiate(prefab, pos, rot);
				BattleEffect e = hitEffect.AddComponent<BattleEffect>();
				e._Owner = owner;
				e.Init();
				yield return new WaitForSeconds(3f);
				Destroy(hitEffect);
			}
		}
	}
}
