using System;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour, IProjectile
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (Projectile.m_rayMaskSolids == 0)
		{
			Projectile.m_rayMaskSolids = LayerMask.GetMask(new string[]
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
		this.m_nview.Register("OnHit", new Action<long>(this.RPC_OnHit));
	}

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateRotation(Time.fixedDeltaTime);
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_didHit)
		{
			Vector3 position = base.transform.position;
			this.m_vel += Vector3.down * this.m_gravity * Time.fixedDeltaTime;
			base.transform.position += this.m_vel * Time.fixedDeltaTime;
			if (this.m_rotateVisual == 0f)
			{
				base.transform.rotation = Quaternion.LookRotation(this.m_vel);
			}
			if (this.m_canHitWater)
			{
				float waterLevel = WaterVolume.GetWaterLevel(base.transform.position, 1f);
				if (base.transform.position.y < waterLevel)
				{
					this.OnHit(null, base.transform.position, true);
				}
			}
			if (!this.m_didHit)
			{
				Vector3 b = base.transform.position - position;
				foreach (RaycastHit raycastHit in Physics.SphereCastAll(position - b, this.m_rayRadius, b.normalized, b.magnitude * 2f, Projectile.m_rayMaskSolids))
				{
					this.OnHit(raycastHit.collider, raycastHit.point, false);
					if (this.m_didHit)
					{
						break;
					}
				}
			}
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

	public Vector3 GetVelocity()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return Vector3.zero;
		}
		if (this.m_didHit)
		{
			return Vector3.zero;
		}
		return this.m_vel;
	}

	private void UpdateRotation(float dt)
	{
		if ((double)this.m_rotateVisual == 0.0 || this.m_visual == null)
		{
			return;
		}
		this.m_visual.transform.Rotate(new Vector3(this.m_rotateVisual * dt, 0f, 0f));
	}

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item)
	{
		this.m_owner = owner;
		this.m_vel = velocity;
		if (hitNoise >= 0f)
		{
			this.m_hitNoise = hitNoise;
		}
		if (hitData != null)
		{
			this.m_damage = hitData.m_damage;
			this.m_blockable = hitData.m_blockable;
			this.m_dodgeable = hitData.m_dodgeable;
			this.m_attackForce = hitData.m_pushForce;
			this.m_backstabBonus = hitData.m_backstabBonus;
			this.m_statusEffect = hitData.m_statusEffect;
			this.m_skill = hitData.m_skill;
		}
		if (this.m_respawnItemOnHit)
		{
			this.m_spawnItem = item;
		}
		LineConnect component = base.GetComponent<LineConnect>();
		if (component)
		{
			component.SetPeer(owner.GetZDOID());
		}
	}

	private void DoAOE(Vector3 hitPoint, ref bool hitCharacter, ref bool didDamage)
	{
		Collider[] array = Physics.OverlapSphere(hitPoint, this.m_aoe, Projectile.m_rayMaskSolids, QueryTriggerInteraction.UseGlobal);
		HashSet<GameObject> hashSet = new HashSet<GameObject>();
		foreach (Collider collider in array)
		{
			GameObject gameObject = Projectile.FindHitObject(collider);
			IDestructible component = gameObject.GetComponent<IDestructible>();
			if (component != null && !hashSet.Contains(gameObject))
			{
				hashSet.Add(gameObject);
				if (this.IsValidTarget(component, ref hitCharacter))
				{
					Vector3 vector = collider.ClosestPointOnBounds(hitPoint);
					Vector3 vector2 = (Vector3.Distance(vector, hitPoint) > 0.1f) ? (vector - hitPoint) : this.m_vel;
					vector2.y = 0f;
					vector2.Normalize();
					HitData hitData = new HitData();
					hitData.m_hitCollider = collider;
					hitData.m_damage = this.m_damage;
					hitData.m_pushForce = this.m_attackForce;
					hitData.m_backstabBonus = this.m_backstabBonus;
					hitData.m_point = vector;
					hitData.m_dir = vector2.normalized;
					hitData.m_statusEffect = this.m_statusEffect;
					hitData.m_dodgeable = this.m_dodgeable;
					hitData.m_blockable = this.m_blockable;
					hitData.m_skill = this.m_skill;
					hitData.SetAttacker(this.m_owner);
					component.Damage(hitData);
					didDamage = true;
				}
			}
		}
	}

	private bool IsValidTarget(IDestructible destr, ref bool hitCharacter)
	{
		Character character = destr as Character;
		if (character)
		{
			if (character == this.m_owner)
			{
				return false;
			}
			if (this.m_owner != null && !this.m_owner.IsPlayer() && !BaseAI.IsEnemy(this.m_owner, character))
			{
				return false;
			}
			if (this.m_dodgeable && character.IsDodgeInvincible())
			{
				return false;
			}
			hitCharacter = true;
		}
		return true;
	}

	private void OnHit(Collider collider, Vector3 hitPoint, bool water)
	{
		GameObject gameObject = collider ? Projectile.FindHitObject(collider) : null;
		bool flag = false;
		bool flag2 = false;
		if (this.m_aoe > 0f)
		{
			this.DoAOE(hitPoint, ref flag2, ref flag);
		}
		else
		{
			IDestructible destructible = gameObject ? gameObject.GetComponent<IDestructible>() : null;
			if (destructible != null)
			{
				if (!this.IsValidTarget(destructible, ref flag2))
				{
					return;
				}
				HitData hitData = new HitData();
				hitData.m_hitCollider = collider;
				hitData.m_damage = this.m_damage;
				hitData.m_pushForce = this.m_attackForce;
				hitData.m_backstabBonus = this.m_backstabBonus;
				hitData.m_point = hitPoint;
				hitData.m_dir = base.transform.forward;
				hitData.m_statusEffect = this.m_statusEffect;
				hitData.m_dodgeable = this.m_dodgeable;
				hitData.m_blockable = this.m_blockable;
				hitData.m_skill = this.m_skill;
				hitData.SetAttacker(this.m_owner);
				destructible.Damage(hitData);
				flag = true;
			}
		}
		if (water)
		{
			this.m_hitWaterEffects.Create(hitPoint, Quaternion.identity, null, 1f);
		}
		else
		{
			this.m_hitEffects.Create(hitPoint, Quaternion.identity, null, 1f);
		}
		if (this.m_spawnOnHit != null || this.m_spawnItem != null)
		{
			this.SpawnOnHit(gameObject, collider);
		}
		if (this.m_hitNoise > 0f && this.m_owner != null)
		{
			BaseAI.AlertAllInRange(base.transform.position, this.m_hitNoise, this.m_owner);
		}
		if (this.m_owner != null && flag && this.m_owner.IsPlayer())
		{
			(this.m_owner as Player).RaiseSkill(this.m_skill, flag2 ? 1f : 0.5f);
		}
		this.m_didHit = true;
		base.transform.position = hitPoint;
		this.m_nview.InvokeRPC("OnHit", Array.Empty<object>());
		if (!this.m_stayAfterHitStatic)
		{
			ZNetScene.instance.Destroy(base.gameObject);
			return;
		}
		if (collider && collider.attachedRigidbody != null)
		{
			this.m_ttl = Mathf.Min(1f, this.m_ttl);
		}
	}

	private void RPC_OnHit(long sender)
	{
		if (this.m_hideOnHit)
		{
			this.m_hideOnHit.SetActive(false);
		}
		if (this.m_stopEmittersOnHit)
		{
			ParticleSystem[] componentsInChildren = base.GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].emission.enabled = false;
			}
		}
	}

	private void SpawnOnHit(GameObject go, Collider collider)
	{
		if (this.m_groundHitOnly && go.GetComponent<Heightmap>() == null)
		{
			return;
		}
		if (this.m_staticHitOnly)
		{
			if (collider && collider.attachedRigidbody != null)
			{
				return;
			}
			if (go && go.GetComponent<IDestructible>() != null)
			{
				return;
			}
		}
		if (this.m_spawnOnHitChance < 1f && UnityEngine.Random.value > this.m_spawnOnHitChance)
		{
			return;
		}
		Vector3 vector = base.transform.position + base.transform.TransformDirection(this.m_spawnOffset);
		Quaternion rotation = base.transform.rotation;
		if (this.m_spawnRandomRotation)
		{
			rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
		}
		if (this.m_spawnOnHit != null)
		{
			IProjectile component = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnOnHit, vector, rotation).GetComponent<IProjectile>();
			if (component != null)
			{
				component.Setup(this.m_owner, this.m_vel, this.m_hitNoise, null, null);
			}
		}
		if (this.m_spawnItem != null)
		{
			ItemDrop.DropItem(this.m_spawnItem, 0, vector, base.transform.rotation);
		}
		this.m_spawnOnHitEffects.Create(vector, Quaternion.identity, null, 1f);
	}

	public static GameObject FindHitObject(Collider collider)
	{
		IDestructible componentInParent = collider.gameObject.GetComponentInParent<IDestructible>();
		if (componentInParent != null)
		{
			return (componentInParent as MonoBehaviour).gameObject;
		}
		if (collider.attachedRigidbody)
		{
			return collider.attachedRigidbody.gameObject;
		}
		return collider.gameObject;
	}

	public HitData.DamageTypes m_damage;

	public float m_aoe;

	public bool m_dodgeable;

	public bool m_blockable;

	public float m_attackForce;

	public float m_backstabBonus = 4f;

	public string m_statusEffect = "";

	public bool m_canHitWater;

	public float m_ttl = 4f;

	public float m_gravity;

	public float m_rayRadius;

	public float m_hitNoise = 50f;

	public bool m_stayAfterHitStatic;

	public GameObject m_hideOnHit;

	public bool m_stopEmittersOnHit = true;

	public EffectList m_hitEffects = new EffectList();

	public EffectList m_hitWaterEffects = new EffectList();

	[Header("Spawn on hit")]
	public bool m_respawnItemOnHit;

	public GameObject m_spawnOnHit;

	[Range(0f, 1f)]
	public float m_spawnOnHitChance = 1f;

	public bool m_showBreakMessage;

	public bool m_staticHitOnly;

	public bool m_groundHitOnly;

	public Vector3 m_spawnOffset = Vector3.zero;

	public bool m_spawnRandomRotation;

	public EffectList m_spawnOnHitEffects = new EffectList();

	[Header("Rotate projectile")]
	public float m_rotateVisual;

	public GameObject m_visual;

	private ZNetView m_nview;

	private Vector3 m_vel = Vector3.zero;

	private Character m_owner;

	private Skills.SkillType m_skill;

	private ItemDrop.ItemData m_spawnItem;

	private bool m_didHit;

	private static int m_rayMaskSolids;
}
