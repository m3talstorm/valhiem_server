using System;
using UnityEngine;

public class HoverText : MonoBehaviour, Hoverable
{
	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_text);
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_text);
	}

	public string m_text = "";
}
