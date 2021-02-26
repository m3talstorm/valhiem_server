using System;
using UnityEngine;

public class Switch : MonoBehaviour, Interactable, Hoverable
{
	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			if (this.m_holdRepeatInterval <= 0f)
			{
				return false;
			}
			if (Time.time - this.m_lastUseTime < this.m_holdRepeatInterval)
			{
				return false;
			}
		}
		this.m_lastUseTime = Time.time;
		return this.m_onUse != null && this.m_onUse(this, character, null);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return this.m_onUse != null && this.m_onUse(this, user, item);
	}

	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_hoverText);
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_name);
	}

	public Switch.Callback m_onUse;

	public string m_hoverText = "";

	public string m_name = "";

	public float m_holdRepeatInterval = -1f;

	private float m_lastUseTime;

	public delegate bool Callback(Switch caller, Humanoid user, ItemDrop.ItemData item);
}
