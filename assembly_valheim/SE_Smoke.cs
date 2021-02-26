using System;
using UnityEngine;

public class SE_Smoke : StatusEffect
{
	public override bool CanAdd(Character character)
	{
		return !character.m_tolerateSmoke && base.CanAdd(character);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		this.m_timer += dt;
		if (this.m_timer > this.m_damageInterval)
		{
			this.m_timer = 0f;
			HitData hitData = new HitData();
			hitData.m_point = this.m_character.GetCenterPoint();
			hitData.m_damage = this.m_damage;
			this.m_character.ApplyDamage(hitData, true, false, HitData.DamageModifier.Normal);
		}
	}

	[Header("SE_Burning")]
	public HitData.DamageTypes m_damage;

	public float m_damageInterval = 1f;

	private float m_timer;
}
