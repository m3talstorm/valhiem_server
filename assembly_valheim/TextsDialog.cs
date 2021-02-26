using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class TextsDialog : MonoBehaviour
{
	private void Awake()
	{
		this.m_baseListSize = this.m_listRoot.rect.height;
	}

	public void Setup(Player player)
	{
		base.gameObject.SetActive(true);
		this.FillTextList();
		if (this.m_texts.Count > 0)
		{
			this.ShowText(this.m_texts[0]);
			return;
		}
		this.m_textAreaTopic.text = "";
		this.m_textArea.text = "";
	}

	private void Update()
	{
		this.UpdateGamepadInput();
	}

	private void FillTextList()
	{
		foreach (TextsDialog.TextInfo textInfo in this.m_texts)
		{
			UnityEngine.Object.Destroy(textInfo.m_listElement);
		}
		this.m_texts.Clear();
		this.UpdateTextsList();
		for (int i = 0; i < this.m_texts.Count; i++)
		{
			TextsDialog.TextInfo text = this.m_texts[i];
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, Vector3.zero, Quaternion.identity, this.m_listRoot);
			gameObject.SetActive(true);
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)(-(float)i) * this.m_spacing);
			Utils.FindChild(gameObject.transform, "name").GetComponent<Text>().text = Localization.instance.Localize(text.m_topic);
			text.m_listElement = gameObject;
			text.m_selected = Utils.FindChild(gameObject.transform, "selected").gameObject;
			text.m_selected.SetActive(false);
			gameObject.GetComponent<Button>().onClick.AddListener(delegate
			{
				this.OnSelectText(text);
			});
		}
		float size = Mathf.Max(this.m_baseListSize, (float)this.m_texts.Count * this.m_spacing);
		this.m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		if (this.m_texts.Count > 0)
		{
			this.m_recipeEnsureVisible.CenterOnItem(this.m_texts[0].m_listElement.transform as RectTransform);
		}
	}

	private void UpdateGamepadInput()
	{
		if (this.m_texts.Count > 0)
		{
			if (ZInput.GetButtonDown("JoyLStickDown"))
			{
				this.ShowText(Mathf.Min(this.m_texts.Count - 1, this.GetSelectedText() + 1));
			}
			if (ZInput.GetButtonDown("JoyLStickUp"))
			{
				this.ShowText(Mathf.Max(0, this.GetSelectedText() - 1));
			}
		}
	}

	private void OnSelectText(TextsDialog.TextInfo text)
	{
		this.ShowText(text);
	}

	private int GetSelectedText()
	{
		for (int i = 0; i < this.m_texts.Count; i++)
		{
			if (this.m_texts[i].m_selected.activeSelf)
			{
				return i;
			}
		}
		return 0;
	}

	private void ShowText(int i)
	{
		this.ShowText(this.m_texts[i]);
	}

	private void ShowText(TextsDialog.TextInfo text)
	{
		this.m_textAreaTopic.text = Localization.instance.Localize(text.m_topic);
		this.m_textArea.text = Localization.instance.Localize(text.m_text);
		foreach (TextsDialog.TextInfo textInfo in this.m_texts)
		{
			textInfo.m_selected.SetActive(false);
		}
		text.m_selected.SetActive(true);
	}

	public void OnClose()
	{
		base.gameObject.SetActive(false);
	}

	private void UpdateTextsList()
	{
		this.m_texts.Clear();
		foreach (KeyValuePair<string, string> keyValuePair in Player.m_localPlayer.GetKnownTexts())
		{
			this.m_texts.Add(new TextsDialog.TextInfo(Localization.instance.Localize(keyValuePair.Key), Localization.instance.Localize(keyValuePair.Value)));
		}
		this.m_texts.Sort((TextsDialog.TextInfo a, TextsDialog.TextInfo b) => a.m_topic.CompareTo(b.m_topic));
		this.AddLog();
		this.AddActiveEffects();
	}

	private void AddLog()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string str in MessageHud.instance.GetLog())
		{
			stringBuilder.Append(str + "\n\n");
		}
		this.m_texts.Insert(0, new TextsDialog.TextInfo(Localization.instance.Localize("$inventory_logs"), stringBuilder.ToString()));
	}

	private void AddActiveEffects()
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		List<StatusEffect> list = new List<StatusEffect>();
		Player.m_localPlayer.GetSEMan().GetHUDStatusEffects(list);
		StringBuilder stringBuilder = new StringBuilder(256);
		foreach (StatusEffect statusEffect in list)
		{
			stringBuilder.Append("<color=orange>" + Localization.instance.Localize(statusEffect.m_name) + "</color>\n");
			stringBuilder.Append(Localization.instance.Localize(statusEffect.GetTooltipString()));
			stringBuilder.Append("\n\n");
		}
		StatusEffect statusEffect2;
		float num;
		Player.m_localPlayer.GetGuardianPowerHUD(out statusEffect2, out num);
		if (statusEffect2)
		{
			stringBuilder.Append("<color=yellow>" + Localization.instance.Localize("$inventory_selectedgp") + "</color>\n");
			stringBuilder.Append("<color=orange>" + Localization.instance.Localize(statusEffect2.m_name) + "</color>\n");
			stringBuilder.Append(Localization.instance.Localize(statusEffect2.GetTooltipString()));
		}
		this.m_texts.Insert(0, new TextsDialog.TextInfo(Localization.instance.Localize("$inventory_activeeffects"), stringBuilder.ToString()));
	}

	public RectTransform m_listRoot;

	public GameObject m_elementPrefab;

	public Text m_totalSkillText;

	public float m_spacing = 80f;

	public Text m_textAreaTopic;

	public Text m_textArea;

	public ScrollRectEnsureVisible m_recipeEnsureVisible;

	private List<TextsDialog.TextInfo> m_texts = new List<TextsDialog.TextInfo>();

	private float m_baseListSize;

	public class TextInfo
	{
		public TextInfo(string topic, string text)
		{
			this.m_topic = topic;
			this.m_text = text;
		}

		public string m_topic;

		public string m_text;

		public GameObject m_listElement;

		public GameObject m_selected;
	}
}
