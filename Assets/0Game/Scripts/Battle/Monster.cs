using Animancer;
using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using VRM;
using static DataManager;
using static SingletonManager;

namespace Battle
{
	public partial class Monster : MonoBehaviour
	{
		Character c;
		NavMeshAgent _Agent;

		BattleController Controller => BattleController.Instance;

		void Start()
		{
			c = GetComponent<Character>();
			_Agent = GetComponent<NavMeshAgent>();
			_Agent.isStopped = true;

			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				while (true)
				{
					yield return new WaitForSeconds(1f);
					c.NormalAttack(default);
				}
			}
		}

		void Update()
		{
			_Agent.SetDestination(Controller._Player.transform.position);
			
		}
	}
}
