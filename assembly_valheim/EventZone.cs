using System;
using UnityEngine;

public class EventZone : MonoBehaviour
{
	private void OnTriggerStay(Collider collider)
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
		EventZone.m_triggered = this;
	}

	private void OnTriggerExit(Collider collider)
	{
		if (EventZone.m_triggered != this)
		{
			return;
		}
		Player component = collider.GetComponent<Player>();
		if (component == null)
		{
			return;
		}
		if (Player.m_localPlayer != component)
		{
			return;
		}
		EventZone.m_triggered = null;
	}

	public static string GetEvent()
	{
		if (EventZone.m_triggered && EventZone.m_triggered.m_event.Length > 0)
		{
			return EventZone.m_triggered.m_event;
		}
		return null;
	}

	public string m_event = "";

	private static EventZone m_triggered;
}
