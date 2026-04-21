using Animancer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class BattleAttack : MonoBehaviour
	{
		public enum SkillType { Normal, Special, Skill };
		public enum RangeType { Melee, Range };
		public enum AreaType { Single, Area };

		public string _Name;

		public float _Cooltime;
		public float _SpeedMultiplier;
		public SkillType _SkillType;
		public RangeType _RangeType;
		public AreaType _AreaType;

		[HideInInspector] public Character _Owner;
		[HideInInspector] public State _StateInfo;
	}
}
