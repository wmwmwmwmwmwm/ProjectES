using Animancer;
using Animancer.FSM;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battle
{
	public class State : IState
	{
		public Character c;
		public TransitionAsset _Asset;
		public int _Priority;
		public bool _Repeat;
		public float _Duration;
		public bool _CanMove, _CanDash, _CanJump, _CanAttack, _CanGuard;

		public Action _OnEnd;

		public bool CanEnterState => true;

		public bool CanExitState => c._FSM.NextState._Priority >= _Priority;

		public void OnEnterState()
		{
			AnimancerState state;
			state = c._BaseLayer.Play(_Asset);

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

			if (!_Repeat)
			{
				state.Time = 0f;
			}

			List<AnimationClip> clips = new(1);
			_Asset.GetAnimationClips(clips);
			c.PlayEffect(clips.First());
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
