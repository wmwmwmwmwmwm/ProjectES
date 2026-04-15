using Animancer;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class BattleAttack_Missile : MonoBehaviour
	{
		public Missile _MissilePrefab;
		public List<Transform> _SpawnPoints;

		[BoxGroup("설정")] public float _SpreadDegree;
		[BoxGroup("설정")] public int _FireCount;
		[BoxGroup("설정")] public float _FireDuration;
	}
}
