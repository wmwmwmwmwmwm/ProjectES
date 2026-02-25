using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class Missile : MonoBehaviour
	{
		public SphereCollider _Collider;
		public AttackHit _AttackHit;

		public float _MoveSpeed;
		public float _Duration;

		[HideInInspector] public BattleAttack _Attack;
		[HideInInspector] public Rigidbody _Rigidbody;
		[HideInInspector] public RaycastHit[] _HitResults;
		[HideInInspector] public HashSet<GameObject> _AlreadyTargets;
		[HideInInspector] public bool _DestroyTrigger;
		[HideInInspector] public Bomb _Bomb;

		BattleController Controller => BattleController.Instance;

		void Awake()
		{
			_Attack = GetComponent<BattleAttack>();
			_Rigidbody = GetComponent<Rigidbody>();
			_Bomb = GetComponent<Bomb>();
			_HitResults = new RaycastHit[30];
			_AlreadyTargets = new();

			if (_Bomb)
			{
				_Bomb.Init();
			}
		}

		void FixedUpdate()
		{
			if (_DestroyTrigger) return;

			// 히트 판정
			Vector3 deltaPosition = _MoveSpeed * Time.fixedDeltaTime * transform.forward;
			int layerMask = _Attack._Owner.GetOppositeLayerMask();
			layerMask |= Layer.TerrainLayerMask;
			int count = Physics.SphereCastNonAlloc(
				origin: transform.position,
				radius: _Collider.radius,
				direction: transform.forward,
				results: _HitResults,
				maxDistance: deltaPosition.magnitude,
				layerMask: layerMask);
			for (int i = 0; i < count; i++)
			{
				RaycastHit hit = _HitResults[i];
				if (_AlreadyTargets.Contains(hit.collider.gameObject)) continue;

				// 지형에 충돌
				if (hit.collider.gameObject.layer == Layer.TerrainLayer)
				{
					if (_Bomb)
					{
						BombExplosion(hit.point);
					}
					else
					{
						Controller.PlayEffect123123(_AttackHit._HitEffectPrefab, _Attack._Owner, hit.point, Quaternion.LookRotation(transform.forward));
						_DestroyTrigger = true;
					}
					_AlreadyTargets.Add(hit.collider.gameObject);
					continue;
				}

				// 공격 적중
				if (_Bomb)
				{
					BombExplosion(hit.point);
				}
				else
				{
					Character target = hit.collider.GetComponent<Character>();
					target.TakeDamage(_Attack._Owner, _Attack, _AttackHit, hit.point, transform.forward);
					_AlreadyTargets.Add(target.gameObject);
					_DestroyTrigger = true;
				}

				if (_DestroyTrigger) break;
			}

			// 이동
			_Rigidbody.MovePosition(transform.position + deltaPosition);
		}

		void BombExplosion(Vector3 hitPoint)
		{
			// 적에게 데미지
			int layerMask = _Attack._Owner.GetOppositeLayerMask();
			int count = Physics.OverlapSphereNonAlloc(
				position: transform.position,
				radius: _Bomb._AreaCollider.radius,
				results: _Bomb._HitResults,
				layerMask: layerMask);
			for (int i = 0; i < count; i++)
			{
				Collider col = _Bomb._HitResults[i];
				if (_AlreadyTargets.Contains(col.gameObject)) continue;

				Character target = col.GetComponent<Character>();
				Vector3 direction = (target.transform.position - hitPoint).normalized;
				target.TakeDamage(_Attack._Owner, _Attack, _AttackHit, null, direction);
				_AlreadyTargets.Add(target.gameObject);
			}

			_DestroyTrigger = true;
			Controller.PlayEffect123123(_Bomb._ParticlePrefab.gameObject, _Attack._Owner, transform.position, Quaternion.identity);
		}
	}
}
