using System;
using UnityEngine;

public class StatusEffect : ScriptableObject
{
	public StatusEffect Clone()
	{
		return base.MemberwiseClone() as StatusEffect;
	}

	public virtual bool CanAdd(Character character)
	{
		return true;
	}

	public virtual void Setup(Character character)
	{
		this.m_character = character;
		if (!string.IsNullOrEmpty(this.m_startMessage))
		{
			this.m_character.Message(this.m_startMessageType, this.m_startMessage, 0, null);
		}
		this.TriggerStartEffects();
	}

	public virtual void SetAttacker(Character attacker)
	{
	}

	public virtual string GetTooltipString()
	{
		return this.m_tooltip;
	}

	private void OnApplicationQuit()
	{
		this.m_startEffectInstances = null;
	}

	public virtual void OnDestroy()
	{
		this.RemoveStartEffects();
	}

	protected void TriggerStartEffects()
	{
		this.RemoveStartEffects();
		float radius = this.m_character.GetRadius();
		this.m_startEffectInstances = this.m_startEffects.Create(this.m_character.GetCenterPoint(), this.m_character.transform.rotation, this.m_character.transform, radius * 2f);
	}

	private void RemoveStartEffects()
	{
		if (this.m_startEffectInstances != null && ZNetScene.instance != null)
		{
			foreach (GameObject gameObject in this.m_startEffectInstances)
			{
				if (gameObject)
				{
					ZNetView component = gameObject.GetComponent<ZNetView>();
					if (component.IsValid())
					{
						component.ClaimOwnership();
						component.Destroy();
					}
				}
			}
			this.m_startEffectInstances = null;
		}
	}

	public virtual void Stop()
	{
		this.RemoveStartEffects();
		this.m_stopEffects.Create(this.m_character.transform.position, this.m_character.transform.rotation, null, 1f);
		if (!string.IsNullOrEmpty(this.m_stopMessage))
		{
			this.m_character.Message(this.m_stopMessageType, this.m_stopMessage, 0, null);
		}
	}

	public virtual void UpdateStatusEffect(float dt)
	{
		this.m_time += dt;
		if (this.m_repeatInterval > 0f && !string.IsNullOrEmpty(this.m_repeatMessage))
		{
			this.m_msgTimer += dt;
			if (this.m_msgTimer > this.m_repeatInterval)
			{
				this.m_msgTimer = 0f;
				this.m_character.Message(this.m_repeatMessageType, this.m_repeatMessage, 0, null);
			}
		}
	}

	public virtual bool IsDone()
	{
		return this.m_ttl > 0f && this.m_time > this.m_ttl;
	}

	public virtual void ResetTime()
	{
		this.m_time = 0f;
	}

	public float GetDuration()
	{
		return this.m_time;
	}

	public float GetRemaningTime()
	{
		return this.m_ttl - this.m_time;
	}

	public virtual string GetIconText()
	{
		if (this.m_ttl > 0f)
		{
			return StatusEffect.GetTimeString(this.m_ttl - this.GetDuration(), false, false);
		}
		return "";
	}

	public static string GetTimeString(float time, bool sufix = false, bool alwaysShowMinutes = false)
	{
		if (time <= 0f)
		{
			return "";
		}
		int num = Mathf.CeilToInt(time);
		int num2 = (int)((float)num / 60f);
		int num3 = Mathf.Max(0, num - num2 * 60);
		if (sufix)
		{
			if (num2 > 0 || alwaysShowMinutes)
			{
				return string.Concat(new object[]
				{
					num2,
					"m:",
					num3.ToString("00"),
					"s"
				});
			}
			return num3.ToString() + "s";
		}
		else
		{
			if (num2 > 0 || alwaysShowMinutes)
			{
				return num2 + ":" + num3.ToString("00");
			}
			return num3.ToString();
		}
	}

	public virtual void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
	}

	public virtual void ModifyHealthRegen(ref float regenMultiplier)
	{
	}

	public virtual void ModifyStaminaRegen(ref float staminaRegen)
	{
	}

	public virtual void ModifyDamageMods(ref HitData.DamageModifiers modifiers)
	{
	}

	public virtual void ModifyRaiseSkill(Skills.SkillType skill, ref float value)
	{
	}

	public virtual void ModifySpeed(ref float speed)
	{
	}

	public virtual void ModifyNoise(float baseNoise, ref float noise)
	{
	}

	public virtual void ModifyStealth(float baseStealth, ref float stealth)
	{
	}

	public virtual void ModifyMaxCarryWeight(float baseLimit, ref float limit)
	{
	}

	public virtual void ModifyRunStaminaDrain(float baseDrain, ref float drain)
	{
	}

	public virtual void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	public virtual void OnDamaged(HitData hit, Character attacker)
	{
	}

	public bool HaveAttribute(StatusEffect.StatusAttribute value)
	{
		return (this.m_attributes & value) > StatusEffect.StatusAttribute.None;
	}

	[Header("__Common__")]
	public string m_name = "";

	public string m_category = "";

	public Sprite m_icon;

	public bool m_flashIcon;

	public bool m_cooldownIcon;

	[TextArea]
	public string m_tooltip = "";

	[BitMask(typeof(StatusEffect.StatusAttribute))]
	public StatusEffect.StatusAttribute m_attributes;

	public MessageHud.MessageType m_startMessageType = MessageHud.MessageType.TopLeft;

	public string m_startMessage = "";

	public MessageHud.MessageType m_stopMessageType = MessageHud.MessageType.TopLeft;

	public string m_stopMessage = "";

	public MessageHud.MessageType m_repeatMessageType = MessageHud.MessageType.TopLeft;

	public string m_repeatMessage = "";

	public float m_repeatInterval;

	public float m_ttl;

	public EffectList m_startEffects = new EffectList();

	public EffectList m_stopEffects = new EffectList();

	[Header("__Guardian power__")]
	public float m_cooldown;

	public string m_activationAnimation = "gpower";

	[NonSerialized]
	public bool m_isNew = true;

	private float m_msgTimer;

	protected Character m_character;

	protected float m_time;

	protected GameObject[] m_startEffectInstances;

	public enum StatusAttribute
	{
		None,
		ColdResistance,
		DoubleImpactDamage,
		SailingPower = 4
	}
}
