using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static SingletonManager;

public class TitleController : MonoBehaviour
{
	public Button _StartButton;

	void Start()
	{
		_StartButton.onClick.AddListener(StartButton);
	}

	void StartButton()
	{
		Game.LoadBattleScene(SceneName.Glacier);
	}
}
