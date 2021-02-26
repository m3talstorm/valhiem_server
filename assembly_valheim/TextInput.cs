using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TextInput : MonoBehaviour
{
	private void Awake()
	{
		TextInput.m_instance = this;
		this.m_panel.SetActive(false);
	}

	public static TextInput instance
	{
		get
		{
			return TextInput.m_instance;
		}
	}

	private void OnDestroy()
	{
		TextInput.m_instance = null;
	}

	public static bool IsVisible()
	{
		return TextInput.m_instance && TextInput.m_instance.m_visibleFrame;
	}

	private void Update()
	{
		this.m_visibleFrame = TextInput.m_instance.m_panel.gameObject.activeSelf;
		if (!this.m_visibleFrame)
		{
			return;
		}
		if (global::Console.IsVisible() || Chat.instance.HasFocus())
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			this.Hide();
			return;
		}
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			string text = this.m_textField.text;
			this.OnEnter(text);
			this.Hide();
		}
		if (!this.m_textField.isFocused)
		{
			EventSystem.current.SetSelectedGameObject(this.m_textField.gameObject);
		}
	}

	private void OnEnter(string text)
	{
		if (this.m_queuedSign != null)
		{
			this.m_queuedSign.SetText(text);
			this.m_queuedSign = null;
		}
	}

	public void RequestText(TextReceiver sign, string topic, int charLimit)
	{
		this.m_queuedSign = sign;
		this.Show(topic, sign.GetText(), charLimit);
	}

	private void Show(string topic, string text, int charLimit)
	{
		this.m_panel.SetActive(true);
		this.m_textField.text = text;
		this.m_topic.text = Localization.instance.Localize(topic);
		this.m_textField.ActivateInputField();
		this.m_textField.characterLimit = charLimit;
	}

	public void Hide()
	{
		this.m_panel.SetActive(false);
	}

	private static TextInput m_instance;

	public GameObject m_panel;

	public InputField m_textField;

	public Text m_topic;

	private TextReceiver m_queuedSign;

	private bool m_visibleFrame;
}
