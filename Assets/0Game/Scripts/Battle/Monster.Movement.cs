using Animancer;
using Animancer.FSM;
using KinematicCharacterController;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem.Interactions;
using static SingletonManager;
using static UnityEngine.InputSystem.InputAction;

namespace Battle
{
	public partial class Monster
	{

		public void BeforeCharacterUpdate(float deltaTime)
		{
			// This is called before the motor does anything
		}

		public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
		{
		}

		public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
		}

		public void AfterCharacterUpdate(float deltaTime)
		{
		}

		public bool IsColliderValidForCollisions(Collider coll)
		{
			// This is called after when the motor wants to know if the collider can be collided with (or if we just go through it)
			return true;
		}

		public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
		{
			// This is called when the motor's ground probing detects a ground hit
		}

		public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
		{
			// This is called when the motor's movement logic detects a hit
		}

		public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
		{
			// This is called after every hit detected in the motor, to give you a chance to modify the HitStabilityReport any way you want
		}

		public void PostGroundingUpdate(float deltaTime)
		{
			// This is called after the motor has finished its ground probing, but before PhysicsMover/Velocity/etc.... handling
		}

		public void OnDiscreteCollisionDetected(Collider hitCollider)
		{
			// This is called by the motor when it is detecting a collision that did not result from a "movement hit".
		}
	}
}
