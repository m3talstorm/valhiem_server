using System;
using UnityEngine;

public class Tracker : MonoBehaviour
{
	private void Awake()
	{
		ZNetView component = base.GetComponent<ZNetView>();
		if (component && component.IsOwner())
		{
			this.m_active = true;
			ZNet.instance.SetReferencePosition(base.transform.position);
		}
	}

	public void SetActive(bool active)
	{
		this.m_active = active;
	}

	private void OnDestroy()
	{
		this.m_active = false;
	}

	private void FixedUpdate()
	{
		if (this.m_active)
		{
			ZNet.instance.SetReferencePosition(base.transform.position);
		}
	}

	private bool m_active;
}
