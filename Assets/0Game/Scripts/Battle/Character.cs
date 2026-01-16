using Animancer;
using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
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
			//Inputs.NormalAttack.performed += ;
			//Inputs.Guard.performed += _GuardAction;
		}

		void OnDisable()
		{
			Inputs.Dash -= Dash;
			Inputs.Jump.performed -= Jump;
			//Inputs.NormalAttack.performed -= _NormalAttackAction;
			//Inputs.Guard.performed -= _GuardAction;
		}

		void Update()
		{
			// 카메라 회전
			_LookRotation.x += Inputs.Look.y * _CameraRotationSpeed * Time.deltaTime * -1f;
			_LookRotation.x = Mathf.Clamp(_LookRotation.x, -85f, 85f);
			_LookRotation.y += Inputs.Look.x * _CameraRotationSpeed * Time.deltaTime;
			_CameraTarget.eulerAngles = _LookRotation;

			// 애니메이션
			UpdateFSM();
		}
	}
}
