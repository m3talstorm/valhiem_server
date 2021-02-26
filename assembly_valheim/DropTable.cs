using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DropTable
{
	public DropTable Clone()
	{
		return base.MemberwiseClone() as DropTable;
	}

	public List<ItemDrop.ItemData> GetDropListItems()
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
		if (this.m_drops.Count == 0)
		{
			return list;
		}
		if (UnityEngine.Random.value > this.m_dropChance)
		{
			return list;
		}
		List<DropTable.DropData> list2 = new List<DropTable.DropData>(this.m_drops);
		float num = 0f;
		foreach (DropTable.DropData dropData in list2)
		{
			num += dropData.m_weight;
		}
		int num2 = UnityEngine.Random.Range(this.m_dropMin, this.m_dropMax + 1);
		for (int i = 0; i < num2; i++)
		{
			float num3 = UnityEngine.Random.Range(0f, num);
			bool flag = false;
			float num4 = 0f;
			foreach (DropTable.DropData dropData2 in list2)
			{
				num4 += dropData2.m_weight;
				if (num3 <= num4)
				{
					flag = true;
					this.AddItemToList(list, dropData2);
					if (this.m_oneOfEach)
					{
						list2.Remove(dropData2);
						num -= dropData2.m_weight;
						break;
					}
					break;
				}
			}
			if (!flag && list2.Count > 0)
			{
				this.AddItemToList(list, list2[0]);
			}
		}
		return list;
	}

	private void AddItemToList(List<ItemDrop.ItemData> toDrop, DropTable.DropData data)
	{
		ItemDrop.ItemData itemData = data.m_item.GetComponent<ItemDrop>().m_itemData;
		ItemDrop.ItemData itemData2 = itemData.Clone();
		itemData2.m_dropPrefab = data.m_item;
		int min = Mathf.Max(1, data.m_stackMin);
		int num = Mathf.Min(itemData.m_shared.m_maxStackSize, data.m_stackMax);
		itemData2.m_stack = UnityEngine.Random.Range(min, num + 1);
		toDrop.Add(itemData2);
	}

	public List<GameObject> GetDropList()
	{
		int amount = UnityEngine.Random.Range(this.m_dropMin, this.m_dropMax + 1);
		return this.GetDropList(amount);
	}

	private List<GameObject> GetDropList(int amount)
	{
		List<GameObject> list = new List<GameObject>();
		if (this.m_drops.Count == 0)
		{
			return list;
		}
		if (UnityEngine.Random.value > this.m_dropChance)
		{
			return list;
		}
		List<DropTable.DropData> list2 = new List<DropTable.DropData>(this.m_drops);
		float num = 0f;
		foreach (DropTable.DropData dropData in list2)
		{
			num += dropData.m_weight;
		}
		for (int i = 0; i < amount; i++)
		{
			float num2 = UnityEngine.Random.Range(0f, num);
			bool flag = false;
			float num3 = 0f;
			foreach (DropTable.DropData dropData2 in list2)
			{
				num3 += dropData2.m_weight;
				if (num2 <= num3)
				{
					flag = true;
					int num4 = UnityEngine.Random.Range(dropData2.m_stackMin, dropData2.m_stackMax);
					for (int j = 0; j < num4; j++)
					{
						list.Add(dropData2.m_item);
					}
					if (this.m_oneOfEach)
					{
						list2.Remove(dropData2);
						num -= dropData2.m_weight;
						break;
					}
					break;
				}
			}
			if (!flag && list2.Count > 0)
			{
				list.Add(list2[0].m_item);
			}
		}
		return list;
	}

	public bool IsEmpty()
	{
		return this.m_drops.Count == 0;
	}

	public List<DropTable.DropData> m_drops = new List<DropTable.DropData>();

	public int m_dropMin = 1;

	public int m_dropMax = 1;

	[Range(0f, 1f)]
	public float m_dropChance = 1f;

	public bool m_oneOfEach;

	[Serializable]
	public struct DropData
	{
		public GameObject m_item;

		public int m_stackMin;

		public int m_stackMax;

		public float m_weight;
	}
}
