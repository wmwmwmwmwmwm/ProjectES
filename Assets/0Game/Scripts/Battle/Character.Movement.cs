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
	public partial class Character
	{
		[Header("움직임")]
		public KinematicCharacterMotor _Motor;
		public float _Gravity;
		public float _CameraRotationSpeed;
		public float _RotationSpeed;
		public float _MoveSpeed;
		public float _MoveGroundAccel, _MoveAirAccel;
		public float _DashSpeed;
		public float _DashTime;
		public float _JumpSpeed;

		enum MoveRequest { None, Jump, DashFwd, DashBwd, DashLeft, DashRight }

		Vector2 _LookRotation;
		MoveRequest _MoveRequest;
		float _LastRequestTime;
		float _LastCanJumpTime;
		float _LastDashTime;
		Vector3 _DashDir;

		const float MoveGraceTime = 0.1f;

		void InitMovement()
		{
			_Motor.CharacterController = this;
			PositionConstraint posConstraint = _CameraTarget.GetComponent<PositionConstraint>();
			posConstraint.AddSource(new ConstraintSource()
			{
				sourceTransform = transform,
				weight = 1f,
			});
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

		public void BeforeCharacterUpdate(float deltaTime)
		{
			// This is called before the motor does anything
		}

		public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
		{
			// Y회전
			Quaternion destRot = Quaternion.Euler(0f, _CameraTarget.eulerAngles.y, 0f);
			currentRotation = Quaternion.RotateTowards(currentRotation, destRot, _RotationSpeed * deltaTime);
		}

		public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			// XZ축 이동
			float dashNormalizedTime = (Time.time - _LastDashTime) / _DashTime;
			Vector3 moveInputVector = _CameraTarget.rotation * Inputs.Movement.Vector2ToXZ();
			if (_FSM.CurrentState._CanMove)
			{
				if (_Motor.GroundingStatus.IsStableOnGround)
				{
					Vector3 targetVelocity = moveInputVector * _MoveSpeed;
					currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, _MoveGroundAccel * deltaTime);
					if (currentVelocity.sqrMagnitude > 0.01f)
					{
						_FSM.TrySetState(_Move);
						Vector3 localMoveDirection3 = transform.InverseTransformDirection(currentVelocity);
						Vector2 localMoveDirection2 = new(localMoveDirection3.x, localMoveDirection3.z);
						_MoveParameter.TargetValue = localMoveDirection2.normalized;
					}
					else
					{
						_FSM.TrySetState(_Idle);
					}

					// 착지
					if (!_Motor.LastGroundingStatus.IsStableOnGround)
					{
						_FSM.TrySetState(_Land);
					}
				}
				else
				{
					Vector3 addedVelocity = _MoveAirAccel * deltaTime * moveInputVector;
					Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, _Motor.CharacterUp);

					// 공중에서 가속
					if (currentVelocityOnInputsPlane.magnitude < _MoveSpeed)
					{
						Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, _MoveSpeed);
						addedVelocity = newTotal - currentVelocityOnInputsPlane;
					}
					else
					{
						if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
						{
							addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
						}
					}

					// 공중에서 오르기 방지
					if (_Motor.GroundingStatus.FoundAnyGround)
					{
						Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(_Motor.CharacterUp, _Motor.GroundingStatus.GroundNormal), _Motor.CharacterUp).normalized;
						addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
					}

					currentVelocity += addedVelocity;
					_FSM.TrySetState(_Jump);
				}
			}

			// 대쉬
			if (dashNormalizedTime < 1f)
			{
				currentVelocity = _DashSpeed * transform.TransformDirection(_DashDir);
			}

			// 중력
			if (!_Motor.GroundingStatus.IsStableOnGround)
			{
				currentVelocity += _Gravity * deltaTime * Vector3.down;
			}

			switch (_MoveRequest)
			{
				// 점프
				case MoveRequest.Jump:
					bool canJump = _Motor.GroundingStatus.FoundAnyGround || Time.time - _LastCanJumpTime <= MoveGraceTime;
					canJump &= _FSM.CurrentState._CanJump;
					if (canJump)
					{
						Vector3 jumpDirection = _Motor.CharacterUp;
						if (_Motor.GroundingStatus.FoundAnyGround && !_Motor.GroundingStatus.IsStableOnGround)
						{
							jumpDirection = _Motor.GroundingStatus.GroundNormal;
						}

						_Motor.ForceUnground();
						currentVelocity += (jumpDirection * _JumpSpeed) - Vector3.Project(currentVelocity, _Motor.CharacterUp);
						_MoveRequest = MoveRequest.None;
					}
					break;

				// 대쉬
				case MoveRequest.DashFwd:
				case MoveRequest.DashBwd:
				case MoveRequest.DashLeft:
				case MoveRequest.DashRight:
					if (_FSM.CurrentState._CanDash)
					{
						_LastDashTime = Time.time;
						_DashDir = _MoveRequest switch
						{
							MoveRequest.DashFwd => Vector3.forward,
							MoveRequest.DashBwd => Vector3.back,
							MoveRequest.DashLeft => Vector3.left,
							_ => Vector3.right
						};
						DashState state = _MoveRequest switch
						{
							MoveRequest.DashFwd => _DashFwd,
							MoveRequest.DashBwd => _DashBwd,
							MoveRequest.DashLeft => _DashLeft,
							_ => _DashRight
						};
						_FSM.TrySetState(state);
						_MoveRequest = MoveRequest.None;
					}
					break;
			}
		}

		public void AfterCharacterUpdate(float deltaTime)
		{
			if (_MoveRequest != MoveRequest.None && Time.time - _LastRequestTime > MoveGraceTime)
			{
				_MoveRequest = MoveRequest.None;
			}

			if (_Motor.GroundingStatus.FoundAnyGround)
			{
				_LastCanJumpTime = Time.time;
			}
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
