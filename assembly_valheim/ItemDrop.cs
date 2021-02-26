using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ItemDrop : MonoBehaviour, Hoverable, Interactable
{
	private void Awake()
	{
		this.m_myIndex = ItemDrop.m_instances.Count;
		ItemDrop.m_instances.Add(this);
		string prefabName = this.GetPrefabName(base.gameObject.name);
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(prefabName);
		this.m_itemData.m_dropPrefab = itemPrefab;
		if (Application.isEditor)
		{
			this.m_itemData.m_shared = itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		}
		Rigidbody component = base.GetComponent<Rigidbody>();
		if (component)
		{
			component.maxDepenetrationVelocity = 1f;
		}
		this.m_spawnTime = Time.time;
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview && this.m_nview.IsValid())
		{
			if (this.m_nview.IsOwner())
			{
				DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong("SpawnTime", 0L));
				if (dateTime.Ticks == 0L)
				{
					this.m_nview.GetZDO().Set("SpawnTime", ZNet.instance.GetTime().Ticks);
				}
			}
			this.m_nview.Register("RequestOwn", new Action<long>(this.RPC_RequestOwn));
			this.Load();
			base.InvokeRepeating("SlowUpdate", UnityEngine.Random.Range(1f, 2f), 10f);
		}
	}

	private void OnDestroy()
	{
		ItemDrop.m_instances[this.m_myIndex] = ItemDrop.m_instances[ItemDrop.m_instances.Count - 1];
		ItemDrop.m_instances[this.m_myIndex].m_myIndex = this.m_myIndex;
		ItemDrop.m_instances.RemoveAt(ItemDrop.m_instances.Count - 1);
	}

	private void Start()
	{
		this.Save();
		IEquipmentVisual componentInChildren = base.gameObject.GetComponentInChildren<IEquipmentVisual>();
		if (componentInChildren != null)
		{
			componentInChildren.Setup(this.m_itemData.m_variant);
		}
	}

	private double GetTimeSinceSpawned()
	{
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong("SpawnTime", 0L));
		return (ZNet.instance.GetTime() - d).TotalSeconds;
	}

	private void SlowUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.TerrainCheck();
		if (this.m_autoDestroy)
		{
			this.TimedDestruction();
		}
		if (ItemDrop.m_instances.Count > 200)
		{
			this.AutoStackItems();
		}
	}

	private void TerrainCheck()
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y - groundHeight < -0.5f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 0.5f;
			base.transform.position = position;
			Rigidbody component = base.GetComponent<Rigidbody>();
			if (component)
			{
				component.velocity = Vector3.zero;
			}
		}
	}

	private void TimedDestruction()
	{
		if (this.IsInsideBase())
		{
			return;
		}
		if (Player.IsPlayerInRange(base.transform.position, 25f))
		{
			return;
		}
		if (this.GetTimeSinceSpawned() < 3600.0)
		{
			return;
		}
		this.m_nview.Destroy();
	}

	private bool IsInsideBase()
	{
		return base.transform.position.y > ZoneSystem.instance.m_waterLevel + -2f && EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.PlayerBase, 0f);
	}

	private void AutoStackItems()
	{
		if (this.m_itemData.m_shared.m_maxStackSize <= 1 || this.m_itemData.m_stack >= this.m_itemData.m_shared.m_maxStackSize)
		{
			return;
		}
		if (this.m_haveAutoStacked)
		{
			return;
		}
		this.m_haveAutoStacked = true;
		if (ItemDrop.m_itemMask == 0)
		{
			ItemDrop.m_itemMask = LayerMask.GetMask(new string[]
			{
				"item"
			});
		}
		bool flag = false;
		foreach (Collider collider in Physics.OverlapSphere(base.transform.position, 4f, ItemDrop.m_itemMask))
		{
			if (collider.attachedRigidbody)
			{
				ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
				if (!(component == null) && !(component == this) && !(component.m_nview == null) && component.m_nview.IsValid() && component.m_nview.IsOwner() && !(component.m_itemData.m_shared.m_name != this.m_itemData.m_shared.m_name) && component.m_itemData.m_quality == this.m_itemData.m_quality)
				{
					int num = this.m_itemData.m_shared.m_maxStackSize - this.m_itemData.m_stack;
					if (num == 0)
					{
						break;
					}
					if (component.m_itemData.m_stack <= num)
					{
						this.m_itemData.m_stack += component.m_itemData.m_stack;
						flag = true;
						component.m_nview.Destroy();
					}
				}
			}
		}
		if (flag)
		{
			this.Save();
		}
	}

	public string GetHoverText()
	{
		string text = this.m_itemData.m_shared.m_name;
		if (this.m_itemData.m_quality > 1)
		{
			text = string.Concat(new object[]
			{
				text,
				"[",
				this.m_itemData.m_quality,
				"] "
			});
		}
		if (this.m_itemData.m_stack > 1)
		{
			text = text + " x" + this.m_itemData.m_stack.ToString();
		}
		return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup");
	}

	public string GetHoverName()
	{
		return this.m_itemData.m_shared.m_name;
	}

	private string GetPrefabName(string name)
	{
		char[] anyOf = new char[]
		{
			'(',
			' '
		};
		int num = name.IndexOfAny(anyOf);
		string result;
		if (num >= 0)
		{
			result = name.Substring(0, num);
		}
		else
		{
			result = name;
		}
		return result;
	}

	public bool Interact(Humanoid character, bool repeat)
	{
		if (repeat)
		{
			return false;
		}
		this.Pickup(character);
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void Pickup(Humanoid character)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.CanPickup())
		{
			this.Load();
			character.Pickup(base.gameObject);
			this.Save();
			return;
		}
		this.m_pickupRequester = character;
		base.CancelInvoke("PickupUpdate");
		float num = 0.05f;
		base.InvokeRepeating("PickupUpdate", num, num);
		this.RequestOwn();
	}

	public void RequestOwn()
	{
		if (Time.time - this.m_lastOwnerRequest < 0.2f)
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			return;
		}
		this.m_lastOwnerRequest = Time.time;
		this.m_nview.InvokeRPC("RequestOwn", Array.Empty<object>());
	}

	public bool RemoveOne()
	{
		if (!this.CanPickup())
		{
			this.RequestOwn();
			return false;
		}
		if (this.m_itemData.m_stack <= 1)
		{
			this.m_nview.Destroy();
			return true;
		}
		this.m_itemData.m_stack--;
		this.Save();
		return true;
	}

	public void OnPlayerDrop()
	{
		this.m_autoPickup = false;
	}

	public bool CanPickup()
	{
		return this.m_nview == null || !this.m_nview.IsValid() || ((double)(Time.time - this.m_spawnTime) >= 0.5 && this.m_nview.IsOwner());
	}

	private void RPC_RequestOwn(long uid)
	{
		ZLog.Log(string.Concat(new object[]
		{
			"Player ",
			uid,
			" wants to pickup ",
			base.gameObject.name,
			"   im: ",
			ZDOMan.instance.GetMyID()
		}));
		if (!this.m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
			return;
		}
		this.m_nview.GetZDO().SetOwner(uid);
	}

	private void PickupUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.CanPickup())
		{
			ZLog.Log("Im finally the owner");
			base.CancelInvoke("PickupUpdate");
			this.Load();
			(this.m_pickupRequester as Player).Pickup(base.gameObject);
			this.Save();
			return;
		}
		ZLog.Log("Im still nto the owner");
	}

	private void Save()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			ItemDrop.SaveToZDO(this.m_itemData, this.m_nview.GetZDO());
		}
	}

	private void Load()
	{
		ItemDrop.LoadFromZDO(this.m_itemData, this.m_nview.GetZDO());
	}

	public static void SaveToZDO(ItemDrop.ItemData itemData, ZDO zdo)
	{
		zdo.Set("durability", itemData.m_durability);
		zdo.Set("stack", itemData.m_stack);
		zdo.Set("quality", itemData.m_quality);
		zdo.Set("variant", itemData.m_variant);
		zdo.Set("crafterID", itemData.m_crafterID);
		zdo.Set("crafterName", itemData.m_crafterName);
	}

	public static void LoadFromZDO(ItemDrop.ItemData itemData, ZDO zdo)
	{
		itemData.m_durability = zdo.GetFloat("durability", itemData.m_durability);
		itemData.m_stack = zdo.GetInt("stack", itemData.m_stack);
		itemData.m_quality = zdo.GetInt("quality", itemData.m_quality);
		itemData.m_variant = zdo.GetInt("variant", itemData.m_variant);
		itemData.m_crafterID = zdo.GetLong("crafterID", itemData.m_crafterID);
		itemData.m_crafterName = zdo.GetString("crafterName", itemData.m_crafterName);
	}

	public static ItemDrop DropItem(ItemDrop.ItemData item, int amount, Vector3 position, Quaternion rotation)
	{
		ItemDrop component = UnityEngine.Object.Instantiate<GameObject>(item.m_dropPrefab, position, rotation).GetComponent<ItemDrop>();
		component.m_itemData = item.Clone();
		if (amount > 0)
		{
			component.m_itemData.m_stack = amount;
		}
		component.Save();
		return component;
	}

	private void OnDrawGizmos()
	{
	}

	private static List<ItemDrop> m_instances = new List<ItemDrop>();

	private int m_myIndex = -1;

	public bool m_autoPickup = true;

	public bool m_autoDestroy = true;

	public ItemDrop.ItemData m_itemData = new ItemDrop.ItemData();

	private ZNetView m_nview;

	private Character m_pickupRequester;

	private float m_lastOwnerRequest;

	private float m_spawnTime;

	private const double m_autoDestroyTimeout = 3600.0;

	private const double m_autoPickupDelay = 0.5;

	private const float m_autoDespawnBaseMinAltitude = -2f;

	private const int m_autoStackTreshold = 200;

	private const float m_autoStackRange = 4f;

	private static int m_itemMask = 0;

	private bool m_haveAutoStacked;

	[Serializable]
	public class ItemData
	{
		public ItemDrop.ItemData Clone()
		{
			return base.MemberwiseClone() as ItemDrop.ItemData;
		}

		public bool IsEquipable()
		{
			return this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility;
		}

		public bool IsWeapon()
		{
			return this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch;
		}

		public bool HavePrimaryAttack()
		{
			return !string.IsNullOrEmpty(this.m_shared.m_attack.m_attackAnimation);
		}

		public bool HaveSecondaryAttack()
		{
			return !string.IsNullOrEmpty(this.m_shared.m_secondaryAttack.m_attackAnimation);
		}

		public float GetArmor()
		{
			return this.GetArmor(this.m_quality);
		}

		public float GetArmor(int quality)
		{
			return this.m_shared.m_armor + (float)Mathf.Max(0, quality - 1) * this.m_shared.m_armorPerLevel;
		}

		public int GetValue()
		{
			return this.m_shared.m_value * this.m_stack;
		}

		public float GetWeight()
		{
			return this.m_shared.m_weight * (float)this.m_stack;
		}

		public HitData.DamageTypes GetDamage()
		{
			return this.GetDamage(this.m_quality);
		}

		public float GetDurabilityPercentage()
		{
			float maxDurability = this.GetMaxDurability();
			if (maxDurability == 0f)
			{
				return 1f;
			}
			return Mathf.Clamp01(this.m_durability / maxDurability);
		}

		public float GetMaxDurability()
		{
			return this.GetMaxDurability(this.m_quality);
		}

		public float GetMaxDurability(int quality)
		{
			return this.m_shared.m_maxDurability + (float)Mathf.Max(0, quality - 1) * this.m_shared.m_durabilityPerLevel;
		}

		public HitData.DamageTypes GetDamage(int quality)
		{
			HitData.DamageTypes damages = this.m_shared.m_damages;
			if (quality > 1)
			{
				damages.Add(this.m_shared.m_damagesPerLevel, quality - 1);
			}
			return damages;
		}

		public float GetBaseBlockPower()
		{
			return this.GetBaseBlockPower(this.m_quality);
		}

		public float GetBaseBlockPower(int quality)
		{
			return this.m_shared.m_blockPower + (float)Mathf.Max(0, quality - 1) * this.m_shared.m_blockPowerPerLevel;
		}

		public float GetBlockPower(float skillFactor)
		{
			return this.GetBlockPower(this.m_quality, skillFactor);
		}

		public float GetBlockPower(int quality, float skillFactor)
		{
			float baseBlockPower = this.GetBaseBlockPower(quality);
			return baseBlockPower + baseBlockPower * skillFactor * 0.5f;
		}

		public float GetBlockPowerTooltip(int quality)
		{
			if (Player.m_localPlayer == null)
			{
				return 0f;
			}
			float skillFactor = Player.m_localPlayer.GetSkillFactor(Skills.SkillType.Blocking);
			return this.GetBlockPower(quality, skillFactor);
		}

		public float GetDeflectionForce()
		{
			return this.GetDeflectionForce(this.m_quality);
		}

		public float GetDeflectionForce(int quality)
		{
			return this.m_shared.m_deflectionForce + (float)Mathf.Max(0, quality - 1) * this.m_shared.m_deflectionForcePerLevel;
		}

		public string GetTooltip()
		{
			return ItemDrop.ItemData.GetTooltip(this, this.m_quality, false);
		}

		public Sprite GetIcon()
		{
			return this.m_shared.m_icons[this.m_variant];
		}

		private static void AddHandedTip(ItemDrop.ItemData item, StringBuilder text)
		{
			ItemDrop.ItemData.ItemType itemType = item.m_shared.m_itemType;
			if (itemType <= ItemDrop.ItemData.ItemType.TwoHandedWeapon)
			{
				switch (itemType)
				{
				case ItemDrop.ItemData.ItemType.OneHandedWeapon:
				case ItemDrop.ItemData.ItemType.Shield:
					break;
				case ItemDrop.ItemData.ItemType.Bow:
					goto IL_43;
				default:
					if (itemType != ItemDrop.ItemData.ItemType.TwoHandedWeapon)
					{
						return;
					}
					goto IL_43;
				}
			}
			else if (itemType != ItemDrop.ItemData.ItemType.Torch)
			{
				if (itemType != ItemDrop.ItemData.ItemType.Tool)
				{
					return;
				}
				goto IL_43;
			}
			text.Append("\n$item_onehanded");
			return;
			IL_43:
			text.Append("\n$item_twohanded");
		}

		public static string GetTooltip(ItemDrop.ItemData item, int qualityLevel, bool crafting)
		{
			Player localPlayer = Player.m_localPlayer;
			StringBuilder stringBuilder = new StringBuilder(256);
			stringBuilder.Append(item.m_shared.m_description);
			stringBuilder.Append("\n\n");
			if (item.m_shared.m_dlc.Length > 0)
			{
				stringBuilder.Append("\n<color=aqua>$item_dlc</color>");
			}
			ItemDrop.ItemData.AddHandedTip(item, stringBuilder);
			if (item.m_crafterID != 0L)
			{
				stringBuilder.AppendFormat("\n$item_crafter: <color=orange>{0}</color>", item.m_crafterName);
			}
			if (!item.m_shared.m_teleportable)
			{
				stringBuilder.Append("\n<color=orange>$item_noteleport</color>");
			}
			if (item.m_shared.m_value > 0)
			{
				stringBuilder.AppendFormat("\n$item_value: <color=orange>{0}  ({1})</color>", item.GetValue(), item.m_shared.m_value);
			}
			stringBuilder.AppendFormat("\n$item_weight: <color=orange>{0}</color>", item.GetWeight().ToString("0.0"));
			if (item.m_shared.m_maxQuality > 1)
			{
				stringBuilder.AppendFormat("\n$item_quality: <color=orange>{0}</color>", qualityLevel);
			}
			if (item.m_shared.m_useDurability)
			{
				if (crafting)
				{
					float maxDurability = item.GetMaxDurability(qualityLevel);
					stringBuilder.AppendFormat("\n$item_durability: <color=orange>{0}</color>", maxDurability);
				}
				else
				{
					float maxDurability2 = item.GetMaxDurability(qualityLevel);
					float durability = item.m_durability;
					stringBuilder.AppendFormat("\n$item_durability: <color=orange>{0}%</color> <color=yellow>({1}/{2})</color>", (item.GetDurabilityPercentage() * 100f).ToString("0"), durability.ToString("0"), maxDurability2.ToString("0"));
				}
				if (item.m_shared.m_canBeReparied)
				{
					Recipe recipe = ObjectDB.instance.GetRecipe(item);
					if (recipe != null)
					{
						int minStationLevel = recipe.m_minStationLevel;
						stringBuilder.AppendFormat("\n$item_repairlevel: <color=orange>{0}</color>", minStationLevel.ToString());
					}
				}
			}
			switch (item.m_shared.m_itemType)
			{
			case ItemDrop.ItemData.ItemType.Consumable:
			{
				if (item.m_shared.m_food > 0f)
				{
					stringBuilder.AppendFormat("\n$item_food_health: <color=orange>{0}</color>", item.m_shared.m_food);
					stringBuilder.AppendFormat("\n$item_food_stamina: <color=orange>{0}</color>", item.m_shared.m_foodStamina);
					stringBuilder.AppendFormat("\n$item_food_duration: <color=orange>{0}s</color>", item.m_shared.m_foodBurnTime);
					stringBuilder.AppendFormat("\n$item_food_regen: <color=orange>{0} hp/tick</color>", item.m_shared.m_foodRegen);
				}
				string statusEffectTooltip = item.GetStatusEffectTooltip();
				if (statusEffectTooltip.Length > 0)
				{
					stringBuilder.Append("\n\n");
					stringBuilder.Append(statusEffectTooltip);
				}
				break;
			}
			case ItemDrop.ItemData.ItemType.OneHandedWeapon:
			case ItemDrop.ItemData.ItemType.Bow:
			case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
			case ItemDrop.ItemData.ItemType.Torch:
			{
				stringBuilder.Append(item.GetDamage(qualityLevel).GetTooltipString(item.m_shared.m_skillType));
				stringBuilder.AppendFormat("\n$item_blockpower: <color=orange>{0}</color> <color=yellow>({1})</color>", item.GetBaseBlockPower(qualityLevel), item.GetBlockPowerTooltip(qualityLevel).ToString("0"));
				if (item.m_shared.m_timedBlockBonus > 1f)
				{
					stringBuilder.AppendFormat("\n$item_deflection: <color=orange>{0}</color>", item.GetDeflectionForce(qualityLevel));
					stringBuilder.AppendFormat("\n$item_parrybonus: <color=orange>{0}x</color>", item.m_shared.m_timedBlockBonus);
				}
				stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", item.m_shared.m_attackForce);
				stringBuilder.AppendFormat("\n$item_backstab: <color=orange>{0}x</color>", item.m_shared.m_backstabBonus);
				string projectileTooltip = item.GetProjectileTooltip(qualityLevel);
				if (projectileTooltip.Length > 0)
				{
					stringBuilder.Append("\n\n");
					stringBuilder.Append(projectileTooltip);
				}
				string statusEffectTooltip2 = item.GetStatusEffectTooltip();
				if (statusEffectTooltip2.Length > 0)
				{
					stringBuilder.Append("\n\n");
					stringBuilder.Append(statusEffectTooltip2);
				}
				break;
			}
			case ItemDrop.ItemData.ItemType.Shield:
				stringBuilder.AppendFormat("\n$item_blockpower: <color=orange>{0}</color> <color=yellow>({1})</color>", item.GetBaseBlockPower(qualityLevel), item.GetBlockPowerTooltip(qualityLevel).ToString("0"));
				if (item.m_shared.m_timedBlockBonus > 1f)
				{
					stringBuilder.AppendFormat("\n$item_deflection: <color=orange>{0}</color>", item.GetDeflectionForce(qualityLevel));
					stringBuilder.AppendFormat("\n$item_parrybonus: <color=orange>{0}x</color>", item.m_shared.m_timedBlockBonus);
				}
				break;
			case ItemDrop.ItemData.ItemType.Helmet:
			case ItemDrop.ItemData.ItemType.Chest:
			case ItemDrop.ItemData.ItemType.Legs:
			case ItemDrop.ItemData.ItemType.Shoulder:
			{
				stringBuilder.AppendFormat("\n$item_armor: <color=orange>{0}</color>", item.GetArmor(qualityLevel));
				string damageModifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
				if (damageModifiersTooltipString.Length > 0)
				{
					stringBuilder.Append(damageModifiersTooltipString);
				}
				string statusEffectTooltip3 = item.GetStatusEffectTooltip();
				if (statusEffectTooltip3.Length > 0)
				{
					stringBuilder.Append("\n\n");
					stringBuilder.Append(statusEffectTooltip3);
				}
				break;
			}
			case ItemDrop.ItemData.ItemType.Ammo:
				stringBuilder.Append(item.GetDamage(qualityLevel).GetTooltipString(item.m_shared.m_skillType));
				stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", item.m_shared.m_attackForce);
				break;
			}
			if (item.m_shared.m_movementModifier != 0f && localPlayer != null)
			{
				float equipmentMovementModifier = localPlayer.GetEquipmentMovementModifier();
				stringBuilder.AppendFormat("\n$item_movement_modifier: <color=orange>{0}%</color> ($item_total:<color=yellow>{1}%</color>)", (item.m_shared.m_movementModifier * 100f).ToString("+0;-0"), (equipmentMovementModifier * 100f).ToString("+0;-0"));
			}
			string setStatusEffectTooltip = item.GetSetStatusEffectTooltip();
			if (setStatusEffectTooltip.Length > 0)
			{
				stringBuilder.AppendFormat("\n\n$item_seteffect (<color=orange>{0}</color> $item_parts):<color=orange>{1}</color>", item.m_shared.m_setSize, setStatusEffectTooltip);
			}
			return stringBuilder.ToString();
		}

		private string GetStatusEffectTooltip()
		{
			if (this.m_shared.m_attackStatusEffect)
			{
				return this.m_shared.m_attackStatusEffect.GetTooltipString();
			}
			if (this.m_shared.m_consumeStatusEffect)
			{
				return this.m_shared.m_consumeStatusEffect.GetTooltipString();
			}
			return "";
		}

		private string GetSetStatusEffectTooltip()
		{
			if (this.m_shared.m_setStatusEffect)
			{
				StatusEffect setStatusEffect = this.m_shared.m_setStatusEffect;
				if (setStatusEffect != null)
				{
					return setStatusEffect.GetTooltipString();
				}
			}
			return "";
		}

		private string GetProjectileTooltip(int itemQuality)
		{
			string text = "";
			if (this.m_shared.m_attack.m_attackProjectile)
			{
				IProjectile component = this.m_shared.m_attack.m_attackProjectile.GetComponent<IProjectile>();
				if (component != null)
				{
					text += component.GetTooltipString(itemQuality);
				}
			}
			if (this.m_shared.m_spawnOnHit)
			{
				IProjectile component2 = this.m_shared.m_spawnOnHit.GetComponent<IProjectile>();
				if (component2 != null)
				{
					text += component2.GetTooltipString(itemQuality);
				}
			}
			return text;
		}

		public int m_stack = 1;

		public float m_durability = 100f;

		public int m_quality = 1;

		public int m_variant;

		public ItemDrop.ItemData.SharedData m_shared;

		[NonSerialized]
		public long m_crafterID;

		[NonSerialized]
		public string m_crafterName = "";

		[NonSerialized]
		public Vector2i m_gridPos = Vector2i.zero;

		[NonSerialized]
		public bool m_equiped;

		[NonSerialized]
		public GameObject m_dropPrefab;

		[NonSerialized]
		public float m_lastAttackTime;

		[NonSerialized]
		public GameObject m_lastProjectile;

		public enum ItemType
		{
			None,
			Material,
			Consumable,
			OneHandedWeapon,
			Bow,
			Shield,
			Helmet,
			Chest,
			Ammo = 9,
			Customization,
			Legs,
			Hands,
			Trophie,
			TwoHandedWeapon,
			Torch,
			Misc,
			Shoulder,
			Utility,
			Tool,
			Attach_Atgeir
		}

		public enum AnimationState
		{
			Unarmed,
			OneHanded,
			TwoHandedClub,
			Bow,
			Shield,
			Torch,
			LeftTorch,
			Atgeir,
			TwoHandedAxe,
			FishingRod
		}

		public enum AiTarget
		{
			Enemy,
			FriendHurt,
			Friend
		}

		[Serializable]
		public class SharedData
		{
			public string m_name = "";

			public string m_dlc = "";

			public ItemDrop.ItemData.ItemType m_itemType = ItemDrop.ItemData.ItemType.Misc;

			public Sprite[] m_icons = new Sprite[0];

			public ItemDrop.ItemData.ItemType m_attachOverride;

			[TextArea]
			public string m_description = "";

			public int m_maxStackSize = 1;

			public int m_maxQuality = 1;

			public float m_weight = 1f;

			public int m_value;

			public bool m_teleportable = true;

			public bool m_questItem;

			public float m_equipDuration = 1f;

			public int m_variants;

			public Vector2Int m_trophyPos = Vector2Int.zero;

			public PieceTable m_buildPieces;

			public bool m_centerCamera;

			public string m_setName = "";

			public int m_setSize;

			public StatusEffect m_setStatusEffect;

			public StatusEffect m_equipStatusEffect;

			public float m_movementModifier;

			[Header("Food settings")]
			public float m_food;

			public float m_foodStamina;

			public float m_foodBurnTime;

			public float m_foodRegen;

			public Color m_foodColor = Color.white;

			[Header("Armor settings")]
			public Material m_armorMaterial;

			public bool m_helmetHideHair = true;

			public float m_armor = 10f;

			public float m_armorPerLevel = 1f;

			public List<HitData.DamageModPair> m_damageModifiers = new List<HitData.DamageModPair>();

			[Header("Shield settings")]
			public float m_blockPower = 10f;

			public float m_blockPowerPerLevel;

			public float m_deflectionForce;

			public float m_deflectionForcePerLevel;

			public float m_timedBlockBonus = 1.5f;

			[Header("Weapon")]
			public ItemDrop.ItemData.AnimationState m_animationState = ItemDrop.ItemData.AnimationState.OneHanded;

			public Skills.SkillType m_skillType = Skills.SkillType.Swords;

			public int m_toolTier;

			public HitData.DamageTypes m_damages;

			public HitData.DamageTypes m_damagesPerLevel;

			public float m_attackForce = 30f;

			public float m_backstabBonus = 4f;

			public bool m_dodgeable;

			public bool m_blockable;

			public StatusEffect m_attackStatusEffect;

			public GameObject m_spawnOnHit;

			public GameObject m_spawnOnHitTerrain;

			[Header("Attacks")]
			public Attack m_attack;

			public Attack m_secondaryAttack;

			[Header("Durability")]
			public bool m_useDurability;

			public bool m_destroyBroken = true;

			public bool m_canBeReparied = true;

			public float m_maxDurability = 100f;

			public float m_durabilityPerLevel = 50f;

			public float m_useDurabilityDrain = 1f;

			public float m_durabilityDrain;

			[Header("Hold")]
			public float m_holdDurationMin;

			public float m_holdStaminaDrain;

			public string m_holdAnimationState = "";

			[Header("Ammo")]
			public string m_ammoType = "";

			[Header("AI")]
			public float m_aiAttackRange = 2f;

			public float m_aiAttackRangeMin;

			public float m_aiAttackInterval = 2f;

			public float m_aiAttackMaxAngle = 5f;

			public bool m_aiWhenFlying = true;

			public bool m_aiWhenWalking = true;

			public bool m_aiWhenSwiming = true;

			public bool m_aiPrioritized;

			public ItemDrop.ItemData.AiTarget m_aiTargetType;

			[Header("Effects")]
			public EffectList m_hitEffect = new EffectList();

			public EffectList m_hitTerrainEffect = new EffectList();

			public EffectList m_blockEffect = new EffectList();

			public EffectList m_startEffect = new EffectList();

			public EffectList m_holdStartEffect = new EffectList();

			public EffectList m_triggerEffect = new EffectList();

			public EffectList m_trailStartEffect = new EffectList();

			[Header("Consumable")]
			public StatusEffect m_consumeStatusEffect;
		}
	}
}
