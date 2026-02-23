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
		public GameObject _DamageText;
		public Transform _DamageTextParent;
		public GameObject _DamageScreen;
		public GameObject _EnemyHPPrefab;
		public Transform _EnemyHPParent;
		public UI_PlayerHP _PlayerHPPrefab;
		public Transform _PlayerHPParent;

		void InitUI()
		{
			foreach (Player player in _Players)
			{
                UI_PlayerHP ui = Instantiate(_PlayerHPPrefab, _PlayerHPParent);
				player._UI_HP = ui;
			}
			foreach (Enemy enemy in _Enemys)
			{
				GameObject ui = Instantiate(_EnemyHPPrefab, _EnemyHPParent);
				enemy._HPSlider = ui.GetComponent<Slider>();
				enemy._HPSlider_Inner = ui.transform.Find("Inner").GetComponent<Slider>();
			}
		}

		void UpdateUI()
		{
			foreach (Player player in _Players)
			{
                // 체력
                Character c = player._Character;
				float hp = Mathf.Max(1f, c._HP);
				float percent = hp / c._MaxHP;
				player._UI_HP._Slider.value = percent;
				player._UI_HP._Slider_Inner.value = Mathf.MoveTowards(player._UI_HP._Slider_Inner.value, percent, 0.3f * Time.deltaTime);
				player._UI_HP._HPText.text = $"{hp:0} / {c._MaxHP:0}";

				// 스킬
				RefreshSkillIcon(_Skill1SkillIcon, c._Skill1._Attack._Cooltime, c._LastSkill1Time);
				RefreshSkillIcon(_Skill2SkillIcon, c._Skill2._Attack._Cooltime, c._LastSkill2Time);
				RefreshSkillIcon(_UltimateSkillIcon, c._Ultimate._Attack._Cooltime, c._LastUltimateTime);

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
