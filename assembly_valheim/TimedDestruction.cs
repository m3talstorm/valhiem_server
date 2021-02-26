using System;
using UnityEngine;

public class TimedDestruction : MonoBehaviour
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
		base.InvokeRepeating("DestroyNow", this.m_timeout, 1f);
	}

	public void Trigger(float timeout)
	{
		base.InvokeRepeating("DestroyNow", timeout, 1f);
	}

	private void DestroyNow()
	{
		if (!this.m_nview)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		ZNetScene.instance.Destroy(base.gameObject);
	}

	public float m_timeout = 1f;

	public bool m_triggerOnAwake;

	private ZNetView m_nview;
}
