using Animancer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class BattleAttack : MonoBehaviour
	{
		public AnimationClip _Clip;
		public TransitionAsset _Transition;

		public float _Cooltime;
		public AttackSkillType _SkillType;
		public AttackRangeType _RangeType;
		public AttackAreaType _AreaType;

		[HideInInspector] public Character _Owner;
		[HideInInspector] public State _StateInfo;
	}
}
