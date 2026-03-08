using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using static SingletonManager;
using static UnityEngine.InputSystem.InputAction;

namespace Battle
{
	public partial class Player : MonoBehaviour
	{
		public Transform _CameraTarget;
		public CinemachineThirdPersonFollow _CameraThirdPerson;
		public float _CameraRotationSpeed;

		[HideInInspector] public Character _Character;
		[HideInInspector] public Vector2 _LookInput;
		[HideInInspector] public Vector2 _LookRotation;
		[HideInInspector] public UI_PlayerHP _UI_HP;

		public void Init()
		{
			_Character = GetComponent<Character>();
			_Character.Init();
		}

		public void ReceiveInput(bool on)
		{
			if (on)
			{
				Inputs.Movement.performed += Move;
				Inputs.Look.performed += Look;
				Inputs.Dash += _Character.Dash;
				Inputs.Jump.performed += _Character.Jump;
				Inputs.Guard.performed += _Character.Guard;
				Inputs.NormalAttack.performed += _Character.NormalAttack;
				Inputs.SpecialAttack.performed += _Character.SpecialAttack;
				Inputs.Skill1.performed += _Character.Skill1;
				Inputs.Skill2.performed += _Character.Skill2;
				Inputs.Ultimate.performed += _Character.Ultimate;
				Inputs.Character1.performed += _Character.Character1;
				Inputs.Character2.performed += _Character.Character2;
			}
			else
			{
				Inputs.Movement.performed -= Move;
				Inputs.Look.performed -= Look;
				Inputs.Dash -= _Character.Dash;
				Inputs.Jump.performed -= _Character.Jump;
				Inputs.Guard.performed -= _Character.Guard;
				Inputs.NormalAttack.performed -= _Character.NormalAttack;
				Inputs.SpecialAttack.performed -= _Character.SpecialAttack;
				Inputs.Skill1.performed -= _Character.Skill1;
				Inputs.Skill2.performed -= _Character.Skill2;
				Inputs.Ultimate.performed -= _Character.Ultimate;
				Inputs.Character1.performed -= _Character.Character1;
				Inputs.Character2.performed -= _Character.Character2;
			}
		}

		void Update()
		{
			// 카메라 위치
			float shoulderHeight = _Character._Motor.GroundingStatus.FoundAnyGround ? _Character._ShoulderHeight : _Character._ShoulderHeight - 0.5f;
			float y = Mathf.MoveTowards(_CameraThirdPerson.ShoulderOffset.y, shoulderHeight, 5f * Time.deltaTime);
			_CameraThirdPerson.ShoulderOffset.y = y;

			// 카메라 회전
			_LookRotation.x += _LookInput.y * _CameraRotationSpeed * -1f * 0.01f;
			_LookRotation.x = Mathf.Clamp(_LookRotation.x, -80f, 60f);
			_LookRotation.y += _LookInput.x * _CameraRotationSpeed * 0.01f;
			if (_LookRotation.y - transform.eulerAngles.y > 180f)
			{
				_LookRotation.y -= 360f;
			}
			else if (_LookRotation.y - transform.eulerAngles.y < -180f)
			{
				_LookRotation.y += 360f;
			}
			_LookRotation.y = Mathf.Clamp(_LookRotation.y, transform.eulerAngles.y - 60f, transform.eulerAngles.y + 60f);
			_CameraTarget.SetPositionAndRotation(transform.position, Quaternion.Euler(_LookRotation));
			_Character._AimDestRotation = _CameraTarget.rotation;
		}

		void Move(CallbackContext obj)
		{
			_Character._MoveInput = obj.ReadValue<Vector2>().Vector2ToXZ();
		}

		void Look(CallbackContext obj)
		{
			_LookInput = obj.ReadValue<Vector2>();
		}
	}
}
