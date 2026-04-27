using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Animancer;
using Animancer.FSM;
using DG.Tweening;
using NaughtyAttributes;
using Animancer.TransitionLibraries;
using System.Linq;

namespace Battle
{
	public partial class Character
	{
		[Header("애니메이션")]
		public TransitionLibraryAsset _TransitionLibrary;
		[ShowIf("_ShowAll")] public AvatarMask _UpperBodyMask;
		[ShowIf("_ShowAll")] public AnimationCurve _AttackMoveCurve;
		[ShowIf("_ShowAll")] public StringAsset _MoveX, _MoveY;
		[ShowIf("_ShowAll")] public StringAsset _NextAttack;

		AnimancerComponent _Animancer;
		[HideInInspector] public AnimancerLayer _BaseLayer, _UpperBodyLayer;
		[HideInInspector] public StateMachine<State>.WithDefault _FSM;
		SmoothedVector2Parameter _MoveParameter;

		// BaseLayer
		TransitionAsset _IdleAsset;
		TransitionAsset _MoveAsset;
		TransitionAsset _RunAsset;
		[HideInInspector] public TransitionAsset _DashFwdAsset, _DashBwdAsset, _DashLeftAsset, _DashRightAsset;
		[HideInInspector] public TransitionAsset _JumpAsset, _LandAsset;
		List<TransitionAsset> _DamageAssets;
		TransitionAsset _GetDownAsset, _GetUpAsset;
		TransitionAsset _DieAsset;
		List<TransitionAsset> _NormalAttackAssets;
		TransitionAsset _JumpAttackAsset;
		TransitionAsset _DashAttackAsset;
		TransitionAsset _SpecialAttackAsset;
		TransitionAsset _JumpSpecialAttackAsset;
		TransitionAsset _GuardAttackAsset;
		TransitionAsset _Skill1Asset;
		TransitionAsset _Skill2Asset;
		TransitionAsset _UltimateAsset;

		// UpperBodyLayer
		[HideInInspector] public TransitionAsset _GuardUpAsset, _GuardDownAsset;

		[HideInInspector] public State _Idle;
		[HideInInspector] public State _Move;
		[HideInInspector] public State _Run;
		[HideInInspector] public State _Dash;
		[HideInInspector] public State _Jump;
		[HideInInspector] public State _Fall;
		[HideInInspector] public State _Land;
		State _Damage;
		State _GetDown, _GetUp;
		State _Die;
		List<State> _NormalAttacks;
		State _JumpAttack;
		[HideInInspector] public State _DashAttack;
		State _SpecialAttack;
		[HideInInspector] public State _JumpSpecialAttack;
		State _GuardAttack;
		[HideInInspector] public State _Skill1;
		[HideInInspector] public State _Skill2;
		[HideInInspector] public State _Ultimate;

