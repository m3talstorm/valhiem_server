using System;
using UnityEngine;

public class LocationProxy : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.SpawnLocation();
	}

	public void SetLocation(string location, int seed, bool spawnNow, int pgw)
	{
		int stableHashCode = location.GetStableHashCode();
		this.m_nview.GetZDO().Set("location", stableHashCode);
		this.m_nview.GetZDO().Set("seed", seed);
		this.m_nview.GetZDO().SetPGWVersion(pgw);
		if (spawnNow)
		{
			this.SpawnLocation();
		}
	}

	private bool SpawnLocation()
	{
		int @int = this.m_nview.GetZDO().GetInt("location", 0);
		int int2 = this.m_nview.GetZDO().GetInt("seed", 0);
		if (@int == 0)
		{
			return false;
		}
		this.m_instance = ZoneSystem.instance.SpawnProxyLocation(@int, int2, base.transform.position, base.transform.rotation);
		if (this.m_instance == null)
		{
			return false;
		}
		this.m_instance.transform.SetParent(base.transform, true);
		return true;
	}

	private GameObject m_instance;

	private ZNetView m_nview;
}
