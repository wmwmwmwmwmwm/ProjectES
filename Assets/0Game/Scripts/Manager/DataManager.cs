using Animancer;
using Battle;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{
	[Serializable]
	public class Effect
	{
		public AnimationClip _Clip;
		public TransitionAsset _Transition;
		public GameObject _EffectPrefab;
		//public AssetReferenceT<GameObject> _Prefab;
		public Vector3 _Pos;
		public Vector3 _Rot;
		public float _Scale;
		public float _Delay;
		public bool _IsLocal;
	}
	public List<Effect> _Effects;

	public List<BattleAttack> _BattleAttacks;
	//[Serializable]
	//public class Attack
	//{
	//	public AnimationClip _Clip;
	//	public BattleAttack _AttackPrefab;

	//	public float _Cooltime;
	//	public AttackSkillType _SkillType;
	//	public AttackRangeType _RangeType;
	//	public AttackAreaType _AreaType;

	//	public GameObject _HitEffectPrefab;
	//	public float _HitDelay;
	//	public float _DamageDuration, _AttackerHitStunDuration;
	//	public float _ForceForward, _ForceUp;
	//}
	//public List<Attack> _Attacks;

	//[Button("asdf")]
	//public void aaa()
	//{
	//	for (int i = 0; i < _BattleAttacks.Count; i++)
	//	{
	//		e attack = _BattleAttacks[i];
	//		BattleAttack battleAttack = _BattleAttacks[i];
	//		battleAttack._Clip = attack._Clip;
	//		battleAttack._Cooltime = attack._Cooltime;
	//		battleAttack._SkillType = attack._SkillType;
	//		battleAttack._RangeType = attack._RangeType;
	//		battleAttack._AreaType = attack._AreaType;
	//		var melee = battleAttack.GetComponent<MeleeAttack>();
	//		var hit = melee._AttackHits.First();
	//		hit._HitEffectPrefab = attack._HitEffectPrefab;
	//		hit._HitDelay = attack._HitDelay;
	//		hit._DamageDuration = attack._DamageDuration;
	//		hit._AttackerHitStunDuration = attack._AttackerHitStunDuration;
	//		hit._ForceForward = attack._ForceForward;
	//		hit._ForceUp = attack._ForceUp;
	//		Util.SetDirty(battleAttack);
	//	}
	//	Util.SetDirty(this);
	//}

	public Dictionary<TransitionAsset, Effect> _EffectDict;
	public Dictionary<TransitionAsset, BattleAttack> _AttackDict;

	protected override void Init()
	{
		_EffectDict = new();
		foreach (Effect data in _Effects)
		{
			_EffectDict.Add(data._Transition, data);
		}
		_AttackDict = new();
		foreach (BattleAttack data in _BattleAttacks)
		{
			_AttackDict.Add(data._Transition, data);
		}
	}

	public Effect GetEffectData(TransitionAsset transition)
	{
		if (!transition) return null;
		_EffectDict.TryGetValue(transition, out Effect data);
		return data;
	}

	public BattleAttack GetAttackData(TransitionAsset transition)
	{
		if (!transition) return null;
		_AttackDict.TryGetValue(transition, out BattleAttack data);
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
