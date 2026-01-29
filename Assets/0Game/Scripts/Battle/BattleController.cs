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
		public Transform _BgSky, _BgGround, _BgNear;

		void Start()
		{
			Game.LockCursor(true);
		}

		void Update()
		{
			// 배경 회전
			_BgNear.rotation = _MainCamera.rotation;
			_BgGround.eulerAngles = _BgGround.eulerAngles.WithX(_MainCamera.eulerAngles.x);
		}
	}
}
