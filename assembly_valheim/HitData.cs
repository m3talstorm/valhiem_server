using System;
using System.Collections.Generic;
using UnityEngine;

public class HitData
{
	public void Serialize(ref ZPackage pkg)
	{
		pkg.Write(this.m_damage.m_damage);
		pkg.Write(this.m_damage.m_blunt);
		pkg.Write(this.m_damage.m_slash);
		pkg.Write(this.m_damage.m_pierce);
		pkg.Write(this.m_damage.m_chop);
		pkg.Write(this.m_damage.m_pickaxe);
		pkg.Write(this.m_damage.m_fire);
		pkg.Write(this.m_damage.m_frost);
		pkg.Write(this.m_damage.m_lightning);
		pkg.Write(this.m_damage.m_poison);
		pkg.Write(this.m_damage.m_spirit);
		pkg.Write(this.m_toolTier);
		pkg.Write(this.m_pushForce);
		pkg.Write(this.m_backstabBonus);
		pkg.Write(this.m_staggerMultiplier);
		pkg.Write(this.m_dodgeable);
		pkg.Write(this.m_blockable);
		pkg.Write(this.m_point);
		pkg.Write(this.m_dir);
		pkg.Write(this.m_statusEffect);
		pkg.Write(this.m_attacker);
		pkg.Write((int)this.m_skill);
	}

	public void Deserialize(ref ZPackage pkg)
	{
		this.m_damage.m_damage = pkg.ReadSingle();
		this.m_damage.m_blunt = pkg.ReadSingle();
		this.m_damage.m_slash = pkg.ReadSingle();
		this.m_damage.m_pierce = pkg.ReadSingle();
		this.m_damage.m_chop = pkg.ReadSingle();
		this.m_damage.m_pickaxe = pkg.ReadSingle();
		this.m_damage.m_fire = pkg.ReadSingle();
		this.m_damage.m_frost = pkg.ReadSingle();
		this.m_damage.m_lightning = pkg.ReadSingle();
		this.m_damage.m_poison = pkg.ReadSingle();
		this.m_damage.m_spirit = pkg.ReadSingle();
		this.m_toolTier = pkg.ReadInt();
		this.m_pushForce = pkg.ReadSingle();
		this.m_backstabBonus = pkg.ReadSingle();
		this.m_staggerMultiplier = pkg.ReadSingle();
		this.m_dodgeable = pkg.ReadBool();
		this.m_blockable = pkg.ReadBool();
		this.m_point = pkg.ReadVector3();
		this.m_dir = pkg.ReadVector3();
		this.m_statusEffect = pkg.ReadString();
		this.m_attacker = pkg.ReadZDOID();
		this.m_skill = (Skills.SkillType)pkg.ReadInt();
	}

	public float GetTotalPhysicalDamage()
	{
		return this.m_damage.GetTotalPhysicalDamage();
	}

	public float GetTotalElementalDamage()
	{
		return this.m_damage.GetTotalElementalDamage();
	}

	public float GetTotalDamage()
	{
		return this.m_damage.GetTotalDamage();
	}

	private float ApplyModifier(float baseDamage, HitData.DamageModifier mod, ref float normalDmg, ref float resistantDmg, ref float weakDmg, ref float immuneDmg)
	{
		if (mod == HitData.DamageModifier.Ignore)
		{
			return 0f;
		}
		float num = baseDamage;
		switch (mod)
		{
		case HitData.DamageModifier.Resistant:
			num /= 2f;
			resistantDmg += baseDamage;
			return num;
		case HitData.DamageModifier.Weak:
			num *= 1.5f;
			weakDmg += baseDamage;
			return num;
		case HitData.DamageModifier.Immune:
			num = 0f;
			immuneDmg += baseDamage;
			return num;
		case HitData.DamageModifier.VeryResistant:
			num /= 4f;
			resistantDmg += baseDamage;
			return num;
		case HitData.DamageModifier.VeryWeak:
			num *= 2f;
			weakDmg += baseDamage;
			return num;
		}
		normalDmg += baseDamage;
		return num;
	}

