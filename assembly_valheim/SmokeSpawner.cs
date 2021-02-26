using System;
using UnityEngine;

public class SmokeSpawner : MonoBehaviour
{
	private void Start()
	{
		this.m_time = UnityEngine.Random.Range(0f, this.m_interval);
	}

	private void Update()
	{
		this.m_time += Time.deltaTime;
		if (this.m_time > this.m_interval)
		{
			this.m_time = 0f;
			this.Spawn();
		}
	}

	private void Spawn()
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || Vector3.Distance(localPlayer.transform.position, base.transform.position) > 64f)
		{
			this.m_lastSpawnTime = Time.time;
			return;
		}
		if (this.TestBlocked())
		{
			return;
		}
		if (Smoke.GetTotalSmoke() > 100)
		{
			Smoke.FadeOldest();
		}
		UnityEngine.Object.Instantiate<GameObject>(this.m_smokePrefab, base.transform.position, UnityEngine.Random.rotation);
		this.m_lastSpawnTime = Time.time;
	}

	private bool TestBlocked()
	{
		return Physics.CheckSphere(base.transform.position, this.m_testRadius, this.m_testMask.value);
	}

	public bool IsBlocked()
	{
		if (!base.gameObject.activeInHierarchy)
		{
			return this.TestBlocked();
		}
		return Time.time - this.m_lastSpawnTime > 4f;
	}

	private const float m_minPlayerDistance = 64f;

	private const int m_maxGlobalSmoke = 100;

	private const float m_blockedMinTime = 4f;

	public GameObject m_smokePrefab;

	public float m_interval = 0.5f;

	public LayerMask m_testMask;

	public float m_testRadius = 0.5f;

	private float m_lastSpawnTime;

	private float m_time;
}
