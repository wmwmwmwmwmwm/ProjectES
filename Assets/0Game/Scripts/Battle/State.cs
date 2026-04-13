using Animancer;
using Animancer.FSM;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SingletonManager;

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
		public enum RootMotionMode { None, GroundOnly, All }
		public RootMotionMode _RootMotionMode;
		public string _EffectName;
		public string _AttackName;

		public AnimancerState _State;
		public Action _OnEnd;

		public List<Character.Effect> _EffectDatas;
		public BattleAttack _Attack;

		public bool IsAttack => _Attack != null;

		public bool CanEnterState => true;

		public bool CanExitState => c._FSM.NextState._Priority >= _Priority;

		public void Init()
		{
			if (_Asset == null) return;

			_State = c._BaseLayer.GetOrCreateState(_Asset);
			_EffectDatas = c._EffectContainer.GetEffectDatas(_Asset);
			_Attack = c._AttackContainer.GetAttack(_Asset);

			// Duration : 설정되어 있다면 애니메이션이 끝나도 홀드
			if (_Duration == 0f)
			{
				_State.Events(c).OnEnd = () =>
				{
					c._FSM.ForceSetDefaultState();
					_OnEnd?.Invoke();
				};
			}
			// Duration < 0f : 루프
			else if (_Duration < 0f)
			{
				_State.Events(c).OnEnd = () =>
				{
					_OnEnd?.Invoke();
				};
			}
		}

		public void OnEnterState()
		{
			_State = c._BaseLayer.Play(_Asset);

			// 공격이 아니면 N번째 공격 상태 초기화
			if (!IsAttack)
			{
				c._AttackIndex = -1;
			}

			// 다음 공격 가능 조건 초기화
			c._NextAttackAvailable = false;
			c._NextAttackInput = false;

			// 벽 점프 초기화
			if (this == c._JumpSpecialAttack)
			{
				c._AlreadyWallJump = false;
			}

			// Restart : 같은 State로 다시 들어올 때 애니메이션을 재시작
			if (_Restart)
			{
				_State.Time = 0f;
			}

			// Y축 정지 트리거
			c._StopYTrigger = _RootMotionMode == RootMotionMode.All;
		}

		public void OnExitState() { }

		public void UpdateState()
		{
			if (_Duration > 0f && _State.Time > _Duration)
			{
				c._FSM.ForceSetDefaultState();
				_OnEnd?.Invoke();
			}
		}

		public override string ToString() => _Asset.ToString();

		public void SetAsset(TransitionAsset asset)
		{
			_Asset = asset;
			Init();
		}
	}
}
