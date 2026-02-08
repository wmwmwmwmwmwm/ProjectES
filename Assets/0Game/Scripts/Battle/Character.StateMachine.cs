using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Animancer;
using Animancer.FSM;
using System.Linq;
using DG.Tweening;

namespace Battle
{
	public partial class Character
	{
		[Header("애니메이션")]
		public AnimancerComponent _Animancer;
		public AvatarMask _UpperBodyMask;
		public AnimationCurve _AttackMoveCurve;

		// BaseLayer
		public TransitionAsset _IdleAsset;
		public TransitionAsset _MoveAsset;
		public TransitionAsset _RunAsset;
		public TransitionAsset _DashFwdAsset, _DashBwdAsset, _DashLeftAsset, _DashRightAsset;
		public TransitionAsset _JumpAsset, _LandAsset;
		public List<TransitionAsset> _DamageAssets;
		public TransitionAsset _GetDownAsset, _GetUpAsset;
		public TransitionAsset _DieAsset;
		public List<TransitionAsset> _NormalAttackAssets;
		public TransitionAsset _JumpAttackAsset;
		public TransitionAsset _DashAttackAsset;
		public TransitionAsset _SpecialAttackAsset;
		public TransitionAsset _JumpSpecialAttackAsset;
		public TransitionAsset _Skill1Asset;
		public TransitionAsset _Skill2Asset;
		public TransitionAsset _UltimateAsset;

		// UpperBodyLayer
		public TransitionAsset _GuardUpAsset, _GuardDownAsset;

		public StringAsset _MoveX, _MoveY;
		public StringAsset _NextAttack;

		[HideInInspector] public AnimancerLayer _BaseLayer, _UpperBodyLayer;
		[HideInInspector] public StateMachine<State>.WithDefault _FSM;
		SmoothedVector2Parameter _MoveParameter;

		State _Idle;
		State _Move;
		State _Run;
		State _Dash;
		State _Jump;
		State _Fall;
		State _Land;
		State _Damage;
		State _GetDown, _GetUp;
		State _Die;
		List<State> _NormalAttacks;
		State _JumpAttack;
		State _DashAttack;
		State _SpecialAttack;
		State _JumpSpecialAttack;
		State _Skill1;
		State _Skill2;
		State _Ultimate;

