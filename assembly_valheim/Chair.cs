using System;
using UnityEngine;

public class Chair : MonoBehaviour, Hoverable, Interactable
{
	public string GetHoverText()
	{
		if (Time.time - Chair.m_lastSitTime < 2f)
		{
			return "";
		}
		if (!this.InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=grey>$piece_toofar</color>");
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid human, bool hold)
	{
		if (hold)
		{
			return false;
		}
		Player player = human as Player;
		if (!this.InUseDistance(player))
		{
			return false;
		}
		if (Time.time - Chair.m_lastSitTime < 2f)
		{
			return false;
		}
		if (player)
		{
			if (player.IsEncumbered())
			{
				return false;
			}
			player.AttachStart(this.m_attachPoint, false, false, this.m_attachAnimation, this.m_detachOffset);
			Chair.m_lastSitTime = Time.time;
		}
		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, this.m_attachPoint.position) < this.m_useDistance;
	}

	public string m_name = "Chair";

	public float m_useDistance = 2f;

	public Transform m_attachPoint;

	public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);

	public string m_attachAnimation = "attach_chair";

	private const float m_minSitDelay = 2f;

	private static float m_lastSitTime;
}
