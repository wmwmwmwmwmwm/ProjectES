using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class MeleeAttack : MonoBehaviour
	{
		public BoxCollider _Collider;
		public List<AttackHit> _AttackHits;

		[HideInInspector] public Character _Owner;
		[HideInInspector] public State _StateInfo;
		[HideInInspector] public Collider[] _HitResults;

		void Awake()
		{
			_HitResults = new Collider[30];
		}
	}
}