	public void ApplyResistance(HitData.DamageModifiers modifiers, out HitData.DamageModifier significantModifier)
	{
		float damage = this.m_damage.m_damage;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		this.m_damage.m_blunt = this.ApplyModifier(this.m_damage.m_blunt, modifiers.m_blunt, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_slash = this.ApplyModifier(this.m_damage.m_slash, modifiers.m_slash, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_pierce = this.ApplyModifier(this.m_damage.m_pierce, modifiers.m_pierce, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_chop = this.ApplyModifier(this.m_damage.m_chop, modifiers.m_chop, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_pickaxe = this.ApplyModifier(this.m_damage.m_pickaxe, modifiers.m_pickaxe, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_fire = this.ApplyModifier(this.m_damage.m_fire, modifiers.m_fire, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_frost = this.ApplyModifier(this.m_damage.m_frost, modifiers.m_frost, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_lightning = this.ApplyModifier(this.m_damage.m_lightning, modifiers.m_lightning, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_poison = this.ApplyModifier(this.m_damage.m_poison, modifiers.m_poison, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_spirit = this.ApplyModifier(this.m_damage.m_spirit, modifiers.m_spirit, ref damage, ref num, ref num2, ref num3);
		significantModifier = HitData.DamageModifier.Immune;
		if (num3 >= num && num3 >= num2 && num3 >= damage)
		{
			significantModifier = HitData.DamageModifier.Immune;
		}
		if (damage >= num && damage >= num2 && damage >= num3)
		{
			significantModifier = HitData.DamageModifier.Normal;
		}
		if (num >= num2 && num >= num3 && num >= damage)
		{
			significantModifier = HitData.DamageModifier.Resistant;
		}
		if (num2 >= num && num2 >= num3 && num2 >= damage)
		{
			significantModifier = HitData.DamageModifier.Weak;
		}
	}

	public void ApplyArmor(float ac)
	{
		this.m_damage.ApplyArmor(ac);
	}

	public void ApplyModifier(float multiplier)
	{
		this.m_damage.m_blunt = this.m_damage.m_blunt * multiplier;
		this.m_damage.m_slash = this.m_damage.m_slash * multiplier;
		this.m_damage.m_pierce = this.m_damage.m_pierce * multiplier;
		this.m_damage.m_chop = this.m_damage.m_chop * multiplier;
		this.m_damage.m_pickaxe = this.m_damage.m_pickaxe * multiplier;
		this.m_damage.m_fire = this.m_damage.m_fire * multiplier;
		this.m_damage.m_frost = this.m_damage.m_frost * multiplier;
		this.m_damage.m_lightning = this.m_damage.m_lightning * multiplier;
		this.m_damage.m_poison = this.m_damage.m_poison * multiplier;
		this.m_damage.m_spirit = this.m_damage.m_spirit * multiplier;
	}

	public float GetTotalBlockableDamage()
	{
		return this.m_damage.m_blunt + this.m_damage.m_slash + this.m_damage.m_pierce + this.m_damage.m_fire + this.m_damage.m_frost + this.m_damage.m_lightning + this.m_damage.m_poison + this.m_damage.m_spirit;
	}

	public void BlockDamage(float damage)
	{
		float totalBlockableDamage = this.GetTotalBlockableDamage();
		float num = Mathf.Max(0f, totalBlockableDamage - damage);
		if (totalBlockableDamage <= 0f)
		{
			return;
		}
		float num2 = num / totalBlockableDamage;
		this.m_damage.m_blunt = this.m_damage.m_blunt * num2;
		this.m_damage.m_slash = this.m_damage.m_slash * num2;
		this.m_damage.m_pierce = this.m_damage.m_pierce * num2;
		this.m_damage.m_fire = this.m_damage.m_fire * num2;
		this.m_damage.m_frost = this.m_damage.m_frost * num2;
		this.m_damage.m_lightning = this.m_damage.m_lightning * num2;
		this.m_damage.m_poison = this.m_damage.m_poison * num2;
		this.m_damage.m_spirit = this.m_damage.m_spirit * num2;
	}

	public bool HaveAttacker()
	{
		return !this.m_attacker.IsNone();
	}

	public Character GetAttacker()
	{
		if (this.m_attacker.IsNone())
		{
			return null;
		}
		if (ZNetScene.instance == null)
		{
			return null;
		}
		GameObject gameObject = ZNetScene.instance.FindInstance(this.m_attacker);
		if (gameObject == null)
		{
			return null;
		}
		return gameObject.GetComponent<Character>();
	}

	public void SetAttacker(Character attacker)
	{
		if (attacker)
		{
			this.m_attacker = attacker.GetZDOID();
			return;
		}
		this.m_attacker = ZDOID.None;
	}

	public HitData.DamageTypes m_damage;

	public int m_toolTier;

	public bool m_dodgeable;

	public bool m_blockable;

	public float m_pushForce;

	public float m_backstabBonus = 1f;

	public float m_staggerMultiplier = 1f;

	public Vector3 m_point = Vector3.zero;

	public Vector3 m_dir = Vector3.zero;

	public string m_statusEffect = "";

	public ZDOID m_attacker = ZDOID.None;

	public Skills.SkillType m_skill;

	public Collider m_hitCollider;

	[Flags]
	public enum DamageType
	{
		Blunt = 1,
		Slash = 2,
		Pierce = 4,
		Chop = 8,
		Pickaxe = 16,
		Fire = 32,
		Frost = 64,
		Lightning = 128,
		Poison = 256,
		Spirit = 512,
		Physical = 31,
		Elemental = 224
	}

	public enum DamageModifier
	{
		Normal,
		Resistant,
		Weak,
		Immune,
		Ignore,
		VeryResistant,
		VeryWeak
	}

	[Serializable]
	public struct DamageModPair
	{
		public HitData.DamageType m_type;

		public HitData.DamageModifier m_modifier;
	}

	[Serializable]
	public struct DamageModifiers
	{
		public HitData.DamageModifiers Clone()
		{
			return (HitData.DamageModifiers)base.MemberwiseClone();
		}

		public void Apply(List<HitData.DamageModPair> modifiers)
		{
			foreach (HitData.DamageModPair damageModPair in modifiers)
			{
				HitData.DamageType type = damageModPair.m_type;
				if (type <= HitData.DamageType.Fire)
				{
					if (type <= HitData.DamageType.Chop)
					{
						switch (type)
						{
						case HitData.DamageType.Blunt:
							this.ApplyIfBetter(ref this.m_blunt, damageModPair.m_modifier);
							break;
						case HitData.DamageType.Slash:
							this.ApplyIfBetter(ref this.m_slash, damageModPair.m_modifier);
							break;
						case HitData.DamageType.Blunt | HitData.DamageType.Slash:
							break;
						case HitData.DamageType.Pierce:
							this.ApplyIfBetter(ref this.m_pierce, damageModPair.m_modifier);
							break;
						default:
							if (type == HitData.DamageType.Chop)
							{
								this.ApplyIfBetter(ref this.m_chop, damageModPair.m_modifier);
							}
							break;
						}
					}
					else if (type != HitData.DamageType.Pickaxe)
					{
						if (type == HitData.DamageType.Fire)
						{
							this.ApplyIfBetter(ref this.m_fire, damageModPair.m_modifier);
						}
					}
					else
					{
						this.ApplyIfBetter(ref this.m_pickaxe, damageModPair.m_modifier);
					}
				}
				else if (type <= HitData.DamageType.Lightning)
				{
					if (type != HitData.DamageType.Frost)
					{
						if (type == HitData.DamageType.Lightning)
						{
							this.ApplyIfBetter(ref this.m_lightning, damageModPair.m_modifier);
						}
					}
					else
					{
						this.ApplyIfBetter(ref this.m_frost, damageModPair.m_modifier);
					}
				}
				else if (type != HitData.DamageType.Poison)
				{
					if (type == HitData.DamageType.Spirit)
					{
						this.ApplyIfBetter(ref this.m_spirit, damageModPair.m_modifier);
					}
				}
				else
				{
					this.ApplyIfBetter(ref this.m_poison, damageModPair.m_modifier);
				}
			}
		}

		public HitData.DamageModifier GetModifier(HitData.DamageType type)
		{
			if (type <= HitData.DamageType.Fire)
			{
				if (type <= HitData.DamageType.Chop)
				{
					switch (type)
					{
					case HitData.DamageType.Blunt:
						return this.m_blunt;
					case HitData.DamageType.Slash:
						return this.m_slash;
					case HitData.DamageType.Blunt | HitData.DamageType.Slash:
						break;
					case HitData.DamageType.Pierce:
						return this.m_pierce;
					default:
						if (type == HitData.DamageType.Chop)
						{
							return this.m_chop;
						}
						break;
					}
				}
				else
				{
					if (type == HitData.DamageType.Pickaxe)
					{
						return this.m_pickaxe;
					}
					if (type == HitData.DamageType.Fire)
					{
						return this.m_fire;
					}
				}
			}
			else if (type <= HitData.DamageType.Lightning)
			{
				if (type == HitData.DamageType.Frost)
				{
					return this.m_frost;
				}
				if (type == HitData.DamageType.Lightning)
				{
					return this.m_lightning;
				}
			}
			else
			{
				if (type == HitData.DamageType.Poison)
				{
					return this.m_poison;
				}
				if (type == HitData.DamageType.Spirit)
				{
					return this.m_spirit;
				}
			}
			return HitData.DamageModifier.Normal;
		}

		private void ApplyIfBetter(ref HitData.DamageModifier original, HitData.DamageModifier mod)
		{
			if (this.ShouldOverride(original, mod))
			{
				original = mod;
			}
		}

		private bool ShouldOverride(HitData.DamageModifier a, HitData.DamageModifier b)
		{
			return a != HitData.DamageModifier.Ignore && (b == HitData.DamageModifier.Immune || ((a != HitData.DamageModifier.VeryResistant || b != HitData.DamageModifier.Resistant) && (a != HitData.DamageModifier.VeryWeak || b != HitData.DamageModifier.Weak)));
		}

		public void Print()
		{
			ZLog.Log("m_blunt " + this.m_blunt);
			ZLog.Log("m_slash " + this.m_slash);
			ZLog.Log("m_pierce " + this.m_pierce);
			ZLog.Log("m_chop " + this.m_chop);
			ZLog.Log("m_pickaxe " + this.m_pickaxe);
			ZLog.Log("m_fire " + this.m_fire);
			ZLog.Log("m_frost " + this.m_frost);
			ZLog.Log("m_lightning " + this.m_lightning);
			ZLog.Log("m_poison " + this.m_poison);
			ZLog.Log("m_spirit " + this.m_spirit);
		}

		public HitData.DamageModifier m_blunt;

		public HitData.DamageModifier m_slash;

		public HitData.DamageModifier m_pierce;

		public HitData.DamageModifier m_chop;

		public HitData.DamageModifier m_pickaxe;

		public HitData.DamageModifier m_fire;

		public HitData.DamageModifier m_frost;

		public HitData.DamageModifier m_lightning;

		public HitData.DamageModifier m_poison;

		public HitData.DamageModifier m_spirit;
	}

	[Serializable]
	public struct DamageTypes
	{
		public bool HaveDamage()
		{
			return this.m_damage > 0f || this.m_blunt > 0f || this.m_slash > 0f || this.m_pierce > 0f || this.m_chop > 0f || this.m_pickaxe > 0f || this.m_fire > 0f || this.m_frost > 0f || this.m_lightning > 0f || this.m_poison > 0f || this.m_spirit > 0f;
		}

		public float GetTotalPhysicalDamage()
		{
			return this.m_blunt + this.m_slash + this.m_pierce;
		}

		public float GetTotalElementalDamage()
		{
			return this.m_fire + this.m_frost + this.m_lightning;
		}

		public float GetTotalDamage()
		{
			return this.m_damage + this.m_blunt + this.m_slash + this.m_pierce + this.m_chop + this.m_pickaxe + this.m_fire + this.m_frost + this.m_lightning + this.m_poison + this.m_spirit;
		}

		public HitData.DamageTypes Clone()
		{
			return (HitData.DamageTypes)base.MemberwiseClone();
		}

		public void Add(HitData.DamageTypes other, int multiplier = 1)
		{
			this.m_damage += other.m_damage * (float)multiplier;
			this.m_blunt += other.m_blunt * (float)multiplier;
			this.m_slash += other.m_slash * (float)multiplier;
			this.m_pierce += other.m_pierce * (float)multiplier;
			this.m_chop += other.m_chop * (float)multiplier;
			this.m_pickaxe += other.m_pickaxe * (float)multiplier;
			this.m_fire += other.m_fire * (float)multiplier;
			this.m_frost += other.m_frost * (float)multiplier;
			this.m_lightning += other.m_lightning * (float)multiplier;
			this.m_poison += other.m_poison * (float)multiplier;
			this.m_spirit += other.m_spirit * (float)multiplier;
		}

		public void Modify(float multiplier)
		{
			this.m_damage *= multiplier;
			this.m_blunt *= multiplier;
			this.m_slash *= multiplier;
			this.m_pierce *= multiplier;
			this.m_chop *= multiplier;
			this.m_pickaxe *= multiplier;
			this.m_fire *= multiplier;
			this.m_frost *= multiplier;
			this.m_lightning *= multiplier;
			this.m_poison *= multiplier;
			this.m_spirit *= multiplier;
		}

		private float ApplyArmor(float dmg, float ac)
		{
			float result = Mathf.Clamp01(dmg / (ac * 4f)) * dmg;
			if (ac < dmg / 2f)
			{
				result = dmg - ac;
			}
			return result;
		}

		public void ApplyArmor(float ac)
		{
			if (ac <= 0f)
			{
				return;
			}
			float num = this.m_blunt + this.m_chop + this.m_pickaxe + this.m_slash + this.m_pierce + this.m_fire + this.m_frost + this.m_lightning + this.m_spirit;
			if (num <= 0f)
			{
				return;
			}
			float num2 = this.ApplyArmor(num, ac) / num;
			this.m_blunt *= num2;
			this.m_chop *= num2;
			this.m_pickaxe *= num2;
			this.m_slash *= num2;
			this.m_pierce *= num2;
			this.m_fire *= num2;
			this.m_frost *= num2;
			this.m_lightning *= num2;
			this.m_spirit *= num2;
		}

		private string DamageRange(float damage, float minFactor, float maxFactor)
		{
			int num = Mathf.RoundToInt(damage * minFactor);
			int num2 = Mathf.RoundToInt(damage * maxFactor);
			return string.Concat(new object[]
			{
				"<color=orange>",
				Mathf.RoundToInt(damage),
				"</color> <color=yellow>(",
				num.ToString(),
				"-",
				num2.ToString(),
				") </color>"
			});
		}

		public string GetTooltipString(Skills.SkillType skillType = Skills.SkillType.None)
		{
			if (Player.m_localPlayer == null)
			{
				return "";
			}
			float minFactor;
			float maxFactor;
			Player.m_localPlayer.GetSkills().GetRandomSkillRange(out minFactor, out maxFactor, skillType);
			string text = "";
			if (this.m_damage != 0f)
			{
				text = text + "\n$inventory_damage: " + this.DamageRange(this.m_damage, minFactor, maxFactor);
			}
			if (this.m_blunt != 0f)
			{
				text = text + "\n$inventory_blunt: " + this.DamageRange(this.m_blunt, minFactor, maxFactor);
			}
			if (this.m_slash != 0f)
			{
				text = text + "\n$inventory_slash: " + this.DamageRange(this.m_slash, minFactor, maxFactor);
			}
			if (this.m_pierce != 0f)
			{
				text = text + "\n$inventory_pierce: " + this.DamageRange(this.m_pierce, minFactor, maxFactor);
			}
			if (this.m_fire != 0f)
			{
				text = text + "\n$inventory_fire: " + this.DamageRange(this.m_fire, minFactor, maxFactor);
			}
			if (this.m_frost != 0f)
			{
				text = text + "\n$inventory_frost: " + this.DamageRange(this.m_frost, minFactor, maxFactor);
			}
			if (this.m_lightning != 0f)
			{
				text = text + "\n$inventory_lightning: " + this.DamageRange(this.m_lightning, minFactor, maxFactor);
			}
			if (this.m_poison != 0f)
			{
				text = text + "\n$inventory_poison: " + this.DamageRange(this.m_poison, minFactor, maxFactor);
			}
			if (this.m_spirit != 0f)
			{
				text = text + "\n$inventory_spirit: " + this.DamageRange(this.m_spirit, minFactor, maxFactor);
			}
			return text;
		}

		public string GetTooltipString()
		{
			string text = "";
			if (this.m_damage != 0f)
			{
				text = text + "\n$inventory_damage: <color=yellow>" + this.m_damage.ToString() + "</color>";
			}
			if (this.m_blunt != 0f)
			{
				text = text + "\n$inventory_blunt: <color=yellow>" + this.m_blunt.ToString() + "</color>";
			}
			if (this.m_slash != 0f)
			{
				text = text + "\n$inventory_slash: <color=yellow>" + this.m_slash.ToString() + "</color>";
			}
			if (this.m_pierce != 0f)
			{
				text = text + "\n$inventory_pierce: <color=yellow>" + this.m_pierce.ToString() + "</color>";
			}
			if (this.m_fire != 0f)
			{
				text = text + "\n$inventory_fire: <color=yellow>" + this.m_fire.ToString() + "</color>";
			}
			if (this.m_frost != 0f)
			{
				text = text + "\n$inventory_frost: <color=yellow>" + this.m_frost.ToString() + "</color>";
			}
			if (this.m_lightning != 0f)
			{
				text = text + "\n$inventory_lightning: <color=yellow>" + this.m_frost.ToString() + "</color>";
			}
			if (this.m_poison != 0f)
			{
				text = text + "\n$inventory_poison: <color=yellow>" + this.m_poison.ToString() + "</color>";
			}
			if (this.m_spirit != 0f)
			{
				text = text + "\n$inventory_spirit: <color=yellow>" + this.m_spirit.ToString() + "</color>";
			}
			return text;
		}

		public float m_damage;

		public float m_blunt;

		public float m_slash;

		public float m_pierce;

		public float m_chop;

		public float m_pickaxe;

		public float m_fire;

		public float m_frost;

		public float m_lightning;

		public float m_poison;

		public float m_spirit;
	}
}
