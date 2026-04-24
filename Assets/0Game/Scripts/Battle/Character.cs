using Animancer;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Battle.BattleAttack;
using static Battle.EffectContainer;
using static SingletonManager;
using static UnityEngine.InputSystem.InputAction;
using Random = UnityEngine.Random;

namespace Battle
{
	public partial class Character : MonoBehaviour 
	{
		public bool _ShowAll;

		[BoxGroup("설정")] public string _Name;
		[BoxGroup("설정")] public float _RotationSpeed;
		[BoxGroup("설정")] public float _MoveSpeed;
		[BoxGroup("설정")] public float _MoveAccel;
		[BoxGroup("설정")] public float _ShoulderHeight;
		[BoxGroup("설정")] public float _MaxHP;

		//public VRMBlendShapeProxy _BlendShapeProxy;
		[ShowIf("_ShowAll")] public GameObject _HitEffectPrefab;
		[ShowIf("_ShowAll")] public GameObject _GuardEffectPrefab;
		[ShowIf("_ShowAll")] public ParticleSystem _FeetSmokeLeftEffect, _FeetSmokeRightEffect;
		[ShowIf("_ShowAll")] public Material _WhiteMaterial;

		GameObject _Model;
		[HideInInspector] public Player _Player;
		[HideInInspector] public Enemy _Enemy;
		[HideInInspector] public RaycastHit[] _RaycastResults;
		[HideInInspector] public int _AttackIndex;
		[HideInInspector] public bool _NextAttackAvailable;
		[HideInInspector] public bool _NextAttackInput;
		[HideInInspector] public float _AttackMovePercent;
		float _GuardUpTime, _GuardDownTime;
		[HideInInspector] public float _HitStunTimer;
		[HideInInspector] public State _HitStunPrevState;
		[HideInInspector] public Vector3 _HitStunPrevVelocity;
		[HideInInspector] public bool _IsRunning;
		Coroutine _FeetSmokeCoroutine;
		[HideInInspector] public float _LastSkill1Time, _LastSkill2Time, _LastUltimateTime;
		[HideInInspector] public bool _AlreadyWallJump;
		[HideInInspector] public Quaternion _AimDestRotation;
		[HideInInspector] public Vector3 _MoveInput;
		[HideInInspector] public bool _StopYTrigger;
		[HideInInspector] public EffectContainer _EffectContainer;
		[HideInInspector] public AttackContainer _AttackContainer;
		float _InvincibleTimer;

		public BattleController Controller => BattleController.Instance;
		public Vector3 Center
		{
			get
			{
				if (UseKCC)
				{
					return transform.position + Motor.CharacterTransformToCapsuleCenter;
				}
				else
				{
					return _KCC_Rigidbody._Collider.bounds.center;
				}
			}
		}

		public Vector3 Top
		{
			get
			{
				if (UseKCC)
				{
					return transform.position + Motor.CharacterTransformToCapsuleTop;
				}
				else
				{
					Vector3 pos = Center;
					pos.y = _KCC_Rigidbody._Collider.bounds.max.y;
					return pos;
				}
			}
		}

		public Vector3 Bottom
		{
			get
			{
				if (UseKCC)
				{
					return transform.position + Motor.CharacterTransformToCapsuleBottom;
				}
				else
				{
					Vector3 pos = Center;
					pos.y = _KCC_Rigidbody._Collider.bounds.min.y;
					return pos;
				}
			}
		}

