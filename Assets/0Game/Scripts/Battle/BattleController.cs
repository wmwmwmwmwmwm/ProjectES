using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static SingletonManager;

namespace Battle
{
	public partial class BattleController : SingleInstance<BattleController>
	{
		public Transform _MainCamera;
		public Transform _CameraTarget;
		public CinemachineThirdPersonFollow _CameraThirdPerson;
		public Transform _ShakeCameraTransform;

		bool _Init;
		[HideInInspector] public Transform _BgGround;
		[HideInInspector] public Transform _BgNear;
		[HideInInspector] public RectTransform _WorldBound;
		[HideInInspector] public List<Player> _Players;
		[HideInInspector] public List<Enemy> _Enemys;
		[HideInInspector] public Player _ActivePlayer;
		float _CurrentShakeCameraStrength;

		public List<Player> _PlayerPrefabs;
		public List<Enemy> _EnemyPrefabs;

		public void Init()
		{
			bool isTest = Game._StartScene == SceneName.Glacier;

			_Players = new();
			_Enemys = new();
			_EnemyHPUIs = new();
			_BgGround = GameObject.Find("Bg/Ground").transform;
			_BgNear = GameObject.Find("Bg/Near").transform;
			_WorldBound = GameObject.Find("WorldBound").GetComponent<RectTransform>();
			GameObject.Find("StartPosition").SetActive(false);
			InitMinimap();
			InitUI();

			// 배치
			if (isTest) return;
			foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
			{
				Destroy(enemy.gameObject);
			}
			DataManager.Stage stage = Data.GetStageData(Game._CurrentScene);
			List<string> playerNames = new() { "Nolan", "Inasi" }; // todo 전투 캐릭터 리스트
			foreach (string playerName in playerNames)
			{
				SpawnPlayer(playerName, stage._StartPosition, stage._StartRotation);
			}
			foreach (DataManager.Stage.Spawn spawn in stage._Spawns)
			{
				SpawnEnemy(spawn._CharacterName, spawn._Position, spawn._Rotation);
			}
		}

		public void Init2()
		{
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

			// 카메라 위치
			Vector3 cameraOffset = _ShakeCameraTransform.position;
			cameraOffset.y += _ActivePlayer._CameraOffsetY;
			_CameraThirdPerson.ShoulderOffset = cameraOffset;

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

		public void ShakeCamera(float duration)
		{
			if (duration == 0f) return;

			float t = Mathf.InverseLerp(0.1f, 1f, duration);
			float strength = Mathf.Lerp(0.05f, 0.1f, t);
			if (strength < _CurrentShakeCameraStrength) return;

			_CurrentShakeCameraStrength = strength;
			_ShakeCameraTransform.DOShakePosition(
				duration: duration,
				strength: strength,
				vibrato: 1000);
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

		public void SpawnPlayer(string name, Vector3 pos, Quaternion rot)
		{
			Character c = Data.GetBattleCharacter(name);
			Player player = Instantiate(c).GetComponent<Player>();
			player.Init();
			player.c.SetPositionAndRotation(pos, rot);
			player.gameObject.SetActive(false);
			AddPlayerHPUI(player);
			AddMinimapMarker(player.c, true);
			player.Init2();
			_Players.Add(player);
		}

		public void SpawnEnemy(string name, Vector3 pos, Quaternion rot)
		{
			Character c = Data.GetBattleCharacter(name);
			Enemy enemy = Instantiate(c).GetComponent<Enemy>();
			enemy.Init();
			enemy.c.SetPositionAndRotation(pos, rot);
			enemy.GetComponent<NavMeshAgent>().enabled = true;
			AddEnemyHPUI(enemy);
			AddMinimapMarker(enemy.c, false);
			enemy.Init2();
			_Enemys.Add(enemy);
		}
	}
}
