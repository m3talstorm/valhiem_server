using System;
using UnityEngine;
using UnityEngine.UI;

public class TextViewer : MonoBehaviour
{
	private void Awake()
	{
		TextViewer.m_instance = this;
		this.m_root.SetActive(true);
		this.m_introRoot.SetActive(true);
		this.m_ravenRoot.SetActive(true);
		this.m_animator = this.m_root.GetComponent<Animator>();
		this.m_animatorIntro = this.m_introRoot.GetComponent<Animator>();
		this.m_animatorRaven = this.m_ravenRoot.GetComponent<Animator>();
	}

	private void OnDestroy()
	{
		TextViewer.m_instance = null;
	}

	public static TextViewer instance
	{
		get
		{
			return TextViewer.m_instance;
		}
	}

	private void LateUpdate()
	{
		if (!this.IsVisible())
		{
			return;
		}
		this.m_showTime += Time.deltaTime;
		if (this.m_showTime > 0.2f)
		{
			if (this.m_autoHide && Player.m_localPlayer && Vector3.Distance(Player.m_localPlayer.transform.position, this.m_openPlayerPos) > 3f)
			{
				this.Hide();
			}
			if (ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse") || Input.GetKeyDown(KeyCode.Escape))
			{
				this.Hide();
			}
		}
	}

	public void ShowText(TextViewer.Style style, string topic, string text, bool autoHide)
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		topic = Localization.instance.Localize(topic);
		text = Localization.instance.Localize(text);
		if (style == TextViewer.Style.Rune)
		{
			this.m_topic.text = topic;
			this.m_text.text = text;
			this.m_runeText.text = text;
			this.m_animator.SetBool(TextViewer.m_visibleID, true);
		}
		else if (style == TextViewer.Style.Intro)
		{
			this.m_introTopic.text = topic;
			this.m_introText.text = text;
			this.m_animatorIntro.SetTrigger("play");
			ZLog.Log("Show intro " + Time.frameCount);
		}
		else if (style == TextViewer.Style.Raven)
		{
			this.m_ravenTopic.text = topic;
			this.m_ravenText.text = text;
			this.m_animatorRaven.SetBool(TextViewer.m_visibleID, true);
		}
		this.m_autoHide = autoHide;
		this.m_openPlayerPos = Player.m_localPlayer.transform.position;
		this.m_showTime = 0f;
		ZLog.Log("Show text " + topic + ":" + text);
	}

	public void Hide()
	{
		this.m_autoHide = false;
		this.m_animator.SetBool(TextViewer.m_visibleID, false);
		this.m_animatorRaven.SetBool(TextViewer.m_visibleID, false);
	}

	public bool IsVisible()
	{
		return TextViewer.m_instance.m_animatorIntro.GetCurrentAnimatorStateInfo(0).tagHash == TextViewer.m_animatorTagVisible || this.m_animator.GetBool(TextViewer.m_visibleID) || this.m_animatorIntro.GetBool(TextViewer.m_visibleID) || this.m_animatorRaven.GetBool(TextViewer.m_visibleID);
	}

	public static bool IsShowingIntro()
	{
		return TextViewer.m_instance != null && TextViewer.m_instance.m_animatorIntro.GetCurrentAnimatorStateInfo(0).tagHash == TextViewer.m_animatorTagVisible;
	}

	private static TextViewer m_instance;

	private Animator m_animator;

	private Animator m_animatorIntro;

	private Animator m_animatorRaven;

	[Header("Rune")]
	public GameObject m_root;

	public Text m_topic;

	public Text m_text;

	public Text m_runeText;

	public GameObject m_closeText;

	[Header("Intro")]
	public GameObject m_introRoot;

	public Text m_introTopic;

	public Text m_introText;

	[Header("Raven")]
	public GameObject m_ravenRoot;

	public Text m_ravenTopic;

	public Text m_ravenText;

	private static int m_visibleID = Animator.StringToHash("visible");

	private static int m_animatorTagVisible = Animator.StringToHash("visible");

	private float m_showTime;

	private bool m_autoHide;

	private Vector3 m_openPlayerPos = Vector3.zero;

	public enum Style
	{
		Rune,
		Intro,
		Raven
	}
}
