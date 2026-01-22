using Animancer;
using KinematicCharacterController;
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
		public Transform _CameraTarget;

		void Start()
		{
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

		void OnEnable()
		{
			Inputs.Dash += Dash;
			Inputs.Jump.performed += Jump;
			Inputs.NormalAttack.performed += NormalAttack;
			Inputs.Guard.performed += Guard;
		}

		void OnDisable()
		{
			Inputs.Dash -= Dash;
			Inputs.Jump.performed -= Jump;
			Inputs.NormalAttack.performed -= NormalAttack;
			Inputs.Guard.performed -= Guard;
		}

		void Update()
		{
			// 카메라 회전
			_LookRotation.x += Inputs.Look.y * _CameraRotationSpeed * -1f * 0.01f;
			_LookRotation.x = Mathf.Clamp(_LookRotation.x, -85f, 85f);
			_LookRotation.y += Inputs.Look.x * _CameraRotationSpeed * 0.01f;
			_LookRotation.y = Mathf.Clamp(_LookRotation.y, transform.eulerAngles.y - 60f, transform.eulerAngles.y + 60f);
			_CameraTarget.SetPositionAndRotation(transform.position, Quaternion.Euler(_LookRotation));

			// 애니메이션
			UpdateFSM();
		}

		void Dash(Direction4 dir)
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

		void Jump(CallbackContext obj)
		{
			_MoveRequest = MoveRequest.Jump;
			_LastRequestTime = Time.time;
		}

		void NormalAttack(CallbackContext obj)
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
			}
		}

		void NextAttack()
		{
			if (!_NextAttackInput) return;

			_NextAttackInput = false;
			_AttackIndex++;
			_FSM.TrySetState(_NormalAttacks[_AttackIndex]);
		}

		void Guard(CallbackContext obj)
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

			if (Inputs.Guard.WasReleasedThisFrame() && IsGuarding())
			{
				AnimancerState state = _UpperBodyLayer.Play(_GuardDownAsset);
				state.Time = 0f;
				state.Events(this).OnEnd ??= () =>
				{
					_UpperBodyLayer.SetWeight(0f);
				};
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
