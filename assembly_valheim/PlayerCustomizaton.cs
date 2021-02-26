using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCustomizaton : MonoBehaviour
{
	private void OnEnable()
	{
		this.m_maleToggle.isOn = true;
		this.m_femaleToggle.isOn = false;
		this.m_beardPanel.gameObject.SetActive(true);
		this.m_beards = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Beard");
		this.m_hairs = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Hair");
		this.m_beards.Sort((ItemDrop x, ItemDrop y) => Localization.instance.Localize(x.m_itemData.m_shared.m_name).CompareTo(Localization.instance.Localize(y.m_itemData.m_shared.m_name)));
		this.m_hairs.Sort((ItemDrop x, ItemDrop y) => Localization.instance.Localize(x.m_itemData.m_shared.m_name).CompareTo(Localization.instance.Localize(y.m_itemData.m_shared.m_name)));
		this.m_beards.Remove(this.m_noBeard);
		this.m_beards.Insert(0, this.m_noBeard);
		this.m_hairs.Remove(this.m_noHair);
		this.m_hairs.Insert(0, this.m_noHair);
	}

	private void Update()
	{
		if (this.GetPlayer() == null)
		{
			return;
		}
		this.m_selectedHair.text = Localization.instance.Localize(this.GetHair());
		this.m_selectedBeard.text = Localization.instance.Localize(this.GetBeard());
		Color c = Color.Lerp(this.m_skinColor0, this.m_skinColor1, this.m_skinHue.value);
		this.GetPlayer().SetSkinColor(Utils.ColorToVec3(c));
		Color c2 = Color.Lerp(this.m_hairColor0, this.m_hairColor1, this.m_hairTone.value) * Mathf.Lerp(this.m_hairMinLevel, this.m_hairMaxLevel, this.m_hairLevel.value);
		this.GetPlayer().SetHairColor(Utils.ColorToVec3(c2));
	}

	private Player GetPlayer()
	{
		return base.GetComponentInParent<FejdStartup>().GetPreviewPlayer();
	}

	public void OnHairHueChange(float v)
	{
	}

	public void OnSkinHueChange(float v)
	{
	}

	public void SetPlayerModel(int index)
	{
		this.GetPlayer().SetPlayerModel(index);
		if (index == 1)
		{
			this.ResetBeard();
		}
	}

	public void OnHairLeft()
	{
		this.SetHair(this.GetHairIndex() - 1);
	}

	public void OnHairRight()
	{
		this.SetHair(this.GetHairIndex() + 1);
	}

	public void OnBeardLeft()
	{
		if (this.GetPlayer().GetPlayerModel() == 1)
		{
			return;
		}
		this.SetBeard(this.GetBeardIndex() - 1);
	}

	public void OnBeardRight()
	{
		if (this.GetPlayer().GetPlayerModel() == 1)
		{
			return;
		}
		this.SetBeard(this.GetBeardIndex() + 1);
	}

	private void ResetBeard()
	{
		this.GetPlayer().SetBeard(this.m_noBeard.gameObject.name);
	}

	private void SetBeard(int index)
	{
		if (index < 0 || index >= this.m_beards.Count)
		{
			return;
		}
		this.GetPlayer().SetBeard(this.m_beards[index].gameObject.name);
	}

	private void SetHair(int index)
	{
		ZLog.Log("Set hair " + index);
		if (index < 0 || index >= this.m_hairs.Count)
		{
			return;
		}
		this.GetPlayer().SetHair(this.m_hairs[index].gameObject.name);
	}

	private int GetBeardIndex()
	{
		string beard = this.GetPlayer().GetBeard();
		for (int i = 0; i < this.m_beards.Count; i++)
		{
			if (this.m_beards[i].gameObject.name == beard)
			{
				return i;
			}
		}
		return 0;
	}

	private int GetHairIndex()
	{
		string hair = this.GetPlayer().GetHair();
		for (int i = 0; i < this.m_hairs.Count; i++)
		{
			if (this.m_hairs[i].gameObject.name == hair)
			{
				return i;
			}
		}
		return 0;
	}

	private string GetHair()
	{
		return this.m_hairs[this.GetHairIndex()].m_itemData.m_shared.m_name;
	}

	private string GetBeard()
	{
		return this.m_beards[this.GetBeardIndex()].m_itemData.m_shared.m_name;
	}

	public Color m_skinColor0 = Color.white;

	public Color m_skinColor1 = Color.white;

	public Color m_hairColor0 = Color.white;

	public Color m_hairColor1 = Color.white;

	public float m_hairMaxLevel = 1f;

	public float m_hairMinLevel = 0.1f;

	public Text m_selectedBeard;

	public Text m_selectedHair;

	public Slider m_skinHue;

	public Slider m_hairLevel;

	public Slider m_hairTone;

	public RectTransform m_beardPanel;

	public Toggle m_maleToggle;

	public Toggle m_femaleToggle;

	public ItemDrop m_noHair;

	public ItemDrop m_noBeard;

	private List<ItemDrop> m_beards;

	private List<ItemDrop> m_hairs;
}
