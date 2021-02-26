using System;
using UnityEngine;

public class SE_Frost : StatusEffect
{
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
	}

	public void AddDamage(float damage)
	{
		float num = this.m_character.IsPlayer() ? this.m_freezeTimePlayer : this.m_freezeTimeEnemy;
		float num2 = Mathf.Clamp01(damage / this.m_character.GetMaxHealth()) * num;
		float num3 = this.m_ttl - this.m_time;
		if (num2 > num3)
		{
			this.m_ttl = num2;
			this.ResetTime();
			base.TriggerStartEffects();
		}
	}

	public override void ModifySpeed(ref float speed)
	{
		float num = Mathf.Clamp01(this.m_time / this.m_ttl);
		num = Mathf.Pow(num, 2f);
		speed *= Mathf.Clamp(num, this.m_minSpeedFactor, 1f);
	}

	[Header("SE_Frost")]
	public float m_freezeTimeEnemy = 10f;

	public float m_freezeTimePlayer = 10f;

	public float m_minSpeedFactor = 0.1f;
}
