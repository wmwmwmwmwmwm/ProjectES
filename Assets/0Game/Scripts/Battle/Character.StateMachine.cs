using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Animancer;
using Animancer.FSM;

namespace Battle
{
	public partial class Character
	{
		[Header("애니메이션")]
		public AnimancerComponent _Animancer;
		public AvatarMask _UpperBodyMask;

		// BaseLayer
		public TransitionAsset _IdleAsset;
		public TransitionAsset _MoveAsset;
		public TransitionAsset _RunAsset;
		public TransitionAsset _DashFwdAsset, _DashBwdAsset, _DashLeftAsset, _DashRightAsset;
		public TransitionAsset _JumpAsset, _LandAsset;
		public List<TransitionAsset> _DamageAssets;
		public TransitionAsset _GetDownAsset, _GetUpAsset;
		public TransitionAsset _DieAsset;
		public List<TransitionAsset> _NormalAttackAssets;
		public TransitionAsset _JumpAttackAsset;

		// UpperBodyLayer
		public TransitionAsset _GuardUpAsset, _GuardDownAsset;

		public StringAsset _MoveX, _MoveY;
		public StringAsset _NextAttack;

		[HideInInspector] public AnimancerLayer _BaseLayer, _UpperBodyLayer;
		[HideInInspector] public StateMachine<State>.WithDefault _FSM;
		SmoothedVector2Parameter _MoveParameter;

		State _Idle;
		State _Move;
		State _Run;
		State _DashFwd, _DashBwd, _DashLeft, _DashRight;
		State _Jump;
		State _Land;
		State _Damage;
		State _GetDown, _GetUp;
		State _Die;
		List<State> _NormalAttacks;
		State _JumpAttack;

		void InitFSM()
		{
			_BaseLayer = _Animancer.Layers[0];
			_UpperBodyLayer = _Animancer.Layers[1];
			_UpperBodyLayer.Mask = _UpperBodyMask;
			_MoveParameter = new(_Animancer, _MoveX, _MoveY, 0.15f);

			// BaseLayer
			_Idle = new()
			{
				c = this,
				_Asset = _IdleAsset,
				_Priority = -1,
				_CanMove = true,
				_CanDash = true,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Move = new()
			{
				c = this,
				_Asset = _MoveAsset,
				_Priority = -1,
				_CanMove = true,
				_CanDash = true,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Run = new()
			{
				c = this,
				_Asset = _RunAsset,
				_Priority = -1,
				_CanMove = true,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_DashFwd = new()
			{
				c = this,
				_Asset = _DashFwdAsset,
				_Restart = true,
				_Duration = _DashDuration,
				_OnEnd = () => _IsRunning = true,
			};
			_DashBwd = new()
			{
				c = this,
				_Asset = _DashBwdAsset,
				_Restart = true,
				_Duration = _DashDuration,
			};
			_DashLeft = new()
			{
				c = this,
				_Asset = _DashLeftAsset,
				_Restart = true,
				_Duration = _DashDuration,
			};
			_DashRight = new()
			{
				c = this,
				_Asset = _DashRightAsset,
				_Restart = true,
				_Duration = _DashDuration,
			};
			_Jump = new()
			{
				c = this,
				_Asset = _JumpAsset,
				_Priority = -1,
				_CanMove = true,
				_CanDash = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Land = new()
			{
				c = this,
				_Asset = _LandAsset,
				_Priority = -1,
				_CanMove = true,
				_CanDash = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Damage = new()
			{
				c = this,
				_RandomAssets = _DamageAssets,
				_Priority = 1,
			};
			_GetDown = new()
			{
				c = this,
				_Asset = _GetDownAsset,
				_Priority = 2,
				_Duration = float.MaxValue,
			};
			_GetUp = new()
			{
				c = this,
				_Asset = _GetUpAsset,
				_Priority = 2,
			};
			_Die = new()
			{
				c = this,
				_Asset = _DieAsset,
				_Priority = 3,
				_Duration = float.MaxValue,
			};
			_NormalAttacks = new();
			for (int i = 0; i < _NormalAttackAssets.Count; i++)
			{
				TransitionAsset asset = _NormalAttackAssets[i];
				bool isLast = i == _NormalAttackAssets.Count - 1;
				_NormalAttacks.Add(new()
				{
					c = this,
					_Asset = asset,
					_Restart = true,
					_CanGuard = true,
					_CanAttack = !isLast,
				});
			}
			_JumpAttack = new()
			{
				c = this,
				_Asset = _JumpAttackAsset,
				_Restart = true,
				_CanGuard = true,
			};

			// 이벤트
			_Animancer.Events.TryAdd(_NextAttack, () => _NextAttackAvailable = true);

			_FSM.DefaultState = _Idle;
		}

		void UpdateFSM()
		{
            State state = _FSM.CurrentState;
			state.UpdateState();

			// 다음 공격
			if (_NextAttackAvailable && _NextAttackInput)
			{
				_NextAttackInput = false;
				_AttackIndex++;
				_FSM.TrySetState(_NormalAttacks[_AttackIndex]);
				GiveDamage();
            }

			// 공격 시 살짝 이동 가능
			if (state.IsAttack)
			{
				_AttackMovePercent = (_BaseLayer.CurrentState.NormalizedTime - 0.5f) * 2f;
				_AttackMovePercent = Mathf.Max(0f, _AttackMovePercent);
			}
			else
			{
				_AttackMovePercent = 0f;
			}

			// 가드 취소
			if (!state._CanGuard)
			{
				GuardCancel();
			}

			// 달리기 멈추기
			if (_IsRunning)
            {
                float degree = Util.DirectionToRotationZ(new(_MoveInput.x, _MoveInput.z));

				bool active = degree > 30f && degree < 150f;
				active &= state._CanMove || state == _DashFwd;
				active &= !IsGuarding();
				_IsRunning = active;
			}

			// 일어나기
			if (state == _GetDown)
			{
				bool getUp = _BaseLayer.CurrentState.NormalizedTime >= 1f;
				getUp &= _Motor.GroundingStatus.IsStableOnGround;
				if (getUp)
				{
					_FSM.TrySetState(_GetUp);
				}
			}
        }

		void Play_Canceling(State state)
		{
			_BaseLayer.Play(state._Asset, 0f);
			_FSM.ForceSetState(state);
		}
	}
}
