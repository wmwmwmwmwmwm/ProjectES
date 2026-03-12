using Animancer;
using DG.Tweening;
using KinematicCharacterController;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRM;
using static DataManager;
using static SingletonManager;
using static UnityEngine.InputSystem.InputAction;

namespace Battle
{
	public partial class Character : MonoBehaviour, ICharacterController
	{
		[BoxGroup("설정")] public GameObject _ModelPrefab;
		[BoxGroup("설정")] public float _RotationSpeed;
		[BoxGroup("설정")] public float _MoveSpeed;
		[BoxGroup("설정")] public float _MoveAccel;
		[BoxGroup("설정")] public float _DashDuration;
		[BoxGroup("설정")] public float _JumpSpeed;
		[BoxGroup("설정")] public float _ShoulderHeight;
		[BoxGroup("설정")] public float _MaxHP;

		public VRMBlendShapeProxy _BlendShapeProxy;
		public GameObject _HitEffectPrefab;
		public GameObject _GuardEffectPrefab;
		public ParticleSystem _DashWindEffect;
		public ParticleSystem _FeetSmokeLeftEffect, _FeetSmokeRightEffect;
		public Material _WhiteMaterial;

		Player _Player;
		Enemy _Enemy;
		CapsuleCollider _Collider;
		RaycastHit[] _RaycastResults;
		[HideInInspector] public int _AttackIndex;
		[HideInInspector] public bool _NextAttackAvailable;
		[HideInInspector] public bool _NextAttackInput;
		float _AttackMovePercent;
		float _GuardUpTime, _GuardDownTime;
		float _HitStunTimer;
		Vector3 _HitStunPrevVelocity;
		bool _IsRunning;
		Coroutine _FeetSmokeCoroutine;
		Coroutine _DashWindCoroutine;
		[HideInInspector] public float _LastSkill1Time, _LastSkill2Time, _LastUltimateTime;
		[HideInInspector] public bool _AlreadyWallJump;

		const float MoveGraceDuration = 0.1f;
		const float WallJumpAngleThreshold = 60f;

		public BattleController Controller => BattleController.Instance;
		public Vector3 Center => transform.position + _Collider.center;
		public Vector3 Top => transform.position + _Motor.CharacterTransformToCapsuleTop;
		public Vector3 Bottom => transform.position + _Motor.CharacterTransformToCapsuleBottom;

		public void Init()
		{
			_Player = GetComponent<Player>();
			_Enemy = GetComponent<Enemy>();
			_Collider = GetComponent<CapsuleCollider>();
			_RaycastResults = new RaycastHit[10];

			// 시간 초기화
			_GuardUpTime = Const.TimeDefault;
			_GuardDownTime = Const.TimeDefault;
			_LastRequestTime = Const.TimeDefault;
			_LastCanJumpTime = Const.TimeDefault;
			_LastDashTime = Const.TimeDefault;
			_LastSkill1Time = Const.TimeDefault;
			_LastSkill2Time = Const.TimeDefault;
			_LastUltimateTime = Const.TimeDefault;

			// 모델 생성
			Transform parent = _Player ? _Player._CooltimeJitter : transform;
			GameObject model = Instantiate(_ModelPrefab, parent);
			_Animancer = model.AddComponent<AnimancerComponent>();
			_Animancer.Animator = model.GetComponent<Animator>();
			RedirectRootMotionToCharacter redirect = model.AddComponent<RedirectRootMotionToCharacter>();
			redirect._Animator = _Animancer.Animator;
			redirect._Character = this;

			// 컴포넌트 초기화
			_Motor = GetComponent<KinematicCharacterMotor>();
			_Motor.Init();
			InitMovement();
			InitFSM();
			_AttackIndex = -1;
			EmitEffect(_DashWindEffect, false);
			EmitEffect(_FeetSmokeLeftEffect, false);
			EmitEffect(_FeetSmokeRightEffect, false);
			Transform leftFoot = _Animancer.Animator.avatar ? _Animancer.Animator.GetBoneTransform(HumanBodyBones.LeftFoot) : transform;
			_FeetSmokeLeftEffect.transform.SetParent(leftFoot, false);
			Transform rightFoot = _Animancer.Animator.avatar ? _Animancer.Animator.GetBoneTransform(HumanBodyBones.RightFoot) : transform;
			_FeetSmokeRightEffect.transform.SetParent(rightFoot, false);

			// 스탯 초기화
			SetHP(_MaxHP);

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
			if (_FSM.CurrentState.IsAttack)
			{
				canAttack &= _FSM.CurrentState._Attack._SkillType <= AttackSkillType.Normal;
			}
			if (!canAttack) return;

			// 저스트 가드
			if (_Player && _Player.IsJustGuard())
			{
				Play(_GuardAttack);
				Attack();
				_Player._JustGuardCancelTrigger = true;
			}
			// 대쉬 
			else if (IsDashing())
			{
				_FadeOutDeaccelTimer = _DashDuration;
				DashCancel();
				Play(_DashAttack);
				Attack();
			}
			// 지상
			else if (_Motor.GroundingStatus.IsStableOnGround && !_Motor.MustUnground())
			{
				// 1타 공격
				if (_AttackIndex < 0)
				{
					_AttackIndex = 0;
					Play(_NormalAttacks[_AttackIndex]);
					Attack();
				}
				// 2~타 공격
				else
				{
					_NextAttackInput = true;
				}
			}
			// 공중
			else if (_FSM.CurrentState != _JumpAttack) 
			{
				Play(_JumpAttack);
				Attack();
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
					if (_NormalAttacks.Contains(_FSM.CurrentState))
					{
						Play_Canceling(_SpecialAttack, true);
					}
					else return;
				}
				else
				{
					Play(_SpecialAttack);
				}
				Attack();
			}
			// 공중
			else
			{
				if (_FSM.CurrentState.IsAttack) return;

				Play(_JumpSpecialAttack);
				Attack();
			}
		}

