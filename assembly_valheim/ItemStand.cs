using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemStand : MonoBehaviour, Interactable, Hoverable
{
	private void Awake()
	{
		this.m_nview = (this.m_netViewOverride ? this.m_netViewOverride : base.gameObject.GetComponent<ZNetView>());
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		WearNTear component = base.GetComponent<WearNTear>();
		if (component)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(this.OnDestroyed));
		}
		this.m_nview.Register("DropItem", new Action<long>(this.RPC_DropItem));
		this.m_nview.Register("RequestOwn", new Action<long>(this.RPC_RequestOwn));
		this.m_nview.Register("DestroyAttachment", new Action<long>(this.RPC_DestroyAttachment));
		this.m_nview.Register<string, int>("SetVisualItem", new Action<long, string, int>(this.RPC_SetVisualItem));
		base.InvokeRepeating("UpdateVisual", 1f, 4f);
	}

	private void OnDestroyed()
	{
		if (this.m_nview.IsOwner())
		{
			this.DropItem();
		}
	}

	public string GetHoverText()
	{
		if (!Player.m_localPlayer)
		{
			return "";
		}
		if (this.HaveAttachment())
		{
			if (this.m_canBeRemoved)
			{
				return Localization.instance.Localize(this.m_name + " ( " + this.m_currentItemName + " )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_itemstand_take");
			}
			if (!(this.m_guardianPower != null))
			{
				return "";
			}
			if (base.IsInvoking("DelayedPowerActivation"))
			{
				return "";
			}
			if (this.IsGuardianPowerActive(Player.m_localPlayer))
			{
				return "";
			}
			string tooltipString = this.m_guardianPower.GetTooltipString();
			return Localization.instance.Localize(string.Concat(new string[]
			{
				"<color=orange>",
				this.m_guardianPower.m_name,
				"</color>\n",
				tooltipString,
				"\n\n[<color=yellow><b>$KEY_Use</b></color>] $guardianstone_hook_activate"
			}));
		}
		else
		{
			if (this.m_autoAttach && this.m_supportedItems.Count == 1)
			{
				return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_itemstand_attach");
			}
			return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>1-8</b></color>] $piece_itemstand_attach");
		}
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid user, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (!this.HaveAttachment())
		{
			if (this.m_autoAttach && this.m_supportedItems.Count == 1)
			{
				ItemDrop.ItemData item = user.GetInventory().GetItem(this.m_supportedItems[0].m_itemData.m_shared.m_name);
				if (item != null)
				{
					this.UseItem(user, item);
					return true;
				}
				user.Message(MessageHud.MessageType.Center, "$piece_itemstand_missingitem", 0, null);
				return false;
			}
		}
		else
		{
			if (this.m_canBeRemoved)
			{
				this.m_nview.InvokeRPC("DropItem", Array.Empty<object>());
				return true;
			}
			if (this.m_guardianPower != null)
			{
				if (base.IsInvoking("DelayedPowerActivation"))
				{
					return false;
				}
				if (this.IsGuardianPowerActive(user))
				{
					return false;
				}
				user.Message(MessageHud.MessageType.Center, "$guardianstone_hook_power_activate ", 0, null);
				this.m_activatePowerEffects.Create(base.transform.position, base.transform.rotation, null, 1f);
				this.m_activatePowerEffectsPlayer.Create(user.transform.position, Quaternion.identity, user.transform, 1f);
				base.Invoke("DelayedPowerActivation", this.m_powerActivationDelay);
				return true;
			}
		}
		return false;
	}

	private bool IsGuardianPowerActive(Humanoid user)
	{
		return (user as Player).GetGuardianPowerName() == this.m_guardianPower.name;
	}

	private void DelayedPowerActivation()
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			return;
		}
		localPlayer.SetGuardianPower(this.m_guardianPower.name);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (this.HaveAttachment())
		{
			return false;
		}
		if (!this.CanAttach(item))
		{
			user.Message(MessageHud.MessageType.Center, "$piece_itemstand_cantattach", 0, null);
			return true;
		}
		if (!this.m_nview.IsOwner())
		{
			this.m_nview.InvokeRPC("RequestOwn", Array.Empty<object>());
		}
		this.m_queuedItem = item;
		base.CancelInvoke("UpdateAttach");
		base.InvokeRepeating("UpdateAttach", 0f, 0.1f);
		return true;
	}

	private void RPC_DropItem(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_canBeRemoved)
		{
			return;
		}
		this.DropItem();
	}

	public void DestroyAttachment()
	{
		this.m_nview.InvokeRPC("DestroyAttachment", Array.Empty<object>());
	}

	public void RPC_DestroyAttachment(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.HaveAttachment())
		{
			return;
		}
		this.m_nview.GetZDO().Set("item", "");
		this.m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", new object[]
		{
			"",
			0
		});
		this.m_destroyEffects.Create(this.m_dropSpawnPoint.position, Quaternion.identity, null, 1f);
	}

	private void DropItem()
	{
		if (!this.HaveAttachment())
		{
			return;
		}
		string @string = this.m_nview.GetZDO().GetString("item", "");
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(@string);
		if (itemPrefab)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(itemPrefab, this.m_dropSpawnPoint.position, this.m_dropSpawnPoint.rotation);
			ItemDrop.LoadFromZDO(gameObject.GetComponent<ItemDrop>().m_itemData, this.m_nview.GetZDO());
			gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
			this.m_effects.Create(this.m_dropSpawnPoint.position, Quaternion.identity, null, 1f);
		}
		this.m_nview.GetZDO().Set("item", "");
		this.m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", new object[]
		{
			"",
			0
		});
	}

	private Transform GetAttach(ItemDrop.ItemData item)
	{
		return this.m_attachOther;
	}

	private void UpdateAttach()
	{
		if (this.m_nview.IsOwner())
		{
			base.CancelInvoke("UpdateAttach");
			Player localPlayer = Player.m_localPlayer;
			if (this.m_queuedItem != null && localPlayer != null && localPlayer.GetInventory().ContainsItem(this.m_queuedItem) && !this.HaveAttachment())
			{
				ItemDrop.ItemData itemData = this.m_queuedItem.Clone();
				itemData.m_stack = 1;
				this.m_nview.GetZDO().Set("item", this.m_queuedItem.m_dropPrefab.name);
				ItemDrop.SaveToZDO(itemData, this.m_nview.GetZDO());
				localPlayer.UnequipItem(this.m_queuedItem, true);
				localPlayer.GetInventory().RemoveOneItem(this.m_queuedItem);
				this.m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", new object[]
				{
					itemData.m_dropPrefab.name,
					itemData.m_variant
				});
				Transform attach = this.GetAttach(this.m_queuedItem);
				this.m_effects.Create(attach.transform.position, Quaternion.identity, null, 1f);
			}
			this.m_queuedItem = null;
		}
	}

	private void RPC_RequestOwn(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().SetOwner(sender);
	}

	private void UpdateVisual()
	{
		string @string = this.m_nview.GetZDO().GetString("item", "");
		int @int = this.m_nview.GetZDO().GetInt("variant", 0);
		this.SetVisualItem(@string, @int);
	}

	private void RPC_SetVisualItem(long sender, string itemName, int variant)
	{
		this.SetVisualItem(itemName, variant);
	}

	private void SetVisualItem(string itemName, int variant)
	{
		if (this.m_visualName == itemName && this.m_visualVariant == variant)
		{
			return;
		}
		this.m_visualName = itemName;
		this.m_visualVariant = variant;
		this.m_currentItemName = "";
		if (this.m_visualName == "")
		{
			UnityEngine.Object.Destroy(this.m_visualItem);
			return;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemName);
		if (itemPrefab == null)
		{
			ZLog.LogWarning("Missing item prefab " + itemName);
			return;
		}
		GameObject attachPrefab = this.GetAttachPrefab(itemPrefab);
		if (attachPrefab == null)
		{
			ZLog.LogWarning("Failed to get attach prefab for item " + itemName);
			return;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		this.m_currentItemName = component.m_itemData.m_shared.m_name;
		Transform attach = this.GetAttach(component.m_itemData);
		this.m_visualItem = UnityEngine.Object.Instantiate<GameObject>(attachPrefab, attach.position, attach.rotation, attach);
		this.m_visualItem.transform.localPosition = attachPrefab.transform.localPosition;
		this.m_visualItem.transform.localRotation = attachPrefab.transform.localRotation;
		IEquipmentVisual componentInChildren = this.m_visualItem.GetComponentInChildren<IEquipmentVisual>();
		if (componentInChildren != null)
		{
			componentInChildren.Setup(this.m_visualVariant);
		}
	}

	private GameObject GetAttachPrefab(GameObject item)
	{
		Transform transform = item.transform.Find("attach");
		if (transform)
		{
			return transform.gameObject;
		}
		return null;
	}

	private bool CanAttach(ItemDrop.ItemData item)
	{
		return !(this.GetAttachPrefab(item.m_dropPrefab) == null) && !this.IsUnsupported(item) && this.IsSupported(item) && this.m_supportedTypes.Contains(item.m_shared.m_itemType);
	}

	public bool IsUnsupported(ItemDrop.ItemData item)
	{
		using (List<ItemDrop>.Enumerator enumerator = this.m_unsupportedItems.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_itemData.m_shared.m_name == item.m_shared.m_name)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsSupported(ItemDrop.ItemData item)
	{
		if (this.m_supportedItems.Count == 0)
		{
			return true;
		}
		using (List<ItemDrop>.Enumerator enumerator = this.m_supportedItems.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_itemData.m_shared.m_name == item.m_shared.m_name)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool HaveAttachment()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetString("item", "") != "";
	}

	public string GetAttachedItem()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		return this.m_nview.GetZDO().GetString("item", "");
	}

	public ZNetView m_netViewOverride;

	public string m_name = "";

	public Transform m_attachOther;

	public Transform m_dropSpawnPoint;

	public bool m_canBeRemoved = true;

	public bool m_autoAttach;

	public List<ItemDrop.ItemData.ItemType> m_supportedTypes = new List<ItemDrop.ItemData.ItemType>();

	public List<ItemDrop> m_unsupportedItems = new List<ItemDrop>();

	public List<ItemDrop> m_supportedItems = new List<ItemDrop>();

	public EffectList m_effects = new EffectList();

	public EffectList m_destroyEffects = new EffectList();

	[Header("Guardian power")]
	public float m_powerActivationDelay = 2f;

	public StatusEffect m_guardianPower;

	public EffectList m_activatePowerEffects = new EffectList();

	public EffectList m_activatePowerEffectsPlayer = new EffectList();

	private string m_visualName = "";

	private int m_visualVariant;

	private GameObject m_visualItem;

	private string m_currentItemName = "";

	private ItemDrop.ItemData m_queuedItem;

	private ZNetView m_nview;
}
