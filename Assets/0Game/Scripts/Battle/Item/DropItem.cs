using UnityEngine;

namespace Battle
{
	public abstract class DropItem : MonoBehaviour
	{
		protected BattleController Controller => BattleController.Instance;
	}
}
