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
		public float _MoveSpeed;
		public bool _Restart;
		public float _Duration;
		public bool _LimitRotate;
		public bool _CanDash, _CanJump, _CanAttack, _CanGuard;
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

			// NextAttackAvailable : 다음 공격 가능 조건 초기화
			c._NextAttackAvailable = false;

			// Duration : 설정되어 있다면 애니메이션이 끝나도 홀드
			if (_Duration == 0f)
			{
				state.Events(c).OnEnd ??= () =>
				{
					c._FSM.ForceSetDefaultState();
					_OnEnd?.Invoke();
				};
			}

			// Restart : 같은 State로 다시 들어올 때 애니메이션을 재시작
			if (_Restart)
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
				_OnEnd?.Invoke();
			}
		}
	}
}
