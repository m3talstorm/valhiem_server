using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonDB : MonoBehaviour
{
	public static DungeonDB instance
	{
		get
		{
			return DungeonDB.m_instance;
		}
	}

	private void Awake()
	{
		DungeonDB.m_instance = this;
		SceneManager.LoadScene("rooms", LoadSceneMode.Additive);
		ZLog.Log("DungeonDB Awake " + Time.frameCount);
	}

	public bool SkipSaving()
	{
		return this.m_error;
	}

	private void Start()
	{
		ZLog.Log("DungeonDB Start " + Time.frameCount);
		this.m_rooms = DungeonDB.SetupRooms();
		this.GenerateHashList();
	}

	public static List<DungeonDB.RoomData> GetRooms()
	{
		return DungeonDB.m_instance.m_rooms;
	}

	private static List<DungeonDB.RoomData> SetupRooms()
	{
		GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
		GameObject gameObject = null;
		foreach (GameObject gameObject2 in array)
		{
			if (gameObject2.name == "_Rooms")
			{
				gameObject = gameObject2;
				break;
			}
		}
		if (gameObject == null || (DungeonDB.m_instance && gameObject.activeSelf))
		{
			if (DungeonDB.m_instance)
			{
				DungeonDB.m_instance.m_error = true;
			}
			ZLog.LogError("Rooms are fucked, missing _Rooms or its enabled");
		}
		List<DungeonDB.RoomData> list = new List<DungeonDB.RoomData>();
		for (int j = 0; j < gameObject.transform.childCount; j++)
		{
			Room component = gameObject.transform.GetChild(j).GetComponent<Room>();
			DungeonDB.RoomData roomData = new DungeonDB.RoomData();
			roomData.m_room = component;
			ZoneSystem.PrepareNetViews(component.gameObject, roomData.m_netViews);
			ZoneSystem.PrepareRandomSpawns(component.gameObject, roomData.m_randomSpawns);
			list.Add(roomData);
		}
		return list;
	}

	public DungeonDB.RoomData GetRoom(int hash)
	{
		DungeonDB.RoomData result;
		if (this.m_roomByHash.TryGetValue(hash, out result))
		{
			return result;
		}
		return null;
	}

	private void GenerateHashList()
	{
		this.m_roomByHash.Clear();
		foreach (DungeonDB.RoomData roomData in this.m_rooms)
		{
			int stableHashCode = roomData.m_room.gameObject.name.GetStableHashCode();
			this.m_roomByHash.Add(stableHashCode, roomData);
		}
	}

	private static DungeonDB m_instance;

	private List<DungeonDB.RoomData> m_rooms = new List<DungeonDB.RoomData>();

	private Dictionary<int, DungeonDB.RoomData> m_roomByHash = new Dictionary<int, DungeonDB.RoomData>();

	private bool m_error;

	public class RoomData
	{
		public Room m_room;

		[NonSerialized]
		public List<ZNetView> m_netViews = new List<ZNetView>();

		[NonSerialized]
		public List<RandomSpawn> m_randomSpawns = new List<RandomSpawn>();
	}
}
