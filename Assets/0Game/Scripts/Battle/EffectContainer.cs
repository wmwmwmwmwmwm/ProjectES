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
	public class EffectContainer : MonoBehaviour
	{
		[ReadOnly] public List<Effect> _Datas;

		Dictionary<TransitionAsset, List<Effect>> _DataDict;

		public void Init()
		{
			_DataDict = _Datas.GroupBy(x => x._Transition).ToDictionary(a => a.Key, b => b.ToList());
		}

		public List<Effect> GetEffectDatas(TransitionAsset transition)
		{
			if (!transition) return null;
			_DataDict.TryGetValue(transition, out List<Effect> datas);

			if (_Datas == null)
			{
				Debug.LogError($"{gameObject.name} {transition} 이펙트 설정 없음");
			}

			return datas;
		}
	}
}
