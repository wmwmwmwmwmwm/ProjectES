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
		public string _Name;
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
	public List<EffectInfo> _Effects;

    private void OnValidate()
    {
		_Effects = _EffectInfos;
		Util.SetDirty(gameObject);
    }

    [Serializable]
	public class AttackInfo
	{
		public string _Name;
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
	public List<AttackInfo> _AttackInfos;

	public Dictionary<string, EffectInfo> _EffectInfoDict;
	public Dictionary<string, AttackInfo> _AttackInfoDict;

	protected override void Init()
	{
		_EffectInfoDict = new();
		foreach (EffectInfo info in _EffectInfos)
		{
			_EffectInfoDict.Add(info._Name, info);
		}
	}

	public EffectInfo GetEffectInfo(string name)
	{
		_EffectInfoDict.TryGetValue(name, out EffectInfo info);
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
