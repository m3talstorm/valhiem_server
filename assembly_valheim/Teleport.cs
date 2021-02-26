using System;
using UnityEngine;

public class Teleport : MonoBehaviour, Hoverable, Interactable
{
	public string GetHoverText()
	{
		return Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] " + this.m_hoverText);
	}

	public string GetHoverName()
	{
		return "";
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
		this.Interact(component, false);
	}

	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_targetPoint == null)
		{
			return false;
		}
		if (character.TeleportTo(this.m_targetPoint.GetTeleportPoint(), this.m_targetPoint.transform.rotation, false))
		{
			if (this.m_enterText.Length > 0)
			{
				MessageHud.instance.ShowBiomeFoundMsg(this.m_enterText, false);
			}
			return true;
		}
		return false;
	}

	private Vector3 GetTeleportPoint()
	{
		return base.transform.position + base.transform.forward - base.transform.up;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void OnDrawGizmos()
	{
	}

	public string m_hoverText = "$location_enter";

	public string m_enterText = "";

	public Teleport m_targetPoint;
}
