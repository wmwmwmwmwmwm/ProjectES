using UnityEngine;
using System;

namespace Battle
{
	public class Item_Blue : DropItem
	{
		public override void Obtain()
		{
			Controller.ObtainItem(this);
		}
	}
}
