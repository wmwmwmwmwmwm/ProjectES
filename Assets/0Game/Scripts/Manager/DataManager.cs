using Battle;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{
	[Serializable]
	public class Effect
	{
		public AnimationClip _Clip;
		public GameObject _EffectPrefab;
		//public AssetReferenceT<GameObject> _Prefab;
		public Vector3 _Pos;
		public Vector3 _Rot;
		public float _Scale;
		public float _Delay;
		public bool _IsLocal;
	}
	public List<Effect> _Effects;

	[Serializable]
	public class Attack
	{
		public AnimationClip _Clip;
		public AttackCollider _AttackColliderPrefab;
		public GameObject _HitEffectPrefab;
		public float _HitDelay;
		public float _DamageDuration, _AttackerHitStunDuration;
		public float _ForceForward, _ForceUp;
		public float _Cooltime;
		public AttackSkillType _SkillType;
		public AttackRangeType _RangeType;
		public AttackAreaType _AreaType;
	}
	public List<Attack> _Attacks;

    public Dictionary<AnimationClip, Effect> _EffectDict;
	public Dictionary<AnimationClip, Attack> _AttackDict;

	protected override void Init()
	{
		_EffectDict = new();
		foreach (Effect data in _Effects)
		{
			_EffectDict.Add(data._Clip, data);
		}
		_AttackDict = new();
		foreach (Attack data in _Attacks)
		{
			_AttackDict.Add(data._Clip, data);
		}
	}

	public Effect GetEffectData(AnimationClip clip)
	{
		if (!clip) return null;
		_EffectDict.TryGetValue(clip, out Effect data);
		return data;
	}

	public Attack GetAttackData(AnimationClip clip)
	{
		if (!clip) return null;
		_AttackDict.TryGetValue(clip, out Attack data);
		return data;
	}

	public void SetupEffectPosition(GameObject instance, Effect info, Transform parent)
	{
		instance.transform.SetParent(parent);
		instance.transform.SetLocalPositionAndRotation(info._Pos, Quaternion.Euler(info._Rot));
		instance.transform.localScale = info._Scale * Vector3.one;
		Transform tr = info._IsLocal ? parent : null;
		instance.transform.SetParent(tr);
	}
}
