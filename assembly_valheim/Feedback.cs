using System;
using UnityEngine;
using UnityEngine.UI;

public class Feedback : MonoBehaviour
{
	private void Awake()
	{
		Feedback.m_instance = this;
	}

	private void OnDestroy()
	{
		if (Feedback.m_instance == this)
		{
			Feedback.m_instance = null;
		}
	}

	public static bool IsVisible()
	{
		return Feedback.m_instance != null;
	}

	private void LateUpdate()
	{
		this.m_sendButton.interactable = this.IsValid();
		if (Feedback.IsVisible() && (Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyMenu")))
		{
			this.OnBack();
		}
	}

	private bool IsValid()
	{
		return this.m_subject.text.Length != 0 && this.m_text.text.Length != 0;
	}

	public void OnBack()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void OnSend()
	{
		if (!this.IsValid())
		{
			return;
		}
		string category = this.GetCategory();
		Gogan.LogEvent("Feedback_" + category, this.m_subject.text, this.m_text.text, 0L);
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private string GetCategory()
	{
		if (this.m_catBug.isOn)
		{
			return "Bug";
		}
		if (this.m_catFeedback.isOn)
		{
			return "Feedback";
		}
		if (this.m_catIdea.isOn)
		{
			return "Idea";
		}
		return "";
	}

	private static Feedback m_instance;

	public Text m_subject;

	public Text m_text;

	public Button m_sendButton;

	public Toggle m_catBug;

	public Toggle m_catFeedback;

	public Toggle m_catIdea;
}
