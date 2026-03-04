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

	string _CurrentScene;
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

	public void LoadBattleScene(string sceneName)
	{
		StartCoroutine(Internal());
		IEnumerator Internal()
		{
			SceneManager.LoadScene(SceneName.Loading, LoadSceneMode.Additive);
			AsyncOperation unloadProgress = SceneManager.UnloadSceneAsync(_CurrentScene);
			yield return new WaitUntil(() => unloadProgress.isDone);
            AsyncOperation loadProgress = SceneManager.LoadSceneAsync(SceneName.Battle, LoadSceneMode.Additive);
			AsyncOperation loadProgress2 = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
			yield return new WaitUntil(() => loadProgress.isDone);
			yield return new WaitUntil(() => loadProgress2.isDone);
            Scene scene = SceneManager.GetSceneByName(sceneName);
			SceneManager.SetActiveScene(scene);
		}
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
