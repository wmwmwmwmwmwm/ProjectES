using UnityEngine;

public class Fragment : MonoBehaviour
{
	void Update()
	{
		if (transform.position.y < -1000f)
		{
			Destroy(gameObject);
		}
	}
}