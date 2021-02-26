using System;
using UnityEngine;

public class SE_Finder : StatusEffect
{
	public override void UpdateStatusEffect(float dt)
	{
		this.m_updateBeaconTimer += dt;
		if (this.m_updateBeaconTimer > 1f)
		{
			this.m_updateBeaconTimer = 0f;
			Beacon beacon = Beacon.FindClosestBeaconInRange(this.m_character.transform.position);
			if (beacon != this.m_beacon)
			{
				this.m_beacon = beacon;
				if (this.m_beacon)
				{
					this.m_lastDistance = Utils.DistanceXZ(this.m_character.transform.position, this.m_beacon.transform.position);
					this.m_pingTimer = 0f;
				}
			}
		}
		if (this.m_beacon != null)
		{
			float num = Utils.DistanceXZ(this.m_character.transform.position, this.m_beacon.transform.position);
			float num2 = Mathf.Clamp01(num / this.m_beacon.m_range);
			float num3 = Mathf.Lerp(this.m_closeFrequency, this.m_distantFrequency, num2);
			this.m_pingTimer += dt;
			if (this.m_pingTimer > num3)
			{
				this.m_pingTimer = 0f;
				if (num2 < 0.2f)
				{
					this.m_pingEffectNear.Create(this.m_character.transform.position, this.m_character.transform.rotation, this.m_character.transform, 1f);
				}
				else if (num2 < 0.6f)
				{
					this.m_pingEffectMed.Create(this.m_character.transform.position, this.m_character.transform.rotation, this.m_character.transform, 1f);
				}
				else
				{
					this.m_pingEffectFar.Create(this.m_character.transform.position, this.m_character.transform.rotation, this.m_character.transform, 1f);
				}
				this.m_lastDistance = num;
			}
		}
	}

	[Header("SE_Finder")]
	public EffectList m_pingEffectNear = new EffectList();

	public EffectList m_pingEffectMed = new EffectList();

	public EffectList m_pingEffectFar = new EffectList();

	public float m_closerTriggerDistance = 2f;

	public float m_furtherTriggerDistance = 4f;

	public float m_closeFrequency = 1f;

	public float m_distantFrequency = 5f;

	private float m_updateBeaconTimer;

	private float m_pingTimer;

	private Beacon m_beacon;

	private float m_lastDistance;
}
