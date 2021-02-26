using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RandEventSystem : MonoBehaviour
{
	public static RandEventSystem instance
	{
		get
		{
			return RandEventSystem.m_instance;
		}
	}

	private void Awake()
	{
		RandEventSystem.m_instance = this;
	}

	private void OnDestroy()
	{
		RandEventSystem.m_instance = null;
	}

	private void Start()
	{
		ZRoutedRpc.instance.Register<string, float, Vector3>("SetEvent", new Action<long, string, float, Vector3>(this.RPC_SetEvent));
	}

	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		this.UpdateForcedEvents(fixedDeltaTime);
		this.UpdateRandomEvent(fixedDeltaTime);
		if (this.m_forcedEvent != null)
		{
			this.m_forcedEvent.Update(ZNet.instance.IsServer(), this.m_forcedEvent == this.m_activeEvent, true, fixedDeltaTime);
		}
		if (this.m_randomEvent != null && ZNet.instance.IsServer())
		{
			bool playerInArea = this.IsAnyPlayerInEventArea(this.m_randomEvent);
			if (this.m_randomEvent.Update(true, this.m_randomEvent == this.m_activeEvent, playerInArea, fixedDeltaTime))
			{
				this.SetRandomEvent(null, Vector3.zero);
			}
		}
		if (this.m_forcedEvent != null)
		{
			this.SetActiveEvent(this.m_forcedEvent, false);
			return;
		}
		if (this.m_randomEvent == null || !Player.m_localPlayer)
		{
			this.SetActiveEvent(null, false);
			return;
		}
		if (this.IsInsideRandomEventArea(this.m_randomEvent, Player.m_localPlayer.transform.position))
		{
			this.SetActiveEvent(this.m_randomEvent, false);
			return;
		}
		this.SetActiveEvent(null, false);
	}

	private bool IsInsideRandomEventArea(RandomEvent re, Vector3 position)
	{
		return position.y <= 3000f && Utils.DistanceXZ(position, re.m_pos) < this.m_randomEventRange;
	}

	private void UpdateRandomEvent(float dt)
	{
		if (ZNet.instance.IsServer())
		{
			this.m_eventTimer += dt;
			if (this.m_eventTimer > this.m_eventIntervalMin * 60f)
			{
				this.m_eventTimer = 0f;
				if (UnityEngine.Random.Range(0f, 100f) <= this.m_eventChance)
				{
					this.StartRandomEvent();
				}
			}
			this.m_sendTimer += dt;
			if (this.m_sendTimer > 2f)
			{
				this.m_sendTimer = 0f;
				this.SendCurrentRandomEvent();
			}
		}
	}

	private void UpdateForcedEvents(float dt)
	{
		this.m_forcedEventUpdateTimer += dt;
		if (this.m_forcedEventUpdateTimer > 2f)
		{
			this.m_forcedEventUpdateTimer = 0f;
			string forcedEvent = this.GetForcedEvent();
			this.SetForcedEvent(forcedEvent);
		}
	}

	private void SetForcedEvent(string name)
	{
		if (this.m_forcedEvent != null && name != null && this.m_forcedEvent.m_name == name)
		{
			return;
		}
		if (this.m_forcedEvent != null)
		{
			if (this.m_forcedEvent == this.m_activeEvent)
			{
				this.SetActiveEvent(null, true);
			}
			this.m_forcedEvent.OnStop();
			this.m_forcedEvent = null;
		}
		RandomEvent @event = this.GetEvent(name);
		if (@event != null)
		{
			this.m_forcedEvent = @event.Clone();
			this.m_forcedEvent.OnStart();
		}
	}

	private string GetForcedEvent()
	{
		if (EnemyHud.instance != null)
		{
			Character activeBoss = EnemyHud.instance.GetActiveBoss();
			if (activeBoss != null && activeBoss.m_bossEvent.Length > 0)
			{
				return activeBoss.m_bossEvent;
			}
			string @event = EventZone.GetEvent();
			if (@event != null)
			{
				return @event;
			}
		}
		return null;
	}

	private void SendCurrentRandomEvent()
	{
		if (this.m_randomEvent != null)
		{
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SetEvent", new object[]
			{
				this.m_randomEvent.m_name,
				this.m_randomEvent.m_time,
				this.m_randomEvent.m_pos
			});
			return;
		}
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SetEvent", new object[]
		{
			"",
			0f,
			Vector3.zero
		});
	}

	private void RPC_SetEvent(long sender, string eventName, float time, Vector3 pos)
	{
		if (ZNet.instance.IsServer())
		{
			return;
		}
		if (this.m_randomEvent == null || this.m_randomEvent.m_name != eventName)
		{
			this.SetRandomEventByName(eventName, pos);
		}
		if (this.m_randomEvent != null)
		{
			this.m_randomEvent.m_time = time;
			this.m_randomEvent.m_pos = pos;
		}
	}

	public void StartRandomEvent()
	{
		if (!ZNet.instance.IsServer())
		{
			return;
		}
		List<KeyValuePair<RandomEvent, Vector3>> possibleRandomEvents = this.GetPossibleRandomEvents();
		ZLog.Log("Possible events:" + possibleRandomEvents.Count);
		if (possibleRandomEvents.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<RandomEvent, Vector3> keyValuePair in possibleRandomEvents)
		{
			ZLog.DevLog("Event " + keyValuePair.Key.m_name);
		}
		KeyValuePair<RandomEvent, Vector3> keyValuePair2 = possibleRandomEvents[UnityEngine.Random.Range(0, possibleRandomEvents.Count)];
		this.SetRandomEvent(keyValuePair2.Key, keyValuePair2.Value);
	}

	private RandomEvent GetEvent(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		foreach (RandomEvent randomEvent in this.m_events)
		{
			if (randomEvent.m_name == name && randomEvent.m_enabled)
			{
				return randomEvent;
			}
		}
		return null;
	}

	public void SetRandomEventByName(string name, Vector3 pos)
	{
		RandomEvent @event = this.GetEvent(name);
		this.SetRandomEvent(@event, pos);
	}

	public void ResetRandomEvent()
	{
		this.SetRandomEvent(null, Vector3.zero);
	}

	public bool HaveEvent(string name)
	{
		return this.GetEvent(name) != null;
	}

	private void SetRandomEvent(RandomEvent ev, Vector3 pos)
	{
		if (this.m_randomEvent != null)
		{
			if (this.m_randomEvent == this.m_activeEvent)
			{
				this.SetActiveEvent(null, true);
			}
			this.m_randomEvent.OnStop();
			this.m_randomEvent = null;
		}
		if (ev != null)
		{
			this.m_randomEvent = ev.Clone();
			this.m_randomEvent.m_pos = pos;
			this.m_randomEvent.OnStart();
			ZLog.Log("Random event set:" + ev.m_name);
			if (Player.m_localPlayer)
			{
				Player.m_localPlayer.ShowTutorial("randomevent", false);
			}
		}
		if (ZNet.instance.IsServer())
		{
			this.SendCurrentRandomEvent();
		}
	}

	private bool IsAnyPlayerInEventArea(RandomEvent re)
	{
		foreach (ZDO zdo in ZNet.instance.GetAllCharacterZDOS())
		{
			if (this.IsInsideRandomEventArea(re, zdo.GetPosition()))
			{
				return true;
			}
		}
		return false;
	}

	private List<KeyValuePair<RandomEvent, Vector3>> GetPossibleRandomEvents()
	{
		List<KeyValuePair<RandomEvent, Vector3>> list = new List<KeyValuePair<RandomEvent, Vector3>>();
		List<ZDO> allCharacterZDOS = ZNet.instance.GetAllCharacterZDOS();
		foreach (RandomEvent randomEvent in this.m_events)
		{
			if (randomEvent.m_enabled && randomEvent.m_random && this.HaveGlobalKeys(randomEvent))
			{
				List<Vector3> validEventPoints = this.GetValidEventPoints(randomEvent, allCharacterZDOS);
				if (validEventPoints.Count != 0)
				{
					Vector3 value = validEventPoints[UnityEngine.Random.Range(0, validEventPoints.Count)];
					list.Add(new KeyValuePair<RandomEvent, Vector3>(randomEvent, value));
				}
			}
		}
		return list;
	}

	private List<Vector3> GetValidEventPoints(RandomEvent ev, List<ZDO> characters)
	{
		List<Vector3> list = new List<Vector3>();
		foreach (ZDO zdo in characters)
		{
			if (this.InValidBiome(ev, zdo) && this.CheckBase(ev, zdo) && zdo.GetPosition().y <= 3000f)
			{
				list.Add(zdo.GetPosition());
			}
		}
		return list;
	}

	private bool InValidBiome(RandomEvent ev, ZDO zdo)
	{
		if (ev.m_biome == Heightmap.Biome.None)
		{
			return true;
		}
		Vector3 position = zdo.GetPosition();
		return (WorldGenerator.instance.GetBiome(position) & ev.m_biome) != Heightmap.Biome.None;
	}

	private bool CheckBase(RandomEvent ev, ZDO zdo)
	{
		return ev.m_nearBaseOnly && zdo.GetInt("baseValue", 0) >= 3;
	}

	private bool HaveGlobalKeys(RandomEvent ev)
	{
		foreach (string name in ev.m_requiredGlobalKeys)
		{
			if (!ZoneSystem.instance.GetGlobalKey(name))
			{
				return false;
			}
		}
		foreach (string name2 in ev.m_notRequiredGlobalKeys)
		{
			if (ZoneSystem.instance.GetGlobalKey(name2))
			{
				return false;
			}
		}
		return true;
	}

	public List<SpawnSystem.SpawnData> GetCurrentSpawners()
	{
		if (this.m_activeEvent != null)
		{
			return this.m_activeEvent.m_spawn;
		}
		return null;
	}

	public string GetEnvOverride()
	{
		if (this.m_activeEvent != null && !string.IsNullOrEmpty(this.m_activeEvent.m_forceEnvironment) && this.m_activeEvent.InEventBiome())
		{
			return this.m_activeEvent.m_forceEnvironment;
		}
		return null;
	}

	public string GetMusicOverride()
	{
		if (this.m_activeEvent != null && !string.IsNullOrEmpty(this.m_activeEvent.m_forceMusic))
		{
			return this.m_activeEvent.m_forceMusic;
		}
		return null;
	}

	private void SetActiveEvent(RandomEvent ev, bool end = false)
	{
		if (ev != null && this.m_activeEvent != null && ev.m_name == this.m_activeEvent.m_name)
		{
			return;
		}
		if (this.m_activeEvent != null)
		{
			this.m_activeEvent.OnDeactivate(end);
			this.m_activeEvent = null;
		}
		if (ev != null)
		{
			this.m_activeEvent = ev;
			if (this.m_activeEvent != null)
			{
				this.m_activeEvent.OnActivate();
			}
		}
	}

	public static bool InEvent()
	{
		return !(RandEventSystem.m_instance == null) && RandEventSystem.m_instance.m_activeEvent != null;
	}

	public static bool HaveActiveEvent()
	{
		return !(RandEventSystem.m_instance == null) && (RandEventSystem.m_instance.m_activeEvent != null || RandEventSystem.m_instance.m_randomEvent != null || RandEventSystem.m_instance.m_activeEvent != null);
	}

	public RandomEvent GetCurrentRandomEvent()
	{
		return this.m_randomEvent;
	}

	public RandomEvent GetActiveEvent()
	{
		return this.m_activeEvent;
	}

	public void PrepareSave()
	{
		this.m_tempSaveEventTimer = this.m_eventTimer;
		if (this.m_randomEvent != null)
		{
			this.m_tempSaveRandomEvent = this.m_randomEvent.m_name;
			this.m_tempSaveRandomEventTime = this.m_randomEvent.m_time;
			this.m_tempSaveRandomEventPos = this.m_randomEvent.m_pos;
			return;
		}
		this.m_tempSaveRandomEvent = "";
		this.m_tempSaveRandomEventTime = 0f;
		this.m_tempSaveRandomEventPos = Vector3.zero;
	}

	public void SaveAsync(BinaryWriter writer)
	{
		writer.Write(this.m_tempSaveEventTimer);
		writer.Write(this.m_tempSaveRandomEvent);
		writer.Write(this.m_tempSaveRandomEventTime);
		writer.Write(this.m_tempSaveRandomEventPos.x);
		writer.Write(this.m_tempSaveRandomEventPos.y);
		writer.Write(this.m_tempSaveRandomEventPos.z);
	}

	public void Load(BinaryReader reader, int version)
	{
		this.m_eventTimer = reader.ReadSingle();
		if (version >= 25)
		{
			string text = reader.ReadString();
			float time = reader.ReadSingle();
			Vector3 pos;
			pos.x = reader.ReadSingle();
			pos.y = reader.ReadSingle();
			pos.z = reader.ReadSingle();
			if (!string.IsNullOrEmpty(text))
			{
				this.SetRandomEventByName(text, pos);
				if (this.m_randomEvent != null)
				{
					this.m_randomEvent.m_time = time;
					this.m_randomEvent.m_pos = pos;
				}
			}
		}
	}

	private static RandEventSystem m_instance;

	public float m_eventIntervalMin = 1f;

	public float m_eventChance = 25f;

	public float m_randomEventRange = 200f;

	private float m_eventTimer;

	private float m_sendTimer;

	public List<RandomEvent> m_events = new List<RandomEvent>();

	private RandomEvent m_randomEvent;

	private float m_forcedEventUpdateTimer;

	private RandomEvent m_forcedEvent;

	private RandomEvent m_activeEvent;

	private float m_tempSaveEventTimer;

	private string m_tempSaveRandomEvent;

	private float m_tempSaveRandomEventTime;

	private Vector3 m_tempSaveRandomEventPos;
}
