using UnityEngine;
using System;

namespace Battle
{
	public class Item_Grey : DropItem
	{
		public override void Obtain()
		{
			Controller.ObtainItem(this);
		}
	}
}
