using UnityEngine;

public class SingletonManager : MonoBehaviour
{
	public InputManager _InputManagerPrefab;
	public GameManager _GameControllerPrefab;
	public UIManager _UIManagerPrefab;

	public static InputManager Inputs => InputManager.Instance;
	public static GameManager Game => GameManager.Instance;
	public static UIManager UI => UIManager.Instance;

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
		_Init = true;
	}
}
