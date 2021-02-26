using System;
using UnityEngine;

public class Beehive : MonoBehaviour, Hoverable, Interactable
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong("lastTime", 0L) == 0L)
		{
			this.m_nview.GetZDO().Set("lastTime", ZNet.instance.GetTime().Ticks);
		}
		this.m_nview.Register("Extract", new Action<long>(this.RPC_Extract));
		base.InvokeRepeating("UpdateBees", 0f, 10f);
	}

	public string GetHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		int honeyLevel = this.GetHoneyLevel();
		if (honeyLevel > 0)
		{
			return Localization.instance.Localize(string.Concat(new object[]
			{
				this.m_name,
				" ( ",
				this.m_honeyItem.m_itemData.m_shared.m_name,
				" x ",
				honeyLevel,
				" )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_beehive_extract"
			}));
		}
		return Localization.instance.Localize(this.m_name + " ( $piece_container_empty )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_beehive_check");
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid character, bool repeat)
	{
		if (repeat)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true))
		{
			return true;
		}
		if (this.GetHoneyLevel() > 0)
		{
			this.Extract();
		}
		else
		{
			if (!this.CheckBiome())
			{
				character.Message(MessageHud.MessageType.Center, "$piece_beehive_area", 0, null);
				return true;
			}
			if (!this.HaveFreeSpace())
			{
				character.Message(MessageHud.MessageType.Center, "$piece_beehive_freespace", 0, null);
				return true;
			}
			if (!EnvMan.instance.IsDaylight())
			{
				character.Message(MessageHud.MessageType.Center, "$piece_beehive_sleep", 0, null);
				return true;
			}
			character.Message(MessageHud.MessageType.Center, "$piece_beehive_happy", 0, null);
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void Extract()
	{
		this.m_nview.InvokeRPC("Extract", Array.Empty<object>());
	}

	private void RPC_Extract(long caller)
	{
		int honeyLevel = this.GetHoneyLevel();
		if (honeyLevel > 0)
		{
			this.m_spawnEffect.Create(this.m_spawnPoint.position, Quaternion.identity, null, 1f);
			for (int i = 0; i < honeyLevel; i++)
			{
				Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.5f;
				Vector3 position = this.m_spawnPoint.position + new Vector3(vector.x, 0.25f * (float)i, vector.y);
				UnityEngine.Object.Instantiate<ItemDrop>(this.m_honeyItem, position, Quaternion.identity);
			}
			this.ResetLevel();
		}
	}

	private float GetTimeSinceLastUpdate()
	{
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong("lastTime", ZNet.instance.GetTime().Ticks));
		DateTime time = ZNet.instance.GetTime();
		TimeSpan timeSpan = time - d;
		this.m_nview.GetZDO().Set("lastTime", time.Ticks);
		double num = timeSpan.TotalSeconds;
		if (num < 0.0)
		{
			num = 0.0;
		}
		return (float)num;
	}

	private void ResetLevel()
	{
		this.m_nview.GetZDO().Set("level", 0);
	}

	private void IncreseLevel(int i)
	{
		int num = this.GetHoneyLevel();
		num += i;
		num = Mathf.Clamp(num, 0, this.m_maxHoney);
		this.m_nview.GetZDO().Set("level", num);
	}

	private int GetHoneyLevel()
	{
		return this.m_nview.GetZDO().GetInt("level", 0);
	}

	private void UpdateBees()
	{
		bool flag = this.CheckBiome() && this.HaveFreeSpace();
		bool active = flag && EnvMan.instance.IsDaylight();
		this.m_beeEffect.SetActive(active);
		if (this.m_nview.IsOwner() && flag)
		{
			float timeSinceLastUpdate = this.GetTimeSinceLastUpdate();
			float num = this.m_nview.GetZDO().GetFloat("product", 0f);
			num += timeSinceLastUpdate;
			if (num > this.m_secPerUnit)
			{
				int i = (int)(num / this.m_secPerUnit);
				this.IncreseLevel(i);
				num = 0f;
			}
			this.m_nview.GetZDO().Set("product", num);
		}
	}

	private bool HaveFreeSpace()
	{
		float num;
		bool flag;
		Cover.GetCoverForPoint(this.m_coverPoint.position, out num, out flag);
		return num < this.m_maxCover;
	}

	private bool CheckBiome()
	{
		return (Heightmap.FindBiome(base.transform.position) & this.m_biome) > Heightmap.Biome.None;
	}

	public string m_name = "";

	public Transform m_coverPoint;

	public Transform m_spawnPoint;

	public GameObject m_beeEffect;

	public float m_maxCover = 0.25f;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome;

	public float m_secPerUnit = 10f;

	public int m_maxHoney = 4;

	public ItemDrop m_honeyItem;

	public EffectList m_spawnEffect = new EffectList();

	private ZNetView m_nview;
}
