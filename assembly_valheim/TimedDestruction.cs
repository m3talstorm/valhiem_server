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
		base.Invoke("DestroyNow", this.m_timeout);
	}

	public void Trigger(float timeout)
	{
		base.Invoke("DestroyNow", timeout);
	}

	private void DestroyNow()
	{
		if (this.m_nview)
		{
			if (!this.m_nview.IsValid())
			{
				return;
			}
			if (this.m_nview.GetZDO().m_owner == 0L)
			{
				this.m_nview.ClaimOwnership();
			}
			if (this.m_nview.IsOwner())
			{
				ZNetScene.instance.Destroy(base.gameObject);
				return;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public float m_timeout = 1f;

	public bool m_triggerOnAwake;

	private ZNetView m_nview;
}
