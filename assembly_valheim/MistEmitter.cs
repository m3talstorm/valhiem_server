using System;
using UnityEngine;

public class MistEmitter : MonoBehaviour
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
		this.m_placeTimer += Time.deltaTime;
		if (this.m_placeTimer > this.m_interval)
		{
			this.m_placeTimer = 0f;
			this.PlaceOne();
		}
	}

	private void PlaceOne()
	{
		Vector3 vector;
		if (this.GetRandomPoint(base.transform.position, this.m_totalRadius, out vector))
		{
			int num = 0;
			float num2 = 6.2831855f / (float)this.m_rays;
			for (int i = 0; i < this.m_rays; i++)
			{
				float angle = (float)i * num2;
				if ((double)this.GetPointOnEdge(vector, angle, this.m_testRadius).y < (double)vector.y - 0.1)
				{
					num++;
				}
			}
			if (num > this.m_rays / 4)
			{
				return;
			}
			if (EffectArea.IsPointInsideArea(vector, EffectArea.Type.Fire, this.m_testRadius))
			{
				return;
			}
			ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
			emitParams.position = vector + Vector3.up * this.m_placeOffset;
			this.m_psystem.Emit(emitParams, 1);
		}
	}

	private bool GetRandomPoint(Vector3 center, float radius, out Vector3 p)
	{
		float f = UnityEngine.Random.value * 3.1415927f * 2f;
		float num = UnityEngine.Random.Range(0f, radius);
		p = center + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
		float num2;
		if (!ZoneSystem.instance.GetGroundHeight(p, out num2))
		{
			return false;
		}
		if (num2 < ZoneSystem.instance.m_waterLevel)
		{
			return false;
		}
		p.y = num2;
		return true;
	}

	private Vector3 GetPointOnEdge(Vector3 center, float angle, float radius)
	{
		Vector3 vector = center + new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
		vector.y = ZoneSystem.instance.GetGroundHeight(vector);
		if (vector.y < ZoneSystem.instance.m_waterLevel)
		{
			vector.y = ZoneSystem.instance.m_waterLevel;
		}
		return vector;
	}

	public float m_interval = 1f;

	public float m_totalRadius = 30f;

	public float m_testRadius = 5f;

	public int m_rays = 10;

	public float m_placeOffset = 1f;

	public ParticleSystem m_psystem;

	private float m_placeTimer;

	private bool m_emit = true;
}
