using System;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour, IDestructible, Hoverable, IWaterInteractable
{
	protected virtual void Awake()
	{
		Character.m_characters.Add(this);
		this.m_collider = base.GetComponent<CapsuleCollider>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_zanim = base.GetComponent<ZSyncAnimation>();
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_animator = base.GetComponentInChildren<Animator>();
		this.m_animEvent = this.m_animator.GetComponent<CharacterAnimEvent>();
		this.m_baseAI = base.GetComponent<BaseAI>();
		this.m_animator.logWarnings = false;
		this.m_visual = base.transform.Find("Visual").gameObject;
		this.m_lodGroup = this.m_visual.GetComponent<LODGroup>();
		this.m_head = this.m_animator.GetBoneTransform(HumanBodyBones.Head);
		this.m_body.maxDepenetrationVelocity = 2f;
		if (Character.m_smokeRayMask == 0)
		{
			Character.m_smokeRayMask = LayerMask.GetMask(new string[]
			{
				"smoke"
			});
			Character.m_characterLayer = LayerMask.NameToLayer("character");
			Character.m_characterNetLayer = LayerMask.NameToLayer("character_net");
			Character.m_characterGhostLayer = LayerMask.NameToLayer("character_ghost");
		}
		if (Character.forward_speed == 0)
		{
			Character.forward_speed = ZSyncAnimation.GetHash("forward_speed");
			Character.sideway_speed = ZSyncAnimation.GetHash("sideway_speed");
			Character.turn_speed = ZSyncAnimation.GetHash("turn_speed");
			Character.inWater = ZSyncAnimation.GetHash("inWater");
			Character.onGround = ZSyncAnimation.GetHash("onGround");
			Character.encumbered = ZSyncAnimation.GetHash("encumbered");
			Character.flying = ZSyncAnimation.GetHash("flying");
		}
		if (this.m_lodGroup)
		{
			this.m_originalLocalRef = this.m_lodGroup.localReferencePoint;
		}
		this.m_seman = new SEMan(this, this.m_nview);
		if (this.m_nview.GetZDO() != null)
		{
			if (!this.IsPlayer())
			{
				if (this.m_nview.IsOwner() && this.GetHealth() == this.GetMaxHealth())
				{
					this.SetupMaxHealth();
				}
				this.m_tamed = this.m_nview.GetZDO().GetBool("tamed", this.m_tamed);
				this.m_level = this.m_nview.GetZDO().GetInt("level", 1);
			}
			this.m_nview.Register<HitData>("Damage", new Action<long, HitData>(this.RPC_Damage));
			this.m_nview.Register<float, bool>("Heal", new Action<long, float, bool>(this.RPC_Heal));
			this.m_nview.Register<float>("AddNoise", new Action<long, float>(this.RPC_AddNoise));
			this.m_nview.Register<Vector3>("Stagger", new Action<long, Vector3>(this.RPC_Stagger));
			this.m_nview.Register("ResetCloth", new Action<long>(this.RPC_ResetCloth));
			this.m_nview.Register<bool>("SetTamed", new Action<long, bool>(this.RPC_SetTamed));
		}
	}

	private void SetupMaxHealth()
	{
		int level = this.GetLevel();
		float difficultyHealthScale = Game.instance.GetDifficultyHealthScale(base.transform.position);
		this.SetMaxHealth(this.m_health * difficultyHealthScale * (float)level);
	}

	protected virtual void Start()
	{
		this.m_nview.GetZDO();
	}

	public virtual void OnDestroy()
	{
		this.m_seman.OnDestroy();
		Character.m_characters.Remove(this);
	}

	public void SetLevel(int level)
	{
		if (level < 1)
		{
			return;
		}
		this.m_level = level;
		this.m_nview.GetZDO().Set("level", level);
		this.SetupMaxHealth();
		if (this.m_onLevelSet != null)
		{
			this.m_onLevelSet(this.m_level);
		}
	}

	public int GetLevel()
	{
		return this.m_level;
	}

	public virtual bool IsPlayer()
	{
		return false;
	}

	public Character.Faction GetFaction()
	{
		return this.m_faction;
	}

	protected virtual void FixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		this.UpdateLayer();
		this.UpdateContinousEffects();
		this.UpdateWater(fixedDeltaTime);
		this.UpdateGroundTilt(fixedDeltaTime);
		this.SetVisible(this.m_nview.HasOwner());
		if (this.m_nview.IsOwner())
		{
			this.UpdateGroundContact(fixedDeltaTime);
			this.UpdateNoise(fixedDeltaTime);
			this.m_seman.Update(fixedDeltaTime);
			this.UpdateStagger(fixedDeltaTime);
			this.UpdatePushback(fixedDeltaTime);
			this.UpdateMotion(fixedDeltaTime);
			this.UpdateSmoke(fixedDeltaTime);
			this.UnderWorldCheck(fixedDeltaTime);
			this.SyncVelocity();
			this.CheckDeath();
		}
	}

	private void UpdateLayer()
	{
		if (this.m_collider.gameObject.layer == Character.m_characterLayer || this.m_collider.gameObject.layer == Character.m_characterNetLayer)
		{
			if (this.m_nview.IsOwner())
			{
				this.m_collider.gameObject.layer = (this.IsAttached() ? Character.m_characterNetLayer : Character.m_characterLayer);
				return;
			}
			this.m_collider.gameObject.layer = Character.m_characterNetLayer;
		}
	}

	private void UnderWorldCheck(float dt)
	{
		if (this.IsDead())
		{
			return;
		}
		this.m_underWorldCheckTimer += dt;
		if (this.m_underWorldCheckTimer > 5f || this.IsPlayer())
		{
			this.m_underWorldCheckTimer = 0f;
			float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
			if (base.transform.position.y < groundHeight - 1f)
			{
				Vector3 position = base.transform.position;
				position.y = groundHeight + 0.5f;
				base.transform.position = position;
				this.m_body.position = position;
				this.m_body.velocity = Vector3.zero;
			}
		}
	}

	private void UpdateSmoke(float dt)
	{
		if (this.m_tolerateSmoke)
		{
			return;
		}
		this.m_smokeCheckTimer += dt;
		if (this.m_smokeCheckTimer > 2f)
		{
			this.m_smokeCheckTimer = 0f;
			if (Physics.CheckSphere(this.GetTopPoint() + Vector3.up * 0.1f, 0.5f, Character.m_smokeRayMask))
			{
				this.m_seman.AddStatusEffect("Smoked", true);
				return;
			}
			this.m_seman.RemoveStatusEffect("Smoked", true);
		}
	}

	private void UpdateContinousEffects()
	{
		this.SetupContinousEffect(base.transform.position, this.m_sliding, this.m_slideEffects, ref this.m_slideEffects_instances);
		Vector3 position = base.transform.position;
		position.y = this.m_waterLevel + 0.05f;
		this.SetupContinousEffect(position, this.InWater(), this.m_waterEffects, ref this.m_waterEffects_instances);
	}

	private void SetupContinousEffect(Vector3 point, bool enabled, EffectList effects, ref GameObject[] instances)
	{
		if (!effects.HasEffects())
		{
			return;
		}
		if (!enabled)
		{
			if (instances != null)
			{
				foreach (GameObject gameObject in instances)
				{
					if (gameObject)
					{
						foreach (ParticleSystem particleSystem in gameObject.GetComponentsInChildren<ParticleSystem>())
						{
							particleSystem.emission.enabled = false;
							particleSystem.Stop();
						}
						CamShaker componentInChildren = gameObject.GetComponentInChildren<CamShaker>();
						if (componentInChildren)
						{
							UnityEngine.Object.Destroy(componentInChildren);
						}
						ZSFX componentInChildren2 = gameObject.GetComponentInChildren<ZSFX>();
						if (componentInChildren2)
						{
							componentInChildren2.FadeOut();
						}
						TimedDestruction component = gameObject.GetComponent<TimedDestruction>();
						if (component)
						{
							component.Trigger();
						}
						else
						{
							UnityEngine.Object.Destroy(gameObject);
						}
					}
				}
				instances = null;
			}
			return;
		}
		if (instances == null)
		{
			instances = effects.Create(point, Quaternion.identity, base.transform, 1f);
			return;
		}
		foreach (GameObject gameObject2 in instances)
		{
			if (gameObject2)
			{
				gameObject2.transform.position = point;
			}
		}
	}

	protected virtual void OnSwiming(Vector3 targetVel, float dt)
	{
	}

	protected virtual void OnSneaking(float dt)
	{
	}

	protected virtual void OnJump()
	{
	}

	protected virtual bool TakeInput()
	{
		return true;
	}

	private float GetSlideAngle()
	{
		if (!this.IsPlayer())
		{
			return 90f;
		}
		return 38f;
	}

	private void ApplySlide(float dt, ref Vector3 currentVel, Vector3 bodyVel, bool running)
	{
		bool flag = this.CanWallRun();
		float num = Mathf.Acos(Mathf.Clamp01(this.m_lastGroundNormal.y)) * 57.29578f;
		Vector3 lastGroundNormal = this.m_lastGroundNormal;
		lastGroundNormal.y = 0f;
		lastGroundNormal.Normalize();
		Vector3 velocity = this.m_body.velocity;
		Vector3 rhs = Vector3.Cross(this.m_lastGroundNormal, Vector3.up);
		Vector3 a = Vector3.Cross(this.m_lastGroundNormal, rhs);
		bool flag2 = currentVel.magnitude > 0.1f;
		if (num > this.GetSlideAngle())
		{
			if (running && flag && flag2)
			{
				this.UseStamina(10f * dt);
				this.m_slippage = 0f;
				this.m_wallRunning = true;
			}
			else
			{
				this.m_slippage = Mathf.MoveTowards(this.m_slippage, 1f, 1f * dt);
			}
			Vector3 b = a * 5f;
			currentVel = Vector3.Lerp(currentVel, b, this.m_slippage);
			this.m_sliding = (this.m_slippage > 0.5f);
			return;
		}
		this.m_slippage = 0f;
	}

	private void UpdateMotion(float dt)
	{
		this.UpdateBodyFriction();
		this.m_sliding = false;
		this.m_wallRunning = false;
		this.m_running = false;
		if (this.IsDead())
		{
			return;
		}
		if (this.IsDebugFlying())
		{
			this.UpdateDebugFly(dt);
			return;
		}
		if (this.InIntro())
		{
			this.m_maxAirAltitude = base.transform.position.y;
			this.m_body.velocity = Vector3.zero;
			this.m_body.angularVelocity = Vector3.zero;
		}
		if (!this.InWaterSwimDepth() && !this.IsOnGround())
		{
			float y = base.transform.position.y;
			this.m_maxAirAltitude = Mathf.Max(this.m_maxAirAltitude, y);
		}
		if (this.IsSwiming())
		{
			this.UpdateSwiming(dt);
		}
		else if (this.m_flying)
		{
			this.UpdateFlying(dt);
		}
		else
		{
			this.UpdateWalking(dt);
		}
		this.m_lastGroundTouch += Time.fixedDeltaTime;
		this.m_jumpTimer += Time.fixedDeltaTime;
	}

	private void UpdateDebugFly(float dt)
	{
		float num = (float)(this.m_run ? 50 : 20);
		Vector3 b = this.m_moveDir * num;
		if (this.TakeInput())
		{
			if (ZInput.GetButton("Jump"))
			{
				b.y = num;
			}
			else if (Input.GetKey(KeyCode.LeftControl))
			{
				b.y = -num;
			}
		}
		this.m_currentVel = Vector3.Lerp(this.m_currentVel, b, 0.5f);
		this.m_body.velocity = this.m_currentVel;
		this.m_body.useGravity = false;
		this.m_lastGroundTouch = 0f;
		this.m_maxAirAltitude = base.transform.position.y;
		this.m_body.rotation = Quaternion.RotateTowards(base.transform.rotation, this.m_lookYaw, this.m_turnSpeed * dt);
		this.m_body.angularVelocity = Vector3.zero;
		this.UpdateEyeRotation();
	}

	private void UpdateSwiming(float dt)
	{
		bool flag = this.IsOnGround();
		if (Mathf.Max(0f, this.m_maxAirAltitude - base.transform.position.y) > 0.5f && this.m_onLand != null)
		{
			this.m_onLand(new Vector3(base.transform.position.x, this.m_waterLevel, base.transform.position.z));
		}
		this.m_maxAirAltitude = base.transform.position.y;
		float d = this.m_swimSpeed * this.GetAttackSpeedFactorMovement();
		if (this.InMinorAction())
		{
			d = 0f;
		}
		this.m_seman.ApplyStatusEffectSpeedMods(ref d);
		Vector3 vector = this.m_moveDir * d;
		if (vector.magnitude > 0f && this.IsOnGround())
		{
			vector = Vector3.ProjectOnPlane(vector, this.m_lastGroundNormal).normalized * vector.magnitude;
		}
		if (this.IsPlayer())
		{
			this.m_currentVel = Vector3.Lerp(this.m_currentVel, vector, this.m_swimAcceleration);
		}
		else
		{
			float num = vector.magnitude;
			float magnitude = this.m_currentVel.magnitude;
			if (num > magnitude)
			{
				num = Mathf.MoveTowards(magnitude, num, this.m_swimAcceleration);
				vector = vector.normalized * num;
			}
			this.m_currentVel = Vector3.Lerp(this.m_currentVel, vector, 0.5f);
		}
		if (vector.magnitude > 0.1f)
		{
			this.AddNoise(15f);
		}
		this.AddPushbackForce(ref this.m_currentVel);
		Vector3 force = this.m_currentVel - this.m_body.velocity;
		force.y = 0f;
		if (force.magnitude > 20f)
		{
			force = force.normalized * 20f;
		}
		this.m_body.AddForce(force, ForceMode.VelocityChange);
		float num2 = this.m_waterLevel - this.m_swimDepth;
		if (base.transform.position.y < num2)
		{
			float t = Mathf.Clamp01((num2 - base.transform.position.y) / 2f);
			float target = Mathf.Lerp(0f, 10f, t);
			Vector3 velocity = this.m_body.velocity;
			velocity.y = Mathf.MoveTowards(velocity.y, target, 50f * dt);
			this.m_body.velocity = velocity;
		}
		else
		{
			float t2 = Mathf.Clamp01(-(num2 - base.transform.position.y) / 1f);
			float num3 = Mathf.Lerp(0f, 10f, t2);
			Vector3 velocity2 = this.m_body.velocity;
			velocity2.y = Mathf.MoveTowards(velocity2.y, -num3, 30f * dt);
			this.m_body.velocity = velocity2;
		}
		float target2 = 0f;
		if (this.m_moveDir.magnitude > 0.1f || this.AlwaysRotateCamera())
		{
			float swimTurnSpeed = this.m_swimTurnSpeed;
			this.m_seman.ApplyStatusEffectSpeedMods(ref swimTurnSpeed);
			target2 = this.UpdateRotation(swimTurnSpeed, dt);
		}
		this.m_body.angularVelocity = Vector3.zero;
		this.UpdateEyeRotation();
		this.m_body.useGravity = true;
		float num4 = Vector3.Dot(this.m_currentVel, base.transform.forward);
		float value = Vector3.Dot(this.m_currentVel, base.transform.right);
		float num5 = Vector3.Dot(this.m_body.velocity, base.transform.forward);
		this.m_currentTurnVel = Mathf.SmoothDamp(this.m_currentTurnVel, target2, ref this.m_currentTurnVelChange, 0.5f, 99f);
		this.m_zanim.SetFloat(Character.forward_speed, this.IsPlayer() ? num4 : num5);
		this.m_zanim.SetFloat(Character.sideway_speed, value);
		this.m_zanim.SetFloat(Character.turn_speed, this.m_currentTurnVel);
		this.m_zanim.SetBool(Character.inWater, !flag);
		this.m_zanim.SetBool(Character.onGround, false);
		this.m_zanim.SetBool(Character.encumbered, false);
		this.m_zanim.SetBool(Character.flying, false);
		if (!flag)
		{
			this.OnSwiming(vector, dt);
		}
	}

	private void UpdateFlying(float dt)
	{
		float d = (this.m_run ? this.m_flyFastSpeed : this.m_flySlowSpeed) * this.GetAttackSpeedFactorMovement();
		Vector3 b = this.CanMove() ? (this.m_moveDir * d) : Vector3.zero;
		this.m_currentVel = Vector3.Lerp(this.m_currentVel, b, this.m_acceleration);
		this.m_maxAirAltitude = base.transform.position.y;
		this.ApplyRootMotion(ref this.m_currentVel);
		this.AddPushbackForce(ref this.m_currentVel);
		Vector3 force = this.m_currentVel - this.m_body.velocity;
		if (force.magnitude > 20f)
		{
			force = force.normalized * 20f;
		}
		this.m_body.AddForce(force, ForceMode.VelocityChange);
		float target = 0f;
		if ((this.m_moveDir.magnitude > 0.1f || this.AlwaysRotateCamera()) && !this.InDodge() && this.CanMove())
		{
			float flyTurnSpeed = this.m_flyTurnSpeed;
			this.m_seman.ApplyStatusEffectSpeedMods(ref flyTurnSpeed);
			target = this.UpdateRotation(flyTurnSpeed, dt);
		}
		this.m_body.angularVelocity = Vector3.zero;
		this.UpdateEyeRotation();
		this.m_body.useGravity = false;
		float num = Vector3.Dot(this.m_currentVel, base.transform.forward);
		float value = Vector3.Dot(this.m_currentVel, base.transform.right);
		float num2 = Vector3.Dot(this.m_body.velocity, base.transform.forward);
		this.m_currentTurnVel = Mathf.SmoothDamp(this.m_currentTurnVel, target, ref this.m_currentTurnVelChange, 0.5f, 99f);
		this.m_zanim.SetFloat(Character.forward_speed, this.IsPlayer() ? num : num2);
		this.m_zanim.SetFloat(Character.sideway_speed, value);
		this.m_zanim.SetFloat(Character.turn_speed, this.m_currentTurnVel);
		this.m_zanim.SetBool(Character.inWater, false);
		this.m_zanim.SetBool(Character.onGround, false);
		this.m_zanim.SetBool(Character.encumbered, false);
		this.m_zanim.SetBool(Character.flying, true);
	}

	private void UpdateWalking(float dt)
	{
		Vector3 moveDir = this.m_moveDir;
		bool flag = this.IsCrouching();
		this.m_running = this.CheckRun(moveDir, dt);
		float num = this.m_speed * this.GetJogSpeedFactor();
		if ((this.m_walk || this.InMinorAction()) && !flag)
		{
			num = this.m_walkSpeed;
		}
		else if (this.m_running)
		{
			bool flag2 = this.InWaterDepth() > 0.4f;
			float num2 = this.m_runSpeed * this.GetRunSpeedFactor();
			num = (flag2 ? Mathf.Lerp(num, num2, 0.33f) : num2);
			if (this.IsPlayer() && moveDir.magnitude > 0f)
			{
				moveDir.Normalize();
			}
		}
		else if (flag || this.IsEncumbered())
		{
			num = this.m_crouchSpeed;
		}
		num *= this.GetAttackSpeedFactorMovement();
		this.m_seman.ApplyStatusEffectSpeedMods(ref num);
		Vector3 vector = this.CanMove() ? (moveDir * num) : Vector3.zero;
		if (vector.magnitude > 0f && this.IsOnGround())
		{
			vector = Vector3.ProjectOnPlane(vector, this.m_lastGroundNormal).normalized * vector.magnitude;
		}
		float num3 = vector.magnitude;
		float magnitude = this.m_currentVel.magnitude;
		if (num3 > magnitude)
		{
			num3 = Mathf.MoveTowards(magnitude, num3, this.m_acceleration);
			vector = vector.normalized * num3;
		}
		else if (this.IsPlayer())
		{
			num3 = Mathf.MoveTowards(magnitude, num3, this.m_acceleration * 2f);
			vector = ((vector.magnitude > 0f) ? (vector.normalized * num3) : (this.m_currentVel.normalized * num3));
		}
		this.m_currentVel = Vector3.Lerp(this.m_currentVel, vector, 0.5f);
		Vector3 velocity = this.m_body.velocity;
		Vector3 currentVel = this.m_currentVel;
		currentVel.y = velocity.y;
		if (this.IsOnGround() && this.m_lastAttachBody == null)
		{
			this.ApplySlide(dt, ref currentVel, velocity, this.m_running);
		}
		this.ApplyRootMotion(ref currentVel);
		this.AddPushbackForce(ref currentVel);
		this.ApplyGroundForce(ref currentVel, vector);
		Vector3 vector2 = currentVel - velocity;
		if (!this.IsOnGround())
		{
			if (vector.magnitude > 0.1f)
			{
				vector2 *= this.m_airControl;
			}
			else
			{
				vector2 = Vector3.zero;
			}
		}
		if (this.IsAttached())
		{
			vector2 = Vector3.zero;
		}
		if (this.IsSneaking())
		{
			this.OnSneaking(dt);
		}
		if (vector2.magnitude > 20f)
		{
			vector2 = vector2.normalized * 20f;
		}
		if (vector2.magnitude > 0.01f)
		{
			this.m_body.AddForce(vector2, ForceMode.VelocityChange);
		}
		if (this.m_lastGroundBody && this.m_lastGroundBody.gameObject.layer != base.gameObject.layer && this.m_lastGroundBody.mass > this.m_body.mass)
		{
			float d = this.m_body.mass / this.m_lastGroundBody.mass;
			this.m_lastGroundBody.AddForceAtPosition(-vector2 * d, base.transform.position, ForceMode.VelocityChange);
		}
		float target = 0f;
		if ((moveDir.magnitude > 0.1f || this.AlwaysRotateCamera()) && !this.InDodge() && this.CanMove())
		{
			float turnSpeed = this.m_run ? this.m_runTurnSpeed : this.m_turnSpeed;
			this.m_seman.ApplyStatusEffectSpeedMods(ref turnSpeed);
			target = this.UpdateRotation(turnSpeed, dt);
		}
		this.UpdateEyeRotation();
		this.m_body.useGravity = true;
		float num4 = Vector3.Dot(this.m_currentVel, Vector3.ProjectOnPlane(base.transform.forward, this.m_lastGroundNormal).normalized);
		float value = Vector3.Dot(this.m_currentVel, Vector3.ProjectOnPlane(base.transform.right, this.m_lastGroundNormal).normalized);
		float num5 = Vector3.Dot(this.m_body.velocity, base.transform.forward);
		this.m_currentTurnVel = Mathf.SmoothDamp(this.m_currentTurnVel, target, ref this.m_currentTurnVelChange, 0.5f, 99f);
		this.m_zanim.SetFloat(Character.forward_speed, this.IsPlayer() ? num4 : num5);
		this.m_zanim.SetFloat(Character.sideway_speed, value);
		this.m_zanim.SetFloat(Character.turn_speed, this.m_currentTurnVel);
		this.m_zanim.SetBool(Character.inWater, false);
		this.m_zanim.SetBool(Character.onGround, this.IsOnGround());
		this.m_zanim.SetBool(Character.encumbered, this.IsEncumbered());
		this.m_zanim.SetBool(Character.flying, false);
		if (this.m_currentVel.magnitude > 0.1f)
		{
			if (this.m_running)
			{
				this.AddNoise(30f);
				return;
			}
			if (!flag)
			{
				this.AddNoise(15f);
			}
		}
	}

	public bool IsSneaking()
	{
		return this.IsCrouching() && this.m_currentVel.magnitude > 0.1f && this.IsOnGround();
	}

	private float GetSlopeAngle()
	{
		if (!this.IsOnGround())
		{
			return 0f;
		}
		float num = Vector3.SignedAngle(base.transform.forward, this.m_lastGroundNormal, base.transform.right);
		return -(90f - -num);
	}

	protected void AddPushbackForce(ref Vector3 velocity)
	{
		if (this.m_pushForce != Vector3.zero)
		{
			Vector3 normalized = this.m_pushForce.normalized;
			float num = Vector3.Dot(normalized, velocity);
			if (num < 10f)
			{
				velocity += normalized * (10f - num);
			}
			if (this.IsSwiming() || this.m_flying)
			{
				velocity *= 0.5f;
			}
		}
	}

	private void ApplyPushback(HitData hit)
	{
		if (hit.m_pushForce != 0f)
		{
			float d = Mathf.Min(40f, hit.m_pushForce / this.m_body.mass * 5f);
			Vector3 pushForce = hit.m_dir * d;
			pushForce.y = 0f;
			if (this.m_pushForce.magnitude < pushForce.magnitude)
			{
				this.m_pushForce = pushForce;
			}
		}
	}

	private void UpdatePushback(float dt)
	{
		this.m_pushForce = Vector3.MoveTowards(this.m_pushForce, Vector3.zero, 100f * dt);
	}

	private void ApplyGroundForce(ref Vector3 vel, Vector3 targetVel)
	{
		Vector3 vector = Vector3.zero;
		if (this.IsOnGround() && this.m_lastGroundBody)
		{
			vector = this.m_lastGroundBody.GetPointVelocity(base.transform.position);
			vector.y = 0f;
		}
		Ship standingOnShip = this.GetStandingOnShip();
		if (standingOnShip != null)
		{
			if (targetVel.magnitude > 0.01f)
			{
				this.m_lastAttachBody = null;
			}
			else if (this.m_lastAttachBody != this.m_lastGroundBody)
			{
				this.m_lastAttachBody = this.m_lastGroundBody;
				this.m_lastAttachPos = this.m_lastAttachBody.transform.InverseTransformPoint(this.m_body.position);
			}
			if (this.m_lastAttachBody)
			{
				Vector3 vector2 = this.m_lastAttachBody.transform.TransformPoint(this.m_lastAttachPos);
				Vector3 a = vector2 - this.m_body.position;
				if (a.magnitude < 4f)
				{
					Vector3 position = vector2;
					position.y = this.m_body.position.y;
					if (standingOnShip.IsOwner())
					{
						a.y = 0f;
						vector += a * 10f;
					}
					else
					{
						this.m_body.position = position;
					}
				}
				else
				{
					this.m_lastAttachBody = null;
				}
			}
		}
		else
		{
			this.m_lastAttachBody = null;
		}
		vel += vector;
	}

	private float UpdateRotation(float turnSpeed, float dt)
	{
		Quaternion quaternion = this.AlwaysRotateCamera() ? this.m_lookYaw : Quaternion.LookRotation(this.m_moveDir);
		float yawDeltaAngle = Utils.GetYawDeltaAngle(base.transform.rotation, quaternion);
		float num = 1f;
		if (!this.IsPlayer())
		{
			num = Mathf.Clamp01(Mathf.Abs(yawDeltaAngle) / 90f);
			num = Mathf.Pow(num, 0.5f);
		}
		float num2 = turnSpeed * this.GetAttackSpeedFactorRotation() * num;
		Quaternion rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, num2 * dt);
		if (Mathf.Abs(yawDeltaAngle) > 0.001f)
		{
			base.transform.rotation = rotation;
		}
		return num2 * Mathf.Sign(yawDeltaAngle) * 0.017453292f;
	}

	private void UpdateGroundTilt(float dt)
	{
		if (this.m_visual == null)
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			if (this.m_groundTilt != Character.GroundTiltType.None)
			{
				if (!this.IsFlying() && this.IsOnGround())
				{
					Vector3 vector = this.m_lastGroundNormal;
					if (this.m_groundTilt == Character.GroundTiltType.PitchRaycast || this.m_groundTilt == Character.GroundTiltType.FullRaycast)
					{
						Vector3 p = base.transform.position + base.transform.forward * this.m_collider.radius;
						Vector3 p2 = base.transform.position - base.transform.forward * this.m_collider.radius;
						float num;
						Vector3 b;
						ZoneSystem.instance.GetSolidHeight(p, out num, out b);
						float num2;
						Vector3 b2;
						ZoneSystem.instance.GetSolidHeight(p2, out num2, out b2);
						vector = (vector + b + b2).normalized;
					}
					Vector3 vector2 = base.transform.InverseTransformVector(vector);
					vector2 = Vector3.RotateTowards(Vector3.up, vector2, 0.87266463f, 1f);
					this.m_groundTiltNormal = Vector3.Lerp(this.m_groundTiltNormal, vector2, 0.05f);
					Vector3 vector3;
					if (this.m_groundTilt == Character.GroundTiltType.Pitch || this.m_groundTilt == Character.GroundTiltType.PitchRaycast)
					{
						Vector3 b3 = Vector3.Project(this.m_groundTiltNormal, Vector3.right);
						vector3 = this.m_groundTiltNormal - b3;
					}
					else
					{
						vector3 = this.m_groundTiltNormal;
					}
					Vector3 forward = Vector3.Cross(vector3, Vector3.left);
					this.m_visual.transform.localRotation = Quaternion.LookRotation(forward, vector3);
				}
				else
				{
					this.m_visual.transform.localRotation = Quaternion.RotateTowards(this.m_visual.transform.localRotation, Quaternion.identity, dt * 200f);
				}
				this.m_nview.GetZDO().Set("tiltrot", this.m_visual.transform.localRotation);
				return;
			}
			if (this.CanWallRun())
			{
				if (this.m_wallRunning)
				{
					Vector3 vector4 = Vector3.Lerp(Vector3.up, this.m_lastGroundNormal, 0.65f);
					Vector3 forward2 = Vector3.ProjectOnPlane(base.transform.forward, vector4);
					forward2.Normalize();
					Quaternion to = Quaternion.LookRotation(forward2, vector4);
					this.m_visual.transform.rotation = Quaternion.RotateTowards(this.m_visual.transform.rotation, to, 30f * dt);
				}
				else
				{
					this.m_visual.transform.localRotation = Quaternion.RotateTowards(this.m_visual.transform.localRotation, Quaternion.identity, 100f * dt);
				}
				this.m_nview.GetZDO().Set("tiltrot", this.m_visual.transform.localRotation);
				return;
			}
		}
		else if (this.m_groundTilt != Character.GroundTiltType.None || this.CanWallRun())
		{
			Quaternion quaternion = this.m_nview.GetZDO().GetQuaternion("tiltrot", Quaternion.identity);
			this.m_visual.transform.localRotation = quaternion;
		}
	}

	public bool IsWallRunning()
	{
		return this.m_wallRunning;
	}

	private bool IsOnSnow()
	{
		return false;
	}

	public void Heal(float hp, bool showText = true)
	{
		if (hp <= 0f)
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.RPC_Heal(0L, hp, showText);
			return;
		}
		this.m_nview.InvokeRPC("Heal", new object[]
		{
			hp,
			showText
		});
	}

	private void RPC_Heal(long sender, float hp, bool showText)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		float health = this.GetHealth();
		if (health <= 0f || this.IsDead())
		{
			return;
		}
		float num = Mathf.Min(health + hp, this.GetMaxHealth());
		if (num > health)
		{
			this.SetHealth(num);
			if (showText)
			{
				Vector3 topPoint = this.GetTopPoint();
				DamageText.instance.ShowText(DamageText.TextType.Heal, topPoint, hp, this.IsPlayer());
			}
		}
	}

	public Vector3 GetTopPoint()
	{
		Vector3 center = this.m_collider.bounds.center;
		center.y = this.m_collider.bounds.max.y;
		return center;
	}

	public float GetRadius()
	{
		return this.m_collider.radius;
	}

	public Vector3 GetHeadPoint()
	{
		return this.m_head.position;
	}

	public Vector3 GetEyePoint()
	{
		return this.m_eye.position;
	}

	public Vector3 GetCenterPoint()
	{
		return this.m_collider.bounds.center;
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Character;
	}

	public void Damage(HitData hit)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("Damage", new object[]
		{
			hit
		});
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (this.IsDebugFlying())
		{
			return;
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetHealth() <= 0f || this.IsDead() || this.IsTeleporting() || this.InCutscene())
		{
			return;
		}
		if (hit.m_dodgeable && this.IsDodgeInvincible())
		{
			return;
		}
		Character attacker = hit.GetAttacker();
		if (hit.HaveAttacker() && attacker == null)
		{
			return;
		}
		if (this.IsPlayer() && !this.IsPVPEnabled() && attacker != null && attacker.IsPlayer())
		{
			return;
		}
		if (attacker != null && !attacker.IsPlayer())
		{
			float difficultyDamageScale = Game.instance.GetDifficultyDamageScale(base.transform.position);
			hit.ApplyModifier(difficultyDamageScale);
		}
		this.m_seman.OnDamaged(hit, attacker);
		if (this.m_baseAI != null && !this.m_baseAI.IsAlerted() && hit.m_backstabBonus > 1f && Time.time - this.m_backstabTime > 300f)
		{
			this.m_backstabTime = Time.time;
			hit.ApplyModifier(hit.m_backstabBonus);
			this.m_backstabHitEffects.Create(hit.m_point, Quaternion.identity, base.transform, 1f);
		}
		if (this.IsStaggering() && !this.IsPlayer())
		{
			hit.ApplyModifier(2f);
			this.m_critHitEffects.Create(hit.m_point, Quaternion.identity, base.transform, 1f);
		}
		if (hit.m_blockable && this.IsBlocking())
		{
			this.BlockAttack(hit, attacker);
		}
		this.ApplyPushback(hit);
		if (!string.IsNullOrEmpty(hit.m_statusEffect))
		{
			StatusEffect statusEffect = this.m_seman.GetStatusEffect(hit.m_statusEffect);
			if (statusEffect == null)
			{
				statusEffect = this.m_seman.AddStatusEffect(hit.m_statusEffect, false);
			}
			if (statusEffect != null && attacker != null)
			{
				statusEffect.SetAttacker(attacker);
			}
		}
		HitData.DamageModifiers damageModifiers = this.GetDamageModifiers();
		HitData.DamageModifier mod;
		hit.ApplyResistance(damageModifiers, out mod);
		if (this.IsPlayer())
		{
			float bodyArmor = this.GetBodyArmor();
			hit.ApplyArmor(bodyArmor);
			this.DamageArmorDurability(hit);
		}
		float poison = hit.m_damage.m_poison;
		float fire = hit.m_damage.m_fire;
		float spirit = hit.m_damage.m_spirit;
		hit.m_damage.m_poison = 0f;
		hit.m_damage.m_fire = 0f;
		hit.m_damage.m_spirit = 0f;
		this.ApplyDamage(hit, true, true, mod);
		this.AddFireDamage(fire);
		this.AddSpiritDamage(spirit);
		this.AddPoisonDamage(poison);
		this.AddFrostDamage(hit.m_damage.m_frost);
		this.AddLightningDamage(hit.m_damage.m_lightning);
	}

	protected HitData.DamageModifier GetDamageModifier(HitData.DamageType damageType)
	{
		return this.GetDamageModifiers().GetModifier(damageType);
	}

	protected HitData.DamageModifiers GetDamageModifiers()
	{
		HitData.DamageModifiers result = this.m_damageModifiers.Clone();
		this.ApplyArmorDamageMods(ref result);
		this.m_seman.ApplyDamageMods(ref result);
		return result;
	}

	public void ApplyDamage(HitData hit, bool showDamageText, bool triggerEffects, HitData.DamageModifier mod = HitData.DamageModifier.Normal)
	{
		if (this.IsDebugFlying() || this.IsDead() || this.IsTeleporting() || this.InCutscene())
		{
			return;
		}
		float totalDamage = hit.GetTotalDamage();
		if (showDamageText && (totalDamage > 0f || !this.IsPlayer()))
		{
			DamageText.instance.ShowText(mod, hit.m_point, totalDamage, this.IsPlayer());
		}
		if (totalDamage <= 0f)
		{
			return;
		}
		if (!this.InGodMode() && !this.InGhostMode())
		{
			float num = this.GetHealth();
			num -= totalDamage;
			this.SetHealth(num);
		}
		float totalPhysicalDamage = hit.m_damage.GetTotalPhysicalDamage();
		this.AddStaggerDamage(totalPhysicalDamage * hit.m_staggerMultiplier, hit.m_dir);
		if (triggerEffects && totalDamage > 2f)
		{
			this.DoDamageCameraShake(hit);
			if (hit.m_damage.GetTotalPhysicalDamage() > 0f)
			{
				this.m_hitEffects.Create(hit.m_point, Quaternion.identity, base.transform, 1f);
			}
		}
		this.OnDamaged(hit);
		if (this.m_onDamaged != null)
		{
			this.m_onDamaged(totalDamage, hit.GetAttacker());
		}
		if (Character.m_dpsDebugEnabled)
		{
			Character.AddDPS(totalDamage, this);
		}
	}

	protected virtual void DoDamageCameraShake(HitData hit)
	{
	}

	protected virtual void DamageArmorDurability(HitData hit)
	{
	}

	private void AddFireDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		SE_Burning se_Burning = this.m_seman.GetStatusEffect("Burning") as SE_Burning;
		if (se_Burning == null)
		{
			se_Burning = (this.m_seman.AddStatusEffect("Burning", false) as SE_Burning);
		}
		se_Burning.AddFireDamage(damage);
	}

	private void AddSpiritDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		SE_Burning se_Burning = this.m_seman.GetStatusEffect("Spirit") as SE_Burning;
		if (se_Burning == null)
		{
			se_Burning = (this.m_seman.AddStatusEffect("Spirit", false) as SE_Burning);
		}
		se_Burning.AddSpiritDamage(damage);
	}

	private void AddPoisonDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		SE_Poison se_Poison = this.m_seman.GetStatusEffect("Poison") as SE_Poison;
		if (se_Poison == null)
		{
			se_Poison = (this.m_seman.AddStatusEffect("Poison", false) as SE_Poison);
		}
		se_Poison.AddDamage(damage);
	}

	private void AddFrostDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		SE_Frost se_Frost = this.m_seman.GetStatusEffect("Frost") as SE_Frost;
		if (se_Frost == null)
		{
			se_Frost = (this.m_seman.AddStatusEffect("Frost", false) as SE_Frost);
		}
		se_Frost.AddDamage(damage);
	}

	private void AddLightningDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		this.m_seman.AddStatusEffect("Lightning", true);
	}

	private void AddStaggerDamage(float damage, Vector3 forceDirection)
	{
		if (this.m_staggerDamageFactor <= 0f && !this.IsPlayer())
		{
			return;
		}
		this.m_staggerDamage += damage;
		this.m_staggerTimer = 0f;
		float maxHealth = this.GetMaxHealth();
		float num = this.IsPlayer() ? (maxHealth / 2f) : (maxHealth * this.m_staggerDamageFactor);
		if (this.m_staggerDamage >= num)
		{
			this.m_staggerDamage = 0f;
			this.Stagger(forceDirection);
		}
	}

	private static void AddDPS(float damage, Character me)
	{
		if (me == Player.m_localPlayer)
		{
			Character.CalculateDPS("To-you ", Character.m_playerDamage, damage);
			return;
		}
		Character.CalculateDPS("To-others ", Character.m_enemyDamage, damage);
	}

	private static void CalculateDPS(string name, List<KeyValuePair<float, float>> damages, float damage)
	{
		float time = Time.time;
		if (damages.Count > 0 && Time.time - damages[damages.Count - 1].Key > 5f)
		{
			damages.Clear();
		}
		damages.Add(new KeyValuePair<float, float>(time, damage));
		float num = Time.time - damages[0].Key;
		if (num < 0.01f)
		{
			return;
		}
		float num2 = 0f;
		foreach (KeyValuePair<float, float> keyValuePair in damages)
		{
			num2 += keyValuePair.Value;
		}
		float num3 = num2 / num;
		string text = string.Concat(new object[]
		{
			"DPS ",
			name,
			" (",
			damages.Count,
			" attacks): ",
			num3.ToString("0.0")
		});
		ZLog.Log(text);
		MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text, 0, null);
	}

	private void UpdateStagger(float dt)
	{
		if (this.m_staggerDamageFactor <= 0f && !this.IsPlayer())
		{
			return;
		}
		this.m_staggerTimer += dt;
		if (this.m_staggerTimer > 3f)
		{
			this.m_staggerDamage = 0f;
		}
	}

	public void Stagger(Vector3 forceDirection)
	{
		if (this.m_nview.IsOwner())
		{
			this.RPC_Stagger(0L, forceDirection);
			return;
		}
		this.m_nview.InvokeRPC("Stagger", new object[]
		{
			forceDirection
		});
	}

	private void RPC_Stagger(long sender, Vector3 forceDirection)
	{
		if (!this.IsStaggering())
		{
			if (forceDirection.magnitude > 0.01f)
			{
				forceDirection.y = 0f;
				base.transform.rotation = Quaternion.LookRotation(-forceDirection);
			}
			this.m_zanim.SetTrigger("stagger");
		}
	}

	protected virtual void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
	{
	}

	public virtual float GetBodyArmor()
	{
		return 0f;
	}

	protected virtual bool BlockAttack(HitData hit, Character attacker)
	{
		return false;
	}

	protected virtual void OnDamaged(HitData hit)
	{
	}

	private void OnCollisionStay(Collision collision)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_jumpTimer < 0.1f)
		{
			return;
		}
		foreach (ContactPoint contactPoint in collision.contacts)
		{
			float num = contactPoint.point.y - base.transform.position.y;
			if (contactPoint.normal.y > 0.1f && num < this.m_collider.radius)
			{
				if (contactPoint.normal.y > this.m_groundContactNormal.y || !this.m_groundContact)
				{
					this.m_groundContact = true;
					this.m_groundContactNormal = contactPoint.normal;
					this.m_groundContactPoint = contactPoint.point;
					this.m_lowestContactCollider = collision.collider;
				}
				else
				{
					Vector3 vector = Vector3.Normalize(this.m_groundContactNormal + contactPoint.normal);
					if (vector.y > this.m_groundContactNormal.y)
					{
						this.m_groundContactNormal = vector;
						this.m_groundContactPoint = (this.m_groundContactPoint + contactPoint.point) * 0.5f;
					}
				}
			}
		}
	}

	private void UpdateGroundContact(float dt)
	{
		if (!this.m_groundContact)
		{
			return;
		}
		this.m_lastGroundCollider = this.m_lowestContactCollider;
		this.m_lastGroundNormal = this.m_groundContactNormal;
		this.m_lastGroundPoint = this.m_groundContactPoint;
		this.m_lastGroundBody = (this.m_lastGroundCollider ? this.m_lastGroundCollider.attachedRigidbody : null);
		if (!this.IsPlayer() && this.m_lastGroundBody != null && this.m_lastGroundBody.gameObject.layer == base.gameObject.layer)
		{
			this.m_lastGroundCollider = null;
			this.m_lastGroundBody = null;
		}
		float num = Mathf.Max(0f, this.m_maxAirAltitude - base.transform.position.y);
		if (num > 0.8f)
		{
			if (this.m_onLand != null)
			{
				Vector3 lastGroundPoint = this.m_lastGroundPoint;
				if (this.InWater())
				{
					lastGroundPoint.y = this.m_waterLevel;
				}
				this.m_onLand(this.m_lastGroundPoint);
			}
			this.ResetCloth();
		}
		if (this.IsPlayer() && num > 4f)
		{
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = Mathf.Clamp01((num - 4f) / 16f) * 100f;
			hitData.m_point = this.m_lastGroundPoint;
			hitData.m_dir = this.m_lastGroundNormal;
			this.Damage(hitData);
		}
		this.ResetGroundContact();
		this.m_lastGroundTouch = 0f;
		this.m_maxAirAltitude = base.transform.position.y;
	}

	private void ResetGroundContact()
	{
		this.m_lowestContactCollider = null;
		this.m_groundContact = false;
		this.m_groundContactNormal = Vector3.zero;
		this.m_groundContactPoint = Vector3.zero;
	}

	public Ship GetStandingOnShip()
	{
		if (!this.IsOnGround())
		{
			return null;
		}
		if (this.m_lastGroundBody)
		{
			return this.m_lastGroundBody.GetComponent<Ship>();
		}
		return null;
	}

	public bool IsOnGround()
	{
		return this.m_lastGroundTouch < 0.2f || this.m_body.IsSleeping();
	}

	private void CheckDeath()
	{
		if (this.IsDead())
		{
			return;
		}
		if (this.GetHealth() <= 0f)
		{
			this.OnDeath();
			if (this.m_onDeath != null)
			{
				this.m_onDeath();
			}
		}
	}

	protected virtual void OnRagdollCreated(Ragdoll ragdoll)
	{
	}

	protected virtual void OnDeath()
	{
		GameObject[] array = this.m_deathEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f);
		for (int i = 0; i < array.Length; i++)
		{
			Ragdoll component = array[i].GetComponent<Ragdoll>();
			if (component)
			{
				CharacterDrop component2 = base.GetComponent<CharacterDrop>();
				LevelEffects componentInChildren = base.GetComponentInChildren<LevelEffects>();
				Vector3 velocity = this.m_body.velocity;
				if (this.m_pushForce.magnitude * 0.5f > velocity.magnitude)
				{
					velocity = this.m_pushForce * 0.5f;
				}
				float hue = 0f;
				float saturation = 0f;
				float value = 0f;
				if (componentInChildren)
				{
					componentInChildren.GetColorChanges(out hue, out saturation, out value);
				}
				component.Setup(velocity, hue, saturation, value, component2);
				this.OnRagdollCreated(component);
				if (component2)
				{
					component2.SetDropsEnabled(false);
				}
			}
		}
		if (!string.IsNullOrEmpty(this.m_defeatSetGlobalKey))
		{
			ZoneSystem.instance.SetGlobalKey(this.m_defeatSetGlobalKey);
		}
		ZNetScene.instance.Destroy(base.gameObject);
		GoogleAnalyticsV4.instance.LogEvent("Game", "Killed", this.m_name, 0L);
	}

	public float GetHealth()
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo == null)
		{
			return this.GetMaxHealth();
		}
		return zdo.GetFloat("health", this.GetMaxHealth());
	}

	public void SetHealth(float health)
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo == null || !this.m_nview.IsOwner())
		{
			return;
		}
		if (health < 0f)
		{
			health = 0f;
		}
		zdo.Set("health", health);
	}

	public float GetHealthPercentage()
	{
		return this.GetHealth() / this.GetMaxHealth();
	}

	public virtual bool IsDead()
	{
		return false;
	}

	public void SetMaxHealth(float health)
	{
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("max_health", health);
		}
		if (this.GetHealth() > health)
		{
			this.SetHealth(health);
		}
	}

	public float GetMaxHealth()
	{
		if (this.m_nview.GetZDO() != null)
		{
			return this.m_nview.GetZDO().GetFloat("max_health", this.m_health);
		}
		return this.m_health;
	}

	public virtual float GetMaxStamina()
	{
		return 0f;
	}

	public virtual float GetStaminaPercentage()
	{
		return 1f;
	}

	public bool IsBoss()
	{
		return this.m_boss;
	}

	public void SetLookDir(Vector3 dir)
	{
		if (dir.magnitude <= Mathf.Epsilon)
		{
			dir = base.transform.forward;
		}
		else
		{
			dir.Normalize();
		}
		this.m_lookDir = dir;
		dir.y = 0f;
		this.m_lookYaw = Quaternion.LookRotation(dir);
	}

	public Vector3 GetLookDir()
	{
		return this.m_eye.forward;
	}

	public virtual void OnAttackTrigger()
	{
	}

	public virtual void OnStopMoving()
	{
	}

	public virtual void OnWeaponTrailStart()
	{
	}

	public void SetMoveDir(Vector3 dir)
	{
		this.m_moveDir = dir;
	}

	public void SetRun(bool run)
	{
		this.m_run = run;
	}

	public void SetWalk(bool walk)
	{
		this.m_walk = walk;
	}

	public bool GetWalk()
	{
		return this.m_walk;
	}

	protected virtual void UpdateEyeRotation()
	{
		this.m_eye.rotation = Quaternion.LookRotation(this.m_lookDir);
	}

	public void OnAutoJump(Vector3 dir, float upVel, float forwardVel)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.IsOnGround() || this.IsDead() || this.InAttack() || this.InDodge() || this.IsKnockedBack())
		{
			return;
		}
		if (Time.time - this.m_lastAutoJumpTime < 0.5f)
		{
			return;
		}
		this.m_lastAutoJumpTime = Time.time;
		if (Vector3.Dot(this.m_moveDir, dir) < 0.5f)
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		vector.y = upVel;
		vector += dir * forwardVel;
		this.m_body.velocity = vector;
		this.m_lastGroundTouch = 1f;
		this.m_jumpTimer = 0f;
		this.m_jumpEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f);
		this.SetCrouch(false);
		this.UpdateBodyFriction();
	}

	public void Jump()
	{
		if (this.IsOnGround() && !this.IsDead() && !this.InAttack() && !this.IsEncumbered() && !this.InDodge() && !this.IsKnockedBack())
		{
			bool flag = false;
			if (!this.HaveStamina(this.m_jumpStaminaUsage))
			{
				if (this.IsPlayer())
				{
					Hud.instance.StaminaBarNoStaminaFlash();
				}
				flag = true;
			}
			float num = 0f;
			Skills skills = this.GetSkills();
			if (skills != null)
			{
				num = skills.GetSkillFactor(Skills.SkillType.Jump);
				if (!flag)
				{
					this.RaiseSkill(Skills.SkillType.Jump, 1f);
				}
			}
			Vector3 vector = this.m_body.velocity;
			Mathf.Acos(Mathf.Clamp01(this.m_lastGroundNormal.y));
			Vector3 normalized = (this.m_lastGroundNormal + Vector3.up).normalized;
			float num2 = 1f + num * 0.4f;
			float num3 = this.m_jumpForce * num2;
			float num4 = Vector3.Dot(normalized, vector);
			if (num4 < num3)
			{
				vector += normalized * (num3 - num4);
			}
			vector += this.m_moveDir * this.m_jumpForceForward * num2;
			if (flag)
			{
				vector *= this.m_jumpForceTiredFactor;
			}
			this.m_body.WakeUp();
			this.m_body.velocity = vector;
			this.ResetGroundContact();
			this.m_lastGroundTouch = 1f;
			this.m_jumpTimer = 0f;
			this.m_zanim.SetTrigger("jump");
			this.AddNoise(30f);
			this.m_jumpEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f);
			this.OnJump();
			this.SetCrouch(false);
			this.UpdateBodyFriction();
		}
	}

	private void UpdateBodyFriction()
	{
		this.m_collider.material.frictionCombine = PhysicMaterialCombine.Multiply;
		if (this.IsDead())
		{
			this.m_collider.material.staticFriction = 1f;
			this.m_collider.material.dynamicFriction = 1f;
			this.m_collider.material.frictionCombine = PhysicMaterialCombine.Maximum;
			return;
		}
		if (this.IsSwiming())
		{
			this.m_collider.material.staticFriction = 0.2f;
			this.m_collider.material.dynamicFriction = 0.2f;
			return;
		}
		if (!this.IsOnGround())
		{
			this.m_collider.material.staticFriction = 0f;
			this.m_collider.material.dynamicFriction = 0f;
			return;
		}
		if (this.IsFlying())
		{
			this.m_collider.material.staticFriction = 0f;
			this.m_collider.material.dynamicFriction = 0f;
			return;
		}
		if (this.m_moveDir.magnitude < 0.1f)
		{
			this.m_collider.material.staticFriction = 0.8f * (1f - this.m_slippage);
			this.m_collider.material.dynamicFriction = 0.8f * (1f - this.m_slippage);
			this.m_collider.material.frictionCombine = PhysicMaterialCombine.Maximum;
			return;
		}
		this.m_collider.material.staticFriction = 0.4f * (1f - this.m_slippage);
		this.m_collider.material.dynamicFriction = 0.4f * (1f - this.m_slippage);
	}

	public virtual bool StartAttack(Character target, bool charge)
	{
		return false;
	}

	public virtual void OnNearFire(Vector3 point)
	{
	}

	public ZDOID GetZDOID()
	{
		if (this.m_nview.IsValid())
		{
			return this.m_nview.GetZDO().m_uid;
		}
		return ZDOID.None;
	}

	public bool IsOwner()
	{
		return this.m_nview.IsValid() && this.m_nview.IsOwner();
	}

	public long GetOwner()
	{
		if (this.m_nview.IsValid())
		{
			return this.m_nview.GetZDO().m_owner;
		}
		return 0L;
	}

	public virtual bool UseMeleeCamera()
	{
		return false;
	}

	public virtual bool AlwaysRotateCamera()
	{
		return true;
	}

	public void SetInWater(float depth)
	{
		this.m_waterLevel = depth;
	}

	public virtual bool IsPVPEnabled()
	{
		return false;
	}

	public virtual bool InIntro()
	{
		return false;
	}

	public virtual bool InCutscene()
	{
		return false;
	}

	public virtual bool IsCrouching()
	{
		return false;
	}

	public virtual bool InBed()
	{
		return false;
	}

	public virtual bool IsAttached()
	{
		return false;
	}

	protected virtual void SetCrouch(bool crouch)
	{
	}

	public virtual void AttachStart(Transform attachPoint, bool hideWeapons, bool isBed, string attachAnimation, Vector3 detachOffset)
	{
	}

	public virtual void AttachStop()
	{
	}

	private void UpdateWater(float dt)
	{
		this.m_swimTimer += dt;
		if (this.InWaterSwimDepth())
		{
			if (this.m_nview.IsOwner())
			{
				this.m_seman.AddStatusEffect("Wet", true);
			}
			if (this.m_canSwim)
			{
				this.m_swimTimer = 0f;
			}
		}
	}

	public bool IsSwiming()
	{
		return this.m_swimTimer < 0.5f;
	}

	public bool InWaterSwimDepth()
	{
		return this.InWaterDepth() > Mathf.Max(0f, this.m_swimDepth - 0.4f);
	}

	private float InWaterDepth()
	{
		if (this.GetStandingOnShip() != null)
		{
			return 0f;
		}
		return Mathf.Max(0f, this.m_waterLevel - base.transform.position.y);
	}

	public bool InWater()
	{
		return this.InWaterDepth() > 0f;
	}

	protected virtual bool CheckRun(Vector3 moveDir, float dt)
	{
		return this.m_run && moveDir.magnitude >= 0.1f && !this.IsCrouching() && !this.IsEncumbered() && !this.InDodge();
	}

	public bool IsRunning()
	{
		return this.m_running;
	}

	public virtual bool InPlaceMode()
	{
		return false;
	}

	public virtual bool HaveStamina(float amount = 0f)
	{
		return true;
	}

	public virtual void AddStamina(float v)
	{
	}

	public virtual void UseStamina(float stamina)
	{
	}

	public bool IsStaggering()
	{
		return this.m_animator.GetCurrentAnimatorStateInfo(0).tagHash == Character.m_animatorTagStagger;
	}

	public virtual bool CanMove()
	{
		AnimatorStateInfo animatorStateInfo = this.m_animator.IsInTransition(0) ? this.m_animator.GetNextAnimatorStateInfo(0) : this.m_animator.GetCurrentAnimatorStateInfo(0);
		return animatorStateInfo.tagHash != Character.m_animatorTagFreeze && animatorStateInfo.tagHash != Character.m_animatorTagStagger && animatorStateInfo.tagHash != Character.m_animatorTagSitting;
	}

	public virtual bool IsEncumbered()
	{
		return false;
	}

	public virtual bool IsTeleporting()
	{
		return false;
	}

	private bool CanWallRun()
	{
		return this.IsPlayer();
	}

	public void ShowPickupMessage(ItemDrop.ItemData item, int amount)
	{
		this.Message(MessageHud.MessageType.TopLeft, "$msg_added " + item.m_shared.m_name, amount, item.GetIcon());
	}

	public void ShowRemovedMessage(ItemDrop.ItemData item, int amount)
	{
		this.Message(MessageHud.MessageType.TopLeft, "$msg_removed " + item.m_shared.m_name, amount, item.GetIcon());
	}

	public virtual void Message(MessageHud.MessageType type, string msg, int amount = 0, Sprite icon = null)
	{
	}

	public CapsuleCollider GetCollider()
	{
		return this.m_collider;
	}

	public virtual void OnStealthSuccess(Character character, float factor)
	{
	}

	public virtual float GetStealthFactor()
	{
		return 1f;
	}

	private void UpdateNoise(float dt)
	{
		this.m_noiseRange = Mathf.Max(0f, this.m_noiseRange - dt * 4f);
		this.m_syncNoiseTimer += dt;
		if (this.m_syncNoiseTimer > 0.5f)
		{
			this.m_syncNoiseTimer = 0f;
			this.m_nview.GetZDO().Set("noise", this.m_noiseRange);
		}
	}

	public void AddNoise(float range)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.RPC_AddNoise(0L, range);
			return;
		}
		this.m_nview.InvokeRPC("AddNoise", new object[]
		{
			range
		});
	}

	private void RPC_AddNoise(long sender, float range)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (range > this.m_noiseRange)
		{
			this.m_noiseRange = range;
			this.m_seman.ModifyNoise(this.m_noiseRange, ref this.m_noiseRange);
		}
	}

	public float GetNoiseRange()
	{
		if (!this.m_nview.IsValid())
		{
			return 0f;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_noiseRange;
		}
		return this.m_nview.GetZDO().GetFloat("noise", 0f);
	}

	public virtual bool InGodMode()
	{
		return false;
	}

	public virtual bool InGhostMode()
	{
		return false;
	}

	public virtual bool IsDebugFlying()
	{
		return false;
	}

	public virtual string GetHoverText()
	{
		Tameable component = base.GetComponent<Tameable>();
		if (component)
		{
			return component.GetHoverText();
		}
		return "";
	}

	public virtual string GetHoverName()
	{
		return Localization.instance.Localize(this.m_name);
	}

	public virtual bool IsHoldingAttack()
	{
		return false;
	}

	public virtual bool InAttack()
	{
		return false;
	}

	protected virtual void StopEmote()
	{
	}

	public virtual bool InMinorAction()
	{
		return false;
	}

	public virtual bool InDodge()
	{
		return false;
	}

	public virtual bool IsDodgeInvincible()
	{
		return false;
	}

	public virtual bool InEmote()
	{
		return false;
	}

	public virtual bool IsBlocking()
	{
		return false;
	}

	public bool IsFlying()
	{
		return this.m_flying;
	}

	public bool IsKnockedBack()
	{
		return this.m_pushForce != Vector3.zero;
	}

	private void OnDrawGizmosSelected()
	{
		if (this.m_nview != null && this.m_nview.GetZDO() != null)
		{
			float @float = this.m_nview.GetZDO().GetFloat("noise", 0f);
			Gizmos.DrawWireSphere(base.transform.position, @float);
		}
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * this.m_swimDepth, new Vector3(1f, 0.05f, 1f));
		if (this.IsOnGround())
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(this.m_lastGroundPoint, this.m_lastGroundPoint + this.m_lastGroundNormal);
		}
	}

	public virtual bool TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		return false;
	}

	private void SyncVelocity()
	{
		this.m_nview.GetZDO().Set("BodyVelocity", this.m_body.velocity);
	}

	public Vector3 GetVelocity()
	{
		if (!this.m_nview.IsValid())
		{
			return Vector3.zero;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_body.velocity;
		}
		return this.m_nview.GetZDO().GetVec3("BodyVelocity", Vector3.zero);
	}

	public void AddRootMotion(Vector3 vel)
	{
		if (this.InDodge() || this.InAttack() || this.InEmote())
		{
			this.m_rootMotion += vel;
		}
	}

	private void ApplyRootMotion(ref Vector3 vel)
	{
		Vector3 vector = this.m_rootMotion * 55f;
		if (vector.magnitude > vel.magnitude)
		{
			vel = vector;
		}
		this.m_rootMotion = Vector3.zero;
	}

	public static void GetCharactersInRange(Vector3 point, float radius, List<Character> characters)
	{
		foreach (Character character in Character.m_characters)
		{
			if (Vector3.Distance(character.transform.position, point) < radius)
			{
				characters.Add(character);
			}
		}
	}

	public static List<Character> GetAllCharacters()
	{
		return Character.m_characters;
	}

	public static bool IsCharacterInRange(Vector3 point, float range)
	{
		using (List<Character>.Enumerator enumerator = Character.m_characters.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (Vector3.Distance(enumerator.Current.transform.position, point) < range)
				{
					return true;
				}
			}
		}
		return false;
	}

	public virtual void OnTargeted(bool sensed, bool alerted)
	{
	}

	public GameObject GetVisual()
	{
		return this.m_visual;
	}

	protected void UpdateLodgroup()
	{
		if (this.m_lodGroup == null)
		{
			return;
		}
		Renderer[] componentsInChildren = this.m_visual.GetComponentsInChildren<Renderer>();
		LOD[] lods = this.m_lodGroup.GetLODs();
		lods[0].renderers = componentsInChildren;
		this.m_lodGroup.SetLODs(lods);
	}

	public virtual float GetEquipmentMovementModifier()
	{
		return 0f;
	}

	protected virtual float GetJogSpeedFactor()
	{
		return 1f;
	}

	protected virtual float GetRunSpeedFactor()
	{
		return 1f;
	}

	protected virtual float GetAttackSpeedFactorMovement()
	{
		return 1f;
	}

	protected virtual float GetAttackSpeedFactorRotation()
	{
		return 1f;
	}

	public virtual void RaiseSkill(Skills.SkillType skill, float value = 1f)
	{
	}

	public virtual Skills GetSkills()
	{
		return null;
	}

	public virtual float GetSkillFactor(Skills.SkillType skill)
	{
		return 0f;
	}

	public virtual float GetRandomSkillFactor(Skills.SkillType skill)
	{
		return UnityEngine.Random.Range(0.75f, 1f);
	}

	public bool IsMonsterFaction()
	{
		return !this.IsTamed() && (this.m_faction == Character.Faction.ForestMonsters || this.m_faction == Character.Faction.Undead || this.m_faction == Character.Faction.Demon || this.m_faction == Character.Faction.PlainsMonsters || this.m_faction == Character.Faction.MountainMonsters || this.m_faction == Character.Faction.SeaMonsters);
	}

	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	public Collider GetLastGroundCollider()
	{
		return this.m_lastGroundCollider;
	}

	public Vector3 GetLastGroundNormal()
	{
		return this.m_groundContactNormal;
	}

	public void ResetCloth()
	{
		this.m_nview.InvokeRPC(ZNetView.Everybody, "ResetCloth", Array.Empty<object>());
	}

	private void RPC_ResetCloth(long sender)
	{
		foreach (Cloth cloth in base.GetComponentsInChildren<Cloth>())
		{
			if (cloth.enabled)
			{
				cloth.enabled = false;
				cloth.enabled = true;
			}
		}
	}

	public virtual bool GetRelativePosition(out ZDOID parent, out Vector3 relativePos, out Vector3 relativeVel)
	{
		relativeVel = Vector3.zero;
		if (this.IsOnGround() && this.m_lastGroundBody)
		{
			ZNetView component = this.m_lastGroundBody.GetComponent<ZNetView>();
			if (component && component.IsValid())
			{
				parent = component.GetZDO().m_uid;
				relativePos = component.transform.InverseTransformPoint(base.transform.position);
				relativeVel = component.transform.InverseTransformVector(this.m_body.velocity - this.m_lastGroundBody.velocity);
				return true;
			}
		}
		parent = ZDOID.None;
		relativePos = Vector3.zero;
		return false;
	}

	public Quaternion GetLookYaw()
	{
		return this.m_lookYaw;
	}

	public Vector3 GetMoveDir()
	{
		return this.m_moveDir;
	}

	public BaseAI GetBaseAI()
	{
		return this.m_baseAI;
	}

	public float GetMass()
	{
		return this.m_body.mass;
	}

	protected void SetVisible(bool visible)
	{
		if (this.m_lodGroup == null)
		{
			return;
		}
		if (this.m_lodVisible == visible)
		{
			return;
		}
		this.m_lodVisible = visible;
		if (this.m_lodVisible)
		{
			this.m_lodGroup.localReferencePoint = this.m_originalLocalRef;
			return;
		}
		this.m_lodGroup.localReferencePoint = new Vector3(999999f, 999999f, 999999f);
	}

	public void SetTamed(bool tamed)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_tamed == tamed)
		{
			return;
		}
		this.m_nview.InvokeRPC("SetTamed", new object[]
		{
			tamed
		});
	}

	private void RPC_SetTamed(long sender, bool tamed)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_tamed == tamed)
		{
			return;
		}
		this.m_tamed = tamed;
		this.m_nview.GetZDO().Set("tamed", this.m_tamed);
	}

	public bool IsTamed()
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!this.m_nview.IsOwner() && Time.time - this.m_lastTamedCheck > 1f)
		{
			this.m_lastTamedCheck = Time.time;
			this.m_tamed = this.m_nview.GetZDO().GetBool("tamed", this.m_tamed);
		}
		return this.m_tamed;
	}

	public SEMan GetSEMan()
	{
		return this.m_seman;
	}

	public bool InInterior()
	{
		return base.transform.position.y > 3000f;
	}

	public static void SetDPSDebug(bool enabled)
	{
		Character.m_dpsDebugEnabled = enabled;
	}

	public static bool IsDPSDebugEnabled()
	{
		return Character.m_dpsDebugEnabled;
	}

	private float m_underWorldCheckTimer;

	private Collider m_lowestContactCollider;

	private bool m_groundContact;

	private Vector3 m_groundContactPoint = Vector3.zero;

	private Vector3 m_groundContactNormal = Vector3.zero;

	public Action<float, Character> m_onDamaged;

	public Action m_onDeath;

	public Action<int> m_onLevelSet;

	public Action<Vector3> m_onLand;

	[Header("Character")]
	public string m_name = "";

	public Character.Faction m_faction = Character.Faction.AnimalsVeg;

	public bool m_boss;

	public string m_bossEvent = "";

	public string m_defeatSetGlobalKey = "";

	[Header("Movement & Physics")]
	public float m_crouchSpeed = 2f;

	public float m_walkSpeed = 5f;

	public float m_speed = 10f;

	public float m_turnSpeed = 300f;

	public float m_runSpeed = 20f;

	public float m_runTurnSpeed = 300f;

	public float m_flySlowSpeed = 5f;

	public float m_flyFastSpeed = 12f;

	public float m_flyTurnSpeed = 12f;

	public float m_acceleration = 1f;

	public float m_jumpForce = 10f;

	public float m_jumpForceForward;

	public float m_jumpForceTiredFactor = 0.7f;

	public float m_airControl = 0.1f;

	private const float m_slopeStaminaDrain = 10f;

	public const float m_minSlideDegreesPlayer = 38f;

	public const float m_minSlideDegreesMonster = 90f;

	private const float m_rootMotionMultiplier = 55f;

	private const float m_continousPushForce = 10f;

	private const float m_pushForcedissipation = 100f;

	private const float m_maxMoveForce = 20f;

	public bool m_canSwim = true;

	public float m_swimDepth = 2f;

	public float m_swimSpeed = 2f;

	public float m_swimTurnSpeed = 100f;

	public float m_swimAcceleration = 0.05f;

	public Character.GroundTiltType m_groundTilt;

	public bool m_flying;

	public float m_jumpStaminaUsage = 10f;

	[Header("Bodyparts")]
	public Transform m_eye;

	protected Transform m_head;

	[Header("Effects")]
	public EffectList m_hitEffects = new EffectList();

	public EffectList m_critHitEffects = new EffectList();

	public EffectList m_backstabHitEffects = new EffectList();

	public EffectList m_deathEffects = new EffectList();

	public EffectList m_waterEffects = new EffectList();

	public EffectList m_slideEffects = new EffectList();

	public EffectList m_jumpEffects = new EffectList();

	[Header("Health & Damage")]
	public bool m_tolerateWater = true;

	public bool m_tolerateFire;

	public bool m_tolerateSmoke = true;

	public float m_health = 10f;

	public HitData.DamageModifiers m_damageModifiers;

	public bool m_staggerWhenBlocked = true;

	public float m_staggerDamageFactor;

	private const float m_staggerResetTime = 3f;

	private float m_staggerDamage;

	private float m_staggerTimer;

	private float m_backstabTime = -99999f;

	private const float m_backstabResetTime = 300f;

	private GameObject[] m_waterEffects_instances;

	private GameObject[] m_slideEffects_instances;

	protected Vector3 m_moveDir = Vector3.zero;

	protected Vector3 m_lookDir = Vector3.forward;

	protected Quaternion m_lookYaw = Quaternion.identity;

	protected bool m_run;

	protected bool m_walk;

	protected bool m_attack;

	protected bool m_attackDraw;

	protected bool m_secondaryAttack;

	protected bool m_blocking;

	protected GameObject m_visual;

	protected LODGroup m_lodGroup;

	protected Rigidbody m_body;

	protected CapsuleCollider m_collider;

	protected ZNetView m_nview;

	protected ZSyncAnimation m_zanim;

	protected Animator m_animator;

	protected CharacterAnimEvent m_animEvent;

	protected BaseAI m_baseAI;

	private const float m_maxFallHeight = 20f;

	private const float m_minFallHeight = 4f;

	private const float m_maxFallDamage = 100f;

	private const float m_staggerDamageBonus = 2f;

	private const float m_baseVisualRange = 30f;

	private const float m_autoJumpInterval = 0.5f;

	private float m_jumpTimer;

	private float m_lastAutoJumpTime;

	private float m_lastGroundTouch;

	private Vector3 m_lastGroundNormal = Vector3.up;

	private Vector3 m_lastGroundPoint = Vector3.up;

	private Collider m_lastGroundCollider;

	private Rigidbody m_lastGroundBody;

	private Vector3 m_lastAttachPos = Vector3.zero;

	private Rigidbody m_lastAttachBody;

	protected float m_maxAirAltitude = -10000f;

	protected float m_waterLevel = -10000f;

	private float m_swimTimer = 999f;

	protected SEMan m_seman;

	private float m_noiseRange;

	private float m_syncNoiseTimer;

	private bool m_tamed;

	private float m_lastTamedCheck;

	private int m_level = 1;

	private Vector3 m_currentVel = Vector3.zero;

	private float m_currentTurnVel;

	private float m_currentTurnVelChange;

	private Vector3 m_groundTiltNormal = Vector3.up;

	protected Vector3 m_pushForce = Vector3.zero;

	private Vector3 m_rootMotion = Vector3.zero;

	private static int forward_speed = 0;

	private static int sideway_speed = 0;

	private static int turn_speed = 0;

	private static int inWater = 0;

	private static int onGround = 0;

	private static int encumbered = 0;

	private static int flying = 0;

	private float m_slippage;

	protected bool m_wallRunning;

	protected bool m_sliding;

	protected bool m_running;

	private Vector3 m_originalLocalRef;

	private bool m_lodVisible = true;

	private static int m_smokeRayMask = 0;

	private float m_smokeCheckTimer;

	private static bool m_dpsDebugEnabled = false;

	private static List<KeyValuePair<float, float>> m_enemyDamage = new List<KeyValuePair<float, float>>();

	private static List<KeyValuePair<float, float>> m_playerDamage = new List<KeyValuePair<float, float>>();

	private static List<Character> m_characters = new List<Character>();

	protected static int m_characterLayer = 0;

	protected static int m_characterNetLayer = 0;

	protected static int m_characterGhostLayer = 0;

	protected static int m_animatorTagFreeze = Animator.StringToHash("freeze");

	protected static int m_animatorTagStagger = Animator.StringToHash("stagger");

	protected static int m_animatorTagSitting = Animator.StringToHash("sitting");

	public enum Faction
	{
		Players,
		AnimalsVeg,
		ForestMonsters,
		Undead,
		Demon,
		MountainMonsters,
		SeaMonsters,
		PlainsMonsters,
		Boss
	}

	public enum GroundTiltType
	{
		None,
		Pitch,
		Full,
		PitchRaycast,
		FullRaycast
	}
}
