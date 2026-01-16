using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameManager : Singleton<GameManager>
{
	float _FixedDeltaTime;

	protected override void Init()
	{
		_FixedDeltaTime = Time.fixedDeltaTime;
	}

	void Update()
	{
		Time.fixedDeltaTime = Mathf.Min(Time.deltaTime, _FixedDeltaTime);
	}
}
