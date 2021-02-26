using System;
using UnityEngine;

public class Recipe : ScriptableObject
{
	public int GetRequiredStationLevel(int quality)
	{
		return Mathf.Max(1, this.m_minStationLevel) + (quality - 1);
	}

	public CraftingStation GetRequiredStation(int quality)
	{
		if (this.m_craftingStation)
		{
			return this.m_craftingStation;
		}
		if (quality > 1)
		{
			return this.m_repairStation;
		}
		return null;
	}

	public ItemDrop m_item;

	public int m_amount = 1;

	public bool m_enabled = true;

	[Header("Requirements")]
	public CraftingStation m_craftingStation;

	public CraftingStation m_repairStation;

	public int m_minStationLevel = 1;

	public Piece.Requirement[] m_resources = new Piece.Requirement[0];
}
