using System;
using UnityEngine;

public class SE_Shield : StatusEffect
{
	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override bool IsDone()
	{
		if (this.m_damage > this.m_absorbDamage)
		{
			this.m_breakEffects.Create(this.m_character.GetCenterPoint(), this.m_character.transform.rotation, this.m_character.transform, this.m_character.GetRadius() * 2f);
			return true;
		}
		return base.IsDone();
	}

	public override void OnDamaged(HitData hit, Character attacker)
	{
		float totalDamage = hit.GetTotalDamage();
		this.m_damage += totalDamage;
		hit.ApplyModifier(0f);
		this.m_hitEffects.Create(hit.m_point, Quaternion.identity, null, 1f);
	}

	[Header("__SE_Shield__")]
	public float m_absorbDamage = 100f;

	public EffectList m_breakEffects = new EffectList();

	public EffectList m_hitEffects = new EffectList();

	private float m_damage;
}
