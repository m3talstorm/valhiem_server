using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class Attack
{
	public bool StartDraw(Humanoid character, ItemDrop.ItemData weapon)
	{
		if (!Attack.HaveAmmo(character, weapon))
		{
			return false;
		}
		Attack.EquipAmmoItem(character, weapon);
		return true;
	}

	public bool Start(Humanoid character, Rigidbody body, ZSyncAnimation zanim, CharacterAnimEvent animEvent, VisEquipment visEquipment, ItemDrop.ItemData weapon, Attack previousAttack, float timeSinceLastAttack, float attackDrawPercentage)
	{
		if (this.m_attackAnimation == "")
		{
			return false;
		}
		this.m_character = character;
		this.m_baseAI = this.m_character.GetComponent<BaseAI>();
		this.m_body = body;
		this.m_zanim = zanim;
		this.m_animEvent = animEvent;
		this.m_visEquipment = visEquipment;
		this.m_weapon = weapon;
		this.m_attackDrawPercentage = attackDrawPercentage;
		if (Attack.m_attackMask == 0)
		{
			Attack.m_attackMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"piece_nonsolid",
				"character",
				"character_net",
				"character_ghost",
				"hitbox",
				"character_noenv",
				"vehicle"
			});
			Attack.m_attackMaskTerrain = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"piece_nonsolid",
				"terrain",
				"character",
				"character_net",
				"character_ghost",
				"hitbox",
				"character_noenv",
				"vehicle"
			});
		}
		float staminaUsage = this.GetStaminaUsage();
		if (staminaUsage > 0f && !character.HaveStamina(staminaUsage + 0.1f))
		{
			if (character.IsPlayer())
			{
				Hud.instance.StaminaBarNoStaminaFlash();
			}
			return false;
		}
		if (!Attack.HaveAmmo(character, this.m_weapon))
		{
			return false;
		}
		Attack.EquipAmmoItem(character, this.m_weapon);
		if (this.m_attackChainLevels > 1)
		{
			if (previousAttack != null && previousAttack.m_attackAnimation == this.m_attackAnimation)
			{
				this.m_currentAttackCainLevel = previousAttack.m_nextAttackChainLevel;
			}
			if (this.m_currentAttackCainLevel >= this.m_attackChainLevels || timeSinceLastAttack > 0.2f)
			{
				this.m_currentAttackCainLevel = 0;
			}
			this.m_zanim.SetTrigger(this.m_attackAnimation + this.m_currentAttackCainLevel);
		}
		else if (this.m_attackRandomAnimations >= 2)
		{
			int num = UnityEngine.Random.Range(0, this.m_attackRandomAnimations);
			this.m_zanim.SetTrigger(this.m_attackAnimation + num);
		}
		else
		{
			this.m_zanim.SetTrigger(this.m_attackAnimation);
		}
		if (character.IsPlayer() && this.m_attackType != Attack.AttackType.None && this.m_currentAttackCainLevel == 0)
		{
			if (ZInput.IsMouseActive() || this.m_attackType == Attack.AttackType.Projectile)
			{
				character.transform.rotation = character.GetLookYaw();
				this.m_body.rotation = character.transform.rotation;
			}
			else if (ZInput.IsGamepadActive() && !character.IsBlocking() && character.GetMoveDir().magnitude > 0.3f)
			{
				character.transform.rotation = Quaternion.LookRotation(character.GetMoveDir());
				this.m_body.rotation = character.transform.rotation;
			}
		}
		weapon.m_lastAttackTime = Time.time;
		this.m_animEvent.ResetChain();
		return true;
	}

	private float GetStaminaUsage()
	{
		if (this.m_attackStamina <= 0f)
		{
			return 0f;
		}
		float attackStamina = this.m_attackStamina;
		float skillFactor = this.m_character.GetSkillFactor(this.m_weapon.m_shared.m_skillType);
		return attackStamina - attackStamina * 0.33f * skillFactor;
	}

	public void Update(float dt)
	{
		this.m_time += dt;
		if (this.m_character.InAttack())
		{
			if (!this.m_wasInAttack)
			{
				this.m_character.UseStamina(this.GetStaminaUsage());
				Transform attackOrigin = this.GetAttackOrigin();
				this.m_weapon.m_shared.m_startEffect.Create(attackOrigin.position, this.m_character.transform.rotation, attackOrigin, 1f);
				this.m_startEffect.Create(attackOrigin.position, this.m_character.transform.rotation, attackOrigin, 1f);
				this.m_character.AddNoise(this.m_attackStartNoise);
				this.m_nextAttackChainLevel = this.m_currentAttackCainLevel + 1;
				if (this.m_nextAttackChainLevel >= this.m_attackChainLevels)
				{
					this.m_nextAttackChainLevel = 0;
				}
			}
			this.m_wasInAttack = true;
		}
		else if (this.m_wasInAttack)
		{
			this.OnAttackDone();
			this.m_wasInAttack = false;
		}
		this.UpdateProjectile(dt);
	}

	private void OnAttackDone()
	{
		if (this.m_visEquipment)
		{
			this.m_visEquipment.SetWeaponTrails(false);
		}
	}

	public void Stop()
	{
		if (this.m_wasInAttack)
		{
			this.OnAttackDone();
			this.m_wasInAttack = false;
		}
	}

	public void OnAttackTrigger()
	{
		if (!this.UseAmmo())
		{
			return;
		}
		switch (this.m_attackType)
		{
		case Attack.AttackType.Horizontal:
		case Attack.AttackType.Vertical:
			this.DoMeleeAttack();
			break;
		case Attack.AttackType.Projectile:
			this.ProjectileAttackTriggered();
			break;
		case Attack.AttackType.None:
			this.DoNonAttack();
			break;
		case Attack.AttackType.Area:
			this.DoAreaAttack();
			break;
		}
		if (this.m_consumeItem)
		{
			this.ConsumeItem();
		}
	}

	private void ConsumeItem()
	{
		if (this.m_weapon.m_shared.m_maxStackSize > 1 && this.m_weapon.m_stack > 1)
		{
			this.m_weapon.m_stack--;
			return;
		}
		this.m_character.UnequipItem(this.m_weapon, false);
		this.m_character.GetInventory().RemoveItem(this.m_weapon);
	}

	private static bool EquipAmmoItem(Humanoid character, ItemDrop.ItemData weapon)
	{
		if (!string.IsNullOrEmpty(weapon.m_shared.m_ammoType))
		{
			ItemDrop.ItemData ammoItem = character.GetAmmoItem();
			if (ammoItem != null && character.GetInventory().ContainsItem(ammoItem) && ammoItem.m_shared.m_ammoType == weapon.m_shared.m_ammoType)
			{
				return true;
			}
			ItemDrop.ItemData ammoItem2 = character.GetInventory().GetAmmoItem(weapon.m_shared.m_ammoType);
			if (ammoItem2.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo)
			{
				return character.EquipItem(ammoItem2, true);
			}
		}
		return true;
	}

	private static bool HaveAmmo(Humanoid character, ItemDrop.ItemData weapon)
	{
		if (string.IsNullOrEmpty(weapon.m_shared.m_ammoType))
		{
			return true;
		}
		ItemDrop.ItemData itemData = character.GetAmmoItem();
		if (itemData != null && (!character.GetInventory().ContainsItem(itemData) || itemData.m_shared.m_ammoType != weapon.m_shared.m_ammoType))
		{
			itemData = null;
		}
		if (itemData == null)
		{
			itemData = character.GetInventory().GetAmmoItem(weapon.m_shared.m_ammoType);
		}
		if (itemData == null)
		{
			character.Message(MessageHud.MessageType.Center, "$msg_outof " + weapon.m_shared.m_ammoType, 0, null);
			return false;
		}
		return itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable || character.CanConsumeItem(itemData);
	}

	private bool UseAmmo()
	{
		this.m_ammoItem = null;
		if (string.IsNullOrEmpty(this.m_weapon.m_shared.m_ammoType))
		{
			return true;
		}
		ItemDrop.ItemData itemData = this.m_character.GetAmmoItem();
		if (itemData != null && (!this.m_character.GetInventory().ContainsItem(itemData) || itemData.m_shared.m_ammoType != this.m_weapon.m_shared.m_ammoType))
		{
			itemData = null;
		}
		if (itemData == null)
		{
			itemData = this.m_character.GetInventory().GetAmmoItem(this.m_weapon.m_shared.m_ammoType);
		}
		if (itemData == null)
		{
			this.m_character.Message(MessageHud.MessageType.Center, "$msg_outof " + this.m_weapon.m_shared.m_ammoType, 0, null);
			return false;
		}
		if (itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable)
		{
			bool flag = this.m_character.ConsumeItem(this.m_character.GetInventory(), itemData);
			if (flag)
			{
				this.m_ammoItem = itemData;
			}
			return flag;
		}
		this.m_character.GetInventory().RemoveItem(itemData, 1);
		this.m_ammoItem = itemData;
		return true;
	}

	private void ProjectileAttackTriggered()
	{
		Vector3 pos;
		Vector3 forward;
		this.GetProjectileSpawnPoint(out pos, out forward);
		this.m_weapon.m_shared.m_triggerEffect.Create(pos, Quaternion.LookRotation(forward), null, 1f);
		this.m_triggerEffect.Create(pos, Quaternion.LookRotation(forward), null, 1f);
		if (this.m_weapon.m_shared.m_useDurability && this.m_character.IsPlayer())
		{
			this.m_weapon.m_durability -= this.m_weapon.m_shared.m_useDurabilityDrain;
		}
		if (this.m_projectileBursts == 1)
		{
			this.FireProjectileBurst();
			return;
		}
		this.m_projectileAttackStarted = true;
	}

	private void UpdateProjectile(float dt)
	{
		if (this.m_projectileAttackStarted && this.m_projectileBurstsFired < this.m_projectileBursts)
		{
			this.m_projectileFireTimer -= dt;
			if (this.m_projectileFireTimer <= 0f)
			{
				this.m_projectileFireTimer = this.m_burstInterval;
				this.FireProjectileBurst();
				this.m_projectileBurstsFired++;
			}
		}
	}

	private Transform GetAttackOrigin()
	{
		if (this.m_attackOriginJoint.Length > 0)
		{
			return Utils.FindChild(this.m_character.GetVisual().transform, this.m_attackOriginJoint);
		}
		return this.m_character.transform;
	}

	private void GetProjectileSpawnPoint(out Vector3 spawnPoint, out Vector3 aimDir)
	{
		Transform attackOrigin = this.GetAttackOrigin();
		Transform transform = this.m_character.transform;
		spawnPoint = attackOrigin.position + transform.up * this.m_attackHeight + transform.forward * this.m_attackRange + transform.right * this.m_attackOffset;
		aimDir = this.m_character.GetAimDir(spawnPoint);
		if (this.m_baseAI)
		{
			Character targetCreature = this.m_baseAI.GetTargetCreature();
			if (targetCreature)
			{
				Vector3 normalized = (targetCreature.GetCenterPoint() - spawnPoint).normalized;
				aimDir = Vector3.RotateTowards(this.m_character.transform.forward, normalized, 1.5707964f, 1f);
			}
		}
	}

	private void FireProjectileBurst()
	{
		ItemDrop.ItemData ammoItem = this.m_ammoItem;
		GameObject attackProjectile = this.m_attackProjectile;
		float num = this.m_projectileVel;
		float num2 = this.m_projectileVelMin;
		float num3 = this.m_projectileAccuracy;
		float num4 = this.m_projectileAccuracyMin;
		float num5 = this.m_attackHitNoise;
		if (ammoItem != null && ammoItem.m_shared.m_attack.m_attackProjectile)
		{
			attackProjectile = ammoItem.m_shared.m_attack.m_attackProjectile;
			num += ammoItem.m_shared.m_attack.m_projectileVel;
			num2 += ammoItem.m_shared.m_attack.m_projectileVelMin;
			num3 += ammoItem.m_shared.m_attack.m_projectileAccuracy;
			num4 += ammoItem.m_shared.m_attack.m_projectileAccuracyMin;
			num5 += ammoItem.m_shared.m_attack.m_attackHitNoise;
		}
		float num6 = this.m_character.GetRandomSkillFactor(this.m_weapon.m_shared.m_skillType);
		if (this.m_weapon.m_shared.m_holdDurationMin > 0f)
		{
			num3 = Mathf.Lerp(num4, num3, Mathf.Pow(this.m_attackDrawPercentage, 0.5f));
			num6 *= this.m_attackDrawPercentage;
			num = Mathf.Lerp(num2, num, this.m_attackDrawPercentage);
		}
		Vector3 position;
		Vector3 vector;
		this.GetProjectileSpawnPoint(out position, out vector);
		Transform transform = this.m_character.transform;
		if (this.m_useCharacterFacing)
		{
			Vector3 forward = Vector3.forward;
			if (this.m_useCharacterFacingYAim)
			{
				forward.y = vector.y;
			}
			vector = transform.TransformDirection(forward);
		}
		if (this.m_launchAngle != 0f)
		{
			Vector3 axis = Vector3.Cross(Vector3.up, vector);
			vector = Quaternion.AngleAxis(this.m_launchAngle, axis) * vector;
		}
		for (int i = 0; i < this.m_projectiles; i++)
		{
			if (this.m_destroyPreviousProjectile && this.m_weapon.m_lastProjectile)
			{
				ZNetScene.instance.Destroy(this.m_weapon.m_lastProjectile);
				this.m_weapon.m_lastProjectile = null;
			}
			Vector3 vector2 = vector;
			Vector3 axis2 = Vector3.Cross(vector2, Vector3.up);
			Quaternion rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(-num3, num3), Vector3.up);
			vector2 = Quaternion.AngleAxis(UnityEngine.Random.Range(-num3, num3), axis2) * vector2;
			vector2 = rotation * vector2;
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(attackProjectile, position, Quaternion.LookRotation(vector2));
			HitData hitData = new HitData();
			hitData.m_toolTier = this.m_weapon.m_shared.m_toolTier;
			hitData.m_pushForce = this.m_weapon.m_shared.m_attackForce * this.m_forceMultiplier;
			hitData.m_backstabBonus = this.m_weapon.m_shared.m_backstabBonus;
			hitData.m_staggerMultiplier = this.m_staggerMultiplier;
			hitData.m_damage.Add(this.m_weapon.GetDamage(), 1);
			hitData.m_statusEffect = (this.m_weapon.m_shared.m_attackStatusEffect ? this.m_weapon.m_shared.m_attackStatusEffect.name : "");
			hitData.m_blockable = this.m_weapon.m_shared.m_blockable;
			hitData.m_dodgeable = this.m_weapon.m_shared.m_dodgeable;
			hitData.m_skill = this.m_weapon.m_shared.m_skillType;
			hitData.SetAttacker(this.m_character);
			if (ammoItem != null)
			{
				hitData.m_damage.Add(ammoItem.GetDamage(), 1);
				hitData.m_pushForce += ammoItem.m_shared.m_attackForce;
				if (ammoItem.m_shared.m_attackStatusEffect != null)
				{
					hitData.m_statusEffect = ammoItem.m_shared.m_attackStatusEffect.name;
				}
				if (!ammoItem.m_shared.m_blockable)
				{
					hitData.m_blockable = false;
				}
				if (!ammoItem.m_shared.m_dodgeable)
				{
					hitData.m_dodgeable = false;
				}
			}
			hitData.m_pushForce *= num6;
			hitData.m_damage.Modify(this.m_damageMultiplier);
			hitData.m_damage.Modify(num6);
			hitData.m_damage.Modify(this.GetLevelDamageFactor());
			this.m_character.GetSEMan().ModifyAttack(this.m_weapon.m_shared.m_skillType, ref hitData);
			IProjectile component = gameObject.GetComponent<IProjectile>();
			if (component != null)
			{
				component.Setup(this.m_character, vector2 * num, num5, hitData, this.m_weapon);
			}
			this.m_weapon.m_lastProjectile = gameObject;
		}
	}

	private void DoNonAttack()
	{
		if (this.m_weapon.m_shared.m_useDurability && this.m_character.IsPlayer())
		{
			this.m_weapon.m_durability -= this.m_weapon.m_shared.m_useDurabilityDrain;
		}
		Transform attackOrigin = this.GetAttackOrigin();
		this.m_weapon.m_shared.m_triggerEffect.Create(attackOrigin.position, this.m_character.transform.rotation, attackOrigin, 1f);
		this.m_triggerEffect.Create(attackOrigin.position, this.m_character.transform.rotation, attackOrigin, 1f);
		if (this.m_weapon.m_shared.m_consumeStatusEffect)
		{
			this.m_character.GetSEMan().AddStatusEffect(this.m_weapon.m_shared.m_consumeStatusEffect, true);
		}
		this.m_character.AddNoise(this.m_attackHitNoise);
	}

	private float GetLevelDamageFactor()
	{
		return 1f + (float)Mathf.Max(0, this.m_character.GetLevel() - 1) * 0.5f;
	}

	private void DoAreaAttack()
	{
		Transform transform = this.m_character.transform;
		Transform attackOrigin = this.GetAttackOrigin();
		Vector3 vector = attackOrigin.position + Vector3.up * this.m_attackHeight + transform.forward * this.m_attackRange + transform.right * this.m_attackOffset;
		this.m_weapon.m_shared.m_triggerEffect.Create(vector, transform.rotation, attackOrigin, 1f);
		this.m_triggerEffect.Create(vector, transform.rotation, attackOrigin, 1f);
		Vector3 vector2 = vector - transform.position;
		vector2.y = 0f;
		vector2.Normalize();
		int num = 0;
		Vector3 vector3 = Vector3.zero;
		bool flag = false;
		bool flag2 = false;
		float randomSkillFactor = this.m_character.GetRandomSkillFactor(this.m_weapon.m_shared.m_skillType);
		int layerMask = this.m_hitTerrain ? Attack.m_attackMaskTerrain : Attack.m_attackMask;
		Collider[] array = Physics.OverlapSphere(vector, this.m_attackRayWidth, layerMask, QueryTriggerInteraction.UseGlobal);
		HashSet<GameObject> hashSet = new HashSet<GameObject>();
		foreach (Collider collider in array)
		{
			if (!(collider.gameObject == this.m_character.gameObject))
			{
				GameObject gameObject = Projectile.FindHitObject(collider);
				if (!(gameObject == this.m_character.gameObject) && !hashSet.Contains(gameObject))
				{
					hashSet.Add(gameObject);
					Vector3 vector4;
					if (collider is MeshCollider)
					{
						vector4 = collider.ClosestPointOnBounds(vector);
					}
					else
					{
						vector4 = collider.ClosestPoint(vector);
					}
					IDestructible component = gameObject.GetComponent<IDestructible>();
					if (component != null)
					{
						Vector3 vector5 = vector4 - vector;
						vector5.y = 0f;
						float num2 = Vector3.Dot(vector2, vector5);
						if (num2 < 0f)
						{
							vector5 += vector2 * -num2;
						}
						vector5.Normalize();
						HitData hitData = new HitData();
						hitData.m_toolTier = this.m_weapon.m_shared.m_toolTier;
						hitData.m_statusEffect = (this.m_weapon.m_shared.m_attackStatusEffect ? this.m_weapon.m_shared.m_attackStatusEffect.name : "");
						hitData.m_pushForce = this.m_weapon.m_shared.m_attackForce * randomSkillFactor * this.m_forceMultiplier;
						hitData.m_backstabBonus = this.m_weapon.m_shared.m_backstabBonus;
						hitData.m_staggerMultiplier = this.m_staggerMultiplier;
						hitData.m_dodgeable = this.m_weapon.m_shared.m_dodgeable;
						hitData.m_blockable = this.m_weapon.m_shared.m_blockable;
						hitData.m_skill = this.m_weapon.m_shared.m_skillType;
						hitData.m_damage.Add(this.m_weapon.GetDamage(), 1);
						hitData.m_point = vector4;
						hitData.m_dir = vector5;
						hitData.m_hitCollider = collider;
						hitData.SetAttacker(this.m_character);
						hitData.m_damage.Modify(this.m_damageMultiplier);
						hitData.m_damage.Modify(randomSkillFactor);
						hitData.m_damage.Modify(this.GetLevelDamageFactor());
						if (this.m_attackChainLevels > 1 && this.m_currentAttackCainLevel == this.m_attackChainLevels - 1 && this.m_lastChainDamageMultiplier > 1f)
						{
							hitData.m_damage.Modify(this.m_lastChainDamageMultiplier);
							hitData.m_pushForce *= 1.2f;
						}
						this.m_character.GetSEMan().ModifyAttack(this.m_weapon.m_shared.m_skillType, ref hitData);
						Character character = component as Character;
						if (character)
						{
							if ((!this.m_character.IsPlayer() && !BaseAI.IsEnemy(this.m_character, character)) || (hitData.m_dodgeable && character.IsDodgeInvincible()))
							{
								goto IL_407;
							}
							flag2 = true;
						}
						component.Damage(hitData);
						flag = true;
					}
					num++;
					vector3 += vector4;
				}
			}
			IL_407:;
		}
		if (num > 0)
		{
			vector3 /= (float)num;
			this.m_weapon.m_shared.m_hitEffect.Create(vector3, Quaternion.identity, null, 1f);
			this.m_hitEffect.Create(vector3, Quaternion.identity, null, 1f);
			if (this.m_weapon.m_shared.m_useDurability && this.m_character.IsPlayer())
			{
				this.m_weapon.m_durability -= 1f;
			}
			this.m_character.AddNoise(this.m_attackHitNoise);
			if (flag)
			{
				this.m_character.RaiseSkill(this.m_weapon.m_shared.m_skillType, flag2 ? 1.5f : 1f);
			}
		}
		if (this.m_spawnOnTrigger)
		{
			IProjectile component2 = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnOnTrigger, vector, Quaternion.identity).GetComponent<IProjectile>();
			if (component2 != null)
			{
				component2.Setup(this.m_character, this.m_character.transform.forward, -1f, null, null);
			}
		}
	}

	private void GetMeleeAttackDir(out Transform originJoint, out Vector3 attackDir)
	{
		originJoint = this.GetAttackOrigin();
		Vector3 forward = this.m_character.transform.forward;
		Vector3 aimDir = this.m_character.GetAimDir(originJoint.position);
		aimDir.x = forward.x;
		aimDir.z = forward.z;
		aimDir.Normalize();
		attackDir = Vector3.RotateTowards(this.m_character.transform.forward, aimDir, 0.017453292f * this.m_maxYAngle, 10f);
	}

	private void AddHitPoint(List<Attack.HitPoint> list, GameObject go, Collider collider, Vector3 point, float distance)
	{
		Attack.HitPoint hitPoint = null;
		for (int i = list.Count - 1; i >= 0; i--)
		{
			if (list[i].go == go)
			{
				hitPoint = list[i];
				break;
			}
		}
		if (hitPoint == null)
		{
			hitPoint = new Attack.HitPoint();
			hitPoint.go = go;
			hitPoint.collider = collider;
			hitPoint.firstPoint = point;
			list.Add(hitPoint);
		}
		hitPoint.avgPoint += point;
		hitPoint.count++;
		if (distance < hitPoint.closestDistance)
		{
			hitPoint.closestPoint = point;
			hitPoint.closestDistance = distance;
		}
	}

	private void DoMeleeAttack()
	{
		Transform transform;
		Vector3 vector;
		this.GetMeleeAttackDir(out transform, out vector);
		Vector3 point = this.m_character.transform.InverseTransformDirection(vector);
		Quaternion quaternion = Quaternion.LookRotation(vector, Vector3.up);
		this.m_weapon.m_shared.m_triggerEffect.Create(transform.position, quaternion, transform, 1f);
		this.m_triggerEffect.Create(transform.position, quaternion, transform, 1f);
		Vector3 vector2 = transform.position + Vector3.up * this.m_attackHeight + this.m_character.transform.right * this.m_attackOffset;
		float num = this.m_attackAngle / 2f;
		float num2 = 4f;
		float attackRange = this.m_attackRange;
		List<Attack.HitPoint> list = new List<Attack.HitPoint>();
		HashSet<Skills.SkillType> hashSet = new HashSet<Skills.SkillType>();
		int layerMask = this.m_hitTerrain ? Attack.m_attackMaskTerrain : Attack.m_attackMask;
		for (float num3 = -num; num3 <= num; num3 += num2)
		{
			Quaternion rotation = Quaternion.identity;
			if (this.m_attackType == Attack.AttackType.Horizontal)
			{
				rotation = Quaternion.Euler(0f, -num3, 0f);
			}
			else if (this.m_attackType == Attack.AttackType.Vertical)
			{
				rotation = Quaternion.Euler(num3, 0f, 0f);
			}
			Vector3 vector3 = this.m_character.transform.TransformDirection(rotation * point);
			Debug.DrawLine(vector2, vector2 + vector3 * attackRange);
			RaycastHit[] array;
			if (this.m_attackRayWidth > 0f)
			{
				array = Physics.SphereCastAll(vector2, this.m_attackRayWidth, vector3, Mathf.Max(0f, attackRange - this.m_attackRayWidth), layerMask, QueryTriggerInteraction.Ignore);
			}
			else
			{
				array = Physics.RaycastAll(vector2, vector3, attackRange, layerMask, QueryTriggerInteraction.Ignore);
			}
			Array.Sort<RaycastHit>(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
			foreach (RaycastHit raycastHit in array)
			{
				if (!(raycastHit.collider.gameObject == this.m_character.gameObject))
				{
					Vector3 vector4 = raycastHit.point;
					if (raycastHit.distance < 1E-45f)
					{
						if (raycastHit.collider is MeshCollider)
						{
							vector4 = vector2 + vector3 * attackRange;
						}
						else
						{
							vector4 = raycastHit.collider.ClosestPoint(vector2);
						}
					}
					if (this.m_attackAngle >= 180f || Vector3.Dot(vector4 - vector2, vector) > 0f)
					{
						GameObject gameObject = Projectile.FindHitObject(raycastHit.collider);
						if (!(gameObject == this.m_character.gameObject))
						{
							Vagon component = gameObject.GetComponent<Vagon>();
							if (!component || !component.IsAttached(this.m_character))
							{
								Character component2 = gameObject.GetComponent<Character>();
								if (!(component2 != null) || ((this.m_character.IsPlayer() || BaseAI.IsEnemy(this.m_character, component2)) && (!this.m_weapon.m_shared.m_dodgeable || !component2.IsDodgeInvincible())))
								{
									this.AddHitPoint(list, gameObject, raycastHit.collider, vector4, raycastHit.distance);
									if (!this.m_hitThroughWalls)
									{
										break;
									}
								}
							}
						}
					}
				}
			}
		}
		int num4 = 0;
		Vector3 vector5 = Vector3.zero;
		bool flag = false;
		bool flag2 = false;
		foreach (Attack.HitPoint hitPoint in list)
		{
			GameObject go = hitPoint.go;
			Vector3 vector6 = hitPoint.avgPoint / (float)hitPoint.count;
			Vector3 vector7 = vector6;
			switch (this.m_hitPointtype)
			{
			case Attack.HitPointType.Closest:
				vector7 = hitPoint.closestPoint;
				break;
			case Attack.HitPointType.Average:
				vector7 = vector6;
				break;
			case Attack.HitPointType.First:
				vector7 = hitPoint.firstPoint;
				break;
			}
			num4++;
			vector5 += vector6;
			this.m_weapon.m_shared.m_hitEffect.Create(vector7, Quaternion.identity, null, 1f);
			this.m_hitEffect.Create(vector7, Quaternion.identity, null, 1f);
			IDestructible component3 = go.GetComponent<IDestructible>();
			if (component3 != null)
			{
				DestructibleType destructibleType = component3.GetDestructibleType();
				Skills.SkillType skillType = this.m_weapon.m_shared.m_skillType;
				if (this.m_specialHitSkill != Skills.SkillType.None && (destructibleType & this.m_specialHitType) != DestructibleType.None)
				{
					skillType = this.m_specialHitSkill;
				}
				float num5 = this.m_character.GetRandomSkillFactor(skillType);
				if (this.m_lowerDamagePerHit && list.Count > 1)
				{
					num5 /= (float)list.Count * 0.75f;
				}
				HitData hitData = new HitData();
				hitData.m_toolTier = this.m_weapon.m_shared.m_toolTier;
				hitData.m_statusEffect = (this.m_weapon.m_shared.m_attackStatusEffect ? this.m_weapon.m_shared.m_attackStatusEffect.name : "");
				hitData.m_pushForce = this.m_weapon.m_shared.m_attackForce * num5 * this.m_forceMultiplier;
				hitData.m_backstabBonus = this.m_weapon.m_shared.m_backstabBonus;
				hitData.m_staggerMultiplier = this.m_staggerMultiplier;
				hitData.m_dodgeable = this.m_weapon.m_shared.m_dodgeable;
				hitData.m_blockable = this.m_weapon.m_shared.m_blockable;
				hitData.m_skill = skillType;
				hitData.m_damage = this.m_weapon.GetDamage();
				hitData.m_point = vector7;
				hitData.m_dir = (vector7 - vector2).normalized;
				hitData.m_hitCollider = hitPoint.collider;
				hitData.SetAttacker(this.m_character);
				hitData.m_damage.Modify(this.m_damageMultiplier);
				hitData.m_damage.Modify(num5);
				hitData.m_damage.Modify(this.GetLevelDamageFactor());
				if (this.m_attackChainLevels > 1 && this.m_currentAttackCainLevel == this.m_attackChainLevels - 1)
				{
					hitData.m_damage.Modify(2f);
					hitData.m_pushForce *= 1.2f;
				}
				this.m_character.GetSEMan().ModifyAttack(skillType, ref hitData);
				if (component3 is Character)
				{
					flag2 = true;
				}
				component3.Damage(hitData);
				if ((destructibleType & this.m_resetChainIfHit) != DestructibleType.None)
				{
					this.m_nextAttackChainLevel = 0;
				}
				hashSet.Add(skillType);
				if (!this.m_multiHit)
				{
					break;
				}
			}
			if (go.GetComponent<Heightmap>() != null && !flag)
			{
				flag = true;
				this.m_weapon.m_shared.m_hitTerrainEffect.Create(vector6, quaternion, null, 1f);
				this.m_hitTerrainEffect.Create(vector6, quaternion, null, 1f);
				if (this.m_weapon.m_shared.m_spawnOnHitTerrain)
				{
					this.SpawnOnHitTerrain(vector6, this.m_weapon.m_shared.m_spawnOnHitTerrain);
				}
				if (!this.m_multiHit)
				{
					break;
				}
			}
		}
		if (num4 > 0)
		{
			vector5 /= (float)num4;
			if (this.m_weapon.m_shared.m_useDurability && this.m_character.IsPlayer())
			{
				this.m_weapon.m_durability -= this.m_weapon.m_shared.m_useDurabilityDrain;
			}
			this.m_character.AddNoise(this.m_attackHitNoise);
			this.m_animEvent.FreezeFrame(0.15f);
			if (this.m_weapon.m_shared.m_spawnOnHit)
			{
				IProjectile component4 = UnityEngine.Object.Instantiate<GameObject>(this.m_weapon.m_shared.m_spawnOnHit, vector5, quaternion).GetComponent<IProjectile>();
				if (component4 != null)
				{
					component4.Setup(this.m_character, Vector3.zero, this.m_attackHitNoise, null, this.m_weapon);
				}
			}
			foreach (Skills.SkillType skill in hashSet)
			{
				this.m_character.RaiseSkill(skill, flag2 ? 1.5f : 1f);
			}
		}
		if (this.m_spawnOnTrigger)
		{
			IProjectile component5 = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnOnTrigger, vector2, Quaternion.identity).GetComponent<IProjectile>();
			if (component5 != null)
			{
				component5.Setup(this.m_character, this.m_character.transform.forward, -1f, null, this.m_weapon);
			}
		}
	}

	private void SpawnOnHitTerrain(Vector3 hitPoint, GameObject prefab)
	{
		TerrainModifier componentInChildren = prefab.GetComponentInChildren<TerrainModifier>();
		if (componentInChildren)
		{
			if (!PrivateArea.CheckAccess(hitPoint, componentInChildren.GetRadius(), true))
			{
				return;
			}
			if (Location.IsInsideNoBuildLocation(hitPoint))
			{
				return;
			}
		}
		TerrainModifier.SetTriggerOnPlaced(true);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, hitPoint, Quaternion.LookRotation(this.m_character.transform.forward));
		TerrainModifier.SetTriggerOnPlaced(false);
		IProjectile component = gameObject.GetComponent<IProjectile>();
		if (component != null)
		{
			component.Setup(this.m_character, Vector3.zero, this.m_attackHitNoise, null, this.m_weapon);
		}
	}

	public Attack Clone()
	{
		return base.MemberwiseClone() as Attack;
	}

	public ItemDrop.ItemData GetWeapon()
	{
		return this.m_weapon;
	}

	public bool CanStartChainAttack()
	{
		return this.m_nextAttackChainLevel > 0 && this.m_animEvent.CanChain();
	}

	public void OnTrailStart()
	{
		if (this.m_attackType == Attack.AttackType.Projectile)
		{
			Transform attackOrigin = this.GetAttackOrigin();
			this.m_weapon.m_shared.m_trailStartEffect.Create(attackOrigin.position, this.m_character.transform.rotation, null, 1f);
			this.m_trailStartEffect.Create(attackOrigin.position, this.m_character.transform.rotation, null, 1f);
			return;
		}
		Transform transform;
		Vector3 forward;
		this.GetMeleeAttackDir(out transform, out forward);
		Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
		this.m_weapon.m_shared.m_trailStartEffect.Create(transform.position, rot, null, 1f);
		this.m_trailStartEffect.Create(transform.position, rot, null, 1f);
	}

	[Header("Common")]
	public Attack.AttackType m_attackType;

	public string m_attackAnimation = "";

	public int m_attackRandomAnimations;

	public int m_attackChainLevels;

	public bool m_consumeItem;

	public bool m_hitTerrain = true;

	public float m_attackStamina = 20f;

	public float m_speedFactor = 0.2f;

	public float m_speedFactorRotation = 0.2f;

	public float m_attackStartNoise = 10f;

	public float m_attackHitNoise = 30f;

	public float m_damageMultiplier = 1f;

	public float m_forceMultiplier = 1f;

	public float m_staggerMultiplier = 1f;

	[Header("Misc")]
	public string m_attackOriginJoint = "";

	public float m_attackRange = 1.5f;

	public float m_attackHeight = 0.6f;

	public float m_attackOffset;

	public GameObject m_spawnOnTrigger;

	[Header("Melee/AOE")]
	public float m_attackAngle = 90f;

	public float m_attackRayWidth;

	public float m_maxYAngle;

	public bool m_lowerDamagePerHit = true;

	public Attack.HitPointType m_hitPointtype;

	public bool m_hitThroughWalls;

	public bool m_multiHit = true;

	public float m_lastChainDamageMultiplier = 2f;

	[BitMask(typeof(DestructibleType))]
	public DestructibleType m_resetChainIfHit;

	[Header("Melee special-skill")]
	public Skills.SkillType m_specialHitSkill;

	[BitMask(typeof(DestructibleType))]
	public DestructibleType m_specialHitType;

	[Header("Projectile")]
	public GameObject m_attackProjectile;

	public float m_projectileVel = 10f;

	public float m_projectileVelMin = 2f;

	public float m_projectileAccuracy = 10f;

	public float m_projectileAccuracyMin = 20f;

	public bool m_useCharacterFacing;

	public bool m_useCharacterFacingYAim;

	[FormerlySerializedAs("m_useCharacterFacingAngle")]
	public float m_launchAngle;

	public int m_projectiles = 1;

	public int m_projectileBursts = 1;

	public float m_burstInterval;

	public bool m_destroyPreviousProjectile;

	[Header("Attack-Effects")]
	public EffectList m_hitEffect = new EffectList();

	public EffectList m_hitTerrainEffect = new EffectList();

	public EffectList m_startEffect = new EffectList();

	public EffectList m_triggerEffect = new EffectList();

	public EffectList m_trailStartEffect = new EffectList();

	protected static int m_attackMask;

	protected static int m_attackMaskTerrain;

	private Humanoid m_character;

	private BaseAI m_baseAI;

	private Rigidbody m_body;

	private ZSyncAnimation m_zanim;

	private CharacterAnimEvent m_animEvent;

	[NonSerialized]
	private ItemDrop.ItemData m_weapon;

	private VisEquipment m_visEquipment;

	private float m_attackDrawPercentage;

	private const float m_freezeFrameDuration = 0.15f;

	private const float m_chainAttackMaxTime = 0.2f;

	private int m_nextAttackChainLevel;

	private int m_currentAttackCainLevel;

	private bool m_wasInAttack;

	private float m_time;

	private bool m_projectileAttackStarted;

	private float m_projectileFireTimer = -1f;

	private int m_projectileBurstsFired;

	[NonSerialized]
	private ItemDrop.ItemData m_ammoItem;

	private class HitPoint
	{
		public GameObject go;

		public Vector3 avgPoint = Vector3.zero;

		public int count;

		public Vector3 firstPoint;

		public Collider collider;

		public Vector3 closestPoint;

		public float closestDistance = 999999f;
	}

	public enum AttackType
	{
		Horizontal,
		Vertical,
		Projectile,
		None,
		Area,
		TriggerProjectile
	}

	public enum HitPointType
	{
		Closest,
		Average,
		First
	}
}
