using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverUI : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
	public Action<GameObject, PointerEventData> HoverEnterCallback;
	public Action<GameObject, PointerEventData> HoverCallback;
	public Action<GameObject, PointerEventData> HoverExitCallback;

	public void OnPointerEnter(PointerEventData eventData)
	{
		HoverEnterCallback?.Invoke(gameObject, eventData);
	}

	public void OnPointerMove(PointerEventData eventData)
	{
		HoverCallback?.Invoke(gameObject, eventData);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		HoverExitCallback?.Invoke(gameObject, eventData);
	}
}
