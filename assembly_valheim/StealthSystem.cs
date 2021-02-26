using System;
using UnityEngine;

public class StealthSystem : MonoBehaviour
{
	public static StealthSystem instance
	{
		get
		{
			return StealthSystem.m_instance;
		}
	}

	private void Awake()
	{
		StealthSystem.m_instance = this;
	}

	private void OnDestroy()
	{
		StealthSystem.m_instance = null;
	}

	public float GetLightFactor(Vector3 point)
	{
		float lightLevel = this.GetLightLevel(point);
		return Utils.LerpStep(this.m_minLightLevel, this.m_maxLightLevel, lightLevel);
	}

	public float GetLightLevel(Vector3 point)
	{
		if (Time.time - this.m_lastLightListUpdate > 1f)
		{
			this.m_lastLightListUpdate = Time.time;
			this.m_allLights = UnityEngine.Object.FindObjectsOfType<Light>();
		}
		float num = RenderSettings.ambientIntensity * RenderSettings.ambientLight.grayscale;
		foreach (Light light in this.m_allLights)
		{
			if (!(light == null))
			{
				if (light.type == LightType.Directional)
				{
					float num2 = 1f;
					if (light.shadows != LightShadows.None && (Physics.Raycast(point - light.transform.forward * 1000f, light.transform.forward, 1000f, this.m_shadowTestMask) || Physics.Raycast(point, -light.transform.forward, 1000f, this.m_shadowTestMask)))
					{
						num2 = 1f - light.shadowStrength;
					}
					float num3 = light.intensity * light.color.grayscale * num2;
					num += num3;
				}
				else
				{
					float num4 = Vector3.Distance(light.transform.position, point);
					if (num4 <= light.range)
					{
						float num5 = 1f;
						if (light.shadows != LightShadows.None)
						{
							Vector3 vector = point - light.transform.position;
							if (Physics.Raycast(light.transform.position, vector.normalized, vector.magnitude, this.m_shadowTestMask) || Physics.Raycast(point, -vector.normalized, vector.magnitude, this.m_shadowTestMask))
							{
								num5 = 1f - light.shadowStrength;
							}
						}
						float num6 = 1f - num4 / light.range;
						float num7 = light.intensity * light.color.grayscale * num6 * num5;
						num += num7;
					}
				}
			}
		}
		return num;
	}

	private static StealthSystem m_instance;

	public LayerMask m_shadowTestMask;

	public float m_minLightLevel = 0.2f;

	public float m_maxLightLevel = 1.6f;

	private Light[] m_allLights;

	private float m_lastLightListUpdate;

	private const float m_lightUpdateInterval = 1f;
}
