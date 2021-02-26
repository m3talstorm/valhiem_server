using System;
using UnityEngine;

public class GlobalWind : MonoBehaviour
{
	private void Start()
	{
		if (EnvMan.instance == null)
		{
			return;
		}
		this.m_ps = base.GetComponent<ParticleSystem>();
		this.m_cloth = base.GetComponent<Cloth>();
		if (this.m_checkPlayerShelter)
		{
			this.m_player = base.GetComponentInParent<Player>();
		}
		if (this.m_smoothUpdate)
		{
			base.InvokeRepeating("UpdateWind", 0f, 0.01f);
			return;
		}
		base.InvokeRepeating("UpdateWind", UnityEngine.Random.Range(1.5f, 2.5f), 2f);
		this.UpdateWind();
	}

	private void UpdateWind()
	{
		if (this.m_alignToWindDirection)
		{
			Vector3 windDir = EnvMan.instance.GetWindDir();
			base.transform.rotation = Quaternion.LookRotation(windDir, Vector3.up);
		}
		if (this.m_ps)
		{
			if (!this.m_ps.emission.enabled)
			{
				return;
			}
			Vector3 windForce = EnvMan.instance.GetWindForce();
			if (this.m_particleVelocity)
			{
				ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = this.m_ps.velocityOverLifetime;
				velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
				velocityOverLifetime.x = windForce.x * this.m_multiplier;
				velocityOverLifetime.z = windForce.z * this.m_multiplier;
			}
			if (this.m_particleForce)
			{
				ParticleSystem.ForceOverLifetimeModule forceOverLifetime = this.m_ps.forceOverLifetime;
				forceOverLifetime.space = ParticleSystemSimulationSpace.World;
				forceOverLifetime.x = windForce.x * this.m_multiplier;
				forceOverLifetime.z = windForce.z * this.m_multiplier;
			}
			if (this.m_particleEmission)
			{
				this.m_ps.emission.rateOverTimeMultiplier = Mathf.Lerp((float)this.m_particleEmissionMin, (float)this.m_particleEmissionMax, EnvMan.instance.GetWindIntensity());
			}
		}
		if (this.m_cloth)
		{
			Vector3 a = EnvMan.instance.GetWindForce();
			if (this.m_checkPlayerShelter && this.m_player != null && this.m_player.InShelter())
			{
				a = Vector3.zero;
			}
			this.m_cloth.externalAcceleration = a * this.m_multiplier;
			this.m_cloth.randomAcceleration = a * this.m_multiplier * this.m_clothRandomAccelerationFactor;
		}
	}

	public float m_multiplier = 1f;

	public bool m_smoothUpdate;

	public bool m_alignToWindDirection;

	[Header("Particles")]
	public bool m_particleVelocity = true;

	public bool m_particleForce;

	public bool m_particleEmission;

	public int m_particleEmissionMin;

	public int m_particleEmissionMax = 1;

	[Header("Cloth")]
	public float m_clothRandomAccelerationFactor = 0.5f;

	public bool m_checkPlayerShelter;

	private ParticleSystem m_ps;

	private Cloth m_cloth;

	private Player m_player;
}
