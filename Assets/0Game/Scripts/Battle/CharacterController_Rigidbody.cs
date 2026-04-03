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

		public void Init()
		{
			c = GetComponent<Character>();
			_Collider = GetComponentInChildren<Collider>();
			_Mover = GetComponent<PhysicsMover>();
			_Mover.MoverController = this;
			_Mover.Init();
		}

		public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
		{
			// Remember pose before animation
			transform.GetPositionAndRotation(out Vector3 beforePos, out Quaternion beforeRot);

			// Update animation
			// 트랜스폼 이동

			// Set our platform's goal pose to the animation's
			goalPosition = transform.position;
			goalRotation = transform.rotation;

			// Reset the actual transform pose to where it was before evaluating. 
			// This is so that the real movement can be handled by the physics mover; not the animation
			transform.SetPositionAndRotation(beforePos, beforeRot);

			c._RootMotionPosDelta = Vector3.zero;
			c._RootMotionRotDelta = Quaternion.identity;
		}
	}
}
