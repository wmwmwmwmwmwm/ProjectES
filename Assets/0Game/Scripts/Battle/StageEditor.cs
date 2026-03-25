#if UNITY_EDITOR
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static SingletonManager;

namespace Battle
{ 
	public class StageEditor : MonoBehaviour
	{
		[BoxGroup("설정")] public string _StageName;

		public Transform _StartPosition;
		public DataManager _DataManagerPrefab;

		[Button("저장")]
		public void SaveButton()
		{
			_DataManagerPrefab._Stages.RemoveAll(x => x._Name == _StageName);

			DataManager.Stage stage = new()
			{
				_Name = _StageName,
				_StartPosition = _StartPosition.position,
				_StartRotation = _StartPosition.rotation,
				_Spawns = new(),
			};
			Enemy[] enemys = FindObjectsByType<Enemy>(FindObjectsSortMode.InstanceID);
			foreach (Enemy enemy in enemys)
			{
				DataManager.Stage.Spawn spawn = new()
				{
					_CharacterName = enemy.GetComponent<Character>()._Name,
					_Position = enemy.transform.position,
					_Rotation = enemy.transform.rotation,
				};
				stage._Spawns.Add(spawn);
			}
			_DataManagerPrefab._Stages.Add(stage);
			Util.SetDirty(_DataManagerPrefab);
			AssetDatabase.SaveAssets();

			Debug.Log($"{_StageName} 저장 완료");
		}

		[Button("로드")]
		public void LoadButton()
		{
			DataManager.Stage stage = _DataManagerPrefab._Stages.Find(x => x._Name == _StageName);
			if (stage == null)
			{
				Debug.LogWarning($"{_StageName} 스테이지가 존재하지 않음");
				return;
			}

			if (FindAnyObjectByType<Enemy>())
			{
				bool yes = EditorUtility.DisplayDialog("", "모든 적들을 초기화합니다", "Delete", "Cancel");
				if (yes)
				{
					DeleteAll();
				}
				else return;
			}

			DeleteAll();
			_StartPosition.SetPositionAndRotation(stage._StartPosition, stage._StartRotation);
			foreach (DataManager.Stage.Spawn spawn in stage._Spawns)
			{
				Character c = _DataManagerPrefab._BattleCharacters.Find(x => x._Name == spawn._CharacterName);
				Character character = PrefabUtility.InstantiatePrefab(c) as Character;
				character.transform.SetPositionAndRotation(spawn._Position, spawn._Rotation);
			}

			Debug.Log($"{_StageName} 로드 완료");
		}

		[Button("초기화")]
		public void ResetButton()
		{
			bool yes = EditorUtility.DisplayDialog("", "모든 적들을 초기화합니다", "Delete", "Cancel");
			if (!yes) return;

			DeleteAll();
			Debug.Log($"초기화 완료");
		}

		void DeleteAll()
		{
			Enemy[] enemys = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
			foreach (Enemy enemy in enemys)
			{
				DestroyImmediate(enemy.gameObject);
			}
		}
	}
}
#endif
