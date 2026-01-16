using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;
using static SingletonManager;

namespace Battle
{
	public class BattleController : MonoBehaviour
	{
		public Transform _MainCamera, _BackgroundCamera;

		void Update()
		{
			// 배경 카메라
			_BackgroundCamera.rotation = _MainCamera.rotation;
		}
	}
}
