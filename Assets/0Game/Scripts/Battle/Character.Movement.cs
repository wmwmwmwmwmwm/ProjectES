using Animancer;
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

		[HideInInspector] public Vector3 _MoveInput;
		MoveRequest _MoveRequest;
		float _LastRequestTime;
		float _LastCanJumpTime;
		float _LastDashTime;
		Vector3 _DashDir;
		[HideInInspector] public Vector3 _RootMotionPosDelta;
		[HideInInspector] public Quaternion _RootMotionRotDelta;
		[HideInInspector] public Quaternion _AimDestRotation;
		float _FadeInDeaccelTimer, _FadeOutDeaccelTimer;
		Vector3 _Impulse;
		Vector3? _AttackJumpDirection;

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
							MoveRequest.DashFwd => _Motor.CharacterForward,
							MoveRequest.DashBwd => -_Motor.CharacterForward,
							MoveRequest.DashLeft => -_Motor.CharacterRight,
							_ => _Motor.CharacterRight
						};
						_Dash._Asset = _MoveRequest switch
						{
							MoveRequest.DashFwd => _DashFwdAsset,
							MoveRequest.DashBwd => _DashBwdAsset,
							MoveRequest.DashLeft => _DashLeftAsset,
							_ => _DashRightAsset
						};
						_FSM.TrySetState(_Dash);
						_FadeOutDeaccelTimer = 0f;
						_Impulse = _MoveSpeed * _Dash._MoveSpeed * _DashDir;

						// 달리기
						if (_MoveRequest == MoveRequest.DashFwd)
						{
							_IsRunning = true;
						}
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
			else 
			{
				Quaternion destRot = Quaternion.Euler(0f, _AimDestRotation.eulerAngles.y, 0f);
				float delta = _RotationSpeed * deltaTime;
				delta *= _FSM.CurrentState._LimitRotate ? _AttackMovePercent : 1f;
				currentRotation = Quaternion.RotateTowards(currentRotation, destRot, delta);
			}
		}

		public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			// 경직
			if (IsHitStun())
			{
				currentVelocity = Vector3.zero;
				_HitStunTimer -= deltaTime;
				if (_HitStunTimer <= 0f)
				{
					currentVelocity = _HitStunPrevVelocity;
				}
				return;
			}

			Vector3 moveInputVector = _AimDestRotation * _MoveInput;
			moveInputVector.y = 0f;
			moveInputVector.Normalize();
			float moveSpeed = _MoveSpeed * _FSM.CurrentState._MoveSpeed;
			float moveAccel = _MoveAccel * _FSM.CurrentState._MoveSpeed;

			// 페이드 인 감속
			if (_FadeInDeaccelTimer > 0f)
			{
				float t = _FadeInDeaccelTimer;
				moveAccel *= t;
				if (_Motor.GroundingStatus.IsStableOnGround)
				{
					Vector2 xz = new(currentVelocity.x, currentVelocity.z);
					xz = Vector2.ClampMagnitude(xz, moveSpeed * t);
					currentVelocity.x = xz.x;
					currentVelocity.z = xz.y;
				}
				_FadeInDeaccelTimer -= deltaTime;
			}

			// 페이드 아웃 감속
			if (_FadeOutDeaccelTimer > 0f)
			{
				float t = _FadeOutDeaccelTimer;
				moveAccel *= t;
				if (_Motor.GroundingStatus.IsStableOnGround)
				{
					Vector2 xz = new(currentVelocity.x, currentVelocity.z);
					xz = Vector2.MoveTowards(xz, Vector2.zero, xz.magnitude / _FadeOutDeaccelTimer * deltaTime);
					currentVelocity.x = xz.x;
					currentVelocity.z = xz.y;
				}
				_FadeOutDeaccelTimer -= deltaTime;
			}

			// 누운 상태 감속
			bool deaccel = _FSM.CurrentState == _GetDown || _FSM.CurrentState == _GetUp || _FSM.CurrentState == _Die;
			deaccel &= _Motor.GroundingStatus.FoundAnyGround;
			if (deaccel)
			{
				currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, 0f, 50f * deltaTime);
				currentVelocity.z = Mathf.MoveTowards(currentVelocity.z, 0f, 50f * deltaTime);
			}

			// MoveInput 관련
			InputMoveProcess(ref currentVelocity);

			// 중력
			if (!_Motor.GroundingStatus.IsStableOnGround)
			{
				currentVelocity += Physics.gravity * deltaTime;
			}

			// 점프 판정
			bool jump = false;
			bool wallJump = false;
			TransitionAsset asset = default;
			Vector3 dir = default;
			switch (_MoveRequest)
			{
				case MoveRequest.Jump:
					if (!_FSM.CurrentState._CanJump) break;

					// 지상
					bool groundJump = _Motor.GroundingStatus.IsStableOnGround;
					groundJump |= Time.time - _LastCanJumpTime <= MoveGraceDuration;
					groundJump &= Vector3.Angle(_Motor.CharacterUp, _Motor.GroundingStatus.GroundNormal) < _Motor.MaxStableSlopeAngle;
					if (groundJump)
					{
						jump = true;
						dir = _Motor.CharacterUp;
						asset = _JumpAsset;
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
						for (int i = 0; i < directions.Count; i++)
						{
							// 벽 방향키 판정
							Direction4 dir4 = i switch
							{
								0 => Direction4.Up,
								1 => Direction4.Down,
								2 => Direction4.Left,
								_ => Direction4.Right
							};
							bool pressed = dir4 switch
							{
								Direction4.Up => Inputs.Backward.IsPressed(),
								Direction4.Down => Inputs.Forward.IsPressed(),
								Direction4.Left => Inputs.Right.IsPressed(),
								_ => Inputs.Left.IsPressed()
							};
							if (!pressed) continue;

							// 벽이 있는지 Sweep
							Vector3 direction = directions[i];
							int count = _Motor.CharacterCollisionsSweep(
								position: Center,
								rotation: transform.rotation,
								direction: direction,
								distance: 1f,
								closestHit: out RaycastHit hit,
								hits: _RaycastResults);
							if (count == 0) continue;

							// 각도 판정
							Vector3 jumpDir = -direction;
							float angle = Vector3.Angle(jumpDir, hit.normal);
							if (angle < WallJumpAngleThreshold)
							{
								jump = true;
								wallJump = true;
								dir = Vector3.RotateTowards(jumpDir, _Motor.CharacterUp, 45f * Mathf.Deg2Rad, 0f);
								asset = dir4 switch
								{
									Direction4.Up => _DashFwdAsset,
									Direction4.Down => _DashBwdAsset,
									Direction4.Left => _DashLeftAsset,
									_ => _DashRightAsset
								};
								PlayEffect123123(_GuardEffectPrefab, this, Bottom, Quaternion.identity);
								break;
							}
						}
					}
					break;
			}

			// 공중 공격으로 점프
			if (_AttackJumpDirection != null)
			{
				jump = true;
				wallJump = true;
				dir = Vector3.RotateTowards(_AttackJumpDirection.Value, _Motor.CharacterUp, 45f * Mathf.Deg2Rad, 0f);
				_AttackJumpDirection = null;
			}

			// 점프 수행
			Vector3 jumpVelocity = dir * _JumpSpeed;
			if (jump)
			{
				_Motor.ForceUnground();
				if (!wallJump)
				{
					currentVelocity += jumpVelocity - Vector3.Project(currentVelocity, _Motor.CharacterUp);
				}
				else
				{
					Vector3 v = jumpVelocity * 1.5f;
					currentVelocity.x = v.x;
					currentVelocity.z = v.z;
					currentVelocity.y = Mathf.Max(v.y, currentVelocity.y);
					currentVelocity.y += _JumpSpeed * 0.5f;
				}
				if (asset)
				{
					_Jump._Asset = asset;
					_FSM.TryResetState(_Jump);
				}
				DashCancel();
				_MoveRequest = MoveRequest.None;
			}

			// 날려짐
			if (_Impulse != Vector3.zero)
			{
				if (_Impulse.y != 0f)
				{
					_Motor.ForceUnground();
				}
				currentVelocity = _Impulse;
				_Impulse = Vector3.zero;
			}

			void InputMoveProcess(ref Vector3 currentVelocity)
			{
				if (!IsMovable()) return;

				// 루트 모션 이동
				bool rootMotion = _RootMotionPosDelta != Vector3.zero;
				rootMotion &= _Motor.GroundingStatus.IsStableOnGround;
				rootMotion &= !_Motor.MustUnground();
				rootMotion &= _FSM.CurrentState._UseRootMotion;
				if (rootMotion)
				{
					currentVelocity = _RootMotionPosDelta / deltaTime;

					// 전후 이동으로 강도 조정
					currentVelocity *= _MoveInput.z + 1f;

					// 공격 시 살짝 이동
					currentVelocity += moveSpeed * _AttackMovePercent * moveInputVector;

					currentVelocity = _Motor.GetDirectionTangentToSurface(currentVelocity, _Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
					return;
				}

				// 대쉬
				if (IsDashing()) return;

				// 지상 이동
				if (_Motor.GroundingStatus.IsStableOnGround)
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
					float airAccel = moveAccel * 0.2f;
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
					_FSM.TrySetState(_Fall);
				}
			}
		}

		public void AfterCharacterUpdate(float deltaTime)
		{
			if (_MoveRequest != MoveRequest.None && Time.time - _LastRequestTime > MoveGraceDuration)
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
