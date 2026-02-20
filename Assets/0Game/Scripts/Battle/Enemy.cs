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
		public Slider _HPSlider;
		public Slider _HPSlider_Inner;
		public Transform _HPSliderPosition;

		Character c;
		NavMeshAgent _Agent;

		bool _Noticed;
		float _LastAttackTime;
		Collider[] _ColliderHits;

		BattleController Controller => BattleController.Instance;

		void Awake()
		{
			c = GetComponent<Character>();
			_Agent = GetComponent<NavMeshAgent>();
			_Agent.updatePosition = false;
			_Agent.updateRotation = false;
			_Agent.updateUpAxis = false;
			_LastAttackTime = Const.TimeDefault;
			_ColliderHits = new Collider[30];
		}

		void Update()
		{
			// AI
			Idle();
			Move();
			Attack();

            // UI
            Camera camera = Controller._MainCamera.GetComponent<Camera>();
			_HPSlider.transform.localPosition = Controller._Canvas.WorldToCanvas(camera, _HPSliderPosition.position);
            float cameraDistance = (transform.position - Controller._MainCamera.position).magnitude;
			_HPSlider.transform.localScale = Controller._HPSliderScaleCurve.Evaluate(cameraDistance) * Vector3.one;
		}

		public void NoticeAround()
		{
			int count = Physics.OverlapSphereNonAlloc(position: transform.position,
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

			Vector3 playerPos = Controller._Player.transform.position;
			_Agent.SetDestination(playerPos);
		}

		void Attack()
		{
			if (!_Noticed) return;
			if (Time.time - _LastAttackTime < 2f) return;

			Vector3 distanceVector = GetPlayerDistanceVector();
			if (distanceVector.magnitude < 2f)
			{
				c.NormalAttack(default);
				_LastAttackTime = Time.time;
			}
		}

		Vector3 GetPlayerDistanceVector()
		{
			Vector3 playerPos = Controller._Player.transform.position;
			return transform.position - playerPos;
		}

		public void SetHPSliderValue(float value)
		{
			_HPSlider.gameObject.SetActive(value > 0f && value < 1f);
			_HPSlider.value = value;
			_HPSlider_Inner.DOComplete();
			_HPSlider_Inner.DOValue(value, 0.3f).SetEase(Ease.Linear).SetSpeedBased(true);
		}
	}
}
