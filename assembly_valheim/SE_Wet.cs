using System;
using UnityEngine;

public class SE_Wet : SE_Stats
{
	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (!this.m_character.m_tolerateWater)
		{
			this.m_timer += dt;
			if (this.m_timer > this.m_damageInterval)
			{
				this.m_timer = 0f;
				HitData hitData = new HitData();
				hitData.m_point = this.m_character.transform.position;
				hitData.m_damage.m_damage = this.m_waterDamage;
				this.m_character.Damage(hitData);
			}
		}
		if (this.m_character.GetSEMan().HaveStatusEffect("CampFire"))
		{
			this.m_time += dt * 10f;
		}
		if (this.m_character.GetSEMan().HaveStatusEffect("Burning"))
		{
			this.m_time += dt * 50f;
		}
	}

	[Header("__SE_Wet__")]
	public float m_waterDamage;

	public float m_damageInterval = 0.5f;

	private float m_timer;
}
