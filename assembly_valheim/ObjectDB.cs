using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDB : MonoBehaviour
{
	public static ObjectDB instance
	{
		get
		{
			return ObjectDB.m_instance;
		}
	}

	private void Awake()
	{
		ObjectDB.m_instance = this;
		this.UpdateItemHashes();
	}

	public void CopyOtherDB(ObjectDB other)
	{
		this.m_items = other.m_items;
		this.m_recipes = other.m_recipes;
		this.m_StatusEffects = other.m_StatusEffects;
		this.UpdateItemHashes();
	}

	private void UpdateItemHashes()
	{
		this.m_itemByHash.Clear();
		foreach (GameObject gameObject in this.m_items)
		{
			this.m_itemByHash.Add(gameObject.name.GetStableHashCode(), gameObject);
		}
	}

	public StatusEffect GetStatusEffect(string name)
	{
		foreach (StatusEffect statusEffect in this.m_StatusEffects)
		{
			if (statusEffect.name == name)
			{
				return statusEffect;
			}
		}
		return null;
	}

	public GameObject GetItemPrefab(string name)
	{
		foreach (GameObject gameObject in this.m_items)
		{
			if (gameObject.name == name)
			{
				return gameObject;
			}
		}
		return null;
	}

	public GameObject GetItemPrefab(int hash)
	{
		GameObject result;
		if (this.m_itemByHash.TryGetValue(hash, out result))
		{
			return result;
		}
		return null;
	}

	public int GetPrefabHash(GameObject prefab)
	{
		return prefab.name.GetStableHashCode();
	}

	public List<ItemDrop> GetAllItems(ItemDrop.ItemData.ItemType type, string startWith)
	{
		List<ItemDrop> list = new List<ItemDrop>();
		foreach (GameObject gameObject in this.m_items)
		{
			ItemDrop component = gameObject.GetComponent<ItemDrop>();
			if (component.m_itemData.m_shared.m_itemType == type && component.gameObject.name.StartsWith(startWith))
			{
				list.Add(component);
			}
		}
		return list;
	}

	public Recipe GetRecipe(ItemDrop.ItemData item)
	{
		foreach (Recipe recipe in this.m_recipes)
		{
			if (!(recipe.m_item == null) && recipe.m_item.m_itemData.m_shared.m_name == item.m_shared.m_name)
			{
				return recipe;
			}
		}
		return null;
	}

	private static ObjectDB m_instance;

	public List<StatusEffect> m_StatusEffects = new List<StatusEffect>();

	public List<GameObject> m_items = new List<GameObject>();

	public List<Recipe> m_recipes = new List<Recipe>();

	private Dictionary<int, GameObject> m_itemByHash = new Dictionary<int, GameObject>();
}
