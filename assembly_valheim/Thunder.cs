using System;
using UnityEngine;

public class Thunder : MonoBehaviour
{
	private void Start()
	{
		this.m_strikeTimer = UnityEngine.Random.Range(this.m_strikeIntervalMin, this.m_strikeIntervalMax);
	}

	private void Update()
	{
		if (this.m_strikeTimer > 0f)
		{
			this.m_strikeTimer -= Time.deltaTime;
			if (this.m_strikeTimer <= 0f)
			{
				this.DoFlash();
			}
		}
		if (this.m_thunderTimer > 0f)
		{
			this.m_thunderTimer -= Time.deltaTime;
			if (this.m_thunderTimer <= 0f)
			{
				this.DoThunder();
				this.m_strikeTimer = UnityEngine.Random.Range(this.m_strikeIntervalMin, this.m_strikeIntervalMax);
			}
		}
		if (this.m_spawnThor)
		{
			this.m_thorTimer += Time.deltaTime;
			if (this.m_thorTimer > this.m_thorInterval)
			{
				this.m_thorTimer = 0f;
				if (UnityEngine.Random.value <= this.m_thorChance && (this.m_requiredGlobalKey == "" || ZoneSystem.instance.GetGlobalKey(this.m_requiredGlobalKey)))
				{
					this.SpawnThor();
				}
			}
		}
	}

	private void SpawnThor()
	{
		float num = UnityEngine.Random.value * 6.2831855f;
		Vector3 vector = base.transform.position + new Vector3(Mathf.Sin(num), 0f, Mathf.Cos(num)) * this.m_thorSpawnDistance;
		vector.y += UnityEngine.Random.Range(this.m_thorSpawnAltitudeMin, this.m_thorSpawnAltitudeMax);
		float groundHeight = ZoneSystem.instance.GetGroundHeight(vector);
		if (vector.y < groundHeight)
		{
			vector.y = groundHeight + 50f;
		}
		float f = num + 180f + (float)UnityEngine.Random.Range(-45, 45);
		Vector3 vector2 = base.transform.position + new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f)) * this.m_thorSpawnDistance;
		vector2.y += UnityEngine.Random.Range(this.m_thorSpawnAltitudeMin, this.m_thorSpawnAltitudeMax);
		float groundHeight2 = ZoneSystem.instance.GetGroundHeight(vector2);
		if (vector.y < groundHeight2)
		{
			vector.y = groundHeight2 + 50f;
		}
		Vector3 normalized = (vector2 - vector).normalized;
		UnityEngine.Object.Instantiate<GameObject>(this.m_thorPrefab, vector, Quaternion.LookRotation(normalized));
	}

	private void DoFlash()
	{
		float f = UnityEngine.Random.value * 6.2831855f;
		float d = UnityEngine.Random.Range(this.m_flashDistanceMin, this.m_flashDistanceMax);
		this.m_flashPos = base.transform.position + new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f)) * d;
		this.m_flashPos.y = this.m_flashPos.y + this.m_flashAltitude;
		Quaternion rotation = Quaternion.LookRotation((base.transform.position - this.m_flashPos).normalized);
		GameObject[] array = this.m_flashEffect.Create(this.m_flashPos, Quaternion.identity, null, 1f);
		for (int i = 0; i < array.Length; i++)
		{
			Light[] componentsInChildren = array[i].GetComponentsInChildren<Light>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				componentsInChildren[j].transform.rotation = rotation;
			}
		}
		this.m_thunderTimer = UnityEngine.Random.Range(this.m_thunderDelayMin, this.m_thunderDelayMax);
	}

	private void DoThunder()
	{
		this.m_thunderEffect.Create(this.m_flashPos, Quaternion.identity, null, 1f);
	}

	public float m_strikeIntervalMin = 3f;

	public float m_strikeIntervalMax = 10f;

	public float m_thunderDelayMin = 3f;

	public float m_thunderDelayMax = 5f;

	public float m_flashDistanceMin = 50f;

	public float m_flashDistanceMax = 200f;

	public float m_flashAltitude = 100f;

	public EffectList m_flashEffect = new EffectList();

	public EffectList m_thunderEffect = new EffectList();

	[Header("Thor")]
	public bool m_spawnThor;

	public string m_requiredGlobalKey = "";

	public GameObject m_thorPrefab;

	public float m_thorSpawnDistance = 300f;

	public float m_thorSpawnAltitudeMax = 100f;

	public float m_thorSpawnAltitudeMin = 100f;

	public float m_thorInterval = 10f;

	public float m_thorChance = 1f;

	private Vector3 m_flashPos = Vector3.zero;

	private float m_strikeTimer = -1f;

	private float m_thunderTimer = -1f;

	private float m_thorTimer;
}
