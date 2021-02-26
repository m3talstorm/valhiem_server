using System;
using UnityEngine;

public class SE_Spawn : StatusEffect
{
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (this.m_spawned)
		{
			return;
		}
		if (this.m_time > this.m_delay)
		{
			this.m_spawned = true;
			Vector3 position = this.m_character.transform.TransformVector(this.m_spawnOffset);
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_prefab, position, Quaternion.identity);
			Projectile component = gameObject.GetComponent<Projectile>();
			if (component)
			{
				component.Setup(this.m_character, Vector3.zero, -1f, null, null);
			}
			this.m_spawnEffect.Create(gameObject.transform.position, gameObject.transform.rotation, null, 1f);
		}
	}

	[Header("__SE_Spawn__")]
	public float m_delay = 10f;

	public GameObject m_prefab;

	public Vector3 m_spawnOffset = new Vector3(0f, 0f, 0f);

	public EffectList m_spawnEffect = new EffectList();

	private bool m_spawned;
}