		public void Init()
		{
			_KCC = GetComponent<CharacterController_KCC>();
			_KCC_Rigidbody = GetComponent<CharacterController_Rigidbody>();
			_Player = GetComponent<Player>();
			_Enemy = GetComponent<Enemy>();
			_EffectContainer = GetComponent<EffectContainer>();
			_EffectContainer.Init();
			_AttackContainer = GetComponent<AttackContainer>();
			_AttackContainer.Init();

			_RaycastResults = new RaycastHit[10];
			_RootMotionPosDelta = Vector3.zero;
			_RootMotionRotDelta = Quaternion.identity;

			// 시간 초기화
			_GuardUpTime = Const.TimeDefault;
			_GuardDownTime = Const.TimeDefault;
			_LastSkill1Time = Const.TimeDefault;
			_LastSkill2Time = Const.TimeDefault;
			_LastUltimateTime = Const.TimeDefault;

			// 모델 초기화
			_Model = GetComponentInChildren<Animator>().gameObject;
			Transform parent = _Player ? _Player._CooltimeJitter : transform;
			_Model.transform.SetParent(parent);
			_Animancer = _Model.AddComponent<AnimancerComponent>();
			_Animancer.Animator = _Model.GetComponent<Animator>();
			RedirectRootMotionToCharacter redirect = _Model.AddComponent<RedirectRootMotionToCharacter>();
			redirect._Animator = _Animancer.Animator;
			redirect._Character = this;

			// 컴포넌트 초기화
			if (UseKCC)
			{
				_KCC.Init();
			}
			else
			{
				_KCC_Rigidbody.Init();
			}
			InitFSM();
			_AttackIndex = -1;
			EmitEffect(_FeetSmokeLeftEffect, false);
			EmitEffect(_FeetSmokeRightEffect, false);
			Transform leftFoot = _Animancer.Animator.avatar ? _Animancer.Animator.GetBoneTransform(HumanBodyBones.LeftFoot) : transform;
			_FeetSmokeLeftEffect.transform.SetParent(leftFoot, false);
			Transform rightFoot = _Animancer.Animator.avatar ? _Animancer.Animator.GetBoneTransform(HumanBodyBones.RightFoot) : transform;
			_FeetSmokeRightEffect.transform.SetParent(rightFoot, false);
		}

		public void Init2()
		{
			// 스탯 초기화
			SetHP(_MaxHP);

			//StartCoroutine(Internal());
			//IEnumerator Internal()
			//{
			//	yield return new WaitForSeconds(0.1f);
			//	_BlendShapeProxy.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Neutral), 1f);
			//}
		}

		void Update()
		{
			// 애니메이션
			UpdateFSM();

			// 경직
			_BaseLayer.Speed = IsHitStun() ? 0f : 1f;

			_InvincibleTimer -= Time.deltaTime;
		}

		public void Dash(Direction4 dir)
		{
			_KCC.Dash(dir);
		}

		public void Jump(CallbackContext obj)
		{
			_KCC.Jump();
		}

		public void NormalAttack(CallbackContext obj)
		{
			bool canAttack = CanAttack();
			if (_FSM.CurrentState.IsAttack)
			{
				canAttack &= _FSM.CurrentState._Attack._SkillType <= SkillType.Normal;
			}
			if (!canAttack) return;

			// 저스트 가드
			if (_Player && _Player.IsJustGuard())
			{
				PlayAction(_GuardAttack);
				Attack();
				_Player._JustGuardCancelTrigger = true;
			}
			// 대쉬 
			else if (UseKCC && _KCC.IsDashing())
			{
				_KCC.DashAttack();
			}
			// 지상
			else if (IsGrounded())
			{
				// 1타 공격
				if (_AttackIndex < 0)
				{
					_AttackIndex = 0;
					PlayAction(_NormalAttacks[_AttackIndex]);
					Attack();
				}
				// 2~타 공격
				else
				{
					_NextAttackInput = true;
				}
			}
			// 공중
			else if (_FSM.CurrentState != _JumpAttack) 
			{
				PlayAction(_JumpAttack);
				Attack();
			}
		}

		public void SpecialAttack(CallbackContext obj)
		{
			bool canAttack = CanAttack();
			if (!canAttack) return;

			// 지상
			if (IsGrounded())
			{
				if (_FSM.CurrentState.IsAttack)
				{
					if (_NormalAttacks.Contains(_FSM.CurrentState))
					{
						Play_Canceling(_SpecialAttack, true);
					}
					else return;
				}
				else
				{
					PlayAction(_SpecialAttack);
				}
				Attack();
			}
			// 공중
			else
			{
				if (_FSM.CurrentState.IsAttack) return;
				
				PlayAction(_JumpSpecialAttack);
				Attack();
			}
		}

