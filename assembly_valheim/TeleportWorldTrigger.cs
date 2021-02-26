using System;
using UnityEngine;

public class TeleportWorldTrigger : MonoBehaviour
{
	private void Awake()
	{
		this.m_tp = base.GetComponentInParent<TeleportWorld>();
	}

	private void OnTriggerEnter(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (component == null)
		{
			return;
		}
		if (Player.m_localPlayer != component)
		{
			return;
		}
		ZLog.Log("TRIGGER");
		this.m_tp.Teleport(component);
	}

	private TeleportWorld m_tp;
}
