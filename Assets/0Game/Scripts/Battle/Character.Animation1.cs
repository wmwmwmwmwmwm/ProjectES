//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Playables;
//using VRM;
//using static UnityEngine.InputSystem.InputAction;
//using static SingletonManager;
//using NaughtyAttributes;

//public partial class MainCharacter
//{
//	public CinemachineImpulseSource AttackHitImpulse;
//	public float MoveSpeed, JumpSpeed;

//	Animator ThisAnimator;
//	Camera CharacterCamera;
//	CharacterController ThisController;
//	VRMBlendShapeProxy BlendShapeProxy;
//	PlayableDirector ThisDirector;
//	static int IdleAndMovementHash = Animator.StringToHash("IdleAndMovement");
//	static int JumpStartHash = Animator.StringToHash("JumpStart");
//	static int JumpMidAirHash = Animator.StringToHash("JumpMidAir");
//	static int LandHash = Animator.StringToHash("Land");
//	static int NormalAttack1Hash = Animator.StringToHash("Attack1");
//	static int NormalAttack2Hash = Animator.StringToHash("Attack2");
//	static int NormalAttack3Hash = Animator.StringToHash("Attack3");
//	static int NormalAttack4Hash = Animator.StringToHash("Attack4");
//	static int GuardHash = Animator.StringToHash("Guard");

//	RaycastHit LastRaycastHit;
//	[ReadOnly] public Vector2 MoveVelocityXZ;
//	float MoveVelocityY;
//	bool OnGround, GroundedThisFrame, FallingThisFrame, Guarding;
//	float AfterAttackTime;
//	readonly float AfterAttackDuration = 0.2f;
//	public bool AfterAttackDelaying => Time.time - AfterAttackTime < AfterAttackDuration;
//	float JumpTime;
//	float DestinationYaw;
//	float Acceleration
//	{
//		get
//		{
//			int CurrentStateHash = ThisAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash;
//			int NextStateHash = ThisAnimator.GetNextAnimatorStateInfo(0).shortNameHash;
//			float CurrentAcceleration = GetAcceleration(CurrentStateHash);
//			float NextAcceleration = GetAcceleration(NextStateHash);
//			float GetAcceleration(int hash)
//			{
//				if (AfterAttackDelaying) return 80f;
//				else if (hash == IdleAndMovementHash) return 100f;
//				else if (hash == JumpStartHash) return 40f;
//				else if (hash == JumpMidAirHash) return 40f;
//				else if (hash == LandHash) return 80f;
//				else return 0f;
//			}
//			return (CurrentAcceleration + NextAcceleration) / 2f;
//		}
//	}

//	bool CanMove
//	{
//		get
//		{
//			int CurrentStateHash = ThisAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash;
//			int NextStateHash = ThisAnimator.GetNextAnimatorStateInfo(0).shortNameHash;
//			return IsMovableState(CurrentStateHash) || IsMovableState(NextStateHash);
//			bool IsMovableState(int hash)
//			{
//				if (hash == IdleAndMovementHash || hash == JumpStartHash || hash == JumpMidAirHash || hash == LandHash || AfterAttackDelaying)
//					return true;
//				return false;
//			}
//		}
//	}

//	void Awake()
//	{
//		//Game.PlayerCharacter = this;
//		ThisAnimator = GetComponent<Animator>();
//		ThisController = GetComponent<CharacterController>();
//		BlendShapeProxy = GetComponent<VRMBlendShapeProxy>();
//		ThisDirector = GetComponent<PlayableDirector>();
//		CharacterCamera = Camera.main;
//		StartCoroutine(BlendShape());
//		IEnumerator BlendShape()
//		{
//			yield return new WaitForSeconds(0.1f);
//			BlendShapeProxy.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Neutral), 1f);
//		}
//	}

//	void OnEnable()
//	{
//		Inputs.Jump.performed += Jump;
//		Inputs.NormalAttack.performed += NormalAttack;
//	}

//	void OnDisable()
//	{
//		Inputs.Jump.performed -= Jump;
//		Inputs.NormalAttack.performed -= NormalAttack;
//	}

//	void Update()
//	{
//		bool OnGroundLastFrame = OnGround;
//		Ray ray = new(transform.position + Vector3.up * (ThisController.radius + Physics.defaultContactOffset), Vector3.down);
//		OnGround = Physics.SphereCast(ray, ThisController.radius - Physics.defaultContactOffset, out RaycastHit raycastHit, 0.1f, LayerMask.GetMask("Terrain"));
//		//OnGround = Physics.CapsuleCast(transform.position + Vector3.up * Physics.defaultContactOffset, transform.position + Vector3.down * Physics.defaultContactOffset, ThisController.radius - Physics.defaultContactOffset, Vector3.down, out RaycastHit raycastHit, 0.1f, LayerMask.GetMask("Terrain"), QueryTriggerInteraction.UseGlobal);
//		print(raycastHit.collider);
//		print(raycastHit.distance);
//		GroundedThisFrame = !OnGroundLastFrame && OnGround;
//		FallingThisFrame = OnGroundLastFrame && !OnGround;
//		if (FallingThisFrame && LastRaycastHit.distance > 0.4f)
//		{
//			ThisAnimator.SetTrigger("Fall");
//		}
//		LastRaycastHit = raycastHit;

