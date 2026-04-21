using System.Collections.Generic;
using UnityEngine;
using Animancer;
using static Battle.Character;
using System.Linq;
using System;

namespace Battle
{
	public class EffectContainer : MonoBehaviour
	{
		[Serializable]
		public class Effect
		{
			public string _Name;
			public GameObject _EffectPrefab;
			//public AssetReferenceT<GameObject> _Prefab;
			public Vector3 _Pos;
			public Vector3 _Rot;
			public float _Scale;
			public float _Delay;
			public bool _IsLocal;
		}

		public List<Effect> _Datas;

		Dictionary<string, List<Effect>> _DataDict;

		public void Init()
		{
			_DataDict = _Datas.GroupBy(x => x._Name).ToDictionary(a => a.Key, b => b.ToList());
		}

		public List<Effect> GetEffectDatas(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;
			_DataDict.TryGetValue(name, out List<Effect> datas);

			if (_Datas == null)
			{
				Debug.LogError($"{gameObject.name} {name} 이펙트 설정 없음");
			}

			return datas;
		}
	}
}
