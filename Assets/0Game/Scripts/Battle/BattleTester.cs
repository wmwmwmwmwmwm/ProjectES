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

		public GameObject _StartPosition;

		BattleController Controller => BattleController.Instance;

		void Start()
		{
			if (Game._StartScene != SceneName.Glacier) return;

			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				_StartPosition.SetActive(false);
				foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
				{
					Destroy(enemy.gameObject);
				}
				SceneManager.LoadScene(SceneName.Battle, LoadSceneMode.Additive);
				Scene battleScene = SceneManager.GetSceneByName(SceneName.Battle);
				yield return new WaitUntil(() => battleScene.isLoaded);
				DataManager.Stage stage = Data.GetStageData(_StageName);
				List<Player> players = new();
				foreach (Player prefab in _PlayerPrefabs)
				{
					Player player = Instantiate(prefab);
					player.transform.SetPositionAndRotation(stage._StartPosition, stage._StartRotation);
					players.Add(player);
				}
				Controller._Players = players;
				List<Enemy> enemys = new();
				foreach (DataManager.Stage.Spawn spawn in stage._Spawns)
				{
					Character c = Data.GetCharacter(spawn._CharacterName);
					Enemy enemy = Instantiate(c).GetComponent<Enemy>();
					enemy.transform.SetPositionAndRotation(stage._StartPosition, stage._StartRotation);
					enemys.Add(enemy);
				}
				Controller._Enemys = enemys;
				Controller.Init();
			}
		}
	}
}
