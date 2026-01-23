using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public partial class Player : MonoBehaviour
	{
		public Transform _CameraTarget;
		public float _CameraRotationSpeed;

		Character _Character;
		Vector2 _LookRotation;

		void Start()
		{
			_Character = GetComponent<Character>();
		}

		void OnEnable()
		{
			Inputs.Dash += _Character.Dash;
			Inputs.Jump.performed += _Character.Jump;
			Inputs.NormalAttack.performed += _Character.NormalAttack;
			Inputs.Guard.performed += _Character.Guard;
		}

		void OnDisable()
		{
			Inputs.Dash -= _Character.Dash;
			Inputs.Jump.performed -= _Character.Jump;
			Inputs.NormalAttack.performed -= _Character.NormalAttack;
			Inputs.Guard.performed -= _Character.Guard;
		}

		void Update()
		{
			// 카메라 회전
			_LookRotation.x += Inputs.Look.y * _CameraRotationSpeed * -1f * 0.01f;
			_LookRotation.x = Mathf.Clamp(_LookRotation.x, -85f, 85f);
			_LookRotation.y += Inputs.Look.x * _CameraRotationSpeed * 0.01f;
			_LookRotation.y = Mathf.Clamp(_LookRotation.y, transform.eulerAngles.y - 60f, transform.eulerAngles.y + 60f);
			_CameraTarget.SetPositionAndRotation(transform.position, Quaternion.Euler(_LookRotation));
			_Character._AimDestRotation = _CameraTarget.rotation;
		}
	}
}