//		// XZ축 이동
//		if (CanMove)
//		{
//			Vector3 InputMoveVectorLocal = Inputs.Movement.normalized.Vector2ToXZ();
//			Vector3 InputMoveVectorWorld = transform.TransformVector(InputMoveVectorLocal);
//			InputMoveVectorWorld *= InputMoveVectorLocal.z < -0.1f ? 0.5f : 1f;
//			Vector2 NewMoveVelocityXZ = new Vector2(InputMoveVectorWorld.x, InputMoveVectorWorld.z) * MoveSpeed;
//			if (Guarding)
//			{
//				NewMoveVelocityXZ /= 2f;
//			}
//			MoveVelocityXZ = Vector2.MoveTowards(MoveVelocityXZ, NewMoveVelocityXZ, Acceleration * Time.deltaTime);
//		}
//		else
//		{
//			MoveVelocityXZ = Vector2.zero;
//		}
//		Vector3 MoveVector = new(MoveVelocityXZ.x, MoveVelocityY, MoveVelocityXZ.y);
//		ThisController.Move(Time.deltaTime * MoveVector);

//		// Yaw로 캐릭터 방향 업데이트
//		float CurrentYaw = transform.eulerAngles.y;
//		DestinationYaw = CharacterCamera.transform.eulerAngles.y;
//		if (DestinationYaw - CurrentYaw > 180f)
//			DestinationYaw -= 360f;
//		else if (CurrentYaw - DestinationYaw > 180f)
//			CurrentYaw -= 360f;
//		float Yaw = Mathf.MoveTowardsAngle(CurrentYaw, DestinationYaw, CanMove ? 720f * Time.deltaTime : 0f);
//		transform.rotation = Quaternion.Euler(0f, Yaw, 0f);

//		Vector3 LocalMoveVector = transform.InverseTransformVector(MoveVector.WithY(0f)) / MoveSpeed;
//		ThisAnimator.SetFloat("HorizontalMovement", LocalMoveVector.x);
//		ThisAnimator.SetFloat("VerticalMovement", LocalMoveVector.z);
//		ThisAnimator.SetFloat("TurnStrength", DestinationYaw - CurrentYaw);

//		// 중력 적용
//		if (!OnGround || Time.time - JumpTime < 0.4f)
//		{
//			MoveVelocityY += Physics.gravity.y * Time.deltaTime;
//		}
//		else
//		{
//			MoveVelocityY = -0.1f;
//		}

//		// 착지
//		if (GroundedThisFrame)
//		{
//			ThisAnimator.SetTrigger("Land");
//			MoveVelocityXZ *= 0.2f;
//		}

//		// 가드
//		ThisAnimator.SetBool("Guard", Inputs.Guard.IsPressed());
//	}

//	public void YawUpdateImmediate()
//	{
//		StartCoroutine(YawUpdateImmediateCoroutine());
//		IEnumerator YawUpdateImmediateCoroutine()
//		{
//			ThisAnimator.applyRootMotion = false;
//			transform.rotation = Quaternion.Euler(0f, DestinationYaw, 0f);
//			yield return null;
//			ThisAnimator.applyRootMotion = true;
//		}

//	}


//	void NormalAttack(CallbackContext context)
//	{
//		if (Guarding) return;
//		ThisAnimator.SetTrigger("NormalAttack");
//	}

//	#region 애니메이션 이벤트에서 호출

//	public void AttackOrCancel()
//	{
//		AfterAttackTime = Time.time;
//		ThisAnimator.ResetTrigger("NormalAttack");
//		ThisAnimator.SetTrigger("CurrentMotionExit");
//	}

//	public BoxCollider AttackHitbox;
//	public GameObject HitParticle;
//	public float DealPushAmount;
//	public void DealDamage()
//	{
//		Collider[] Colliders = Physics.OverlapBox(AttackHitbox.transform.position + AttackHitbox.center, AttackHitbox.size * 0.5f);
//		foreach (Collider OneCollider in Colliders)
//		{
//			if (!OneCollider.CompareTag("Enemy")) continue;
//			Vector3 EffectPoint = OneCollider.ClosestPoint(AttackHitbox.transform.position);
//			Instantiate(HitParticle, EffectPoint, Quaternion.identity);
//			Vector3 TargetMoveVector = OneCollider.transform.position.WithY(0f) - transform.position.WithY(0f);
//			TestCube CubeComponent = OneCollider.GetComponent<TestCube>();
//			CubeComponent.MoveVelocity += TargetMoveVector.normalized * DealPushAmount;
//			CubeComponent.DamageDealt();
//			Vector3 ImpulseVelocity = AttackHitImpulse.m_DefaultVelocity;
//			ImpulseVelocity.x = Random.Range(-ImpulseVelocity.x, ImpulseVelocity.x);
//			AttackHitImpulse.GenerateImpulseWithVelocity(ImpulseVelocity);
//		}
//	}

//	public void Guard()
//	{
//		Guarding = true;
//	}

//	#endregion

//	void Jump(CallbackContext context)
//	{
//		if (!OnGround) return;
//		MoveVelocityY = JumpSpeed;
//		JumpTime = Time.time;
//		ThisAnimator.SetTrigger("Jump");
//	}

//	public void Attack(TimelineAsset AttackTimeline)
//	{
//		IEnumerable<TrackAsset> Tracks = AttackTimeline.GetOutputTracks();
//		foreach (TrackAsset Track in Tracks)
//		{
//			if (Track is ControlTrack)
//			{
//				foreach (TimelineClip OneClip in Track.GetClips())
//				{
//					ControlPlayableAsset ParticleAsset = OneClip.asset as ControlPlayableAsset;
//					ThisDirector.SetReferenceValue(ParticleAsset.sourceGameObject.exposedName, gameObject);
//					ParticleAsset.prefabGameObject.transform.SetParent(null, true);
//				}
//			}
//		}
//		ThisDirector.Play(AttackTimeline);
//	}

//	public void GuardCancel()
//	{
//		Guarding = false;
//	}
//}
