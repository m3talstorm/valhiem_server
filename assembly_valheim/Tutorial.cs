using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
	public static Tutorial instance
	{
		get
		{
			return Tutorial.m_instance;
		}
	}

	private void Awake()
	{
		Tutorial.m_instance = this;
		this.m_windowRoot.gameObject.SetActive(false);
	}

	private void Update()
	{
	}

	public void ShowText(string name, bool force)
	{
		Tutorial.TutorialText tutorialText = this.m_texts.Find((Tutorial.TutorialText x) => x.m_name == name);
		if (tutorialText != null)
		{
			this.SpawnRaven(tutorialText.m_name, tutorialText.m_topic, tutorialText.m_text, tutorialText.m_label);
		}
	}

	private void SpawnRaven(string key, string topic, string text, string label)
	{
		if (!Raven.IsInstantiated())
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_ravenPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
		}
		Raven.AddTempText(key, topic, text, label, false);
	}

	public List<Tutorial.TutorialText> m_texts = new List<Tutorial.TutorialText>();

	public RectTransform m_windowRoot;

	public Text m_topic;

	public Text m_text;

	public GameObject m_ravenPrefab;

	private static Tutorial m_instance;

	private Queue<string> m_tutQueue = new Queue<string>();

	[Serializable]
	public class TutorialText
	{
		public string m_name;

		public string m_topic = "";

		public string m_label = "";

		[TextArea]
		public string m_text = "";
	}
}
