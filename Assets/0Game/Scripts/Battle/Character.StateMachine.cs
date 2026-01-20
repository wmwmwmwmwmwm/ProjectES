using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using static SingletonManager;
using Animancer;
using Animancer.FSM;
using System;

namespace Battle
{
	public partial class Character
	{
		[Header("애니메이션")]
		public AnimancerComponent _Animancer;
		public TransitionAsset _IdleAsset;
		public TransitionAsset _MoveAsset;
		public TransitionAsset _DashFwdAsset, _DashBwdAsset, _DashLeftAsset, _DashRightAsset;
		public TransitionAsset _JumpAsset, _LandAsset;
		public List<TransitionAsset> _NormalAttackAssets;
		public StringAsset _MoveX, _MoveY;
		public StringAsset _NextAttack;

		[HideInInspector] public StateMachine<State>.WithDefault _FSM;
		SmoothedVector2Parameter _MoveParameter;
		State _Idle;
		State _Move;
		State _DashFwd, _DashBwd, _DashLeft, _DashRight;
		State _Jump;
		State _Land;
		List<State> _NormalAttacks;

		int _AttackIndex;
		bool _NextAttackInput;

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
				_CanAttack = true,
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
			};
			_DashFwd = new()
			{
				c = this,
				_Asset = _DashFwdAsset,
				_Duration = _DashTime,
				_Restart = true,
			};
			_DashBwd = new()
			{
				c = this,
				_Asset = _DashBwdAsset,
				_Duration = _DashTime,
				_Restart = true,
			};
			_DashLeft = new()
			{
				c = this,
				_Asset = _DashLeftAsset,
				_Duration = _DashTime,
				_Restart = true,
			};
			_DashRight = new()
			{
				c = this,
				_Asset = _DashRightAsset,
				_Duration = _DashTime,
				_Restart = true,
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
					_CanAttack = !isLast,
					_Restart = true,
					_OnEnd = () => _AttackIndex = -1,
				});
			}

			// 이벤트
			_Animancer.Events.TryAdd(_NextAttack, NextAttack);

			_FSM.DefaultState = _Idle;
		}

		void UpdateFSM()
		{
			_FSM.CurrentState.UpdateState();
		}
	}

	public class State : IState
	{
		public Character c;
		public TransitionAsset _Asset;
		public int _Priority;
		public float _Duration;
		public bool _CanMove, _CanDash, _CanJump, _CanAttack;
		public bool _Restart;

		public Action _OnEnd;

		public bool CanEnterState => true;

		public bool CanExitState => c._FSM.NextState._Priority >= _Priority;

		public void OnEnterState()
		{
			AnimancerState state = c._Animancer.Play(_Asset);
			if (_Duration == 0f)
			{
				state.Events(c).OnEnd ??= () =>
				{
					c._FSM.ForceSetDefaultState();
					_OnEnd?.Invoke();
				};
			}
			else
			{
				state.Events(c).OnEnd ??= () =>
				{
					_OnEnd?.Invoke();
				};
			}

			if (_Restart)
			{
				state.Time = 0f;
			}
		}

		public void OnExitState() { }

		public void UpdateState()
		{
			if (_Duration > 0f && c._Animancer.States.Current.Time > _Duration)
			{
				c._FSM.ForceSetDefaultState.Invoke();
			}
		}
	}
}
