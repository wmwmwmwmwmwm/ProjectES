using UnityEngine;
using System;

namespace Battle
{
	public class Item_Red : DropItem
	{
		public override void Obtain()
		{
			Controller.ObtainItem(this);
		}
	}
}
