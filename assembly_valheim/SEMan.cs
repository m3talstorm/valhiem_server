using System;
using System.Collections.Generic;

public class SEMan
{
	public SEMan(Character character, ZNetView nview)
	{
		this.m_character = character;
		this.m_nview = nview;
		this.m_nview.Register<string, bool>("AddStatusEffect", new Action<long, string, bool>(this.RPC_AddStatusEffect));
	}

	public void OnDestroy()
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.OnDestroy();
		}
		this.m_statusEffects.Clear();
	}

	public void ApplyStatusEffectSpeedMods(ref float speed)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifySpeed(ref speed);
		}
	}

	public void ApplyDamageMods(ref HitData.DamageModifiers mods)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyDamageMods(ref mods);
		}
	}

	public void Update(float dt)
	{
		this.m_statusEffectAttributes = 0;
		int count = this.m_statusEffects.Count;
		for (int i = 0; i < count; i++)
		{
			StatusEffect statusEffect = this.m_statusEffects[i];
			statusEffect.UpdateStatusEffect(dt);
			if (statusEffect.IsDone())
			{
				this.m_removeStatusEffects.Add(statusEffect);
			}
			else
			{
				this.m_statusEffectAttributes |= (int)statusEffect.m_attributes;
			}
		}
		if (this.m_removeStatusEffects.Count > 0)
		{
			foreach (StatusEffect statusEffect2 in this.m_removeStatusEffects)
			{
				statusEffect2.Stop();
				this.m_statusEffects.Remove(statusEffect2);
			}
			this.m_removeStatusEffects.Clear();
		}
		this.m_nview.GetZDO().Set("seAttrib", this.m_statusEffectAttributes);
	}

	public StatusEffect AddStatusEffect(string name, bool resetTime = false)
	{
		if (this.m_nview.IsOwner())
		{
			return this.Internal_AddStatusEffect(name, resetTime);
		}
		this.m_nview.InvokeRPC("AddStatusEffect", new object[]
		{
			name,
			resetTime
		});
		return null;
	}

	private void RPC_AddStatusEffect(long sender, string name, bool resetTime)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.Internal_AddStatusEffect(name, resetTime);
	}

	private StatusEffect Internal_AddStatusEffect(string name, bool resetTime)
	{
		StatusEffect statusEffect = this.GetStatusEffect(name);
		if (statusEffect)
		{
			if (resetTime)
			{
				statusEffect.ResetTime();
			}
			return null;
		}
		StatusEffect statusEffect2 = ObjectDB.instance.GetStatusEffect(name);
		if (statusEffect2 == null)
		{
			return null;
		}
		return this.AddStatusEffect(statusEffect2, false);
	}

	public StatusEffect AddStatusEffect(StatusEffect statusEffect, bool resetTime = false)
	{
		StatusEffect statusEffect2 = this.GetStatusEffect(statusEffect.name);
		if (statusEffect2)
		{
			if (resetTime)
			{
				statusEffect2.ResetTime();
			}
			return null;
		}
		if (!statusEffect.CanAdd(this.m_character))
		{
			return null;
		}
		StatusEffect statusEffect3 = statusEffect.Clone();
		this.m_statusEffects.Add(statusEffect3);
		statusEffect3.Setup(this.m_character);
		if (this.m_character.IsPlayer())
		{
			Gogan.LogEvent("Game", "StatusEffect", statusEffect.name, 0L);
		}
		return statusEffect3;
	}

	public bool RemoveStatusEffect(StatusEffect se, bool quiet = false)
	{
		return this.RemoveStatusEffect(se.name, quiet);
	}

	public bool RemoveStatusEffect(string name, bool quiet = false)
	{
		for (int i = 0; i < this.m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = this.m_statusEffects[i];
			if (statusEffect.name == name)
			{
				if (quiet)
				{
					statusEffect.m_stopMessage = "";
				}
				statusEffect.Stop();
				this.m_statusEffects.Remove(statusEffect);
				return true;
			}
		}
		return false;
	}

	public bool HaveStatusEffectCategory(string cat)
	{
		if (cat.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < this.m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = this.m_statusEffects[i];
			if (statusEffect.m_category.Length > 0 && statusEffect.m_category == cat)
			{
				return true;
			}
		}
		return false;
	}

	public bool HaveStatusAttribute(StatusEffect.StatusAttribute value)
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (this.m_nview.IsOwner())
		{
			return (this.m_statusEffectAttributes & (int)value) != 0;
		}
		return (this.m_nview.GetZDO().GetInt("seAttrib", 0) & (int)value) != 0;
	}

	public bool HaveStatusEffect(string name)
	{
		for (int i = 0; i < this.m_statusEffects.Count; i++)
		{
			if (this.m_statusEffects[i].name == name)
			{
				return true;
			}
		}
		return false;
	}

	public List<StatusEffect> GetStatusEffects()
	{
		return this.m_statusEffects;
	}

	public StatusEffect GetStatusEffect(string name)
	{
		for (int i = 0; i < this.m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = this.m_statusEffects[i];
			if (statusEffect.name == name)
			{
				return statusEffect;
			}
		}
		return null;
	}

	public void GetHUDStatusEffects(List<StatusEffect> effects)
	{
		for (int i = 0; i < this.m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = this.m_statusEffects[i];
			if (statusEffect.m_icon)
			{
				effects.Add(statusEffect);
			}
		}
	}

	public void ModifyNoise(float baseNoise, ref float noise)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyNoise(baseNoise, ref noise);
		}
	}

	public void ModifyRaiseSkill(Skills.SkillType skill, ref float multiplier)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyRaiseSkill(skill, ref multiplier);
		}
	}

	public void ModifyStaminaRegen(ref float staminaMultiplier)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyStaminaRegen(ref staminaMultiplier);
		}
	}

	public void ModifyHealthRegen(ref float regenMultiplier)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyHealthRegen(ref regenMultiplier);
		}
	}

	public void ModifyMaxCarryWeight(float baseLimit, ref float limit)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyMaxCarryWeight(baseLimit, ref limit);
		}
	}

	public void ModifyStealth(float baseStealth, ref float stealth)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyStealth(baseStealth, ref stealth);
		}
	}

	public void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyAttack(skill, ref hitData);
		}
	}

	public void ModifyRunStaminaDrain(float baseDrain, ref float drain)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyRunStaminaDrain(baseDrain, ref drain);
		}
		if (drain < 0f)
		{
			drain = 0f;
		}
	}

	public void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyJumpStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	public void OnDamaged(HitData hit, Character attacker)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.OnDamaged(hit, attacker);
		}
	}

	protected List<StatusEffect> m_statusEffects = new List<StatusEffect>();

	private List<StatusEffect> m_removeStatusEffects = new List<StatusEffect>();

	private int m_statusEffectAttributes;

	private Character m_character;

	private ZNetView m_nview;
}