		public void Skill1(CallbackContext obj)
		{
			bool canAttack = _FSM.CurrentState._CanAttack;
			canAttack &= !IsGuarding();
			if (!canAttack) return;

			// 쿨타임
			if (_Player && Time.time - _LastSkill1Time < _Skill1._Attack._Cooltime)
			{
				_Player.CooltimeJitter();
				return;
			}

			if (_FSM.CurrentState.IsAttack)
			{
				if (_FSM.CurrentState._Attack._SkillType < AttackSkillType.Skill)
				{
					Play_Canceling(_Skill1, true);
				}
				else return;
			}
			else
			{
				Play(_Skill1);
			}
			Attack();
			_LastSkill1Time = Time.time;
		}

		public void Skill2(CallbackContext obj)
		{
			bool canAttack = _FSM.CurrentState._CanAttack;
			canAttack &= !IsGuarding();
			if (!canAttack) return;

			// 쿨타임
			if (_Player && Time.time - _LastSkill2Time < _Skill2._Attack._Cooltime)
			{
				_Player.CooltimeJitter();
				return;
			}

			if (_FSM.CurrentState.IsAttack)
			{
				if (_FSM.CurrentState._Attack._SkillType < AttackSkillType.Skill)
				{
					Play_Canceling(_Skill2, true);
				}
				else return;
			}
			else
			{
				Play(_Skill2);
			}
			Attack();
			_LastSkill2Time = Time.time;
		}

