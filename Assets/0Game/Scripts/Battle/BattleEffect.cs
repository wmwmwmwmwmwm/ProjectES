using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
	public partial class BattleEffect : MonoBehaviour
	{
		[ReadOnly] public Character _Owner;
		[ReadOnly] public ParticleSystem _Particle;

		float _Speed;

		public void Init()
		{
			_Particle = GetComponent<ParticleSystem>();
			_Speed = _Particle.main.simulationSpeed;
		}

		void Update()
		{
			// 경직
			ParticleSystem.MainModule main = _Particle.main;
			main.simulationSpeed = _Owner.IsHitStun() ? _Speed * 0f : _Speed;
		}
	}
}
