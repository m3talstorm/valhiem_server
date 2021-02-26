using System;
using UnityEngine;

public class Plant : SlowUpdate, Hoverable
{
	public override void Awake()
	{
		base.Awake();
		this.m_nview = base.gameObject.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong("plantTime", 0L) == 0L)
		{
			this.m_nview.GetZDO().Set("plantTime", ZNet.instance.GetTime().Ticks);
		}
		this.m_spawnTime = Time.time;
	}

	public string GetHoverText()
	{
		switch (this.m_status)
		{
		case Plant.Status.Healthy:
			return Localization.instance.Localize(this.m_name + " ( $piece_plant_healthy )");
		case Plant.Status.NoSun:
			return Localization.instance.Localize(this.m_name + " ( $piece_plant_nosun )");
		case Plant.Status.NoSpace:
			return Localization.instance.Localize(this.m_name + " ( $piece_plant_nospace )");
		case Plant.Status.WrongBiome:
			return Localization.instance.Localize(this.m_name + " ( $piece_plant_wrongbiome )");
		case Plant.Status.NotCultivated:
			return Localization.instance.Localize(this.m_name + " ( $piece_plant_notcultivated )");
		default:
			return "";
		}
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_name);
	}

	private double TimeSincePlanted()
	{
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong("plantTime", ZNet.instance.GetTime().Ticks));
		return (ZNet.instance.GetTime() - d).TotalSeconds;
	}

	public override void SUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (Time.time - this.m_updateTime < 10f)
		{
			return;
		}
		this.m_updateTime = Time.time;
		double num = this.TimeSincePlanted();
		this.UpdateHealth(num);
		float growTime = this.GetGrowTime();
		if (this.m_healthyGrown)
		{
			bool flag = num > (double)(growTime * 0.5f);
			this.m_healthy.SetActive(!flag && this.m_status == Plant.Status.Healthy);
			this.m_unhealthy.SetActive(!flag && this.m_status > Plant.Status.Healthy);
			this.m_healthyGrown.SetActive(flag && this.m_status == Plant.Status.Healthy);
			this.m_unhealthyGrown.SetActive(flag && this.m_status > Plant.Status.Healthy);
		}
		else
		{
			this.m_healthy.SetActive(this.m_status == Plant.Status.Healthy);
			this.m_unhealthy.SetActive(this.m_status > Plant.Status.Healthy);
		}
		if (this.m_nview.IsOwner() && Time.time - this.m_spawnTime > 10f && num > (double)growTime)
		{
			this.Grow();
		}
	}

	private float GetGrowTime()
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState((int)((ulong)this.m_nview.GetZDO().m_uid.id + (ulong)this.m_nview.GetZDO().m_uid.userID));
		float value = UnityEngine.Random.value;
		UnityEngine.Random.state = state;
		return Mathf.Lerp(this.m_growTime, this.m_growTimeMax, value);
	}

	private void Grow()
	{
		if (this.m_status != Plant.Status.Healthy)
		{
			if (this.m_destroyIfCantGrow)
			{
				this.Destroy();
			}
			return;
		}
		GameObject original = this.m_grownPrefabs[UnityEngine.Random.Range(0, this.m_grownPrefabs.Length)];
		Quaternion quaternion = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, base.transform.position, quaternion);
		ZNetView component = gameObject.GetComponent<ZNetView>();
		float num = UnityEngine.Random.Range(this.m_minScale, this.m_maxScale);
		component.SetLocalScale(new Vector3(num, num, num));
		TreeBase component2 = gameObject.GetComponent<TreeBase>();
		if (component2)
		{
			component2.Grow();
		}
		this.m_nview.Destroy();
		this.m_growEffect.Create(base.transform.position, quaternion, null, num);
	}

	private void UpdateHealth(double timeSincePlanted)
	{
		if (timeSincePlanted < 10.0)
		{
			this.m_status = Plant.Status.Healthy;
			return;
		}
		Heightmap heightmap = Heightmap.FindHeightmap(base.transform.position);
		if (heightmap)
		{
			if ((heightmap.GetBiome(base.transform.position) & this.m_biome) == Heightmap.Biome.None)
			{
				this.m_status = Plant.Status.WrongBiome;
				return;
			}
			if (this.m_needCultivatedGround && !heightmap.IsCultivated(base.transform.position))
			{
				this.m_status = Plant.Status.NotCultivated;
				return;
			}
		}
		if (this.HaveRoof())
		{
			this.m_status = Plant.Status.NoSun;
			return;
		}
		if (!this.HaveGrowSpace())
		{
			this.m_status = Plant.Status.NoSpace;
			return;
		}
		this.m_status = Plant.Status.Healthy;
	}

	private void Destroy()
	{
		IDestructible component = base.GetComponent<IDestructible>();
		if (component != null)
		{
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = 9999f;
			component.Damage(hitData);
		}
	}

	private bool HaveRoof()
	{
		if (Plant.m_roofMask == 0)
		{
			Plant.m_roofMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"piece"
			});
		}
		return Physics.Raycast(base.transform.position, Vector3.up, 100f, Plant.m_roofMask);
	}

	private bool HaveGrowSpace()
	{
		if (Plant.m_spaceMask == 0)
		{
			Plant.m_spaceMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"piece_nonsolid"
			});
		}
		Collider[] array = Physics.OverlapSphere(base.transform.position, this.m_growRadius, Plant.m_spaceMask);
		for (int i = 0; i < array.Length; i++)
		{
			Plant component = array[i].GetComponent<Plant>();
			if (!component || (!(component == this) && component.GetStatus() == Plant.Status.Healthy))
			{
				return false;
			}
		}
		return true;
	}

	private Plant.Status GetStatus()
	{
		return this.m_status;
	}

	public string m_name = "Plant";

	public float m_growTime = 10f;

	public float m_growTimeMax = 2000f;

	public GameObject[] m_grownPrefabs = new GameObject[0];

	public float m_minScale = 1f;

	public float m_maxScale = 1f;

	public float m_growRadius = 1f;

	public bool m_needCultivatedGround;

	public bool m_destroyIfCantGrow;

	[SerializeField]
	private GameObject m_healthy;

	[SerializeField]
	private GameObject m_unhealthy;

	[SerializeField]
	private GameObject m_healthyGrown;

	[SerializeField]
	private GameObject m_unhealthyGrown;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome;

	public EffectList m_growEffect = new EffectList();

	private Plant.Status m_status;

	private ZNetView m_nview;

	private float m_updateTime;

	private float m_spawnTime;

	private static int m_spaceMask;

	private static int m_roofMask;

	private enum Status
	{
		Healthy,
		NoSun,
		NoSpace,
		WrongBiome,
		NotCultivated
	}
}
