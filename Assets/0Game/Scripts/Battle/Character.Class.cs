using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using static SingletonManager;
using System;
using Animancer;

namespace Battle
{
	public partial class Character
	{
		[Serializable]
		public class Effect
		{
			public TransitionAsset _Transition;
			public GameObject _EffectPrefab;
			//public AssetReferenceT<GameObject> _Prefab;
			public Vector3 _Pos;
			public Vector3 _Rot;
			public float _Scale;
			public float _Delay;
			public bool _IsLocal;
		}
	}
}
