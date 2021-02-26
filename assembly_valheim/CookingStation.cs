using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CookingStation : MonoBehaviour, Interactable, Hoverable
{
	private void Awake()
	{
		this.m_nview = base.gameObject.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_ps = new ParticleSystem[this.m_slots.Length];
		this.m_as = new AudioSource[this.m_slots.Length];
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			this.m_ps[i] = this.m_slots[i].GetComponentInChildren<ParticleSystem>();
			this.m_as[i] = this.m_slots[i].GetComponentInChildren<AudioSource>();
		}
		this.m_nview.Register("RemoveDoneItem", new Action<long>(this.RPC_RemoveDoneItem));
		this.m_nview.Register<string>("AddItem", new Action<long, string>(this.RPC_AddItem));
		this.m_nview.Register<int, string>("SetSlotVisual", new Action<long, int, string>(this.RPC_SetSlotVisual));
		base.InvokeRepeating("UpdateCooking", 0f, 1f);
	}

	private void UpdateCooking()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner() && this.IsFireLit())
		{
			for (int i = 0; i < this.m_slots.Length; i++)
			{
				string text;
				float num;
				this.GetSlot(i, out text, out num);
				if (text != "" && text != this.m_overCookedItem.name)
				{
					CookingStation.ItemConversion itemConversion = this.GetItemConversion(text);
					if (text == null)
					{
						this.SetSlot(i, "", 0f);
					}
					else
					{
						num += 1f;
						if (num > itemConversion.m_cookTime * 2f)
						{
							this.m_overcookedEffect.Create(this.m_slots[i].position, Quaternion.identity, null, 1f);
							this.SetSlot(i, this.m_overCookedItem.name, num);
						}
						else if (num > itemConversion.m_cookTime && text == itemConversion.m_from.name)
						{
							this.m_doneEffect.Create(this.m_slots[i].position, Quaternion.identity, null, 1f);
							this.SetSlot(i, itemConversion.m_to.name, num);
						}
						else
						{
							this.SetSlot(i, text, num);
						}
					}
				}
			}
		}
		this.UpdateVisual();
	}

	private void UpdateVisual()
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			string item;
			float num;
			this.GetSlot(i, out item, out num);
			this.SetSlotVisual(i, item);
		}
	}

	private void RPC_SetSlotVisual(long sender, int slot, string item)
	{
		this.SetSlotVisual(slot, item);
	}

	private void SetSlotVisual(int i, string item)
	{
		if (item == "")
		{
			this.m_ps[i].emission.enabled = false;
			this.m_as[i].mute = true;
			if (this.m_slots[i].childCount > 0)
			{
				UnityEngine.Object.Destroy(this.m_slots[i].GetChild(0).gameObject);
				return;
			}
		}
		else
		{
			this.m_ps[i].emission.enabled = true;
			this.m_as[i].mute = false;
			if (this.m_slots[i].childCount == 0 || this.m_slots[i].GetChild(0).name != item)
			{
				if (this.m_slots[i].childCount > 0)
				{
					UnityEngine.Object.Destroy(this.m_slots[i].GetChild(0).gameObject);
				}
				Component component = ObjectDB.instance.GetItemPrefab(item).transform.Find("attach");
				Transform transform = this.m_slots[i];
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(component.gameObject, transform.position, transform.rotation, transform);
				gameObject.name = item;
				Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					componentsInChildren[j].shadowCastingMode = ShadowCastingMode.Off;
				}
			}
		}
	}

	private void RPC_RemoveDoneItem(long sender)
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			string text;
			float num;
			this.GetSlot(i, out text, out num);
			if (text != "" && this.IsItemDone(text))
			{
				this.SpawnItem(text);
				this.SetSlot(i, "", 0f);
				this.m_nview.InvokeRPC(ZNetView.Everybody, "SetSlotVisual", new object[]
				{
					i,
					""
				});
				return;
			}
		}
	}

	private bool HaveDoneItem()
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			string text;
			float num;
			this.GetSlot(i, out text, out num);
			if (text != "" && this.IsItemDone(text))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsItemDone(string itemName)
	{
		if (itemName == this.m_overCookedItem.name)
		{
			return true;
		}
		CookingStation.ItemConversion itemConversion = this.GetItemConversion(itemName);
		return itemConversion != null && itemName == itemConversion.m_to.name;
	}

	private void SpawnItem(string name)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		Vector3 vector = base.transform.position + Vector3.up * this.m_spawnOffset;
		Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
		UnityEngine.Object.Instantiate<GameObject>(itemPrefab, vector, rotation).GetComponent<Rigidbody>().velocity = Vector3.up * this.m_spawnForce;
		this.m_pickEffector.Create(vector, Quaternion.identity, null, 1f);
	}

	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_cstand_cook\n[<color=yellow><b>1-8</b></color>] $piece_cstand_cook");
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
		if (this.HaveDoneItem())
		{
			this.m_nview.InvokeRPC("RemoveDoneItem", Array.Empty<object>());
			return true;
		}
		ItemDrop.ItemData itemData = this.FindCookableItem(user.GetInventory());
		if (itemData == null)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_nocookitems", 0, null);
			return false;
		}
		this.UseItem(user, itemData);
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (!this.IsFireLit())
		{
			user.Message(MessageHud.MessageType.Center, "$msg_needfire", 0, null);
			return false;
		}
		if (this.GetFreeSlot() == -1)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_nocookroom", 0, null);
			return false;
		}
		return this.CookItem(user.GetInventory(), item);
	}

	private bool IsFireLit()
	{
		return EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Burning, 0.25f);
	}

	private ItemDrop.ItemData FindCookableItem(Inventory inventory)
	{
		foreach (CookingStation.ItemConversion itemConversion in this.m_conversion)
		{
			ItemDrop.ItemData item = inventory.GetItem(itemConversion.m_from.m_itemData.m_shared.m_name);
			if (item != null)
			{
				return item;
			}
		}
		return null;
	}

	private bool CookItem(Inventory inventory, ItemDrop.ItemData item)
	{
		string name = item.m_dropPrefab.name;
		if (!this.m_nview.HasOwner())
		{
			this.m_nview.ClaimOwnership();
		}
		if (!this.IsItemAllowed(item))
		{
			return false;
		}
		if (this.GetFreeSlot() == -1)
		{
			return false;
		}
		inventory.RemoveOneItem(item);
		this.m_nview.InvokeRPC("AddItem", new object[]
		{
			name
		});
		return true;
	}

	private void RPC_AddItem(long sender, string itemName)
	{
		if (!this.IsItemAllowed(itemName))
		{
			return;
		}
		int freeSlot = this.GetFreeSlot();
		if (freeSlot == -1)
		{
			return;
		}
		this.SetSlot(freeSlot, itemName, 0f);
		this.m_nview.InvokeRPC(ZNetView.Everybody, "SetSlotVisual", new object[]
		{
			freeSlot,
			itemName
		});
		this.m_addEffect.Create(this.m_slots[freeSlot].position, Quaternion.identity, null, 1f);
	}

	private void SetSlot(int slot, string itemName, float cookedTime)
	{
		this.m_nview.GetZDO().Set("slot" + slot, itemName);
		this.m_nview.GetZDO().Set("slot" + slot, cookedTime);
	}

	private void GetSlot(int slot, out string itemName, out float cookedTime)
	{
		itemName = this.m_nview.GetZDO().GetString("slot" + slot, "");
		cookedTime = this.m_nview.GetZDO().GetFloat("slot" + slot, 0f);
	}

	private int GetFreeSlot()
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			if (this.m_nview.GetZDO().GetString("slot" + i, "") == "")
			{
				return i;
			}
		}
		return -1;
	}

	private bool IsItemAllowed(ItemDrop.ItemData item)
	{
		return this.IsItemAllowed(item.m_dropPrefab.name);
	}

	private bool IsItemAllowed(string itemName)
	{
		using (List<CookingStation.ItemConversion>.Enumerator enumerator = this.m_conversion.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_from.gameObject.name == itemName)
				{
					return true;
				}
			}
		}
		return false;
	}

	private CookingStation.ItemConversion GetItemConversion(string itemName)
	{
		foreach (CookingStation.ItemConversion itemConversion in this.m_conversion)
		{
			if (itemConversion.m_from.gameObject.name == itemName || itemConversion.m_to.gameObject.name == itemName)
			{
				return itemConversion;
			}
		}
		return null;
	}

	private const float cookDelta = 1f;

	public EffectList m_addEffect = new EffectList();

	public EffectList m_doneEffect = new EffectList();

	public EffectList m_overcookedEffect = new EffectList();

	public EffectList m_pickEffector = new EffectList();

	public float m_spawnOffset = 0.5f;

	public float m_spawnForce = 5f;

	public ItemDrop m_overCookedItem;

	public List<CookingStation.ItemConversion> m_conversion = new List<CookingStation.ItemConversion>();

	public Transform[] m_slots;

	public string m_name = "";

	private ZNetView m_nview;

	private ParticleSystem[] m_ps;

	private AudioSource[] m_as;

	[Serializable]
	public class ItemConversion
	{
		public ItemDrop m_from;

		public ItemDrop m_to;

		public float m_cookTime = 10f;
	}
}
