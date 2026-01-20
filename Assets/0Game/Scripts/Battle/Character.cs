using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRM;
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
			//Inputs.Guard.performed += _GuardAction;
		}

		void OnDisable()
		{
			Inputs.Dash -= Dash;
			Inputs.Jump.performed -= Jump;
			Inputs.NormalAttack.performed -= NormalAttack;
			//Inputs.Guard.performed -= _GuardAction;
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

		void NextAttack()
		{
			if (!_NextAttackInput) return;

			_NextAttackInput = false;
			_AttackIndex++;
			_FSM.TrySetState(_NormalAttacks[_AttackIndex]);
		}
	}
}
