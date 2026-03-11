using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using static SingletonManager;

namespace Battle
{
	public partial class Character
	{
		[HideInInspector] public float _HP;

		void SetHP(float hp)
		{
			_HP = hp;

			if (_Enemy)
			{
				float percent = _HP / _MaxHP;
				_Enemy.SetHPSliderValue(percent);
			}
		}
	}
}
