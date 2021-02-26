using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillsDialog : MonoBehaviour
{
	private void Awake()
	{
		this.m_baseListSize = this.m_listRoot.rect.height;
	}

	public void Setup(Player player)
	{
		base.gameObject.SetActive(true);
		foreach (GameObject obj in this.m_elements)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_elements.Clear();
		List<Skills.Skill> skillList = player.GetSkills().GetSkillList();
		for (int i = 0; i < skillList.Count; i++)
		{
			Skills.Skill skill = skillList[i];
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, Vector3.zero, Quaternion.identity, this.m_listRoot);
			gameObject.SetActive(true);
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)(-(float)i) * this.m_spacing);
			gameObject.GetComponentInChildren<UITooltip>().m_text = skill.m_info.m_description;
			Utils.FindChild(gameObject.transform, "icon").GetComponent<Image>().sprite = skill.m_info.m_icon;
			Utils.FindChild(gameObject.transform, "name").GetComponent<Text>().text = Localization.instance.Localize("$skill_" + skill.m_info.m_skill.ToString().ToLower());
			Utils.FindChild(gameObject.transform, "leveltext").GetComponent<Text>().text = ((int)skill.m_level).ToString();
			Utils.FindChild(gameObject.transform, "levelbar").GetComponent<GuiBar>().SetValue(skill.m_level / 100f);
			Utils.FindChild(gameObject.transform, "currentlevel").GetComponent<GuiBar>().SetValue(skill.GetLevelPercentage());
			this.m_elements.Add(gameObject);
		}
		float size = Mathf.Max(this.m_baseListSize, (float)skillList.Count * this.m_spacing);
		this.m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		this.m_totalSkillText.text = string.Concat(new string[]
		{
			"<color=orange>",
			player.GetSkills().GetTotalSkill().ToString("0"),
			"</color><color=white> / </color><color=orange>",
			player.GetSkills().GetTotalSkillCap().ToString("0"),
			"</color>"
		});
	}

	public void OnClose()
	{
		base.gameObject.SetActive(false);
	}

	public RectTransform m_listRoot;

	public GameObject m_elementPrefab;

	public Text m_totalSkillText;

	public float m_spacing = 80f;

	private float m_baseListSize;

	private List<GameObject> m_elements = new List<GameObject>();
}
