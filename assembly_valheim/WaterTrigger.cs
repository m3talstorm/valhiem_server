using System;
using UnityEngine;

public class WaterTrigger : MonoBehaviour
{
	private void Update()
	{
		this.m_cooldownTimer += Time.deltaTime;
		if (this.m_cooldownTimer > this.m_cooldownDelay)
		{
			float waterLevel = WaterVolume.GetWaterLevel(base.transform.position, 1f);
			if (base.transform.position.y < waterLevel)
			{
				this.m_effects.Create(base.transform.position, base.transform.rotation, base.transform, 1f);
				this.m_cooldownTimer = 0f;
			}
		}
	}

	public EffectList m_effects = new EffectList();

	public float m_cooldownDelay = 2f;

	private float m_cooldownTimer;
}
