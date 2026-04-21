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
		Dictionary<string, BattleAttack> _AttackDict;

		public void Init()
		{
			_Attacks = _Actions.GetComponentsInChildren<BattleAttack>().ToList();
			_Attacks.ForEach(x => x.gameObject.SetActive(false));
			_AttackDict = _Attacks.ToDictionary(a => a._Name, b => b);
		}

		public BattleAttack GetAttack(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;
			_AttackDict.TryGetValue(name, out BattleAttack data);
			return data;
		}
	}
}
