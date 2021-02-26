using System;
using UnityEngine;

public class PickableItem : MonoBehaviour, Hoverable, Interactable
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.SetupRandomPrefab();
		this.m_nview.Register("Pick", new Action<long>(this.RPC_Pick));
		this.SetupItem(true);
	}

	private void SetupRandomPrefab()
	{
		if (this.m_itemPrefab == null && this.m_randomItemPrefabs.Length != 0)
		{
			int @int = this.m_nview.GetZDO().GetInt("itemPrefab", 0);
			if (@int == 0)
			{
				if (this.m_nview.IsOwner())
				{
					PickableItem.RandomItem randomItem = this.m_randomItemPrefabs[UnityEngine.Random.Range(0, this.m_randomItemPrefabs.Length)];
					this.m_itemPrefab = randomItem.m_itemPrefab;
					this.m_stack = UnityEngine.Random.Range(randomItem.m_stackMin, randomItem.m_stackMax + 1);
					int prefabHash = ObjectDB.instance.GetPrefabHash(this.m_itemPrefab.gameObject);
					this.m_nview.GetZDO().Set("itemPrefab", prefabHash);
					this.m_nview.GetZDO().Set("itemStack", this.m_stack);
					return;
				}
				return;
			}
			else
			{
				GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(@int);
				if (itemPrefab == null)
				{
					ZLog.LogError(string.Concat(new object[]
					{
						"Failed to find saved prefab ",
						@int,
						" in PickableItem ",
						base.gameObject.name
					}));
					return;
				}
				this.m_itemPrefab = itemPrefab.GetComponent<ItemDrop>();
				this.m_stack = this.m_nview.GetZDO().GetInt("itemStack", 0);
			}
		}
	}

	public string GetHoverText()
	{
		if (this.m_picked)
		{
			return "";
		}
		return Localization.instance.Localize(this.GetHoverName() + "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup");
	}

	public string GetHoverName()
	{
		if (!this.m_itemPrefab)
		{
			return "None";
		}
		int stackSize = this.GetStackSize();
		if (stackSize > 1)
		{
			return this.m_itemPrefab.m_itemData.m_shared.m_name + " x " + stackSize;
		}
		return this.m_itemPrefab.m_itemData.m_shared.m_name;
	}

	public bool Interact(Humanoid character, bool repeat)
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		this.m_nview.InvokeRPC("Pick", Array.Empty<object>());
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void RPC_Pick(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_picked)
		{
			return;
		}
		this.m_picked = true;
		this.m_pickEffector.Create(base.transform.position, Quaternion.identity, null, 1f);
		this.Drop();
		this.m_nview.Destroy();
	}

	private void Drop()
	{
		Vector3 position = base.transform.position + Vector3.up * 0.2f;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_itemPrefab.gameObject, position, base.transform.rotation);
		gameObject.GetComponent<ItemDrop>().m_itemData.m_stack = this.GetStackSize();
		gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
	}

	private int GetStackSize()
	{
		return Mathf.Clamp((this.m_stack > 0) ? this.m_stack : this.m_itemPrefab.m_itemData.m_stack, 1, this.m_itemPrefab.m_itemData.m_shared.m_maxStackSize);
	}

	private GameObject GetAttachPrefab()
	{
		Transform transform = this.m_itemPrefab.transform.Find("attach");
		if (transform)
		{
			return transform.gameObject;
		}
		return null;
	}

	private void SetupItem(bool enabled)
	{
		if (!enabled)
		{
			if (this.m_instance)
			{
				UnityEngine.Object.Destroy(this.m_instance);
				this.m_instance = null;
			}
			return;
		}
		if (this.m_instance)
		{
			return;
		}
		if (this.m_itemPrefab == null)
		{
			return;
		}
		GameObject attachPrefab = this.GetAttachPrefab();
		if (attachPrefab == null)
		{
			ZLog.LogWarning("Failed to get attach prefab for item " + this.m_itemPrefab.name);
			return;
		}
		this.m_instance = UnityEngine.Object.Instantiate<GameObject>(attachPrefab, base.transform.position, base.transform.rotation, base.transform);
		this.m_instance.transform.localPosition = attachPrefab.transform.localPosition;
		this.m_instance.transform.localRotation = attachPrefab.transform.localRotation;
	}

	private bool DrawPrefabMesh(ItemDrop prefab)
	{
		if (prefab == null)
		{
			return false;
		}
		bool result = false;
		Gizmos.color = Color.yellow;
		foreach (MeshFilter meshFilter in prefab.gameObject.GetComponentsInChildren<MeshFilter>())
		{
			if (meshFilter && meshFilter.sharedMesh)
			{
				Vector3 position = prefab.transform.position;
				Quaternion lhs = Quaternion.Inverse(prefab.transform.rotation);
				Vector3 point = meshFilter.transform.position - position;
				Vector3 position2 = base.transform.position + base.transform.rotation * point;
				Quaternion rhs = lhs * meshFilter.transform.rotation;
				Quaternion rotation = base.transform.rotation * rhs;
				Gizmos.DrawMesh(meshFilter.sharedMesh, position2, rotation, meshFilter.transform.lossyScale);
				result = true;
			}
		}
		return result;
	}

	public ItemDrop m_itemPrefab;

	public int m_stack;

	public PickableItem.RandomItem[] m_randomItemPrefabs = new PickableItem.RandomItem[0];

	public EffectList m_pickEffector = new EffectList();

	private ZNetView m_nview;

	private GameObject m_instance;

	private bool m_picked;

	[Serializable]
	public struct RandomItem
	{
		public ItemDrop m_itemPrefab;

		public int m_stackMin;

		public int m_stackMax;
	}
}
