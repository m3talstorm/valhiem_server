using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseAI : MonoBehaviour
{
	protected virtual void Awake()
	{
		BaseAI.m_instances.Add(this);
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_character = base.GetComponent<Character>();
		this.m_animator = base.GetComponent<ZSyncAnimation>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_solidRayMask = LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"Default_small",
			"piece",
			"terrain",
			"vehicle"
		});
		this.m_viewBlockMask = LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"Default_small",
			"piece",
			"terrain",
			"viewblock",
			"vehicle"
		});
		this.m_monsterTargetRayMask = LayerMask.GetMask(new string[]
		{
			"piece",
			"piece_nonsolid",
			"Default",
			"static_solid",
			"Default_small",
			"vehicle"
		});
		Character character = this.m_character;
		character.m_onDamaged = (Action<float, Character>)Delegate.Combine(character.m_onDamaged, new Action<float, Character>(this.OnDamaged));
		Character character2 = this.m_character;
		character2.m_onDeath = (Action)Delegate.Combine(character2.m_onDeath, new Action(this.OnDeath));
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong(BaseAI.spawnTimeHash, 0L) == 0L)
		{
			this.m_nview.GetZDO().Set(BaseAI.spawnTimeHash, ZNet.instance.GetTime().Ticks);
			if (!string.IsNullOrEmpty(this.m_spawnMessage))
			{
				MessageHud.instance.MessageAll(MessageHud.MessageType.Center, this.m_spawnMessage);
			}
		}
		this.m_randomMoveUpdateTimer = UnityEngine.Random.Range(0f, this.m_randomMoveInterval);
		this.m_nview.Register("Alert", new Action<long>(this.RPC_Alert));
		this.m_huntPlayer = this.m_nview.GetZDO().GetBool("huntplayer", this.m_huntPlayer);
		this.m_spawnPoint = this.m_nview.GetZDO().GetVec3("spawnpoint", base.transform.position);
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set("spawnpoint", this.m_spawnPoint);
		}
		base.InvokeRepeating("DoIdleSound", this.m_idleSoundInterval, this.m_idleSoundInterval);
	}

	private void OnDestroy()
	{
		BaseAI.m_instances.Remove(this);
	}

	public void SetPatrolPoint()
	{
		this.SetPatrolPoint(base.transform.position);
	}

	public void SetPatrolPoint(Vector3 point)
	{
		this.m_patrol = true;
		this.m_patrolPoint = point;
		this.m_nview.GetZDO().Set("patrolPoint", point);
		this.m_nview.GetZDO().Set("patrol", true);
	}

	public void ResetPatrolPoint()
	{
		this.m_patrol = false;
		this.m_nview.GetZDO().Set("patrol", false);
	}

	public bool GetPatrolPoint(out Vector3 point)
	{
		if (Time.time - this.m_patrolPointUpdateTime > 1f)
		{
			this.m_patrolPointUpdateTime = Time.time;
			this.m_patrol = this.m_nview.GetZDO().GetBool("patrol", false);
			if (this.m_patrol)
			{
				this.m_patrolPoint = this.m_nview.GetZDO().GetVec3("patrolPoint", this.m_patrolPoint);
			}
		}
		point = this.m_patrolPoint;
		return this.m_patrol;
	}

	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_updateTimer += Time.fixedDeltaTime;
		if (this.m_updateTimer >= 0.05f)
		{
			this.UpdateAI(0.05f);
			this.m_updateTimer -= 0.05f;
		}
	}

	protected virtual void UpdateAI(float dt)
	{
		if (this.m_nview.IsOwner())
		{
			this.UpdateTakeoffLanding(dt);
			if (this.m_jumpInterval > 0f)
			{
				this.m_jumpTimer += dt;
			}
			if (this.m_randomMoveUpdateTimer > 0f)
			{
				this.m_randomMoveUpdateTimer -= dt;
			}
			this.UpdateRegeneration(dt);
			this.m_timeSinceHurt += dt;
			return;
		}
		this.m_alerted = this.m_nview.GetZDO().GetBool("alert", false);
	}

	private void UpdateRegeneration(float dt)
	{
		this.m_regenTimer += dt;
		if (this.m_regenTimer > 1f)
		{
			this.m_regenTimer = 0f;
			float num = this.m_character.GetMaxHealth() / 3600f;
			float worldTimeDelta = this.GetWorldTimeDelta();
			this.m_character.Heal(num * worldTimeDelta, false);
		}
	}

	public bool IsTakingOff()
	{
		return this.m_randomFly && this.m_character.IsFlying() && this.m_randomFlyTimer < this.m_takeoffTime;
	}

	public void UpdateTakeoffLanding(float dt)
	{
		if (!this.m_randomFly)
		{
			return;
		}
		this.m_randomFlyTimer += dt;
		if (this.m_character.InAttack() || this.m_character.IsStaggering())
		{
			return;
		}
		if (this.m_character.IsFlying())
		{
			if (this.m_randomFlyTimer > this.m_airDuration && this.GetAltitude() < this.m_maxLandAltitude)
			{
				this.m_randomFlyTimer = 0f;
				if (UnityEngine.Random.value <= this.m_chanceToLand)
				{
					this.m_character.m_flying = false;
					this.m_animator.SetTrigger("fly_land");
					return;
				}
			}
		}
		else if (this.m_randomFlyTimer > this.m_groundDuration)
		{
			this.m_randomFlyTimer = 0f;
			if (UnityEngine.Random.value <= this.m_chanceToTakeoff)
			{
				this.m_character.m_flying = true;
				this.m_character.m_jumpEffects.Create(this.m_character.transform.position, Quaternion.identity, null, 1f);
				this.m_animator.SetTrigger("fly_takeoff");
			}
		}
	}

	private float GetWorldTimeDelta()
	{
		DateTime time = ZNet.instance.GetTime();
		long @long = this.m_nview.GetZDO().GetLong(BaseAI.worldTimeHash, 0L);
		if (@long == 0L)
		{
			this.m_nview.GetZDO().Set(BaseAI.worldTimeHash, time.Ticks);
			return 0f;
		}
		DateTime d = new DateTime(@long);
		TimeSpan timeSpan = time - d;
		this.m_nview.GetZDO().Set(BaseAI.worldTimeHash, time.Ticks);
		return (float)timeSpan.TotalSeconds;
	}

	public TimeSpan GetTimeSinceSpawned()
	{
		long num = this.m_nview.GetZDO().GetLong("spawntime", 0L);
		if (num == 0L)
		{
			num = ZNet.instance.GetTime().Ticks;
			this.m_nview.GetZDO().Set("spawntime", num);
		}
		DateTime d = new DateTime(num);
		return ZNet.instance.GetTime() - d;
	}

	private void DoIdleSound()
	{
		if (this.IsSleeping())
		{
			return;
		}
		if (UnityEngine.Random.value > this.m_idleSoundChance)
		{
			return;
		}
		this.m_idleSound.Create(base.transform.position, Quaternion.identity, null, 1f);
	}

	protected void Follow(GameObject go, float dt)
	{
		float num = Vector3.Distance(go.transform.position, base.transform.position);
		bool run = num > 10f;
		if (num < 3f)
		{
			this.StopMoving();
			return;
		}
		this.MoveTo(dt, go.transform.position, 0f, run);
	}

	protected void MoveToWater(float dt, float maxRange)
	{
		float num = this.m_haveWaterPosition ? 2f : 0.5f;
		if (Time.time - this.m_lastMoveToWaterUpdate > num)
		{
			this.m_lastMoveToWaterUpdate = Time.time;
			Vector3 vector = base.transform.position;
			for (int i = 0; i < 10; i++)
			{
				Vector3 b = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * UnityEngine.Random.Range(4f, maxRange);
				Vector3 vector2 = base.transform.position + b;
				vector2.y = ZoneSystem.instance.GetSolidHeight(vector2);
				if (vector2.y < vector.y)
				{
					vector = vector2;
				}
			}
			if (vector.y < ZoneSystem.instance.m_waterLevel)
			{
				this.m_moveToWaterPosition = vector;
				this.m_haveWaterPosition = true;
			}
			else
			{
				this.m_haveWaterPosition = false;
			}
		}
		if (this.m_haveWaterPosition)
		{
			this.MoveTowards(this.m_moveToWaterPosition - base.transform.position, true);
		}
	}

	protected void MoveAwayAndDespawn(float dt, bool run)
	{
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 40f);
		if (closestPlayer != null)
		{
			Vector3 normalized = (closestPlayer.transform.position - base.transform.position).normalized;
			this.MoveTo(dt, base.transform.position - normalized * 5f, 0f, run);
			return;
		}
		this.m_nview.Destroy();
	}

	protected void IdleMovement(float dt)
	{
		Vector3 centerPoint = this.m_character.IsTamed() ? base.transform.position : this.m_spawnPoint;
		Vector3 vector;
		if (this.GetPatrolPoint(out vector))
		{
			centerPoint = vector;
		}
		this.RandomMovement(dt, centerPoint);
	}

	protected void RandomMovement(float dt, Vector3 centerPoint)
	{
		if (this.m_randomMoveUpdateTimer <= 0f)
		{
			if (Utils.DistanceXZ(centerPoint, base.transform.position) > this.m_randomMoveRange * 2f)
			{
				Vector3 vector = centerPoint - base.transform.position;
				vector.y = 0f;
				vector.Normalize();
				vector = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(-30, 30), 0f) * vector;
				this.m_randomMoveTarget = base.transform.position + vector * this.m_randomMoveRange * 2f;
			}
			else
			{
				Vector3 b = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * base.transform.forward * UnityEngine.Random.Range(this.m_randomMoveRange * 0.7f, this.m_randomMoveRange);
				this.m_randomMoveTarget = centerPoint + b;
			}
			float waterLevel;
			if (this.m_character.IsFlying() && ZoneSystem.instance.GetSolidHeight(this.m_randomMoveTarget, out waterLevel))
			{
				if (waterLevel < ZoneSystem.instance.m_waterLevel)
				{
					waterLevel = ZoneSystem.instance.m_waterLevel;
				}
				this.m_randomMoveTarget.y = waterLevel + UnityEngine.Random.Range(this.m_flyAltitudeMin, this.m_flyAltitudeMax);
			}
			if (!this.IsValidRandomMovePoint(this.m_randomMoveTarget))
			{
				return;
			}
			this.m_randomMoveUpdateTimer = UnityEngine.Random.Range(this.m_randomMoveInterval, this.m_randomMoveInterval + this.m_randomMoveInterval / 2f);
			if (this.m_avoidWater && this.m_character.IsSwiming())
			{
				this.m_randomMoveUpdateTimer /= 4f;
			}
		}
		bool flag = this.IsAlerted() || Utils.DistanceXZ(base.transform.position, centerPoint) > this.m_randomMoveRange * 2f;
		if (this.MoveTo(dt, this.m_randomMoveTarget, 0f, flag) && flag)
		{
			this.m_randomMoveUpdateTimer = 0f;
		}
	}

	protected void Flee(float dt, Vector3 from)
	{
		float time = Time.time;
		if (time - this.m_fleeTargetUpdateTime > 2f)
		{
			this.m_fleeTargetUpdateTime = time;
			Vector3 point = -(from - base.transform.position);
			point.y = 0f;
			point.Normalize();
			bool flag = false;
			for (int i = 0; i < 4; i++)
			{
				this.m_fleeTarget = base.transform.position + Quaternion.Euler(0f, UnityEngine.Random.Range(-45f, 45f), 0f) * point * 25f;
				if (this.HavePath(this.m_fleeTarget) && (!this.m_avoidWater || this.m_character.IsSwiming() || ZoneSystem.instance.GetSolidHeight(this.m_fleeTarget) >= ZoneSystem.instance.m_waterLevel))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				this.m_fleeTarget = base.transform.position + Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * 25f;
			}
		}
		this.MoveTo(dt, this.m_fleeTarget, 0f, this.IsAlerted());
	}

	protected bool AvoidFire(float dt, Character moveToTarget, bool superAfraid)
	{
		if (superAfraid)
		{
			EffectArea effectArea = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Fire, 3f);
			if (effectArea)
			{
				this.m_nearFireTime = Time.time;
				this.m_nearFireArea = effectArea;
			}
			if (Time.time - this.m_nearFireTime < 6f && this.m_nearFireArea)
			{
				this.SetAlerted(true);
				this.Flee(dt, this.m_nearFireArea.transform.position);
				return true;
			}
		}
		else
		{
			EffectArea effectArea2 = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Fire, 3f);
			if (effectArea2)
			{
				if (moveToTarget != null && EffectArea.IsPointInsideArea(moveToTarget.transform.position, EffectArea.Type.Fire, 0f))
				{
					this.RandomMovementArroundPoint(dt, effectArea2.transform.position, effectArea2.GetRadius() + 3f + 1f, this.IsAlerted());
					return true;
				}
				this.RandomMovementArroundPoint(dt, effectArea2.transform.position, (effectArea2.GetRadius() + 3f) * 1.5f, this.IsAlerted());
				return true;
			}
		}
		return false;
	}

	protected void RandomMovementArroundPoint(float dt, Vector3 point, float distance, bool run)
	{
		float time = Time.time;
		if (time - this.aroundPointUpdateTime > this.m_randomCircleInterval)
		{
			this.aroundPointUpdateTime = time;
			Vector3 point2 = base.transform.position - point;
			point2.y = 0f;
			point2.Normalize();
			float num;
			if (Vector3.Distance(base.transform.position, point) < distance / 2f)
			{
				num = (float)(((double)UnityEngine.Random.value > 0.5) ? 90 : -90);
			}
			else
			{
				num = (float)(((double)UnityEngine.Random.value > 0.5) ? 40 : -40);
			}
			Vector3 a = Quaternion.Euler(0f, num, 0f) * point2;
			this.arroundPointTarget = point + a * distance;
			if (Vector3.Dot(base.transform.forward, this.arroundPointTarget - base.transform.position) < 0f)
			{
				a = Quaternion.Euler(0f, -num, 0f) * point2;
				this.arroundPointTarget = point + a * distance;
				if (this.m_serpentMovement && Vector3.Distance(point, base.transform.position) > distance / 2f && Vector3.Dot(base.transform.forward, this.arroundPointTarget - base.transform.position) < 0f)
				{
					this.arroundPointTarget = point - a * distance;
				}
			}
			if (this.m_character.IsFlying())
			{
				this.arroundPointTarget.y = this.arroundPointTarget.y + UnityEngine.Random.Range(this.m_flyAltitudeMin, this.m_flyAltitudeMax);
			}
		}
		if (this.MoveTo(dt, this.arroundPointTarget, 0f, run))
		{
			if (run)
			{
				this.aroundPointUpdateTime = 0f;
			}
			if (!this.m_serpentMovement && !run)
			{
				this.LookAt(point);
			}
		}
	}

	private bool GetSolidHeight(Vector3 p, out float height, float maxYDistance)
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(p + Vector3.up * maxYDistance, Vector3.down, out raycastHit, maxYDistance * 2f, this.m_solidRayMask))
		{
			height = raycastHit.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	protected bool IsValidRandomMovePoint(Vector3 point)
	{
		if (this.m_character.IsFlying())
		{
			return true;
		}
		float num;
		if (this.m_avoidWater && this.GetSolidHeight(point, out num, 50f))
		{
			if (this.m_character.IsSwiming())
			{
				float num2;
				if (this.GetSolidHeight(base.transform.position, out num2, 50f) && num < num2)
				{
					return false;
				}
			}
			else if (num < ZoneSystem.instance.m_waterLevel)
			{
				return false;
			}
		}
		return (!this.m_afraidOfFire && !this.m_avoidFire) || !EffectArea.IsPointInsideArea(point, EffectArea.Type.Fire, 0f);
	}

	protected virtual void OnDamaged(float damage, Character attacker)
	{
		this.m_timeSinceHurt = 0f;
	}

	protected virtual void OnDeath()
	{
		if (!string.IsNullOrEmpty(this.m_deathMessage))
		{
			MessageHud.instance.MessageAll(MessageHud.MessageType.Center, this.m_deathMessage);
		}
	}

	public bool CanSenseTarget(Character target)
	{
		return this.CanHearTarget(target) || this.CanSeeTarget(target);
	}

	public bool CanHearTarget(Character target)
	{
		if (target.IsPlayer())
		{
			Player player = target as Player;
			if (player.InDebugFlyMode() || player.InGhostMode())
			{
				return false;
			}
		}
		float num = Vector3.Distance(target.transform.position, base.transform.position);
		float num2 = this.m_hearRange;
		if (this.m_character.InInterior())
		{
			num2 = Mathf.Min(8f, num2);
		}
		return num <= num2 && num < target.GetNoiseRange();
	}

	public bool CanSeeTarget(Character target)
	{
		if (target.IsPlayer())
		{
			Player player = target as Player;
			if (player.InDebugFlyMode() || player.InGhostMode())
			{
				return false;
			}
		}
		float num = Vector3.Distance(target.transform.position, base.transform.position);
		if (num > this.m_viewRange)
		{
			return false;
		}
		float factor = 1f - num / this.m_viewRange;
		float stealthFactor = target.GetStealthFactor();
		float num2 = this.m_viewRange * stealthFactor;
		if (num > num2)
		{
			target.OnStealthSuccess(this.m_character, factor);
			return false;
		}
		if (!this.IsAlerted() && Vector3.Angle(target.transform.position - this.m_character.transform.position, base.transform.forward) > this.m_viewAngle)
		{
			target.OnStealthSuccess(this.m_character, factor);
			return false;
		}
		Vector3 vector = (target.IsCrouching() ? target.GetCenterPoint() : target.m_eye.position) - this.m_character.m_eye.position;
		if (Physics.Raycast(this.m_character.m_eye.position, vector.normalized, vector.magnitude, this.m_viewBlockMask))
		{
			target.OnStealthSuccess(this.m_character, factor);
			return false;
		}
		return true;
	}

	public bool CanSeeTarget(StaticTarget target)
	{
		Vector3 center = target.GetCenter();
		if (Vector3.Distance(center, base.transform.position) > this.m_viewRange)
		{
			return false;
		}
		Vector3 rhs = center - this.m_character.m_eye.position;
		if (!this.IsAlerted() && Vector3.Dot(base.transform.forward, rhs) < 0f)
		{
			return false;
		}
		List<Collider> allColliders = target.GetAllColliders();
		int num = Physics.RaycastNonAlloc(this.m_character.m_eye.position, rhs.normalized, BaseAI.m_tempRaycastHits, rhs.magnitude, this.m_viewBlockMask);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = BaseAI.m_tempRaycastHits[i];
			if (!allColliders.Contains(raycastHit.collider))
			{
				return false;
			}
		}
		return true;
	}

	protected void MoveTowardsSwoop(Vector3 dir, bool run, float distance)
	{
		dir = dir.normalized;
		float num = Mathf.Clamp01(Vector3.Dot(dir, this.m_character.transform.forward));
		num *= num;
		float num2 = Mathf.Clamp01(distance / this.m_serpentTurnRadius);
		float num3 = 1f - (1f - num2) * (1f - num);
		num3 = num3 * 0.9f + 0.1f;
		Vector3 moveDir = base.transform.forward * num3;
		this.LookTowards(dir);
		this.m_character.SetMoveDir(moveDir);
		this.m_character.SetRun(run);
	}

	protected void MoveTowards(Vector3 dir, bool run)
	{
		dir = dir.normalized;
		this.LookTowards(dir);
		if (this.m_smoothMovement)
		{
			float num = Vector3.Angle(dir, base.transform.forward);
			float d = 1f - Mathf.Clamp01(num / this.m_moveMinAngle);
			Vector3 moveDir = base.transform.forward * d;
			moveDir.y = dir.y;
			this.m_character.SetMoveDir(moveDir);
			this.m_character.SetRun(run);
			if (this.m_jumpInterval > 0f && this.m_jumpTimer >= this.m_jumpInterval)
			{
				this.m_jumpTimer = 0f;
				this.m_character.Jump();
				return;
			}
		}
		else if (this.IsLookingTowards(dir, this.m_moveMinAngle))
		{
			this.m_character.SetMoveDir(dir);
			this.m_character.SetRun(run);
			if (this.m_jumpInterval > 0f && this.m_jumpTimer >= this.m_jumpInterval)
			{
				this.m_jumpTimer = 0f;
				this.m_character.Jump();
				return;
			}
		}
		else
		{
			this.StopMoving();
		}
	}

	protected void LookAt(Vector3 point)
	{
		Vector3 vector = point - this.m_character.m_eye.position;
		if (Utils.LengthXZ(vector) < 0.01f)
		{
			return;
		}
		vector.Normalize();
		this.LookTowards(vector);
	}

	protected void LookTowards(Vector3 dir)
	{
		this.m_character.SetLookDir(dir);
	}

	protected bool IsLookingAt(Vector3 point, float minAngle)
	{
		return this.IsLookingTowards((point - base.transform.position).normalized, minAngle);
	}

	protected bool IsLookingTowards(Vector3 dir, float minAngle)
	{
		dir.y = 0f;
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		return Vector3.Angle(dir, forward) < minAngle;
	}

	protected void StopMoving()
	{
		this.m_character.SetMoveDir(Vector3.zero);
	}

	protected bool HavePath(Vector3 target)
	{
		if (this.m_character.IsFlying())
		{
			return true;
		}
		float time = Time.time;
		float num = time - this.m_lastHavePathTime;
		Vector3 position = base.transform.position;
		if (Vector3.Distance(position, this.m_havePathFrom) > 2f || Vector3.Distance(target, this.m_havePathTarget) > 1f || num > 5f)
		{
			this.m_havePathFrom = position;
			this.m_havePathTarget = target;
			this.m_lastHavePathTime = time;
			this.m_lastHavePathResult = Pathfinding.instance.HavePath(position, target, this.m_pathAgentType);
		}
		return this.m_lastHavePathResult;
	}

	protected bool FindPath(Vector3 target)
	{
		float time = Time.time;
		float num = time - this.m_lastFindPathTime;
		if (num < 1f)
		{
			return this.m_lastFindPathResult;
		}
		if (Vector3.Distance(target, this.m_lastFindPathTarget) < 1f && num < 5f)
		{
			return this.m_lastFindPathResult;
		}
		this.m_lastFindPathTarget = target;
		this.m_lastFindPathTime = time;
		this.m_lastFindPathResult = Pathfinding.instance.GetPath(base.transform.position, target, this.m_path, this.m_pathAgentType, false, true);
		return this.m_lastFindPathResult;
	}

	protected bool FoundPath()
	{
		return this.m_lastFindPathResult;
	}

	protected bool MoveTo(float dt, Vector3 point, float dist, bool run)
	{
		if (this.m_character.m_flying)
		{
			dist = Mathf.Max(dist, 1f);
			float num;
			if (ZoneSystem.instance.GetSolidHeight(point, out num))
			{
				point.y = Mathf.Max(point.y, num + this.m_flyAltitudeMin);
			}
			return this.MoveAndAvoid(dt, point, dist, run);
		}
		float num2 = run ? 1f : 0.5f;
		if (this.m_serpentMovement)
		{
			num2 = 3f;
		}
		if (Utils.DistanceXZ(point, base.transform.position) < Mathf.Max(dist, num2))
		{
			this.StopMoving();
			return true;
		}
		if (!this.FindPath(point))
		{
			this.StopMoving();
			return true;
		}
		if (this.m_path.Count == 0)
		{
			this.StopMoving();
			return true;
		}
		Vector3 vector = this.m_path[0];
		if (Utils.DistanceXZ(vector, base.transform.position) < num2)
		{
			this.m_path.RemoveAt(0);
			if (this.m_path.Count == 0)
			{
				this.StopMoving();
				return true;
			}
		}
		else if (this.m_serpentMovement)
		{
			float distance = Vector3.Distance(vector, base.transform.position);
			Vector3 normalized = (vector - base.transform.position).normalized;
			this.MoveTowardsSwoop(normalized, run, distance);
		}
		else
		{
			Vector3 normalized2 = (vector - base.transform.position).normalized;
			this.MoveTowards(normalized2, run);
		}
		return false;
	}

	protected bool MoveAndAvoid(float dt, Vector3 point, float dist, bool run)
	{
		Vector3 vector = point - base.transform.position;
		if (this.m_character.IsFlying())
		{
			if (vector.magnitude < dist)
			{
				this.StopMoving();
				return true;
			}
		}
		else
		{
			vector.y = 0f;
			if (vector.magnitude < dist)
			{
				this.StopMoving();
				return true;
			}
		}
		vector.Normalize();
		float radius = this.m_character.GetRadius();
		float num = radius + 1f;
		if (!this.m_character.InAttack())
		{
			this.m_getOutOfCornerTimer -= dt;
			if (this.m_getOutOfCornerTimer > 0f)
			{
				Vector3 dir = Quaternion.Euler(0f, this.m_getOutOfCornerAngle, 0f) * -vector;
				this.MoveTowards(dir, run);
				return false;
			}
			this.m_stuckTimer += Time.fixedDeltaTime;
			if (this.m_stuckTimer > 1.5f)
			{
				if (Vector3.Distance(base.transform.position, this.m_lastPosition) < 0.2f)
				{
					this.m_getOutOfCornerTimer = 4f;
					this.m_getOutOfCornerAngle = UnityEngine.Random.Range(-20f, 20f);
					this.m_stuckTimer = 0f;
					return false;
				}
				this.m_stuckTimer = 0f;
				this.m_lastPosition = base.transform.position;
			}
		}
		if (this.CanMove(vector, radius, num))
		{
			this.MoveTowards(vector, run);
		}
		else
		{
			Vector3 forward = base.transform.forward;
			if (this.m_character.IsFlying())
			{
				forward.y = 0.2f;
				forward.Normalize();
			}
			Vector3 b = base.transform.right * radius * 0.75f;
			float num2 = num * 1.5f;
			Vector3 centerPoint = this.m_character.GetCenterPoint();
			float num3 = this.Raycast(centerPoint - b, forward, num2, 0.1f);
			float num4 = this.Raycast(centerPoint + b, forward, num2, 0.1f);
			if (num3 >= num2 && num4 >= num2)
			{
				this.MoveTowards(forward, run);
			}
			else
			{
				Vector3 dir2 = Quaternion.Euler(0f, -20f, 0f) * forward;
				Vector3 dir3 = Quaternion.Euler(0f, 20f, 0f) * forward;
				if (num3 > num4)
				{
					this.MoveTowards(dir2, run);
				}
				else
				{
					this.MoveTowards(dir3, run);
				}
			}
		}
		return false;
	}

	private bool CanMove(Vector3 dir, float checkRadius, float distance)
	{
		Vector3 centerPoint = this.m_character.GetCenterPoint();
		Vector3 right = base.transform.right;
		return this.Raycast(centerPoint, dir, distance, 0.1f) >= distance && this.Raycast(centerPoint - right * (checkRadius - 0.1f), dir, distance, 0.1f) >= distance && this.Raycast(centerPoint + right * (checkRadius - 0.1f), dir, distance, 0.1f) >= distance;
	}

	public float Raycast(Vector3 p, Vector3 dir, float distance, float radius)
	{
		if (radius == 0f)
		{
			RaycastHit raycastHit;
			if (Physics.Raycast(p, dir, out raycastHit, distance, this.m_solidRayMask))
			{
				return raycastHit.distance;
			}
			return distance;
		}
		else
		{
			RaycastHit raycastHit2;
			if (Physics.SphereCast(p, radius, dir, out raycastHit2, distance, this.m_solidRayMask))
			{
				return raycastHit2.distance;
			}
			return distance;
		}
	}

	public bool IsEnemey(Character other)
	{
		return BaseAI.IsEnemy(this.m_character, other);
	}

	public static bool IsEnemy(Character a, Character b)
	{
		if (a == b)
		{
			return false;
		}
		Character.Faction faction = a.GetFaction();
		Character.Faction faction2 = b.GetFaction();
		if (faction == faction2)
		{
			return false;
		}
		bool flag = a.IsTamed();
		bool flag2 = b.IsTamed();
		if (flag || flag2)
		{
			return (!flag || !flag2) && (!flag || faction2 != Character.Faction.Players) && (!flag2 || faction != Character.Faction.Players);
		}
		switch (faction)
		{
		case Character.Faction.Players:
			return true;
		case Character.Faction.AnimalsVeg:
			return true;
		case Character.Faction.ForestMonsters:
			return faction2 != Character.Faction.AnimalsVeg && faction2 != Character.Faction.Boss;
		case Character.Faction.Undead:
			return faction2 != Character.Faction.Demon && faction2 != Character.Faction.Boss;
		case Character.Faction.Demon:
			return faction2 != Character.Faction.Undead && faction2 != Character.Faction.Boss;
		case Character.Faction.MountainMonsters:
			return faction2 != Character.Faction.Boss;
		case Character.Faction.SeaMonsters:
			return faction2 != Character.Faction.Boss;
		case Character.Faction.PlainsMonsters:
			return faction2 != Character.Faction.Boss;
		case Character.Faction.Boss:
			return faction2 == Character.Faction.Players;
		default:
			return false;
		}
	}

	protected StaticTarget FindRandomStaticTarget(float maxDistance, bool priorityTargetsOnly)
	{
		float radius = this.m_character.GetRadius();
		Collider[] array = Physics.OverlapSphere(base.transform.position, radius + maxDistance, this.m_monsterTargetRayMask);
		if (array.Length == 0)
		{
			return null;
		}
		List<StaticTarget> list = new List<StaticTarget>();
		Collider[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			StaticTarget componentInParent = array2[i].GetComponentInParent<StaticTarget>();
			if (!(componentInParent == null) && componentInParent.IsValidMonsterTarget())
			{
				if (priorityTargetsOnly)
				{
					if (!componentInParent.m_primaryTarget)
					{
						goto IL_80;
					}
				}
				else if (!componentInParent.m_randomTarget)
				{
					goto IL_80;
				}
				if (this.CanSeeTarget(componentInParent))
				{
					list.Add(componentInParent);
				}
			}
			IL_80:;
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	protected StaticTarget FindClosestStaticPriorityTarget(float maxDistance)
	{
		float num = Mathf.Min(maxDistance, this.m_viewRange);
		Collider[] array = Physics.OverlapSphere(base.transform.position, num, this.m_monsterTargetRayMask);
		if (array.Length == 0)
		{
			return null;
		}
		StaticTarget result = null;
		float num2 = num;
		Collider[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			StaticTarget componentInParent = array2[i].GetComponentInParent<StaticTarget>();
			if (!(componentInParent == null) && componentInParent.IsValidMonsterTarget() && componentInParent.m_primaryTarget)
			{
				float num3 = Vector3.Distance(base.transform.position, componentInParent.GetCenter());
				if (num3 < num2 && this.CanSeeTarget(componentInParent))
				{
					result = componentInParent;
					num2 = num3;
				}
			}
		}
		return result;
	}

	protected void HaveFriendsInRange(float range, out Character hurtFriend, out Character friend)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		friend = this.HaveFriendInRange(allCharacters, range);
		hurtFriend = this.HaveHurtFriendInRange(allCharacters, range);
	}

	private Character HaveFriendInRange(List<Character> characters, float range)
	{
		foreach (Character character in characters)
		{
			if (!(character == this.m_character) && !BaseAI.IsEnemy(this.m_character, character) && Vector3.Distance(character.transform.position, base.transform.position) <= range)
			{
				return character;
			}
		}
		return null;
	}

	protected Character HaveFriendInRange(float range)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		return this.HaveFriendInRange(allCharacters, range);
	}

	private Character HaveHurtFriendInRange(List<Character> characters, float range)
	{
		foreach (Character character in characters)
		{
			if (!BaseAI.IsEnemy(this.m_character, character) && Vector3.Distance(character.transform.position, base.transform.position) <= range && character.GetHealth() < character.GetMaxHealth())
			{
				return character;
			}
		}
		return null;
	}

	protected Character HaveHurtFriendInRange(float range)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		return this.HaveHurtFriendInRange(allCharacters, range);
	}

	protected Character FindEnemy()
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		Character character = null;
		float num = 99999f;
		foreach (Character character2 in allCharacters)
		{
			if (BaseAI.IsEnemy(this.m_character, character2) && !character2.IsDead())
			{
				BaseAI baseAI = character2.GetBaseAI();
				if ((!(baseAI != null) || !baseAI.IsSleeping()) && this.CanSenseTarget(character2))
				{
					float num2 = Vector3.Distance(character2.transform.position, base.transform.position);
					if (num2 < num || character == null)
					{
						character = character2;
						num = num2;
					}
				}
			}
		}
		if (!(character == null) || !this.HuntPlayer())
		{
			return character;
		}
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 200f);
		if (closestPlayer && (closestPlayer.InDebugFlyMode() || closestPlayer.InGhostMode()))
		{
			return null;
		}
		return closestPlayer;
	}

	public void SetHuntPlayer(bool hunt)
	{
		if (this.m_huntPlayer == hunt)
		{
			return;
		}
		this.m_huntPlayer = hunt;
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set("huntplayer", this.m_huntPlayer);
		}
	}

	public virtual bool HuntPlayer()
	{
		return this.m_huntPlayer;
	}

	protected bool HaveAlertedCreatureInRange(float range)
	{
		foreach (BaseAI baseAI in BaseAI.m_instances)
		{
			if (Vector3.Distance(base.transform.position, baseAI.transform.position) < range && baseAI.IsAlerted())
			{
				return true;
			}
		}
		return false;
	}

	public static void AlertAllInRange(Vector3 center, float range, Character attacker)
	{
		foreach (BaseAI baseAI in BaseAI.m_instances)
		{
			if ((!attacker || baseAI.IsEnemey(attacker)) && Vector3.Distance(baseAI.transform.position, center) < range)
			{
				baseAI.Alert();
			}
		}
	}

	public void Alert()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.IsAlerted())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.SetAlerted(true);
			return;
		}
		this.m_nview.InvokeRPC("Alert", Array.Empty<object>());
	}

	private void RPC_Alert(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.SetAlerted(true);
	}

	protected virtual void SetAlerted(bool alert)
	{
		if (this.m_alerted == alert)
		{
			return;
		}
		this.m_alerted = alert;
		this.m_animator.SetBool("alert", this.m_alerted);
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set("alert", this.m_alerted);
		}
		if (this.m_alerted)
		{
			this.m_alertedEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
		}
	}

	public static bool InStealthRange(Character me)
	{
		bool result = false;
		foreach (BaseAI baseAI in BaseAI.GetAllInstances())
		{
			if (BaseAI.IsEnemy(me, baseAI.m_character))
			{
				float num = Vector3.Distance(me.transform.position, baseAI.transform.position);
				if (num < baseAI.m_viewRange || num < 10f)
				{
					if (baseAI.IsAlerted())
					{
						return false;
					}
					result = true;
				}
			}
		}
		return result;
	}

	public static Character FindClosestEnemy(Character me, Vector3 point, float maxDistance)
	{
		Character character = null;
		float num = maxDistance;
		foreach (Character character2 in Character.GetAllCharacters())
		{
			if (BaseAI.IsEnemy(me, character2))
			{
				float num2 = Vector3.Distance(character2.transform.position, point);
				if (character == null || num2 < num)
				{
					character = character2;
					num = num2;
				}
			}
		}
		return character;
	}

	public static Character FindRandomEnemy(Character me, Vector3 point, float maxDistance)
	{
		List<Character> list = new List<Character>();
		foreach (Character character in Character.GetAllCharacters())
		{
			if (BaseAI.IsEnemy(me, character) && Vector3.Distance(character.transform.position, point) < maxDistance)
			{
				list.Add(character);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	public bool IsAlerted()
	{
		return this.m_alerted;
	}

	protected float GetAltitude()
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(this.m_character.transform.position);
		return this.m_character.transform.position.y - groundHeight;
	}

	public static List<BaseAI> GetAllInstances()
	{
		return BaseAI.m_instances;
	}

	protected virtual void OnDrawGizmosSelected()
	{
		if (this.m_lastFindPathResult)
		{
			Gizmos.color = Color.yellow;
			for (int i = 0; i < this.m_path.Count - 1; i++)
			{
				Vector3 a = this.m_path[i];
				Vector3 a2 = this.m_path[i + 1];
				Gizmos.DrawLine(a + Vector3.up * 0.1f, a2 + Vector3.up * 0.1f);
			}
			Gizmos.color = Color.cyan;
			foreach (Vector3 a3 in this.m_path)
			{
				Gizmos.DrawSphere(a3 + Vector3.up * 0.1f, 0.1f);
			}
			Gizmos.color = Color.green;
			Gizmos.DrawLine(base.transform.position, this.m_lastFindPathTarget);
			Gizmos.DrawSphere(this.m_lastFindPathTarget, 0.2f);
			return;
		}
		Gizmos.color = Color.red;
		Gizmos.DrawLine(base.transform.position, this.m_lastFindPathTarget);
		Gizmos.DrawSphere(this.m_lastFindPathTarget, 0.2f);
	}

	public virtual bool IsSleeping()
	{
		return false;
	}

	public bool HasZDOOwner()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().HasOwner();
	}

	public static bool CanUseAttack(Character character, ItemDrop.ItemData item)
	{
		bool flag = character.IsFlying();
		bool flag2 = character.IsSwiming();
		return (item.m_shared.m_aiWhenFlying && flag) || (item.m_shared.m_aiWhenWalking && !flag && !flag2) || (item.m_shared.m_aiWhenSwiming && flag2);
	}

	public virtual Character GetTargetCreature()
	{
		return null;
	}

	private float m_lastMoveToWaterUpdate;

	private bool m_haveWaterPosition;

	private Vector3 m_moveToWaterPosition = Vector3.zero;

	private float m_fleeTargetUpdateTime;

	private Vector3 m_fleeTarget = Vector3.zero;

	private float m_nearFireTime;

	private EffectArea m_nearFireArea;

	private float aroundPointUpdateTime;

	private Vector3 arroundPointTarget = Vector3.zero;

	private const bool m_debugDraw = false;

	public float m_viewRange = 50f;

	public float m_viewAngle = 90f;

	public float m_hearRange = 9999f;

	private const float m_interiorMaxHearRange = 8f;

	private const float m_despawnDistance = 80f;

	private const float m_regenAllHPTime = 3600f;

	public EffectList m_alertedEffects = new EffectList();

	public EffectList m_idleSound = new EffectList();

	public float m_idleSoundInterval = 5f;

	public float m_idleSoundChance = 0.5f;

	public Pathfinding.AgentType m_pathAgentType = Pathfinding.AgentType.Humanoid;

	public float m_moveMinAngle = 10f;

	public bool m_smoothMovement = true;

	public bool m_serpentMovement;

	public float m_serpentTurnRadius = 20f;

	public float m_jumpInterval;

	[Header("Random circle")]
	public float m_randomCircleInterval = 2f;

	[Header("Random movement")]
	public float m_randomMoveInterval = 5f;

	public float m_randomMoveRange = 4f;

	[Header("Fly behaviour")]
	public bool m_randomFly;

	public float m_chanceToTakeoff = 1f;

	public float m_chanceToLand = 1f;

	public float m_groundDuration = 10f;

	public float m_airDuration = 10f;

	public float m_maxLandAltitude = 5f;

	public float m_flyAltitudeMin = 3f;

	public float m_flyAltitudeMax = 10f;

	public float m_takeoffTime = 5f;

	[Header("Other")]
	public bool m_avoidFire;

	public bool m_afraidOfFire;

	public bool m_avoidWater = true;

	public string m_spawnMessage = "";

	public string m_deathMessage = "";

	private bool m_patrol;

	private Vector3 m_patrolPoint = Vector3.zero;

	private float m_patrolPointUpdateTime;

	protected ZNetView m_nview;

	protected Character m_character;

	protected ZSyncAnimation m_animator;

	protected Rigidbody m_body;

	private float m_updateTimer;

	private int m_solidRayMask;

	private int m_viewBlockMask;

	private int m_monsterTargetRayMask;

	private Vector3 m_randomMoveTarget = Vector3.zero;

	private float m_randomMoveUpdateTimer;

	private float m_jumpTimer;

	private float m_randomFlyTimer;

	private float m_regenTimer;

	protected bool m_alerted;

	protected bool m_huntPlayer;

	protected Vector3 m_spawnPoint = Vector3.zero;

	private const float m_getOfOfCornerMaxAngle = 20f;

	private float m_getOutOfCornerTimer;

	private float m_getOutOfCornerAngle;

	private Vector3 m_lastPosition = Vector3.zero;

	private float m_stuckTimer;

	protected float m_timeSinceHurt = 99999f;

	private Vector3 m_havePathTarget = new Vector3(-999999f, -999999f, -999999f);

	private Vector3 m_havePathFrom = new Vector3(-999999f, -999999f, -999999f);

	private float m_lastHavePathTime;

	private bool m_lastHavePathResult;

	private Vector3 m_lastFindPathTarget = new Vector3(-999999f, -999999f, -999999f);

	private float m_lastFindPathTime;

	private bool m_lastFindPathResult;

	private List<Vector3> m_path = new List<Vector3>();

	private static RaycastHit[] m_tempRaycastHits = new RaycastHit[128];

	private static List<BaseAI> m_instances = new List<BaseAI>();

	private static int worldTimeHash = "lastWorldTime".GetStableHashCode();

	private static int spawnTimeHash = "spawntime".GetStableHashCode();
}
