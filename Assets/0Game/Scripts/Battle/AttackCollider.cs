using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class AttackCollider : MonoBehaviour
	{
		public BoxCollider _Collider;

		[HideInInspector] public Character _Owner;
		[HideInInspector] public State _StateInfo;
		[HideInInspector] public bool _AlreadyWallJump;
	}
}