		public void Skill1(CallbackContext obj)
		{
			bool canAttack = CanAttack();
			if (!canAttack) return;

			// 쿨타임
			if (_Player && Time.time - _LastSkill1Time < _Skill1._Attack._Cooltime)
			{
				_Player.CooltimeJitter();
				return;
			}

			if (_FSM.CurrentState.IsAttack)
			{
				if (_FSM.CurrentState._Attack._SkillType < SkillType.Skill)
				{
					Play_Canceling(_Skill1, true);
				}
				else return;
			}
			else
			{
				PlayAction(_Skill1);
			}
			Attack();
			_LastSkill1Time = Time.time;
		}

		public void Skill2(CallbackContext obj)
		{
			bool canAttack = CanAttack();
			if (!canAttack) return;

			// 쿨타임
			if (_Player && Time.time - _LastSkill2Time < _Skill2._Attack._Cooltime)
			{
				_Player.CooltimeJitter();
				return;
			}

			if (_FSM.CurrentState.IsAttack)
			{
				if (_FSM.CurrentState._Attack._SkillType < SkillType.Skill)
				{
					Play_Canceling(_Skill2, true);
				}
				else return;
			}
			else
			{
				PlayAction(_Skill2);
			}
			Attack();
			_LastSkill2Time = Time.time;
		}

		public void Ultimate(CallbackContext obj)
		{
			bool canAttack = CanAttack();
			if (!canAttack) return;

			// 쿨타임
			if (_Player && Time.time - _LastUltimateTime < _Ultimate._Attack._Cooltime)
			{
				_Player.CooltimeJitter();
				return;
			}

			if (_FSM.CurrentState.IsAttack)
			{
				if (_FSM.CurrentState._Attack._SkillType < SkillType.Skill)
				{
					Play_Canceling(_Ultimate, true);
				}
				else return;
			}
			else
			{
				PlayAction(_Ultimate);
			}
			Attack();
			_LastUltimateTime = Time.time;
		}

		bool CanAttack()
		{
			bool canAttack = _FSM.CurrentState._CanAttack;
			canAttack &= !IsGuarding();
			canAttack &= _FSM.CurrentState != _DashAttack;
			return canAttack;
		}

		public void Guard(CallbackContext obj)
		{
			if (!_FSM.CurrentState._CanGuard) return;

			// 가드 시작
			if (Inputs.Guard.WasPressedThisFrame() && !IsGuarding())
			{
				if (_FSM.CurrentState.IsAttack)
				{
					Play_Canceling(_Idle, false);
				}
				_UpperBodyLayer.SetWeight(1f);
				AnimancerState state = _UpperBodyLayer.Play(_GuardUpAsset);
				state.Time = 0f;
				_GuardUpTime = Time.time;
			}

			// 가드 해제
			if (!Inputs.Guard.IsPressed() && IsGuarding())
			{
				AnimancerState state = _UpperBodyLayer.Play(_GuardDownAsset);
				state.Events(this).OnEnd ??= GuardCancel;
				_GuardDownTime = Time.time;
			}
		}

		public void Attack()
		{
			BattleAttack attack = _FSM.CurrentState._Attack;
			if (!attack)
			{
				Debug.LogWarning($"[{gameObject.name}] [{_FSM.CurrentState}] 공격 설정 없음");
				return;
			}

			switch (attack._RangeType)
			{
				case RangeType.Melee:
					MeleeAttack();
					break;
				case RangeType.Range:
					RangeAttack();
					break;
			}
		}

		void MeleeAttack()
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				State state = _FSM.CurrentState;
				Effect firstEffectData = state._EffectDatas.First();
				List<Effect>.Enumerator effectEnum = state._EffectDatas.GetEnumerator();
				BattleAttack attackData = state._Attack;

				// 공격 최소 딜레이
				yield return new WaitForSeconds(firstEffectData._Delay * 0.3f);

				BattleAttack attack = SpawnAttack(state);
				MeleeAttack melee = attack.GetComponent<MeleeAttack>();
				melee.Init();