		public void Ultimate(CallbackContext obj)
		{
			bool canAttack = _FSM.CurrentState._CanAttack;
			canAttack &= !IsGuarding();
			if (!canAttack) return;

			// 쿨타임
			if (_Player && Time.time - _LastUltimateTime < _Ultimate._Attack._Cooltime)
			{
				_Player.CooltimeJitter();
				return;
			}

			if (_FSM.CurrentState.IsAttack)
			{
				if (_FSM.CurrentState._Attack._SkillType < AttackSkillType.Ultimate)
				{
					Play_Canceling(_Ultimate, true);
				}
				else return;
			}
			else
			{
				Play(_Ultimate);
			}
			Attack();
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

		void Attack()
		{
			switch (_FSM.CurrentState._Attack._RangeType)
			{
				case AttackRangeType.Melee:
					MeleeAttack();
					break;
				case AttackRangeType.Range:
					FireMissile();
					break;
			}
		}

		void MeleeAttack()
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				State state = _FSM.CurrentState;
				Effect firstEffectData = state._EffectDatas.First();
				List<Effect>.Enumerator effectEnum = state._EffectDatas.GetEnumerator();
				BattleAttack attackData = state._Attack;

				// 공격 최소 딜레이
				yield return new WaitForSeconds(firstEffectData._Delay * 0.3f);

				BattleAttack attack = Instantiate(state._Attack, transform);
				attack._Owner = this;
				attack._StateInfo = state;
				MeleeAttack melee = attack.GetComponent<MeleeAttack>();

				foreach (AttackHit attackHit in melee._AttackHits)
				{
					if (_FSM.CurrentState != state) yield break;

					bool hasNext = effectEnum.MoveNext();
					Effect effectData = hasNext ? effectEnum.Current : firstEffectData;

					// 공중 공격으로 점프
					int raycastCount = Physics.RaycastNonAlloc(
							origin: Center,
							direction: _Motor.CharacterForward,
							results: _RaycastResults,
							maxDistance: melee._Collider.size.z,
							layerMask: Layer.TerrainLayerMask);
					List<RaycastHit> hits = _RaycastResults.ArrayToList(raycastCount);
					RaycastHit nearest = hits.MinBy(x => x.distance);
					if (raycastCount > 0 && attackData._SkillType == AttackSkillType.Normal)
					{
						float angle = Vector3.Angle(_Motor.CharacterForward, -nearest.normal);
						if (angle < WallJumpAngleThreshold && !_Motor.GroundingStatus.IsStableOnGround)
						{
							_AttackJumpDirection = -_Motor.CharacterForward;
						}
					}

					// 딜레이
					float delay = melee._AttackHits.IndexOf(attackHit) == 0 ? effectData._Delay * 0.7f : effectData._Delay;
					yield return new WaitForSeconds(delay);

					// 이펙트
					PlayEffect(effectData);

					// 히트 판정
					int overlapCount = Physics.OverlapBoxNonAlloc(
						center: melee._Collider.GetCenter(),
						halfExtents: melee._Collider.size / 2f,
						results: melee._HitResults,
						orientation: attack.transform.rotation,
						mask: GetOppositeLayerMask());

					// 공격 적중
					if (overlapCount > 0)
					{
						List<Collider> overlaps = melee._HitResults.ArrayToList(overlapCount);
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
							Vector3 attackDir = c.Center - Center;
							attackDir.Normalize();
							int count = Physics.RaycastNonAlloc(
								origin: Center,
								direction: attackDir,
								results: _RaycastResults,
								maxDistance: 100f,
								layerMask: GetOppositeLayerMask());
							RaycastHit hit = default;
							for (int i = 0; i < count; i++)
							{
								RaycastHit iter = _RaycastResults[i];
								if (col == iter.collider)
								{
									hit = iter;
									break;
								}
							}
							c.TakeDamage(this, attack, attackHit, hit.point, attackDir);
							yield return new WaitForSeconds(attackHit._AttackerHitStunDuration);
						}
					}
					// 벽에 적중
					else if (raycastCount > 0)
					{
						Controller.PlayEffect123123(_GuardEffectPrefab, this, nearest.point, Quaternion.identity);
					}
				}

