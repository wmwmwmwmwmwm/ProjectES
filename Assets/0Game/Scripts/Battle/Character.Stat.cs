using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using static SingletonManager;

namespace Battle
{
	public partial class Character
	{
		public float _MaxHP;

		[HideInInspector] public float _HP;
	}
}
