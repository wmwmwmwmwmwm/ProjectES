using Animancer;
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
		public GameObject _GuardEffectPrefab;

		CapsuleCollider _Collider;
		Collider[] _MeleeAttackResults;
		RaycastHit[] _RaycastResults;
		[HideInInspector] public int _AttackIndex;
		[HideInInspector] public bool _NextAttackAvailable;
		[HideInInspector] public bool _NextAttackInput;
		float _AttackMovePercent;
		float _GuardDownTime;
		float _HitStunTime;
		bool _IsRunning;

		const float TimeDefault = -10000f;
		const float WallJumpAngleThreshold = 40f;

		public Vector3 Center => transform.position + _Collider.center;
		public Vector3 Bottom => transform.position + _Motor.CharacterTransformToCapsuleBottom;

		void Start()
		{
			_MeleeAttackResults = new Collider[100];
			_RaycastResults = new RaycastHit[10];
			_Collider = GetComponent<CapsuleCollider>();

			_GuardDownTime = TimeDefault;
			_HitStunTime = TimeDefault;
			_LastRequestTime = TimeDefault;
			_LastCanJumpTime = TimeDefault;
			_LastDashTime = TimeDefault;
			_DeaccelTime = TimeDefault;

			InitMovement();
			InitFSM();
			_AttackIndex = -1;

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
			if (Time.time - _HitStunTime < 0.03f)
			{
				_BaseLayer.Speed = 0f;
			}
			else
			{
				_BaseLayer.Speed = 1f;
			}
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
			// 공격 가능 상태가 아님
			if (!_FSM.CurrentState._CanAttack) return;

			// 가드 중
			if (IsGuarding()) return;

			// 대쉬 공격
			if (IsDashing() || _IsRunning)
			{
				_FSM.TrySetState(_DashAttack);
				GiveDamage();
			}
			// 일반 공격
			else if (_Motor.GroundingStatus.IsStableOnGround && !_Motor.MustUnground())
			{
				// 1타 공격
				if (_AttackIndex < 0 || _AttackIndex == _NormalAttacks.Count - 1)
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
			// 점프 공격
			else
			{
				_FSM.TrySetState(_JumpAttack);
				GiveDamage();
			}
		}

		public void SpecialAttack(CallbackContext obj)
		{
			// 공격 가능 상태가 아님
			if (!_FSM.CurrentState._CanAttack) return;

			// 가드 중
			if (IsGuarding()) return;

			// 대쉬 공격
			if (IsDashing() || _IsRunning)
			{
				_FSM.TrySetState(_DashAttack);
				GiveDamage();
			}
			// 일반 공격
			else if (_Motor.GroundingStatus.IsStableOnGround && !_Motor.MustUnground())
			{
				// 1타 공격
				if (_AttackIndex < 0 || _AttackIndex == _NormalAttacks.Count - 1)
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
			// 점프 공격
			else
			{
				_FSM.TrySetState(_JumpAttack);
				GiveDamage();
			}
		}

		public void Guard(CallbackContext obj)
		{
			if (!_FSM.CurrentState._CanGuard) return;

			// 가드 시작
			Play_Canceling(_Idle);
			if (Inputs.Guard.WasPressedThisFrame())
			{
				_UpperBodyLayer.SetWeight(1f);
				AnimancerState state = _UpperBodyLayer.Play(_GuardUpAsset);
				state.Time = 0f;
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
				AttackCollider attack = Instantiate(_MeleeAttackCollider);
				attack._StateInfo = _FSM.CurrentState;
				attack.transform.SetPositionAndRotation(transform.position, transform.rotation);

				// 공중 공격으로 점프
				RaycastHit[] raycastResults = new RaycastHit[10];
				int raycastCount = Raycast();
				List<RaycastHit> hits = raycastResults.ArrayToList(raycastCount);
				RaycastHit nearest = hits.MinBy(x => x.distance);
				if (raycastCount > 0)
				{
					float angle = Vector3.Angle(_Motor.CharacterForward, -nearest.normal);
					if (angle < WallJumpAngleThreshold && !_Motor.GroundingStatus.IsStableOnGround)
					{
						_AttackJumpTrigger = true;
					}
				}

				// 공격 판정 딜레이
				if (attack._StateInfo._EffectInfo != null)
				{
					yield return new WaitForSeconds(attack._StateInfo._EffectInfo._Delay);
				}

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
						yield return new WaitForSeconds(0.01f);
					}
				}
				// 벽에 적중
				else if(raycastCount > 0)
				{
					PlayEffect123123(_GuardEffectPrefab, nearest.point, Quaternion.identity);
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

				EffectInfo info = attack._StateInfo._EffectInfo;
				if (info == null) yield break;
				float delay = info._HitDelay - info._Delay;
				yield return new WaitForSeconds(delay);

				_HitStunTime = Time.time;
				attacker._HitStunTime = Time.time;

				// 가드 판정
				bool guard = IsGuardingEffective();
				float angle = Vector3.Angle(transform.forward, new(-attackDir.x, 0f, -attackDir.z));
				guard &= angle < 90f;
				if (guard)
				{
					_DeaccelTime = Time.time;
					PlayEffect123123(_GuardEffectPrefab, hit.point, Quaternion.LookRotation(attackDir));
				}
				else
				{
					if (info._ForceForward == 0f && info._ForceUp == 0f)
					{
						_Damage._Asset = _DamageAssets.PickOne();
						_Damage._Duration = info._DamageDuration;
						_FSM.TrySetState(_Damage);
					}
					else
					{
						_Impulse = new(0f, info._ForceUp, info._ForceForward);
						_Impulse = attacker.transform.TransformDirection(_Impulse);
						_FSM.TrySetState(_GetDown);
					}
					PlayEffect123123(info._HitEffectPrefab, hit.point, Quaternion.LookRotation(attackDir));
				}

				// 쓰러짐
				if (_HP <= 0)
				{
					_FSM.TrySetState(_Die);
					_Collider.enabled = false;
				}
			}
		}

		public void PlayEffect(EffectInfo info)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				GameObject effect = Instantiate(info._EffectPrefab);
				Data.SetupEffectPosition(effect, info, transform);
				ParticleSystem.MainModule main = effect.GetComponent<ParticleSystem>().main;
				yield return new WaitForSeconds(main.duration);
				Destroy(effect);
			}
		}

		public void PlayEffect123123(GameObject prefab, Vector3 pos, Quaternion rot)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				GameObject hitEffect = Instantiate(prefab, pos, rot);
				yield return new WaitForSeconds(3f);
				Destroy(hitEffect);
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
	}
}
