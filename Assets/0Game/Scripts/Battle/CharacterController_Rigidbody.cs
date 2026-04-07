using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using KinematicCharacterController;
using static SingletonManager;

namespace Battle
{
	public class CharacterController_Rigidbody : MonoBehaviour, IMoverController
	{
		[HideInInspector] public PhysicsMover _Mover;
		[HideInInspector] public Collider _Collider;

		Character c;
		Vector3 _CurrentVelocity;
		Quaternion _CurrentRotation;

		public void Init()
		{
			c = GetComponent<Character>();
			_Collider = GetComponentInChildren<Collider>();
			_Mover = GetComponent<PhysicsMover>();

			_CurrentVelocity = Vector3.zero;
			_CurrentRotation = Quaternion.identity;
			_Mover.MoverController = this;
			_Mover.Init();
		}

		public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
		{
			// 캐릭터 이동 처리
			c.UpdateRotation_Shared(ref _CurrentRotation, deltaTime);
			c.UpdateVelocity_Shared1(ref _CurrentVelocity, deltaTime);
			c.InputMoveProcess(ref _CurrentVelocity, deltaTime);
			//c.UpdateVelocity_Shared2(ref _CurrentVelocity, deltaTime);
			//c.UpdateVelocity_Shared3(ref _CurrentVelocity, deltaTime);

			// 이동 위치, 회전 설정
			goalPosition = transform.position + _CurrentVelocity * deltaTime;
			goalRotation = _CurrentRotation;

			// 루트 모션 초기화
			c._RootMotionPosDelta = Vector3.zero;
			c._RootMotionRotDelta = Quaternion.identity;
		}
	}
}
