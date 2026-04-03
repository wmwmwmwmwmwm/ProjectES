using DG.Tweening;
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

		[HideInInspector] public Character c;
		NavMeshAgent _Agent;
		bool _Noticed;
		float _LastAttackTime;
		Collider[] _ColliderHits;
		[HideInInspector] public UI_EnemyHP _HPUI;

		BattleController Controller => BattleController.Instance;
		bool IsMovable => _Agent;

		public void Init()
		{
			_ColliderHits = new Collider[30];
			c = GetComponent<Character>();
			_Agent = GetComponent<NavMeshAgent>();

			if (IsMovable)
			{
				_Agent.updatePosition = false;
				_Agent.updateRotation = false;
				_Agent.updateUpAxis = false;
			}
			_LastAttackTime = Const.TimeDefault;
			_LastSummonTime = Const.TimeDefault;

			c.Init();
		}

		public void Init2()
		{
			c.Init2();
		}

		void Update()
		{
			if (c.IsDead()) return;

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
				Enemy enemy = _ColliderHits[i].GetComponent<Enemy>();
				enemy._Noticed = true;
			}
		}

		void Idle()
		{
			if (_Noticed) return;

			Vector3 distanceVector = GetPlayerDistanceVector();
			if (distanceVector.magnitude < 10f)
			{
				_Noticed = true;
			}
		}

		void Move()
		{
			if (!_Noticed) return;

			bool move = GetPlayerDistanceVector().magnitude > 2f;
			Vector3 first = _Agent.path.corners[0];
			if (first != Vector3.zero)
			{
				Vector3 dir = first - transform.position;
				c._AimDestRotation = Quaternion.LookRotation(dir);
				c._MoveInput = move ? Vector3.forward : Vector3.zero;
			}
			else if (_Agent.path.corners.Length > 1)
			{
				Vector3 second = _Agent.path.corners[1];
				Vector3 dir = second - transform.position;
				c._AimDestRotation = Quaternion.LookRotation(dir);
				c._MoveInput = move ? Vector3.forward : Vector3.zero;
			}

			Vector3 playerPos = Controller._ActivePlayer.transform.position;
			_Agent.SetDestination(playerPos);
		}

		void Attack()
		{
			if (!_Noticed) return;
			if (Time.time - _LastAttackTime < 1f) return;

			Vector3 distanceVector = GetPlayerDistanceVector();
			if (distanceVector.magnitude < 2f)
			{
				c.NormalAttack(default);
				_LastAttackTime = Time.time;
			}
		}

		float _LastSummonTime;
		public GameObject _SummonParticlePrefab;
		public string _SummonEnemyName;
		public SphereCollider _SummonArea;
		void SummonOnAir()
		{
			if (string.IsNullOrEmpty(_SummonEnemyName)) return;
			if (!_Noticed) return;
			if (Time.time - _LastSummonTime < 1f) return;

			Controller.PlayEffect123123(_SummonParticlePrefab, c, transform.position, transform.rotation);
			Vector3 pos = transform.position;
			Controller.SpawnEnemy(_SummonEnemyName, pos, transform.rotation);
			_LastSummonTime = Time.time;
		}

		//Vector3 GetRandomSummonPosition()
		//{
		//	Vector3 center = _SummonArea.transform.position;
		//	float radius = _SummonArea.radius * _SummonArea.transform.localScale.x;
			
		//	Vector3 randomPos;
		//	int maxAttempts = 10;
		//	int attempts = 0;
			
		//	do
		//	{
		//		// 반구 위의 무작위 지점 생성 (위쪽만)
		//		float phi = Random.Range(0f, Mathf.PI * 0.5f);
		//		float theta = Random.Range(0f, Mathf.PI * 2f);
				
		//		float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
		//		float y = radius * Mathf.Cos(phi);
		//		float z = radius * Mathf.Sin(phi) * Mathf.Sin(theta);
				
		//		randomPos = center + new Vector3(x, y, z);
		//		attempts++;
				
		//	} while (Physics.OverlapSphere(randomPos, 0.5f).Length > 0 && attempts < maxAttempts);
			
		//	return randomPos;
		//}

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
