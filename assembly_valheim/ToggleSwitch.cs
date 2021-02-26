using System;
using UnityEngine;

public class ToggleSwitch : MonoBehaviour, Interactable, Hoverable
{
	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_onUse != null)
		{
			this.m_onUse(this, character);
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public string GetHoverText()
	{
		return this.m_hoverText;
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public void SetState(bool enabled)
	{
		this.m_state = enabled;
		this.m_renderer.material = (this.m_state ? this.m_enableMaterial : this.m_disableMaterial);
	}

	public MeshRenderer m_renderer;

	public Material m_enableMaterial;

	public Material m_disableMaterial;

	public Action<ToggleSwitch, Humanoid> m_onUse;

	public string m_hoverText = "";

	public string m_name = "";

	private bool m_state;
}
