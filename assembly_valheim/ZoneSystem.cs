using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZoneSystem : MonoBehaviour
{
	public static ZoneSystem instance
	{
		get
		{
			return ZoneSystem.m_instance;
		}
	}

	private void Awake()
	{
		ZoneSystem.m_instance = this;
		this.m_terrainRayMask = LayerMask.GetMask(new string[]
		{
			"terrain"
		});
		this.m_blockRayMask = LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"Default_small",
			"piece"
		});
		this.m_solidRayMask = LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"Default_small",
			"piece",
			"terrain"
		});
		SceneManager.LoadScene("locations", LoadSceneMode.Additive);
		ZLog.Log("Zonesystem Awake " + Time.frameCount);
	}

	private void Start()
	{
		ZLog.Log("Zonesystem Start " + Time.frameCount);
		this.SetupLocations();
		this.ValidateVegetation();
		if (!this.m_locationsGenerated && ZNet.instance.IsServer())
		{
			this.GenerateLocations();
		}
		ZRoutedRpc instance = ZRoutedRpc.instance;
		instance.m_onNewPeer = (Action<long>)Delegate.Combine(instance.m_onNewPeer, new Action<long>(this.OnNewPeer));
		if (ZNet.instance.IsServer())
		{
			ZRoutedRpc.instance.Register<string>("SetGlobalKey", new Action<long, string>(this.RPC_SetGlobalKey));
			return;
		}
		ZRoutedRpc.instance.Register<List<string>>("GlobalKeys", new Action<long, List<string>>(this.RPC_GlobalKeys));
		ZRoutedRpc.instance.Register<ZPackage>("LocationIcons", new Action<long, ZPackage>(this.RPC_LocationIcons));
	}

	private void SendGlobalKeys(long peer)
	{
		List<string> list = new List<string>(this.m_globalKeys);
		ZRoutedRpc.instance.InvokeRoutedRPC(peer, "GlobalKeys", new object[]
		{
			list
		});
	}

	private void RPC_GlobalKeys(long sender, List<string> keys)
	{
		ZLog.Log("client got keys " + keys.Count);
		this.m_globalKeys.Clear();
		foreach (string item in keys)
		{
			this.m_globalKeys.Add(item);
		}
	}

	private void SendLocationIcons(long peer)
	{
		ZPackage zpackage = new ZPackage();
		this.tempIconList.Clear();
		this.GetLocationIcons(this.tempIconList);
		zpackage.Write(this.tempIconList.Count);
		foreach (KeyValuePair<Vector3, string> keyValuePair in this.tempIconList)
		{
			zpackage.Write(keyValuePair.Key);
			zpackage.Write(keyValuePair.Value);
		}
		ZRoutedRpc.instance.InvokeRoutedRPC(peer, "LocationIcons", new object[]
		{
			zpackage
		});
	}

	private void RPC_LocationIcons(long sender, ZPackage pkg)
	{
		ZLog.Log("client got location icons");
		this.m_locationIcons.Clear();
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			Vector3 key = pkg.ReadVector3();
			string value = pkg.ReadString();
			this.m_locationIcons[key] = value;
		}
		ZLog.Log("Icons:" + num);
	}

	private void OnNewPeer(long peerID)
	{
		if (ZNet.instance.IsServer())
		{
			ZLog.Log("Server: New peer connected,sending global keys");
			this.SendGlobalKeys(peerID);
			this.SendLocationIcons(peerID);
		}
	}

	private void SetupLocations()
	{
		GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
		GameObject gameObject = null;
		foreach (GameObject gameObject2 in array)
		{
			if (gameObject2.name == "_Locations")
			{
				gameObject = gameObject2;
				break;
			}
		}
		Location[] componentsInChildren = gameObject.GetComponentsInChildren<Location>(true);
		Location[] array3 = componentsInChildren;
		for (int i = 0; i < array3.Length; i++)
		{
			if (array3[i].transform.gameObject.activeInHierarchy)
			{
				this.m_error = true;
			}
		}
		foreach (ZoneSystem.ZoneLocation zoneLocation in this.m_locations)
		{
			Transform transform = null;
			foreach (Location location in componentsInChildren)
			{
				if (location.gameObject.name == zoneLocation.m_prefabName)
				{
					transform = location.transform;
					break;
				}
			}
			if (!(transform == null) || zoneLocation.m_enable)
			{
				zoneLocation.m_prefab = transform.gameObject;
				zoneLocation.m_hash = zoneLocation.m_prefab.name.GetStableHashCode();
				Location componentInChildren = zoneLocation.m_prefab.GetComponentInChildren<Location>();
				zoneLocation.m_location = componentInChildren;
				zoneLocation.m_interiorRadius = (componentInChildren.m_hasInterior ? componentInChildren.m_interiorRadius : 0f);
				zoneLocation.m_exteriorRadius = componentInChildren.m_exteriorRadius;
				if (Application.isPlaying)
				{
					ZoneSystem.PrepareNetViews(zoneLocation.m_prefab, zoneLocation.m_netViews);
					ZoneSystem.PrepareRandomSpawns(zoneLocation.m_prefab, zoneLocation.m_randomSpawns);
					if (!this.m_locationsByHash.ContainsKey(zoneLocation.m_hash))
					{
						this.m_locationsByHash.Add(zoneLocation.m_hash, zoneLocation);
					}
				}
			}
		}
	}

	public static void PrepareNetViews(GameObject root, List<ZNetView> views)
	{
		views.Clear();
		foreach (ZNetView znetView in root.GetComponentsInChildren<ZNetView>(true))
		{
			if (Utils.IsEnabledInheirarcy(znetView.gameObject, root))
			{
				views.Add(znetView);
			}
		}
	}

	public static void PrepareRandomSpawns(GameObject root, List<RandomSpawn> randomSpawns)
	{
		randomSpawns.Clear();
		foreach (RandomSpawn randomSpawn in root.GetComponentsInChildren<RandomSpawn>(true))
		{
			if (Utils.IsEnabledInheirarcy(randomSpawn.gameObject, root))
			{
				randomSpawns.Add(randomSpawn);
				randomSpawn.Prepare();
			}
		}
	}

	private void OnDestroy()
	{
		ZoneSystem.m_instance = null;
	}

	private void ValidateVegetation()
	{
		foreach (ZoneSystem.ZoneVegetation zoneVegetation in this.m_vegetation)
		{
			if (zoneVegetation.m_enable && zoneVegetation.m_prefab && zoneVegetation.m_prefab.GetComponent<ZNetView>() == null)
			{
				ZLog.LogError(string.Concat(new string[]
				{
					"Vegetation ",
					zoneVegetation.m_prefab.name,
					" [ ",
					zoneVegetation.m_name,
					"] is missing ZNetView"
				}));
			}
		}
	}

	public void PrepareSave()
	{
		this.m_tempGeneratedZonesSaveClone = new HashSet<Vector2i>(this.m_generatedZones);
		this.m_tempGlobalKeysSaveClone = new HashSet<string>(this.m_globalKeys);
		this.m_tempLocationsSaveClone = new List<ZoneSystem.LocationInstance>(this.m_locationInstances.Values);
		this.m_tempLocationsGeneratedSaveClone = this.m_locationsGenerated;
	}

	public void SaveASync(BinaryWriter writer)
	{
		writer.Write(this.m_tempGeneratedZonesSaveClone.Count);
		foreach (Vector2i vector2i in this.m_tempGeneratedZonesSaveClone)
		{
			writer.Write(vector2i.x);
			writer.Write(vector2i.y);
		}
		writer.Write(this.m_pgwVersion);
		writer.Write(this.m_locationVersion);
		writer.Write(this.m_tempGlobalKeysSaveClone.Count);
		foreach (string value in this.m_tempGlobalKeysSaveClone)
		{
			writer.Write(value);
		}
		writer.Write(this.m_tempLocationsGeneratedSaveClone);
		writer.Write(this.m_tempLocationsSaveClone.Count);
		foreach (ZoneSystem.LocationInstance locationInstance in this.m_tempLocationsSaveClone)
		{
			writer.Write(locationInstance.m_location.m_prefabName);
			writer.Write(locationInstance.m_position.x);
			writer.Write(locationInstance.m_position.y);
			writer.Write(locationInstance.m_position.z);
			writer.Write(locationInstance.m_placed);
		}
		this.m_tempGeneratedZonesSaveClone.Clear();
		this.m_tempGeneratedZonesSaveClone = null;
		this.m_tempGlobalKeysSaveClone.Clear();
		this.m_tempGlobalKeysSaveClone = null;
		this.m_tempLocationsSaveClone.Clear();
		this.m_tempLocationsSaveClone = null;
	}

	public void Load(BinaryReader reader, int version)
	{
		this.m_generatedZones.Clear();
		int num = reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			Vector2i item = default(Vector2i);
			item.x = reader.ReadInt32();
			item.y = reader.ReadInt32();
			this.m_generatedZones.Add(item);
		}
		if (version >= 13)
		{
			int num2 = reader.ReadInt32();
			int num3 = (version >= 21) ? reader.ReadInt32() : 0;
			if (num2 != this.m_pgwVersion)
			{
				this.m_generatedZones.Clear();
			}
			if (version >= 14)
			{
				this.m_globalKeys.Clear();
				int num4 = reader.ReadInt32();
				for (int j = 0; j < num4; j++)
				{
					string item2 = reader.ReadString();
					this.m_globalKeys.Add(item2);
				}
			}
			if (version >= 18)
			{
				if (version >= 20)
				{
					this.m_locationsGenerated = reader.ReadBoolean();
				}
				this.m_locationInstances.Clear();
				int num5 = reader.ReadInt32();
				for (int k = 0; k < num5; k++)
				{
					string text = reader.ReadString();
					Vector3 zero = Vector3.zero;
					zero.x = reader.ReadSingle();
					zero.y = reader.ReadSingle();
					zero.z = reader.ReadSingle();
					bool generated = false;
					if (version >= 19)
					{
						generated = reader.ReadBoolean();
					}
					ZoneSystem.ZoneLocation location = this.GetLocation(text);
					if (location != null)
					{
						this.RegisterLocation(location, zero, generated);
					}
					else
					{
						ZLog.DevLog("Failed to find location " + text);
					}
				}
				ZLog.Log("Loaded " + num5 + " locations");
				if (num2 != this.m_pgwVersion)
				{
					this.m_locationInstances.Clear();
					this.m_locationsGenerated = false;
				}
				if (num3 != this.m_locationVersion)
				{
					this.m_locationsGenerated = false;
				}
			}
		}
	}

	private void Update()
	{
		if (ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
		{
			return;
		}
		this.m_updateTimer += Time.deltaTime;
		if (this.m_updateTimer > 0.1f)
		{
			this.m_updateTimer = 0f;
			bool flag = this.CreateLocalZones(ZNet.instance.GetReferencePosition());
			this.UpdateTTL(0.1f);
			if (ZNet.instance.IsServer() && !flag)
			{
				this.CreateGhostZones(ZNet.instance.GetReferencePosition());
				foreach (ZNetPeer znetPeer in ZNet.instance.GetPeers())
				{
					this.CreateGhostZones(znetPeer.GetRefPos());
				}
			}
		}
	}

	private bool CreateGhostZones(Vector3 refPoint)
	{
		Vector2i zone = this.GetZone(refPoint);
		GameObject gameObject;
		if (!this.IsZoneGenerated(zone) && this.SpawnZone(zone, ZoneSystem.SpawnMode.Ghost, out gameObject))
		{
			return true;
		}
		int num = this.m_activeArea + this.m_activeDistantArea;
		for (int i = zone.y - num; i <= zone.y + num; i++)
		{
			for (int j = zone.x - num; j <= zone.x + num; j++)
			{
				Vector2i zoneID = new Vector2i(j, i);
				GameObject gameObject2;
				if (!this.IsZoneGenerated(zoneID) && this.SpawnZone(zoneID, ZoneSystem.SpawnMode.Ghost, out gameObject2))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool CreateLocalZones(Vector3 refPoint)
	{
		Vector2i zone = this.GetZone(refPoint);
		if (this.PokeLocalZone(zone))
		{
			return true;
		}
		for (int i = zone.y - this.m_activeArea; i <= zone.y + this.m_activeArea; i++)
		{
			for (int j = zone.x - this.m_activeArea; j <= zone.x + this.m_activeArea; j++)
			{
				Vector2i vector2i = new Vector2i(j, i);
				if (!(vector2i == zone) && this.PokeLocalZone(vector2i))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool PokeLocalZone(Vector2i zoneID)
	{
		ZoneSystem.ZoneData zoneData;
		if (this.m_zones.TryGetValue(zoneID, out zoneData))
		{
			zoneData.m_ttl = 0f;
			return false;
		}
		ZoneSystem.SpawnMode mode = (ZNet.instance.IsServer() && !this.IsZoneGenerated(zoneID)) ? ZoneSystem.SpawnMode.Full : ZoneSystem.SpawnMode.Client;
		GameObject root;
		if (this.SpawnZone(zoneID, mode, out root))
		{
			ZoneSystem.ZoneData zoneData2 = new ZoneSystem.ZoneData();
			zoneData2.m_root = root;
			this.m_zones.Add(zoneID, zoneData2);
			return true;
		}
		return false;
	}

	public bool IsZoneLoaded(Vector3 point)
	{
		Vector2i zone = this.GetZone(point);
		return this.IsZoneLoaded(zone);
	}

	public bool IsZoneLoaded(Vector2i zoneID)
	{
		return this.m_zones.ContainsKey(zoneID);
	}

	private bool SpawnZone(Vector2i zoneID, ZoneSystem.SpawnMode mode, out GameObject root)
	{
		Vector3 zonePos = this.GetZonePos(zoneID);
		Heightmap componentInChildren = this.m_zonePrefab.GetComponentInChildren<Heightmap>();
		if (!HeightmapBuilder.instance.IsTerrainReady(zonePos, componentInChildren.m_width, componentInChildren.m_scale, componentInChildren.m_isDistantLod, WorldGenerator.instance))
		{
			root = null;
			return false;
		}
		root = UnityEngine.Object.Instantiate<GameObject>(this.m_zonePrefab, zonePos, Quaternion.identity);
		if ((mode == ZoneSystem.SpawnMode.Ghost || mode == ZoneSystem.SpawnMode.Full) && !this.IsZoneGenerated(zoneID))
		{
			Heightmap componentInChildren2 = root.GetComponentInChildren<Heightmap>();
			this.m_tempClearAreas.Clear();
			this.m_tempSpawnedObjects.Clear();
			this.PlaceLocations(zoneID, zonePos, root.transform, componentInChildren2, this.m_tempClearAreas, mode, this.m_tempSpawnedObjects);
			this.PlaceVegetation(zoneID, zonePos, root.transform, componentInChildren2, this.m_tempClearAreas, mode, this.m_tempSpawnedObjects);
			this.PlaceZoneCtrl(zoneID, zonePos, mode, this.m_tempSpawnedObjects);
			if (mode == ZoneSystem.SpawnMode.Ghost)
			{
				foreach (GameObject obj in this.m_tempSpawnedObjects)
				{
					UnityEngine.Object.Destroy(obj);
				}
				this.m_tempSpawnedObjects.Clear();
				UnityEngine.Object.Destroy(root);
				root = null;
			}
			this.SetZoneGenerated(zoneID);
		}
		return true;
	}

	private void PlaceZoneCtrl(Vector2i zoneID, Vector3 zoneCenterPos, ZoneSystem.SpawnMode mode, List<GameObject> spawnedObjects)
	{
		if (mode == ZoneSystem.SpawnMode.Full || mode == ZoneSystem.SpawnMode.Ghost)
		{
			if (mode == ZoneSystem.SpawnMode.Ghost)
			{
				ZNetView.StartGhostInit();
			}
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_zoneCtrlPrefab, zoneCenterPos, Quaternion.identity);
			gameObject.GetComponent<ZNetView>().GetZDO().SetPGWVersion(this.m_pgwVersion);
			if (mode == ZoneSystem.SpawnMode.Ghost)
			{
				spawnedObjects.Add(gameObject);
				ZNetView.FinishGhostInit();
			}
		}
	}

	private Vector3 GetRandomPointInRadius(Vector3 center, float radius)
	{
		float f = UnityEngine.Random.value * 3.1415927f * 2f;
		float num = UnityEngine.Random.Range(0f, radius);
		return center + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
	}

	private void PlaceVegetation(Vector2i zoneID, Vector3 zoneCenterPos, Transform parent, Heightmap hmap, List<ZoneSystem.ClearArea> clearAreas, ZoneSystem.SpawnMode mode, List<GameObject> spawnedObjects)
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		int seed = WorldGenerator.instance.GetSeed();
		float num = this.m_zoneSize / 2f;
		int num2 = 1;
		foreach (ZoneSystem.ZoneVegetation zoneVegetation in this.m_vegetation)
		{
			num2++;
			if (zoneVegetation.m_enable && hmap.HaveBiome(zoneVegetation.m_biome))
			{
				UnityEngine.Random.InitState(seed + zoneID.x * 4271 + zoneID.y * 9187 + zoneVegetation.m_prefab.name.GetStableHashCode());
				int num3 = 1;
				if (zoneVegetation.m_max < 1f)
				{
					if (UnityEngine.Random.value > zoneVegetation.m_max)
					{
						continue;
					}
				}
				else
				{
					num3 = UnityEngine.Random.Range((int)zoneVegetation.m_min, (int)zoneVegetation.m_max + 1);
				}
				bool flag = zoneVegetation.m_prefab.GetComponent<ZNetView>() != null;
				float num4 = Mathf.Cos(0.017453292f * zoneVegetation.m_maxTilt);
				float num5 = Mathf.Cos(0.017453292f * zoneVegetation.m_minTilt);
				float num6 = num - zoneVegetation.m_groupRadius;
				int num7 = zoneVegetation.m_forcePlacement ? (num3 * 50) : num3;
				int num8 = 0;
				for (int i = 0; i < num7; i++)
				{
					Vector3 vector = new Vector3(UnityEngine.Random.Range(zoneCenterPos.x - num6, zoneCenterPos.x + num6), 0f, UnityEngine.Random.Range(zoneCenterPos.z - num6, zoneCenterPos.z + num6));
					int num9 = UnityEngine.Random.Range(zoneVegetation.m_groupSizeMin, zoneVegetation.m_groupSizeMax + 1);
					bool flag2 = false;
					for (int j = 0; j < num9; j++)
					{
						Vector3 vector2 = (j == 0) ? vector : this.GetRandomPointInRadius(vector, zoneVegetation.m_groupRadius);
						float num10 = (float)UnityEngine.Random.Range(0, 360);
						float num11 = UnityEngine.Random.Range(zoneVegetation.m_scaleMin, zoneVegetation.m_scaleMax);
						float x = UnityEngine.Random.Range(-zoneVegetation.m_randTilt, zoneVegetation.m_randTilt);
						float z = UnityEngine.Random.Range(-zoneVegetation.m_randTilt, zoneVegetation.m_randTilt);
						if (!zoneVegetation.m_blockCheck || !this.IsBlocked(vector2))
						{
							Vector3 vector3;
							Heightmap.Biome biome;
							Heightmap.BiomeArea biomeArea;
							Heightmap heightmap;
							this.GetGroundData(ref vector2, out vector3, out biome, out biomeArea, out heightmap);
							if ((zoneVegetation.m_biome & biome) != Heightmap.Biome.None && (zoneVegetation.m_biomeArea & biomeArea) != (Heightmap.BiomeArea)0)
							{
								float num12 = vector2.y - this.m_waterLevel;
								if (num12 >= zoneVegetation.m_minAltitude && num12 <= zoneVegetation.m_maxAltitude)
								{
									if (zoneVegetation.m_minOceanDepth != zoneVegetation.m_maxOceanDepth)
									{
										float oceanDepth = heightmap.GetOceanDepth(vector2);
										if (oceanDepth < zoneVegetation.m_minOceanDepth || oceanDepth > zoneVegetation.m_maxOceanDepth)
										{
											goto IL_437;
										}
									}
									if (vector3.y >= num4 && vector3.y <= num5)
									{
										if (zoneVegetation.m_terrainDeltaRadius > 0f)
										{
											float num13;
											Vector3 vector4;
											this.GetTerrainDelta(vector2, zoneVegetation.m_terrainDeltaRadius, out num13, out vector4);
											if (num13 > zoneVegetation.m_maxTerrainDelta || num13 < zoneVegetation.m_minTerrainDelta)
											{
												goto IL_437;
											}
										}
										if (zoneVegetation.m_inForest)
										{
											float forestFactor = WorldGenerator.GetForestFactor(vector2);
											if (forestFactor < zoneVegetation.m_forestTresholdMin || forestFactor > zoneVegetation.m_forestTresholdMax)
											{
												goto IL_437;
											}
										}
										if (!this.InsideClearArea(clearAreas, vector2))
										{
											if (zoneVegetation.m_snapToWater)
											{
												vector2.y = this.m_waterLevel;
											}
											vector2.y += zoneVegetation.m_groundOffset;
											Quaternion rotation = Quaternion.identity;
											if (zoneVegetation.m_chanceToUseGroundTilt > 0f && UnityEngine.Random.value <= zoneVegetation.m_chanceToUseGroundTilt)
											{
												rotation = Quaternion.AngleAxis(num10, vector3);
											}
											else
											{
												rotation = Quaternion.Euler(x, num10, z);
											}
											if (flag)
											{
												if (mode == ZoneSystem.SpawnMode.Full || mode == ZoneSystem.SpawnMode.Ghost)
												{
													if (mode == ZoneSystem.SpawnMode.Ghost)
													{
														ZNetView.StartGhostInit();
													}
													GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(zoneVegetation.m_prefab, vector2, rotation);
													ZNetView component = gameObject.GetComponent<ZNetView>();
													component.SetLocalScale(new Vector3(num11, num11, num11));
													component.GetZDO().SetPGWVersion(this.m_pgwVersion);
													if (mode == ZoneSystem.SpawnMode.Ghost)
													{
														spawnedObjects.Add(gameObject);
														ZNetView.FinishGhostInit();
													}
												}
											}
											else
											{
												GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(zoneVegetation.m_prefab, vector2, rotation);
												gameObject2.transform.localScale = new Vector3(num11, num11, num11);
												gameObject2.transform.SetParent(parent, true);
											}
											flag2 = true;
										}
									}
								}
							}
						}
						IL_437:;
					}
					if (flag2)
					{
						num8++;
					}
					if (num8 >= num3)
					{
						break;
					}
				}
			}
		}
		UnityEngine.Random.state = state;
	}

	private bool InsideClearArea(List<ZoneSystem.ClearArea> areas, Vector3 point)
	{
		foreach (ZoneSystem.ClearArea clearArea in areas)
		{
			if (point.x > clearArea.m_center.x - clearArea.m_radius && point.x < clearArea.m_center.x + clearArea.m_radius && point.z > clearArea.m_center.z - clearArea.m_radius && point.z < clearArea.m_center.z + clearArea.m_radius)
			{
				return true;
			}
		}
		return false;
	}

	private ZoneSystem.ZoneLocation GetLocation(int hash)
	{
		ZoneSystem.ZoneLocation result;
		if (this.m_locationsByHash.TryGetValue(hash, out result))
		{
			return result;
		}
		return null;
	}

	private ZoneSystem.ZoneLocation GetLocation(string name)
	{
		foreach (ZoneSystem.ZoneLocation zoneLocation in this.m_locations)
		{
			if (zoneLocation.m_prefabName == name)
			{
				return zoneLocation;
			}
		}
		return null;
	}

	private void ClearNonPlacedLocations()
	{
		Dictionary<Vector2i, ZoneSystem.LocationInstance> dictionary = new Dictionary<Vector2i, ZoneSystem.LocationInstance>();
		foreach (KeyValuePair<Vector2i, ZoneSystem.LocationInstance> keyValuePair in this.m_locationInstances)
		{
			if (keyValuePair.Value.m_placed)
			{
				dictionary.Add(keyValuePair.Key, keyValuePair.Value);
			}
		}
		this.m_locationInstances = dictionary;
	}

	private void CheckLocationDuplicates()
	{
		ZLog.Log("Checking for location duplicates");
		for (int i = 0; i < this.m_locations.Count; i++)
		{
			ZoneSystem.ZoneLocation zoneLocation = this.m_locations[i];
			if (zoneLocation.m_enable)
			{
				for (int j = i + 1; j < this.m_locations.Count; j++)
				{
					ZoneSystem.ZoneLocation zoneLocation2 = this.m_locations[j];
					if (zoneLocation2.m_enable && zoneLocation.m_prefabName == zoneLocation2.m_prefabName)
					{
						ZLog.LogWarning("Two locations points to the same location prefab " + zoneLocation.m_prefabName);
					}
				}
			}
		}
	}

	public void GenerateLocations()
	{
		if (!Application.isPlaying)
		{
			ZLog.Log("Setting up locations");
			this.SetupLocations();
		}
		ZLog.Log("Generating locations");
		DateTime now = DateTime.Now;
		this.m_locationsGenerated = true;
		UnityEngine.Random.State state = UnityEngine.Random.state;
		this.CheckLocationDuplicates();
		this.ClearNonPlacedLocations();
		foreach (ZoneSystem.ZoneLocation zoneLocation in from a in this.m_locations
		orderby a.m_prioritized descending
		select a)
		{
			if (zoneLocation.m_enable && zoneLocation.m_quantity != 0)
			{
				this.GenerateLocations(zoneLocation);
			}
		}
		UnityEngine.Random.state = state;
		ZLog.Log(" Done generating locations, duration:" + (DateTime.Now - now).TotalMilliseconds + " ms");
	}

	private int CountNrOfLocation(ZoneSystem.ZoneLocation location)
	{
		int num = 0;
		using (Dictionary<Vector2i, ZoneSystem.LocationInstance>.ValueCollection.Enumerator enumerator = this.m_locationInstances.Values.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_location.m_prefabName == location.m_prefabName)
				{
					num++;
				}
			}
		}
		if (num > 0)
		{
			ZLog.Log(string.Concat(new object[]
			{
				"Old location found ",
				location.m_prefabName,
				" x ",
				num
			}));
		}
		return num;
	}

	private void GenerateLocations(ZoneSystem.ZoneLocation location)
	{
		DateTime now = DateTime.Now;
		UnityEngine.Random.InitState(WorldGenerator.instance.GetSeed() + location.m_prefabName.GetStableHashCode());
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		float locationRadius = Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius);
		int num9 = 0;
		int num10 = this.CountNrOfLocation(location);
		float num11 = 10000f;
		if (location.m_centerFirst)
		{
			num11 = location.m_minDistance;
		}
		if (location.m_unique && num10 > 0)
		{
			return;
		}
		int num12 = 0;
		while (num12 < 100000 && num10 < location.m_quantity)
		{
			Vector2i randomZone = this.GetRandomZone(num11);
			if (location.m_centerFirst)
			{
				num11 += 1f;
			}
			if (this.m_locationInstances.ContainsKey(randomZone))
			{
				num++;
			}
			else if (!this.IsZoneGenerated(randomZone))
			{
				Vector3 zonePos = this.GetZonePos(randomZone);
				Heightmap.BiomeArea biomeArea = WorldGenerator.instance.GetBiomeArea(zonePos);
				if ((location.m_biomeArea & biomeArea) == (Heightmap.BiomeArea)0)
				{
					num4++;
				}
				else
				{
					for (int i = 0; i < 20; i++)
					{
						num9++;
						Vector3 randomPointInZone = this.GetRandomPointInZone(randomZone, locationRadius);
						float magnitude = randomPointInZone.magnitude;
						if (location.m_minDistance != 0f && magnitude < location.m_minDistance)
						{
							num2++;
						}
						else if (location.m_maxDistance != 0f && magnitude > location.m_maxDistance)
						{
							num2++;
						}
						else
						{
							Heightmap.Biome biome = WorldGenerator.instance.GetBiome(randomPointInZone);
							if ((location.m_biome & biome) == Heightmap.Biome.None)
							{
								num3++;
							}
							else
							{
								randomPointInZone.y = WorldGenerator.instance.GetHeight(randomPointInZone.x, randomPointInZone.z);
								float num13 = randomPointInZone.y - this.m_waterLevel;
								if (num13 < location.m_minAltitude || num13 > location.m_maxAltitude)
								{
									num5++;
								}
								else
								{
									if (location.m_inForest)
									{
										float forestFactor = WorldGenerator.GetForestFactor(randomPointInZone);
										if (forestFactor < location.m_forestTresholdMin || forestFactor > location.m_forestTresholdMax)
										{
											num6++;
											goto IL_266;
										}
									}
									float num14;
									Vector3 vector;
									WorldGenerator.instance.GetTerrainDelta(randomPointInZone, location.m_exteriorRadius, out num14, out vector);
									if (num14 > location.m_maxTerrainDelta || num14 < location.m_minTerrainDelta)
									{
										num8++;
									}
									else
									{
										if (location.m_minDistanceFromSimilar <= 0f || !this.HaveLocationInRange(location.m_prefabName, location.m_group, randomPointInZone, location.m_minDistanceFromSimilar))
										{
											this.RegisterLocation(location, randomPointInZone, false);
											num10++;
											break;
										}
										num7++;
									}
								}
							}
						}
						IL_266:;
					}
				}
			}
			num12++;
		}
		if (num10 < location.m_quantity)
		{
			ZLog.LogWarning(string.Concat(new object[]
			{
				"Failed to place all ",
				location.m_prefabName,
				", placed ",
				num10,
				" out of ",
				location.m_quantity
			}));
			ZLog.DevLog("errorLocationInZone " + num);
			ZLog.DevLog("errorCenterDistance " + num2);
			ZLog.DevLog("errorBiome " + num3);
			ZLog.DevLog("errorBiomeArea " + num4);
			ZLog.DevLog("errorAlt " + num5);
			ZLog.DevLog("errorForest " + num6);
			ZLog.DevLog("errorSimilar " + num7);
			ZLog.DevLog("errorTerrainDelta " + num8);
		}
		DateTime.Now - now;
	}

	private Vector2i GetRandomZone(float range)
	{
		int num = (int)range / (int)this.m_zoneSize;
		Vector2i vector2i;
		do
		{
			vector2i = new Vector2i(UnityEngine.Random.Range(-num, num), UnityEngine.Random.Range(-num, num));
		}
		while (this.GetZonePos(vector2i).magnitude >= 10000f);
		return vector2i;
	}

	private Vector3 GetRandomPointInZone(Vector2i zone, float locationRadius)
	{
		Vector3 zonePos = this.GetZonePos(zone);
		float num = this.m_zoneSize / 2f;
		float x = UnityEngine.Random.Range(-num + locationRadius, num - locationRadius);
		float z = UnityEngine.Random.Range(-num + locationRadius, num - locationRadius);
		return zonePos + new Vector3(x, 0f, z);
	}

	private Vector3 GetRandomPointInZone(float locationRadius)
	{
		Vector3 point = new Vector3(UnityEngine.Random.Range(-10000f, 10000f), 0f, UnityEngine.Random.Range(-10000f, 10000f));
		Vector2i zone = this.GetZone(point);
		Vector3 zonePos = this.GetZonePos(zone);
		float num = this.m_zoneSize / 2f;
		return new Vector3(UnityEngine.Random.Range(zonePos.x - num + locationRadius, zonePos.x + num - locationRadius), 0f, UnityEngine.Random.Range(zonePos.z - num + locationRadius, zonePos.z + num - locationRadius));
	}

	private void PlaceLocations(Vector2i zoneID, Vector3 zoneCenterPos, Transform parent, Heightmap hmap, List<ZoneSystem.ClearArea> clearAreas, ZoneSystem.SpawnMode mode, List<GameObject> spawnedObjects)
	{
		if (!this.m_locationsGenerated)
		{
			this.GenerateLocations();
		}
		DateTime now = DateTime.Now;
		ZoneSystem.LocationInstance locationInstance;
		if (this.m_locationInstances.TryGetValue(zoneID, out locationInstance))
		{
			if (locationInstance.m_placed)
			{
				return;
			}
			Vector3 position = locationInstance.m_position;
			Vector3 vector;
			Heightmap.Biome biome;
			Heightmap.BiomeArea biomeArea;
			Heightmap heightmap;
			this.GetGroundData(ref position, out vector, out biome, out biomeArea, out heightmap);
			if (locationInstance.m_location.m_snapToWater)
			{
				position.y = this.m_waterLevel;
			}
			if (locationInstance.m_location.m_location.m_clearArea)
			{
				ZoneSystem.ClearArea item = new ZoneSystem.ClearArea(position, locationInstance.m_location.m_exteriorRadius);
				clearAreas.Add(item);
			}
			Quaternion rot = Quaternion.identity;
			if (locationInstance.m_location.m_slopeRotation)
			{
				float num;
				Vector3 vector2;
				this.GetTerrainDelta(position, locationInstance.m_location.m_exteriorRadius, out num, out vector2);
				Vector3 forward = new Vector3(vector2.x, 0f, vector2.z);
				forward.Normalize();
				rot = Quaternion.LookRotation(forward);
				Vector3 eulerAngles = rot.eulerAngles;
				eulerAngles.y = Mathf.Round(eulerAngles.y / 22.5f) * 22.5f;
				rot.eulerAngles = eulerAngles;
			}
			else if (locationInstance.m_location.m_randomRotation)
			{
				rot = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 16) * 22.5f, 0f);
			}
			int seed = WorldGenerator.instance.GetSeed() + zoneID.x * 4271 + zoneID.y * 9187;
			this.SpawnLocation(locationInstance.m_location, seed, position, rot, mode, spawnedObjects);
			locationInstance.m_placed = true;
			this.m_locationInstances[zoneID] = locationInstance;
			TimeSpan timeSpan = DateTime.Now - now;
			ZLog.Log(string.Concat(new object[]
			{
				"Placed locations in zone ",
				zoneID,
				"  duration ",
				timeSpan.TotalMilliseconds,
				" ms"
			}));
			if (locationInstance.m_location.m_unique)
			{
				this.RemoveUnplacedLocations(locationInstance.m_location);
			}
			if (locationInstance.m_location.m_iconPlaced)
			{
				this.SendLocationIcons(ZRoutedRpc.Everybody);
			}
		}
	}

	private void RemoveUnplacedLocations(ZoneSystem.ZoneLocation location)
	{
		List<Vector2i> list = new List<Vector2i>();
		foreach (KeyValuePair<Vector2i, ZoneSystem.LocationInstance> keyValuePair in this.m_locationInstances)
		{
			if (keyValuePair.Value.m_location == location && !keyValuePair.Value.m_placed)
			{
				list.Add(keyValuePair.Key);
			}
		}
		foreach (Vector2i key in list)
		{
			this.m_locationInstances.Remove(key);
		}
		ZLog.DevLog("Removed " + list.Count.ToString() + " unplaced locations of type " + location.m_prefabName);
	}

	public bool TestSpawnLocation(string name, Vector3 pos)
	{
		if (!ZNet.instance.IsServer())
		{
			return false;
		}
		ZoneSystem.ZoneLocation location = this.GetLocation(name);
		if (location == null)
		{
			ZLog.Log("Missing location:" + name);
			global::Console.instance.Print("Missing location:" + name);
			return false;
		}
		if (location.m_prefab == null)
		{
			ZLog.Log("Missing prefab in location:" + name);
			global::Console.instance.Print("Missing location:" + name);
			return false;
		}
		float num = Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius);
		Vector2i zone = this.GetZone(pos);
		Vector3 zonePos = this.GetZonePos(zone);
		pos.x = Mathf.Clamp(pos.x, zonePos.x - this.m_zoneSize / 2f + num, zonePos.x + this.m_zoneSize / 2f - num);
		pos.z = Mathf.Clamp(pos.z, zonePos.z - this.m_zoneSize / 2f + num, zonePos.z + this.m_zoneSize / 2f - num);
		ZLog.Log(string.Concat(new object[]
		{
			"radius ",
			num,
			"  ",
			zonePos,
			" ",
			pos
		}));
		MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Location spawned, world saving DISABLED until restart", 0, null);
		this.m_didZoneTest = true;
		float y = (float)UnityEngine.Random.Range(0, 16) * 22.5f;
		List<GameObject> spawnedGhostObjects = new List<GameObject>();
		this.SpawnLocation(location, UnityEngine.Random.Range(0, 99999), pos, Quaternion.Euler(0f, y, 0f), ZoneSystem.SpawnMode.Full, spawnedGhostObjects);
		return true;
	}

	public GameObject SpawnProxyLocation(int hash, int seed, Vector3 pos, Quaternion rot)
	{
		ZoneSystem.ZoneLocation location = this.GetLocation(hash);
		if (location == null)
		{
			ZLog.LogWarning("Missing location:" + hash);
			return null;
		}
		List<GameObject> spawnedGhostObjects = new List<GameObject>();
		return this.SpawnLocation(location, seed, pos, rot, ZoneSystem.SpawnMode.Client, spawnedGhostObjects);
	}

	private GameObject SpawnLocation(ZoneSystem.ZoneLocation location, int seed, Vector3 pos, Quaternion rot, ZoneSystem.SpawnMode mode, List<GameObject> spawnedGhostObjects)
	{
		Vector3 position = location.m_prefab.transform.position;
		Quaternion lhs = Quaternion.Inverse(location.m_prefab.transform.rotation);
		UnityEngine.Random.InitState(seed);
		if (mode == ZoneSystem.SpawnMode.Full || mode == ZoneSystem.SpawnMode.Ghost)
		{
			foreach (ZNetView znetView in location.m_netViews)
			{
				znetView.gameObject.SetActive(true);
			}
			foreach (RandomSpawn randomSpawn in location.m_randomSpawns)
			{
				randomSpawn.Randomize();
			}
			WearNTear.m_randomInitialDamage = location.m_location.m_applyRandomDamage;
			foreach (ZNetView znetView2 in location.m_netViews)
			{
				if (znetView2.gameObject.activeSelf)
				{
					Vector3 point = znetView2.gameObject.transform.position - position;
					Vector3 position2 = pos + rot * point;
					Quaternion rhs = lhs * znetView2.gameObject.transform.rotation;
					Quaternion rotation = rot * rhs;
					if (mode == ZoneSystem.SpawnMode.Ghost)
					{
						ZNetView.StartGhostInit();
					}
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(znetView2.gameObject, position2, rotation);
					gameObject.GetComponent<ZNetView>().GetZDO().SetPGWVersion(this.m_pgwVersion);
					DungeonGenerator component = gameObject.GetComponent<DungeonGenerator>();
					if (component)
					{
						component.Generate(mode);
					}
					if (mode == ZoneSystem.SpawnMode.Ghost)
					{
						spawnedGhostObjects.Add(gameObject);
						ZNetView.FinishGhostInit();
					}
				}
			}
			WearNTear.m_randomInitialDamage = false;
			this.CreateLocationProxy(location, seed, pos, rot, mode, spawnedGhostObjects);
			SnapToGround.SnappAll();
			return null;
		}
		foreach (RandomSpawn randomSpawn2 in location.m_randomSpawns)
		{
			randomSpawn2.Randomize();
		}
		foreach (ZNetView znetView3 in location.m_netViews)
		{
			znetView3.gameObject.SetActive(false);
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(location.m_prefab, pos, rot);
		gameObject2.SetActive(true);
		SnapToGround.SnappAll();
		return gameObject2;
	}

	private void CreateLocationProxy(ZoneSystem.ZoneLocation location, int seed, Vector3 pos, Quaternion rotation, ZoneSystem.SpawnMode mode, List<GameObject> spawnedGhostObjects)
	{
		if (mode == ZoneSystem.SpawnMode.Ghost)
		{
			ZNetView.StartGhostInit();
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_locationProxyPrefab, pos, rotation);
		LocationProxy component = gameObject.GetComponent<LocationProxy>();
		bool spawnNow = mode == ZoneSystem.SpawnMode.Full;
		component.SetLocation(location.m_prefab.name, seed, spawnNow, this.m_pgwVersion);
		if (mode == ZoneSystem.SpawnMode.Ghost)
		{
			spawnedGhostObjects.Add(gameObject);
			ZNetView.FinishGhostInit();
		}
	}

	private void RegisterLocation(ZoneSystem.ZoneLocation location, Vector3 pos, bool generated)
	{
		ZoneSystem.LocationInstance value = default(ZoneSystem.LocationInstance);
		value.m_location = location;
		value.m_position = pos;
		value.m_placed = generated;
		Vector2i zone = this.GetZone(pos);
		if (this.m_locationInstances.ContainsKey(zone))
		{
			ZLog.LogWarning("Location already exist in zone " + zone);
			return;
		}
		this.m_locationInstances.Add(zone, value);
	}

	private bool HaveLocationInRange(string prefabName, string group, Vector3 p, float radius)
	{
		foreach (ZoneSystem.LocationInstance locationInstance in this.m_locationInstances.Values)
		{
			if ((locationInstance.m_location.m_prefabName == prefabName || (group.Length > 0 && group == locationInstance.m_location.m_group)) && Vector3.Distance(locationInstance.m_position, p) < radius)
			{
				return true;
			}
		}
		return false;
	}

	public bool GetLocationIcon(string name, out Vector3 pos)
	{
		if (ZNet.instance.IsServer())
		{
			using (Dictionary<Vector2i, ZoneSystem.LocationInstance>.Enumerator enumerator = this.m_locationInstances.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<Vector2i, ZoneSystem.LocationInstance> keyValuePair = enumerator.Current;
					if ((keyValuePair.Value.m_location.m_iconAlways || (keyValuePair.Value.m_location.m_iconPlaced && keyValuePair.Value.m_placed)) && keyValuePair.Value.m_location.m_prefabName == name)
					{
						pos = keyValuePair.Value.m_position;
						return true;
					}
				}
				goto IL_F1;
			}
		}
		foreach (KeyValuePair<Vector3, string> keyValuePair2 in this.m_locationIcons)
		{
			if (keyValuePair2.Value == name)
			{
				pos = keyValuePair2.Key;
				return true;
			}
		}
		IL_F1:
		pos = Vector3.zero;
		return false;
	}

	public void GetLocationIcons(Dictionary<Vector3, string> icons)
	{
		if (ZNet.instance.IsServer())
		{
			using (Dictionary<Vector2i, ZoneSystem.LocationInstance>.ValueCollection.Enumerator enumerator = this.m_locationInstances.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ZoneSystem.LocationInstance locationInstance = enumerator.Current;
					if (locationInstance.m_location.m_iconAlways || (locationInstance.m_location.m_iconPlaced && locationInstance.m_placed))
					{
						icons[locationInstance.m_position] = locationInstance.m_location.m_prefabName;
					}
				}
				return;
			}
		}
		foreach (KeyValuePair<Vector3, string> keyValuePair in this.m_locationIcons)
		{
			icons.Add(keyValuePair.Key, keyValuePair.Value);
		}
	}

	private void GetTerrainDelta(Vector3 center, float radius, out float delta, out Vector3 slopeDirection)
	{
		int num = 10;
		float num2 = -999999f;
		float num3 = 999999f;
		Vector3 b = center;
		Vector3 a = center;
		for (int i = 0; i < num; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * radius;
			Vector3 vector2 = center + new Vector3(vector.x, 0f, vector.y);
			float groundHeight = this.GetGroundHeight(vector2);
			if (groundHeight < num3)
			{
				num3 = groundHeight;
				a = vector2;
			}
			if (groundHeight > num2)
			{
				num2 = groundHeight;
				b = vector2;
			}
		}
		delta = num2 - num3;
		slopeDirection = Vector3.Normalize(a - b);
	}

	public bool IsBlocked(Vector3 p)
	{
		p.y += 2000f;
		return Physics.Raycast(p, Vector3.down, 10000f, this.m_blockRayMask);
	}

	public float GetAverageGroundHeight(Vector3 p, float radius)
	{
		Vector3 origin = p;
		origin.y = 6000f;
		RaycastHit raycastHit;
		if (Physics.Raycast(origin, Vector3.down, out raycastHit, 10000f, this.m_terrainRayMask))
		{
			return raycastHit.point.y;
		}
		return p.y;
	}

	public float GetGroundHeight(Vector3 p)
	{
		Vector3 origin = p;
		origin.y = 6000f;
		RaycastHit raycastHit;
		if (Physics.Raycast(origin, Vector3.down, out raycastHit, 10000f, this.m_terrainRayMask))
		{
			return raycastHit.point.y;
		}
		return p.y;
	}

	public bool GetGroundHeight(Vector3 p, out float height)
	{
		p.y = 6000f;
		RaycastHit raycastHit;
		if (Physics.Raycast(p, Vector3.down, out raycastHit, 10000f, this.m_terrainRayMask))
		{
			height = raycastHit.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	public float GetSolidHeight(Vector3 p)
	{
		Vector3 origin = p;
		origin.y += 1000f;
		RaycastHit raycastHit;
		if (Physics.Raycast(origin, Vector3.down, out raycastHit, 2000f, this.m_solidRayMask))
		{
			return raycastHit.point.y;
		}
		return p.y;
	}

	public bool GetSolidHeight(Vector3 p, out float height)
	{
		p.y += 1000f;
		RaycastHit raycastHit;
		if (Physics.Raycast(p, Vector3.down, out raycastHit, 2000f, this.m_solidRayMask) && !raycastHit.collider.attachedRigidbody)
		{
			height = raycastHit.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	public bool GetSolidHeight(Vector3 p, float radius, out float height, Transform ignore)
	{
		height = p.y - 1000f;
		p.y += 1000f;
		int num;
		if (radius <= 0f)
		{
			num = Physics.RaycastNonAlloc(p, Vector3.down, this.rayHits, 2000f, this.m_solidRayMask);
		}
		else
		{
			num = Physics.SphereCastNonAlloc(p, radius, Vector3.down, this.rayHits, 2000f, this.m_solidRayMask);
		}
		bool result = false;
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = this.rayHits[i];
			Collider collider = raycastHit.collider;
			if (!(collider.attachedRigidbody != null) && (!(ignore != null) || !Utils.IsParent(collider.transform, ignore)))
			{
				if (raycastHit.point.y > height)
				{
					height = raycastHit.point.y;
				}
				result = true;
			}
		}
		return result;
	}

	public bool GetSolidHeight(Vector3 p, out float height, out Vector3 normal)
	{
		GameObject gameObject;
		return this.GetSolidHeight(p, out height, out normal, out gameObject);
	}

	public bool GetSolidHeight(Vector3 p, out float height, out Vector3 normal, out GameObject go)
	{
		p.y += 1000f;
		RaycastHit raycastHit;
		if (Physics.Raycast(p, Vector3.down, out raycastHit, 2000f, this.m_solidRayMask) && !raycastHit.collider.attachedRigidbody)
		{
			height = raycastHit.point.y;
			normal = raycastHit.normal;
			go = raycastHit.collider.gameObject;
			return true;
		}
		height = 0f;
		normal = Vector3.zero;
		go = null;
		return false;
	}

	public bool FindFloor(Vector3 p, out float height)
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out raycastHit, 1000f, this.m_solidRayMask))
		{
			height = raycastHit.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	public void GetGroundData(ref Vector3 p, out Vector3 normal, out Heightmap.Biome biome, out Heightmap.BiomeArea biomeArea, out Heightmap hmap)
	{
		biome = Heightmap.Biome.None;
		biomeArea = Heightmap.BiomeArea.Everything;
		hmap = null;
		RaycastHit raycastHit;
		if (Physics.Raycast(p + Vector3.up * 5000f, Vector3.down, out raycastHit, 10000f, this.m_terrainRayMask))
		{
			p.y = raycastHit.point.y;
			normal = raycastHit.normal;
			Heightmap component = raycastHit.collider.GetComponent<Heightmap>();
			if (component)
			{
				biome = component.GetBiome(raycastHit.point);
				biomeArea = component.GetBiomeArea();
				hmap = component;
			}
			return;
		}
		normal = Vector3.up;
	}

	private void UpdateTTL(float dt)
	{
		foreach (KeyValuePair<Vector2i, ZoneSystem.ZoneData> keyValuePair in this.m_zones)
		{
			keyValuePair.Value.m_ttl += dt;
		}
		foreach (KeyValuePair<Vector2i, ZoneSystem.ZoneData> keyValuePair2 in this.m_zones)
		{
			if (keyValuePair2.Value.m_ttl > this.m_zoneTTL && !ZNetScene.instance.HaveInstanceInSector(keyValuePair2.Key))
			{
				UnityEngine.Object.Destroy(keyValuePair2.Value.m_root);
				this.m_zones.Remove(keyValuePair2.Key);
				break;
			}
		}
	}

	public void GetVegetation(Heightmap.Biome biome, List<ZoneSystem.ZoneVegetation> vegetation)
	{
		foreach (ZoneSystem.ZoneVegetation zoneVegetation in this.m_vegetation)
		{
			if ((zoneVegetation.m_biome & biome) != Heightmap.Biome.None || zoneVegetation.m_biome == biome)
			{
				vegetation.Add(zoneVegetation);
			}
		}
	}

	public void GetLocations(Heightmap.Biome biome, List<ZoneSystem.ZoneLocation> locations, bool skipDisabled)
	{
		foreach (ZoneSystem.ZoneLocation zoneLocation in this.m_locations)
		{
			if (((zoneLocation.m_biome & biome) != Heightmap.Biome.None || zoneLocation.m_biome == biome) && (!skipDisabled || zoneLocation.m_enable))
			{
				locations.Add(zoneLocation);
			}
		}
	}

	public bool FindClosestLocation(string name, Vector3 point, out ZoneSystem.LocationInstance closest)
	{
		float num = 999999f;
		closest = default(ZoneSystem.LocationInstance);
		bool result = false;
		foreach (ZoneSystem.LocationInstance locationInstance in this.m_locationInstances.Values)
		{
			float num2 = Vector3.Distance(locationInstance.m_position, point);
			if (locationInstance.m_location.m_prefabName == name && num2 < num)
			{
				num = num2;
				closest = locationInstance;
				result = true;
			}
		}
		return result;
	}

	public Vector2i GetZone(Vector3 point)
	{
		int x = Mathf.FloorToInt((point.x + this.m_zoneSize / 2f) / this.m_zoneSize);
		int y = Mathf.FloorToInt((point.z + this.m_zoneSize / 2f) / this.m_zoneSize);
		return new Vector2i(x, y);
	}

	public Vector3 GetZonePos(Vector2i id)
	{
		return new Vector3((float)id.x * this.m_zoneSize, 0f, (float)id.y * this.m_zoneSize);
	}

	private void SetZoneGenerated(Vector2i zoneID)
	{
		this.m_generatedZones.Add(zoneID);
	}

	private bool IsZoneGenerated(Vector2i zoneID)
	{
		return this.m_generatedZones.Contains(zoneID);
	}

	public bool SkipSaving()
	{
		return this.m_error || this.m_didZoneTest;
	}

	public void ResetGlobalKeys()
	{
		this.m_globalKeys.Clear();
		this.SendGlobalKeys(ZRoutedRpc.Everybody);
	}

	public void SetGlobalKey(string name)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC("SetGlobalKey", new object[]
		{
			name
		});
	}

	public bool GetGlobalKey(string name)
	{
		return this.m_globalKeys.Contains(name);
	}

	private void RPC_SetGlobalKey(long sender, string name)
	{
		if (this.m_globalKeys.Contains(name))
		{
			return;
		}
		this.m_globalKeys.Add(name);
		this.SendGlobalKeys(ZRoutedRpc.Everybody);
	}

	public List<string> GetGlobalKeys()
	{
		return new List<string>(this.m_globalKeys);
	}

	public Dictionary<Vector2i, ZoneSystem.LocationInstance>.ValueCollection GetLocationList()
	{
		return this.m_locationInstances.Values;
	}

	private Dictionary<Vector3, string> tempIconList = new Dictionary<Vector3, string>();

	private RaycastHit[] rayHits = new RaycastHit[200];

	private static ZoneSystem m_instance;

	[HideInInspector]
	public List<Heightmap.Biome> m_biomeFolded = new List<Heightmap.Biome>();

	[HideInInspector]
	public List<Heightmap.Biome> m_vegetationFolded = new List<Heightmap.Biome>();

	[HideInInspector]
	public List<Heightmap.Biome> m_locationFolded = new List<Heightmap.Biome>();

	[NonSerialized]
	public bool m_drawLocations;

	[NonSerialized]
	public string m_drawLocationsFilter = "";

	[global::Tooltip("Zones to load around center sector")]
	public int m_activeArea = 1;

	public int m_activeDistantArea = 1;

	[global::Tooltip("Zone size, should match netscene sector size")]
	public float m_zoneSize = 64f;

	[global::Tooltip("Time before destroying inactive zone")]
	public float m_zoneTTL = 4f;

	[global::Tooltip("Time before spawning active zone")]
	public float m_zoneTTS = 4f;

	public GameObject m_zonePrefab;

	public GameObject m_zoneCtrlPrefab;

	public GameObject m_locationProxyPrefab;

	public float m_waterLevel = 30f;

	[Header("Versions")]
	public int m_pgwVersion = 53;

	public int m_locationVersion = 1;

	[Header("Generation data")]
	public List<ZoneSystem.ZoneVegetation> m_vegetation = new List<ZoneSystem.ZoneVegetation>();

	public List<ZoneSystem.ZoneLocation> m_locations = new List<ZoneSystem.ZoneLocation>();

	private Dictionary<int, ZoneSystem.ZoneLocation> m_locationsByHash = new Dictionary<int, ZoneSystem.ZoneLocation>();

	private bool m_error;

	private bool m_didZoneTest;

	private int m_terrainRayMask;

	private int m_blockRayMask;

	private int m_solidRayMask;

	private float m_updateTimer;

	private Dictionary<Vector2i, ZoneSystem.ZoneData> m_zones = new Dictionary<Vector2i, ZoneSystem.ZoneData>();

	private HashSet<Vector2i> m_generatedZones = new HashSet<Vector2i>();

	private bool m_locationsGenerated;

	private Dictionary<Vector2i, ZoneSystem.LocationInstance> m_locationInstances = new Dictionary<Vector2i, ZoneSystem.LocationInstance>();

	private Dictionary<Vector3, string> m_locationIcons = new Dictionary<Vector3, string>();

	private HashSet<string> m_globalKeys = new HashSet<string>();

	private HashSet<Vector2i> m_tempGeneratedZonesSaveClone;

	private HashSet<string> m_tempGlobalKeysSaveClone;

	private List<ZoneSystem.LocationInstance> m_tempLocationsSaveClone;

	private bool m_tempLocationsGeneratedSaveClone;

	private List<ZoneSystem.ClearArea> m_tempClearAreas = new List<ZoneSystem.ClearArea>();

	private List<GameObject> m_tempSpawnedObjects = new List<GameObject>();

	private class ZoneData
	{
		public GameObject m_root;

		public float m_ttl;
	}

	private class ClearArea
	{
		public ClearArea(Vector3 p, float r)
		{
			this.m_center = p;
			this.m_radius = r;
		}

		public Vector3 m_center;

		public float m_radius;
	}

	[Serializable]
	public class ZoneVegetation
	{
		public ZoneSystem.ZoneVegetation Clone()
		{
			return base.MemberwiseClone() as ZoneSystem.ZoneVegetation;
		}

		public string m_name = "veg";

		public GameObject m_prefab;

		public bool m_enable = true;

		public float m_min;

		public float m_max = 10f;

		public bool m_forcePlacement;

		public float m_scaleMin = 1f;

		public float m_scaleMax = 1f;

		public float m_randTilt;

		public float m_chanceToUseGroundTilt;

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		[BitMask(typeof(Heightmap.BiomeArea))]
		public Heightmap.BiomeArea m_biomeArea = Heightmap.BiomeArea.Everything;

		public bool m_blockCheck = true;

		public float m_minAltitude = -1000f;

		public float m_maxAltitude = 1000f;

		public float m_minOceanDepth;

		public float m_maxOceanDepth;

		public float m_minTilt;

		public float m_maxTilt = 90f;

		public float m_terrainDeltaRadius;

		public float m_maxTerrainDelta = 2f;

		public float m_minTerrainDelta;

		public bool m_snapToWater;

		public float m_groundOffset;

		public int m_groupSizeMin = 1;

		public int m_groupSizeMax = 1;

		public float m_groupRadius;

		[Header("Forest fractal 0-1 inside forest")]
		public bool m_inForest;

		public float m_forestTresholdMin;

		public float m_forestTresholdMax = 1f;

		[HideInInspector]
		public bool m_foldout;
	}

	[Serializable]
	public class ZoneLocation
	{
		public ZoneSystem.ZoneLocation Clone()
		{
			return base.MemberwiseClone() as ZoneSystem.ZoneLocation;
		}

		public bool m_enable = true;

		public string m_prefabName;

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		[BitMask(typeof(Heightmap.BiomeArea))]
		public Heightmap.BiomeArea m_biomeArea = Heightmap.BiomeArea.Everything;

		public int m_quantity;

		public float m_chanceToSpawn = 10f;

		public bool m_prioritized;

		public bool m_centerFirst;

		public bool m_unique;

		public string m_group = "";

		public float m_minDistanceFromSimilar;

		public bool m_iconAlways;

		public bool m_iconPlaced;

		public bool m_randomRotation = true;

		public bool m_slopeRotation;

		public bool m_snapToWater;

		public float m_maxTerrainDelta = 2f;

		public float m_minTerrainDelta;

		[Header("Forest fractal 0-1 inside forest")]
		public bool m_inForest;

		public float m_forestTresholdMin;

		public float m_forestTresholdMax = 1f;

		[Space(10f)]
		public float m_minDistance;

		public float m_maxDistance;

		public float m_minAltitude = -1000f;

		public float m_maxAltitude = 1000f;

		[NonSerialized]
		public GameObject m_prefab;

		[NonSerialized]
		public int m_hash;

		[NonSerialized]
		public Location m_location;

		[NonSerialized]
		public float m_interiorRadius = 10f;

		[NonSerialized]
		public float m_exteriorRadius = 10f;

		[NonSerialized]
		public List<ZNetView> m_netViews = new List<ZNetView>();

		[NonSerialized]
		public List<RandomSpawn> m_randomSpawns = new List<RandomSpawn>();

		[HideInInspector]
		public bool m_foldout;
	}

	public struct LocationInstance
	{
		public ZoneSystem.ZoneLocation m_location;

		public Vector3 m_position;

		public bool m_placed;
	}

	public enum SpawnMode
	{
		Full,
		Client,
		Ghost
	}
}
