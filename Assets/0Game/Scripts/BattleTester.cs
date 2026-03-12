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
		public Transform _StartPosition;
		public List<Player> _PlayerPrefabs;
		public List<Enemy> _EnemyPrefabs;

		BattleController Controller => BattleController.Instance;

		void Start()
		{
			if (Game._StartScene != SceneName.Glacier) return;

			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				SceneManager.LoadScene(SceneName.Battle, LoadSceneMode.Additive);
				Scene battleScene = SceneManager.GetSceneByName(SceneName.Battle);
				yield return new WaitUntil(() => battleScene.isLoaded);
				List<Player> players = new();
				foreach (Player prefab in _PlayerPrefabs)
				{
					Player player = Instantiate(prefab);
					player.transform.SetPositionAndRotation(_StartPosition.position, _StartPosition.rotation);
					players.Add(player);
				}
				Controller._Players = players;
				List<Enemy> enemys = new();
				foreach (Enemy prefab in _EnemyPrefabs)
				{
					Enemy enemy = Instantiate(prefab);
					enemys.Add(enemy);
				}
				Controller._Enemys = enemys;
				Controller.Init();
				yield return null;
				Game.LockCursor(true);
			}
		}
	}
}
