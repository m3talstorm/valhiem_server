using System;
using UnityEngine;

public class ImpactEffect : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		if (this.m_maxVelocity < this.m_minVelocity)
		{
			this.m_maxVelocity = this.m_minVelocity;
		}
	}

	public void OnCollisionEnter(Collision info)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview && !this.m_nview.IsOwner())
		{
			return;
		}
		if (info.contacts.Length == 0)
		{
			return;
		}
		if (!this.m_hitEffectEnabled)
		{
			return;
		}
		if ((this.m_triggerMask.value & 1 << info.collider.gameObject.layer) == 0)
		{
			return;
		}
		float magnitude = info.relativeVelocity.magnitude;
		if (magnitude < this.m_minVelocity)
		{
			return;
		}
		ContactPoint contactPoint = info.contacts[0];
		Vector3 point = contactPoint.point;
		Vector3 pointVelocity = this.m_body.GetPointVelocity(point);
		this.m_hitEffectEnabled = false;
		base.Invoke("ResetHitTimer", this.m_interval);
		if (this.m_damages.HaveDamage())
		{
			GameObject gameObject = Projectile.FindHitObject(contactPoint.otherCollider);
			float num = Utils.LerpStep(this.m_minVelocity, this.m_maxVelocity, magnitude);
			IDestructible component = gameObject.GetComponent<IDestructible>();
			if (component != null)
			{
				Character character = component as Character;
				if (character)
				{
					if (!this.m_damagePlayers && character.IsPlayer())
					{
						return;
					}
					float num2 = Vector3.Dot(-info.relativeVelocity.normalized, pointVelocity);
					if (num2 < this.m_minVelocity)
					{
						return;
					}
					ZLog.Log("Rel vel " + num2);
					num = Utils.LerpStep(this.m_minVelocity, this.m_maxVelocity, num2);
					if (character.GetSEMan().HaveStatusAttribute(StatusEffect.StatusAttribute.DoubleImpactDamage))
					{
						num *= 2f;
					}
				}
				if (!this.m_damageFish && gameObject.GetComponent<Fish>())
				{
					return;
				}
				HitData hitData = new HitData();
				hitData.m_point = point;
				hitData.m_dir = pointVelocity.normalized;
				hitData.m_hitCollider = info.collider;
				hitData.m_toolTier = this.m_toolTier;
				hitData.m_damage = this.m_damages.Clone();
				hitData.m_damage.Modify(num);
				component.Damage(hitData);
			}
			if (this.m_damageToSelf)
			{
				IDestructible component2 = base.GetComponent<IDestructible>();
				if (component2 != null)
				{
					HitData hitData2 = new HitData();
					hitData2.m_point = point;
					hitData2.m_dir = -pointVelocity.normalized;
					hitData2.m_toolTier = this.m_toolTier;
					hitData2.m_damage = this.m_damages.Clone();
					hitData2.m_damage.Modify(num);
					component2.Damage(hitData2);
				}
			}
		}
		Vector3 rhs = Vector3.Cross(-Vector3.Normalize(info.relativeVelocity), contactPoint.normal);
		Vector3 vector = Vector3.Cross(contactPoint.normal, rhs);
		Quaternion rot = Quaternion.identity;
		if (vector != Vector3.zero && contactPoint.normal != Vector3.zero)
		{
			rot = Quaternion.LookRotation(vector, contactPoint.normal);
		}
		this.m_hitEffect.Create(point, rot, null, 1f);
		if (this.m_firstHit && this.m_hitDestroyChance > 0f && UnityEngine.Random.value <= this.m_hitDestroyChance)
		{
			this.m_destroyEffect.Create(point, rot, null, 1f);
			GameObject gameObject2 = base.gameObject;
			if (base.transform.parent)
			{
				Animator componentInParent = base.transform.GetComponentInParent<Animator>();
				if (componentInParent)
				{
					gameObject2 = componentInParent.gameObject;
				}
			}
			UnityEngine.Object.Destroy(gameObject2);
		}
		this.m_firstHit = false;
	}

	private Vector3 GetAVGPos(ContactPoint[] points)
	{
		ZLog.Log("Pooints " + points.Length);
		Vector3 vector = Vector3.zero;
		foreach (ContactPoint contactPoint in points)
		{
			ZLog.Log("P " + contactPoint.otherCollider.gameObject.name);
			vector += contactPoint.point;
		}
		return vector;
	}

	private void ResetHitTimer()
	{
		this.m_hitEffectEnabled = true;
	}

	public EffectList m_hitEffect = new EffectList();

	public EffectList m_destroyEffect = new EffectList();

	public float m_hitDestroyChance;

	public float m_minVelocity;

	public float m_maxVelocity;

	public bool m_damageToSelf;

	public bool m_damagePlayers = true;

	public bool m_damageFish;

	public int m_toolTier;

	public HitData.DamageTypes m_damages;

	public LayerMask m_triggerMask;

	public float m_interval = 0.5f;

	private bool m_firstHit = true;

	private bool m_hitEffectEnabled = true;

	private ZNetView m_nview;

	private Rigidbody m_body;
}
