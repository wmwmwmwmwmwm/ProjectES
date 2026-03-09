using DG.Tweening;
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

	public CanvasGroup _Fader;
	public void FadeIn(float time = 0.6f)
	{
		StartCoroutine(Internal());
		IEnumerator Internal()
		{
			Image image = _Fader.GetComponent<Image>();
			image.raycastTarget = true;
			_Fader.DOComplete();
			_Fader.alpha = 1f;
			yield return _Fader.DOFade(0f, time).SetEase(Ease.InQuad).WaitForCompletion();
			image.raycastTarget = false;
		}
	}

	public void FadeOut(float time = 1.2f)
	{
		StartCoroutine(Internal());
		IEnumerator Internal()
		{
			Image image = _Fader.GetComponent<Image>();
			image.raycastTarget = true;
			_Fader.DOComplete();
			_Fader.alpha = 0f;
			yield return _Fader.DOFade(1f, time).SetEase(Ease.OutQuad).WaitForCompletion();
			image.raycastTarget = false;
		}
	}
}
