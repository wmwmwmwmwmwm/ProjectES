using UnityEngine;

public class SingletonManager : MonoBehaviour
{
	public GameManager _GameControllerPrefab;
	public InputManager _InputManagerPrefab;
	public UIManager _UIManagerPrefab;
	public DataManager _DataManagerPrefab;
	public StoryManager _StoryManagerPrefab;

	public static GameManager Game => GameManager.Instance;
	public static InputManager Inputs => InputManager.Instance;
	public static UIManager UI => UIManager.Instance;
	public static DataManager Data => DataManager.Instance;
	public static StoryManager Story => StoryManager.Instance;

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
		StoryManager.CreateInstance(_StoryManagerPrefab.gameObject);
		_Init = true;
	}
}
