using System;

[Serializable]
public class EnvEntry
{
	public string m_environment = "";

	public float m_weight = 1f;

	[NonSerialized]
	public EnvSetup m_env;
}
