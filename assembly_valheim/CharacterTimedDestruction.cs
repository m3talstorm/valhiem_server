using System;
using UnityEngine;

public class CharacterTimedDestruction : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_triggerOnAwake)
		{
			this.Trigger();
		}
	}

	public void Trigger()
	{
		base.InvokeRepeating("DestroyNow", UnityEngine.Random.Range(this.m_timeoutMin, this.m_timeoutMax), 1f);
	}

	public void Trigger(float timeout)
	{
		base.InvokeRepeating("DestroyNow", timeout, 1f);
	}

	private void DestroyNow()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		Character component = base.GetComponent<Character>();
		HitData hitData = new HitData();
		hitData.m_damage.m_damage = 99999f;
		hitData.m_point = base.transform.position;
		component.ApplyDamage(hitData, false, true, HitData.DamageModifier.Normal);
	}

	public float m_timeoutMin = 1f;

	public float m_timeoutMax = 1f;

	public bool m_triggerOnAwake;

	private ZNetView m_nview;

	private Character m_character;
}
