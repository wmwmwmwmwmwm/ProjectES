using UnityEngine;

public class SingletonManager : MonoBehaviour
{
	public GameManager _GameControllerPrefab;
	public InputManager _InputManagerPrefab;
	public UIManager _UIManagerPrefab;
	public DataManager _DataManagerPrefab;

	public static GameManager Game => GameManager.Instance;
	public static InputManager Inputs => InputManager.Instance;
	public static UIManager UI => UIManager.Instance;
	public static DataManager Data => DataManager.Instance;

	bool _Init;

	void Awake()
	{
		LoadManager();
	}

	public void LoadManager()
	{
		if (_Init) return;

		InputManager.CreateInstance(_InputManagerPrefab.gameObject);
		GameManager.CreateInstance(_GameControllerPrefab.gameObject);
		UIManager.CreateInstance(_UIManagerPrefab.gameObject);
		DataManager.CreateInstance(_DataManagerPrefab.gameObject);
		_Init = true;
	}
}
