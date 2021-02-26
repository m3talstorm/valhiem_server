using System;
using UnityEngine;

public class HitArea : MonoBehaviour, IDestructible
{
	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Default;
	}

	public void Damage(HitData hit)
	{
		if (this.m_onHit != null)
		{
			this.m_onHit(hit, this);
		}
	}

	public Action<HitData, HitArea> m_onHit;

	public float m_health = 1f;

	[NonSerialized]
	public GameObject m_parentObject;
}
