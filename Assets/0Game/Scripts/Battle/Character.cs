using Animancer;
using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using VRM;
using static DataManager;
using static PlasticGui.WorkspaceWindow.Merge.MergeInProgress;
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
		RaycastHit[] _MeleeAttackRaycastResults;

		public Vector3 Center => transform.position + _Collider.center;

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
				AttackCollider attack = Instantiate(_MeleeAttackCollider);
				attack._StateInfo = _FSM.CurrentState;
				attack.transform.SetPositionAndRotation(transform.position, transform.rotation);
				Vector3 attackPos = Center;
				if (attack._StateInfo._EffectInfo != null)
				{
					yield return new WaitForSeconds(attack._StateInfo._EffectInfo._Delay);
				}

				// 히트 판정
				int count = Physics.OverlapBoxNonAlloc(
					center: attack._Collider.GetCenter(),
					halfExtents: attack._Collider.size / 2f,
					results: _MeleeAttackResults,
					orientation: attack.transform.rotation,
					mask: GetOppositeLayerMask());
				for (int i = 0; i < count; i++)
				{
					Collider result = _MeleeAttackResults[i];
					Character c = result.GetComponent<Character>();
					c.TakeDamage(this, attack);
				}
				Destroy(attack.gameObject);
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
                    results: _MeleeAttackRaycastResults,
                    maxDistance: 100f,
                    layerMask: GetLayerMask());
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

                EffectInfo info = attack._StateInfo._EffectInfo;
                if (info == null) yield break;
				float delay = info._HitDelay - info._Delay;
				yield return new WaitForSeconds(delay);

				// 역경직

				// 가드 판정
				bool guard = IsGuardingEffective();
				print(Vector3.Angle(transform.forward, new(-attackDir.x, 0f, -attackDir.z)));
                float angle = Vector3.Angle(transform.forward, new(-attackDir.x, 0f, -attackDir.z));
				guard &= angle < 90f;
				if (guard)
				{
					_DeaccelFlag = true;
					GameObject hitEffect = Instantiate(_GuardEffectPrefab, hit.point, Quaternion.identity);
					yield return new WaitForSeconds(3f);
					Destroy(hitEffect);
				}
				else
				{
					_Damage._Duration = info._DamageDuration;
					_FSM.TrySetState(_Damage);
					GameObject hitEffect = Instantiate(info._HitEffectPrefab, hit.point, Quaternion.identity);
					yield return new WaitForSeconds(3f);
					Destroy(hitEffect);
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

		bool IsGuarding() => _UpperBodyLayer.Weight > 0f;
		bool IsGuardingEffective() => _UpperBodyLayer.Weight > 0.9f;
	}
}
