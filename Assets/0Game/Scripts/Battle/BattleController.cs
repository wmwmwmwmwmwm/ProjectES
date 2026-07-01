using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
		public CinemachineCamera _CinemachineCamera;
		public Transform _CameraTarget, _CameraTargetArm;
		public Transform _ShakeCameraTransform;

		bool _Init;
		CinemachineBrain _CinemachineBrain;
		CinemachineThirdPersonFollow _CameraThirdPerson;
		[HideInInspector] public Transform _BgGround;
		[HideInInspector] public Transform _BgNear;
		[HideInInspector] public RectTransform _WorldBound;
		[HideInInspector] public List<Player> _Players;
		[HideInInspector] public List<Enemy> _Enemys;
		[HideInInspector] public Player _ActivePlayer;
		[HideInInspector] public DataManager.Stage _CurrentStage;
		[HideInInspector] public Interactable _CurrentInteractable;
		float _CurrentShakeCameraStrength;

		// 유저 정보
		int _GreyItem, _BlueItem, _RedItem;

		IEnumerator Start()
		{
			if (Game.IsTestMode()) yield break;

			yield return new WaitUntil(() => Game.GetCurrentScene().isLoaded);

			Init();

			// 배치
			foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
			{
				Destroy(enemy.gameObject);
			}
			_CurrentStage = Data.GetStageData(Game._CurrentScene);
			List<string> playerNames = new() { "Nolan", "Inasi" }; // todo 전투 캐릭터 리스트
			foreach (string playerName in playerNames)
			{
				SpawnPlayer(playerName, _CurrentStage._StartPosition, _CurrentStage._StartRotation);
			}
			foreach (DataManager.Stage.Spawn spawn in _CurrentStage._Spawns)
			{
				SpawnEnemy(spawn._CharacterName, spawn._Position, spawn._Rotation, spawn._Scale);
			}

			Init2();
		}

		public void Init()
		{
			//_InvisibleWallOverlaps = new RaycastHit[10];
			_Players = new();
			_Enemys = new();
			_EnemyHPUIs = new();

			_BgGround = GameObject.Find("Bg/Ground").transform;
			_BgNear = GameObject.Find("Bg/Near").transform;
			_WorldBound = GameObject.Find("WorldBound").GetComponent<RectTransform>();
			GameObject.Find("StartPosition").SetActive(false);
			GameObject.Find("EditorCamera").SetActive(false);
			_CinemachineBrain = _MainCamera.GetComponent<CinemachineBrain>();
			_CameraThirdPerson = _CinemachineCamera.GetComponent<CinemachineThirdPersonFollow>();

			InitMinimap();
			InitUI();
		}

		public void Init2()
		{
			Game.LockCursor(true);
			SetActivePlayer(0);
			_ActivePlayer._LookRotation = _CurrentStage._StartRotation.eulerAngles;

			_Init = true;
		}

		void Update()
		{
			if (!_Init) return;

			// 배경 회전
			_BgNear.eulerAngles = new(-_MainCamera.eulerAngles.x, -_MainCamera.eulerAngles.y, 0f);
			_BgGround.eulerAngles = _BgGround.eulerAngles.WithX(_MainCamera.eulerAngles.x);

			// 카메라 위치
			Vector3 cameraOffset = _ActivePlayer._CameraOffset;
			cameraOffset += _ShakeCameraTransform.position;
			_CameraThirdPerson.ShoulderOffset.y = cameraOffset.y;
			_CameraTargetArm.localPosition = new(0f, 0f, cameraOffset.z);

			// 카메라 회전
			_CameraTarget.SetPositionAndRotation(_ActivePlayer.transform.position, Quaternion.Euler(_ActivePlayer._LookRotation));

			// 카메라 업데이트
			_CinemachineBrain.ManualUpdate();

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
				_CameraTarget.SetPositionAndRotation(pos, rot);
				nextPlayer._CameraOffset = nextPlayer.GetCameraOffset();
				nextPlayer.c.Motor.BaseVelocity = _ActivePlayer.c.Motor.BaseVelocity;
				nextPlayer.c.Motor.GroundingStatus = _ActivePlayer.c.Motor.GroundingStatus;
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

		public void ShakeCamera(float duration, float strength)
		{
			if (duration == 0f) return;

			//float t = Mathf.InverseLerp(0.1f, 1f, duration);
			//float strength = Mathf.Lerp(0.05f, 0.1f, t);
			if (strength < _CurrentShakeCameraStrength) return;

			_CurrentShakeCameraStrength = strength;
			_ShakeCameraTransform.DOShakePosition(
				duration: duration,
				strength: strength,
				vibrato: 1000).OnKill(() => _CurrentShakeCameraStrength = 0f);
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

		public void SpawnEnemy(string name, Vector3 pos, Quaternion rot, float scale = 1f)
		{
			Character c = Data.GetBattleCharacter(name);
			Enemy enemy = Instantiate(c).GetComponent<Enemy>();
			enemy.Init();
			enemy.c.SetPositionAndRotation(pos, rot);
			enemy.transform.localScale = Vector3.one * scale;
			enemy._Agent.enabled = true;
			AddEnemyHPUI(enemy);
			AddMinimapMarker(enemy.c, false);
			enemy.Init2();
			_Enemys.Add(enemy);
		}

		public void AddForceToFragment(Fragment fragment, float strength, Vector3 hitDirection)
		{
			Rigidbody rigidbody = fragment.GetComponent<Rigidbody>();
			//hitDirection = hitDirection.RandomizeVector(10f, 0f, 10f);
			rigidbody.AddForce(hitDirection * strength /*+ Vector3.up * 1f*/, ForceMode.Impulse);
		}

		public void ObtainItem(DropItem item)
		{
			switch (item)
			{
				case Item_Grey:
					_GreyItem++;
					RefreshItemCount();
					break;
				case Item_Blue:
					_BlueItem++;
					RefreshItemCount();
					break;
				case Item_Red:
					_RedItem++;
					RefreshItemCount();
					break;
			}
		}

		public void Interact()
		{
			switch (_CurrentInteractable)
			{
				case Door door:
					InteractDoor(door);
					break;
			}
		}

		void InteractDoor(Door door)
		{
			switch (door._Type)
			{
				case Door.DoorType.Grey:
					OpenDoor(ref _GreyItem);
					break;
				case Door.DoorType.Blue:
					OpenDoor(ref _BlueItem);
					break;
				case Door.DoorType.Red:
					OpenDoor(ref _RedItem);
					break;
			}

			void OpenDoor(ref int itemCount)
			{
				if (itemCount == 0) return;

				door.gameObject.SetActive(false);
				itemCount--;
				RefreshItemCount();
			}
		}

		public void SetCurrentInteractable(Interactable interactable)
		{
			_CurrentInteractable = interactable;
			_UseItemOverlay.SetActive(true);
			_RequireItemOverlay.SetActive(true);
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
	}
}
