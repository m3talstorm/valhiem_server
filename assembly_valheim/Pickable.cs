using System;
using UnityEngine;

public class Pickable : MonoBehaviour, Hoverable, Interactable
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo == null)
		{
			return;
		}
		this.m_nview.Register<bool>("SetPicked", new Action<long, bool>(this.RPC_SetPicked));
		this.m_nview.Register("Pick", new Action<long>(this.RPC_Pick));
		this.m_picked = zdo.GetBool("picked", false);
		this.SetPicked(this.m_picked);
		if (this.m_respawnTimeMinutes > 0)
		{
			base.InvokeRepeating("UpdateRespawn", UnityEngine.Random.Range(1f, 5f), 60f);
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
		if (!string.IsNullOrEmpty(this.m_overrideName))
		{
			return this.m_overrideName;
		}
		return this.m_itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
	}

	private void UpdateRespawn()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_picked)
		{
			return;
		}
		long @long = this.m_nview.GetZDO().GetLong("picked_time", 0L);
		DateTime d = new DateTime(@long);
		if ((ZNet.instance.GetTime() - d).TotalMinutes > (double)this.m_respawnTimeMinutes)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "SetPicked", new object[]
			{
				false
			});
		}
	}

	public bool Interact(Humanoid character, bool repeat)
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		this.m_nview.InvokeRPC("Pick", Array.Empty<object>());
		return this.m_useInteractAnimation;
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
		Vector3 pos = this.m_pickEffectAtSpawnPoint ? (base.transform.position + Vector3.up * this.m_spawnOffset) : base.transform.position;
		this.m_pickEffector.Create(pos, Quaternion.identity, null, 1f);
		int num = 0;
		for (int i = 0; i < this.m_amount; i++)
		{
			this.Drop(this.m_itemPrefab, num++, 1);
		}
		if (!this.m_extraDrops.IsEmpty())
		{
			foreach (ItemDrop.ItemData itemData in this.m_extraDrops.GetDropListItems())
			{
				this.Drop(itemData.m_dropPrefab, num++, itemData.m_stack);
			}
		}
		this.m_nview.InvokeRPC(ZNetView.Everybody, "SetPicked", new object[]
		{
			true
		});
	}

	private void RPC_SetPicked(long sender, bool picked)
	{
		this.SetPicked(picked);
	}

	private void SetPicked(bool picked)
	{
		this.m_picked = picked;
		if (this.m_hideWhenPicked)
		{
			this.m_hideWhenPicked.SetActive(!picked);
		}
		if (this.m_nview.IsOwner())
		{
			if (this.m_respawnTimeMinutes > 0 || this.m_hideWhenPicked != null)
			{
				this.m_nview.GetZDO().Set("picked", this.m_picked);
				if (picked && this.m_respawnTimeMinutes > 0)
				{
					DateTime time = ZNet.instance.GetTime();
					this.m_nview.GetZDO().Set("picked_time", time.Ticks);
					return;
				}
			}
			else if (picked)
			{
				this.m_nview.Destroy();
			}
		}
	}

	private void Drop(GameObject prefab, int offset, int stack)
	{
		Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.2f;
		Vector3 position = base.transform.position + Vector3.up * this.m_spawnOffset + new Vector3(vector.x, 0.5f * (float)offset, vector.y);
		Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, position, rotation);
		gameObject.GetComponent<ItemDrop>().m_itemData.m_stack = stack;
		gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public GameObject m_hideWhenPicked;

	public GameObject m_itemPrefab;

	public int m_amount = 1;

	public DropTable m_extraDrops = new DropTable();

	public string m_overrideName = "";

	public int m_respawnTimeMinutes;

	public float m_spawnOffset = 0.5f;

	public EffectList m_pickEffector = new EffectList();

	public bool m_pickEffectAtSpawnPoint;

	public bool m_useInteractAnimation;

	private ZNetView m_nview;

	private bool m_picked;
}
