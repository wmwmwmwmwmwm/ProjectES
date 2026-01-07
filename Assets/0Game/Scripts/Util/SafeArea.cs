using UnityEngine;

public class SafeArea : MonoBehaviour
{
	RectTransform rectTransform;
	Rect safeArea;
	Vector2 bottomLeftPoint;
	Vector2 topRightPoint;

	void Start()
	{
		rectTransform = GetComponent<RectTransform>();
		safeArea = Screen.safeArea;
		bottomLeftPoint = safeArea.position;
		topRightPoint = bottomLeftPoint + safeArea.size;

		bottomLeftPoint.x /= Screen.width;
		bottomLeftPoint.y /= Screen.height;
		topRightPoint.x /= Screen.width;
		topRightPoint.y /= Screen.height;

		Vector2 anchorMin = rectTransform.anchorMin;
		Vector2 anchorMax = rectTransform.anchorMax;
		anchorMin.x = Mathf.Clamp(anchorMin.x, bottomLeftPoint.x, topRightPoint.x);
		anchorMin.y = Mathf.Clamp(anchorMin.y, bottomLeftPoint.y, topRightPoint.y);
		anchorMax.x = Mathf.Clamp(anchorMax.x, bottomLeftPoint.x, topRightPoint.x);
		anchorMax.y = Mathf.Clamp(anchorMax.y, bottomLeftPoint.y, topRightPoint.y);
		rectTransform.anchorMin = anchorMin;
		rectTransform.anchorMax = anchorMax;
	}
}