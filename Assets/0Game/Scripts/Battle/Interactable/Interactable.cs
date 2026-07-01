using UnityEngine;

namespace Battle
{
	public abstract class Interactable : MonoBehaviour
	{
		protected BattleController Controller => BattleController.Instance;
	}
}