		void InitFSM()
		{
			_BaseLayer = _Animancer.Layers[0];
			_UpperBodyLayer = _Animancer.Layers[1];
			_UpperBodyLayer.Mask = _UpperBodyMask;
			_MoveParameter = new(_Animancer, _MoveX, _MoveY, 0.15f);

			// TransitionAsset 초기화
			Dictionary<string, TransitionAsset> transitionDict = _TransitionLibrary.Definition.Transitions.ToDictionary(a => a.name, b => b as TransitionAsset);
			_IdleAsset = transitionDict["Idle"];
			_MoveAsset = transitionDict["Move"];
			_JumpAsset = transitionDict["Jump"]; 
			_LandAsset = transitionDict["Land"];
			_DamageAssets = new();
			for (int i = 0; i < 10; i++)
			{
				int n = i + 1;
				if (transitionDict.TryGetValue($"Damage{n}", out TransitionAsset asset))
				{
					_DamageAssets.Add(asset);
				}
				else break;
			};
			_GetDownAsset = transitionDict["GetDown"]; 
			_GetUpAsset = transitionDict["GetUp"];
			_DieAsset = transitionDict["Die"];
			_Skill1Asset = transitionDict["Skill1"];
			_Skill2Asset = transitionDict["Skill2"];
			_UltimateAsset = transitionDict["Ultimate"];

			if (_Player)
			{
				_RunAsset = transitionDict["Run"];
				_DashFwdAsset = transitionDict["DashFwd"];
				_DashBwdAsset = transitionDict["DashBwd"];
				_DashLeftAsset = transitionDict["DashLeft"];
				_DashRightAsset = transitionDict["DashRight"];
				_NormalAttackAssets = new();
				for (int i = 0; i < 10; i++)
				{
					int n = i + 1;
					if (transitionDict.TryGetValue($"NormalAttack{n}", out TransitionAsset asset))
					{
						_NormalAttackAssets.Add(asset);
					}
					else break;
				}
				_JumpAttackAsset = transitionDict["JumpAttack"];
				_DashAttackAsset = transitionDict["DashAttack"];
				_SpecialAttackAsset = transitionDict["SpecialAttack"];
				_JumpSpecialAttackAsset = transitionDict["JumpSpecialAttack"];
				_GuardAttackAsset = transitionDict["GuardAttack"];
				_GuardUpAsset = transitionDict["GuardUp"];
				_GuardDownAsset = transitionDict["GuardDown"];
			}

			// State 초기화
			_Idle = new()
			{
				c = this,
				_Asset = _IdleAsset,
				_Priority = -12,
				_MoveSpeed = 1f,
				_Duration = -1f,
				_CanDash = true,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Idle.Init();
			_Move = new()
			{
				c = this,
				_Asset = _MoveAsset,
				_Priority = -12,
				_MoveSpeed = 1f,
				_Duration = -1f,
				_CanDash = true,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Move.Init();
			_Jump = new()
			{
				c = this,
				_MoveSpeed = 1f,
				_Priority = -12,
				_Restart = true,
				_CanDash = true,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Jump.Init();
			_Fall = new()
			{
				c = this,
				_Asset = _JumpAsset,
				_Priority = -11,
				_MoveSpeed = 1f,
				_Duration = -1f,
				_CanDash = true,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Fall.Init();
			_Land = new()
			{
				c = this,
				_Asset = _LandAsset,
				_CanDash = true,
				_CanJump = true,
				_CanAttack = true,
				_CanGuard = true,
			};
			_Land.Init();
			_Damage = new()
			{
				c = this,
				_Priority = 10,
				_Restart = true,
				_MoveSpeed = 0.6f,
			};
			_Damage.Init();
			_GetDown = new()
			{
				c = this,
				_Asset = _GetDownAsset,
				_Priority = 11,
				_Duration = float.MaxValue,
			};
			_GetDown.Init();
			_GetUp = new()
			{
				c = this,
				_Asset = _GetUpAsset,
				_Priority = 11,
			};
			_GetUp.Init();
			_Die = new()
			{
				c = this,
				_Asset = _DieAsset,
				_Priority = 12,
				_Duration = float.MaxValue,
			};
			_Die.Init();
			_Skill1 = new()
			{
				c = this,
				_Asset = _Skill1Asset,
				_MoveSpeed = 0.3f,
				_Restart = true,
				_LimitRotate = true,
				_CanAttack = true,
				_RootMotionMode = State.RootMotionMode.GroundOnly,
			};
			_Skill1.Init();
			_Skill2 = new()
			{
				c = this,
				_Asset = _Skill2Asset,
				_MoveSpeed = 0.3f,
				_Restart = true,
				_LimitRotate = true,
				_CanAttack = true,
				_RootMotionMode = State.RootMotionMode.GroundOnly,
			};
			_Skill2.Init();
			_Ultimate = new()
			{
				c = this,
				_Asset = _UltimateAsset,
				_MoveSpeed = 0f,
				_Restart = true,
				_LimitRotate = true,
				_RootMotionMode = State.RootMotionMode.GroundOnly,
			};
			_Ultimate.Init();

			if (_Player)
			{
				_Run = new()
				{
					c = this,
					_Asset = _RunAsset,
					_Priority = -12,
					_MoveSpeed = 2f,
					_Duration = -1f,
					_CanJump = true,
					_CanAttack = true,
					_CanGuard = true,
				};
				_Run.Init();
				_Dash = new()
				{
					c = this,
					_MoveSpeed = 2f,
					_Restart = true,
					_Duration = GetComponent<CharacterController_KCC>()._DashDuration,
					_LimitRotate = true,
					_CanJump = true,
					_CanAttack = true,
				};
				_Dash.Init();
				_NormalAttacks = new();
				for (int i = 0; i < _NormalAttackAssets.Count; i++)
				{
					TransitionAsset asset = _NormalAttackAssets[i];
					State newState = new()
					{
						c = this,
						_Asset = asset,
						_MoveSpeed = 0.3f,
						_Restart = true,
						_LimitRotate = true,
						_CanGuard = true,
						_CanAttack = true,
						_RootMotionMode = State.RootMotionMode.GroundOnly,
					};
					newState.Init();
					_NormalAttacks.Add(newState);
				}
				_JumpAttack = new()
				{
					c = this,
					_Asset = _JumpAttackAsset,
					_MoveSpeed = 1f,
					_Restart = true,
					_LimitRotate = true,
					_CanJump = true,
					_CanAttack = true,
					_CanGuard = true,
				};
				_JumpAttack.Init();
				_DashAttack = new()
				{
					c = this,
					_Asset = _DashAttackAsset,
					_Priority = 1,
					_MoveSpeed = 1f,
					_Restart = true,
					_LimitRotate = true,
					_CanAttack = true,
					_CanGuard = true,
				};
				_DashAttack.Init();
				_SpecialAttack = new()
				{
					c = this,
					_Asset = _SpecialAttackAsset,
					_MoveSpeed = 0.3f,
					_Restart = true,
					_LimitRotate = true,
					_CanGuard = true,
					_CanAttack = true,
					_RootMotionMode = State.RootMotionMode.GroundOnly,
				};
				_SpecialAttack.Init();
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
				_JumpSpecialAttack.Init();
				_GuardAttack = new()
				{
					c = this,
					_Asset = _GuardAttackAsset,
					_MoveSpeed = 0.3f,
					_Restart = true,
					_LimitRotate = true,
				};
				_GuardAttack.Init();
			}

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
				Attack();
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
				active &= IsMovable();
				active &= _MoveSpeed > 0f || state == _Dash || state == _Land;
				active &= !state.IsAttack;
				active &= !IsGuarding();
				_IsRunning = active;
			}

			// 일어나기
			if (UseKCC && state == _GetDown)
			{
				bool getUp = _BaseLayer.CurrentState.NormalizedTime >= 1f;
				getUp &= Motor.GroundingStatus.FoundAnyGround;
				if (getUp)
				{
					bool success = _FSM.TrySetState(_GetUp);

					// 기상 무적
					if (success && _Player)
					{
						_InvincibleTimer = 2.4f;
					}
				}
			}
		}

		public void PlayAction(State state)
		{
			if (state._Restart)
			{
				_FSM.TryResetState(state);
			}
			else
			{
				_FSM.TrySetState(state);
			}
		}

		void Play_Canceling(State state, bool shadow)
		{
			if (shadow)
			{
				StartCoroutine(Internal());
			}
			_FSM.ForceSetState(state);

			IEnumerator Internal()
			{
				// 잔상 생성
				float duration = 1f;
				GameObject model = Instantiate(_Model, transform.position, transform.rotation);
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
				animation.Animator = model.GetComponent<Animator>();
				animation.Clip = _Animancer.States.Current.Clip;
				animation.Play();
				animation.NormalizedTime = _Animancer.States.Current.NormalizedTime;
				animation.Speed = 0f;
				yield return new WaitForSeconds(duration);
				Destroy(model);
			}
		}
	}
}
