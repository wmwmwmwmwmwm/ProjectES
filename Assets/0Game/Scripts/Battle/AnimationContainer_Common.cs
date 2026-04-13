using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Animancer;

namespace Battle
{
	public class AnimationContainer_Common : MonoBehaviour
	{
		// BaseLayer
		public TransitionAsset _IdleAsset;
		public TransitionAsset _MoveAsset;
		public TransitionAsset _JumpAsset;
		public TransitionAsset _LandAsset;
		public List<TransitionAsset> _DamageAssets;
		public TransitionAsset _GetDownAsset, _GetUpAsset;
		public TransitionAsset _DieAsset;
		public TransitionAsset _Skill1Asset;
		public TransitionAsset _Skill2Asset;
		public TransitionAsset _UltimateAsset;
		//public List<Effect> _Effects;
	}
}
