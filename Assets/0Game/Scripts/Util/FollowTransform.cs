using UnityEngine;

public class FollowTransform : MonoBehaviour 
{
	public Transform _Target;
	public bool _Position, _Rotation, _Scale;

	void Reset()
	{
		_Position = true; 
		_Rotation = true; 
		_Scale = true;
	}

	void Update()
	{
		if (_Position)
		{
			transform.position = _Target.position;
		}
		if (_Rotation)
		{
			transform.rotation = _Target.rotation;
		}
		if (_Scale)
		{
			transform.localScale = _Target.localScale;
		}
	}
}
