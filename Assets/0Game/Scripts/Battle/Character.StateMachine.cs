using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using static SingletonManager;
using Animancer;
using Animancer.FSM;
using System;
using System.Linq;

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
		public TransitionAsset _DashFwdAsset, _DashBwdAsset, _DashLeftAsset, _DashRightAsset;
		public TransitionAsset _JumpAsset, _LandAsset;
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
		State _DashFwd, _DashBwd, _DashLeft, _DashRight;
		State _Jump;
		State _Land;
		List<State> _NormalAttacks;
		State _JumpAttack;

		int _AttackIndex;
		bool _NextAttackInput;

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
				_Repeat = true,
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
				_Repeat = true,
			};
			_DashFwd = new()
			{
				c = this,
				_Asset = _DashFwdAsset,
				_Duration = _DashTime,
			};
			_DashBwd = new()
			{
				c = this,
				_Asset = _DashBwdAsset,
				_Duration = _DashTime,
			};
			_DashLeft = new()
			{
				c = this,
				_Asset = _DashLeftAsset,
				_Duration = _DashTime,
			};
			_DashRight = new()
			{
				c = this,
				_Asset = _DashRightAsset,
				_Duration = _DashTime,
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
				_Repeat = true,
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
				_Repeat = true,
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
					_CanGuard = true,
					_OnEnd = () => _AttackIndex = -1,
				});
			}
			_JumpAttack = new()
			{
				c = this,
				_Asset = _JumpAttackAsset,
				_CanGuard = true,
			};

			// 이벤트
			_Animancer.Events.TryAdd(_NextAttack, NextAttack);

			_FSM.DefaultState = _Idle;
		}

		void UpdateFSM()
		{
			_FSM.CurrentState.UpdateState();
		}

		void Play_Canceling(State state)
		{
			_FSM.ForceSetState(state);
			_BaseLayer.Play(state._Asset, 0.03f);
		}
	}
}
