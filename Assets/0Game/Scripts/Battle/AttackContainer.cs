using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Animancer;
using System;
using System.Reflection;
using static Battle.Character;
using System.Linq;

namespace Battle
{
	public class AttackContainer : MonoBehaviour
	{
		public List<BattleAttack> _Attacks;

		Dictionary<TransitionAsset, BattleAttack> _AttackDict;

		public void Init()
		{
			_AttackDict = _Attacks.ToDictionary(a => a._Transition, b => b);
		}

		public BattleAttack GetAttack(TransitionAsset transition)
		{
			if (!transition) return null;
			_AttackDict.TryGetValue(transition, out BattleAttack data);
			return data;
		}
	}
}
