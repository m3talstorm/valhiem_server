using System;
using UnityEngine;

public class Vegvisir : MonoBehaviour, Hoverable, Interactable
{
	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_name + " " + this.m_pinName + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_register_location ");
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			return false;
		}
		Game.instance.DiscoverClosestLocation(this.m_locationName, base.transform.position, this.m_pinName, (int)this.m_pinType);
		GoogleAnalyticsV4.instance.LogEvent("Game", "Vegvisir", this.m_locationName, 0L);
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public string m_name = "$piece_vegvisir";

	public string m_locationName = "";

	public string m_pinName = "Pin";

	public Minimap.PinType m_pinType;
}
