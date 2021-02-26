using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

[Serializable]
public class BiomeEnvSetup
{
	public string m_name = "";

	public Heightmap.Biome m_biome = Heightmap.Biome.Meadows;

	public List<EnvEntry> m_environments = new List<EnvEntry>();

	public string m_musicMorning = "morning";

	public string m_musicEvening = "evening";

	[FormerlySerializedAs("m_musicRandomDay")]
	public string m_musicDay = "";

	[FormerlySerializedAs("m_musicRandomNight")]
	public string m_musicNight = "";
}
