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

		Character _Character;
		Vector2 _LookInput;
		Vector2 _LookRotation;

		void Awake()
		{
			_Character = GetComponent<Character>();
		}

		void OnEnable()
		{
			Inputs.Movement.performed += _Character.Move;
			Inputs.Look.performed += Look;
			Inputs.Dash += _Character.Dash;
			Inputs.Jump.performed += _Character.Jump;
			Inputs.NormalAttack.performed += _Character.NormalAttack;
			Inputs.Guard.performed += _Character.Guard;
		}

		void OnDisable()
		{
			Inputs.Movement.performed -= _Character.Move;
			Inputs.Look.performed -= Look;
			Inputs.Dash -= _Character.Dash;
			Inputs.Jump.performed -= _Character.Jump;
			Inputs.NormalAttack.performed -= _Character.NormalAttack;
			Inputs.Guard.performed -= _Character.Guard;
		}

		void Update()
		{
            // 카메라 위치
            float y = Mathf.MoveTowards(_CameraThirdPerson.ShoulderOffset.y, _Character._Motor.GroundingStatus.FoundAnyGround ? 0f : -0.5f, 5f * Time.deltaTime);
			_CameraThirdPerson.ShoulderOffset.y = y;

			// 카메라 회전
			_LookRotation.x += _LookInput.y * _CameraRotationSpeed * -1f * 0.01f;
			_LookRotation.x = Mathf.Clamp(_LookRotation.x, -80f, 80f);
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

		void Look(CallbackContext obj)
		{
			_LookInput = obj.ReadValue<Vector2>();
		}
	}
}
