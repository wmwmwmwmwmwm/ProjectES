using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class BattleAttack : MonoBehaviour
	{
		[HideInInspector] public Character _Owner;
		[HideInInspector] public State _StateInfo;
	}
}
