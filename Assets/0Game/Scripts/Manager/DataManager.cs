using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
		public float _Delay;
		public bool _IsLocal;

		// 공격
		public GameObject _HitEffectPrefab;
		public float _HitDelay;
		public float _DamageDuration;
		public float _ForceForward, _ForceUp;
	}
	public List<EffectInfo> _EffectInfos;

	public Dictionary<AnimationClip, EffectInfo> _EffectInfoDict;

	protected override void Init()
	{
		_EffectInfoDict = new();
		foreach (EffectInfo info in _EffectInfos)
		{
			_EffectInfoDict.Add(info._Clip, info);
		}
	}

	public EffectInfo GetEffectInfo(AnimationClip clip)
	{
		if (!clip) return null;
		_EffectInfoDict.TryGetValue(clip, out EffectInfo info);
		return info;
	}

	public void SetupEffectPosition(GameObject instance, EffectInfo info, Transform parent)
	{
		instance.transform.SetParent(parent);
		instance.transform.SetLocalPositionAndRotation(info._Pos, Quaternion.Euler(info._Rot));
		instance.transform.localScale = info._Scale * Vector3.one;
		Transform tr = info._IsLocal ? parent : null;
		instance.transform.SetParent(tr);
	}
}
