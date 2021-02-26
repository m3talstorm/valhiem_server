using System;
using UnityEngine;

public class SE_Burning : StatusEffect
{
	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (this.m_character.GetSEMan().HaveStatusEffect("Wet"))
		{
			this.m_time += dt * 5f;
		}
		this.m_timer -= dt;
		if (this.m_timer <= 0f)
		{
			this.m_timer = this.m_damageInterval;
			HitData hitData = new HitData();
			hitData.m_point = this.m_character.GetCenterPoint();
			hitData.m_damage = this.m_damage.Clone();
			this.m_character.ApplyDamage(hitData, true, false, HitData.DamageModifier.Normal);
		}
	}

	public void AddFireDamage(float damage)
	{
		this.m_totalDamage = Mathf.Max(this.m_totalDamage, damage);
		int num = (int)(this.m_ttl / this.m_damageInterval);
		float fire = this.m_totalDamage / (float)num;
		this.m_damage.m_fire = fire;
		this.ResetTime();
	}

	public void AddSpiritDamage(float damage)
	{
		this.m_totalDamage = Mathf.Max(this.m_totalDamage, damage);
		int num = (int)(this.m_ttl / this.m_damageInterval);
		float spirit = this.m_totalDamage / (float)num;
		this.m_damage.m_spirit = spirit;
		this.ResetTime();
	}

	[Header("SE_Burning")]
	public float m_damageInterval = 1f;

	private float m_timer;

	private float m_totalDamage;

	private HitData.DamageTypes m_damage;
}
