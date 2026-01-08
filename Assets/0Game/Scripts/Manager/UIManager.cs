using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
	public Image BlackOverlay;

	protected override void Init()
	{
	}

	public IEnumerator SceneTransition(string SceneName)
	{
		AsyncOperation LoadProgress = SceneManager.LoadSceneAsync(SceneName);
		LoadProgress.allowSceneActivation = false;
		while (LoadProgress.progress < 0.9f)
		{
			BlackOverlay.gameObject.SetActive(true);
			yield return new WaitForSeconds(0.2f);
		}
		LoadProgress.allowSceneActivation = true;
		BlackOverlay.gameObject.SetActive(false);
	}
}
