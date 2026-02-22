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
		public Character _Player;
		public List<Character> _Enemys;

		[Header("UI")]
		public GameObject _EnemyHPSliderPrefab;
		public Transform _EnemyHPSliderParent;

		void Start()
		{
			foreach (Character enemyChar in _Enemys)
			{
				GameObject enemyHPSlider = Instantiate(_EnemyHPSliderPrefab, _EnemyHPSliderParent);
				Enemy enemy = enemyChar.GetComponent<Enemy>();
				enemy._HPSlider = enemyHPSlider.GetComponent<Slider>();
				enemy._HPSlider_Inner = enemyHPSlider.transform.Find("Inner").GetComponent<Slider>();
			}

			Game.LockCursor(true);
		}

		void Update()
		{
			// 배경 회전
			_BgNear.rotation = _MainCamera.rotation;
			_BgGround.eulerAngles = _BgGround.eulerAngles.WithX(_MainCamera.eulerAngles.x);

			UpdateUI();
		}
	}
}
