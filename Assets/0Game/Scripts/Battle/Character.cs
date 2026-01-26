using Animancer;
using KinematicCharacterController;
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
		public VRMBlendShapeProxy _BlendShapeProxy;
		public AttackCollider _MeleeAttackCollider;

		CapsuleCollider _Collider;
		Collider[] _MeleeAttackResults;
		RaycastHit[] _MeleeAttackRaycastResults;

		Vector3 Center => transform.position + _Collider.center;

		void Start()
		{
			_MeleeAttackResults = new Collider[100];
			_MeleeAttackRaycastResults = new RaycastHit[10];
			_Collider = GetComponent<CapsuleCollider>();

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

			// 일반 공격
			if (_Motor.GroundingStatus.IsStableOnGround)
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

		void NextAttack()
		{
			if (!_NextAttackInput) return;

			_NextAttackInput = false;
			_AttackIndex++;
			_FSM.TrySetState(_NormalAttacks[_AttackIndex]);
			GiveDamage();
		}

		public void Guard(CallbackContext obj)
		{
			if (!_FSM.CurrentState._CanGuard) return;

			Play_Canceling(_Idle);
			_AttackIndex = -1;

			if (Inputs.Guard.WasPressedThisFrame())
			{
				_UpperBodyLayer.SetWeight(1f);
				AnimancerState state = _UpperBodyLayer.Play(_GuardUpAsset);
				state.Time = 0f;
			}

			if (!Inputs.Guard.IsPressed() && IsGuarding())
			{
				AnimancerState state = _UpperBodyLayer.Play(_GuardDownAsset);
				state.Events(this).OnEnd ??= () =>
				{
					_UpperBodyLayer.SetWeight(0f);
				};
			}
		}

		public void GiveDamage()
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				State state = _FSM.CurrentState;
				AttackCollider attack = Instantiate(_MeleeAttackCollider);
				attack.transform.SetPositionAndRotation(transform.position, transform.rotation);
				yield return new WaitForSeconds(0.1f);
				if (state != _FSM.CurrentState)
				{
					Destroy(attack.gameObject);
					yield break;
				}
				yield return new WaitForSeconds(attack._HitDelay - 0.1f);

				// 히트 판정
				int count = Physics.OverlapBoxNonAlloc(
					center: attack.transform.position + attack._Collider.center,
					halfExtents: attack._Collider.size,
					results: _MeleeAttackResults,
					orientation: attack.transform.rotation,
					mask: Layer.EnemyLayer);
				for (int i = 0; i < count; i++)
				{
					Collider result = _MeleeAttackResults[i];
					Character c = result.GetComponent<Character>();
					c.TakeDamage(attack, Center);
				}
			}
		}

		public void TakeDamage(AttackCollider attack, Vector3 attackerPos)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				Vector3 direction = Center - attackerPos;
				direction.Normalize();
                int count = Physics.RaycastNonAlloc(
					origin: Center,
					direction: direction,
					results: _MeleeAttackRaycastResults,
					maxDistance: 100f,
					layerMask: Layer.EnemyLayer);
				RaycastHit hit = default;
				for (int i = 0; i < count; i++)
				{
                    RaycastHit iter = _MeleeAttackRaycastResults[i];
					if (_Collider == iter.collider)
					{
						hit = iter;
						break;
					}
				}
				//12321312121231232313231231212312




				GameObject hitEffect = Instantiate(attack._HitEffectPrefab, hit.point, Quaternion.identity);
				yield return new WaitForSeconds(3f);
				Destroy(hitEffect);
			}
		}

		public void PlayEffect(AnimationClip clip)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				if (!Data._EffectInfoDict.TryGetValue(clip, out EffectInfo info)) yield break;

				GameObject effect = Instantiate(info._EffectPrefab);
				Data.SetupEffect(effect, info, transform);
				ParticleSystem.MainModule main = effect.GetComponent<ParticleSystem>().main;
				yield return new WaitForSeconds(main.duration);
				Destroy(effect);
			}
		}

		bool IsGuarding() => _UpperBodyLayer.Weight > 0f;
		bool IsGuardingEffective() => _UpperBodyLayer.Weight > 0.9f;
	}
}
