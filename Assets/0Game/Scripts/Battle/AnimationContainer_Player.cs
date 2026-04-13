using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Animancer;

namespace Battle
{
	public class AnimationContainer_Player : MonoBehaviour
	{
		// BaseLayer
		public TransitionAsset _RunAsset;
		public TransitionAsset _DashFwdAsset, _DashBwdAsset, _DashLeftAsset, _DashRightAsset;
		public List<TransitionAsset> _NormalAttackAssets;
		public TransitionAsset _JumpAttackAsset;
		public TransitionAsset _DashAttackAsset;
		public TransitionAsset _SpecialAttackAsset;
		public TransitionAsset _JumpSpecialAttackAsset;
		public TransitionAsset _GuardAttackAsset;

		// UpperBodyLayer
		public TransitionAsset _GuardUpAsset, _GuardDownAsset;
	}
}
