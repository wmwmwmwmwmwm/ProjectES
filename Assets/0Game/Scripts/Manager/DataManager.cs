using Animancer;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{
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

	[Serializable]
	public class Character
	{
		public string _Name;

		[Serializable]
		public class Facial
		{
			public string _Name;

			[Serializable]
			public class BlendShape
			{
				public string _BlendShapeName;
				public float _Value;
			}
			public List<BlendShape> _BlendShapes;
		}
		public List<Facial> _Facials;

		[HideInInspector] public Dictionary<string, Facial> _FacialDict;

		public Facial GetFacial(string name) => _FacialDict[name];
	}
	public List<Character> _Characters;

	public List<Battle.Character> _BattleCharacters;

	public Dictionary<string, Stage> _StageDict;
	public Dictionary<string, Battle.Character> _BattleCharacterDict;
	public Dictionary<string, Character> _CharacterDict;

	protected override void Init()
	{
		_StageDict = _Stages.ToDictionary(a => a._Name, b => b);
		_BattleCharacterDict = _BattleCharacters.ToDictionary(a => a._Name, b => b);
		_CharacterDict = _Characters.ToDictionary(a => a._Name, b => b);
		foreach (Character c in _Characters)
		{
			c._FacialDict = c._Facials.ToDictionary(a => a._Name, b => b);
		}
	}

	public Stage GetStageData(string name)
	{
		_StageDict.TryGetValue(name, out Stage stage);
		return stage;
	}

	public Battle.Character GetBattleCharacter(string name)
	{
		_BattleCharacterDict.TryGetValue(name, out Battle.Character character);
		return character;
	}

	public Character GetCharacter(string name)
	{
		_CharacterDict.TryGetValue(name, out Character character);
		return character;
	}

	public void SetupEffectPosition(GameObject instance, Battle.EffectContainer.Effect info, Transform parent)
	{
		instance.transform.SetParent(parent);
		instance.transform.SetLocalPositionAndRotation(info._Pos, Quaternion.Euler(info._Rot));
		instance.transform.localScale = info._Scale * Vector3.one;
		Transform tr = info._IsLocal ? parent : null;
		instance.transform.SetParent(tr);
	}
}
