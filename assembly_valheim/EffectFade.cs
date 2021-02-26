using System;
using UnityEngine;

public class EffectFade : MonoBehaviour
{
	private void Awake()
	{
		this.m_particles = base.gameObject.GetComponentsInChildren<ParticleSystem>();
		this.m_light = base.gameObject.GetComponentInChildren<Light>();
		this.m_audioSource = base.gameObject.GetComponentInChildren<AudioSource>();
		if (this.m_light)
		{
			this.m_lightBaseIntensity = this.m_light.intensity;
			this.m_light.intensity = 0f;
		}
		if (this.m_audioSource)
		{
			this.m_baseVolume = this.m_audioSource.volume;
			this.m_audioSource.volume = 0f;
		}
		this.SetActive(false);
	}

	private void Update()
	{
		this.m_intensity = Mathf.MoveTowards(this.m_intensity, this.m_active ? 1f : 0f, Time.deltaTime / this.m_fadeDuration);
		if (this.m_light)
		{
			this.m_light.intensity = this.m_intensity * this.m_lightBaseIntensity;
			this.m_light.enabled = (this.m_light.intensity > 0f);
		}
		if (this.m_audioSource)
		{
			this.m_audioSource.volume = this.m_intensity * this.m_baseVolume;
		}
	}

	public void SetActive(bool active)
	{
		if (this.m_active == active)
		{
			return;
		}
		this.m_active = active;
		ParticleSystem[] particles = this.m_particles;
		for (int i = 0; i < particles.Length; i++)
		{
			particles[i].emission.enabled = active;
		}
	}

	public float m_fadeDuration = 1f;

	private ParticleSystem[] m_particles;

	private Light m_light;

	private AudioSource m_audioSource;

	private float m_baseVolume;

	private float m_lightBaseIntensity;

	private bool m_active = true;

	private float m_intensity;
}
