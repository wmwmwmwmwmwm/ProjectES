using Animancer;
using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static DataManager;
using static PlasticGui.WorkspaceWindow.Merge.MergeInProgress;
using static SingletonManager;

public class EffectEditorController : MonoBehaviour
{
	[Header("UI")]
	public Button _OpenAnimationButton;
	public Button _OpenEffectButton;
	public Button _SaveButton;
	public Toggle _IsLocalToggle;
	public TMP_Text _LogText;

	[Header("애셋")]
	public DataManager _DataManagerPrefab;

	[Header("오브젝트")]
	public GameObject _Character;

	SoloAnimation _SoloAnimation;
	AnimationClip _AnimationClip;
	GameObject _EffectPrefab;
	ParticleSystem _Effect;

	bool Active => _SoloAnimation.Clip && _Effect;

	void Start()
	{
		_SoloAnimation = _Character.GetComponent<SoloAnimation>();
		_OpenAnimationButton.onClick.AddListener(OpenAnimationButton);
		_OpenEffectButton.onClick.AddListener(OpenEffectButton);
		_SaveButton.onClick.AddListener(SaveButton);
		_IsLocalToggle.onValueChanged.AddListener(IsLocalToggle);
		RefreshUI();
	}

	void Update()
	{
		if (!Active) return;

		if (!_SoloAnimation.IsPlaying || _SoloAnimation.NormalizedTime >= 1f)
		{
			_SoloAnimation.Play();
			_SoloAnimation.Time = 0f;
			_Character.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			if (_Effect)
			{
				_Effect.Play(true);
			}
		}
	}

	void OpenAnimationButton()
	{
		string filePath = EditorUtility.OpenFilePanelWithFilters("애니메이션 선택", "", new string[] { "", "anim" });
		if (!File.Exists(filePath)) return;

		// 애니메이션 설정
		filePath = Path.GetRelativePath(Application.dataPath, filePath);
		_AnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/{filePath}");
		_SoloAnimation.Clip = _AnimationClip;

		// 기존 이펙트 초기화
		if (_Effect)
		{
			Destroy(_Effect.gameObject);
			_EffectPrefab = null;
		}

		// 이펙트 로드
		EffectInfo info = Data._EffectInfos.Find(x => x._Clip == _AnimationClip);
		if (info != null)
		{
			_Effect = Instantiate(info._EffectPrefab).GetComponent<ParticleSystem>();
			_EffectPrefab = info._EffectPrefab;
			Data.SetupEffect(_Effect.gameObject, info, _Character.transform);
			_IsLocalToggle.SetIsOnWithoutNotify(info._IsLocal);
		}

		RefreshUI();
	}

	void OpenEffectButton()
	{
		string filePath = EditorUtility.OpenFilePanelWithFilters("이펙트 프리팹 선택", "", new string[] { "", "prefab" });
		if (!File.Exists(filePath)) return;

		filePath = Path.GetRelativePath(Application.dataPath, filePath);
		_EffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/{filePath}");
		if (_Effect)
		{
			Destroy(_Effect.gameObject);
		}
		if (!_EffectPrefab.GetComponent<ParticleSystem>())
		{
			Debug.LogError("프리팹에 ParticleSystem이 없음");
			return;
		}

		_Effect = Instantiate(_EffectPrefab).GetComponent<ParticleSystem>();
		_Effect.transform.SetParent(_Character.transform);
		_Effect.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		_IsLocalToggle.SetIsOnWithoutNotify(true);

		RefreshUI();
	}

	void SaveButton()
	{
		EffectInfo info = new()
		{
			_Clip = _AnimationClip,
			_EffectPrefab = _EffectPrefab,
			_Pos = _Effect.transform.localPosition,
			_Rot = _Effect.transform.localEulerAngles,
			_Scale = _Effect.transform.localScale.x,
			_IsLocal = _IsLocalToggle.isOn,
		};
		int removed = _DataManagerPrefab._EffectInfos.RemoveAll(x => x._Clip == _AnimationClip);
		_DataManagerPrefab._EffectInfos.Add(info);
		EditorUtility.SetDirty(_DataManagerPrefab);
		AssetDatabase.SaveAssets();

		string text = removed > 0 ? "덮어쓰기 저장" : "새로 저장";
		Debug.Log($"[{text}] Clip : {info._Clip}   Position : {info._Pos}   Rotation : {info._Rot}   Rotation : {info._Scale}");
	}

	void IsLocalToggle(bool on)
	{
		Transform tr = on ? _Character.transform : null;
		_Effect.transform.SetParent(tr);
	}

	void RefreshUI()
	{
		_OpenEffectButton.gameObject.SetActive(_AnimationClip);
		_SaveButton.gameObject.SetActive(Active);
		_IsLocalToggle.gameObject.SetActive(Active);
		string clipName = _AnimationClip ? _AnimationClip.name : "None";
		string effectName = _EffectPrefab ? _EffectPrefab.name : "None";
		_LogText.text = $"Animation : {clipName}\nEffect : {effectName}";
	}
}
