using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class EnvSetup
{
	public EnvSetup Clone()
	{
		return base.MemberwiseClone() as EnvSetup;
	}

	public string m_name = "";

	public bool m_default;

	[Header("Gameplay")]
	public bool m_isWet;

	public bool m_isFreezing;

	public bool m_isFreezingAtNight;

	public bool m_isCold;

	public bool m_isColdAtNight = true;

	public bool m_alwaysDark;

	[Header("Ambience")]
	public Color m_ambColorNight = Color.white;

	public Color m_ambColorDay = Color.white;

	[Header("Fog-ambient")]
	public Color m_fogColorNight = Color.white;

	public Color m_fogColorMorning = Color.white;

	public Color m_fogColorDay = Color.white;

	public Color m_fogColorEvening = Color.white;

	[Header("Fog-sun")]
	public Color m_fogColorSunNight = Color.white;

	public Color m_fogColorSunMorning = Color.white;

	public Color m_fogColorSunDay = Color.white;

	public Color m_fogColorSunEvening = Color.white;

	[Header("Fog-distance")]
	public float m_fogDensityNight = 0.01f;

	public float m_fogDensityMorning = 0.01f;

	public float m_fogDensityDay = 0.01f;

	public float m_fogDensityEvening = 0.01f;

	[Header("Sun")]
	public Color m_sunColorNight = Color.white;

	public Color m_sunColorMorning = Color.white;

	public Color m_sunColorDay = Color.white;

	public Color m_sunColorEvening = Color.white;

	public float m_lightIntensityDay = 1.2f;

	public float m_lightIntensityNight;

	public float m_sunAngle = 60f;

	[Header("Wind")]
	public float m_windMin;

	public float m_windMax = 1f;

	[Header("Effects")]
	public GameObject m_envObject;

	public GameObject[] m_psystems;

	public bool m_psystemsOutsideOnly;

	public float m_rainCloudAlpha;

	[Header("Audio")]
	public AudioClip m_ambientLoop;

	public float m_ambientVol = 0.3f;

	public string m_ambientList = "";

	[Header("Music overrides")]
	public string m_musicMorning = "";

	public string m_musicEvening = "";

	[FormerlySerializedAs("m_musicRandomDay")]
	public string m_musicDay = "";

	[FormerlySerializedAs("m_musicRandomNight")]
	public string m_musicNight = "";
}
