using Animancer;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class BattleAttack_Range : MonoBehaviour
	{
		public Missile _MissilePrefab;
		public BoxCollider _SpawnArea;

		[BoxGroup("설정")] public float _SpreadDegree;
		[BoxGroup("설정")] public int _FireCount;
		[BoxGroup("설정")] public float _FireDuration;
	}
}
