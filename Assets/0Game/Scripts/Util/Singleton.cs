using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	public static T Instance;
	public static void CreateInstance(GameObject prefab)
	{
		if (Instance == null)
		{
			GameObject obj = Instantiate(prefab);
            obj.GetComponent<Singleton<T>>().Init();
			Instance = obj.GetComponent<T>();
            DontDestroyOnLoad(obj);
		}
	}

	protected abstract void Init();
}
