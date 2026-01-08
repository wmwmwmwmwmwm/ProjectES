using System;
using Battle;

namespace Battle
{
	public abstract class State
	{
		protected Character c;
		protected State PreviousState, NextState;

		public State(Character controller)
		{
			c = controller;
		}

		public abstract bool CanEnterState { get; }
		public abstract bool CanExitState { get; }
		public abstract void AnimationUpdate();
		public abstract void OnEnterState(float FadeTime);
		public abstract void OnExitState();
	}
}
