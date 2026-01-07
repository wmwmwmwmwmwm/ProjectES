using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using static SingletonManager;

public static partial class Util
{
	public static void TextColorTween(this TMP_Text g, float duration = 0.8f, bool reverse = false)
	{
		g.DOComplete();
		Color original = g.color;
		g.color = new Color(0.2f, 0.2f, 0.2f, 1f);
		TweenerCore<Color, Color, ColorOptions> tween = g.DOColor(original, duration / 2f).SetEase(Ease.InQuad).SetDelay(duration / 2f);
		if (reverse)
		{
			tween.fullPosition = duration;
			tween.PlayBackwards();
		}
	}

	public static void PanelTween(this CanvasGroup g, Vector2 fromDir, bool reverse = false)
	{
		// 투명도
		g.DOComplete();
		float t = 0.6f;
		g.alpha = 0f;
		TweenerCore<float, float, FloatOptions> fadeTween = g.DOFade(1f, t).SetEase(Ease.InQuad);
		if (reverse)
		{
			fadeTween.fullPosition = t;
			fadeTween.PlayBackwards();
		}

		// 위치
		RectTransform rt = g.GetComponent<RectTransform>();
		rt.DOComplete();
		Vector2 dest = rt.anchoredPosition;
		rt.anchoredPosition += fromDir * rt.rect.size;
		TweenerCore<Vector2, Vector2, VectorOptions> posTween = rt.DOAnchorPos(dest, t).SetEase(Ease.OutCubic);
		if (reverse)
		{
			posTween.fullPosition = t;
			posTween.PlayBackwards();
		}

		// 텍스트
		foreach (TMP_Text text in rt.GetComponentsInChildren<TMP_Text>())
		{
			TextColorTween(text, reverse: reverse);
		}
	}
}