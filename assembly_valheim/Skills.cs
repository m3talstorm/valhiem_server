using System;
using System.Collections.Generic;
using UnityEngine;

public class Skills : MonoBehaviour
{
	public void Awake()
	{
		this.m_player = base.GetComponent<Player>();
	}

	public void Save(ZPackage pkg)
	{
		pkg.Write(2);
		pkg.Write(this.m_skillData.Count);
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			pkg.Write((int)keyValuePair.Value.m_info.m_skill);
			pkg.Write(keyValuePair.Value.m_level);
			pkg.Write(keyValuePair.Value.m_accumulator);
		}
	}

	public void Load(ZPackage pkg)
	{
		int num = pkg.ReadInt();
		this.m_skillData.Clear();
		int num2 = pkg.ReadInt();
		for (int i = 0; i < num2; i++)
		{
			Skills.SkillType skillType = (Skills.SkillType)pkg.ReadInt();
			float level = pkg.ReadSingle();
			float accumulator = (num >= 2) ? pkg.ReadSingle() : 0f;
			if (this.IsSkillValid(skillType))
			{
				Skills.Skill skill = this.GetSkill(skillType);
				skill.m_level = level;
				skill.m_accumulator = accumulator;
			}
		}
	}

	private bool IsSkillValid(Skills.SkillType type)
	{
		return Enum.IsDefined(typeof(Skills.SkillType), type);
	}

	public float GetSkillFactor(Skills.SkillType skillType)
	{
		if (skillType == Skills.SkillType.None)
		{
			return 0f;
		}
		return this.GetSkill(skillType).m_level / 100f;
	}

	public void GetRandomSkillRange(out float min, out float max, Skills.SkillType skillType)
	{
		float skillFactor = this.GetSkillFactor(skillType);
		float num = Mathf.Lerp(0.4f, 1f, skillFactor);
		min = Mathf.Clamp01(num - 0.15f);
		max = Mathf.Clamp01(num + 0.15f);
	}

	public float GetRandomSkillFactor(Skills.SkillType skillType)
	{
		float skillFactor = this.GetSkillFactor(skillType);
		float num = Mathf.Lerp(0.4f, 1f, skillFactor);
		float a = Mathf.Clamp01(num - 0.15f);
		float b = Mathf.Clamp01(num + 0.15f);
		return Mathf.Lerp(a, b, UnityEngine.Random.value);
	}

	public void CheatRaiseSkill(string name, float value)
	{
		foreach (object obj in Enum.GetValues(typeof(Skills.SkillType)))
		{
			Skills.SkillType skillType = (Skills.SkillType)obj;
			if (skillType.ToString().ToLower() == name)
			{
				Skills.Skill skill = this.GetSkill(skillType);
				skill.m_level += value;
				skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
				if (this.m_useSkillCap)
				{
					this.RebalanceSkills(skillType);
				}
				this.m_player.Message(MessageHud.MessageType.TopLeft, string.Concat(new object[]
				{
					"Skill incresed ",
					skill.m_info.m_skill.ToString(),
					": ",
					(int)skill.m_level
				}), 0, skill.m_info.m_icon);
				global::Console.instance.Print("Skill " + skillType.ToString() + " = " + skill.m_level.ToString());
				return;
			}
		}
		global::Console.instance.Print("Skill not found " + name);
	}

	public void CheatResetSkill(string name)
	{
		foreach (object obj in Enum.GetValues(typeof(Skills.SkillType)))
		{
			Skills.SkillType skillType = (Skills.SkillType)obj;
			if (skillType.ToString().ToLower() == name)
			{
				this.ResetSkill(skillType);
				global::Console.instance.Print("Skill " + skillType.ToString() + " reset");
				return;
			}
		}
		global::Console.instance.Print("Skill not found " + name);
	}

	public void ResetSkill(Skills.SkillType skillType)
	{
		this.m_skillData.Remove(skillType);
	}

	public void RaiseSkill(Skills.SkillType skillType, float factor = 1f)
	{
		if (skillType == Skills.SkillType.None)
		{
			return;
		}
		Skills.Skill skill = this.GetSkill(skillType);
		float level = skill.m_level;
		if (skill.Raise(factor))
		{
			if (this.m_useSkillCap)
			{
				this.RebalanceSkills(skillType);
			}
			this.m_player.OnSkillLevelup(skillType, skill.m_level);
			MessageHud.MessageType type = ((int)level == 0) ? MessageHud.MessageType.Center : MessageHud.MessageType.TopLeft;
			this.m_player.Message(type, string.Concat(new object[]
			{
				"$msg_skillup $skill_",
				skill.m_info.m_skill.ToString().ToLower(),
				": ",
				(int)skill.m_level
			}), 0, skill.m_info.m_icon);
			GoogleAnalyticsV4.instance.LogEvent("Game", "Levelup", skillType.ToString(), (long)((int)skill.m_level));
		}
	}

	private void RebalanceSkills(Skills.SkillType skillType)
	{
		if (this.GetTotalSkill() < this.m_totalSkillCap)
		{
			return;
		}
		float level = this.GetSkill(skillType).m_level;
		float num = this.m_totalSkillCap - level;
		float num2 = 0f;
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			if (keyValuePair.Key != skillType)
			{
				num2 += keyValuePair.Value.m_level;
			}
		}
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair2 in this.m_skillData)
		{
			if (keyValuePair2.Key != skillType)
			{
				keyValuePair2.Value.m_level = keyValuePair2.Value.m_level / num2 * num;
			}
		}
	}

	public void Clear()
	{
		this.m_skillData.Clear();
	}

	public void OnDeath()
	{
		this.LowerAllSkills(this.m_DeathLowerFactor);
	}

	public void LowerAllSkills(float factor)
	{
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			float num = keyValuePair.Value.m_level * factor;
			keyValuePair.Value.m_level -= num;
			keyValuePair.Value.m_accumulator = 0f;
		}
		this.m_player.Message(MessageHud.MessageType.TopLeft, "$msg_skills_lowered", 0, null);
	}

	private Skills.Skill GetSkill(Skills.SkillType skillType)
	{
		Skills.Skill skill;
		if (this.m_skillData.TryGetValue(skillType, out skill))
		{
			return skill;
		}
		skill = new Skills.Skill(this.GetSkillDef(skillType));
		this.m_skillData.Add(skillType, skill);
		return skill;
	}

	private Skills.SkillDef GetSkillDef(Skills.SkillType type)
	{
		foreach (Skills.SkillDef skillDef in this.m_skills)
		{
			if (skillDef.m_skill == type)
			{
				return skillDef;
			}
		}
		return null;
	}

	public List<Skills.Skill> GetSkillList()
	{
		List<Skills.Skill> list = new List<Skills.Skill>();
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			list.Add(keyValuePair.Value);
		}
		return list;
	}

	public float GetTotalSkill()
	{
		float num = 0f;
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			num += keyValuePair.Value.m_level;
		}
		return num;
	}

	public float GetTotalSkillCap()
	{
		return this.m_totalSkillCap;
	}

	private const int dataVersion = 2;

	private const float randomSkillRange = 0.15f;

	private const float randomSkillMin = 0.4f;

	public const float m_maxSkillLevel = 100f;

	public const float m_skillCurve = 2f;

	public bool m_useSkillCap;

	public float m_totalSkillCap = 600f;

	public List<Skills.SkillDef> m_skills = new List<Skills.SkillDef>();

	public float m_DeathLowerFactor = 0.25f;

	private Dictionary<Skills.SkillType, Skills.Skill> m_skillData = new Dictionary<Skills.SkillType, Skills.Skill>();

	private Player m_player;

	public enum SkillType
	{
		None,
		Swords,
		Knives,
		Clubs,
		Polearms,
		Spears,
		Blocking,
		Axes,
		Bows,
		FireMagic,
		FrostMagic,
		Unarmed,
		Pickaxes,
		WoodCutting,
		Jump = 100,
		Sneak,
		Run,
		Swim,
		All = 999
	}

	[Serializable]
	public class SkillDef
	{
		public Skills.SkillType m_skill = Skills.SkillType.Swords;

		public Sprite m_icon;

		public string m_description = "";

		public float m_increseStep = 1f;
	}

	public class Skill
	{
		public Skill(Skills.SkillDef info)
		{
			this.m_info = info;
		}

		public bool Raise(float factor)
		{
			if (this.m_level >= 100f)
			{
				return false;
			}
			float num = this.m_info.m_increseStep * factor;
			this.m_accumulator += num;
			float nextLevelRequirement = this.GetNextLevelRequirement();
			if (this.m_accumulator >= nextLevelRequirement)
			{
				this.m_level += 1f;
				this.m_level = Mathf.Clamp(this.m_level, 0f, 100f);
				this.m_accumulator = 0f;
				return true;
			}
			return false;
		}

		private float GetNextLevelRequirement()
		{
			return Mathf.Pow(this.m_level + 1f, 1.5f) * 0.5f + 0.5f;
		}

		public float GetLevelPercentage()
		{
			if (this.m_level >= 100f)
			{
				return 0f;
			}
			float nextLevelRequirement = this.GetNextLevelRequirement();
			return Mathf.Clamp01(this.m_accumulator / nextLevelRequirement);
		}

		public Skills.SkillDef m_info;

		public float m_level;

		public float m_accumulator;
	}
}
