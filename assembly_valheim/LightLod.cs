using System;
using System.Collections;
using UnityEngine;

public class LightLod : MonoBehaviour
{
	private void Awake()
	{
		this.m_light = base.GetComponent<Light>();
		this.m_baseRange = this.m_light.range;
		this.m_baseShadowStrength = this.m_light.shadowStrength;
		if (this.m_shadowLod && this.m_light.shadows == LightShadows.None)
		{
			this.m_shadowLod = false;
		}
		if (this.m_lightLod)
		{
			this.m_light.range = 0f;
			this.m_light.enabled = false;
		}
		if (this.m_shadowLod)
		{
			this.m_light.shadowStrength = 0f;
			this.m_light.shadows = LightShadows.None;
		}
	}

	private void OnEnable()
	{
		base.StartCoroutine("UpdateLoop");
	}

	private IEnumerator UpdateLoop()
	{
		for (;;)
		{
			Camera mainCamera = Utils.GetMainCamera();
			if (mainCamera && this.m_light)
			{
				float distance = Vector3.Distance(mainCamera.transform.position, base.transform.position);
				if (this.m_lightLod)
				{
					if (distance < this.m_lightDistance)
					{
						while (this.m_light)
						{
							if (this.m_light.range >= this.m_baseRange && this.m_light.enabled)
							{
								break;
							}
							this.m_light.enabled = true;
							this.m_light.range = Mathf.Min(this.m_baseRange, this.m_light.range + Time.deltaTime * this.m_baseRange);
							yield return null;
						}
					}
					else
					{
						while (this.m_light && (this.m_light.range > 0f || this.m_light.enabled))
						{
							this.m_light.range = Mathf.Max(0f, this.m_light.range - Time.deltaTime * this.m_baseRange);
							if (this.m_light.range <= 0f)
							{
								this.m_light.enabled = false;
							}
							yield return null;
						}
					}
				}
				if (this.m_shadowLod)
				{
					if (distance < this.m_shadowDistance)
					{
						while (this.m_light)
						{
							if (this.m_light.shadowStrength >= this.m_baseShadowStrength && this.m_light.shadows != LightShadows.None)
							{
								break;
							}
							this.m_light.shadows = LightShadows.Soft;
							this.m_light.shadowStrength = Mathf.Min(this.m_baseShadowStrength, this.m_light.shadowStrength + Time.deltaTime * this.m_baseShadowStrength);
							yield return null;
						}
					}
					else
					{
						while (this.m_light && (this.m_light.shadowStrength > 0f || this.m_light.shadows != LightShadows.None))
						{
							this.m_light.shadowStrength = Mathf.Max(0f, this.m_light.shadowStrength - Time.deltaTime * this.m_baseShadowStrength);
							if (this.m_light.shadowStrength <= 0f)
							{
								this.m_light.shadows = LightShadows.None;
							}
							yield return null;
						}
					}
				}
			}
			yield return new WaitForSeconds(1f);
		}
		yield break;
	}

	public bool m_lightLod = true;

	public float m_lightDistance = 40f;

	public bool m_shadowLod = true;

	public float m_shadowDistance = 20f;

	private Light m_light;

	private float m_baseRange;

	private float m_baseShadowStrength;
}
