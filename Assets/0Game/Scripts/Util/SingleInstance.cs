using UnityEngine;

public abstract class SingleInstance<T> : MonoBehaviour where T : MonoBehaviour
{
	public static T Instance;

	protected virtual void Awake()
	{
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this as T;
	}
}
