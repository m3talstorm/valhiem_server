using System;
using UnityEngine;

[ExecuteInEditMode]
public class MenuScene : MonoBehaviour
{
	private void Awake()
	{
		Shader.SetGlobalFloat("_Wet", 0f);
	}

	private void Update()
	{
		Shader.SetGlobalVector("_SkyboxSunDir", -this.m_dirLight.transform.forward);
		Shader.SetGlobalVector("_SunDir", -this.m_dirLight.transform.forward);
		Shader.SetGlobalColor("_SunFogColor", this.m_sunFogColor);
		Shader.SetGlobalColor("_SunColor", this.m_dirLight.color * this.m_dirLight.intensity);
		Shader.SetGlobalColor("_AmbientColor", RenderSettings.ambientLight);
		RenderSettings.fogColor = this.m_fogColor;
		RenderSettings.fogDensity = this.m_fogDensity;
		RenderSettings.ambientLight = this.m_ambientLightColor;
		Vector3 normalized = this.m_windDir.normalized;
		Shader.SetGlobalVector("_GlobalWindForce", normalized * this.m_windIntensity);
		Shader.SetGlobalVector("_GlobalWind1", new Vector4(normalized.x, normalized.y, normalized.z, this.m_windIntensity));
		Shader.SetGlobalVector("_GlobalWind2", Vector4.one);
		Shader.SetGlobalFloat("_GlobalWindAlpha", 0f);
	}

	public Light m_dirLight;

	public Color m_sunFogColor = Color.white;

	public Color m_fogColor = Color.white;

	public Color m_ambientLightColor = Color.white;

	public float m_fogDensity = 1f;

	public Vector3 m_windDir = Vector3.left;

	public float m_windIntensity = 0.5f;
}
