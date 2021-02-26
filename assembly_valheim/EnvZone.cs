using System;
using UnityEngine;

public class EnvZone : MonoBehaviour
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
		if (this.m_force)
		{
			EnvMan.instance.SetForceEnvironment(this.m_environment);
		}
		EnvZone.m_triggered = this;
	}

	private void OnTriggerExit(Collider collider)
	{
		if (EnvZone.m_triggered != this)
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
		if (this.m_force)
		{
			EnvMan.instance.SetForceEnvironment("");
		}
		EnvZone.m_triggered = null;
	}

	public static string GetEnvironment()
	{
		if (EnvZone.m_triggered && !EnvZone.m_triggered.m_force)
		{
			return EnvZone.m_triggered.m_environment;
		}
		return null;
	}

	public string m_environment = "";

	public bool m_force = true;

	private static EnvZone m_triggered;
}
