using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.Load();
	}

	public void Clear()
	{
		while (base.transform.childCount > 0)
		{
			UnityEngine.Object.DestroyImmediate(base.transform.GetChild(0).gameObject);
		}
	}

	public void Generate(ZoneSystem.SpawnMode mode)
	{
		int seed = WorldGenerator.instance.GetSeed();
		Vector2i zone = ZoneSystem.instance.GetZone(base.transform.position);
		int seed2 = seed + zone.x * 4271 + zone.y * 9187;
		this.Generate(seed2, mode);
	}

	public void Generate(int seed, ZoneSystem.SpawnMode mode)
	{
		DateTime now = DateTime.Now;
		this.Clear();
		this.SetupColliders();
		this.SetupAvailableRooms();
		if (ZoneSystem.instance)
		{
			Vector2i zone = ZoneSystem.instance.GetZone(base.transform.position);
			this.m_zoneCenter = ZoneSystem.instance.GetZonePos(zone);
			this.m_zoneCenter.y = base.transform.position.y;
		}
		ZLog.Log("Available rooms:" + DungeonGenerator.m_availableRooms.Count);
		ZLog.Log("To place:" + this.m_maxRooms);
		DungeonGenerator.m_placedRooms.Clear();
		DungeonGenerator.m_openConnections.Clear();
		DungeonGenerator.m_doorConnections.Clear();
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		this.GenerateRooms(mode);
		this.Save();
		ZLog.Log("Placed " + DungeonGenerator.m_placedRooms.Count + " rooms");
		UnityEngine.Random.state = state;
		SnapToGround.SnappAll();
		if (mode == ZoneSystem.SpawnMode.Ghost)
		{
			foreach (Room room in DungeonGenerator.m_placedRooms)
			{
				UnityEngine.Object.DestroyImmediate(room.gameObject);
			}
		}
		DungeonGenerator.m_placedRooms.Clear();
		DungeonGenerator.m_openConnections.Clear();
		DungeonGenerator.m_doorConnections.Clear();
		UnityEngine.Object.DestroyImmediate(this.m_colliderA);
		UnityEngine.Object.DestroyImmediate(this.m_colliderB);
		DateTime.Now - now;
	}

	private void GenerateRooms(ZoneSystem.SpawnMode mode)
	{
		switch (this.m_algorithm)
		{
		case DungeonGenerator.Algorithm.Dungeon:
			this.GenerateDungeon(mode);
			return;
		case DungeonGenerator.Algorithm.CampGrid:
			this.GenerateCampGrid(mode);
			return;
		case DungeonGenerator.Algorithm.CampRadial:
			this.GenerateCampRadial(mode);
			return;
		default:
			return;
		}
	}

	private void GenerateDungeon(ZoneSystem.SpawnMode mode)
	{
		this.PlaceStartRoom(mode);
		this.PlaceRooms(mode);
		this.PlaceEndCaps(mode);
		this.PlaceDoors(mode);
	}

	private void GenerateCampGrid(ZoneSystem.SpawnMode mode)
	{
		float num = Mathf.Cos(0.017453292f * this.m_maxTilt);
		Vector3 a = base.transform.position + new Vector3((float)(-(float)this.m_gridSize) * this.m_tileWidth * 0.5f, 0f, (float)(-(float)this.m_gridSize) * this.m_tileWidth * 0.5f);
		for (int i = 0; i < this.m_gridSize; i++)
		{
			for (int j = 0; j < this.m_gridSize; j++)
			{
				if (UnityEngine.Random.value <= this.m_spawnChance)
				{
					Vector3 pos = a + new Vector3((float)j * this.m_tileWidth, 0f, (float)i * this.m_tileWidth);
					DungeonDB.RoomData randomWeightedRoom = this.GetRandomWeightedRoom(false);
					if (randomWeightedRoom != null)
					{
						if (ZoneSystem.instance)
						{
							Vector3 vector;
							Heightmap.Biome biome;
							Heightmap.BiomeArea biomeArea;
							Heightmap heightmap;
							ZoneSystem.instance.GetGroundData(ref pos, out vector, out biome, out biomeArea, out heightmap);
							if (vector.y < num)
							{
								goto IL_FE;
							}
						}
						Quaternion rot = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 16) * 22.5f, 0f);
						this.PlaceRoom(randomWeightedRoom, pos, rot, null, mode);
					}
				}
				IL_FE:;
			}
		}
	}

	private void GenerateCampRadial(ZoneSystem.SpawnMode mode)
	{
		float num = UnityEngine.Random.Range(this.m_campRadiusMin, this.m_campRadiusMax);
		float num2 = Mathf.Cos(0.017453292f * this.m_maxTilt);
		int num3 = UnityEngine.Random.Range(this.m_minRooms, this.m_maxRooms);
		int num4 = num3 * 20;
		int num5 = 0;
		for (int i = 0; i < num4; i++)
		{
			Vector3 vector = base.transform.position + Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * UnityEngine.Random.Range(0f, num - this.m_perimeterBuffer);
			DungeonDB.RoomData randomWeightedRoom = this.GetRandomWeightedRoom(false);
			if (randomWeightedRoom != null)
			{
				if (ZoneSystem.instance)
				{
					Vector3 vector2;
					Heightmap.Biome biome;
					Heightmap.BiomeArea biomeArea;
					Heightmap heightmap;
					ZoneSystem.instance.GetGroundData(ref vector, out vector2, out biome, out biomeArea, out heightmap);
					if (vector2.y < num2 || vector.y - ZoneSystem.instance.m_waterLevel < this.m_minAltitude)
					{
						goto IL_11D;
					}
				}
				Quaternion campRoomRotation = this.GetCampRoomRotation(randomWeightedRoom, vector);
				if (!this.TestCollision(randomWeightedRoom.m_room, vector, campRoomRotation))
				{
					this.PlaceRoom(randomWeightedRoom, vector, campRoomRotation, null, mode);
					num5++;
					if (num5 >= num3)
					{
						break;
					}
				}
			}
			IL_11D:;
		}
		if (this.m_perimeterSections > 0)
		{
			this.PlaceWall(num, this.m_perimeterSections, mode);
		}
	}

	private Quaternion GetCampRoomRotation(DungeonDB.RoomData room, Vector3 pos)
	{
		if (room.m_room.m_faceCenter)
		{
			Vector3 vector = base.transform.position - pos;
			vector.y = 0f;
			if (vector == Vector3.zero)
			{
				vector = Vector3.forward;
			}
			vector.Normalize();
			float y = Mathf.Round(Utils.YawFromDirection(vector) / 22.5f) * 22.5f;
			return Quaternion.Euler(0f, y, 0f);
		}
		return Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 16) * 22.5f, 0f);
	}

	private void PlaceWall(float radius, int sections, ZoneSystem.SpawnMode mode)
	{
		float num = Mathf.Cos(0.017453292f * this.m_maxTilt);
		int num2 = 0;
		int num3 = sections * 20;
		for (int i = 0; i < num3; i++)
		{
			DungeonDB.RoomData randomWeightedRoom = this.GetRandomWeightedRoom(true);
			if (randomWeightedRoom != null)
			{
				Vector3 vector = base.transform.position + Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * radius;
				Quaternion campRoomRotation = this.GetCampRoomRotation(randomWeightedRoom, vector);
				if (ZoneSystem.instance)
				{
					Vector3 vector2;
					Heightmap.Biome biome;
					Heightmap.BiomeArea biomeArea;
					Heightmap heightmap;
					ZoneSystem.instance.GetGroundData(ref vector, out vector2, out biome, out biomeArea, out heightmap);
					if (vector2.y < num || vector.y - ZoneSystem.instance.m_waterLevel < this.m_minAltitude)
					{
						goto IL_E6;
					}
				}
				if (!this.TestCollision(randomWeightedRoom.m_room, vector, campRoomRotation))
				{
					this.PlaceRoom(randomWeightedRoom, vector, campRoomRotation, null, mode);
					num2++;
					if (num2 >= sections)
					{
						break;
					}
				}
			}
			IL_E6:;
		}
	}

	private void Save()
	{
		if (this.m_nview == null)
		{
			return;
		}
		ZDO zdo = this.m_nview.GetZDO();
		zdo.Set("rooms", DungeonGenerator.m_placedRooms.Count);
		for (int i = 0; i < DungeonGenerator.m_placedRooms.Count; i++)
		{
			Room room = DungeonGenerator.m_placedRooms[i];
			string text = "room" + i.ToString();
			zdo.Set(text, room.GetHash());
			zdo.Set(text + "_pos", room.transform.position);
			zdo.Set(text + "_rot", room.transform.rotation);
		}
	}

	private void Load()
	{
		if (this.m_nview == null)
		{
			return;
		}
		DateTime now = DateTime.Now;
		ZLog.Log("Loading dungeon");
		ZDO zdo = this.m_nview.GetZDO();
		int @int = zdo.GetInt("rooms", 0);
		for (int i = 0; i < @int; i++)
		{
			string text = "room" + i.ToString();
			int int2 = zdo.GetInt(text, 0);
			Vector3 vec = zdo.GetVec3(text + "_pos", Vector3.zero);
			Quaternion quaternion = zdo.GetQuaternion(text + "_rot", Quaternion.identity);
			DungeonDB.RoomData room = DungeonDB.instance.GetRoom(int2);
			if (room == null)
			{
				ZLog.LogWarning("Missing room:" + int2);
			}
			else
			{
				this.PlaceRoom(room, vec, quaternion, null, ZoneSystem.SpawnMode.Client);
			}
		}
		ZLog.Log("Dungeon loaded " + @int);
		ZLog.Log("Dungeon load time " + (DateTime.Now - now).TotalMilliseconds + " ms");
	}

	private void SetupAvailableRooms()
	{
		DungeonGenerator.m_availableRooms.Clear();
		foreach (DungeonDB.RoomData roomData in DungeonDB.GetRooms())
		{
			if ((roomData.m_room.m_theme & this.m_themes) != (Room.Theme)0 && roomData.m_room.m_enabled)
			{
				DungeonGenerator.m_availableRooms.Add(roomData);
			}
		}
	}

	private DungeonGenerator.DoorDef FindDoorType(string type)
	{
		List<DungeonGenerator.DoorDef> list = new List<DungeonGenerator.DoorDef>();
		foreach (DungeonGenerator.DoorDef doorDef in this.m_doorTypes)
		{
			if (doorDef.m_connectionType == type)
			{
				list.Add(doorDef);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private void PlaceDoors(ZoneSystem.SpawnMode mode)
	{
		int num = 0;
		foreach (RoomConnection roomConnection in DungeonGenerator.m_doorConnections)
		{
			if (UnityEngine.Random.value <= this.m_doorChance)
			{
				DungeonGenerator.DoorDef doorDef = this.FindDoorType(roomConnection.m_type);
				if (doorDef == null)
				{
					ZLog.Log("No door type for connection:" + roomConnection.m_type);
				}
				else
				{
					GameObject obj = UnityEngine.Object.Instantiate<GameObject>(doorDef.m_prefab, roomConnection.transform.position, roomConnection.transform.rotation);
					if (mode == ZoneSystem.SpawnMode.Ghost)
					{
						UnityEngine.Object.Destroy(obj);
					}
					num++;
				}
			}
		}
		ZLog.Log("placed " + num + " doors");
	}

	private void PlaceEndCaps(ZoneSystem.SpawnMode mode)
	{
		for (int i = 0; i < DungeonGenerator.m_openConnections.Count; i++)
		{
			RoomConnection roomConnection = DungeonGenerator.m_openConnections[i];
			bool flag = false;
			for (int j = 0; j < DungeonGenerator.m_openConnections.Count; j++)
			{
				if (j != i && roomConnection.TestContact(DungeonGenerator.m_openConnections[j]))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				ZLog.Log("cyclic detected , cool");
			}
			else
			{
				this.FindEndCaps(roomConnection, DungeonGenerator.m_tempRooms);
				IEnumerable<DungeonDB.RoomData> enumerable = from item in DungeonGenerator.m_tempRooms
				orderby item.m_room.m_endCapPrio descending
				select item;
				bool flag2 = false;
				foreach (DungeonDB.RoomData roomData in enumerable)
				{
					if (this.PlaceRoom(roomConnection, roomData, mode))
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					ZLog.LogWarning("Failed to place end cap " + roomConnection.name + " " + roomConnection.transform.parent.gameObject.name);
				}
			}
		}
	}

	private void FindEndCaps(RoomConnection connection, List<DungeonDB.RoomData> rooms)
	{
		rooms.Clear();
		foreach (DungeonDB.RoomData roomData in DungeonGenerator.m_availableRooms)
		{
			if (roomData.m_room.m_endCap && roomData.m_room.HaveConnection(connection))
			{
				rooms.Add(roomData);
			}
		}
		rooms.Shuffle<DungeonDB.RoomData>();
	}

	private DungeonDB.RoomData FindEndCap(RoomConnection connection)
	{
		DungeonGenerator.m_tempRooms.Clear();
		foreach (DungeonDB.RoomData roomData in DungeonGenerator.m_availableRooms)
		{
			if (roomData.m_room.m_endCap && roomData.m_room.HaveConnection(connection))
			{
				DungeonGenerator.m_tempRooms.Add(roomData);
			}
		}
		if (DungeonGenerator.m_tempRooms.Count == 0)
		{
			return null;
		}
		return DungeonGenerator.m_tempRooms[UnityEngine.Random.Range(0, DungeonGenerator.m_tempRooms.Count)];
	}

	private void PlaceRooms(ZoneSystem.SpawnMode mode)
	{
		for (int i = 0; i < this.m_maxRooms; i++)
		{
			this.PlaceOneRoom(mode);
			if (this.CheckRequiredRooms() && DungeonGenerator.m_placedRooms.Count > this.m_minRooms)
			{
				ZLog.Log("All required rooms have been placed, stopping generation");
				return;
			}
		}
	}

	private void PlaceStartRoom(ZoneSystem.SpawnMode mode)
	{
		DungeonDB.RoomData roomData = this.FindStartRoom();
		RoomConnection entrance = roomData.m_room.GetEntrance();
		Quaternion rotation = base.transform.rotation;
		Vector3 pos;
		Quaternion rot;
		this.CalculateRoomPosRot(entrance, base.transform.position, rotation, out pos, out rot);
		this.PlaceRoom(roomData, pos, rot, entrance, mode);
	}

	private bool PlaceOneRoom(ZoneSystem.SpawnMode mode)
	{
		RoomConnection openConnection = this.GetOpenConnection();
		if (openConnection == null)
		{
			return false;
		}
		for (int i = 0; i < 10; i++)
		{
			DungeonDB.RoomData randomRoom = this.GetRandomRoom(openConnection);
			if (randomRoom == null)
			{
				break;
			}
			if (this.PlaceRoom(openConnection, randomRoom, mode))
			{
				return true;
			}
		}
		return false;
	}

	private void CalculateRoomPosRot(RoomConnection roomCon, Vector3 exitPos, Quaternion exitRot, out Vector3 pos, out Quaternion rot)
	{
		Quaternion rhs = Quaternion.Inverse(roomCon.transform.localRotation);
		rot = exitRot * rhs;
		Vector3 localPosition = roomCon.transform.localPosition;
		pos = exitPos - rot * localPosition;
	}

	private bool PlaceRoom(RoomConnection connection, DungeonDB.RoomData roomData, ZoneSystem.SpawnMode mode)
	{
		Room room = roomData.m_room;
		Quaternion quaternion = connection.transform.rotation;
		quaternion *= Quaternion.Euler(0f, 180f, 0f);
		RoomConnection connection2 = room.GetConnection(connection);
		Vector3 pos;
		Quaternion rot;
		this.CalculateRoomPosRot(connection2, connection.transform.position, quaternion, out pos, out rot);
		if (room.m_size.x != 0 && room.m_size.z != 0 && this.TestCollision(room, pos, rot))
		{
			return false;
		}
		this.PlaceRoom(roomData, pos, rot, connection, mode);
		if (!room.m_endCap)
		{
			if (connection.m_allowDoor)
			{
				DungeonGenerator.m_doorConnections.Add(connection);
			}
			DungeonGenerator.m_openConnections.Remove(connection);
		}
		return true;
	}

	private void PlaceRoom(DungeonDB.RoomData room, Vector3 pos, Quaternion rot, RoomConnection fromConnection, ZoneSystem.SpawnMode mode)
	{
		int seed = (int)pos.x * 4271 + (int)pos.y * 9187 + (int)pos.z * 2134;
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		if (mode == ZoneSystem.SpawnMode.Full || mode == ZoneSystem.SpawnMode.Ghost)
		{
			foreach (ZNetView znetView in room.m_netViews)
			{
				znetView.gameObject.SetActive(true);
			}
			foreach (RandomSpawn randomSpawn in room.m_randomSpawns)
			{
				randomSpawn.Randomize();
			}
			Vector3 position = room.m_room.transform.position;
			Quaternion quaternion = Quaternion.Inverse(room.m_room.transform.rotation);
			using (List<ZNetView>.Enumerator enumerator = room.m_netViews.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ZNetView znetView2 = enumerator.Current;
					if (znetView2.gameObject.activeSelf)
					{
						Vector3 point = quaternion * (znetView2.gameObject.transform.position - position);
						Vector3 position2 = pos + rot * point;
						Quaternion rhs = quaternion * znetView2.gameObject.transform.rotation;
						Quaternion rotation = rot * rhs;
						GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(znetView2.gameObject, position2, rotation);
						ZNetView component = gameObject.GetComponent<ZNetView>();
						if (component.GetZDO() != null)
						{
							component.GetZDO().SetPGWVersion(ZoneSystem.instance.m_pgwVersion);
						}
						if (mode == ZoneSystem.SpawnMode.Ghost)
						{
							UnityEngine.Object.Destroy(gameObject);
						}
					}
				}
				goto IL_1E9;
			}
		}
		foreach (RandomSpawn randomSpawn2 in room.m_randomSpawns)
		{
			randomSpawn2.Randomize();
		}
		IL_1E9:
		foreach (ZNetView znetView3 in room.m_netViews)
		{
			znetView3.gameObject.SetActive(false);
		}
		Room component2 = UnityEngine.Object.Instantiate<GameObject>(room.m_room.gameObject, pos, rot, base.transform).GetComponent<Room>();
		component2.gameObject.name = room.m_room.gameObject.name;
		if (mode != ZoneSystem.SpawnMode.Client)
		{
			component2.m_placeOrder = (fromConnection ? (fromConnection.m_placeOrder + 1) : 0);
			DungeonGenerator.m_placedRooms.Add(component2);
			this.AddOpenConnections(component2, fromConnection);
		}
		UnityEngine.Random.state = state;
	}

	private void AddOpenConnections(Room newRoom, RoomConnection skipConnection)
	{
		RoomConnection[] connections = newRoom.GetConnections();
		if (skipConnection != null)
		{
			foreach (RoomConnection roomConnection in connections)
			{
				if (!roomConnection.m_entrance && Vector3.Distance(roomConnection.transform.position, skipConnection.transform.position) >= 0.1f)
				{
					roomConnection.m_placeOrder = newRoom.m_placeOrder;
					DungeonGenerator.m_openConnections.Add(roomConnection);
				}
			}
			return;
		}
		RoomConnection[] array = connections;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].m_placeOrder = newRoom.m_placeOrder;
		}
		DungeonGenerator.m_openConnections.AddRange(connections);
	}

	private void SetupColliders()
	{
		if (this.m_colliderA != null)
		{
			return;
		}
		BoxCollider[] componentsInChildren = base.gameObject.GetComponentsInChildren<BoxCollider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.DestroyImmediate(componentsInChildren[i]);
		}
		this.m_colliderA = base.gameObject.AddComponent<BoxCollider>();
		this.m_colliderB = base.gameObject.AddComponent<BoxCollider>();
	}

	public void Derp()
	{
	}

	private bool IsInsideDungeon(Room room, Vector3 pos, Quaternion rot)
	{
		Bounds bounds = new Bounds(this.m_zoneCenter, this.m_zoneSize);
		Vector3 vector = room.m_size;
		vector *= 0.5f;
		return bounds.Contains(pos + rot * new Vector3(vector.x, vector.y, -vector.z)) && bounds.Contains(pos + rot * new Vector3(-vector.x, vector.y, -vector.z)) && bounds.Contains(pos + rot * new Vector3(vector.x, vector.y, vector.z)) && bounds.Contains(pos + rot * new Vector3(-vector.x, vector.y, vector.z)) && bounds.Contains(pos + rot * new Vector3(vector.x, -vector.y, -vector.z)) && bounds.Contains(pos + rot * new Vector3(-vector.x, -vector.y, -vector.z)) && bounds.Contains(pos + rot * new Vector3(vector.x, -vector.y, vector.z)) && bounds.Contains(pos + rot * new Vector3(-vector.x, -vector.y, vector.z));
	}

	private bool TestCollision(Room room, Vector3 pos, Quaternion rot)
	{
		if (!this.IsInsideDungeon(room, pos, rot))
		{
			return true;
		}
		this.m_colliderA.size = new Vector3((float)room.m_size.x - 0.1f, (float)room.m_size.y - 0.1f, (float)room.m_size.z - 0.1f);
		foreach (Room room2 in DungeonGenerator.m_placedRooms)
		{
			this.m_colliderB.size = room2.m_size;
			Vector3 vector;
			float num;
			if (Physics.ComputePenetration(this.m_colliderA, pos, rot, this.m_colliderB, room2.transform.position, room2.transform.rotation, out vector, out num))
			{
				return true;
			}
		}
		return false;
	}

	private DungeonDB.RoomData GetRandomWeightedRoom(bool perimeterRoom)
	{
		DungeonGenerator.m_tempRooms.Clear();
		float num = 0f;
		foreach (DungeonDB.RoomData roomData in DungeonGenerator.m_availableRooms)
		{
			if (!roomData.m_room.m_entrance && !roomData.m_room.m_endCap && roomData.m_room.m_perimeter == perimeterRoom)
			{
				num += roomData.m_room.m_weight;
				DungeonGenerator.m_tempRooms.Add(roomData);
			}
		}
		if (DungeonGenerator.m_tempRooms.Count == 0)
		{
			return null;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		foreach (DungeonDB.RoomData roomData2 in DungeonGenerator.m_tempRooms)
		{
			num3 += roomData2.m_room.m_weight;
			if (num2 <= num3)
			{
				return roomData2;
			}
		}
		return DungeonGenerator.m_tempRooms[0];
	}

	private DungeonDB.RoomData GetRandomRoom(RoomConnection connection)
	{
		DungeonGenerator.m_tempRooms.Clear();
		foreach (DungeonDB.RoomData roomData in DungeonGenerator.m_availableRooms)
		{
			if (!roomData.m_room.m_entrance && !roomData.m_room.m_endCap && (!connection || (roomData.m_room.HaveConnection(connection) && connection.m_placeOrder >= roomData.m_room.m_minPlaceOrder)))
			{
				DungeonGenerator.m_tempRooms.Add(roomData);
			}
		}
		if (DungeonGenerator.m_tempRooms.Count == 0)
		{
			return null;
		}
		return DungeonGenerator.m_tempRooms[UnityEngine.Random.Range(0, DungeonGenerator.m_tempRooms.Count)];
	}

	private RoomConnection GetOpenConnection()
	{
		if (DungeonGenerator.m_openConnections.Count == 0)
		{
			return null;
		}
		return DungeonGenerator.m_openConnections[UnityEngine.Random.Range(0, DungeonGenerator.m_openConnections.Count)];
	}

	private DungeonDB.RoomData FindStartRoom()
	{
		DungeonGenerator.m_tempRooms.Clear();
		foreach (DungeonDB.RoomData roomData in DungeonGenerator.m_availableRooms)
		{
			if (roomData.m_room.m_entrance)
			{
				DungeonGenerator.m_tempRooms.Add(roomData);
			}
		}
		return DungeonGenerator.m_tempRooms[UnityEngine.Random.Range(0, DungeonGenerator.m_tempRooms.Count)];
	}

	private bool CheckRequiredRooms()
	{
		if (this.m_minRequiredRooms == 0 || this.m_requiredRooms.Count == 0)
		{
			return false;
		}
		int num = 0;
		foreach (Room room in DungeonGenerator.m_placedRooms)
		{
			if (this.m_requiredRooms.Contains(room.gameObject.name))
			{
				num++;
			}
		}
		return num >= this.m_minRequiredRooms;
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0f, 1.5f, 0f, 0.5f);
		Gizmos.DrawWireCube(this.m_zoneCenter, new Vector3(this.m_zoneSize.x, this.m_zoneSize.y, this.m_zoneSize.z));
		Gizmos.matrix = Matrix4x4.identity;
	}

	public DungeonGenerator.Algorithm m_algorithm;

	public int m_maxRooms = 3;

	public int m_minRooms = 20;

	public int m_minRequiredRooms;

	public List<string> m_requiredRooms = new List<string>();

	[BitMask(typeof(Room.Theme))]
	public Room.Theme m_themes = Room.Theme.Crypt;

	[Header("Dungeon")]
	public List<DungeonGenerator.DoorDef> m_doorTypes = new List<DungeonGenerator.DoorDef>();

	[Range(0f, 1f)]
	public float m_doorChance = 0.5f;

	[Header("Camp")]
	public float m_maxTilt = 10f;

	public float m_tileWidth = 8f;

	public int m_gridSize = 4;

	public float m_spawnChance = 1f;

	[Header("Camp radial")]
	public float m_campRadiusMin = 15f;

	public float m_campRadiusMax = 30f;

	public float m_minAltitude = 1f;

	public int m_perimeterSections;

	public float m_perimeterBuffer = 2f;

	[Header("Misc")]
	public Vector3 m_zoneCenter = new Vector3(0f, 0f, 0f);

	public Vector3 m_zoneSize = new Vector3(64f, 64f, 64f);

	private static List<Room> m_placedRooms = new List<Room>();

	private static List<RoomConnection> m_openConnections = new List<RoomConnection>();

	private static List<RoomConnection> m_doorConnections = new List<RoomConnection>();

	private static List<DungeonDB.RoomData> m_availableRooms = new List<DungeonDB.RoomData>();

	private static List<DungeonDB.RoomData> m_tempRooms = new List<DungeonDB.RoomData>();

	private BoxCollider m_colliderA;

	private BoxCollider m_colliderB;

	private ZNetView m_nview;

	[Serializable]
	public class DoorDef
	{
		public GameObject m_prefab;

		public string m_connectionType = "";
	}

	public enum Algorithm
	{
		Dungeon,
		CampGrid,
		CampRadial
	}
}
