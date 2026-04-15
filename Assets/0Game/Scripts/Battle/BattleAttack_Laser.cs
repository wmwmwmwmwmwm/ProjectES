using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class BattleAttack_Laser : MonoBehaviour
	{
		public CapsuleCollider _DamageArea;
		public Transform _Parent;
		public GameObject _Wait, _Impact;

		[BoxGroup("설정")] public float _Delay;
		[BoxGroup("설정")] public float _Duration;
		[BoxGroup("설정")] public float _Width;
		[BoxGroup("설정")] public float _Length;

		[HideInInspector] public Material _WaitMaterial, _ImpactMaterial;
		[HideInInspector] public RaycastHit[] _HitResults;
		[HideInInspector] public HashSet<GameObject> _AlreadyTargets;

		public void Init()
		{
			_WaitMaterial = _Wait.GetComponentInChildren<LineRenderer>().material;
			_ImpactMaterial = _Impact.GetComponentInChildren<LineRenderer>().material;
			_HitResults = new RaycastHit[30];
			_AlreadyTargets = new();
		}
	}
}
