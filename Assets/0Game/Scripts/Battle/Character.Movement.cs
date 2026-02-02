using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public partial class Character
	{
		[Header("움직임")]
		public KinematicCharacterMotor _Motor;
		public float _RotationSpeed;
		public float _MoveSpeed;
		public float _MoveAccel;
		public float _DashDuration;
		public float _JumpSpeed;

		public enum MoveRequest { None, Jump, DashFwd, DashBwd, DashLeft, DashRight }

		Vector3 _MoveInput;
		MoveRequest _MoveRequest;
		float _LastRequestTime;
		float _LastCanJumpTime;
		float _LastDashTime;
		Vector3 _DashDir;
		[HideInInspector] public Vector3 _RootMotionPosDelta;
		[HideInInspector] public Quaternion _RootMotionRotDelta;
		[HideInInspector] public Quaternion _AimDestRotation;
		float _DeaccelTime;
		Vector3 _Impulse;

		const float MoveGraceTime = 0.1f;

		void InitMovement()
		{
			_Motor.CharacterController = this;
		}

		public void BeforeCharacterUpdate(float deltaTime)
		{
			switch (_MoveRequest)
			{
				// 대쉬
				case MoveRequest.DashFwd:
				case MoveRequest.DashBwd:
				case MoveRequest.DashLeft:
				case MoveRequest.DashRight:
					if (_FSM.CurrentState._CanDash && !IsGuarding())
					{
						_LastDashTime = Time.time;
						_DashDir = _MoveRequest switch
						{
							MoveRequest.DashFwd => Vector3.forward,
							MoveRequest.DashBwd => Vector3.back,
							MoveRequest.DashLeft => Vector3.left,
							_ => Vector3.right
						};
						State state = _MoveRequest switch
						{
							MoveRequest.DashFwd => _DashFwd,
							MoveRequest.DashBwd => _DashBwd,
							MoveRequest.DashLeft => _DashLeft,
							_ => _DashRight
						};
						_Impulse = _MoveSpeed * state._MoveSpeed * transform.TransformDirection(_DashDir);
						_FSM.TrySetState(state);
						_MoveRequest = MoveRequest.None;
					}
					break;
			}
		}

		public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
		{
			// Y회전
			if (_RootMotionRotDelta != Quaternion.identity)
			{
				currentRotation *= _RootMotionRotDelta;
			}
			else if (!_FSM.CurrentState._LimitRotate)
			{
				Quaternion destRot = Quaternion.Euler(0f, _AimDestRotation.eulerAngles.y, 0f);
				currentRotation = Quaternion.RotateTowards(currentRotation, destRot, _RotationSpeed * deltaTime);
			}
		}

		public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			Vector3 moveInputVector = _AimDestRotation * _MoveInput;
			float moveSpeed = _MoveSpeed * _FSM.CurrentState._MoveSpeed;
			float moveAccel = _MoveAccel * _FSM.CurrentState._MoveSpeed;

			// 가드 시 감속
			float deaccelTime = Time.time - _DeaccelTime;
			float duration = 0.6f;
			if (deaccelTime < duration)
			{
				float t = Mathf.InverseLerp(0f, duration, deaccelTime);
				moveAccel *= t;
				if (_Motor.GroundingStatus.IsStableOnGround)
				{
					currentVelocity.x *= t;
					currentVelocity.z *= t;
				}
			}

			// 누운 상태 감속
			bool deaccel = _FSM.CurrentState == _GetDown || _FSM.CurrentState == _GetUp || _FSM.CurrentState == _Die;
			deaccel &= _Motor.GroundingStatus.FoundAnyGround;
			if (deaccel)
			{
				currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, 0f, 10f * deltaTime);
				currentVelocity.z = Mathf.MoveTowards(currentVelocity.z, 0f, 10f * deltaTime);
			}

			// 루트 모션 이동
			if (_RootMotionPosDelta != Vector3.zero && !_Motor.MustUnground())
			{
				currentVelocity = _RootMotionPosDelta / deltaTime;

				// 전후 이동으로 강도 조정
				currentVelocity *= _MoveInput.z + 1f;

				// 공격 시 살짝 이동
				currentVelocity += moveSpeed * _AttackMovePercent * moveInputVector;

				currentVelocity = _Motor.GetDirectionTangentToSurface(currentVelocity, _Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
			}
			// 대쉬
			else if (IsDashing()) { }
			// 지상 이동
			else if (_Motor.GroundingStatus.IsStableOnGround)
			{
				Vector3 targetVelocity = moveInputVector * moveSpeed;
				currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, moveAccel * deltaTime);

				if (currentVelocity.sqrMagnitude > 0.01f)
				{
					State moveState = _IsRunning ? _Run : _Move;
					_FSM.TrySetState(moveState);
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
			// 공중 이동
			else
			{
				float airAccel = moveAccel * 0.33f;
				moveInputVector.y = 0f;
				moveInputVector.Normalize();
				Vector3 addedVelocity = airAccel * deltaTime * moveInputVector;
				addedVelocity = Vector3.ProjectOnPlane(addedVelocity, _Motor.CharacterUp);
				Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, _Motor.CharacterUp);

				// 공중에서 가속
				if (currentVelocityOnInputsPlane.magnitude < moveSpeed)
				{
					Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, moveSpeed);
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

			// 중력
			if (!_Motor.GroundingStatus.IsStableOnGround)
			{
				currentVelocity += Physics.gravity * deltaTime;
			}

			switch (_MoveRequest)
			{
				// 점프
				case MoveRequest.Jump:
					if (!_FSM.CurrentState._CanJump) break;

					// 지상
					bool jump = false;
					Vector3 jumpDirection = default;
					if (_Motor.GroundingStatus.FoundAnyGround || Time.time - _LastCanJumpTime <= MoveGraceTime)
					{
						jump = true;
						jumpDirection = _Motor.CharacterUp;
						if (_Motor.GroundingStatus.FoundAnyGround && !_Motor.GroundingStatus.IsStableOnGround)
						{
							jumpDirection = _Motor.GroundingStatus.GroundNormal;
						}
					}
					// 벽
					else
					{
						List<Vector3> directions = new()
						{
							-_Motor.CharacterForward,
							_Motor.CharacterForward,
							_Motor.CharacterRight,
							-_Motor.CharacterRight,
						};
						foreach (Vector3 direction in directions)
						{
							int count = _Motor.CharacterCollisionsSweep(
								position: Center,
								rotation: transform.rotation,
								direction: direction,
								distance: 0.5f,
								closestHit: out RaycastHit hit,
								hits: _RaycastResults);
							if (count == 0) continue;

                            Vector3 jumpDir = -direction;
							float angle = Vector3.Angle(jumpDir, hit.normal);
							if (angle < 40f)
                            {
								jump = true;
								jumpDirection = jumpDir;
								break;
                            }
                        }
					}
					if (jump)
					{
						currentVelocity += (jumpDirection * _JumpSpeed) - Vector3.Project(currentVelocity, _Motor.CharacterUp);
						_Motor.ForceUnground();
						_MoveRequest = MoveRequest.None;
					}
					break;
			}

			// 날려짐
			if (_Impulse != Vector3.zero)
			{
				_Motor.ForceUnground();
				currentVelocity = _Impulse;
				_Impulse = Vector3.zero;
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

			_RootMotionPosDelta = Vector3.zero;
			_RootMotionRotDelta = Quaternion.identity;
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
