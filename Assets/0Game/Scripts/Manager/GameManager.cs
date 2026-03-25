using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
	public int _FrameRate;
	public float _TimeScale;

	[Header("디버그")]
	public GameObject _DebugLine;
	public GameObject _DebugBox;

	[HideInInspector] public string _StartScene;
	[HideInInspector] public string _CurrentScene;
	float _FixedDeltaTime;

	protected override void Init()
	{
		_DebugLine.SetActive(false);
		_DebugBox.SetActive(false);
		_FixedDeltaTime = Time.fixedDeltaTime;
		_StartScene = SceneManager.GetActiveScene().name;
	}

	void OnDestroy()
	{
		LockCursor(false);
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

	public void LoadTitleScene()
	{
		SceneManager.LoadScene(SceneName.Title);
		_CurrentScene = SceneName.Title;
	}

	public void LoadBattleScene(string sceneName)
	{
		StartCoroutine(Internal());
		IEnumerator Internal()
		{
			SceneManager.LoadScene(SceneName.Loading, LoadSceneMode.Additive);
			yield return new WaitUntil(() => SceneManager.GetSceneByName(SceneName.Loading).isLoaded);
			AsyncOperation unloadProgress = SceneManager.UnloadSceneAsync(_CurrentScene);
			AsyncOperation loadProgress = SceneManager.LoadSceneAsync(SceneName.Battle, LoadSceneMode.Additive);
			AsyncOperation loadProgress2 = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
			while (true)
			{
				float progress = unloadProgress.progress;
				progress += loadProgress.progress;
				progress += loadProgress2.progress;
				progress /= 3f;
				LoadingController.Instance._Progress.value = progress;
				LoadingController.Instance._Text.text = progress.ToPercentString();

				if (unloadProgress.isDone && loadProgress.isDone && loadProgress2.isDone) break;
				yield return null;
			}
            Scene scene = SceneManager.GetSceneByName(sceneName);
			SceneManager.SetActiveScene(scene);
			SceneManager.UnloadSceneAsync(SceneName.Loading);
			_CurrentScene = sceneName;
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

	public void DrawDebugBox(Vector3 position, Quaternion rotation = default, float size = 1f)
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
