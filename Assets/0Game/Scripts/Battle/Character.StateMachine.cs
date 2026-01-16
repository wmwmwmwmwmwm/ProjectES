using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using static SingletonManager;
using Animancer;
using Animancer.FSM;

namespace Battle
{
	public partial class Character
	{
		[Header("애니메이션")]
		public AnimancerComponent _Animancer;
		public StringAsset _MoveX, _MoveY;
		public TransitionAsset _IdleAsset;
		public TransitionAsset _MoveAsset;
		public TransitionAsset _DashFwdAsset, _DashBwdAsset, _DashLeftAsset, _DashRightAsset;
		public TransitionAsset _JumpAsset, _LandAsset;

		[HideInInspector] public StateMachine<State>.WithDefault _FSM;
		SmoothedVector2Parameter _MoveParameter;
		IdleState _Idle;
		MoveState _Move;
		DashState _DashFwd, _DashBwd, _DashLeft, _DashRight;
		JumpState _Jump;
		LandState _Land;

		void InitFSM()
		{
			_MoveParameter = new(_Animancer, _MoveX, _MoveY, 0.15f);
			_Idle = new()
			{
				c = this,
				_Asset = _IdleAsset,
				_Priority = -1,
				_CanMove = true,
				_CanDash = true,
				_CanJump = true,
			};
			_Move = new()
			{
				c = this,
				_Asset = _MoveAsset,
				_Priority = -1,
				_CanMove = true,
				_CanDash = true,
				_CanJump = true,
			};
			_DashFwd = new()
			{
				c = this,
				_Asset = _DashFwdAsset,
				_CanMove = true,
				_Duration = _DashTime,
			};
			_DashBwd = new()
			{
				c = this,
				_Asset = _DashBwdAsset,
				_CanMove = true,
				_Duration = _DashTime,
			};
			_DashLeft = new()
			{
				c = this,
				_Asset = _DashLeftAsset,
				_CanMove = true,
				_Duration = _DashTime,
			};
			_DashRight = new()
			{
				c = this,
				_Asset = _DashRightAsset,
				_CanMove = true,
				_Duration = _DashTime,
			};
			_Jump = new()
			{
				c = this,
				_Asset = _JumpAsset,
				_CanMove = true,
				_CanDash = true,
			};
			_Land = new()
			{
				c = this,
				_Asset = _LandAsset,
				_CanMove = true,
				_CanDash = true,
				_CanJump = true,
			};

			_FSM.DefaultState = _Idle;
		}

		void UpdateFSM()
		{
			_FSM.CurrentState.UpdateState();
		}
	}

    public abstract class State : IState
    {
		public Character c;
		public TransitionAsset _Asset;
		public int _Priority;
		public float _Duration;
		public bool _CanMove, _CanDash, _CanJump;

		public virtual bool CanEnterState => true;

        public virtual bool CanExitState => c._FSM.NextState._Priority >= _Priority;

        public virtual void OnEnterState()
		{
            AnimancerState state = c._Animancer.Play(_Asset);
			if (_Duration == 0f)
			{
				state.Events(this).OnEnd ??= c._FSM.ForceSetDefaultState;
			}
		}

		public virtual void OnExitState() { }

		public virtual void UpdateState()
		{
			if (_Duration > 0f && c._Animancer.States.Current.Time > _Duration)
			{
				c._FSM.ForceSetDefaultState.Invoke();
			}
		}
    }

	public class IdleState : State
	{

	}

	public class MoveState : State
	{

	}

	public class DashState : State
	{

	}

	public class JumpState : State
	{

	}

	public class LandState : State
	{

	}
}
