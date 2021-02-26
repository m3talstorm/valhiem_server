using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnArea : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		base.InvokeRepeating("UpdateSpawn", 2f, 2f);
	}

	private void UpdateSpawn()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (ZNetScene.instance.OutsideActiveArea(base.transform.position))
		{
			return;
		}
		if (!Player.IsPlayerInRange(base.transform.position, this.m_triggerDistance))
		{
			return;
		}
		this.m_spawnTimer += 2f;
		if (this.m_spawnTimer > this.m_spawnIntervalSec)
		{
			this.m_spawnTimer = 0f;
			this.SpawnOne();
		}
	}

	private bool SpawnOne()
	{
		int num;
		int num2;
		this.GetInstances(out num, out num2);
		if (num >= this.m_maxNear || num2 >= this.m_maxTotal)
		{
			return false;
		}
		SpawnArea.SpawnData spawnData = this.SelectWeightedPrefab();
		if (spawnData == null)
		{
			return false;
		}
		Vector3 position;
		if (!this.FindSpawnPoint(spawnData.m_prefab, out position))
		{
			return false;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(spawnData.m_prefab, position, Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f));
		if (this.m_setPatrolSpawnPoint)
		{
			BaseAI component = gameObject.GetComponent<BaseAI>();
			if (component != null)
			{
				component.SetPatrolPoint();
			}
		}
		Character component2 = gameObject.GetComponent<Character>();
		if (spawnData.m_maxLevel > 1)
		{
			int num3 = spawnData.m_minLevel;
			while (num3 < spawnData.m_maxLevel && UnityEngine.Random.Range(0f, 100f) <= this.m_levelupChance)
			{
				num3++;
			}
			if (num3 > 1)
			{
				component2.SetLevel(num3);
			}
		}
		Vector3 centerPoint = component2.GetCenterPoint();
		this.m_spawnEffects.Create(centerPoint, Quaternion.identity, null, 1f);
		return true;
	}

	private bool FindSpawnPoint(GameObject prefab, out Vector3 point)
	{
		prefab.GetComponent<BaseAI>();
		for (int i = 0; i < 10; i++)
		{
			Vector3 vector = base.transform.position + Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * UnityEngine.Random.Range(0f, this.m_spawnRadius);
			float num;
			if (ZoneSystem.instance.FindFloor(vector, out num) && (!this.m_onGroundOnly || !ZoneSystem.instance.IsBlocked(vector)))
			{
				vector.y = num + 0.1f;
				point = vector;
				return true;
			}
		}
		point = Vector3.zero;
		return false;
	}

	private SpawnArea.SpawnData SelectWeightedPrefab()
	{
		if (this.m_prefabs.Count == 0)
		{
			return null;
		}
		float num = 0f;
		foreach (SpawnArea.SpawnData spawnData in this.m_prefabs)
		{
			num += spawnData.m_weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		foreach (SpawnArea.SpawnData spawnData2 in this.m_prefabs)
		{
			num3 += spawnData2.m_weight;
			if (num2 <= num3)
			{
				return spawnData2;
			}
		}
		return this.m_prefabs[this.m_prefabs.Count - 1];
	}

	private void GetInstances(out int near, out int total)
	{
		near = 0;
		total = 0;
		Vector3 position = base.transform.position;
		foreach (BaseAI baseAI in BaseAI.GetAllInstances())
		{
			if (this.IsSpawnPrefab(baseAI.gameObject))
			{
				float num = Utils.DistanceXZ(baseAI.transform.position, position);
				if (num < this.m_nearRadius)
				{
					near++;
				}
				if (num < this.m_farRadius)
				{
					total++;
				}
			}
		}
	}

	private bool IsSpawnPrefab(GameObject go)
	{
		string name = go.name;
		foreach (SpawnArea.SpawnData spawnData in this.m_prefabs)
		{
			if (name.StartsWith(spawnData.m_prefab.name))
			{
				return true;
			}
		}
		return false;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position, this.m_spawnRadius);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position, this.m_nearRadius);
	}

	private const float dt = 2f;

	public List<SpawnArea.SpawnData> m_prefabs = new List<SpawnArea.SpawnData>();

	public float m_levelupChance = 15f;

	public float m_spawnIntervalSec = 30f;

	public float m_triggerDistance = 256f;

	public bool m_setPatrolSpawnPoint = true;

	public float m_spawnRadius = 2f;

	public float m_nearRadius = 10f;

	public float m_farRadius = 1000f;

	public int m_maxNear = 3;

	public int m_maxTotal = 20;

	public bool m_onGroundOnly;

	public EffectList m_spawnEffects = new EffectList();

	private ZNetView m_nview;

	private float m_spawnTimer;

	[Serializable]
	public class SpawnData
	{
		public GameObject m_prefab;

		public float m_weight;

		[Header("Level")]
		public int m_maxLevel = 1;

		public int m_minLevel = 1;
	}
}
