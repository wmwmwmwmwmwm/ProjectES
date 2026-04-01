using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static SingletonManager;

namespace Battle
{
	public class BattleTester : MonoBehaviour
	{
		[BoxGroup("설정")] public List<Player> _PlayerPrefabs;
		[BoxGroup("설정")] public string _StageName;

		BattleController Controller => BattleController.Instance;

		void Start()
		{
			if (Game._StartScene != SceneName.Glacier) return;

			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
				{
					Destroy(enemy.gameObject);
				}
				SceneManager.LoadScene(SceneName.Battle, LoadSceneMode.Additive);
				Scene battleScene = SceneManager.GetSceneByName(SceneName.Battle);
				yield return new WaitUntil(() => battleScene.isLoaded);

				Controller.Init();
				DataManager.Stage stage = Data.GetStageData(_StageName);
				Controller._Players = new();
				foreach (Player prefab in _PlayerPrefabs)
				{
					Controller.SpawnPlayer(prefab.GetComponent<Character>()._Name, stage._StartPosition, stage._StartRotation);
				}
				Controller._Enemys = new();
				foreach (DataManager.Stage.Spawn spawn in stage._Spawns)
				{
					Controller.SpawnEnemy(spawn._CharacterName, spawn._Position, spawn._Rotation);
				}
				Controller.Init2();
			}
		}
	}
}
