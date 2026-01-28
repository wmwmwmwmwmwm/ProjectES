using Animancer;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static DataManager;
using static SingletonManager;
using Object = UnityEngine.Object;

public class EffectEditorController : MonoBehaviour
{
	public Button _SaveButton;

	[Header("왼쪽 위")]
	public Button _OpenAnimationButton;
	public Button _OpenEffectButton;
	public Toggle _IsLocalToggle;
	public TMP_Text _LogText;
	public Slider _DelaySlider;

	[Header("왼쪽 아래")]
	public GameObject _BottomLeft;
	public Toggle _IsAttackToggle;
	public Button _OpenHitEffectButton;
	public TMP_Text _BottomLeftLogText;
	public Slider _HitDelaySlider;

	[Header("애셋")]
	public DataManager _DataManagerPrefab;

	[Header("오브젝트")]
	public GameObject _Character;

	SoloAnimation _SoloAnimation;
	AnimationClip _AnimationClip;
	GameObject _EffectPrefab;
	ParticleSystem _Effect;
	GameObject _HitEffectPrefab;
	ParticleSystem _HitEffect;
	float _LastPlayTime;
	bool _PlayEffectFlag, _PlayHitEffectFlag;

	bool Active => _AnimationClip && _Effect;

	void Start()
	{
		_SoloAnimation = _Character.GetComponent<SoloAnimation>();
		_SaveButton.onClick.AddListener(SaveButton);

		// 왼쪽 위
		_OpenAnimationButton.onClick.AddListener(OpenAnimationButton);
		_OpenEffectButton.onClick.AddListener(OpenEffectButton);
		_IsLocalToggle.onValueChanged.AddListener(IsLocalToggle);
		_DelaySlider.onValueChanged.AddListener(DelaySlider);

		// 왼쪽 아래
		_OpenHitEffectButton.onClick.AddListener(OpenHitEffectButton);
		_HitDelaySlider.onValueChanged.AddListener(HitDelaySlider);

		RefreshUI();
	}

    void Update()
	{
		if (!Active) return;

		if (!_SoloAnimation.IsPlaying || _SoloAnimation.NormalizedTime >= 1f)
		{
			_LastPlayTime = Time.time;
			_SoloAnimation.Play();
			_SoloAnimation.Time = 0f;
			_Character.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			_PlayEffectFlag = true;
			_PlayHitEffectFlag = true;
		}

		if (_Effect && _PlayEffectFlag && Time.time - _LastPlayTime > _DelaySlider.value)
		{
			_PlayEffectFlag = false;
			_Effect.Play(true);
		}

		if (_HitEffect && _PlayHitEffectFlag && Time.time - _LastPlayTime > _HitDelaySlider.value)
		{
			_PlayHitEffectFlag = false;
			_HitEffect.Play(true);
		}
	}

	void OpenAnimationButton()
	{
		string filePath = EditorUtility.OpenFilePanelWithFilters("애니메이션 선택", "", new string[] { "", "anim,fbx" });
		if (!File.Exists(filePath)) return;

		// 애니메이션 설정
		if (Path.GetExtension(filePath) == ".anim")
		{
			_AnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Util.ToAssetPath(filePath));
		}
		else
		{
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(Util.ToAssetPath(filePath));
			_AnimationClip = assets.First(x => x is AnimationClip) as AnimationClip;
		}
		_SoloAnimation.Clip = _AnimationClip;

		// 기존 이펙트 초기화
		if (_Effect)
		{
			Destroy(_Effect.gameObject);
			_EffectPrefab = null;
		}
		if (_HitEffect)
		{
			Destroy(_HitEffect.gameObject);
			_HitEffectPrefab = null;
		}

