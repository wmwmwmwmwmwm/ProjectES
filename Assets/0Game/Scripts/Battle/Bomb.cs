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
		
		[HideInInspector] public Collider[] _HitResults;

		public void Init()
		{
			_HitResults = new Collider[100];
		}
	}
}
