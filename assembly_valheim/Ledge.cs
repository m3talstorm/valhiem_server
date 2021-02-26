using System;
using System.Collections.Generic;
using UnityEngine;

public class Ledge : MonoBehaviour
{
	private void Awake()
	{
		if (base.GetComponent<ZNetView>().GetZDO() == null)
		{
			return;
		}
		this.m_collider.enabled = true;
		TriggerTracker above = this.m_above;
		above.m_changed = (Action)Delegate.Combine(above.m_changed, new Action(this.Changed));
	}

	private void Changed()
	{
		List<Collider> colliders = this.m_above.GetColliders();
		if (colliders.Count == 0)
		{
			this.m_collider.enabled = true;
			return;
		}
		bool enabled = false;
		using (List<Collider>.Enumerator enumerator = colliders.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.transform.position.y > base.transform.position.y)
				{
					enabled = true;
					break;
				}
			}
		}
		this.m_collider.enabled = enabled;
	}

	public Collider m_collider;

	public TriggerTracker m_above;
}
