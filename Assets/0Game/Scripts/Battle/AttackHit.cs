using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class AttackHit : MonoBehaviour
	{
		public GameObject _HitEffectPrefab;
		public float _HitDelay;
		public float _DamageDuration, _HitStunDuration;
		public float _ForceForward, _ForceUp;
		public float _Damage;
		public float _ShakeCameraDuration;
	}
}
