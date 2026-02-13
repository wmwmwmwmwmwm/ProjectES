using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SingletonManager;

namespace Battle
{
	public partial class BattleController 
	{
		[Header("UI")]
		public SkillIcon _Skill1SkillIcon;
		public SkillIcon _Skill2SkillIcon;
		public SkillIcon _UltimateSkillIcon;

		void UpdateUI()
		{
			RefreshSkillIcon(_Skill1SkillIcon, _Player._Skill1._AttackData._AttackPrefab._Cooltime, _Player._LastSkill1Time);
			RefreshSkillIcon(_Skill2SkillIcon, _Player._Skill2._AttackData._AttackPrefab._Cooltime, _Player._LastSkill2Time);
			RefreshSkillIcon(_UltimateSkillIcon, _Player._Ultimate._AttackData._AttackPrefab._Cooltime, _Player._LastUltimateTime);

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
}
