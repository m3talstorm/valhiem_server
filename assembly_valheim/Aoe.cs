using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Aoe : MonoBehaviour, IProjectile
{
	private void Awake()
	{
		this.m_nview = base.GetComponentInParent<ZNetView>();
		this.m_rayMask = 0;
		if (this.m_hitCharacters)
		{
			this.m_rayMask |= LayerMask.GetMask(new string[]
			{
				"character",
				"character_net",
				"character_ghost"
			});
		}
		if (this.m_hitProps)
		{
			this.m_rayMask |= LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"hitbox",
				"character_noenv",
				"vehicle"
			});
		}
	}

	public HitData.DamageTypes GetDamage()
	{
		return this.GetDamage(this.m_level);
	}

	public HitData.DamageTypes GetDamage(int itemQuality)
	{
		if (itemQuality <= 1)
		{
			return this.m_damage;
		}
		HitData.DamageTypes damage = this.m_damage;
		damage.Add(this.m_damagePerLevel, itemQuality - 1);
		return damage;
	}

	public string GetTooltipString(int itemQuality)
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		stringBuilder.Append("AOE");
		stringBuilder.Append(this.GetDamage(itemQuality).GetTooltipString());
		stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", this.m_attackForce);
		stringBuilder.AppendFormat("\n$item_backstab: <color=orange>{0}x</color>", this.m_backstabBonus);
		return stringBuilder.ToString();
	}

	private void Start()
	{
		if (this.m_nview != null && (!this.m_nview.IsValid() || !this.m_nview.IsOwner()))
		{
			return;
		}
		if (!this.m_useTriggers && this.m_hitInterval <= 0f)
		{
			this.CheckHits();
		}
	}

	private void FixedUpdate()
	{
		if (this.m_nview != null && (!this.m_nview.IsValid() || !this.m_nview.IsOwner()))
		{
			return;
		}
		if (this.m_hitInterval > 0f)
		{
			this.m_hitTimer -= Time.fixedDeltaTime;
			if (this.m_hitTimer <= 0f)
			{
				this.m_hitTimer = this.m_hitInterval;
				if (this.m_useTriggers)
				{
					this.m_hitList.Clear();
				}
				else
				{
					this.CheckHits();
				}
			}
		}
		if (this.m_owner != null && this.m_attachToCaster)
		{
			base.transform.position = this.m_owner.transform.TransformPoint(this.m_offset);
			base.transform.rotation = this.m_owner.transform.rotation * this.m_localRot;
		}
		if (this.m_ttl > 0f)
		{
			this.m_ttl -= Time.fixedDeltaTime;
			if (this.m_ttl <= 0f)
			{
				ZNetScene.instance.Destroy(base.gameObject);
			}
		}
	}

	private void CheckHits()
	{
		this.m_hitList.Clear();
		Collider[] array = Physics.OverlapSphere(base.transform.position, this.m_radius, this.m_rayMask);
		bool flag = false;
		foreach (Collider collider in array)
		{
			if (this.OnHit(collider, collider.transform.position))
			{
				flag = true;
			}
		}
		if (flag && this.m_owner && this.m_owner.IsPlayer() && this.m_skill != Skills.SkillType.None)
		{
			this.m_owner.RaiseSkill(this.m_skill, 1f);
		}
	}

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item)
	{
		this.m_owner = owner;
		if (item != null)
		{
			this.m_level = item.m_quality;
		}
		if (this.m_attachToCaster && owner != null)
		{
			this.m_offset = owner.transform.InverseTransformPoint(base.transform.position);
			this.m_localRot = Quaternion.Inverse(owner.transform.rotation) * base.transform.rotation;
		}
		if (hitData != null && this.m_useAttackSettings)
		{
			this.m_damage = hitData.m_damage;
			this.m_blockable = hitData.m_blockable;
			this.m_dodgeable = hitData.m_dodgeable;
			this.m_attackForce = hitData.m_pushForce;
			this.m_backstabBonus = hitData.m_backstabBonus;
			this.m_statusEffect = hitData.m_statusEffect;
			this.m_toolTier = hitData.m_toolTier;
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (!this.m_triggerEnterOnly)
		{
			return;
		}
		if (!this.m_useTriggers)
		{
			ZLog.LogWarning("AOE got OnTriggerStay but trigger damage is disabled in " + base.gameObject.name);
			return;
		}
		if (this.m_nview != null && (!this.m_nview.IsValid() || !this.m_nview.IsOwner()))
		{
			return;
		}
		this.OnHit(collider, collider.transform.position);
	}

	private void OnTriggerStay(Collider collider)
	{
		if (this.m_triggerEnterOnly)
		{
			return;
		}
		if (!this.m_useTriggers)
		{
			ZLog.LogWarning("AOE got OnTriggerStay but trigger damage is disabled in " + base.gameObject.name);
			return;
		}
		if (this.m_nview != null && (!this.m_nview.IsValid() || !this.m_nview.IsOwner()))
		{
			return;
		}
		this.OnHit(collider, collider.transform.position);
	}

	private bool OnHit(Collider collider, Vector3 hitPoint)
	{
		GameObject gameObject = Projectile.FindHitObject(collider);
		if (this.m_hitList.Contains(gameObject))
		{
			return false;
		}
		this.m_hitList.Add(gameObject);
		float num = 1f;
		if (this.m_owner && this.m_owner.IsPlayer() && this.m_skill != Skills.SkillType.None)
		{
			num = this.m_owner.GetRandomSkillFactor(this.m_skill);
		}
		bool result = false;
		IDestructible component = gameObject.GetComponent<IDestructible>();
		if (component != null)
		{
			Character character = component as Character;
			if (character)
			{
				if (this.m_nview == null && !character.IsOwner())
				{
					return false;
				}
				if (this.m_owner != null)
				{
					if (!this.m_hitOwner && character == this.m_owner)
					{
						return false;
					}
					if (!this.m_hitSame && character.m_name == this.m_owner.m_name)
					{
						return false;
					}
					bool flag = BaseAI.IsEnemy(this.m_owner, character);
					if (!this.m_hitFriendly && !flag)
					{
						return false;
					}
					if (!this.m_hitEnemy && flag)
					{
						return false;
					}
				}
				if (!this.m_hitCharacters)
				{
					return false;
				}
				if (this.m_dodgeable && character.IsDodgeInvincible())
				{
					return false;
				}
			}
			else if (!this.m_hitProps)
			{
				return false;
			}
			Vector3 dir = this.m_attackForceForward ? base.transform.forward : (hitPoint - base.transform.position).normalized;
			HitData hitData = new HitData();
			hitData.m_hitCollider = collider;
			hitData.m_damage = this.GetDamage();
			hitData.m_pushForce = this.m_attackForce * num;
			hitData.m_backstabBonus = this.m_backstabBonus;
			hitData.m_point = hitPoint;
			hitData.m_dir = dir;
			hitData.m_statusEffect = this.m_statusEffect;
			hitData.m_dodgeable = this.m_dodgeable;
			hitData.m_blockable = this.m_blockable;
			hitData.m_toolTier = this.m_toolTier;
			hitData.SetAttacker(this.m_owner);
			hitData.m_damage.Modify(num);
			component.Damage(hitData);
			if (this.m_damageSelf > 0f)
			{
				IDestructible componentInParent = base.GetComponentInParent<IDestructible>();
				if (componentInParent != null)
				{
					HitData hitData2 = new HitData();
					hitData2.m_damage.m_damage = this.m_damageSelf;
					hitData2.m_point = base.transform.position;
					hitData2.m_blockable = false;
					hitData2.m_dodgeable = false;
					componentInParent.Damage(hitData2);
				}
			}
			result = true;
		}
		this.m_hitEffects.Create(hitPoint, Quaternion.identity, null, 1f);
		return result;
	}

	private void OnDrawGizmos()
	{
		bool useTriggers = this.m_useTriggers;
	}

	[Header("Attack (overridden by item )")]
	public bool m_useAttackSettings = true;

	public HitData.DamageTypes m_damage;

	public bool m_dodgeable;

	public bool m_blockable;

	public int m_toolTier;

	public float m_attackForce;

	public float m_backstabBonus = 4f;

	public string m_statusEffect = "";

	[Header("Attack (other)")]
	public HitData.DamageTypes m_damagePerLevel;

	public bool m_attackForceForward;

	[Header("Damage self")]
	public float m_damageSelf;

	[Header("Ignore targets")]
	public bool m_hitOwner;

	public bool m_hitSame;

	public bool m_hitFriendly = true;

	public bool m_hitEnemy = true;

	public bool m_hitCharacters = true;

	public bool m_hitProps = true;

	[Header("Other")]
	public Skills.SkillType m_skill;

	public bool m_useTriggers;

	public bool m_triggerEnterOnly;

	public float m_radius = 4f;

	public float m_ttl = 4f;

	public float m_hitInterval = 1f;

	public EffectList m_hitEffects = new EffectList();

	public bool m_attachToCaster;

	private ZNetView m_nview;

	private Character m_owner;

	private List<GameObject> m_hitList = new List<GameObject>();

	private float m_hitTimer;

	private Vector3 m_offset = Vector3.zero;

	private Quaternion m_localRot = Quaternion.identity;

	private int m_level;

	private int m_rayMask;
}
