using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static SingletonManager;

namespace Battle
{
	public partial class BattleController : SingleInstance<BattleController>
	{
		public Transform _MainCamera, _BackgroundCamera;
		public Transform _BgSky, _BgGround, _BgNear;
		public List<Player> _Players;
		public List<Enemy> _Enemys;

		[HideInInspector] public Player _ActivePlayer;

		protected override void Awake()
		{
			base.Awake();

			InitUI();
			foreach (Player player in _Players)
            {
				player.Init();
            }
            foreach (Enemy enemy in _Enemys)
            {
				enemy.Init();
            }
		}

		void Start()
		{
			Game.LockCursor(true);
			SetActivePlayer(0);
		}

		void Update()
		{
			// 배경 회전
			_BgNear.rotation = _MainCamera.rotation;
			_BgGround.eulerAngles = _BgGround.eulerAngles.WithX(_MainCamera.eulerAngles.x);

			UpdateUI();
		}

		public void SetActivePlayer(int index)
		{
            _Players[index].transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
            _ActivePlayer = _Players[index];
			foreach (Player player in _Players)
            {
				bool active = player == _ActivePlayer;
				player.gameObject.SetActive(active);
				player.ReceiveInput(active);
            }
			_ActivePlayer.transform.SetPositionAndRotation(pos, rot);
        }
	}
}
