using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static SingletonManager;

namespace Battle
{
	public class BattleTester : MonoBehaviour
	{
		public Player _PlayerPrefab;

		void Start()
		{
			Player player = Instantiate(_PlayerPrefab);
			player.Init();
			Game.LockCursor(true);
		}
	}
}
