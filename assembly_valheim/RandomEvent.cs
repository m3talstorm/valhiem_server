using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RandomEvent
{
	public RandomEvent Clone()
	{
		RandomEvent randomEvent = base.MemberwiseClone() as RandomEvent;
		randomEvent.m_spawn = new List<SpawnSystem.SpawnData>();
		foreach (SpawnSystem.SpawnData spawnData in this.m_spawn)
		{
			randomEvent.m_spawn.Add(spawnData.Clone());
		}
		return randomEvent;
	}

	public bool Update(bool server, bool active, bool playerInArea, float dt)
	{
		if (this.m_pauseIfNoPlayerInArea && !playerInArea)
		{
			return false;
		}
		this.m_time += dt;
		return this.m_duration > 0f && this.m_time > this.m_duration;
	}

	public void OnActivate()
	{
		this.m_active = true;
		if (this.m_firstActivation)
		{
			this.m_firstActivation = false;
			if (this.m_startMessage != "")
			{
				MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, this.m_startMessage, 0, null);
			}
		}
	}

	public void OnDeactivate(bool end)
	{
		this.m_active = false;
		if (end && this.m_endMessage != "")
		{
			MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, this.m_endMessage, 0, null);
		}
	}

	public string GetHudText()
	{
		return this.m_startMessage;
	}

	public void OnStart()
	{
	}

	public void OnStop()
	{
	}

	public bool InEventBiome()
	{
		return (EnvMan.instance.GetCurrentBiome() & this.m_biome) > Heightmap.Biome.None;
	}

	public float GetTime()
	{
		return this.m_time;
	}

	public string m_name = "";

	public bool m_enabled = true;

	public bool m_random = true;

	public float m_duration = 60f;

	public bool m_nearBaseOnly = true;

	public bool m_pauseIfNoPlayerInArea = true;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome;

	[Header("( Keys required to be TRUE )")]
	public List<string> m_requiredGlobalKeys = new List<string>();

	[Header("( Keys required to be FALSE )")]
	public List<string> m_notRequiredGlobalKeys = new List<string>();

	[Space(20f)]
	public string m_startMessage = "";

	public string m_endMessage = "";

	public string m_forceMusic = "";

	public string m_forceEnvironment = "";

	public List<SpawnSystem.SpawnData> m_spawn = new List<SpawnSystem.SpawnData>();

	private bool m_firstActivation = true;

	private bool m_active;

	[NonSerialized]
	public float m_time;

	[NonSerialized]
	public Vector3 m_pos = Vector3.zero;
}
