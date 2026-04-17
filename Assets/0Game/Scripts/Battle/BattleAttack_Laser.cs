using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class BattleAttack_Laser : MonoBehaviour
	{
		public GameObject _Wait, _Impact;

		[BoxGroup("설정")] public float _Delay;
		[BoxGroup("설정")] public float _Duration;
		[BoxGroup("설정")] public float _Width;
		[BoxGroup("설정")] public float _Length;
		[BoxGroup("설정")] public float _DamageInterval;

		bool _GiveDamage;
		BattleAttack _Attack;
		AttackHit _AttackHit;
		LineRenderer _WaitLine, _ImpactLine;
		Material _WaitMaterial, _ImpactMaterial;
		GameObject _HitEffect;
		Dictionary<GameObject, float> _HitTargets;

		public void Init()
		{
			_Attack = GetComponent<BattleAttack>();
			_AttackHit = GetComponent<AttackHit>();
			_WaitLine = _Wait.GetComponentInChildren<LineRenderer>();
			_ImpactLine = _Impact.GetComponentInChildren<LineRenderer>();
			_WaitMaterial = _WaitLine.material;
			_ImpactMaterial = _ImpactLine.material;
			_HitEffect = _ImpactLine.transform.Find("Hit").gameObject;
			_HitTargets = new();
		}

		public void ImpactOn(bool on)
		{
			_GiveDamage = on;
			_Wait.SetActive(!on);
			_Impact.SetActive(on);
		}

		void Update()
		{
			LineRenderer line = !_GiveDamage ? _WaitLine : _ImpactLine;
			Material lineMaterial = !_GiveDamage ? _WaitMaterial : _ImpactMaterial;
			float radius = _Width / 2f;
			bool collided = Physics.SphereCast(
					origin: transform.position - radius * transform.forward,
					radius: radius,
					direction: transform.forward,
					hitInfo: out RaycastHit hitInfo,
					maxDistance: _Length);
			float scale = collided ? hitInfo.distance : _Length;
			line.SetPosition(1, new(0f, 0f, scale));
			lineMaterial.SetTextureScale("_MainTex", new Vector2(scale, 1f));
			lineMaterial.SetTextureScale("_Noise", new Vector2(scale, 1f));
			_HitEffect.SetActive(collided);
			if (collided)
			{
				_HitEffect.transform.position = hitInfo.point;
				_HitEffect.transform.LookAt(hitInfo.point + hitInfo.normal);
			}

			// 데미지 주기
			if (!_GiveDamage || !hitInfo.collider) return;
			Character targetCharacter = hitInfo.collider.GetComponentInParent<Character>();
			if (targetCharacter && _Attack._Owner.IsOpposite(targetCharacter))
			{
				if (_HitTargets.TryGetValue(targetCharacter.gameObject, out float damageTime)
					&& Time.time - damageTime <= _DamageInterval) return;

				targetCharacter.TakeDamage(_Attack, _AttackHit, hitInfo.point, transform.forward);
				_HitTargets[targetCharacter.gameObject] = Time.time;
			}
		}
	}
}
