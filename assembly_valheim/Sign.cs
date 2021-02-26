using System;
using UnityEngine;
using UnityEngine.UI;

public class Sign : MonoBehaviour, Hoverable, Interactable, TextReceiver
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.UpdateText();
		base.InvokeRepeating("UpdateText", 2f, 2f);
	}

	public string GetHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false))
		{
			return "\"" + this.GetText() + "\"";
		}
		return "\"" + this.GetText() + "\"\n" + Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
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
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true))
		{
			return false;
		}
		TextInput.instance.RequestText(this, "$piece_sign_input", this.m_characterLimit);
		return true;
	}

	private void UpdateText()
	{
		string text = this.GetText();
		if (this.m_textWidget.text == text)
		{
			return;
		}
		this.m_textWidget.text = text;
	}

	public string GetText()
	{
		return this.m_nview.GetZDO().GetString("text", this.m_defaultText);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void SetText(string text)
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true))
		{
			return;
		}
		this.m_nview.ClaimOwnership();
		this.m_textWidget.text = text;
		this.m_nview.GetZDO().Set("text", text);
	}

	public Text m_textWidget;

	public string m_name = "Sign";

	public string m_defaultText = "Sign";

	public int m_characterLimit = 50;

	private ZNetView m_nview;
}
