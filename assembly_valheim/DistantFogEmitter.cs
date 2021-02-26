using System;
using UnityEngine;

public class DistantFogEmitter : MonoBehaviour
{
	public void SetEmit(bool emit)
	{
		this.m_emit = emit;
	}

	private void Update()
	{
		if (!this.m_emit)
		{
			return;
		}
		if (WorldGenerator.instance == null)
		{
			return;
		}
		this.m_placeTimer += Time.deltaTime;
		if (this.m_placeTimer > this.m_interval)
		{
			this.m_placeTimer = 0f;
			int num = Mathf.Max(0, this.m_particles - this.TotalNrOfParticles());
			num /= 4;
			for (int i = 0; i < num; i++)
			{
				this.PlaceOne();
			}
		}
	}

	private int TotalNrOfParticles()
	{
		int num = 0;
		foreach (ParticleSystem particleSystem in this.m_psystems)
		{
			num += particleSystem.particleCount;
		}
		return num;
	}

	private void PlaceOne()
	{
		Vector3 a;
		if (this.GetRandomPoint(base.transform.position, out a))
		{
			ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
			emitParams.position = a + Vector3.up * this.m_placeOffset;
			this.m_psystems[UnityEngine.Random.Range(0, this.m_psystems.Length)].Emit(emitParams, 1);
		}
	}

	private bool GetRandomPoint(Vector3 center, out Vector3 p)
	{
		float f = UnityEngine.Random.value * 3.1415927f * 2f;
		float num = Mathf.Sqrt(UnityEngine.Random.value) * (this.m_maxRadius - this.m_minRadius) + this.m_minRadius;
		p = center + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
		p.y = WorldGenerator.instance.GetHeight(p.x, p.z);
		if (p.y < ZoneSystem.instance.m_waterLevel)
		{
			if (this.m_skipWater)
			{
				return false;
			}
			if (UnityEngine.Random.value > this.m_waterSpawnChance)
			{
				return false;
			}
			p.y = ZoneSystem.instance.m_waterLevel;
		}
		else if (p.y > this.m_mountainLimit)
		{
			if (UnityEngine.Random.value > this.m_mountainSpawnChance)
			{
				return false;
			}
		}
		else if (UnityEngine.Random.value > this.m_landSpawnChance)
		{
			return false;
		}
		return true;
	}

	public float m_interval = 1f;

	public float m_minRadius = 100f;

	public float m_maxRadius = 500f;

	public float m_mountainSpawnChance = 1f;

	public float m_landSpawnChance = 0.5f;

	public float m_waterSpawnChance = 0.25f;

	public float m_mountainLimit = 120f;

	public float m_emitStep = 10f;

	public int m_emitPerStep = 10;

	public int m_particles = 100;

	public float m_placeOffset = 1f;

	public ParticleSystem[] m_psystems;

	public bool m_skipWater;

	private float m_placeTimer;

	private bool m_emit = true;

	private Vector3 m_lastPosition = Vector3.zero;
}
