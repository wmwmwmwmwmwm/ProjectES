using DG.Tweening;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static DataManager;
using static SingletonManager;

namespace Battle
{
	public partial class Enemy : MonoBehaviour
	{
		public Transform _HPSliderPosition;

		[BoxGroup("설정")] public bool _CanClimb;

		[HideInInspector] public Character c;
		[HideInInspector] public NavMeshAgent _Agent;
		NavMeshPath _Path;
		bool _Noticed;
		float _FindPathTime;
		Vector3[] _PathCorners;
		float _LastAttackTime;
		[HideInInspector] public float _LastDamagedTime;
		Collider[] _ColliderHits;
		[HideInInspector] public UI_EnemyHP _HPUI;

		BattleController Controller => BattleController.Instance;
		bool IsMovable => _Agent;

		public void Init()
		{
			_ColliderHits = new Collider[30];
			c = GetComponent<Character>();
			_Agent = GetComponent<NavMeshAgent>();
			_Path = new();
			_PathCorners = new Vector3[2];

			if (IsMovable)
			{
				_Agent.updatePosition = false;
				_Agent.updateRotation = false;
				_Agent.updateUpAxis = false;
			}
			_FindPathTime = Const.TimeDefault;
			_LastAttackTime = Const.TimeDefault;
			_LastDamagedTime = Const.TimeDefault;
			_LastSummonTime = Const.TimeDefault;

			c.Init();
		}

		public void Init2()
		{
			c.Init2();
		}

		void Update()
		{
			// AI
			Idle();
			Move();
			Attack();
			SummonOnAir();
		}

		public void NoticeAround()
		{
			int count = Physics.OverlapSphereNonAlloc(
				position: transform.position,
				radius: 30f,
				results: _ColliderHits,
				layerMask: Layer.EnemyLayerMask);
			if (count == 0) return;

			for (int i = 0; i < count; i++)
			{
				Enemy enemy = _ColliderHits[i].GetComponentInParent<Enemy>();
				enemy._Noticed = true;
			}
		}

		void Idle()
		{
			if (c.IsDead()) return;
			if (_Noticed) return;

			Vector3 distanceVector = GetPlayerDistanceVector();
			if (distanceVector.magnitude < 10f)
			{
				_Noticed = true;
			}
		}

		void Move()
		{
			if (c.IsDead())
			{
				c._MoveInput = Vector3.zero;
				return;
			}

			if (!_Noticed) return;
			if (!_Agent.isOnNavMesh) return;

			// 경로 재설정
			_Agent.Warp(transform.position);
			if (Time.time - _FindPathTime > 0.1f)
			{
				Vector3 playerPos = Controller._ActivePlayer.transform.position;
				bool valid = _Agent.CalculatePath(playerPos, _Path);
				if (valid)
				{
					_Agent.SetPath(_Path);
				}
				_FindPathTime = Time.time;
			}

			if (!_Agent.hasPath) return;

			// 이동
			int count = _Path.GetCornersNonAlloc(_PathCorners);
			Vector3 destination;
			if (count > 1)
			{
				destination = _PathCorners[1];
			}
			else 
			{
				destination = _PathCorners[0];
			}
			Vector3 distance = destination - transform.position;
			bool move = distance.magnitude > 2f;
			c._MoveInput = move ? Vector3.forward : Vector3.zero;

			// 회전
			Vector3 playerCenter = Controller._ActivePlayer.c.Center;
			c._AimDestRotation = Quaternion.LookRotation(playerCenter - c.Center);
		}

		void Attack()
		{
			if (c.IsDead()) return;
			if (!_Noticed) return;
			if (Time.time - _LastAttackTime < 1f) return;
			if (Time.time - _LastDamagedTime < 1.6f) return;

			Vector3 distanceVector = GetPlayerDistanceVector();
			if (distanceVector.magnitude < 2000f)
			{
				c.Skill1(default);
				_LastAttackTime = Time.time;
			}
		}

		float _LastSummonTime;
		public GameObject _SummonParticlePrefab;
		public string _SummonEnemyName;
		public SphereCollider _SummonArea;
		void SummonOnAir()
		{
			if (c.IsDead()) return;
			if (string.IsNullOrEmpty(_SummonEnemyName)) return;
			if (!_Noticed) return;
			if (Time.time - _LastSummonTime < 1f) return;

			Controller.PlayEffect123123(_SummonParticlePrefab, c, transform.position, transform.rotation);
			Vector3 pos = transform.position;
			Controller.SpawnEnemy(_SummonEnemyName, pos, transform.rotation);
			_LastSummonTime = Time.time;
		}

		Vector3 GetPlayerDistanceVector()
		{
			Vector3 playerPos = Controller._ActivePlayer.transform.position;
			return transform.position - playerPos;
		}

		public void SetHPSliderValue(float value)
		{
			_HPUI._HPSlider.gameObject.SetActive(value > 0f && value < 1f);
			_HPUI._HPSlider.value = value;
			_HPUI._HPSlider_Inner.DOComplete();
			_HPUI._HPSlider_Inner.DOValue(value, 0.3f).SetEase(Ease.Linear).SetSpeedBased(true);
		}
	}
}
