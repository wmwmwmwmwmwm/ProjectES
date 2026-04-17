using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class Bomb : MonoBehaviour
	{
		public SphereCollider _AreaCollider;
		public GameObject _MissileGraphic, _BombGraphic;

		[BoxGroup("설정")] public bool _HasDuration;
		[BoxGroup("설정"), ShowIf("_HasDuration")] public float _ExplodeDuration;
		[BoxGroup("설정"), ShowIf("_HasDuration")] public float _DamageInterval;

		[HideInInspector] public float _ExplodeTime;
		[HideInInspector] public Collider[] _HitResults;

		public bool Exploded => _ExplodeTime > 0f;

		public void Init()
		{
			_HitResults = new Collider[100];
			_MissileGraphic.SetActive(true);
			_BombGraphic.SetActive(false);
			_ExplodeTime = Const.TimeDefault;
		}
	}
}
