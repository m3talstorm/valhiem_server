using System;
using UnityEngine;

public class TeleportHome : MonoBehaviour
{
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
		Game.instance.RequestRespawn(0f);
	}
}
