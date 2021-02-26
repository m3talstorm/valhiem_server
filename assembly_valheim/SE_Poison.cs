using System;
using UnityEngine;

public class SE_Poison : StatusEffect
{
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		this.m_timer -= dt;
		if (this.m_timer <= 0f)
		{
			this.m_timer = this.m_damageInterval;
			HitData hitData = new HitData();
			hitData.m_point = this.m_character.GetCenterPoint();
			hitData.m_damage.m_poison = this.m_damagePerHit;
			this.m_damageLeft -= this.m_damagePerHit;
			this.m_character.ApplyDamage(hitData, true, false, HitData.DamageModifier.Normal);
		}
	}

	public void AddDamage(float damage)
	{
		if (damage >= this.m_damageLeft)
		{
			this.m_damageLeft = damage;
			float num = this.m_character.IsPlayer() ? this.m_TTLPerDamagePlayer : this.m_TTLPerDamage;
			this.m_ttl = this.m_baseTTL + Mathf.Pow(this.m_damageLeft * num, this.m_TTLPower);
			int num2 = (int)(this.m_ttl / this.m_damageInterval);
			this.m_damagePerHit = this.m_damageLeft / (float)num2;
			ZLog.Log(string.Concat(new object[]
			{
				"Poison damage: ",
				this.m_damageLeft,
				" ttl:",
				this.m_ttl,
				" hits:",
				num2,
				" dmg perhit:",
				this.m_damagePerHit
			}));
			this.ResetTime();
		}
	}

	[Header("SE_Poison")]
	public float m_damageInterval = 1f;

	public float m_baseTTL = 2f;

	public float m_TTLPerDamagePlayer = 2f;

	public float m_TTLPerDamage = 2f;

	public float m_TTLPower = 0.5f;

	private float m_timer;

	private float m_damageLeft;

	private float m_damagePerHit;
}
