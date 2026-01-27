using Animancer;
using Animancer.FSM;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SingletonManager;
using static DataManager;

namespace Battle
{
	public class State : IState
	{
		public Character c;
		public TransitionAsset _Asset;
		public List<TransitionAsset> _RandomAssets;
		public int _Priority;
		public bool _Repeat;
		public float _Duration;
		public bool _CanMove, _CanDash, _CanJump, _CanAttack, _CanGuard;
		public EffectInfo _EffectInfo;

		public Action _OnEnd;

		public bool IsAttack => _EffectInfo?._HitEffectPrefab;

		public bool CanEnterState => true;

		public bool CanExitState => c._FSM.NextState._Priority >= _Priority;

		public void OnEnterState()
		{
			AnimancerState state;

			// RandomAssets : 설정되어 있다면 무작위 재생
			if (_RandomAssets != null)
			{ 
				state = c._BaseLayer.Play(_RandomAssets.PickOne());
			}
			else
			{
				state = c._BaseLayer.Play(_Asset);
			}

			// IsAttack : 공격이 아니면 N번째 공격 상태 초기화
			if (!IsAttack)
			{
				c._AttackIndex = -1;
			}

			// Duration : 설정되어 있다면 애니메이션이 끝나도 홀드
			if (_Duration == 0f)
			{
				state.Events(c).OnEnd ??= () =>
				{
					c._FSM.ForceSetDefaultState();
					_OnEnd?.Invoke();
				};
			}
			else
			{
				state.Events(c).OnEnd ??= () =>
				{
					_OnEnd?.Invoke();
				};
			}

			// Repeat : 같은 State로 다시 들어올 때 애니메이션을 재시작하지 않음
			if (!_Repeat)
			{
				state.Time = 0f;
			}

			// 이펙트
			_EffectInfo = Data.GetEffectInfo(state.Clip);
			if (_EffectInfo != null)
			{
				c.PlayEffect(_EffectInfo);
			}
		}

		public void OnExitState() { }

		public void UpdateState()
		{
			if (_Duration > 0f && c._Animancer.States.Current.Time > _Duration)
			{
				c._FSM.ForceSetDefaultState.Invoke();
			}
		}
	}
}
