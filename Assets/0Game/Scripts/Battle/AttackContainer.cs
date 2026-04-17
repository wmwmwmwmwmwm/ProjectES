using System.Collections.Generic;
using UnityEngine;
using Animancer;
using System.Linq;
using System;
using NaughtyAttributes;

namespace Battle
{
	public class AttackContainer : MonoBehaviour
	{
		public Transform _Actions;

		List<BattleAttack> _Attacks;
		Dictionary<TransitionAsset, BattleAttack> _AttackDict;

		public void Init()
		{
			_Attacks = _Actions.GetComponentsInChildren<BattleAttack>().ToList();
			_Attacks.ForEach(x => x.gameObject.SetActive(false));
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
