using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class World
{
	public World()
	{
		this.m_worldSavePath = World.GetWorldSavePath();
	}

	public World(string name, bool loadError, bool versionError)
	{
		this.m_name = name;
		this.m_loadError = loadError;
		this.m_versionError = versionError;
		this.m_worldSavePath = World.GetWorldSavePath();
	}

	public World(string name, string seed)
	{
		this.m_name = name;
		this.m_seedName = seed;
		this.m_seed = ((this.m_seedName == "") ? 0 : this.m_seedName.GetStableHashCode());
		this.m_uid = (long)name.GetStableHashCode() + Utils.GenerateUID();
		this.m_worldGenVersion = global::Version.m_worldGenVersion;
		this.m_worldSavePath = World.GetWorldSavePath();
	}

	private static string GetWorldSavePath()
	{
		return Application.persistentDataPath + "/worlds";
	}

	public static List<World> GetWorldList()
	{
		string[] array;
		try
		{
			array = Directory.GetFiles(World.GetWorldSavePath(), "*.fwl");
		}
		catch
		{
			array = new string[0];
		}
		List<World> list = new List<World>();
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			World world = World.LoadWorld(Path.GetFileNameWithoutExtension(array2[i]));
			if (world != null)
			{
				list.Add(world);
			}
		}
		return list;
	}

	public static void RemoveWorld(string name)
	{
		try
		{
			string str = World.GetWorldSavePath() + "/" + name;
			File.Delete(str + ".fwl");
			File.Delete(str + ".db");
		}
		catch
		{
		}
	}

	public string GetDBPath()
	{
		return this.m_worldSavePath + "/" + this.m_name + ".db";
	}

	public string GetMetaPath()
	{
		return this.m_worldSavePath + "/" + this.m_name + ".fwl";
	}

	public static string GetMetaPath(string name)
	{
		return World.GetWorldSavePath() + "/" + name + ".fwl";
	}

	public static bool HaveWorld(string name)
	{
		return File.Exists(World.GetWorldSavePath() + "/" + name + ".fwl");
	}

	public static World GetMenuWorld()
	{
		return new World("menu", "")
		{
			m_menu = true
		};
	}

	public static World GetEditorWorld()
	{
		return new World("editor", "");
	}

	public static string GenerateSeed()
	{
		string text = "";
		for (int i = 0; i < 10; i++)
		{
			text += "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ023456789"[UnityEngine.Random.Range(0, "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ023456789".Length)].ToString();
		}
		return text;
	}

	public static World GetCreateWorld(string name)
	{
		ZLog.Log("Get create world " + name);
		World world = World.LoadWorld(name);
		if (!world.m_loadError && !world.m_versionError)
		{
			return world;
		}
		ZLog.Log(" creating");
		world = new World(name, World.GenerateSeed());
		world.SaveWorldMetaData();
		return world;
	}

	public static World GetDevWorld()
	{
		World world = World.LoadWorld("DevWorld");
		if (!world.m_loadError && !world.m_versionError)
		{
			return world;
		}
		world = new World("DevWorld", "");
		world.SaveWorldMetaData();
		return world;
	}

	public void SaveWorldMetaData()
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write(global::Version.m_worldVersion);
		zpackage.Write(this.m_name);
		zpackage.Write(this.m_seedName);
		zpackage.Write(this.m_seed);
		zpackage.Write(this.m_uid);
		zpackage.Write(this.m_worldGenVersion);
		Directory.CreateDirectory(this.m_worldSavePath);
		string metaPath = this.GetMetaPath();
		string text = metaPath + ".new";
		string text2 = metaPath + ".old";
		FileStream fileStream = File.Create(text);
		BinaryWriter binaryWriter = new BinaryWriter(fileStream);
		byte[] array = zpackage.GetArray();
		binaryWriter.Write(array.Length);
		binaryWriter.Write(array);
		fileStream.Dispose();
		if (File.Exists(metaPath))
		{
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
			File.Move(metaPath, text2);
		}
		File.Move(text, metaPath);
	}

	public static World LoadWorld(string name)
	{
		FileStream fileStream = null;
		try
		{
			fileStream = File.OpenRead(World.GetMetaPath(name));
		}
		catch
		{
			if (fileStream != null)
			{
				fileStream.Dispose();
			}
			ZLog.Log("  failed to load " + name);
			return new World(name, true, false);
		}
		World result;
		try
		{
			BinaryReader binaryReader = new BinaryReader(fileStream);
			int count = binaryReader.ReadInt32();
			ZPackage zpackage = new ZPackage(binaryReader.ReadBytes(count));
			int num = zpackage.ReadInt();
			if (!global::Version.IsWorldVersionCompatible(num))
			{
				ZLog.Log("incompatible world version " + num);
				result = new World(name, false, true);
			}
			else
			{
				World world = new World();
				world.m_name = zpackage.ReadString();
				world.m_seedName = zpackage.ReadString();
				world.m_seed = zpackage.ReadInt();
				world.m_uid = zpackage.ReadLong();
				if (num >= 26)
				{
					world.m_worldGenVersion = zpackage.ReadInt();
				}
				result = world;
			}
		}
		catch
		{
			ZLog.LogWarning("  error loading world " + name);
			result = new World(name, true, false);
		}
		finally
		{
			if (fileStream != null)
			{
				fileStream.Dispose();
			}
		}
		return result;
	}

	public string m_name = "";

	public string m_seedName = "";

	public int m_seed;

	public long m_uid;

	public int m_worldGenVersion;

	public bool m_menu;

	public bool m_loadError;

	public bool m_versionError;

	private string m_worldSavePath = "";
}
