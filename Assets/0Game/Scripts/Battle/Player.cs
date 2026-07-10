using Animancer;
using DG.Tweening;
using KinematicCharacterController;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using static SingletonManager;
using static UnityEngine.InputSystem.InputAction;

namespace Battle
{
	public partial class Player : MonoBehaviour
	{
		[BoxGroup("설정")] public float _ShoulderHeight;
		[BoxGroup("설정")] public float _CameraDistance;

		public Transform _CooltimeJitter;
		public ParticleSystem _JustGuardEffect;
		public GameObject _ChangeCharacterEffectPrefab;
		public float _CameraRotationSpeed;

		[HideInInspector] public Character c;
		[HideInInspector] public Vector2 _LookInput;
		[HideInInspector] public Vector2 _LookRotation;
		[HideInInspector] public UI_PlayerHP _UI_HP;
		[HideInInspector] public Coroutine _JustGuardCoroutine;
		[HideInInspector] public bool _JustGuardCancelTrigger;
		[HideInInspector] public Vector3 _CameraOffset;
		Collider[] _InteractionResults;

		BattleController Controller => BattleController.Instance;

		public void Init()
		{
			c = GetComponent<Character>();
			c.Init();
			c.EmitEffect(_JustGuardEffect, false);
			_InteractionResults = new Collider[1];
		}

		public void Init2()
		{
			c.Init2(); 
		}

		public void ReceiveInput(bool on)
		{
			if (on)
			{
				Inputs.Movement.performed += Move;
				Inputs.Look.performed += Look;
				Inputs.Dash += c.Dash;
				Inputs.Jump.performed += c.Jump;
				Inputs.Guard.performed += c.Guard;
				Inputs.NormalAttack.performed += c.NormalAttack;
				Inputs.SpecialAttack.performed += c.SpecialAttack;
				Inputs.Skill1.performed += c.Skill1;
				Inputs.Skill2.performed += c.Skill2;
				Inputs.Ultimate.performed += c.Ultimate;
				Inputs.Character1.performed += Character1;
				Inputs.Character2.performed += Character2;
				Inputs.Interact.performed += Interact;
			}
			else
			{
				Inputs.Movement.performed -= Move;
				Inputs.Look.performed -= Look;
				Inputs.Dash -= c.Dash;
				Inputs.Jump.performed -= c.Jump;
				Inputs.Guard.performed -= c.Guard;
				Inputs.NormalAttack.performed -= c.NormalAttack;
				Inputs.SpecialAttack.performed -= c.SpecialAttack;
				Inputs.Skill1.performed -= c.Skill1;
				Inputs.Skill2.performed -= c.Skill2;
				Inputs.Ultimate.performed -= c.Ultimate;
				Inputs.Character1.performed -= Character1;
				Inputs.Character2.performed -= Character2;
				Inputs.Interact.performed -= Interact;
			}
		}

