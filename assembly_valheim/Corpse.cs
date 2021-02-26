using System;
using System.Collections.Generic;
using UnityEngine;

public class Corpse : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_container = base.GetComponent<Container>();
		this.m_model = base.GetComponentInChildren<SkinnedMeshRenderer>();
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong("timeOfDeath", 0L) == 0L)
		{
			this.m_nview.GetZDO().Set("timeOfDeath", ZNet.instance.GetTime().Ticks);
		}
		base.InvokeRepeating("UpdateDespawn", Corpse.m_updateDt, Corpse.m_updateDt);
	}

	public void SetEquipedItems(List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData itemData in items)
		{
			if (itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest)
			{
				this.m_nview.GetZDO().Set("ChestItem", itemData.m_shared.m_name);
			}
			if (itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs)
			{
				this.m_nview.GetZDO().Set("LegItem", itemData.m_shared.m_name);
			}
		}
	}

	private void UpdateDespawn()
	{
		if (this.m_nview.IsOwner() && !this.m_container.IsInUse())
		{
			if (this.m_container.GetInventory().NrOfItems() <= 0)
			{
				this.m_emptyTimer += Corpse.m_updateDt;
				if (this.m_emptyTimer >= this.m_emptyDespawnDelaySec)
				{
					ZLog.Log("Despawning looted corpse");
					this.m_nview.Destroy();
					return;
				}
			}
			else
			{
				this.m_emptyTimer = 0f;
			}
		}
	}

	private static float m_updateDt = 2f;

	public float m_emptyDespawnDelaySec = 10f;

	public float m_DespawnDelayMin = 20f;

	private float m_emptyTimer;

	private Container m_container;

	private ZNetView m_nview;

	private SkinnedMeshRenderer m_model;
}
