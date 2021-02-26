using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ParticleDecal : MonoBehaviour
{
	private void Awake()
	{
		this.part = base.GetComponent<ParticleSystem>();
		this.collisionEvents = new List<ParticleCollisionEvent>();
	}

	private void OnParticleCollision(GameObject other)
	{
		if (this.m_chance < 100f && UnityEngine.Random.Range(0f, 100f) > this.m_chance)
		{
			return;
		}
		int num = this.part.GetCollisionEvents(other, this.collisionEvents);
		for (int i = 0; i < num; i++)
		{
			ParticleCollisionEvent particleCollisionEvent = this.collisionEvents[i];
			Vector3 eulerAngles = Quaternion.LookRotation(particleCollisionEvent.normal).eulerAngles;
			eulerAngles.x = -eulerAngles.x + 180f;
			eulerAngles.y = -eulerAngles.y;
			eulerAngles.z = (float)UnityEngine.Random.Range(0, 360);
			ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
			emitParams.position = particleCollisionEvent.intersection;
			emitParams.rotation3D = eulerAngles;
			emitParams.velocity = -particleCollisionEvent.normal * 0.001f;
			this.m_decalSystem.Emit(emitParams, 1);
		}
	}

	public ParticleSystem m_decalSystem;

	[Range(0f, 100f)]
	public float m_chance = 100f;

	private ParticleSystem part;

	private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
}
