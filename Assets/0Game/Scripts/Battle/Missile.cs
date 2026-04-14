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

		[BoxGroup("설정")] public float _Duration;
		[BoxGroup("설정")] public float _Gravity;
		[BoxGroup("설정")] public float _MoveSpeed;
		[BoxGroup("설정")] public AnimationCurve _MoveSpeedCurve;
		[BoxGroup("설정")] public float _GuideRotationSpeed;
		[BoxGroup("설정")] public AnimationCurve _GuideRotationSpeedCurve;

		[HideInInspector] public BattleAttack _Attack;
		[HideInInspector] public AttackHit _AttackHit;
		[HideInInspector] public Character _Target;
		[HideInInspector] public Rigidbody _Rigidbody;
		RaycastHit[] _HitResults;
		HashSet<GameObject> _AlreadyTargets;
		Bomb _Bomb;
		float _FireTime;

		BattleController Controller => BattleController.Instance;

		public void Init()
		{
			_Rigidbody = GetComponent<Rigidbody>();
			_Bomb = GetComponent<Bomb>();
			_HitResults = new RaycastHit[30];
			_AlreadyTargets = new();

			_FireTime = Time.time;

			if (_Bomb)
			{
				_Bomb.Init();
			}
		}

		void FixedUpdate()
		{
			// 이동
			float elapsed = Time.time - _FireTime;
			Vector3 velocity = _MoveSpeed * _MoveSpeedCurve.Evaluate(elapsed) * transform.forward;
			velocity.y += Physics.gravity.y * _Gravity * Time.fixedDeltaTime;
			_Rigidbody.linearVelocity = velocity;
			if (_Target)
			{
				Quaternion destRotation = Quaternion.LookRotation(_Target.transform.position - transform.position);
				float delta = _GuideRotationSpeed * _GuideRotationSpeedCurve.Evaluate(elapsed) * Time.fixedDeltaTime;
				_Rigidbody.rotation = Quaternion.RotateTowards(_Rigidbody.rotation, destRotation, delta);
			}

			// 히트 판정
			Vector3 deltaPosition = _Rigidbody.linearVelocity * Time.fixedDeltaTime;
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
					Character target = hit.collider.GetComponentInParent<Character>();
					target.TakeDamage(_Attack._Owner, _Attack, _AttackHit, hit.point, transform.forward);
					_AlreadyTargets.Add(target.gameObject);
				}

				_Attack._Owner.DestroyMissile(this);
				break;
			}
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

				Character target = col.GetComponentInParent<Character>();
				Vector3 direction = (target.transform.position - hitPoint).normalized;
				target.TakeDamage(_Attack._Owner, _Attack, _AttackHit, null, direction);
				_AlreadyTargets.Add(target.gameObject);
			}

			Controller.PlayEffect123123(_Bomb._ParticlePrefab.gameObject, _Attack._Owner, transform.position, Quaternion.identity);
		}
	}
}
