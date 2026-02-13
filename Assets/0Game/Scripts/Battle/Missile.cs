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
		[HideInInspector] public HashSet<Character> _AlreadyTargets;
		[HideInInspector] public bool _DestroyTrigger;

		void Awake()
		{
			_Attack = GetComponent<BattleAttack>();
			_Rigidbody = GetComponent<Rigidbody>();
			_HitResults = new RaycastHit[30];
			_AlreadyTargets = new();
		}

		void FixedUpdate()
		{
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

				// 지형에 충돌
				if (hit.collider.gameObject.layer == Layer.TerrainLayer)
				{
					_Attack._Owner.PlayEffect123123(_AttackHit._HitEffectPrefab, _Attack._Owner, hit.point, Quaternion.LookRotation(transform.forward));
					_DestroyTrigger = true;
					continue;
				}

				// 공격 적중
				Character target = hit.collider.GetComponent<Character>();
                if (!_AlreadyTargets.Contains(target))
                {
                    target.TakeDamage(_Attack._Owner, _Attack, _AttackHit, hit.point, transform.forward);
                    _AlreadyTargets.Add(target);
					_DestroyTrigger = true;
				}
			}

            // 이동
            _Rigidbody.MovePosition(transform.position + deltaPosition);
		}
	}
}
