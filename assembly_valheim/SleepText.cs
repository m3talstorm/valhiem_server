using System;
using UnityEngine;
using UnityEngine.UI;

public class SleepText : MonoBehaviour
{
	private void OnEnable()
	{
		this.m_textField.canvasRenderer.SetAlpha(0f);
		this.m_textField.CrossFadeAlpha(1f, 1f, true);
		this.m_dreamField.enabled = false;
		base.Invoke("HideZZZ", 2f);
		base.Invoke("ShowDreamText", 4f);
	}

	private void HideZZZ()
	{
		this.m_textField.CrossFadeAlpha(0f, 2f, true);
	}

	private void ShowDreamText()
	{
		DreamTexts.DreamText randomDreamText = this.m_dreamTexts.GetRandomDreamText();
		if (randomDreamText == null)
		{
			return;
		}
		this.m_dreamField.enabled = true;
		this.m_dreamField.canvasRenderer.SetAlpha(0f);
		this.m_dreamField.CrossFadeAlpha(1f, 1.5f, true);
		this.m_dreamField.text = Localization.instance.Localize(randomDreamText.m_text);
		base.Invoke("HideDreamText", 6.5f);
	}

	private void HideDreamText()
	{
		this.m_dreamField.CrossFadeAlpha(0f, 1.5f, true);
	}

	public Text m_textField;

	public Text m_dreamField;

	public DreamTexts m_dreamTexts;
}
