using System;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour, Hoverable, Interactable
{
	private void Awake()
	{
		this.m_nview = (this.m_rootObjectOverride ? this.m_rootObjectOverride.GetComponent<ZNetView>() : base.GetComponent<ZNetView>());
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_inventory = new Inventory(this.m_name, this.m_bkg, this.m_width, this.m_height);
		Inventory inventory = this.m_inventory;
		inventory.m_onChanged = (Action)Delegate.Combine(inventory.m_onChanged, new Action(this.OnContainerChanged));
		this.m_piece = base.GetComponent<Piece>();
		if (this.m_nview)
		{
			this.m_nview.Register<long>("RequestOpen", new Action<long, long>(this.RPC_RequestOpen));
			this.m_nview.Register<bool>("OpenRespons", new Action<long, bool>(this.RPC_OpenRespons));
			this.m_nview.Register<long>("RequestTakeAll", new Action<long, long>(this.RPC_RequestTakeAll));
			this.m_nview.Register<bool>("TakeAllRespons", new Action<long, bool>(this.RPC_TakeAllRespons));
		}
		WearNTear wearNTear = this.m_rootObjectOverride ? this.m_rootObjectOverride.GetComponent<WearNTear>() : base.GetComponent<WearNTear>();
		if (wearNTear)
		{
			WearNTear wearNTear2 = wearNTear;
			wearNTear2.m_onDestroyed = (Action)Delegate.Combine(wearNTear2.m_onDestroyed, new Action(this.OnDestroyed));
		}
		Destructible destructible = this.m_rootObjectOverride ? this.m_rootObjectOverride.GetComponent<Destructible>() : base.GetComponent<Destructible>();
		if (destructible)
		{
			Destructible destructible2 = destructible;
			destructible2.m_onDestroyed = (Action)Delegate.Combine(destructible2.m_onDestroyed, new Action(this.OnDestroyed));
		}
		if (this.m_nview.IsOwner() && !this.m_nview.GetZDO().GetBool("addedDefaultItems", false))
		{
			this.AddDefaultItems();
			this.m_nview.GetZDO().Set("addedDefaultItems", true);
		}
		base.InvokeRepeating("CheckForChanges", 0f, 1f);
	}

	private void AddDefaultItems()
	{
		foreach (ItemDrop.ItemData item in this.m_defaultItems.GetDropListItems())
		{
			this.m_inventory.AddItem(item);
		}
	}

	private void DropAllItems(GameObject lootContainerPrefab)
	{
		while (this.m_inventory.NrOfItems() > 0)
		{
			Vector3 position = base.transform.position + UnityEngine.Random.insideUnitSphere * 1f;
			UnityEngine.Object.Instantiate<GameObject>(lootContainerPrefab, position, UnityEngine.Random.rotation).GetComponent<Container>().GetInventory().MoveAll(this.m_inventory);
		}
	}

	private void DropAllItems()
	{
		List<ItemDrop.ItemData> allItems = this.m_inventory.GetAllItems();
		int num = 1;
		foreach (ItemDrop.ItemData item in allItems)
		{
			Vector3 position = base.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
			Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
			ItemDrop.DropItem(item, 0, position, rotation);
			num++;
		}
		this.m_inventory.RemoveAll();
		this.Save();
	}

	private void OnDestroyed()
	{
		if (this.m_nview.IsOwner())
		{
			if (this.m_destroyedLootPrefab)
			{
				this.DropAllItems(this.m_destroyedLootPrefab);
				return;
			}
			this.DropAllItems();
		}
	}

	private void CheckForChanges()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.Load();
		this.UpdateUseVisual();
		if (this.m_autoDestroyEmpty && this.m_nview.IsOwner() && !this.IsInUse() && this.m_inventory.NrOfItems() == 0)
		{
			this.m_nview.Destroy();
		}
	}

	private void UpdateUseVisual()
	{
		bool flag;
		if (this.m_nview.IsOwner())
		{
			flag = this.m_inUse;
			this.m_nview.GetZDO().Set("InUse", this.m_inUse ? 1 : 0);
		}
		else
		{
			flag = (this.m_nview.GetZDO().GetInt("InUse", 0) == 1);
		}
		if (this.m_open)
		{
			this.m_open.SetActive(flag);
		}
		if (this.m_closed)
		{
			this.m_closed.SetActive(!flag);
		}
	}

	public string GetHoverText()
	{
		if (this.m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		string text;
		if (this.m_inventory.NrOfItems() == 0)
		{
			text = this.m_name + " ( $piece_container_empty )";
		}
		else
		{
			text = this.m_name;
		}
		text += "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open";
		return Localization.instance.Localize(text);
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, true))
		{
			return true;
		}
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		if (!this.CheckAccess(playerID))
		{
			character.Message(MessageHud.MessageType.Center, "$msg_cantopen", 0, null);
			return true;
		}
		this.m_nview.InvokeRPC("RequestOpen", new object[]
		{
			playerID
		});
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public bool CanBeRemoved()
	{
		return this.m_privacy != Container.PrivacySetting.Private || this.GetInventory().NrOfItems() <= 0;
	}

	private bool CheckAccess(long playerID)
	{
		switch (this.m_privacy)
		{
		case Container.PrivacySetting.Private:
			return this.m_piece.GetCreator() == playerID;
		case Container.PrivacySetting.Group:
			return false;
		case Container.PrivacySetting.Public:
			return true;
		default:
			return false;
		}
	}

	public bool IsOwner()
	{
		return this.m_nview.IsOwner();
	}

	public bool IsInUse()
	{
		return this.m_inUse;
	}

	public void SetInUse(bool inUse)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_inUse == inUse)
		{
			return;
		}
		this.m_inUse = inUse;
		this.UpdateUseVisual();
		if (inUse)
		{
			this.m_openEffects.Create(base.transform.position, base.transform.rotation, null, 1f);
			return;
		}
		this.m_closeEffects.Create(base.transform.position, base.transform.rotation, null, 1f);
	}

	public Inventory GetInventory()
	{
		return this.m_inventory;
	}

	private void RPC_RequestOpen(long uid, long playerID)
	{
		ZLog.Log(string.Concat(new object[]
		{
			"Player ",
			uid,
			" wants to open ",
			base.gameObject.name,
			"   im: ",
			ZDOMan.instance.GetMyID()
		}));
		if (!this.m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
			return;
		}
		if ((this.IsInUse() || (this.m_wagon && this.m_wagon.InUse())) && uid != ZNet.instance.GetUID())
		{
			ZLog.Log("  in use");
			this.m_nview.InvokeRPC(uid, "OpenRespons", new object[]
			{
				false
			});
			return;
		}
		if (!this.CheckAccess(playerID))
		{
			ZLog.Log("  not yours");
			this.m_nview.InvokeRPC(uid, "OpenRespons", new object[]
			{
				false
			});
			return;
		}
		ZDOMan.instance.ForceSendZDO(uid, this.m_nview.GetZDO().m_uid);
		this.m_nview.GetZDO().SetOwner(uid);
		this.m_nview.InvokeRPC(uid, "OpenRespons", new object[]
		{
			true
		});
	}

	private void RPC_OpenRespons(long uid, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			InventoryGui.instance.Show(this);
			return;
		}
		Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, null);
	}

	public bool TakeAll(Humanoid character)
	{
		if (this.m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, true))
		{
			return false;
		}
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		if (!this.CheckAccess(playerID))
		{
			character.Message(MessageHud.MessageType.Center, "$msg_cantopen", 0, null);
			return false;
		}
		this.m_nview.InvokeRPC("RequestTakeAll", new object[]
		{
			playerID
		});
		return true;
	}

	private void RPC_RequestTakeAll(long uid, long playerID)
	{
		ZLog.Log(string.Concat(new object[]
		{
			"Player ",
			uid,
			" wants to takeall from ",
			base.gameObject.name,
			"   im: ",
			ZDOMan.instance.GetMyID()
		}));
		if (!this.m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
			return;
		}
		if ((this.IsInUse() || (this.m_wagon && this.m_wagon.InUse())) && uid != ZNet.instance.GetUID())
		{
			ZLog.Log("  in use");
			this.m_nview.InvokeRPC(uid, "TakeAllRespons", new object[]
			{
				false
			});
			return;
		}
		if (!this.CheckAccess(playerID))
		{
			ZLog.Log("  not yours");
			this.m_nview.InvokeRPC(uid, "TakeAllRespons", new object[]
			{
				false
			});
			return;
		}
		if (Time.time - this.m_lastTakeAllTime < 2f)
		{
			return;
		}
		this.m_lastTakeAllTime = Time.time;
		this.m_nview.InvokeRPC(uid, "TakeAllRespons", new object[]
		{
			true
		});
	}

	private void RPC_TakeAllRespons(long uid, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			this.m_nview.ClaimOwnership();
			ZDOMan.instance.ForceSendZDO(uid, this.m_nview.GetZDO().m_uid);
			Player.m_localPlayer.GetInventory().MoveAll(this.m_inventory);
			if (this.m_onTakeAllSuccess != null)
			{
				this.m_onTakeAllSuccess();
				return;
			}
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, null);
		}
	}

	private void OnContainerChanged()
	{
		if (this.m_loading)
		{
			return;
		}
		if (!this.IsOwner())
		{
			return;
		}
		this.Save();
	}

	private void Save()
	{
		ZPackage zpackage = new ZPackage();
		this.m_inventory.Save(zpackage);
		string @base = zpackage.GetBase64();
		this.m_nview.GetZDO().Set("items", @base);
		this.m_lastRevision = this.m_nview.GetZDO().m_dataRevision;
		this.m_lastDataString = @base;
	}

	private void Load()
	{
		if (this.m_nview.GetZDO().m_dataRevision == this.m_lastRevision)
		{
			return;
		}
		string @string = this.m_nview.GetZDO().GetString("items", "");
		if (string.IsNullOrEmpty(@string) || @string == this.m_lastDataString)
		{
			return;
		}
		ZPackage pkg = new ZPackage(@string);
		this.m_loading = true;
		this.m_inventory.Load(pkg);
		this.m_loading = false;
		this.m_lastRevision = this.m_nview.GetZDO().m_dataRevision;
		this.m_lastDataString = @string;
	}

	private float m_lastTakeAllTime;

	public Action m_onTakeAllSuccess;

	public string m_name = "Container";

	public Sprite m_bkg;

	public int m_width = 3;

	public int m_height = 2;

	public Container.PrivacySetting m_privacy = Container.PrivacySetting.Public;

	public bool m_checkGuardStone;

	public bool m_autoDestroyEmpty;

	public DropTable m_defaultItems = new DropTable();

	public GameObject m_open;

	public GameObject m_closed;

	public EffectList m_openEffects = new EffectList();

	public EffectList m_closeEffects = new EffectList();

	public ZNetView m_rootObjectOverride;

	public Vagon m_wagon;

	public GameObject m_destroyedLootPrefab;

	private Inventory m_inventory;

	private ZNetView m_nview;

	private Piece m_piece;

	private bool m_inUse;

	private bool m_loading;

	private uint m_lastRevision;

	private string m_lastDataString = "";

	public enum PrivacySetting
	{
		Private,
		Group,
		Public
	}
}
