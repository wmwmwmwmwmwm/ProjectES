using Animancer;
using KinematicCharacterController;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public partial class Character
	{
		CharacterController_KCC _KCC;
		CharacterController_Rigidbody _KCC_Rigidbody;

		[HideInInspector] public float _FadeInDeaccelTimer, _FadeOutDeaccelTimer;
		[HideInInspector] public Vector3 _RootMotionPosDelta;
		[HideInInspector] public Quaternion _RootMotionRotDelta;
		[HideInInspector] public Vector3 _Impulse;
		[HideInInspector] public float _CurrentMoveSpeed;
		[HideInInspector] public float _CurrentMoveAccel;
		[HideInInspector] public Vector3 _CurrentMoveInputVector;

		public bool UseKCC => _KCC;

		public KinematicCharacterMotor Motor => _KCC._Motor;

		public void UpdateRotation_Shared(ref Quaternion currentRotation, float deltaTime)
		{
			if (!IsMovable()) return;

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

		public void UpdateVelocity_Shared1(ref Vector3 currentVelocity, float deltaTime)
		{
			_CurrentMoveSpeed = _MoveSpeed * _FSM.CurrentState._MoveSpeed;
			_CurrentMoveAccel = _MoveAccel * _FSM.CurrentState._MoveSpeed;
			_CurrentMoveInputVector = _AimDestRotation * _MoveInput;
			_CurrentMoveInputVector.y = 0f;
			_CurrentMoveInputVector.Normalize();

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

			// 착지 시 속도 별도 처리
			State state = _FSM.CurrentState;
			if (state == _Land)
			{
				_CurrentMoveSpeed = _IsRunning ? _MoveSpeed * _Run._MoveSpeed : _MoveSpeed * _Move._MoveSpeed;
			}

			// 페이드 인 감속
			if (_FadeInDeaccelTimer > 0f)
			{
				float t = _FadeInDeaccelTimer;
				_CurrentMoveAccel *= t;
				if (IsGrounded())
				{
					Vector2 xz = new(currentVelocity.x, currentVelocity.z);
					xz = Vector2.ClampMagnitude(xz, _CurrentMoveSpeed * t);
					currentVelocity.x = xz.x;
					currentVelocity.z = xz.y;
				}
				_FadeInDeaccelTimer -= deltaTime;
			}

			// 페이드 아웃 감속
			if (_FadeOutDeaccelTimer > 0f)
			{
				float t = _FadeOutDeaccelTimer;
				_CurrentMoveAccel *= t;
				if (IsGrounded())
				{
					Vector2 xz = new(currentVelocity.x, currentVelocity.z);
					xz = Vector2.MoveTowards(xz, Vector2.zero, xz.magnitude / _FadeOutDeaccelTimer * deltaTime);
					currentVelocity.x = xz.x;
					currentVelocity.z = xz.y;
				}
				_FadeOutDeaccelTimer -= deltaTime;
			}

			// 누운 상태 감속
			bool deaccel = state == _GetDown || state == _GetUp || state == _Die;
			deaccel &= IsGrounded();
			if (deaccel)
			{
				currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, 0f, 50f * deltaTime);
				currentVelocity.z = Mathf.MoveTowards(currentVelocity.z, 0f, 50f * deltaTime);
			}
		}

		public void InputMoveProcess(ref Vector3 currentVelocity, float deltaTime)
		{
			if (!IsMovable()) return;

			// 대쉬
			if (UseKCC && _KCC.IsDashing()) return;

			// 루트 모션 이동
			bool rootMotion = _RootMotionPosDelta != Vector3.zero;
			switch (_FSM.CurrentState._RootMotionMode)
			{
				case State.RootMotionMode.None:
					rootMotion = false;
					break;
				case State.RootMotionMode.GroundOnly:
					rootMotion &= IsGrounded();
					break;
			}
			if (rootMotion)
			{
				Vector2 velocityXZ = new()
				{
					x = _RootMotionPosDelta.x / deltaTime,
					y = _RootMotionPosDelta.z / deltaTime
				};

				// 전후 이동으로 강도 조정
				if (_FSM.CurrentState.IsAttack && _FSM.CurrentState._Attack._SkillType <= AttackSkillType.Special)
				{
					velocityXZ *= _MoveInput.z + 1f;
				}

				// 공격 시 살짝 이동
				velocityXZ.x += _CurrentMoveSpeed * _AttackMovePercent * _CurrentMoveInputVector.x;
				velocityXZ.y += _CurrentMoveSpeed * _AttackMovePercent * _CurrentMoveInputVector.z;

				currentVelocity.x = velocityXZ.x;
				currentVelocity.z = velocityXZ.y;
				if (UseKCC)
				{
					currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
				}
				return;
			}

			// 착지
			if (UseKCC && !Motor.LastGroundingStatus.IsStableOnGround)
			{
				_FSM.TrySetState(_Land);
			}

			// 지상 이동
			if (IsGrounded())
			{
				Vector3 targetVelocity = _CurrentMoveInputVector * _CurrentMoveSpeed;
				currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, _CurrentMoveAccel * deltaTime);

				// 애니메이션
				if (currentVelocity.sqrMagnitude > 1f)
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
			}
			// 공중 이동
			else
			{
				float airAccel = _CurrentMoveAccel * 0.2f;
				Vector3 addedVelocity = airAccel * deltaTime * _CurrentMoveInputVector;
				addedVelocity = Vector3.ProjectOnPlane(addedVelocity, Vector3.up);
				Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);

				// 공중에서 가속
				if (currentVelocityOnInputsPlane.magnitude < _CurrentMoveSpeed)
				{
					Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, _CurrentMoveSpeed);
					addedVelocity = newTotal - currentVelocityOnInputsPlane;
				}
				else
				{
					if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
					{
						addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
					}
				}

				if (UseKCC && Motor.GroundingStatus.FoundAnyGround)
				{
					// 공중에서 오르기 방지
					Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Vector3.up, Motor.GroundingStatus.GroundNormal), Vector3.up).normalized;
					addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
				}

				currentVelocity += addedVelocity;
				_FSM.TrySetState(_Fall);
			}
		}

		public void UpdateVelocity_Shared2(ref Vector3 currentVelocity, float deltaTime)
		{
			// 중력
			if (!IsGrounded())
			{
				currentVelocity += Physics.gravity * deltaTime;
			}

			// Y축 정지 트리거
			if (_StopYTrigger)
			{
				currentVelocity.y = 0f;
				_StopYTrigger = false;
			}
		}

		public void UpdateVelocity_Shared3(ref Vector3 currentVelocity, float deltaTime)
		{
			// 날려짐
			if (_Impulse != Vector3.zero)
			{
				if (UseKCC && _Impulse.y != 0f)
				{
					Motor.ForceUnground();
				}
				currentVelocity = _Impulse;
				_Impulse = Vector3.zero;
			}
		}
	}
}
