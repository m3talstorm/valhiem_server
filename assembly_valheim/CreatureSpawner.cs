using System;
using UnityEngine;

public class CreatureSpawner : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		base.InvokeRepeating("UpdateSpawner", UnityEngine.Random.Range(3f, 5f), 5f);
	}

	private void UpdateSpawner()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		ZDOID zdoid = this.m_nview.GetZDO().GetZDOID("spawn_id");
		if (this.m_respawnTimeMinuts <= 0f && !zdoid.IsNone())
		{
			return;
		}
		if (!zdoid.IsNone() && ZDOMan.instance.GetZDO(zdoid) != null)
		{
			this.m_nview.GetZDO().Set("alive_time", ZNet.instance.GetTime().Ticks);
			return;
		}
		if (this.m_respawnTimeMinuts > 0f)
		{
			DateTime time = ZNet.instance.GetTime();
			DateTime d = new DateTime(this.m_nview.GetZDO().GetLong("alive_time", 0L));
			if ((time - d).TotalMinutes < (double)this.m_respawnTimeMinuts)
			{
				return;
			}
		}
		if (!this.m_spawnAtDay && EnvMan.instance.IsDay())
		{
			return;
		}
		if (!this.m_spawnAtNight && EnvMan.instance.IsNight())
		{
			return;
		}
		bool requireSpawnArea = this.m_requireSpawnArea;
		if (!this.m_spawnInPlayerBase && EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.PlayerBase, 0f))
		{
			return;
		}
		if (this.m_triggerNoise > 0f)
		{
			if (!Player.IsPlayerInRange(base.transform.position, this.m_triggerDistance, this.m_triggerNoise))
			{
				return;
			}
		}
		else if (!Player.IsPlayerInRange(base.transform.position, this.m_triggerDistance))
		{
			return;
		}
		this.Spawn();
	}

	private bool HasSpawned()
	{
		return !(this.m_nview == null) && this.m_nview.GetZDO() != null && !this.m_nview.GetZDO().GetZDOID("spawn_id").IsNone();
	}

	private ZNetView Spawn()
	{
		Vector3 position = base.transform.position;
		float num;
		if (ZoneSystem.instance.FindFloor(position, out num))
		{
			position.y = num + 0.25f;
		}
		Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_creaturePrefab, position, rotation);
		ZNetView component = gameObject.GetComponent<ZNetView>();
		BaseAI component2 = gameObject.GetComponent<BaseAI>();
		if (component2 != null && this.m_setPatrolSpawnPoint)
		{
			component2.SetPatrolPoint();
		}
		if (this.m_maxLevel > 1)
		{
			Character component3 = gameObject.GetComponent<Character>();
			if (component3)
			{
				int num2 = this.m_minLevel;
				while (num2 < this.m_maxLevel && UnityEngine.Random.Range(0f, 100f) <= this.m_levelupChance)
				{
					num2++;
				}
				if (num2 > 1)
				{
					component3.SetLevel(num2);
				}
			}
		}
		component.GetZDO().SetPGWVersion(this.m_nview.GetZDO().GetPGWVersion());
		this.m_nview.GetZDO().Set("spawn_id", component.GetZDO().m_uid);
		this.m_nview.GetZDO().Set("alive_time", ZNet.instance.GetTime().Ticks);
		this.SpawnEffect(gameObject);
		return component;
	}

	private void SpawnEffect(GameObject spawnedObject)
	{
		Character component = spawnedObject.GetComponent<Character>();
		Vector3 pos = component ? component.GetCenterPoint() : (base.transform.position + Vector3.up * 0.75f);
		this.m_spawnEffects.Create(pos, Quaternion.identity, null, 1f);
	}

	private float GetRadius()
	{
		return 0.75f;
	}

	private void OnDrawGizmos()
	{
	}

	private const float m_radius = 0.75f;

	public GameObject m_creaturePrefab;

	[Header("Level")]
	public int m_maxLevel = 1;

	public int m_minLevel = 1;

	public float m_levelupChance = 15f;

	[Header("Spawn settings")]
	public float m_respawnTimeMinuts = 20f;

	public float m_triggerDistance = 60f;

	public float m_triggerNoise;

	public bool m_spawnAtNight = true;

	public bool m_spawnAtDay = true;

	public bool m_requireSpawnArea;

	public bool m_spawnInPlayerBase;

	public bool m_setPatrolSpawnPoint;

	public EffectList m_spawnEffects = new EffectList();

	private ZNetView m_nview;
}
