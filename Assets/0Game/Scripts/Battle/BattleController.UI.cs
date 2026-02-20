using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SingletonManager;

namespace Battle
{
	public partial class BattleController 
	{
		[Header("UI")]
		public Canvas _Canvas;
		public SkillIcon _Skill1SkillIcon;
		public SkillIcon _Skill2SkillIcon;
		public SkillIcon _UltimateSkillIcon;
		public AnimationCurve _HPSliderScaleCurve;
		public Slider _PlayerHPSlider;
		public Slider _PlayerHPSlider_Inner;
		public TMP_Text _PlayerHPText;
		public GameObject _DamageText;
		public Transform _DamageTextParent;
		public GameObject _DamageScreen;

		void UpdateUI()
		{
			// 체력
			float percent = _Player._HP / _Player._MaxHP;
			_PlayerHPSlider.value = percent;
			_PlayerHPSlider_Inner.value = Mathf.MoveTowards(_PlayerHPSlider_Inner.value, percent, 0.3f * Time.deltaTime);
			_PlayerHPText.text = $"{_Player._HP} / {_Player._MaxHP}";

			// 스킬
			RefreshSkillIcon(_Skill1SkillIcon, _Player._Skill1._Attack._Cooltime, _Player._LastSkill1Time);
			RefreshSkillIcon(_Skill2SkillIcon, _Player._Skill2._Attack._Cooltime, _Player._LastSkill2Time);
			RefreshSkillIcon(_UltimateSkillIcon, _Player._Ultimate._Attack._Cooltime, _Player._LastUltimateTime);

			void RefreshSkillIcon(SkillIcon icon, float cooltime, float lastTime)
			{
				float elapsed = Time.time - lastTime;
				if (elapsed < cooltime)
				{
					icon._Slider.value = elapsed / cooltime;
					float remained = cooltime - elapsed;
					icon._Text.text = remained.ToString(remained < 1f ? "0.0" : "0");
				}
				else
				{
					icon._Slider.value = 0f;
					icon._Text.text = "";
				}
			}
		}

		public void ShowDamageText(float damage, Vector3 position)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				GameObject newText = Instantiate(_DamageText, _DamageTextParent);
				newText.GetComponentInChildren<TMP_Text>().text = damage.ToString("0");
				float t = 0f;
				while (t < 3f)
				{
                    newText.transform.localPosition = _Canvas.WorldToCanvas(_MainCamera.GetComponent<Camera>(), position);
					t += Time.deltaTime;
					yield return null;
				}
				Destroy(newText);
			}
		}

		public void ShowDamageScreen()
		{
            CanvasGroup canvasGroup = _DamageScreen.GetComponent<CanvasGroup>();
			canvasGroup.alpha = 0.5f;
			canvasGroup.DOKill();
			canvasGroup.DOFade(0f, 0.3f);
		}
	}
}
