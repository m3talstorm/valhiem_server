using System;
using System.Collections.Generic;
using UnityEngine;

public class VisEquipment : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		Transform transform = base.transform.Find("Visual");
		if (transform == null)
		{
			transform = base.transform;
		}
		this.m_visual = transform.gameObject;
		this.m_lodGroup = this.m_visual.GetComponentInChildren<LODGroup>();
		if (this.m_bodyModel != null && this.m_bodyModel.material.HasProperty("_ChestTex"))
		{
			this.m_emptyBodyTexture = this.m_bodyModel.material.GetTexture("_ChestTex");
		}
	}

	private void Start()
	{
		this.UpdateVisuals();
	}

	public void SetWeaponTrails(bool enabled)
	{
		if (this.m_useAllTrails)
		{
			MeleeWeaponTrail[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeleeWeaponTrail>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Emit = enabled;
			}
			return;
		}
		if (this.m_rightItemInstance)
		{
			MeleeWeaponTrail[] componentsInChildren = this.m_rightItemInstance.GetComponentsInChildren<MeleeWeaponTrail>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Emit = enabled;
			}
		}
	}

	public void SetModel(int index)
	{
		if (this.m_modelIndex == index)
		{
			return;
		}
		if (index < 0 || index >= this.m_models.Length)
		{
			return;
		}
		ZLog.Log("Vis equip model set to " + index);
		this.m_modelIndex = index;
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("ModelIndex", this.m_modelIndex);
		}
	}

	public void SetSkinColor(Vector3 color)
	{
		if (color == this.m_skinColor)
		{
			return;
		}
		this.m_skinColor = color;
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("SkinColor", this.m_skinColor);
		}
	}

	public void SetHairColor(Vector3 color)
	{
		if (this.m_hairColor == color)
		{
			return;
		}
		this.m_hairColor = color;
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("HairColor", this.m_hairColor);
		}
	}

	public void SetLeftItem(string name, int variant)
	{
		if (this.m_leftItem == name && this.m_leftItemVariant == variant)
		{
			return;
		}
		this.m_leftItem = name;
		this.m_leftItemVariant = variant;
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("LeftItem", string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
			this.m_nview.GetZDO().Set("LeftItemVariant", variant);
		}
	}

	public void SetRightItem(string name)
	{
		if (this.m_rightItem == name)
		{
			return;
		}
		this.m_rightItem = name;
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("RightItem", string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
		}
	}

	public void SetLeftBackItem(string name, int variant)
	{
		if (this.m_leftBackItem == name && this.m_leftBackItemVariant == variant)
		{
			return;
		}
		this.m_leftBackItem = name;
		this.m_leftBackItemVariant = variant;
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("LeftBackItem", string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
			this.m_nview.GetZDO().Set("LeftBackItemVariant", variant);
		}
	}

	public void SetRightBackItem(string name)
	{
		if (this.m_rightBackItem == name)
		{
			return;
		}
		this.m_rightBackItem = name;
		ZLog.Log("Right back item " + name);
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("RightBackItem", string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
		}
	}

	public void SetChestItem(string name)
	{
		if (this.m_chestItem == name)
		{
			return;
		}
		this.m_chestItem = name;
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("ChestItem", string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
		}
	}

	public void SetLegItem(string name)
	{
		if (this.m_legItem == name)
		{
			return;
		}
		this.m_legItem = name;
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("LegItem", string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
		}
	}

	public void SetHelmetItem(string name)
	{
		if (this.m_helmetItem == name)
		{
			return;
		}
		this.m_helmetItem = name;
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("HelmetItem", string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
		}
	}

	public void SetShoulderItem(string name, int variant)
	{
		if (this.m_shoulderItem == name && this.m_shoulderItemVariant == variant)
		{
			return;
		}
		this.m_shoulderItem = name;
		this.m_shoulderItemVariant = variant;
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("ShoulderItem", string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
			this.m_nview.GetZDO().Set("ShoulderItemVariant", variant);
		}
	}

	public void SetBeardItem(string name)
	{
		if (this.m_beardItem == name)
		{
			return;
		}
		this.m_beardItem = name;
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("BeardItem", string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
		}
	}

	public void SetHairItem(string name)
	{
		if (this.m_hairItem == name)
		{
			return;
		}
		this.m_hairItem = name;
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("HairItem", string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
		}
	}

	public void SetUtilityItem(string name)
	{
		if (this.m_utilityItem == name)
		{
			return;
		}
		this.m_utilityItem = name;
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set("UtilityItem", string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
		}
	}

	private void Update()
	{
		this.UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		this.UpdateEquipmentVisuals();
		if (this.m_isPlayer)
		{
			this.UpdateBaseModel();
			this.UpdateColors();
		}
	}

	private void UpdateColors()
	{
		Color value = Utils.Vec3ToColor(this.m_skinColor);
		Color value2 = Utils.Vec3ToColor(this.m_hairColor);
		if (this.m_nview.GetZDO() != null)
		{
			value = Utils.Vec3ToColor(this.m_nview.GetZDO().GetVec3("SkinColor", Vector3.one));
			value2 = Utils.Vec3ToColor(this.m_nview.GetZDO().GetVec3("HairColor", Vector3.one));
		}
		this.m_bodyModel.materials[0].SetColor("_SkinColor", value);
		this.m_bodyModel.materials[1].SetColor("_SkinColor", value2);
		if (this.m_beardItemInstance)
		{
			Renderer[] componentsInChildren = this.m_beardItemInstance.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].material.SetColor("_SkinColor", value2);
			}
		}
		if (this.m_hairItemInstance)
		{
			Renderer[] componentsInChildren = this.m_hairItemInstance.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].material.SetColor("_SkinColor", value2);
			}
		}
	}

	private void UpdateBaseModel()
	{
		if (this.m_models.Length == 0)
		{
			return;
		}
		int num = this.m_modelIndex;
		if (this.m_nview.GetZDO() != null)
		{
			num = this.m_nview.GetZDO().GetInt("ModelIndex", 0);
		}
		if (this.m_currentModelIndex != num || this.m_bodyModel.sharedMesh != this.m_models[num].m_mesh)
		{
			this.m_currentModelIndex = num;
			this.m_bodyModel.sharedMesh = this.m_models[num].m_mesh;
			this.m_bodyModel.materials[0].SetTexture("_MainTex", this.m_models[num].m_baseMaterial.GetTexture("_MainTex"));
			this.m_bodyModel.materials[0].SetTexture("_SkinBumpMap", this.m_models[num].m_baseMaterial.GetTexture("_SkinBumpMap"));
		}
	}

	private void UpdateEquipmentVisuals()
	{
		int hash = 0;
		int rightHandEquiped = 0;
		int chestEquiped = 0;
		int legEquiped = 0;
		int hash2 = 0;
		int beardEquiped = 0;
		int num = 0;
		int hash3 = 0;
		int utilityEquiped = 0;
		int leftItem = 0;
		int rightItem = 0;
		int variant = this.m_shoulderItemVariant;
		int variant2 = this.m_leftItemVariant;
		int leftVariant = this.m_leftBackItemVariant;
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo != null)
		{
			hash = zdo.GetInt("LeftItem", 0);
			rightHandEquiped = zdo.GetInt("RightItem", 0);
			chestEquiped = zdo.GetInt("ChestItem", 0);
			legEquiped = zdo.GetInt("LegItem", 0);
			hash2 = zdo.GetInt("HelmetItem", 0);
			hash3 = zdo.GetInt("ShoulderItem", 0);
			utilityEquiped = zdo.GetInt("UtilityItem", 0);
			if (this.m_isPlayer)
			{
				beardEquiped = zdo.GetInt("BeardItem", 0);
				num = zdo.GetInt("HairItem", 0);
				leftItem = zdo.GetInt("LeftBackItem", 0);
				rightItem = zdo.GetInt("RightBackItem", 0);
				variant = zdo.GetInt("ShoulderItemVariant", 0);
				variant2 = zdo.GetInt("LeftItemVariant", 0);
				leftVariant = zdo.GetInt("LeftBackItemVariant", 0);
			}
		}
		else
		{
			if (!string.IsNullOrEmpty(this.m_leftItem))
			{
				hash = this.m_leftItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_rightItem))
			{
				rightHandEquiped = this.m_rightItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_chestItem))
			{
				chestEquiped = this.m_chestItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_legItem))
			{
				legEquiped = this.m_legItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_helmetItem))
			{
				hash2 = this.m_helmetItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_shoulderItem))
			{
				hash3 = this.m_shoulderItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(this.m_utilityItem))
			{
				utilityEquiped = this.m_utilityItem.GetStableHashCode();
			}
			if (this.m_isPlayer)
			{
				if (!string.IsNullOrEmpty(this.m_beardItem))
				{
					beardEquiped = this.m_beardItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(this.m_hairItem))
				{
					num = this.m_hairItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(this.m_leftBackItem))
				{
					leftItem = this.m_leftBackItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(this.m_rightBackItem))
				{
					rightItem = this.m_rightBackItem.GetStableHashCode();
				}
			}
		}
		bool flag = false;
		flag = (this.SetRightHandEquiped(rightHandEquiped) || flag);
		flag = (this.SetLeftHandEquiped(hash, variant2) || flag);
		flag = (this.SetChestEquiped(chestEquiped) || flag);
		flag = (this.SetLegEquiped(legEquiped) || flag);
		flag = (this.SetHelmetEquiped(hash2, num) || flag);
		flag = (this.SetShoulderEquiped(hash3, variant) || flag);
		flag = (this.SetUtilityEquiped(utilityEquiped) || flag);
		if (this.m_isPlayer)
		{
			flag = (this.SetBeardEquiped(beardEquiped) || flag);
			flag = (this.SetBackEquiped(leftItem, rightItem, leftVariant) || flag);
			if (this.m_helmetHideHair)
			{
				num = 0;
			}
			flag = (this.SetHairEquiped(num) || flag);
		}
		if (flag)
		{
			this.UpdateLodgroup();
		}
	}

	protected void UpdateLodgroup()
	{
		if (this.m_lodGroup == null)
		{
			return;
		}
		Renderer[] componentsInChildren = this.m_visual.GetComponentsInChildren<Renderer>();
		LOD[] lods = this.m_lodGroup.GetLODs();
		lods[0].renderers = componentsInChildren;
		this.m_lodGroup.SetLODs(lods);
	}

	private bool SetRightHandEquiped(int hash)
	{
		if (this.m_currentRightItemHash == hash)
		{
			return false;
		}
		if (this.m_rightItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_rightItemInstance);
			this.m_rightItemInstance = null;
		}
		this.m_currentRightItemHash = hash;
		if (hash != 0)
		{
			this.m_rightItemInstance = this.AttachItem(hash, 0, this.m_rightHand, true);
		}
		return true;
	}

	private bool SetLeftHandEquiped(int hash, int variant)
	{
		if (this.m_currentLeftItemHash == hash && this.m_currentLeftItemVariant == variant)
		{
			return false;
		}
		if (this.m_leftItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_leftItemInstance);
			this.m_leftItemInstance = null;
		}
		this.m_currentLeftItemHash = hash;
		this.m_currentLeftItemVariant = variant;
		if (hash != 0)
		{
			this.m_leftItemInstance = this.AttachItem(hash, variant, this.m_leftHand, true);
		}
		return true;
	}

	private bool SetBackEquiped(int leftItem, int rightItem, int leftVariant)
	{
		if (this.m_currentLeftBackItemHash == leftItem && this.m_currentRightBackItemHash == rightItem && this.m_currentLeftBackItemVariant == leftVariant)
		{
			return false;
		}
		if (this.m_leftBackItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_leftBackItemInstance);
			this.m_leftBackItemInstance = null;
		}
		if (this.m_rightBackItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_rightBackItemInstance);
			this.m_rightBackItemInstance = null;
		}
		this.m_currentLeftBackItemHash = leftItem;
		this.m_currentRightBackItemHash = rightItem;
		this.m_currentLeftBackItemVariant = leftVariant;
		if (this.m_currentLeftBackItemHash != 0)
		{
			this.m_leftBackItemInstance = this.AttachBackItem(leftItem, leftVariant, false);
		}
		if (this.m_currentRightBackItemHash != 0)
		{
			this.m_rightBackItemInstance = this.AttachBackItem(rightItem, 0, true);
		}
		return true;
	}

	private GameObject AttachBackItem(int hash, int variant, bool rightHand)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing back attach item prefab: " + hash);
			return null;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		ItemDrop.ItemData.ItemType itemType = (component.m_itemData.m_shared.m_attachOverride != ItemDrop.ItemData.ItemType.None) ? component.m_itemData.m_shared.m_attachOverride : component.m_itemData.m_shared.m_itemType;
		if (itemType != ItemDrop.ItemData.ItemType.Torch)
		{
			if (itemType <= ItemDrop.ItemData.ItemType.TwoHandedWeapon)
			{
				switch (itemType)
				{
				case ItemDrop.ItemData.ItemType.OneHandedWeapon:
					return this.AttachItem(hash, variant, this.m_backMelee, true);
				case ItemDrop.ItemData.ItemType.Bow:
					return this.AttachItem(hash, variant, this.m_backBow, true);
				case ItemDrop.ItemData.ItemType.Shield:
					return this.AttachItem(hash, variant, this.m_backShield, true);
				default:
					if (itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon)
					{
						return this.AttachItem(hash, variant, this.m_backTwohandedMelee, true);
					}
					break;
				}
			}
			else
			{
				if (itemType == ItemDrop.ItemData.ItemType.Tool)
				{
					return this.AttachItem(hash, variant, this.m_backTool, true);
				}
				if (itemType == ItemDrop.ItemData.ItemType.Attach_Atgeir)
				{
					return this.AttachItem(hash, variant, this.m_backAtgeir, true);
				}
			}
			return null;
		}
		if (rightHand)
		{
			return this.AttachItem(hash, variant, this.m_backMelee, false);
		}
		return this.AttachItem(hash, variant, this.m_backTool, false);
	}

	private bool SetChestEquiped(int hash)
	{
		if (this.m_currentChestItemHash == hash)
		{
			return false;
		}
		this.m_currentChestItemHash = hash;
		if (this.m_bodyModel == null)
		{
			return true;
		}
		if (this.m_chestItemInstances != null)
		{
			foreach (GameObject gameObject in this.m_chestItemInstances)
			{
				if (this.m_lodGroup)
				{
					Utils.RemoveFromLodgroup(this.m_lodGroup, gameObject);
				}
				UnityEngine.Object.Destroy(gameObject);
			}
			this.m_chestItemInstances = null;
			this.m_bodyModel.material.SetTexture("_ChestTex", this.m_emptyBodyTexture);
			this.m_bodyModel.material.SetTexture("_ChestBumpMap", null);
			this.m_bodyModel.material.SetTexture("_ChestMetal", null);
		}
		if (this.m_currentChestItemHash == 0)
		{
			return true;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing chest item " + hash);
			return true;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if (component.m_itemData.m_shared.m_armorMaterial)
		{
			this.m_bodyModel.material.SetTexture("_ChestTex", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestTex"));
			this.m_bodyModel.material.SetTexture("_ChestBumpMap", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestBumpMap"));
			this.m_bodyModel.material.SetTexture("_ChestMetal", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestMetal"));
		}
		this.m_chestItemInstances = this.AttachArmor(hash, -1);
		return true;
	}

	private bool SetShoulderEquiped(int hash, int variant)
	{
		if (this.m_currentShoulderItemHash == hash && this.m_currenShoulderItemVariant == variant)
		{
			return false;
		}
		this.m_currentShoulderItemHash = hash;
		this.m_currenShoulderItemVariant = variant;
		if (this.m_bodyModel == null)
		{
			return true;
		}
		if (this.m_shoulderItemInstances != null)
		{
			foreach (GameObject gameObject in this.m_shoulderItemInstances)
			{
				if (this.m_lodGroup)
				{
					Utils.RemoveFromLodgroup(this.m_lodGroup, gameObject);
				}
				UnityEngine.Object.Destroy(gameObject);
			}
			this.m_shoulderItemInstances = null;
		}
		if (this.m_currentShoulderItemHash == 0)
		{
			return true;
		}
		if (ObjectDB.instance.GetItemPrefab(hash) == null)
		{
			ZLog.Log("Missing shoulder item " + hash);
			return true;
		}
		this.m_shoulderItemInstances = this.AttachArmor(hash, variant);
		return true;
	}

	private bool SetLegEquiped(int hash)
	{
		if (this.m_currentLegItemHash == hash)
		{
			return false;
		}
		this.m_currentLegItemHash = hash;
		if (this.m_bodyModel == null)
		{
			return true;
		}
		if (this.m_legItemInstances != null)
		{
			foreach (GameObject obj in this.m_legItemInstances)
			{
				UnityEngine.Object.Destroy(obj);
			}
			this.m_legItemInstances = null;
			this.m_bodyModel.material.SetTexture("_LegsTex", this.m_emptyBodyTexture);
			this.m_bodyModel.material.SetTexture("_LegsBumpMap", null);
			this.m_bodyModel.material.SetTexture("_LegsMetal", null);
		}
		if (this.m_currentLegItemHash == 0)
		{
			return true;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing legs item " + hash);
			return true;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if (component.m_itemData.m_shared.m_armorMaterial)
		{
			this.m_bodyModel.material.SetTexture("_LegsTex", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsTex"));
			this.m_bodyModel.material.SetTexture("_LegsBumpMap", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsBumpMap"));
			this.m_bodyModel.material.SetTexture("_LegsMetal", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsMetal"));
		}
		this.m_legItemInstances = this.AttachArmor(hash, -1);
		return true;
	}

	private bool SetBeardEquiped(int hash)
	{
		if (this.m_currentBeardItemHash == hash)
		{
			return false;
		}
		if (this.m_beardItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_beardItemInstance);
			this.m_beardItemInstance = null;
		}
		this.m_currentBeardItemHash = hash;
		if (hash != 0)
		{
			this.m_beardItemInstance = this.AttachItem(hash, 0, this.m_helmet, true);
		}
		return true;
	}

	private bool SetHairEquiped(int hash)
	{
		if (this.m_currentHairItemHash == hash)
		{
			return false;
		}
		if (this.m_hairItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_hairItemInstance);
			this.m_hairItemInstance = null;
		}
		this.m_currentHairItemHash = hash;
		if (hash != 0)
		{
			this.m_hairItemInstance = this.AttachItem(hash, 0, this.m_helmet, true);
		}
		return true;
	}

	private bool SetHelmetEquiped(int hash, int hairHash)
	{
		if (this.m_currentHelmetItemHash == hash)
		{
			return false;
		}
		if (this.m_helmetItemInstance)
		{
			UnityEngine.Object.Destroy(this.m_helmetItemInstance);
			this.m_helmetItemInstance = null;
		}
		this.m_currentHelmetItemHash = hash;
		this.m_helmetHideHair = this.HelmetHidesHair(hash);
		if (hash != 0)
		{
			this.m_helmetItemInstance = this.AttachItem(hash, 0, this.m_helmet, true);
		}
		return true;
	}

	private bool SetUtilityEquiped(int hash)
	{
		if (this.m_currentUtilityItemHash == hash)
		{
			return false;
		}
		if (this.m_utilityItemInstances != null)
		{
			foreach (GameObject gameObject in this.m_utilityItemInstances)
			{
				if (this.m_lodGroup)
				{
					Utils.RemoveFromLodgroup(this.m_lodGroup, gameObject);
				}
				UnityEngine.Object.Destroy(gameObject);
			}
			this.m_utilityItemInstances = null;
		}
		this.m_currentUtilityItemHash = hash;
		if (hash != 0)
		{
			this.m_utilityItemInstances = this.AttachArmor(hash, -1);
		}
		return true;
	}

	private bool HelmetHidesHair(int itemHash)
	{
		if (itemHash == 0)
		{
			return false;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		return !(itemPrefab == null) && itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_helmetHideHair;
	}

	private List<GameObject> AttachArmor(int itemHash, int variant = -1)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab == null)
		{
			ZLog.Log(string.Concat(new object[]
			{
				"Missing attach item: ",
				itemHash,
				"  ob:",
				base.gameObject.name
			}));
			return null;
		}
		List<GameObject> list = new List<GameObject>();
		int childCount = itemPrefab.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = itemPrefab.transform.GetChild(i);
			if (child.gameObject.name.StartsWith("attach_"))
			{
				string text = child.gameObject.name.Substring(7);
				GameObject gameObject;
				if (text == "skin")
				{
					gameObject = UnityEngine.Object.Instantiate<GameObject>(child.gameObject, this.m_bodyModel.transform.position, this.m_bodyModel.transform.parent.rotation, this.m_bodyModel.transform.parent);
					gameObject.SetActive(true);
					foreach (SkinnedMeshRenderer skinnedMeshRenderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
					{
						skinnedMeshRenderer.rootBone = this.m_bodyModel.rootBone;
						skinnedMeshRenderer.bones = this.m_bodyModel.bones;
					}
					foreach (Cloth cloth in gameObject.GetComponentsInChildren<Cloth>())
					{
						if (this.m_clothColliders.Length != 0)
						{
							if (cloth.capsuleColliders.Length != 0)
							{
								List<CapsuleCollider> list2 = new List<CapsuleCollider>(this.m_clothColliders);
								list2.AddRange(cloth.capsuleColliders);
								cloth.capsuleColliders = list2.ToArray();
							}
							else
							{
								cloth.capsuleColliders = this.m_clothColliders;
							}
						}
					}
				}
				else
				{
					Transform transform = Utils.FindChild(this.m_visual.transform, text);
					if (transform == null)
					{
						ZLog.LogWarning("Missing joint " + text + " in item " + itemPrefab.name);
						goto IL_268;
					}
					gameObject = UnityEngine.Object.Instantiate<GameObject>(child.gameObject);
					gameObject.SetActive(true);
					gameObject.transform.SetParent(transform);
					gameObject.transform.localPosition = Vector3.zero;
					gameObject.transform.localRotation = Quaternion.identity;
				}
				if (variant >= 0)
				{
					IEquipmentVisual componentInChildren = gameObject.GetComponentInChildren<IEquipmentVisual>();
					if (componentInChildren != null)
					{
						componentInChildren.Setup(variant);
					}
				}
				this.CleanupInstance(gameObject);
				this.EnableEquipedEffects(gameObject);
				list.Add(gameObject);
			}
			IL_268:;
		}
		return list;
	}

	protected GameObject AttachItem(int itemHash, int variant, Transform joint, bool enableEquipEffects = true)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab == null)
		{
			ZLog.Log(string.Concat(new object[]
			{
				"Missing attach item: ",
				itemHash,
				"  ob:",
				base.gameObject.name,
				"  joint:",
				joint ? joint.name : "none"
			}));
			return null;
		}
		GameObject gameObject = null;
		int childCount = itemPrefab.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = itemPrefab.transform.GetChild(i);
			if (child.gameObject.name == "attach" || child.gameObject.name == "attach_skin")
			{
				gameObject = child.gameObject;
				break;
			}
		}
		if (gameObject == null)
		{
			return null;
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject);
		gameObject2.SetActive(true);
		this.CleanupInstance(gameObject2);
		if (enableEquipEffects)
		{
			this.EnableEquipedEffects(gameObject2);
		}
		if (gameObject.name == "attach_skin")
		{
			gameObject2.transform.SetParent(this.m_bodyModel.transform.parent);
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
			foreach (SkinnedMeshRenderer skinnedMeshRenderer in gameObject2.GetComponentsInChildren<SkinnedMeshRenderer>())
			{
				skinnedMeshRenderer.rootBone = this.m_bodyModel.rootBone;
				skinnedMeshRenderer.bones = this.m_bodyModel.bones;
			}
		}
		else
		{
			gameObject2.transform.SetParent(joint);
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
		}
		IEquipmentVisual componentInChildren = gameObject2.GetComponentInChildren<IEquipmentVisual>();
		if (componentInChildren != null)
		{
			componentInChildren.Setup(variant);
		}
		return gameObject2;
	}

	private void CleanupInstance(GameObject instance)
	{
		Collider[] componentsInChildren = instance.GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
	}

	private void EnableEquipedEffects(GameObject instance)
	{
		Transform transform = instance.transform.Find("equiped");
		if (transform)
		{
			transform.gameObject.SetActive(true);
		}
	}

	public int GetModelIndex()
	{
		int result = this.m_modelIndex;
		if (this.m_nview.IsValid())
		{
			result = this.m_nview.GetZDO().GetInt("ModelIndex", 0);
		}
		return result;
	}

	public SkinnedMeshRenderer m_bodyModel;

	[Header("Attachment points")]
	public Transform m_leftHand;

	public Transform m_rightHand;

	public Transform m_helmet;

	public Transform m_backShield;

	public Transform m_backMelee;

	public Transform m_backTwohandedMelee;

	public Transform m_backBow;

	public Transform m_backTool;

	public Transform m_backAtgeir;

	public CapsuleCollider[] m_clothColliders = new CapsuleCollider[0];

	public VisEquipment.PlayerModel[] m_models = new VisEquipment.PlayerModel[0];

	public bool m_isPlayer;

	public bool m_useAllTrails;

	private string m_leftItem = "";

	private string m_rightItem = "";

	private string m_chestItem = "";

	private string m_legItem = "";

	private string m_helmetItem = "";

	private string m_shoulderItem = "";

	private string m_beardItem = "";

	private string m_hairItem = "";

	private string m_utilityItem = "";

	private string m_leftBackItem = "";

	private string m_rightBackItem = "";

	private int m_shoulderItemVariant;

	private int m_leftItemVariant;

	private int m_leftBackItemVariant;

	private GameObject m_leftItemInstance;

	private GameObject m_rightItemInstance;

	private GameObject m_helmetItemInstance;

	private List<GameObject> m_chestItemInstances;

	private List<GameObject> m_legItemInstances;

	private List<GameObject> m_shoulderItemInstances;

	private List<GameObject> m_utilityItemInstances;

	private GameObject m_beardItemInstance;

	private GameObject m_hairItemInstance;

	private GameObject m_leftBackItemInstance;

	private GameObject m_rightBackItemInstance;

	private int m_currentLeftItemHash;

	private int m_currentRightItemHash;

	private int m_currentChestItemHash;

	private int m_currentLegItemHash;

	private int m_currentHelmetItemHash;

	private int m_currentShoulderItemHash;

	private int m_currentBeardItemHash;

	private int m_currentHairItemHash;

	private int m_currentUtilityItemHash;

	private int m_currentLeftBackItemHash;

	private int m_currentRightBackItemHash;

	private int m_currenShoulderItemVariant;

	private int m_currentLeftItemVariant;

	private int m_currentLeftBackItemVariant;

	private bool m_helmetHideHair;

	private Texture m_emptyBodyTexture;

	private int m_modelIndex;

	private Vector3 m_skinColor = Vector3.one;

	private Vector3 m_hairColor = Vector3.one;

	private int m_currentModelIndex;

	private ZNetView m_nview;

	private GameObject m_visual;

	private LODGroup m_lodGroup;

	[Serializable]
	public class PlayerModel
	{
		public Mesh m_mesh;

		public Material m_baseMaterial;
	}
}