		// 이펙트 로드
		EffectInfo info = Data._EffectInfos.Find(x => x._Clip == _AnimationClip);
		if (info != null)
		{
			_EffectPrefab = info._EffectPrefab;
			_Effect = Instantiate(info._EffectPrefab).GetComponent<ParticleSystem>();
			Data.SetupEffectPosition(_Effect.gameObject, info, _Character.transform);
			_IsLocalToggle.SetIsOnWithoutNotify(info._IsLocal);
			_DelaySlider.value = info._Delay;
			bool isAttack = info._HitEffectPrefab;
			_HitDelaySlider.value = info._HitDelay;
			_IsAttackToggle.SetIsOnWithoutNotify(isAttack);
			if (isAttack)
			{
				_HitEffectPrefab = info._HitEffectPrefab;
				_HitEffect = Instantiate(info._HitEffectPrefab).GetComponent<ParticleSystem>();
				_HitEffect.transform.SetLocalPositionAndRotation(new(0f, 1.5f, 2f), Quaternion.identity);
			}
		}

		RefreshUI();
	}

	void OpenEffectButton()
	{
		string filePath = EditorUtility.OpenFilePanelWithFilters("이펙트 프리팹 선택", "", new string[] { "", "prefab" });
		if (!File.Exists(filePath)) return;

		_EffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Util.ToAssetPath(filePath));
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
			_Delay = _DelaySlider.value,
			_IsLocal = _IsLocalToggle.isOn,
			_HitEffectPrefab = _IsAttackToggle.isOn ? _HitEffectPrefab : null,
			_HitDelay = _HitDelaySlider.value,
			_DamageDuration = 0.3f,
		};
		int removed = _DataManagerPrefab._EffectInfos.RemoveAll(x => x._Clip == _AnimationClip);
		_DataManagerPrefab._EffectInfos.Add(info);
		EditorUtility.SetDirty(_DataManagerPrefab);
		AssetDatabase.SaveAssets();

		string text = removed > 0 ? "덮어쓰기 저장" : "새로 저장";
		Debug.Log($"[{text}] 애니메이션 : {info._Clip.name}   이펙트 : {info._EffectPrefab.name}   타격 이펙트 : {info._HitEffectPrefab.name}");
	}

	void IsLocalToggle(bool on)
	{
		Transform tr = on ? _Character.transform : null;
		_Effect.transform.SetParent(tr);
	}

	void DelaySlider(float v)
	{
		_DelaySlider.GetComponentInChildren<TMP_Text>().text = v.ToString("0.00");
	}

	void OpenHitEffectButton()
	{
		string filePath = EditorUtility.OpenFilePanelWithFilters("이펙트 프리팹 선택", "", new string[] { "", "prefab" });
		if (!File.Exists(filePath)) return;

		_HitEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Util.ToAssetPath(filePath));
		if (_HitEffect)
		{
			Destroy(_HitEffect.gameObject);
		}
		if (!_HitEffectPrefab.GetComponent<ParticleSystem>())
		{
			Debug.LogError("프리팹에 ParticleSystem이 없음");
			return;
		}

		_HitEffect = Instantiate(_HitEffectPrefab).GetComponent<ParticleSystem>();
		_HitEffect.transform.SetLocalPositionAndRotation(new(0f, 1.5f, 2f), Quaternion.identity);

		RefreshUI();
	}

	void HitDelaySlider(float v)
	{
		_HitDelaySlider.GetComponentInChildren<TMP_Text>().text = v.ToString("0.00");
	}

	void RefreshUI()
	{
		_SaveButton.gameObject.SetActive(Active);

		// 왼쪽 위
		_OpenEffectButton.gameObject.SetActive(_AnimationClip);
		_IsLocalToggle.gameObject.SetActive(Active);
		string clipName = _AnimationClip ? _AnimationClip.name : "-";
		string effectName = _EffectPrefab ? _EffectPrefab.name : "-";
		_LogText.text = $"애니메이션 : {clipName}\n이펙트 : {effectName}";
		_DelaySlider.transform.parent.gameObject.SetActive(Active);

		// 왼쪽 아래
		_BottomLeft.SetActive(Active);
		string hitName = _HitEffectPrefab ? _HitEffectPrefab.name : "-";
		_BottomLeftLogText.text = $"타격 이펙트 : {hitName}";
		_OpenHitEffectButton.gameObject.SetActive(_IsAttackToggle.isOn);
		_BottomLeftLogText.gameObject.SetActive(_IsAttackToggle.isOn);
		_HitDelaySlider.transform.parent.gameObject.SetActive(_IsAttackToggle.isOn);
	}
}
