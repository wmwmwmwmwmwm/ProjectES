using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using static SingletonManager;
using KinematicCharacterController;
using Animancer;

namespace Battle
{
	public class CharacterController_KCC : MonoBehaviour, ICharacterController
	{
		public enum MoveRequest { None, Jump, DashFwd, DashBwd, DashLeft, DashRight }

		Character c;
		[HideInInspector] public KinematicCharacterMotor _Motor;
		MoveRequest _MoveRequest;
		float _LastRequestTime;
		float _LastCanJumpTime;
		float _LastDashTime;
		Vector3 _DashDir;
		[HideInInspector] public Vector3? _AttackJumpDirection;
		ParticleSystem _DashWindEffect;
		Coroutine _DashWindCoroutine;

		const float MoveGraceDuration = 0.1f;

		public BattleController Controller => BattleController.Instance;

		public void Init()
		{
			c = GetComponent<Character>();
			_Motor = GetComponent<KinematicCharacterMotor>();
			_DashWindEffect = transform.Find("DashWind").GetComponent<ParticleSystem>();

			_LastRequestTime = Const.TimeDefault;
			_LastCanJumpTime = Const.TimeDefault;
			_LastDashTime = Const.TimeDefault;

			_Motor.Init();
			_Motor.CharacterController = this;
			c.EmitEffect(_DashWindEffect, false);
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
					if (c._FSM.CurrentState._CanDash && !c.IsGuarding())
					{
						_LastDashTime = Time.time;
						_DashDir = _MoveRequest switch
						{
							MoveRequest.DashFwd => _Motor.CharacterForward,
							MoveRequest.DashBwd => -_Motor.CharacterForward,
							MoveRequest.DashLeft => -_Motor.CharacterRight,
							_ => _Motor.CharacterRight
						};
						TransitionAsset asset = _MoveRequest switch
						{
							MoveRequest.DashFwd => c._Anims_Player._DashFwdAsset,
							MoveRequest.DashBwd => c._Anims_Player._DashBwdAsset,
							MoveRequest.DashLeft => c._Anims_Player._DashLeftAsset,
							_ => c._Anims_Player._DashRightAsset
						};
						c._Dash.SetAsset(asset);
						c._FSM.TrySetState(c._Dash);
						c._FadeOutDeaccelTimer = 0f;
						c._Impulse = c._MoveSpeed * c._Dash._MoveSpeed * _DashDir;
						DashWind(_MoveRequest);

						// 달리기
						if (_MoveRequest == MoveRequest.DashFwd)
						{
							c._IsRunning = true;
						}
						_MoveRequest = MoveRequest.None;
					}
					break;
			}
		}

		public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
		{
			c.UpdateRotation_Shared(ref currentRotation, deltaTime);
		}

		public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			c.UpdateVelocity_Shared1(ref currentVelocity, deltaTime);

			c.InputMoveProcess(ref currentVelocity, deltaTime);

			c.UpdateVelocity_Shared2(ref currentVelocity, deltaTime);

			JumpProcess(ref currentVelocity);

			c.UpdateVelocity_Shared3(ref currentVelocity, deltaTime);
		}

		void JumpProcess(ref Vector3 currentVelocity)
		{
			// 점프 판정
			bool jump = false;
			bool wallJump = false;
			TransitionAsset asset = default;
			Vector3 dir = default;
			switch (_MoveRequest)
			{
				case MoveRequest.Jump:
					if (!c._FSM.CurrentState._CanJump) break;

					// 지상
					bool groundJump = _Motor.GroundingStatus.IsStableOnGround;
					groundJump |= Time.time - _LastCanJumpTime <= MoveGraceDuration;
					groundJump &= Vector3.Angle(_Motor.CharacterUp, _Motor.GroundingStatus.GroundNormal) < _Motor.MaxStableSlopeAngle;
					if (groundJump)
					{
						jump = true;
						dir = _Motor.CharacterUp;
						asset = c._Anims_Common._JumpAsset;
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
								position: c.Center,
								rotation: transform.rotation,
								direction: direction,
								distance: 1f,
								closestHit: out RaycastHit hit,
								hits: c._RaycastResults);
							if (count == 0) continue;

							// 각도 판정
							Vector3 jumpDir = -direction;
							float angle = Vector3.Angle(jumpDir, hit.normal);
							if (angle < Character.WallJumpAngleThreshold)
							{
								jump = true;
								wallJump = true;
								dir = Vector3.RotateTowards(jumpDir, _Motor.CharacterUp, 45f * Mathf.Deg2Rad, 0f);
								asset = dir4 switch
								{
									Direction4.Up => c._Anims_Player._DashFwdAsset,
									Direction4.Down => c._Anims_Player._DashBwdAsset,
									Direction4.Left => c._Anims_Player._DashLeftAsset,
									_ => c._Anims_Player._DashRightAsset
								};
								Controller.PlayEffect123123(c._GuardEffectPrefab, c, c.Bottom, Quaternion.identity);
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
			Vector3 jumpVelocity = dir * c._JumpSpeed;
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
					currentVelocity.y += c._JumpSpeed * 0.5f;
				}
				if (asset)
				{
					c._Jump.SetAsset(asset);
					c.PlayAction(c._Jump);
				}
				DashCancel();
				_MoveRequest = MoveRequest.None;
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

			c._RootMotionPosDelta = Vector3.zero;
			c._RootMotionRotDelta = Quaternion.identity;
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

		public void Dash(Direction4 dir)
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

		public void Jump()
		{
			_MoveRequest = MoveRequest.Jump;
			_LastRequestTime = Time.time;
		}

		void DashWind(MoveRequest dashDir)
		{
			if (_DashWindCoroutine != null)
			{
				StopCoroutine(_DashWindCoroutine);
				_DashWindCoroutine = null;
			}
			_DashWindCoroutine = StartCoroutine(Internal());

			IEnumerator Internal()
			{
				_DashWindEffect.transform.localEulerAngles = dashDir switch
				{
					MoveRequest.DashFwd => new Vector3(0f, 0f, 0f),
					MoveRequest.DashBwd => new Vector3(0f, 180f, 0f),
					MoveRequest.DashLeft => new Vector3(0f, 270f, 0f),
					_ => new Vector3(0f, 90f, 0f),
				};
				c.EmitEffect(_DashWindEffect, true);
				yield return new WaitUntil(() => !IsDashing() && !c._IsRunning);
				c.EmitEffect(_DashWindEffect, false);
				_DashWindCoroutine = null;
			}
		}

		void DashCancel()
		{
			_LastDashTime = Const.TimeDefault;
		}

		public bool IsDashing()
		{
			return Time.time - _LastDashTime < c._DashDuration;
		}

		public void DashAttack()
		{
			c._FadeOutDeaccelTimer = c._DashDuration;
			DashCancel();
			c.PlayAction(c._DashAttack);
			c.Attack();
		}
	}
}
