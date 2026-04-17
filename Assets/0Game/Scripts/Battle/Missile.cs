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
		Dictionary<GameObject, float> _HitTargets;
		Bomb _Bomb;
		[HideInInspector] public float _FireTime;

		BattleController Controller => BattleController.Instance;

		public void Init()
		{
			_AttackHit = GetComponent<AttackHit>();
			_Rigidbody = GetComponent<Rigidbody>();
			_Bomb = GetComponent<Bomb>();
			_HitResults = new RaycastHit[30];
			_HitTargets = new();

			if (_Bomb)
			{
				_Bomb.Init();
			}
		}

		void FixedUpdate()
		{
			if (_Bomb && _Bomb.Exploded) return;

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
		}

		void Update()
		{
			// 파괴 판정
			float elapsed = Time.time - _FireTime;
			bool destroy = false;
			if (_Bomb && _Bomb.Exploded)
			{
				float elapsedExplode = Time.time - _Bomb._ExplodeTime;
				destroy &= elapsedExplode > _Bomb._ExplodeDuration;
			}
			else
			{
				destroy = elapsed > _Duration;
			}
			if (destroy)
			{
				_Attack._Owner.DestroyMissile(this);
				return;
			}

			CheckMissileCollision();
		}

		void CheckMissileCollision()
		{
			if (_Bomb && _Bomb.Exploded) return;

			// 히트 판정
			Vector3 deltaPosition = _Rigidbody.linearVelocity * Time.deltaTime;
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
				if (_HitTargets.ContainsKey(hit.collider.gameObject)) continue;

				// 폭탄 터짐
				if (_Bomb)
				{
					BombExplosion(hit.point);
					break;
				}

				// 지형에 충돌
				if (hit.collider.gameObject.layer == Layer.TerrainLayer)
				{
					Controller.PlayEffect123123(_AttackHit._HitEffectPrefab, _Attack._Owner, hit.point, Quaternion.LookRotation(transform.forward));
					_HitTargets.Add(hit.collider.gameObject, Time.time);
				}
				else
				{
					// 공격 적중
					Character target = hit.collider.GetComponentInParent<Character>();
					target.TakeDamage(_Attack, _AttackHit, hit.point, transform.forward);
					_HitTargets.Add(target.gameObject, Time.time);
				}
				_Attack._Owner.DestroyMissile(this);
			}
		}

		void BombExplosion(Vector3 hitPoint)
		{
			StartCoroutine(Interval());
			IEnumerator Interval()
			{
				_Bomb._ExplodeTime = Time.time;
				_Rigidbody.linearVelocity = Vector3.zero;
				_Bomb._MissileGraphic.SetActive(false);
				_Bomb._BombGraphic.SetActive(true);

				while (true)
				{
					// 적에게 데미지
					int count = Physics.OverlapSphereNonAlloc(
						position: transform.position,
						radius: _Bomb._AreaCollider.radius,
						results: _Bomb._HitResults,
						layerMask: _Attack._Owner.GetOppositeLayerMask());
					for (int i = 0; i < count; i++)
					{
						Collider col = _Bomb._HitResults[i];
						if (_HitTargets.TryGetValue(col.gameObject, out float damagedTime))
						{
							if (Time.time - damagedTime < _Bomb._DamageInterval) continue;
						}

						Character target = col.GetComponentInParent<Character>();
						Vector3 direction = (target.transform.position - hitPoint).normalized;
						target.TakeDamage(_Attack, _AttackHit, null, direction);
						_HitTargets[target.gameObject] = Time.time;
					}
					if (!_Bomb._HasDuration) break;

					yield return new WaitForSeconds(0.1f);
				}
			}
		}
	}
}
