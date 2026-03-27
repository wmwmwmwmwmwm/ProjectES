using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using static SingletonManager;

namespace Battle
{
	public partial class BattleController : SingleInstance<BattleController>
	{
		public Transform _MainCamera;
		public Transform _CameraTarget;
		public CinemachineThirdPersonFollow _CameraThirdPerson;

		bool _Init;
		[HideInInspector] public Transform _BgGround;
		[HideInInspector] public Transform _BgNear;
		[HideInInspector] public RectTransform _WorldBound;
		[HideInInspector] public List<Player> _Players;
		[HideInInspector] public List<Enemy> _Enemys;
		[HideInInspector] public Player _ActivePlayer;

		public List<Player> _PlayerPrefabs;
		public List<Enemy> _EnemyPrefabs;

		public void Init()
		{
			bool isTest = Game._StartScene == SceneName.Glacier;

			_BgGround = GameObject.Find("Bg/Ground").transform;
			_BgNear = GameObject.Find("Bg/Near").transform;
			_WorldBound = GameObject.Find("WorldBound").GetComponent<RectTransform>();
			InitMinimap();
			InitUI();

			// 배치
			if (!isTest)
			{
				// todo prefab 없애고 stage로 대체
				List<Player> players = new();
				foreach (Player prefab in _PlayerPrefabs)
				{
					Player player = Instantiate(prefab);
					//player.transform.SetPositionAndRotation(_StartPosition.position, _StartPosition.rotation);
					player.Init();
					players.Add(player);
				}
				_Players = players;
				List<Enemy> enemys = new();
				foreach (Enemy prefab in _EnemyPrefabs)
				{
					Enemy enemy = Instantiate(prefab);
					enemy.Init();
					enemys.Add(enemy);
				}
				_Enemys = enemys;
			}

			// 초기화
			foreach (Player player in _Players)
			{
				player.gameObject.SetActive(false);
				AddMinimapMarker(player.c, true);
			}
			foreach (Enemy enemy in _Enemys)
			{
				AddMinimapMarker(enemy.c, false);
			}

			Game.LockCursor(true);
			SetActivePlayer(0);

			_Init = true;
		}

		void Update()
		{
			if (!_Init) return;

			// 배경 회전
			_BgNear.eulerAngles = new(-_MainCamera.eulerAngles.x, -_MainCamera.eulerAngles.y, 0f);
			_BgGround.eulerAngles = _BgGround.eulerAngles.WithX(_MainCamera.eulerAngles.x);

			UpdateMinimap();
			UpdateUI();
		}

		public bool SetActivePlayer(int index)
		{
			Player nextPlayer = _Players[index];
			if (_ActivePlayer == nextPlayer) return false;

			// 이동
			if (_ActivePlayer)
			{
				nextPlayer.c._MoveInput = _ActivePlayer.c._MoveInput;
				nextPlayer._LookInput = _ActivePlayer._LookInput;
				nextPlayer._LookRotation = _ActivePlayer._LookRotation;
				nextPlayer.c._AimDestRotation = _ActivePlayer.c._AimDestRotation;
				_ActivePlayer.transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
				nextPlayer.c.SetPositionAndRotation(pos, rot);
				nextPlayer.c._Motor.BaseVelocity = _ActivePlayer.c._Motor.BaseVelocity;
				nextPlayer.c._Motor.GroundingStatus = _ActivePlayer.c._Motor.GroundingStatus;
				nextPlayer.c._FSM.ForceSetDefaultState();
			}

			foreach (Player player in _Players)
			{
				bool active = player == nextPlayer;
				player.gameObject.SetActive(active);
				player.ReceiveInput(active);
				player._UI_HP.transform.localScale = active ? Vector3.one : 0.8f * Vector3.one;
			}

			_ActivePlayer = nextPlayer;
			return true;
		}

		public GameObject _AttackAreaDecalPrefab;
		public void ShowAttackAreaDecal(Vector3 position, Quaternion rotation, float duration)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				GameObject decal = Instantiate(_AttackAreaDecalPrefab, position, rotation);
				decal.transform.DOLocalRotate(Vector3.up * 360f, 2.4f, RotateMode.LocalAxisAdd).SetLoops(-1);
				yield return new WaitForSeconds(duration);
				Destroy(decal);
			}
		}

		public void PlayEffect123123(GameObject prefab, Character owner, Vector3 pos, Quaternion rot)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				GameObject hitEffect = Instantiate(prefab, pos, rot);
				BattleEffect e = hitEffect.AddComponent<BattleEffect>();
				e._Owner = owner;
				e.Init();
				yield return new WaitForSeconds(3f);
				Destroy(hitEffect);
			}
		}

		public Transform _VirtualCameraHandle;
		public float duration = 0.1f;
		public float strength = 1f;
		public int vibrato = 10;
		public float randomness = 90f;
		public bool fadeout = false;
		[NaughtyAttributes.Button("aa")]
		public void aa()
		{
			_VirtualCameraHandle.DOShakePosition(
				duration: duration,
				strength: strength,
				vibrato: vibrato,
				randomness: randomness,
				fadeOut: fadeout);
		}
	}
}
