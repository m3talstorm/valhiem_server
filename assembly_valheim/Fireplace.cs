using System;
using UnityEngine;

public class Fireplace : MonoBehaviour, Hoverable, Interactable
{
	public void Awake()
	{
		this.m_nview = base.gameObject.GetComponent<ZNetView>();
		this.m_piece = base.gameObject.GetComponent<Piece>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (Fireplace.m_solidRayMask == 0)
		{
			Fireplace.m_solidRayMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"terrain"
			});
		}
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetFloat("fuel", -1f) == -1f)
		{
			this.m_nview.GetZDO().Set("fuel", this.m_startFuel);
			if (this.m_startFuel > 0f)
			{
				this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, null, 1f);
			}
		}
		this.m_nview.Register("AddFuel", new Action<long>(this.RPC_AddFuel));
		base.InvokeRepeating("UpdateFireplace", 0f, 2f);
		base.InvokeRepeating("CheckEnv", 4f, 4f);
	}

	private void Start()
	{
		if (this.m_playerBaseObject && this.m_piece)
		{
			this.m_playerBaseObject.SetActive(this.m_piece.IsPlacedByPlayer());
		}
	}

	private double GetTimeSinceLastUpdate()
	{
		DateTime time = ZNet.instance.GetTime();
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong("lastTime", time.Ticks));
		TimeSpan timeSpan = time - d;
		this.m_nview.GetZDO().Set("lastTime", time.Ticks);
		double num = timeSpan.TotalSeconds;
		if (num < 0.0)
		{
			num = 0.0;
		}
		return num;
	}

	private void UpdateFireplace()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			float num = this.m_nview.GetZDO().GetFloat("fuel", 0f);
			double timeSinceLastUpdate = this.GetTimeSinceLastUpdate();
			if (this.IsBurning())
			{
				float num2 = (float)(timeSinceLastUpdate / (double)this.m_secPerFuel);
				num -= num2;
				if (num <= 0f)
				{
					num = 0f;
				}
				this.m_nview.GetZDO().Set("fuel", num);
			}
		}
		this.UpdateState();
	}

	private void CheckEnv()
	{
		this.CheckUnderTerrain();
		if (this.m_enabledObjectLow != null && this.m_enabledObjectHigh != null)
		{
			this.CheckWet();
		}
	}

	private void CheckUnderTerrain()
	{
		this.m_blocked = false;
		float num;
		if (Heightmap.GetHeight(base.transform.position, out num) && num > base.transform.position.y + this.m_checkTerrainOffset)
		{
			this.m_blocked = true;
			return;
		}
		RaycastHit raycastHit;
		if (Physics.Raycast(base.transform.position + Vector3.up * this.m_coverCheckOffset, Vector3.up, out raycastHit, 0.5f, Fireplace.m_solidRayMask))
		{
			this.m_blocked = true;
			return;
		}
		if (this.m_smokeSpawner && this.m_smokeSpawner.IsBlocked())
		{
			this.m_blocked = true;
			return;
		}
	}

	private void CheckWet()
	{
		float num;
		bool flag;
		Cover.GetCoverForPoint(base.transform.position + Vector3.up * this.m_coverCheckOffset, out num, out flag);
		this.m_wet = false;
		if (EnvMan.instance.GetWindIntensity() >= 0.8f && num < 0.7f)
		{
			this.m_wet = true;
		}
		if (EnvMan.instance.IsWet() && !flag)
		{
			this.m_wet = true;
		}
	}

	private void UpdateState()
	{
		if (this.IsBurning())
		{
			this.m_enabledObject.SetActive(true);
			if (this.m_enabledObjectHigh && this.m_enabledObjectLow)
			{
				this.m_enabledObjectHigh.SetActive(!this.m_wet);
				this.m_enabledObjectLow.SetActive(this.m_wet);
				return;
			}
		}
		else
		{
			this.m_enabledObject.SetActive(false);
			if (this.m_enabledObjectHigh && this.m_enabledObjectLow)
			{
				this.m_enabledObjectLow.SetActive(false);
				this.m_enabledObjectHigh.SetActive(false);
			}
		}
	}

	public string GetHoverText()
	{
		float @float = this.m_nview.GetZDO().GetFloat("fuel", 0f);
		return Localization.instance.Localize(string.Concat(new object[]
		{
			this.m_name,
			" ( $piece_fire_fuel ",
			Mathf.Ceil(@float),
			"/",
			(int)this.m_maxFuel,
			" )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use ",
			this.m_fuelItem.m_itemData.m_shared.m_name,
			"\n[<color=yellow><b>1-8</b></color>] $piece_useitem"
		}));
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
		if (!this.m_nview.HasOwner())
		{
			this.m_nview.ClaimOwnership();
		}
		Inventory inventory = user.GetInventory();
		if (inventory == null)
		{
			return true;
		}
		if (!inventory.HaveItem(this.m_fuelItem.m_itemData.m_shared.m_name))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_outof " + this.m_fuelItem.m_itemData.m_shared.m_name, 0, null);
			return false;
		}
		if ((float)Mathf.CeilToInt(this.m_nview.GetZDO().GetFloat("fuel", 0f)) >= this.m_maxFuel)
		{
			user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_cantaddmore", new string[]
			{
				this.m_fuelItem.m_itemData.m_shared.m_name
			}), 0, null);
			return false;
		}
		user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_fireadding", new string[]
		{
			this.m_fuelItem.m_itemData.m_shared.m_name
		}), 0, null);
		inventory.RemoveItem(this.m_fuelItem.m_itemData.m_shared.m_name, 1);
		this.m_nview.InvokeRPC("AddFuel", Array.Empty<object>());
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (item.m_shared.m_name == this.m_fuelItem.m_itemData.m_shared.m_name)
		{
			if ((float)Mathf.CeilToInt(this.m_nview.GetZDO().GetFloat("fuel", 0f)) >= this.m_maxFuel)
			{
				user.Message(MessageHud.MessageType.Center, "$msg_cantaddmore " + item.m_shared.m_name, 0, null);
				return true;
			}
			Inventory inventory = user.GetInventory();
			user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_fireadding", new string[]
			{
				item.m_shared.m_name
			}), 0, null);
			inventory.RemoveItem(item, 1);
			this.m_nview.InvokeRPC("AddFuel", Array.Empty<object>());
			return true;
		}
		else
		{
			if (!(this.m_fireworkItem != null) || !(item.m_shared.m_name == this.m_fireworkItem.m_itemData.m_shared.m_name))
			{
				return false;
			}
			if (!this.IsBurning())
			{
				user.Message(MessageHud.MessageType.Center, "$msg_firenotburning", 0, null);
				return true;
			}
			if (user.GetInventory().CountItems(this.m_fireworkItem.m_itemData.m_shared.m_name) < this.m_fireworkItems)
			{
				user.Message(MessageHud.MessageType.Center, "$msg_toofew " + this.m_fireworkItem.m_itemData.m_shared.m_name, 0, null);
				return true;
			}
			user.GetInventory().RemoveItem(item.m_shared.m_name, this.m_fireworkItems);
			user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_throwinfire", new string[]
			{
				item.m_shared.m_name
			}), 0, null);
			ZNetScene.instance.SpawnObject(base.transform.position, Quaternion.identity, this.m_fireworks);
			this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, null, 1f);
			return true;
		}
	}

	private void RPC_AddFuel(long sender)
	{
		if (this.m_nview.IsOwner())
		{
			float num = this.m_nview.GetZDO().GetFloat("fuel", 0f);
			if ((float)Mathf.CeilToInt(num) >= this.m_maxFuel)
			{
				return;
			}
			num = Mathf.Clamp(num, 0f, this.m_maxFuel);
			num += 1f;
			num = Mathf.Clamp(num, 0f, this.m_maxFuel);
			this.m_nview.GetZDO().Set("fuel", num);
			this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, null, 1f);
			this.UpdateState();
		}
	}

	public bool CanBeRemoved()
	{
		return !this.IsBurning();
	}

	public bool IsBurning()
	{
		if (this.m_blocked)
		{
			return false;
		}
		float waterLevel = WaterVolume.GetWaterLevel(this.m_enabledObject.transform.position, 1f);
		return this.m_enabledObject.transform.position.y >= waterLevel && this.m_nview.GetZDO().GetFloat("fuel", 0f) > 0f;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.white;
		Gizmos.DrawWireSphere(base.transform.position + Vector3.up * this.m_coverCheckOffset, 0.5f);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * this.m_checkTerrainOffset, new Vector3(1f, 0.01f, 1f));
	}

	private ZNetView m_nview;

	private Piece m_piece;

	[Header("Fire")]
	public string m_name = "Fire";

	public float m_startFuel = 3f;

	public float m_maxFuel = 10f;

	public float m_secPerFuel = 3f;

	public float m_checkTerrainOffset = 0.2f;

	public float m_coverCheckOffset = 0.5f;

	private const float m_minimumOpenSpace = 0.5f;

	public GameObject m_enabledObject;

	public GameObject m_enabledObjectLow;

	public GameObject m_enabledObjectHigh;

	public GameObject m_playerBaseObject;

	public ItemDrop m_fuelItem;

	public SmokeSpawner m_smokeSpawner;

	public EffectList m_fuelAddedEffects = new EffectList();

	[Header("Fireworks")]
	public ItemDrop m_fireworkItem;

	public int m_fireworkItems = 2;

	public GameObject m_fireworks;

	private bool m_blocked;

	private bool m_wet;

	private static int m_solidRayMask;
}
