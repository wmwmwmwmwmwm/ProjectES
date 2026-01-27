using Animancer;
using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRM;
using static DataManager;
using static SingletonManager;

namespace Battle
{
	public partial class Monster : MonoBehaviour
	{
		public int _HP;

		Character c;

		void Start()
		{
			c = GetComponent<Character>();
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				while (true)
				{
					c.NormalAttack(default);
					yield return new WaitForSeconds(1f);
				}
			}
		}

		void Update()
		{
		}
	}
}
