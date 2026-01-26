using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class AttackCollider : MonoBehaviour
	{
		public BoxCollider _Collider;
		public GameObject _HitEffectPrefab;
		public float _HitDelay;
	}
}
