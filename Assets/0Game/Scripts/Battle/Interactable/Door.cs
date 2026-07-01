using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public class Door : Interactable
	{
		public enum DoorType { Grey, Blue, Red }
		public DoorType _Type;
	}
}
