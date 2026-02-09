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
		public int _Priority;
		public float _MoveSpeed;
		public bool _Restart;
		public float _Duration;
		public bool _LimitRotate;
		public bool _CanDash, _CanJump, _CanAttack, _CanGuard;
		public bool _UseRootMotion;
		public string _EffectName;
		public string _AttackName;

		public Action _OnEnd;

		public Effect _EffectData;
		public Attack _AttackData;

		public bool IsAttack => _AttackData != null;

		public bool CanEnterState => true;

		public bool CanExitState => c._FSM.NextState._Priority >= _Priority;

		public void OnEnterState()
		{
			AnimancerState state = c._BaseLayer.Play(_Asset);
			_EffectData ??= Data.GetEffectData(state.Clip);
			_AttackData ??= Data.GetAttackData(state.Clip);

			// 공격이 아니면 N번째 공격 상태 초기화
			if (!IsAttack)
			{
				c._AttackIndex = -1;
			}

			// 다음 공격 가능 조건 초기화
			c._NextAttackAvailable = false;
			c._NextAttackInput = false;

			// Duration : 설정되어 있다면 애니메이션이 끝나도 홀드
			if (_Duration == 0f)
			{
				state.Events(c).OnEnd ??= () =>
				{
					c._FSM.ForceSetDefaultState();
					_OnEnd?.Invoke();
				};
			}
			// Duration < 0f : 루프
			else if (_Duration < 0f)
			{
				state.Events(c).OnEnd ??= () =>
				{
					_OnEnd?.Invoke();
				};
			}

			// Restart : 같은 State로 다시 들어올 때 애니메이션을 재시작
			if (_Restart)
			{
				state.Time = 0f;
			}

			// 이펙트
			if (_EffectData != null)
			{
				c.PlayEffect(_EffectData, c);
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

        public override string ToString() => _Asset.ToString();
    }
}
