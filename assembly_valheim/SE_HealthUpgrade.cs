using System;
using UnityEngine;

public class SE_HealthUpgrade : StatusEffect
{
	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override void Stop()
	{
		base.Stop();
		Player player = this.m_character as Player;
		if (!player)
		{
			return;
		}
		if (this.m_moreHealth > 0f)
		{
			player.SetMaxHealth(this.m_character.GetMaxHealth() + this.m_moreHealth, true);
			player.SetHealth(this.m_character.GetMaxHealth());
		}
		if (this.m_moreStamina > 0f)
		{
			player.SetMaxStamina(this.m_character.GetMaxStamina() + this.m_moreStamina, true);
		}
		this.m_upgradeEffect.Create(this.m_character.transform.position, Quaternion.identity, null, 1f);
	}

	[Header("Health")]
	public float m_moreHealth;

	[Header("Stamina")]
	public float m_moreStamina;

	public EffectList m_upgradeEffect = new EffectList();
}
