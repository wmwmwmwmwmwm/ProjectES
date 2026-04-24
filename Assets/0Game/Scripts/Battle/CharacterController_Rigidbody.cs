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
		public Transform _Fragment;
		public MeshRenderer _MeshRenderer;

		[HideInInspector] public PhysicsMover _Mover;
		[HideInInspector] public Collider _Collider;

		Character c;
		Vector3 _CurrentVelocity;
		Quaternion _CurrentRotation;
		RaycastHit _RaycastHit;

		public void Init()
		{
			c = GetComponent<Character>();
			_Collider = _MeshRenderer.GetComponent<Collider>();
			_Collider.gameObject.layer = Layer.EnemyLayer;
			_Mover = GetComponent<PhysicsMover>();

			_CurrentVelocity = Vector3.zero;
			_CurrentRotation = Quaternion.identity;
			_Mover.MoverController = this;
			_Mover.Init();
			_Fragment.gameObject.SetActive(false);
		}

		public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
		{
			if (c._MoveSpeed == 0f)
			{
				goalPosition = transform.position;
				goalRotation = transform.rotation;
				return;
			}

			// 캐릭터 이동 처리
			c.UpdateRotation_Shared(ref _CurrentRotation, deltaTime);
			c.UpdateVelocity_Shared1(ref _CurrentVelocity, deltaTime);
			c.InputMoveProcess(ref _CurrentVelocity, deltaTime);

			// 벽 판정
			Bounds bounds = _Collider.bounds;
			Vector3 extents = bounds.extents;
			extents.y = 0.1f;
			float castDistance = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.2f;
			bool hit = Physics.BoxCast(
				center: bounds.center,
				halfExtents: extents,
				direction: _CurrentVelocity,
				hitInfo: out _RaycastHit,
				orientation: _CurrentRotation,
				maxDistance: castDistance,
				layerMask: Layer.TerrainLayerMask);
			if (hit)
			{
				_CurrentVelocity = Vector3.ProjectOnPlane(_CurrentVelocity, _RaycastHit.normal).WithY(0f);
			}

			// 이동 위치, 회전 설정
			goalPosition = transform.position + _CurrentVelocity * deltaTime;
			goalRotation = _CurrentRotation;

			// 루트 모션 초기화
			c._RootMotionPosDelta = Vector3.zero;
			c._RootMotionRotDelta = Quaternion.identity;
		}

		public void ActivateFragment()
		{
			if (_Fragment.childCount == 0) return;

			_Fragment.gameObject.SetActive(true);
			_MeshRenderer.gameObject.SetActive(false);

			foreach (Transform child in _Fragment)
			{
				Rigidbody rigidbody = child.GetComponent<Rigidbody>();
				rigidbody.maxLinearVelocity = 50f / rigidbody.mass;
				rigidbody.maxAngularVelocity = 10f / rigidbody.mass;
				float force = 10f;
				rigidbody.AddExplosionForce(
					force,
					transform.position,
					0f,
					force / 3f,
					ForceMode.Impulse);
			}
		}
	}
}
