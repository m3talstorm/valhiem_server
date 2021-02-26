using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
	public static Game instance
	{
		get
		{
			return Game.m_instance;
		}
	}

	private void Awake()
	{
		Game.m_instance = this;
		Assert.raiseExceptions = true;
		ZInput.Initialize();
		if (!global::Console.instance)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_consolePrefab);
		}
		this.m_playerProfile = new PlayerProfile(null);
		base.InvokeRepeating("ServerLog", 600f, 600f);
		base.InvokeRepeating("CollectResources", 600f, 600f);
		Gogan.LogEvent("Screen", "Enter", "InGame", 0L);
		Gogan.LogEvent("Game", "InputMode", ZInput.IsGamepadActive() ? "Gamepad" : "MK", 0L);
	}

	private void OnDestroy()
	{
		Game.m_instance = null;
	}

	private void Start()
	{
		Application.targetFrameRate = 30;
		ZRoutedRpc.instance.Register("SleepStart", new Action<long>(this.SleepStart));
		ZRoutedRpc.instance.Register("SleepStop", new Action<long>(this.SleepStop));
		ZRoutedRpc.instance.Register<float>("Ping", new Action<long, float>(this.RPC_Ping));
		ZRoutedRpc.instance.Register<float>("Pong", new Action<long, float>(this.RPC_Pong));
		ZRoutedRpc.instance.Register<string, int, Vector3>("DiscoverLocationRespons", new Action<long, string, int, Vector3>(this.RPC_DiscoverLocationRespons));
		if (ZNet.instance.IsServer())
		{
			ZRoutedRpc.instance.Register<string, Vector3, string, int>("DiscoverClosestLocation", new RoutedMethod<string, Vector3, string, int>.Method(this.RPC_DiscoverClosestLocation));
			base.StartCoroutine("ConnectPortals");
			base.InvokeRepeating("UpdateSleeping", 2f, 2f);
		}
	}

	private void ServerLog()
	{
		int peerConnections = ZNet.instance.GetPeerConnections();
		int num = ZDOMan.instance.NrOfObjects();
		int sentZDOs = ZDOMan.instance.GetSentZDOs();
		int recvZDOs = ZDOMan.instance.GetRecvZDOs();
		ZLog.Log(string.Concat(new object[]
		{
			" Connections ",
			peerConnections,
			" ZDOS:",
			num,
			"  sent:",
			sentZDOs,
			" recv:",
			recvZDOs
		}));
	}

	private void CollectResources()
	{
		Resources.UnloadUnusedAssets();
	}

	public void Logout()
	{
		if (this.m_loggedOut)
		{
			return;
		}
		this.m_loggedOut = true;
		this.Shutdown();
		SceneManager.LoadScene("start");
	}

	public bool IsLoggingOut()
	{
		return this.m_loggedOut;
	}

	private void OnApplicationQuit()
	{
		ZLog.Log("Game - OnApplicationQuit");
		this.Shutdown();
		Thread.Sleep(2000);
	}

	private void Shutdown()
	{
		this.SavePlayerProfile(true);
		ZNetScene.instance.Shutdown();
		ZNet.instance.Shutdown();
	}

	private void SavePlayerProfile(bool setLogoutPoint)
	{
		if (Player.m_localPlayer)
		{
			this.m_playerProfile.SavePlayerData(Player.m_localPlayer);
			Minimap.instance.SaveMapData();
			if (setLogoutPoint)
			{
				this.m_playerProfile.SaveLogoutPoint();
			}
		}
		this.m_playerProfile.Save();
	}

	private Player SpawnPlayer(Vector3 spawnPoint)
	{
		ZLog.DevLog("Spawning player:" + Time.frameCount);
		Player component = UnityEngine.Object.Instantiate<GameObject>(this.m_playerPrefab, spawnPoint, Quaternion.identity).GetComponent<Player>();
		component.SetLocalPlayer();
		this.m_playerProfile.LoadPlayerData(component);
		ZNet.instance.SetCharacterID(component.GetZDOID());
		component.OnSpawned();
		return component;
	}

	private Bed FindBedNearby(Vector3 point, float maxDistance)
	{
		foreach (Bed bed in UnityEngine.Object.FindObjectsOfType<Bed>())
		{
			if (bed.IsCurrent())
			{
				return bed;
			}
		}
		return null;
	}

	private bool FindSpawnPoint(out Vector3 point, out bool usedLogoutPoint, float dt)
	{
		this.m_respawnWait += dt;
		usedLogoutPoint = false;
		if (this.m_playerProfile.HaveLogoutPoint())
		{
			Vector3 logoutPoint = this.m_playerProfile.GetLogoutPoint();
			ZNet.instance.SetReferencePosition(logoutPoint);
			if (this.m_respawnWait <= 8f || !ZNetScene.instance.IsAreaReady(logoutPoint))
			{
				point = Vector3.zero;
				return false;
			}
			float num;
			if (!ZoneSystem.instance.GetGroundHeight(logoutPoint, out num))
			{
				ZLog.Log("Invalid spawn point, no ground " + logoutPoint);
				this.m_respawnWait = 0f;
				this.m_playerProfile.ClearLoguoutPoint();
				point = Vector3.zero;
				return false;
			}
			this.m_playerProfile.ClearLoguoutPoint();
			point = logoutPoint;
			if (point.y < num)
			{
				point.y = num;
			}
			point.y += 0.25f;
			usedLogoutPoint = true;
			ZLog.Log("Spawned after " + this.m_respawnWait);
			return true;
		}
		else if (this.m_playerProfile.HaveCustomSpawnPoint())
		{
			Vector3 customSpawnPoint = this.m_playerProfile.GetCustomSpawnPoint();
			ZNet.instance.SetReferencePosition(customSpawnPoint);
			if (this.m_respawnWait <= 8f || !ZNetScene.instance.IsAreaReady(customSpawnPoint))
			{
				point = Vector3.zero;
				return false;
			}
			Bed bed = this.FindBedNearby(customSpawnPoint, 5f);
			if (bed != null)
			{
				ZLog.Log("Found bed at custom spawn point");
				point = bed.GetSpawnPoint();
				return true;
			}
			ZLog.Log("Failed to find bed at custom spawn point, using original");
			this.m_playerProfile.ClearCustomSpawnPoint();
			this.m_respawnWait = 0f;
			point = Vector3.zero;
			return false;
		}
		else
		{
			Vector3 a;
			if (ZoneSystem.instance.GetLocationIcon(this.m_StartLocation, out a))
			{
				point = a + Vector3.up * 2f;
				ZNet.instance.SetReferencePosition(point);
				return ZNetScene.instance.IsAreaReady(point);
			}
			ZNet.instance.SetReferencePosition(Vector3.zero);
			point = Vector3.zero;
			return false;
		}
	}

	private static Vector3 GetPointOnCircle(float distance, float angle)
	{
		return new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
	}

	public void RequestRespawn(float delay)
	{
		base.CancelInvoke("_RequestRespawn");
		base.Invoke("_RequestRespawn", delay);
	}

	private void _RequestRespawn()
	{
		ZLog.Log("Starting respawn");
		if (Player.m_localPlayer)
		{
			this.m_playerProfile.SavePlayerData(Player.m_localPlayer);
		}
		if (Player.m_localPlayer)
		{
			ZNetScene.instance.Destroy(Player.m_localPlayer.gameObject);
			ZNet.instance.SetCharacterID(ZDOID.None);
		}
		this.m_respawnWait = 0f;
		this.m_requestRespawn = true;
		MusicMan.instance.TriggerMusic("respawn");
	}

	private void Update()
	{
		ZInput.Update(Time.deltaTime);
		this.UpdateSaving(Time.deltaTime);
	}

	private void FixedUpdate()
	{
		ZNet.instance.SetReferencePosition(new Vector3(1000000f, 0f, 1000000f));
	}

	private void UpdateSaving(float dt)
	{
		this.m_saveTimer += dt;
		if (this.m_saveTimer > 1200f)
		{
			this.m_saveTimer = 0f;
			this.SavePlayerProfile(false);
			if (ZNet.instance)
			{
				ZNet.instance.Save(false);
			}
		}
	}

	private void UpdateRespawn(float dt)
	{
		Vector3 vector;
		bool flag;
		if (this.m_requestRespawn && this.FindSpawnPoint(out vector, out flag, dt))
		{
			if (!flag)
			{
				this.m_playerProfile.SetHomePoint(vector);
			}
			this.SpawnPlayer(vector);
			this.m_requestRespawn = false;
			if (this.m_firstSpawn)
			{
				this.m_firstSpawn = false;
				Chat.instance.SendText(Talker.Type.Shout, "I have arrived!");
			}
			GC.Collect();
		}
	}

	public bool WaitingForRespawn()
	{
		return this.m_requestRespawn;
	}

	public PlayerProfile GetPlayerProfile()
	{
		return this.m_playerProfile;
	}

	public static void SetProfile(string filename)
	{
		Game.m_profileFilename = filename;
	}

	private IEnumerator ConnectPortals()
	{
		for (;;)
		{
			this.m_tempPortalList.Clear();
			int index = 0;
			bool done = false;
			do
			{
				done = ZDOMan.instance.GetAllZDOsWithPrefabIterative(this.m_portalPrefab.name, this.m_tempPortalList, ref index);
				yield return null;
			}
			while (!done);
			foreach (ZDO zdo in this.m_tempPortalList)
			{
				ZDOID zdoid = zdo.GetZDOID("target");
				string @string = zdo.GetString("tag", "");
				if (!zdoid.IsNone())
				{
					ZDO zdo2 = ZDOMan.instance.GetZDO(zdoid);
					if (zdo2 == null || zdo2.GetString("tag", "") != @string)
					{
						zdo.SetOwner(ZDOMan.instance.GetMyID());
						zdo.Set("target", ZDOID.None);
						ZDOMan.instance.ForceSendZDO(zdo.m_uid);
					}
				}
			}
			foreach (ZDO zdo3 in this.m_tempPortalList)
			{
				string string2 = zdo3.GetString("tag", "");
				if (zdo3.GetZDOID("target").IsNone())
				{
					ZDO zdo4 = this.FindRandomUnconnectedPortal(this.m_tempPortalList, zdo3, string2);
					if (zdo4 != null)
					{
						zdo3.SetOwner(ZDOMan.instance.GetMyID());
						zdo4.SetOwner(ZDOMan.instance.GetMyID());
						zdo3.Set("target", zdo4.m_uid);
						zdo4.Set("target", zdo3.m_uid);
						ZDOMan.instance.ForceSendZDO(zdo3.m_uid);
						ZDOMan.instance.ForceSendZDO(zdo4.m_uid);
					}
				}
			}
			yield return new WaitForSeconds(5f);
		}
		yield break;
	}

	private ZDO FindRandomUnconnectedPortal(List<ZDO> portals, ZDO skip, string tag)
	{
		List<ZDO> list = new List<ZDO>();
		foreach (ZDO zdo in portals)
		{
			if (zdo != skip && zdo.GetZDOID("target").IsNone() && !(zdo.GetString("tag", "") != tag))
			{
				list.Add(zdo);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private ZDO FindClosestUnconnectedPortal(List<ZDO> portals, ZDO skip, Vector3 refPos)
	{
		ZDO zdo = null;
		float num = 99999f;
		foreach (ZDO zdo2 in portals)
		{
			if (zdo2 != skip && zdo2.GetZDOID("target").IsNone())
			{
				float num2 = Vector3.Distance(refPos, zdo2.GetPosition());
				if (zdo == null || num2 < num)
				{
					zdo = zdo2;
					num = num2;
				}
			}
		}
		return zdo;
	}

	private void UpdateSleeping()
	{
		if (!ZNet.instance.IsServer())
		{
			return;
		}
		if (this.m_sleeping)
		{
			if (!EnvMan.instance.IsTimeSkipping())
			{
				this.m_sleeping = false;
				ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SleepStop", Array.Empty<object>());
				return;
			}
		}
		else if (!EnvMan.instance.IsTimeSkipping())
		{
			if (!EnvMan.instance.IsAfternoon() && !EnvMan.instance.IsNight())
			{
				return;
			}
			if (!this.EverybodyIsTryingToSleep())
			{
				return;
			}
			EnvMan.instance.SkipToMorning();
			this.m_sleeping = true;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SleepStart", Array.Empty<object>());
		}
	}

	private bool EverybodyIsTryingToSleep()
	{
		List<ZDO> allCharacterZDOS = ZNet.instance.GetAllCharacterZDOS();
		if (allCharacterZDOS.Count == 0)
		{
			return false;
		}
		using (List<ZDO>.Enumerator enumerator = allCharacterZDOS.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (!enumerator.Current.GetBool("inBed", false))
				{
					return false;
				}
			}
		}
		return true;
	}

	private void SleepStart(long sender)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			localPlayer.SetSleeping(true);
		}
	}

	private void SleepStop(long sender)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			localPlayer.SetSleeping(false);
			localPlayer.AttachStop();
		}
	}

	public void DiscoverClosestLocation(string name, Vector3 point, string pinName, int pinType)
	{
		ZLog.Log("DiscoverClosestLocation");
		ZRoutedRpc.instance.InvokeRoutedRPC("DiscoverClosestLocation", new object[]
		{
			name,
			point,
			pinName,
			pinType
		});
	}

	private void RPC_DiscoverClosestLocation(long sender, string name, Vector3 point, string pinName, int pinType)
	{
		ZoneSystem.LocationInstance locationInstance;
		if (ZoneSystem.instance.FindClosestLocation(name, point, out locationInstance))
		{
			ZLog.Log("Found location of type " + name);
			ZRoutedRpc.instance.InvokeRoutedRPC(sender, "DiscoverLocationRespons", new object[]
			{
				pinName,
				pinType,
				locationInstance.m_position
			});
			return;
		}
		ZLog.LogWarning("Failed to find location of type " + name);
	}

	private void RPC_DiscoverLocationRespons(long sender, string pinName, int pinType, Vector3 pos)
	{
		Minimap.instance.DiscoverLocation(pos, (Minimap.PinType)pinType, pinName);
	}

	public void Ping()
	{
		if (global::Console.instance)
		{
			global::Console.instance.Print("Ping sent to server");
		}
		ZRoutedRpc.instance.InvokeRoutedRPC("Ping", new object[]
		{
			Time.time
		});
	}

	private void RPC_Ping(long sender, float time)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(sender, "Pong", new object[]
		{
			time
		});
	}

	private void RPC_Pong(long sender, float time)
	{
		float num = Time.time - time;
		string text = "Got ping reply from server: " + (int)(num * 1000f) + " ms";
		ZLog.Log(text);
		if (global::Console.instance)
		{
			global::Console.instance.Print(text);
		}
	}

	public void SetForcePlayerDifficulty(int players)
	{
		this.m_forcePlayers = players;
	}

	private int GetPlayerDifficulty(Vector3 pos)
	{
		if (this.m_forcePlayers > 0)
		{
			return this.m_forcePlayers;
		}
		int num = Player.GetPlayersInRangeXZ(pos, 200f);
		if (num < 1)
		{
			num = 1;
		}
		return num;
	}

	public float GetDifficultyDamageScale(Vector3 pos)
	{
		int playerDifficulty = this.GetPlayerDifficulty(pos);
		return 1f + (float)(playerDifficulty - 1) * 0.04f;
	}

	public float GetDifficultyHealthScale(Vector3 pos)
	{
		int playerDifficulty = this.GetPlayerDifficulty(pos);
		return 1f + (float)(playerDifficulty - 1) * 0.4f;
	}

	private List<ZDO> m_tempPortalList = new List<ZDO>();

	private static Game m_instance;

	public GameObject m_playerPrefab;

	public GameObject m_portalPrefab;

	public GameObject m_consolePrefab;

	public string m_StartLocation = "StartTemple";

	private static string m_profileFilename;

	private PlayerProfile m_playerProfile;

	private bool m_requestRespawn;

	private float m_respawnWait;

	private const float m_respawnLoadDuration = 8f;

	private bool m_haveSpawned;

	private bool m_firstSpawn = true;

	private bool m_loggedOut;

	private Vector3 m_randomStartPoint = Vector3.zero;

	private UnityEngine.Random.State m_spawnRandomState;

	private bool m_sleeping;

	private const float m_collectResourcesInterval = 600f;

	private float m_saveTimer;

	private const float m_saveInterval = 1200f;

	private const float m_difficultyScaleRange = 200f;

	private const float m_damageScalePerPlayer = 0.04f;

	private const float m_healthScalePerPlayer = 0.4f;

	private int m_forcePlayers;
}
