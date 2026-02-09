using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace Battle
{
	public partial class BattleEffect : MonoBehaviour
	{
		[ReadOnly] public Character _Owner;
		[ReadOnly] public ParticleSystem _Particle;
		[ReadOnly] public CinemachineImpulseSource _ImpulseSource;

		float _Speed;

		public void Init()
		{
			_Particle = GetComponent<ParticleSystem>();
			_ImpulseSource = GetComponent<CinemachineImpulseSource>();
			_Speed = _Particle.main.simulationSpeed;

			if (_ImpulseSource)
			{
				_ImpulseSource.GenerateImpulse();
			}
		}

		void Update()
		{
			// 경직
			ParticleSystem.MainModule main = _Particle.main;
			main.simulationSpeed = _Owner.IsHitStun() ? _Speed * 0f : _Speed;
		}
	}
}