		public BoxCollider _InteractionCollider;
		void Update()
		{
			// 카메라 위치
			_CameraOffset = Vector3.MoveTowards(_CameraOffset, GetCameraOffset(), 5f * Time.deltaTime);

			// 카메라 회전
			_LookRotation.x += _LookInput.y * _CameraRotationSpeed * -1f * 0.01f;
			_LookRotation.x = Mathf.Clamp(_LookRotation.x, -80f, 60f);
			_LookRotation.y += _LookInput.x * _CameraRotationSpeed * 0.01f;
			if (_LookRotation.y - transform.eulerAngles.y > 180f)
			{
				_LookRotation.y -= 360f;
			}
			else if (_LookRotation.y - transform.eulerAngles.y < -180f)
			{
				_LookRotation.y += 360f;
			}
			_LookRotation.y = Mathf.Clamp(_LookRotation.y, transform.eulerAngles.y - 60f, transform.eulerAngles.y + 60f);
			c._AimDestRotation = Quaternion.Euler(_LookRotation);

			// 인터랙션 판정 : 아이템 획득
			int overlapCapsuleCount = Physics.OverlapCapsuleNonAlloc(
				point0: c.Bottom,
				point1: c.Top,
				radius: c.Motor.Capsule.radius,
				results: _InteractionResults,
				layerMask: Layer.InteractionLayerMask);
			for (int i = 0; i < overlapCapsuleCount; i++)
			{
				Collider result = _InteractionResults[i];
				if (!result) continue;
				DropItem item = result.GetComponentInParent<DropItem>();
				if (!item) continue;

				Controller.ObtainItem(item);
			}

			// 인터랙션 판정 : 문 열기
			int overlapBoxCount = Physics.OverlapBoxNonAlloc(
				center: _InteractionCollider.GetCenter(),
				halfExtents: _InteractionCollider.size / 2f,
				results: _InteractionResults,
				orientation: transform.rotation,
				mask: Layer.InteractionLayerMask);
			bool overlap = overlapBoxCount > 0;
			if (overlap)
			{
				Interactable interactable = _InteractionResults.First().GetComponentInParent<Interactable>();
				if (interactable)
				{
					Controller.SetCurrentInteractable(interactable);
				}
			}
			else
			{
				Controller.SetCurrentInteractable(null);
			}
		}

		void Move(CallbackContext obj)
		{
			c._MoveInput = obj.ReadValue<Vector2>().ToVector3XZ();
		}

		void Look(CallbackContext obj)
		{
			_LookInput = obj.ReadValue<Vector2>();
		}

		public void CooltimeJitter()
		{
			_CooltimeJitter.DOShakePosition(0.3f, strength: 0.06f, vibrato: 100, fadeOut: false).SetEase(Ease.Flash)
				.OnKill(() => _CooltimeJitter.localPosition = Vector3.zero);
		}

		void Character1(CallbackContext obj)
		{
			ChangeCharacter(0);
		}

		void Character2(CallbackContext obj)
		{
			ChangeCharacter(1);
		}

		void ChangeCharacter(int index)
		{
			State state = c._FSM.CurrentState;
			bool stateCondition = state == c._Idle;
			stateCondition |= state == c._Move;
			stateCondition |= state == c._Run;
			stateCondition |= state == c._Jump;
			stateCondition |= state == c._Fall;
			stateCondition |= state == c._Land;
			if (!stateCondition) return;
			if (Controller._Players[index].c.IsDead()) return;

			if (Controller.SetActivePlayer(index))
			{
				Controller.PlayEffect123123(_ChangeCharacterEffectPrefab, c, transform.position, transform.rotation);
			}
		}

		void Interact(CallbackContext obj)
		{
			Controller.Interact();
		}

		public void JustGuard()
		{
			if (_JustGuardCoroutine != null)
			{
				StopCoroutine(_JustGuardCoroutine);
				_JustGuardCoroutine = null;
			}
			_JustGuardCoroutine = StartCoroutine(Internal());

			IEnumerator Internal()
			{
				AnimancerState state = c._UpperBodyLayer.Play(c._GuardUpAsset);
				state.Time = 0f;
				c.EmitEffect(_JustGuardEffect, true);
				float start = Time.time;
				_JustGuardCancelTrigger = false;
				yield return new WaitUntil(() => Time.time - start > 2.4f || _JustGuardCancelTrigger);
				_JustGuardCancelTrigger = false;
				c.EmitEffect(_JustGuardEffect, false);
				_JustGuardCoroutine = null;
			}
		}

		public bool IsJustGuard()
		{
			return _JustGuardCoroutine != null;
		}

		public Vector3 GetCameraOffset()
		{
			_CameraOffset.y = c.Motor.GroundingStatus.FoundAnyGround ? _ShoulderHeight : _ShoulderHeight - 0.5f;
			_CameraOffset.z = -_CameraDistance;
			return _CameraOffset;
		}
	}
}