				foreach (AttackHit attackHit in melee._AttackHits)
				{
					if (_FSM.CurrentState != state) yield break;

					bool hasNext = effectEnum.MoveNext();
					Effect effectData = hasNext ? effectEnum.Current : firstEffectData;

					// 공중 공격으로 점프
					int raycastCount = 0;
					List<RaycastHit> hits;
					RaycastHit nearest = default;
					if (UseKCC)
					{
						raycastCount = Physics.RaycastNonAlloc(
								origin: Center,
								direction: Motor.CharacterForward,
								results: _RaycastResults,
								maxDistance: melee._Collider.size.z,
								layerMask: Layer.TerrainLayerMask);
						hits = _RaycastResults.ArrayToList(raycastCount);
						nearest = hits.MinBy(x => x.distance);
						if (raycastCount > 0 && attackData._SkillType == SkillType.Normal)
						{
							float angle = Vector3.Angle(Motor.CharacterForward, -nearest.normal);
							bool jump = UseKCC;
							jump &= angle < WallJumpAngleThreshold;
							jump &= !Motor.GroundingStatus.IsStableOnGround;
							if (jump)
							{
								_KCC._AttackJumpDirection = -Motor.CharacterForward;
							}
						}
					}

					// 딜레이
					float delay = melee._AttackHits.IndexOf(attackHit) == 0 ? effectData._Delay * 0.7f : effectData._Delay;
					yield return new WaitForSeconds(delay);

					// 이펙트
					PlayEffect(effectData);

					// 히트 판정
					int overlapCount = Physics.OverlapBoxNonAlloc(
						center: melee._Collider.GetCenter(),
						halfExtents: melee._Collider.size / 2f,
						results: melee._HitResults,
						orientation: attack.transform.rotation,
						mask: GetOppositeAndTerrainLayerMask());

					List<Collider> overlaps = melee._HitResults.ArrayToList(overlapCount);
					overlaps.Sort((a, b) =>
					{
						float aDistance = (a.transform.position - transform.position).sqrMagnitude;
						float bDistance = (b.transform.position - transform.position).sqrMagnitude;
						float v = (aDistance - bDistance) * 100f;
						return (int)v;
					});
					Vector3 shoulder = Bottom;
					shoulder.y += _ShoulderHeight;
					foreach (Collider col in overlaps)
					{
						// 지형 적중
						if (col.gameObject.layer == Layer.TerrainLayer)
						{
							if (col.TryGetComponent(out Fragment fragment))
							{
								Vector3 hitPoint = fragment.GetComponent<MeshCollider>().bounds.ClosestPoint(Center);
								Controller.AddForceToFragment(fragment, 50f, transform.forward);
							}
							Controller.PlayEffect123123(_GuardEffectPrefab, this, nearest.point, Quaternion.identity);
							continue;
						}

						// 상대 적중
						Character targetCharacter = col.GetComponentInParent<Character>();
						if (targetCharacter)
						{
							Vector3 attackDir = targetCharacter.Center - shoulder;
							Bounds bounds = targetCharacter.UseKCC ? targetCharacter._KCC._Motor.Capsule.bounds : targetCharacter._KCC_Rigidbody._Collider.bounds;
							Vector3 hitPoint = bounds.ClosestPoint(Center);
							StartCoroutine(DelayTakeDamage());
							IEnumerator DelayTakeDamage()
							{
								// 공격 판정 딜레이
								Effect effectData = attack._StateInfo._EffectDatas.First();
								float delay = attackHit._HitDelay - effectData._Delay;
								yield return new WaitForSeconds(delay);
								targetCharacter.TakeDamage(attack, attackHit, hitPoint, attackDir);
							}
							yield return new WaitForSeconds(attackHit._HitStunDuration);
						}
					}
				}

