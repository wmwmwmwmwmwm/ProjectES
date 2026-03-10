using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static SingletonManager;

namespace Battle
{
	public partial class BattleController : SingleInstance<BattleController>
	{
		public Transform _MainCamera;
		public Transform _BgGround, _BgNear;
		public List<Player> _Players;
		public List<Enemy> _Enemys;

		[Header("테스트")]
		public string _TestStageName;

		[HideInInspector] public Player _ActivePlayer;

		void Start()
		{
			InitUI();
			foreach (Player player in _Players)
			{
				player.Init();
				player.gameObject.SetActive(false);
			}
			foreach (Enemy enemy in _Enemys)
			{
				enemy.Init();
			}

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

		public bool SetActivePlayer(int index)
		{
			Player nextPlayer = _Players[index];
			if (_ActivePlayer == nextPlayer) return false;

			// 이동
			if (_ActivePlayer)
			{
				nextPlayer._Character._MoveInput = _ActivePlayer._Character._MoveInput;
				nextPlayer._LookInput = _ActivePlayer._LookInput;
				nextPlayer._LookRotation = _ActivePlayer._LookRotation;
				nextPlayer._Character._AimDestRotation = _ActivePlayer._Character._AimDestRotation;
				_ActivePlayer.transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
				nextPlayer._Character._Motor.SetPositionAndRotation(pos, rot);
				nextPlayer._Character._Motor.MoveCharacter(pos);
				nextPlayer._Character._Motor.RotateCharacter(rot);
				nextPlayer._Character._Motor.BaseVelocity = _ActivePlayer._Character._Motor.BaseVelocity;
				nextPlayer._Character._Motor.GroundingStatus = _ActivePlayer._Character._Motor.GroundingStatus;
				nextPlayer._Character._FSM.ForceSetDefaultState();
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
	}
}
