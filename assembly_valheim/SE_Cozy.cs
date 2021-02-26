using System;
using UnityEngine;

public class SE_Cozy : SE_Stats
{
	public override void Setup(Character character)
	{
		base.Setup(character);
		this.m_character.Message(MessageHud.MessageType.Center, "$se_resting_start", 0, null);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (this.m_time > this.m_delay)
		{
			this.m_character.GetSEMan().AddStatusEffect(this.m_statusEffect, true);
		}
	}

	public override string GetIconText()
	{
		Player player = this.m_character as Player;
		return Localization.instance.Localize("$se_rested_comfort:" + player.GetComfortLevel());
	}

	[Header("__SE_Cozy__")]
	public float m_delay = 10f;

	public string m_statusEffect = "";

	private int m_comfortLevel;

	private float m_updateTimer;
}
