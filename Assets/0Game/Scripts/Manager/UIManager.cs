using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
	public CanvasGroup _Fader;

	protected override void Init()
	{
	}

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
