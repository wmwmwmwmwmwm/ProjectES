using System.Collections.Generic;
using UnityEngine;
using Animancer;
using System.Linq;
using System;

namespace Battle
{
	public class AttackContainer : MonoBehaviour
	{
		[Serializable]
		public class Pair
		{
			public TransitionAsset _Key;
			public BattleAttack _Attack;
		}
		public List<Pair> _Attacks;

		Dictionary<TransitionAsset, BattleAttack> _AttackDict;

		public void Init()
		{
			_AttackDict = _Attacks.ToDictionary(a => a._Key, b => b._Attack);
		}

		public BattleAttack GetAttack(TransitionAsset transition)
		{
			if (!transition) return null;
			_AttackDict.TryGetValue(transition, out BattleAttack data);
			return data;
		}
	}
}