				Destroy(attack.gameObject);
			}
		}

		void RangeAttack()
		{
			Controller.StartCoroutine(Internal());
			IEnumerator Internal()
			{
				State state = _FSM.CurrentState;
				BattleAttack attack = SpawnAttack(state);

				// 초기화
				BattleAttack_Missile missile = attack.GetComponent<BattleAttack_Missile>();
				BattleAttack_Laser laser = attack.GetComponent<BattleAttack_Laser>();
				if (missile) { }
				else
				{
					laser.Init();
				}

				// 대기
				Effect effectData = state._EffectDatas.First();
				yield return new WaitForSeconds(effectData._Delay);
				PlayEffect(effectData);

				// 발사
				Coroutine coroutine;
				if (missile)
				{
					coroutine = StartCoroutine(FireMissile(missile));

					IEnumerator FireMissile(BattleAttack_Missile attack)
					{
						for (int i = 0; i < attack._FireCount; i++)
						{
							Missile missile = Instantiate(attack._MissilePrefab);
							missile.Init();
							missile._FireTime = Time.time;
							missile._Attack = attack.GetComponent<BattleAttack>();
							missile._Target = _Enemy ? Controller._ActivePlayer.c : null;
							Vector3 pos = attack._SpawnPoints.PickOne().position;
							Vector3 rot = _AimDestRotation.eulerAngles;
							rot = rot.RandomizeVector(attack._SpreadDegree, attack._SpreadDegree, 0f);
							missile.transform.SetPositionAndRotation(pos, Quaternion.Euler(rot));
							missile._Rigidbody.Move(pos, Quaternion.Euler(rot));

							float delay = attack._FireCount > 1 ? attack._FireDuration / (attack._FireCount - 1) : attack._FireDuration;
							yield return new WaitForSeconds(delay);
						}
					}
				}
				else
				{
					coroutine = StartCoroutine(FireLaser(laser));

					IEnumerator FireLaser(BattleAttack_Laser laser)
					{
						// 발사 대기 중
						laser.ImpactOn(false);
						yield return new WaitForSeconds(laser._Delay);

						// 발사 중
						laser.ImpactOn(true);
						yield return new WaitForSeconds(laser._Duration);
					}
				}
				yield return coroutine;

				Destroy(attack.gameObject);
			}
		}

		public void DestroyMissile(Missile missile)
		{
			Destroy(missile.gameObject);
		}

		public void TakeDamage(BattleAttack attack, AttackHit attackHit, Vector3? hitPoint, Vector3 attackDirection)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				// 이펙트 위치
				Vector3 effectPosition = hitPoint != null ? hitPoint.Value : Center;
				Vector3 damageTextPosition = hitPoint != null ? hitPoint.Value : Top;

				// 주변 적 활성화, 데미지 타임 갱신
				if (_Enemy)
				{
					_Enemy.NoticeAround();
					_Enemy._LastDamagedTime = Time.time;
				}

				// 경직
				Character attacker = attack._Owner;
				SetHitStunTimer(attackHit._HitStunDuration, _FSM.CurrentState);
				attacker.SetHitStunTimer(attackHit._HitStunDuration, attacker._FSM.CurrentState);
				yield return new WaitForSeconds(attackHit._HitStunDuration);

				// 가드 판정
				float damage = !IsInvincible() ? attackHit._Damage : 0f;
				bool guard = IsGuardingEffective();
				float angle = Vector3.Angle(transform.forward, new(-attackDirection.x, 0f, -attackDirection.z));
				guard &= angle < 90f;
				if (guard)
				{
					// 저스트 가드
					bool justGuard = Time.time - _GuardUpTime < 0.3f;
					justGuard &= Inputs.Guard.IsPressed();
					justGuard &= attack._RangeType == RangeType.Melee;
					justGuard &= _Player;
					if (justGuard)
					{
						GuardCancel();
						_Player.JustGuard();
						damage = 0f;
					}
					// 일반 가드
					else
					{
						_FadeInDeaccelTimer = 0.6f;
						damage *= attack._AreaType == AreaType.Single ? 0f : 0.5f;
					}

					// 이펙트
					Controller.PlayEffect123123(_GuardEffectPrefab, this, effectPosition, Quaternion.LookRotation(attackDirection));
				}
				else
				{
					bool playDamageAnim = attackHit._DamageDuration > 0f && damage > 0f;

					// 일반 경직
					if (attackHit._ForceForward == 0f && attackHit._ForceUp == 0f)
					{
						_FadeInDeaccelTimer = attackHit._DeaccelDuration;
						if (playDamageAnim)
						{
							_Damage._Duration = attackHit._DamageDuration;
							_Damage.SetAsset(_DamageAssets.PickOne());
							PlayAction(_Damage);
						}
					}
					// 밀어내기
					else if (attackHit._ForceForward != 0f && attackHit._ForceUp == 0f)
					{
						if (attackHit._DeaccelDuration == 0f)
						{
							Debug.LogWarning($"{attack._Name} 공격 : 밀치기 공격에 DeaccelDuration 설정 필요");
						}
						_FadeOutDeaccelTimer = attackHit._DeaccelDuration;
						_Impulse = new(0f, 0f, attackHit._ForceForward);
						_Impulse = Quaternion.LookRotation(attackDirection) * _Impulse;
						FeetSmoke(attackHit._DeaccelDuration);
						if (playDamageAnim)
						{
							_Damage.SetAsset(_DamageAssets.PickOne());
							PlayAction(_Damage);
						}
					}
					// 날리기
					else
					{
						if (playDamageAnim)
						{
							_Impulse = new(0f, attackHit._ForceUp, attackHit._ForceForward);
							_Impulse = Quaternion.LookRotation(attackDirection) * _Impulse;
							_FSM.TrySetState(_GetDown);
						}
					}

					// 이펙트
					Controller.PlayEffect123123(attackHit._HitEffectPrefab, attacker, effectPosition, Quaternion.LookRotation(attackDirection));
					Controller.PlayEffect123123(_HitEffectPrefab, this, effectPosition, Quaternion.LookRotation(attackDirection));
				}

				// 데미지 표시
				if (!IsDead())
				{
					SetHP(_HP - damage);
					if (_Enemy)
					{
						Controller.ShowDamageText(damage, damageTextPosition);
						Controller.ShakeCamera(attackHit._ShakeCameraDuration);
					}
					else
					{
						if (damage > 0f)
						{
							Controller.ShowDamageScreen();
						}
					}
				}

				// 쓰러짐
				if (IsDead())
				{
					_FSM.TrySetState(_Die);
					if (UseKCC)
					{
						Motor.Capsule.enabled = false;
					}
					else
					{
						_KCC_Rigidbody._Collider.enabled = false;
						_KCC_Rigidbody.ActivateFragment();
					}
					if (_Enemy)
					{
						Controller.RemoveEnemyHPUI(_Enemy);
					}
					Controller.RemoveMinimapMarker(this);
				}

				// 특수 공격으로 점프
				bool wallJump = attack._StateInfo == attacker._JumpSpecialAttack;
				wallJump &= !attacker._AlreadyWallJump;
				if (wallJump)
				{
					if (attackHit._HitStunDuration > 0f)
					{
						Debug.LogWarning($"{attack._Name} 공격 : 공중 특수 공격에서 HitStunDuration 0으로 설정 필요");
					}

					attacker._AlreadyWallJump = true;
					Vector3 jumpDir = attacker.Center - Center;
					jumpDir.y = 0f;
					attacker._KCC._AttackJumpDirection = jumpDir.normalized;
				}
			}
		}

		void PlayEffect(Effect info)
		{
			StartCoroutine(Internal());
			IEnumerator Internal()
			{
				GameObject effect = Instantiate(info._EffectPrefab);
				BattleEffect e = effect.AddComponent<BattleEffect>();
				e._Owner = this;
				e.Init();
				Data.SetupEffectPosition(effect, info, transform);
				ParticleSystem.MainModule main = effect.GetComponent<ParticleSystem>().main;
				yield return new WaitForSeconds(main.duration);
				Destroy(effect);
			}
		}

		public bool IsOpposite(Character c)
		{
			if (_Player) return c._Enemy;
			else return c._Player;
		}

		public LayerMask GetOppositeLayer()
		{
			if (gameObject.layer == Layer.PlayerLayer) return Layer.EnemyLayer;
			else return Layer.PlayerLayer;
		}

		public LayerMask GetOppositeLayerMask()
		{
			if (gameObject.layer == Layer.PlayerLayer) return Layer.EnemyLayerMask;
			else return Layer.PlayerLayerMask;
		}

		public LayerMask GetOppositeAndTerrainLayerMask()
		{
			LayerMask layerMask = gameObject.layer == Layer.PlayerLayer ? Layer.EnemyLayerMask : Layer.PlayerLayerMask;
			layerMask |= Layer.TerrainLayerMask;
			return layerMask;
		}

		public bool IsMovable()
		{
			bool movable = true;
			movable &= _FSM.CurrentState != _Damage;
			movable &= _FSM.CurrentState != _GetDown;
			movable &= _FSM.CurrentState != _GetUp;
			movable &= _FSM.CurrentState != _Die;
			return movable;
		}

		public bool IsGrounded()
		{
			if (UseKCC)
			{
				return Motor.GroundingStatus.IsStableOnGround && !Motor.MustUnground();
			}
			else
			{
				return true;
			}
		}

		public bool IsDead()
		{
			return _HP <= 0f;
		}

		public bool IsGuarding()
		{
			return _UpperBodyLayer.Weight > 0f;
		}

		void GuardCancel()
		{
			_UpperBodyLayer.SetWeight(0f);
		}

		bool IsInvincible()
		{
			return _InvincibleTimer > 0f;
		}

		bool IsGuardingEffective()
		{
			if (!_Player) return false;

			bool guard = _UpperBodyLayer.Weight > 0f;
			AnimancerState guardDownState = _UpperBodyLayer.GetOrCreateState(_GuardDownAsset.Transition);
			guard &= _UpperBodyLayer.CurrentState != guardDownState;

			// 가드 해제 그레이스 타임
			guard |= Time.time - _GuardDownTime < 0.1f;

			return guard;
		}

		public bool IsHitStun()
		{
			return _HitStunTimer > 0f && _HitStunPrevState == _FSM.CurrentState;
		}

		public void SetHitStunTimer(float t, State prevState)
		{
			if (t == 0f) return;

			if (_HitStunTimer <= 0f)
			{
				_HitStunPrevState = prevState;
				_HitStunPrevVelocity = UseKCC ? Motor.Velocity : Vector3.zero;
			}
			_HitStunTimer = t;
		}

		void FeetSmoke(float time)
		{
			if (_FeetSmokeCoroutine != null)
			{
				StopCoroutine(_FeetSmokeCoroutine);
				_FeetSmokeCoroutine = null;
			}
			_FeetSmokeCoroutine = StartCoroutine(Internal());

			IEnumerator Internal()
			{
				EmitEffect(_FeetSmokeLeftEffect, true);
				EmitEffect(_FeetSmokeRightEffect, true);
				float start = Time.time;
				yield return new WaitUntil(() => Time.time - start > time || !IsGrounded());
				EmitEffect(_FeetSmokeLeftEffect, false);
				EmitEffect(_FeetSmokeRightEffect, false);
				_FeetSmokeCoroutine = null;
			}
		}

		public void EmitEffect(ParticleSystem particle, bool on)
		{
			ParticleSystem[] particles = particle.GetComponentsInChildren<ParticleSystem>();
			foreach (ParticleSystem p in particles)
			{
				ParticleSystem.EmissionModule emission = p.emission;
				emission.enabled = on;
				if (on)
				{
					p.time = 0f;
					p.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
					p.Play();
				}
			}
		}

		public void SetPositionAndRotation(Vector3 pos, Quaternion rot)
		{
			_AimDestRotation = rot;
			if (UseKCC)
			{
				Motor.SetPositionAndRotation(pos, rot);
				Motor.MoveCharacter(pos);
				Motor.RotateCharacter(rot);
			}
			else
			{
				_KCC_Rigidbody._Mover.SetPositionAndRotation(pos, rot);
			}

			if (_Enemy)
			{
				_Enemy._Agent.Warp(pos);
			}
		}

		BattleAttack SpawnAttack(State state)
		{
			BattleAttack attack = Instantiate(state._Attack, transform);
			attack.gameObject.SetActive(true);
			attack._Owner = this;
			attack._StateInfo = state;
			return attack;
		}

		float GetSpeedMultiplier()
		{
			BattleAttack attack = _FSM.CurrentState._Attack;
			if (attack) return attack._SpeedMultiplier;
			else return 1f;
		}
	}
}