				Destroy(attack.gameObject);
			}
		}

		void FireMissile()
		{
			Controller.StartCoroutine(Internal());
			IEnumerator Internal()
			{
				State state = _FSM.CurrentState;
				BattleAttack attack = Instantiate(state._Attack);
				attack._Owner = this;
				attack._StateInfo = state;
				Effect effectData = state._EffectDatas.First();
				Missile missile = attack.GetComponent<Missile>();
				missile.transform.SetPositionAndRotation(Center, _AimDestRotation);

				// 딜레이
				yield return new WaitForSeconds(effectData._Delay);

				// 이펙트
				PlayEffect(effectData);

				float startTime = Time.time;
				yield return new WaitUntil(() => Time.time - startTime > missile._Duration || missile._DestroyTrigger);

				Destroy(attack.gameObject);
			}
		}

		public void TakeDamage(Character attacker, BattleAttack attack, AttackHit attackHit, Vector3? hitPoint, Vector3 attackDirection)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				// 이펙트 위치
				Vector3 effectPosition = hitPoint != null ? hitPoint.Value : Center;
				Vector3 damageTextPosition = hitPoint != null ? hitPoint.Value : Top;

				// 주변 적 활성화
				if (_Enemy)
				{
					_Enemy.NoticeAround();
				}

				// 공격 판정 딜레이
				Effect effectData = attack._StateInfo._EffectDatas.First();
				float delay = attackHit._HitDelay - effectData._Delay;
				yield return new WaitForSeconds(delay);

				// 경직
				AddHitStunTimer(attackHit._AttackerHitStunDuration);
				attacker.AddHitStunTimer(attackHit._AttackerHitStunDuration);

				// 가드 판정
				float damage = attackHit._Damage;
				bool guard = _Player;
				guard &= IsGuardingEffective();
				float angle = Vector3.Angle(transform.forward, new(-attackDirection.x, 0f, -attackDirection.z));
				guard &= angle < 90f;
				if (guard)
				{
					// 저스트 가드
					bool justGuard = Time.time - _GuardUpTime < 0.3f;
					justGuard &= Inputs.Guard.IsPressed();
					justGuard &= attack._RangeType == AttackRangeType.Melee;
					justGuard &= _Player;
					if (justGuard)
					{
						_Player.JustGuard();
						damage = 0f;
					}
					// 일반 가드
					else
					{
						_FadeInDeaccelTimer = 0.6f;
						damage *= attack._AreaType == AttackAreaType.Single ? 0f : 0.5f;
					}
					Controller.PlayEffect123123(_GuardEffectPrefab, this, effectPosition, Quaternion.LookRotation(attackDirection));
				}
				else
				{
					// 일반 경직
					if (attackHit._ForceForward == 0f && attackHit._ForceUp == 0f)
					{
						_Damage._Duration = attackHit._DamageDuration;
						_FadeInDeaccelTimer = attackHit._DamageDuration;
						_Damage.SetAsset(_DamageAssets.PickOne());
						Play(_Damage);
					}
					// 밀어내기
					else if (attackHit._ForceForward != 0f && attackHit._ForceUp == 0f)
					{
						_Impulse = new(0f, 0f, attackHit._ForceForward);
						_Impulse = attacker.transform.TransformDirection(_Impulse);
						_FadeOutDeaccelTimer = attackHit._DamageDuration;
						_Damage.SetAsset(_DamageAssets.PickOne());
						Play(_Damage);
						FeetSmoke(attackHit._DamageDuration);
					}
					// 날리기
					else 
					{
						_Impulse = new(0f, attackHit._ForceUp, attackHit._ForceForward);
						_Impulse = attacker.transform.TransformDirection(_Impulse);
						_FSM.TrySetState(_GetDown);
					}

					// 이펙트
					Controller.PlayEffect123123(attackHit._HitEffectPrefab, attacker, effectPosition, Quaternion.LookRotation(attackDirection));
					Controller.PlayEffect123123(_HitEffectPrefab, this, effectPosition, Quaternion.LookRotation(attackDirection));
				}

				// 데미지 표시
				SetHP(_HP - damage);
				if (_Enemy)
				{
					Controller.ShowDamageText(damage, damageTextPosition);
				}
				else
				{
					if (damage > 0f)
					{
						Controller.ShowDamageScreen();
					}
				}

				// 쓰러짐
				if (_HP <= 0f)
				{
					_FSM.TrySetState(_Die);
					_Collider.enabled = false;
				}

				// 특수 공격으로 점프
				bool wallJump = attack._StateInfo == attacker._JumpSpecialAttack;
				wallJump &= !attacker._AlreadyWallJump;
				if (wallJump)
				{
					attacker._AlreadyWallJump = true;
					Vector3 jumpDir = attacker.Center - Center;
					jumpDir.y = 0f;
					attacker._AttackJumpDirection = jumpDir.normalized;
				}
			}
		}

		void PlayEffect(Effect info)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				GameObject effect = Instantiate(info._EffectPrefab);
				BattleEffect e = effect.AddComponent<BattleEffect>();
				e._Owner = this;
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

		public bool IsDead()
		{
			return _HP <= 0f;
		}

		void DashCancel()
		{
			_LastDashTime = Const.TimeDefault;
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

		void FeetSmoke(float time)
		{
			if (_FeetSmokeCoroutine != null)
			{
				StopCoroutine(_FeetSmokeCoroutine);
				_FeetSmokeCoroutine = null;
			}
			_FeetSmokeCoroutine = StartCoroutine(Internal());

			IEnumerator Internal()
			{
				EmitEffect(_FeetSmokeLeftEffect, true);
				EmitEffect(_FeetSmokeRightEffect, true);
				float start = Time.time;
				yield return new WaitUntil(() => Time.time - start > time || !_Motor.GroundingStatus.IsStableOnGround);
				EmitEffect(_FeetSmokeLeftEffect, false);
				EmitEffect(_FeetSmokeRightEffect, false);
				_FeetSmokeCoroutine = null;
			}
		}

		void DashWind(MoveRequest dashDir)
		{
			if (_DashWindCoroutine != null)
			{
				StopCoroutine(_DashWindCoroutine);
				_DashWindCoroutine = null;
			}
			_DashWindCoroutine = StartCoroutine(Internal());

			IEnumerator Internal()
			{
				_DashWindEffect.transform.localEulerAngles = dashDir switch
				{
					MoveRequest.DashFwd => new Vector3(0f, 0f, 0f),
					MoveRequest.DashBwd => new Vector3(0f, 180f, 0f),
					MoveRequest.DashLeft => new Vector3(0f, 270f, 0f),
					_ => new Vector3(0f, 90f, 0f),
				};
				EmitEffect(_DashWindEffect, true);
				yield return new WaitUntil(() => !IsDashing() && !_IsRunning);
				EmitEffect(_DashWindEffect, false);
				_DashWindCoroutine = null;
			}
		}

		public void EmitEffect(ParticleSystem particle, bool on)
		{
			ParticleSystem[] particles = particle.GetComponentsInChildren<ParticleSystem>();
			foreach (ParticleSystem p in particles)
			{
				ParticleSystem.EmissionModule emission = p.emission;
				emission.enabled = on;
				if (on)
				{
					p.time = 0f;
					p.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
					p.Play();
				}
			}
		}
	}
}
