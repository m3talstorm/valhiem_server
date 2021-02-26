using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftingStation : MonoBehaviour, Hoverable, Interactable
{
	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview && this.m_nview.GetZDO() == null)
		{
			return;
		}
		CraftingStation.m_allStations.Add(this);
		if (this.m_areaMarker)
		{
			this.m_areaMarker.SetActive(false);
		}
		if (this.m_craftRequireFire)
		{
			base.InvokeRepeating("CheckFire", 1f, 1f);
		}
	}

	private void OnDestroy()
	{
		CraftingStation.m_allStations.Remove(this);
	}

	public bool Interact(Humanoid user, bool repeat)
	{
		if (repeat)
		{
			return false;
		}
		if (user == Player.m_localPlayer)
		{
			if (!this.InUseDistance(user))
			{
				return false;
			}
			Player player = user as Player;
			if (this.CheckUsable(player, true))
			{
				player.SetCraftingStation(this);
				InventoryGui.instance.Show(null);
				return false;
			}
		}
		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public bool CheckUsable(Player player, bool showMessage)
	{
		if (this.m_craftRequireRoof)
		{
			float num;
			bool flag;
			Cover.GetCoverForPoint(this.m_roofCheckPoint.position, out num, out flag);
			if (!flag)
			{
				if (showMessage)
				{
					player.Message(MessageHud.MessageType.Center, "$msg_stationneedroof", 0, null);
				}
				return false;
			}
			if (num < 0.7f)
			{
				if (showMessage)
				{
					player.Message(MessageHud.MessageType.Center, "$msg_stationtooexposed", 0, null);
				}
				return false;
			}
		}
		if (this.m_craftRequireFire && !this.m_haveFire)
		{
			if (showMessage)
			{
				player.Message(MessageHud.MessageType.Center, "$msg_needfire", 0, null);
			}
			return false;
		}
		return true;
	}

	public string GetHoverText()
	{
		if (!this.InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=grey>$piece_toofar</color>");
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use ");
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public void ShowAreaMarker()
	{
		if (this.m_areaMarker)
		{
			this.m_areaMarker.SetActive(true);
			base.CancelInvoke("HideMarker");
			base.Invoke("HideMarker", 0.5f);
			this.PokeInUse();
		}
	}

	private void HideMarker()
	{
		this.m_areaMarker.SetActive(false);
	}

	public static void UpdateKnownStationsInRange(Player player)
	{
		Vector3 position = player.transform.position;
		foreach (CraftingStation craftingStation in CraftingStation.m_allStations)
		{
			if (Vector3.Distance(craftingStation.transform.position, position) < craftingStation.m_discoverRange)
			{
				player.AddKnownStation(craftingStation);
			}
		}
	}

	private void FixedUpdate()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		this.m_useTimer += Time.fixedDeltaTime;
		this.m_updateExtensionTimer += Time.fixedDeltaTime;
		if (this.m_inUseObject)
		{
			this.m_inUseObject.SetActive(this.m_useTimer < 1f);
		}
	}

	private void CheckFire()
	{
		this.m_haveFire = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Burning, 0.25f);
		if (this.m_haveFireObject)
		{
			this.m_haveFireObject.SetActive(this.m_haveFire);
		}
	}

	public void PokeInUse()
	{
		this.m_useTimer = 0f;
		this.TriggerExtensionEffects();
	}

	public static CraftingStation GetCraftingStation(Vector3 point)
	{
		if (CraftingStation.m_triggerMask == 0)
		{
			CraftingStation.m_triggerMask = LayerMask.GetMask(new string[]
			{
				"character_trigger"
			});
		}
		foreach (Collider collider in Physics.OverlapSphere(point, 0.1f, CraftingStation.m_triggerMask, QueryTriggerInteraction.Collide))
		{
			if (collider.gameObject.CompareTag("StationUseArea"))
			{
				CraftingStation componentInParent = collider.GetComponentInParent<CraftingStation>();
				if (componentInParent != null)
				{
					return componentInParent;
				}
			}
		}
		return null;
	}

	public static CraftingStation HaveBuildStationInRange(string name, Vector3 point)
	{
		foreach (CraftingStation craftingStation in CraftingStation.m_allStations)
		{
			if (!(craftingStation.m_name != name))
			{
				float rangeBuild = craftingStation.m_rangeBuild;
				if (Vector3.Distance(craftingStation.transform.position, point) < rangeBuild)
				{
					return craftingStation;
				}
			}
		}
		return null;
	}

	public static void FindStationsInRange(string name, Vector3 point, float range, List<CraftingStation> stations)
	{
		foreach (CraftingStation craftingStation in CraftingStation.m_allStations)
		{
			if (!(craftingStation.m_name != name) && Vector3.Distance(craftingStation.transform.position, point) < range)
			{
				stations.Add(craftingStation);
			}
		}
	}

	public static CraftingStation FindClosestStationInRange(string name, Vector3 point, float range)
	{
		CraftingStation craftingStation = null;
		float num = 99999f;
		foreach (CraftingStation craftingStation2 in CraftingStation.m_allStations)
		{
			if (!(craftingStation2.m_name != name))
			{
				float num2 = Vector3.Distance(craftingStation2.transform.position, point);
				if (num2 < range && (num2 < num || craftingStation == null))
				{
					craftingStation = craftingStation2;
					num = num2;
				}
			}
		}
		return craftingStation;
	}

	private List<StationExtension> GetExtensions()
	{
		if (this.m_updateExtensionTimer > 2f)
		{
			this.m_updateExtensionTimer = 0f;
			this.m_attachedExtensions.Clear();
			StationExtension.FindExtensions(this, base.transform.position, this.m_attachedExtensions);
		}
		return this.m_attachedExtensions;
	}

	private void TriggerExtensionEffects()
	{
		Vector3 connectionEffectPoint = this.GetConnectionEffectPoint();
		foreach (StationExtension stationExtension in this.GetExtensions())
		{
			if (stationExtension)
			{
				stationExtension.StartConnectionEffect(connectionEffectPoint);
			}
		}
	}

	public Vector3 GetConnectionEffectPoint()
	{
		if (this.m_connectionPoint)
		{
			return this.m_connectionPoint.position;
		}
		return base.transform.position;
	}

	public int GetLevel()
	{
		return 1 + this.GetExtensions().Count;
	}

	public bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, base.transform.position) < this.m_useDistance;
	}

	public string m_name = "";

	public Sprite m_icon;

	public float m_discoverRange = 4f;

	public float m_rangeBuild = 10f;

	public bool m_craftRequireRoof = true;

	public bool m_craftRequireFire = true;

	public Transform m_roofCheckPoint;

	public Transform m_connectionPoint;

	public bool m_showBasicRecipies;

	public float m_useDistance = 2f;

	public int m_useAnimation;

	public GameObject m_areaMarker;

	public GameObject m_inUseObject;

	public GameObject m_haveFireObject;

	public EffectList m_craftItemEffects = new EffectList();

	public EffectList m_craftItemDoneEffects = new EffectList();

	public EffectList m_repairItemDoneEffects = new EffectList();

	private const float m_updateExtensionInterval = 2f;

	private float m_updateExtensionTimer;

	private float m_useTimer = 10f;

	private bool m_haveFire;

	private ZNetView m_nview;

	private List<StationExtension> m_attachedExtensions = new List<StationExtension>();

	private static List<CraftingStation> m_allStations = new List<CraftingStation>();

	private static int m_triggerMask = 0;
}
