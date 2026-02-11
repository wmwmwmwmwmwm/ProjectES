using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameManager : Singleton<GameManager>
{
	public int _FrameRate;
	public float _TimeScale;

	[Header("디버그")]
	public GameObject _DebugLine;
	public GameObject _DebugBox;

	float _FixedDeltaTime;

	protected override void Init()
	{
		_DebugLine.SetActive(false);
		_DebugBox.SetActive(false);
		_FixedDeltaTime = Time.fixedDeltaTime;
	}

	void Update()
	{
		Application.targetFrameRate = _FrameRate;
		Time.timeScale = _TimeScale;
		Time.fixedDeltaTime = Mathf.Min(Time.deltaTime, _FixedDeltaTime);
	}

	public void LockCursor(bool _lock)
	{
		Cursor.lockState = _lock ? CursorLockMode.Locked : CursorLockMode.None;
		Cursor.visible = !_lock;
	}

	public void DrawDebugLine(Vector3 from, Vector3 to, float width = 1f)
	{
		StartCoroutine(Internal());
		IEnumerator Internal()
		{
			GameObject cube = Instantiate(_DebugLine);
			cube.transform.position = from;
			cube.transform.LookAt(to);
			Vector3 v = to - from;
			cube.transform.localScale = new(width, width, v.magnitude);
			cube.SetActive(true);
			yield return new WaitForSeconds(5f);
			Destroy(cube);
		}
	}

	public void DrawDebugBox(Vector3 position, Quaternion rotation, float size = 1f)
	{
		StartCoroutine(Internal());
		IEnumerator Internal()
		{
			GameObject box = Instantiate(_DebugBox);
			box.transform.SetPositionAndRotation(position, rotation);
			box.transform.localScale = size * Vector3.one;
			box.SetActive(true);
			yield return new WaitForSeconds(5f);
			Destroy(box);
		}
	}
}
