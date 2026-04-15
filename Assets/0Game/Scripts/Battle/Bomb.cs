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
		public ParticleSystem _ParticlePrefab;

		[BoxGroup("설정")] public bool _HasDuration;
		[BoxGroup("설정"), ShowIf("_HasDuration")] public float _ExplodeDuration;
		[BoxGroup("설정"), ShowIf("_HasDuration")] public float _DamageInterval;

		[HideInInspector] public bool _Explode;
		[HideInInspector] public float _ExplodeTime;
		[HideInInspector] public Collider[] _HitResults;

		public void Init()
		{
			_HitResults = new Collider[100];
		}
	}
}