		void InitFSM()
		{
			_BaseLayer = _Animancer.Layers[0];
			_UpperBodyLayer = _Animancer.Layers[1];
			_UpperBodyLayer.Mask = _UpperBodyMask;
			_MoveParameter = new(_Animancer, _MoveX, _MoveY, 0.15f);

			// BaseLayer
			_Idle = new()
			{
				c = this,
				_Asset = _IdleAsset,
				_Priority = -2,
				_MoveSpeed = 1f,
				_Duration = -1f,
				_CanDash = true,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Move = new()
			{
				c = this,
				_Asset = _MoveAsset,
				_Priority = -2,
				_MoveSpeed = 1f,
				_Duration = -1f,
				_CanDash = true,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Run = new()
			{
				c = this,
				_Asset = _RunAsset,
				_Priority = -2,
				_MoveSpeed = 2.5f,
				_Duration = -1f,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Dash = new()
			{
				c = this,
				_MoveSpeed = 2.5f,
				_Restart = true,
				_Duration = _DashDuration,
				_LimitRotate = true,
				_CanJump = true,
				_CanAttack = true,
				_CancelRootMotion = true,
			};
			_Jump = new()
			{
				c = this,
				_MoveSpeed = 1f,
				_Priority = -2,
				_Restart = true,
				_CanDash = true,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Fall = new()
			{
				c = this,
				_Asset = _JumpAsset,
				_Priority = -2,
				_MoveSpeed = 1f,
				_Duration = -1f,
				_CanDash = true,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Land = new()
			{
				c = this,
				_Asset = _LandAsset,
				_Priority = -1,
				_MoveSpeed = 1f,
				_CanDash = true,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Damage = new()
			{
				c = this,
				_Priority = 1,
				_MoveSpeed = 0.6f,
			};
			_GetDown = new()
			{
				c = this,
				_Asset = _GetDownAsset,
				_Priority = 2,
				_Duration = float.MaxValue,
			};
			_GetUp = new()
			{
				c = this,
				_Asset = _GetUpAsset,
				_Priority = 2,
			};
			_Die = new()
			{
				c = this,
				_Asset = _DieAsset,
				_Priority = 3,
				_Duration = float.MaxValue,
			};
			_NormalAttacks = new();
			for (int i = 0; i < _NormalAttackAssets.Count; i++)
			{
				TransitionAsset asset = _NormalAttackAssets[i];
				bool isLast = i == _NormalAttackAssets.Count - 1;
				_NormalAttacks.Add(new()
				{
					c = this,
					_Asset = asset,
					_MoveSpeed = 0.3f,
					_Restart = true,
					_LimitRotate = true,
					_CanGuard = true,
					_CanAttack = true,
				});
			}
			_JumpAttack = new()
			{
				c = this,
				_Asset = _JumpAttackAsset,
				_MoveSpeed = 1f,
				_Restart = true,
				_LimitRotate = true,
				_CanJump = true,
				_CanGuard = true,
			};
			_DashAttack = new()
			{
				c = this,
				_Asset = _DashAttackAsset,
				_MoveSpeed = 1f,
				_Restart = true,
				_LimitRotate = true,
				_CanGuard = true,
			};
			_SpecialAttack = new()
			{
				c = this,
				_Asset = _SpecialAttackAsset,
				_MoveSpeed = 0.3f,
				_Restart = true,
				_LimitRotate = true,
				_CanGuard = true,
				_CanAttack = true,
			};
			_JumpSpecialAttack = new()
			{
				c = this,
				_Asset = _JumpSpecialAttackAsset,
				_MoveSpeed = 1f,
				_Restart = true,
				_LimitRotate = true,
				_CanJump = true,
				_CanGuard = true,
			};
			_Skill1 = new()
			{
				c = this,
				_Asset = _Skill1Asset,
				_MoveSpeed = 0.3f,
				_Restart = true,
				_LimitRotate = true,
				_CanAttack = true,
			};
			_Skill2 = new()
			{
				c = this,
				_Asset = _Skill2Asset,
				_MoveSpeed = 0.3f,
				_Restart = true,
				_LimitRotate = true,
				_CanAttack = true,
			};
			_Ultimate = new()
			{
				c = this,
				_Asset = _UltimateAsset,
				_MoveSpeed = 0f,
				_Restart = true,
				_LimitRotate = true,
			};

			// 이벤트
			_Animancer.Events.TryAdd(_NextAttack, () => _NextAttackAvailable = true);

			_FSM.DefaultState = _Idle;
		}

		void UpdateFSM()
		{
			State state = _FSM.CurrentState;
			state.UpdateState();

			// 다음 공격
			if (_NextAttackAvailable && _NextAttackInput)
			{
				_NextAttackInput = false;
				_AttackIndex++;
				_FSM.TrySetState(_NormalAttacks[_AttackIndex]);
				GiveDamage();
			}

			// 공격 시 살짝 이동 가능
			if (state.IsAttack)
			{
				_AttackMovePercent = _AttackMoveCurve.Evaluate(_BaseLayer.CurrentState.NormalizedTime);
			}
			else
			{
				_AttackMovePercent = 0f;
			}

			// 가드 취소
			if (!state._CanGuard)
			{
				GuardCancel();
			}

			// 달리기 멈추기
			if (_IsRunning)
			{
				float degree = Util.DirectionToRotationZ(new(_MoveInput.x, _MoveInput.z));

				bool active = degree > 30f && degree < 150f;
				active &= state._MoveSpeed > 0f || state == _Dash;
				active &= !state.IsAttack;
				active &= !IsGuarding();
				_IsRunning = active;
			}

			// 일어나기
			if (state == _GetDown)
			{
				bool getUp = _BaseLayer.CurrentState.NormalizedTime >= 1f;
				getUp &= _Motor.GroundingStatus.IsStableOnGround;
				if (getUp)
				{
					_FSM.TrySetState(_GetUp);
				}
			}
		}

		void Play_Canceling(State state, bool shadow)
		{
			_BaseLayer.Play(state._Asset, 0f);
			_FSM.ForceSetState(state);
			if (!shadow) return;
			StartCoroutine(Internal());

			IEnumerator Internal()
			{
				// 잔상 생성
				float duration = 1f;
				GameObject model = Instantiate(_CancelModel, transform.position, transform.rotation);
				Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
				Material whiteMaterial = new(_WhiteMaterial);
				foreach (Renderer renderer in renderers)
				{
					Material[] mats = renderer.materials;
					for (int i = 0; i < mats.Length; i++)
					{
						mats[i] = whiteMaterial;
					}
					renderer.materials = mats;
				}
				whiteMaterial.DOFade(0f, duration);
				SoloAnimation animation = model.AddComponent<SoloAnimation>();
				animation.Clip = _Animancer.States.Current.Clip;
				animation.NormalizedTime = _Animancer.States.Current.NormalizedTime;
				animation.Speed = 0f;
				animation.Play();
				yield return new WaitForSeconds(duration);
				Destroy(model);
			}
		}
	}
}
