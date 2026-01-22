using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class DataManager : Singleton<DataManager>
{
	[Serializable]
	public class EffectInfo
	{
		public AnimationClip _Clip;
		public GameObject _EffectPrefab;
		//public AssetReferenceT<GameObject> _Prefab;
		public Vector3 _Pos;
		public Vector3 _Rot;
		public float _Scale;
		public bool _IsLocal;
	}
	public List<EffectInfo> _EffectInfos;

	public Dictionary<AnimationClip, EffectInfo> _EffectInfoDict;

	protected override void Init() 
	{
		CreateDictionary();
	}

	public void CreateDictionary()
	{
		_EffectInfoDict = new();
		foreach (EffectInfo info in _EffectInfos)
		{
			_EffectInfoDict.Add(info._Clip, info);
		}
	}

	public void SetupEffect(GameObject instance, EffectInfo info, Transform parent)
	{
		instance.transform.SetParent(parent);
		instance.transform.SetLocalPositionAndRotation(info._Pos, Quaternion.Euler(info._Rot));
		instance.transform.localScale = info._Scale * Vector3.one;
		Transform tr = info._IsLocal ? parent : null;
		instance.transform.SetParent(tr);
	}
}
