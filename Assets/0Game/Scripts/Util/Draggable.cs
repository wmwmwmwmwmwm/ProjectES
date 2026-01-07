using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public bool _DisableColliderWhenDrag;

	public Action<GameObject, PointerEventData> ClickCallback;
	public Action<GameObject, PointerEventData> BeginDragCallback;
	public Action<GameObject, PointerEventData> DragCallback;
	public Action<GameObject, GameObject, PointerEventData> EndDragCallback;

	//float DragStartTime;
	GameObject _DraggingObject;

	public void OnPointerClick(PointerEventData eventData)
	{
		ClickCallback?.Invoke(eventData.pointerCurrentRaycast.gameObject, eventData);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		//DragStartTime = Time.time;
		_DraggingObject = eventData.pointerPress;
		if (_DisableColliderWhenDrag)
        {
            _DraggingObject.GetComponent<Collider2D>().enabled = false;
        }
		BeginDragCallback?.Invoke(_DraggingObject, eventData);
	}

	public void OnDrag(PointerEventData eventData)
	{
		DragCallback?.Invoke(_DraggingObject, eventData);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (_DisableColliderWhenDrag)
		{
            _DraggingObject.GetComponent<Collider2D>().enabled = true;
        }
        _DraggingObject = null;
		GameObject dropPlace = eventData.pointerEnter;
		EndDragCallback?.Invoke(_DraggingObject, dropPlace, eventData);
	}
}
