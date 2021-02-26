using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerProfile
{
	public PlayerProfile(string filename = null)
	{
		this.m_filename = filename;
		this.m_playerName = "Stranger";
		this.m_playerID = Utils.GenerateUID();
	}

	public bool Load()
	{
		return this.m_filename != null && this.LoadPlayerFromDisk();
	}

	public bool Save()
	{
		return this.m_filename != null && this.SavePlayerToDisk();
	}

	public bool HaveIncompatiblPlayerData()
	{
		if (this.m_filename == null)
		{
			return false;
		}
		ZPackage zpackage = this.LoadPlayerDataFromDisk();
		if (zpackage == null)
		{
			return false;
		}
		if (!global::Version.IsPlayerVersionCompatible(zpackage.ReadInt()))
		{
			ZLog.Log("Player data is not compatible, ignoring");
			return true;
		}
		return false;
	}

	public void SavePlayerData(Player player)
	{
		ZPackage zpackage = new ZPackage();
		player.Save(zpackage);
		this.m_playerData = zpackage.GetArray();
	}

	public void LoadPlayerData(Player player)
	{
		player.SetPlayerID(this.m_playerID, this.m_playerName);
		if (this.m_playerData != null)
		{
			ZPackage pkg = new ZPackage(this.m_playerData);
			player.Load(pkg);
			return;
		}
		player.GiveDefaultItems();
	}

	public void SaveLogoutPoint()
	{
		if (Player.m_localPlayer && !Player.m_localPlayer.IsDead() && !Player.m_localPlayer.InIntro())
		{
			this.SetLogoutPoint(Player.m_localPlayer.transform.position);
		}
	}

	private bool SavePlayerToDisk()
	{
		Directory.CreateDirectory(Utils.GetSaveDataPath() + "/characters");
		string text = Utils.GetSaveDataPath() + "/characters/" + this.m_filename + ".fch";
		string text2 = Utils.GetSaveDataPath() + "/characters/" + this.m_filename + ".fch.old";
		string text3 = Utils.GetSaveDataPath() + "/characters/" + this.m_filename + ".fch.new";
		ZPackage zpackage = new ZPackage();
		zpackage.Write(global::Version.m_playerVersion);
		zpackage.Write(this.m_playerStats.m_kills);
		zpackage.Write(this.m_playerStats.m_deaths);
		zpackage.Write(this.m_playerStats.m_crafts);
		zpackage.Write(this.m_playerStats.m_builds);
		zpackage.Write(this.m_worldData.Count);
		foreach (KeyValuePair<long, PlayerProfile.WorldPlayerData> keyValuePair in this.m_worldData)
		{
			zpackage.Write(keyValuePair.Key);
			zpackage.Write(keyValuePair.Value.m_haveCustomSpawnPoint);
			zpackage.Write(keyValuePair.Value.m_spawnPoint);
			zpackage.Write(keyValuePair.Value.m_haveLogoutPoint);
			zpackage.Write(keyValuePair.Value.m_logoutPoint);
			zpackage.Write(keyValuePair.Value.m_haveDeathPoint);
			zpackage.Write(keyValuePair.Value.m_deathPoint);
			zpackage.Write(keyValuePair.Value.m_homePoint);
			zpackage.Write(keyValuePair.Value.m_mapData != null);
			if (keyValuePair.Value.m_mapData != null)
			{
				zpackage.Write(keyValuePair.Value.m_mapData);
			}
		}
		zpackage.Write(this.m_playerName);
		zpackage.Write(this.m_playerID);
		zpackage.Write(this.m_startSeed);
		if (this.m_playerData != null)
		{
			zpackage.Write(true);
			zpackage.Write(this.m_playerData);
		}
		else
		{
			zpackage.Write(false);
		}
		byte[] array = zpackage.GenerateHash();
		byte[] array2 = zpackage.GetArray();
		FileStream fileStream = File.Create(text3);
		BinaryWriter binaryWriter = new BinaryWriter(fileStream);
		binaryWriter.Write(array2.Length);
		binaryWriter.Write(array2);
		binaryWriter.Write(array.Length);
		binaryWriter.Write(array);
		binaryWriter.Flush();
		fileStream.Flush(true);
		fileStream.Close();
		fileStream.Dispose();
		if (File.Exists(text))
		{
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
			File.Move(text, text2);
		}
		File.Move(text3, text);
		return true;
	}

	private bool LoadPlayerFromDisk()
	{
		try
		{
			ZPackage zpackage = this.LoadPlayerDataFromDisk();
			if (zpackage == null)
			{
				ZLog.LogWarning("No player data");
				return false;
			}
			int num = zpackage.ReadInt();
			if (!global::Version.IsPlayerVersionCompatible(num))
			{
				ZLog.Log("Player data is not compatible, ignoring");
				return false;
			}
			if (num >= 28)
			{
				this.m_playerStats.m_kills = zpackage.ReadInt();
				this.m_playerStats.m_deaths = zpackage.ReadInt();
				this.m_playerStats.m_crafts = zpackage.ReadInt();
				this.m_playerStats.m_builds = zpackage.ReadInt();
			}
			this.m_worldData.Clear();
			int num2 = zpackage.ReadInt();
			for (int i = 0; i < num2; i++)
			{
				long key = zpackage.ReadLong();
				PlayerProfile.WorldPlayerData worldPlayerData = new PlayerProfile.WorldPlayerData();
				worldPlayerData.m_haveCustomSpawnPoint = zpackage.ReadBool();
				worldPlayerData.m_spawnPoint = zpackage.ReadVector3();
				worldPlayerData.m_haveLogoutPoint = zpackage.ReadBool();
				worldPlayerData.m_logoutPoint = zpackage.ReadVector3();
				if (num >= 30)
				{
					worldPlayerData.m_haveDeathPoint = zpackage.ReadBool();
					worldPlayerData.m_deathPoint = zpackage.ReadVector3();
				}
				worldPlayerData.m_homePoint = zpackage.ReadVector3();
				if (num >= 29 && zpackage.ReadBool())
				{
					worldPlayerData.m_mapData = zpackage.ReadByteArray();
				}
				this.m_worldData.Add(key, worldPlayerData);
			}
			this.m_playerName = zpackage.ReadString();
			this.m_playerID = zpackage.ReadLong();
			this.m_startSeed = zpackage.ReadString();
			if (zpackage.ReadBool())
			{
				this.m_playerData = zpackage.ReadByteArray();
			}
			else
			{
				this.m_playerData = null;
			}
		}
		catch (Exception ex)
		{
			ZLog.LogWarning("Exception while loading player profile:" + this.m_filename + " , " + ex.ToString());
		}
		return true;
	}

	private ZPackage LoadPlayerDataFromDisk()
	{
		string text = Utils.GetSaveDataPath() + "/characters/" + this.m_filename + ".fch";
		FileStream fileStream;
		try
		{
			fileStream = File.OpenRead(text);
		}
		catch
		{
			ZLog.Log("  failed to load " + text);
			return null;
		}
		byte[] data;
		try
		{
			BinaryReader binaryReader = new BinaryReader(fileStream);
			int num = binaryReader.ReadInt32();
			data = binaryReader.ReadBytes(num);
			int num2 = binaryReader.ReadInt32();
			binaryReader.ReadBytes(num2);
			ZLog.Log(string.Concat(new object[]
			{
				"Data size:",
				num,
				"  hash size:",
				num2
			}));
		}
		catch
		{
			ZLog.LogError("  error loading player.dat");
			fileStream.Dispose();
			return null;
		}
		fileStream.Dispose();
		return new ZPackage(data);
	}

	public void SetLogoutPoint(Vector3 point)
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveLogoutPoint = true;
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_logoutPoint = point;
	}

	public void SetDeathPoint(Vector3 point)
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveDeathPoint = true;
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_deathPoint = point;
	}

	public void SetMapData(byte[] data)
	{
		long worldUID = ZNet.instance.GetWorldUID();
		if (worldUID != 0L)
		{
			this.GetWorldData(worldUID).m_mapData = data;
		}
	}

	public byte[] GetMapData()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_mapData;
	}

	public void ClearLoguoutPoint()
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveLogoutPoint = false;
	}

	public bool HaveLogoutPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveLogoutPoint;
	}

	public Vector3 GetLogoutPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_logoutPoint;
	}

	public bool HaveDeathPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveDeathPoint;
	}

	public Vector3 GetDeathPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_deathPoint;
	}

	public void SetCustomSpawnPoint(Vector3 point)
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveCustomSpawnPoint = true;
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_spawnPoint = point;
	}

	public Vector3 GetCustomSpawnPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_spawnPoint;
	}

	public bool HaveCustomSpawnPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveCustomSpawnPoint;
	}

	public void ClearCustomSpawnPoint()
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveCustomSpawnPoint = false;
	}

	public void SetHomePoint(Vector3 point)
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_homePoint = point;
	}

	public Vector3 GetHomePoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_homePoint;
	}

	public void SetName(string name)
	{
		this.m_playerName = name;
	}

	public string GetName()
	{
		return this.m_playerName;
	}

	public long GetPlayerID()
	{
		return this.m_playerID;
	}

	public static List<PlayerProfile> GetAllPlayerProfiles()
	{
		string[] array;
		try
		{
			array = Directory.GetFiles(Utils.GetSaveDataPath() + "/characters", "*.fch");
		}
		catch
		{
			array = new string[0];
		}
		List<PlayerProfile> list = new List<PlayerProfile>();
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(array2[i]);
			ZLog.Log("loading " + fileNameWithoutExtension);
			PlayerProfile playerProfile = new PlayerProfile(fileNameWithoutExtension);
			if (!playerProfile.Load())
			{
				ZLog.Log("Failed to load " + fileNameWithoutExtension);
			}
			else
			{
				list.Add(playerProfile);
			}
		}
		return list;
	}

	public static void RemoveProfile(string name)
	{
		try
		{
			File.Delete(Utils.GetSaveDataPath() + "/characters/" + name + ".fch");
		}
		catch
		{
		}
	}

	public static bool HaveProfile(string name)
	{
		return File.Exists(Utils.GetSaveDataPath() + "/characters/" + name + ".fch");
	}

	public string GetFilename()
	{
		return this.m_filename;
	}

	private PlayerProfile.WorldPlayerData GetWorldData(long worldUID)
	{
		PlayerProfile.WorldPlayerData worldPlayerData;
		if (this.m_worldData.TryGetValue(worldUID, out worldPlayerData))
		{
			return worldPlayerData;
		}
		worldPlayerData = new PlayerProfile.WorldPlayerData();
		this.m_worldData.Add(worldUID, worldPlayerData);
		return worldPlayerData;
	}

	private string m_filename = "";

	private string m_playerName = "";

	private long m_playerID;

	private string m_startSeed = "";

	public static Vector3 m_originalSpawnPoint = new Vector3(-676f, 50f, 299f);

	private Dictionary<long, PlayerProfile.WorldPlayerData> m_worldData = new Dictionary<long, PlayerProfile.WorldPlayerData>();

	public PlayerProfile.PlayerStats m_playerStats = new PlayerProfile.PlayerStats();

	private byte[] m_playerData;

	private class WorldPlayerData
	{
		public Vector3 m_spawnPoint = Vector3.zero;

		public bool m_haveCustomSpawnPoint;

		public Vector3 m_logoutPoint = Vector3.zero;

		public bool m_haveLogoutPoint;

		public Vector3 m_deathPoint = Vector3.zero;

		public bool m_haveDeathPoint;

		public Vector3 m_homePoint = Vector3.zero;

		public byte[] m_mapData;
	}

	public class PlayerStats
	{
		public int m_kills;

		public int m_deaths;

		public int m_crafts;

		public int m_builds;
	}
}
