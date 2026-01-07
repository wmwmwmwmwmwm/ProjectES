using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public Action<GameObject, PointerEventData> ClickCallback;
	public Action<GameObject, PointerEventData> BeginDragCallback;
	public Action<GameObject, PointerEventData> DragCallback;
	public Action<GameObject, GameObject, PointerEventData> EndDragCallback;

	//float DragStartTime;
	GameObject DraggingObject;

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left) return;

		ClickCallback?.Invoke(eventData.pointerPress, eventData);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left) return;

		//DragStartTime = Time.time;
		DraggingObject = eventData.pointerPress;
		DraggingObject.GetComponent<Graphic>().raycastTarget = false;
		BeginDragCallback?.Invoke(DraggingObject, eventData);
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left) return;

		DragCallback?.Invoke(DraggingObject, eventData);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left) return;

		DraggingObject.GetComponent<Graphic>().raycastTarget = true;
		DraggingObject = null;
		GameObject DropPlaceObject = eventData.pointerEnter;
		EndDragCallback?.Invoke(DraggingObject, DropPlaceObject, eventData);
	}
}
