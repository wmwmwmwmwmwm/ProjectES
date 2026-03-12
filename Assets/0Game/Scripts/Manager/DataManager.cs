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

	[Serializable]
	public class Stage
	{
		public string _Name;
		public Vector3 _StartPosition;
		public Quaternion _StartRotation;

		[Serializable]
		public class Spawn
		{
			public string _CharacterName;
			public Vector3 _Position;
			public Quaternion _Rotation;
		}
		public List<Spawn> _Spawns;
	}
	public List<Stage> _Stages;

	public List<BattleAttack> _BattleAttacks;
	public List<Character> _Characters;

	public Dictionary<TransitionAsset, List<Effect>> _EffectDict;
	public Dictionary<TransitionAsset, BattleAttack> _AttackDict;
	public Dictionary<string, Stage> _StageDict;
	public Dictionary<string, Character> _CharacterDict;

	protected override void Init()
	{
		_EffectDict = _Effects.GroupBy(x => x._Transition).ToDictionary(a => a.Key, b => b.ToList());
		_AttackDict = _BattleAttacks.ToDictionary(a => a._Transition, b => b);
		_StageDict = _Stages.ToDictionary(a => a._Name, b => b);
		_CharacterDict = _Characters.ToDictionary(a => a._Name, b => b);
	}

	public List<Effect> GetEffectDatas(TransitionAsset transition)
	{
		if (!transition) return null;
		_EffectDict.TryGetValue(transition, out List<Effect> datas);
		return datas;
	}

	public BattleAttack GetAttack(TransitionAsset transition)
	{
		if (!transition) return null;
		_AttackDict.TryGetValue(transition, out BattleAttack data);
		return data;
	}

	public Stage GetStageData(string name)
	{
		_StageDict.TryGetValue(name, out Stage stage);
		return stage;
	}

	public Character GetCharacter(string name)
	{
		_CharacterDict.TryGetValue(name, out Character character);
		return character;
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
