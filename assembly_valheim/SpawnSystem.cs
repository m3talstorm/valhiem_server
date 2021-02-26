using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSystem : MonoBehaviour
{
	private void Awake()
	{
		SpawnSystem.m_instances.Add(this);
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_heightmap = Heightmap.FindHeightmap(base.transform.position);
		base.InvokeRepeating("UpdateSpawning", 4f, 4f);
	}

	private void OnDestroy()
	{
		SpawnSystem.m_instances.Remove(this);
	}

	private void UpdateSpawning()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (Player.m_localPlayer == null)
		{
			return;
		}
		this.m_nearPlayers.Clear();
		this.GetPlayersInZone(this.m_nearPlayers);
		if (this.m_nearPlayers.Count == 0)
		{
			return;
		}
		DateTime time = ZNet.instance.GetTime();
		this.UpdateSpawnList(this.m_spawners, time, false);
		List<SpawnSystem.SpawnData> currentSpawners = RandEventSystem.instance.GetCurrentSpawners();
		if (currentSpawners != null)
		{
			this.UpdateSpawnList(currentSpawners, time, true);
		}
	}

	private void UpdateSpawnList(List<SpawnSystem.SpawnData> spawners, DateTime currentTime, bool eventSpawners)
	{
		string str = eventSpawners ? "e_" : "b_";
		int num = 0;
		foreach (SpawnSystem.SpawnData spawnData in spawners)
		{
			num++;
			if (spawnData.m_enabled && this.m_heightmap.HaveBiome(spawnData.m_biome))
			{
				int stableHashCode = (str + spawnData.m_prefab.name + num.ToString()).GetStableHashCode();
				DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(stableHashCode, 0L));
				TimeSpan timeSpan = currentTime - d;
				int num2 = Mathf.Min(spawnData.m_maxSpawned, (int)(timeSpan.TotalSeconds / (double)spawnData.m_spawnInterval));
				if (num2 > 0)
				{
					this.m_nview.GetZDO().Set(stableHashCode, currentTime.Ticks);
				}
				for (int i = 0; i < num2; i++)
				{
					if (UnityEngine.Random.Range(0f, 100f) <= spawnData.m_spawnChance)
					{
						if ((!string.IsNullOrEmpty(spawnData.m_requiredGlobalKey) && !ZoneSystem.instance.GetGlobalKey(spawnData.m_requiredGlobalKey)) || (spawnData.m_requiredEnvironments.Count > 0 && !EnvMan.instance.IsEnvironment(spawnData.m_requiredEnvironments)) || (!spawnData.m_spawnAtDay && EnvMan.instance.IsDay()) || (!spawnData.m_spawnAtNight && EnvMan.instance.IsNight()) || SpawnSystem.GetNrOfInstances(spawnData.m_prefab, Vector3.zero, 0f, eventSpawners, false) >= spawnData.m_maxSpawned)
						{
							break;
						}
						Vector3 vector;
						Player player;
						if (this.FindBaseSpawnPoint(spawnData, this.m_nearPlayers, out vector, out player) && (spawnData.m_spawnDistance <= 0f || !SpawnSystem.HaveInstanceInRange(spawnData.m_prefab, vector, spawnData.m_spawnDistance)))
						{
							int num3 = UnityEngine.Random.Range(spawnData.m_groupSizeMin, spawnData.m_groupSizeMax + 1);
							float d2 = (num3 > 1) ? spawnData.m_groupRadius : 0f;
							int num4 = 0;
							for (int j = 0; j < num3 * 2; j++)
							{
								Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
								Vector3 a = vector + new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y) * d2;
								if (this.IsSpawnPointGood(spawnData, ref a))
								{
									this.Spawn(spawnData, a + Vector3.up * spawnData.m_groundOffset, eventSpawners);
									num4++;
									if (num4 >= num3)
									{
										break;
									}
								}
							}
							ZLog.Log(string.Concat(new object[]
							{
								"Spawned ",
								spawnData.m_prefab.name,
								" x ",
								num4
							}));
						}
					}
				}
			}
		}
	}

	private void Spawn(SpawnSystem.SpawnData critter, Vector3 spawnPoint, bool eventSpawner)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(critter.m_prefab, spawnPoint, Quaternion.identity);
		BaseAI component = gameObject.GetComponent<BaseAI>();
		if (component != null)
		{
			if (critter.m_huntPlayer)
			{
				component.SetHuntPlayer(true);
			}
			if (critter.m_maxLevel > 1 && (critter.m_levelUpMinCenterDistance <= 0f || spawnPoint.magnitude > critter.m_levelUpMinCenterDistance))
			{
				Character component2 = gameObject.GetComponent<Character>();
				if (component2)
				{
					int num = critter.m_minLevel;
					while (num < critter.m_maxLevel && UnityEngine.Random.Range(0f, 100f) <= this.m_levelupChance)
					{
						num++;
					}
					if (num > 1)
					{
						component2.SetLevel(num);
					}
				}
			}
			MonsterAI monsterAI = component as MonsterAI;
			if (monsterAI)
			{
				if (!critter.m_spawnAtDay)
				{
					monsterAI.SetDespawnInDay(true);
				}
				if (eventSpawner)
				{
					monsterAI.SetEventCreature(true);
				}
			}
		}
	}

	private bool IsSpawnPointGood(SpawnSystem.SpawnData spawn, ref Vector3 spawnPoint)
	{
		Vector3 vector;
		Heightmap.Biome biome;
		Heightmap.BiomeArea biomeArea;
		Heightmap heightmap;
		ZoneSystem.instance.GetGroundData(ref spawnPoint, out vector, out biome, out biomeArea, out heightmap);
		if ((spawn.m_biome & biome) == Heightmap.Biome.None)
		{
			return false;
		}
		if ((spawn.m_biomeArea & biomeArea) == (Heightmap.BiomeArea)0)
		{
			return false;
		}
		if (ZoneSystem.instance.IsBlocked(spawnPoint))
		{
			return false;
		}
		float num = spawnPoint.y - ZoneSystem.instance.m_waterLevel;
		if (num < spawn.m_minAltitude || num > spawn.m_maxAltitude)
		{
			return false;
		}
		float num2 = Mathf.Cos(0.017453292f * spawn.m_maxTilt);
		float num3 = Mathf.Cos(0.017453292f * spawn.m_minTilt);
		if (vector.y < num2 || vector.y > num3)
		{
			return false;
		}
		float range = (spawn.m_spawnRadiusMin > 0f) ? spawn.m_spawnRadiusMin : 40f;
		if (Player.IsPlayerInRange(spawnPoint, range))
		{
			return false;
		}
		if (EffectArea.IsPointInsideArea(spawnPoint, EffectArea.Type.PlayerBase, 0f))
		{
			return false;
		}
		if (!spawn.m_inForest || !spawn.m_outsideForest)
		{
			bool flag = WorldGenerator.InForest(spawnPoint);
			if (!spawn.m_inForest && flag)
			{
				return false;
			}
			if (!spawn.m_outsideForest && !flag)
			{
				return false;
			}
		}
		if (spawn.m_minOceanDepth != spawn.m_maxOceanDepth && heightmap != null)
		{
			float oceanDepth = heightmap.GetOceanDepth(spawnPoint);
			if (oceanDepth < spawn.m_minOceanDepth || oceanDepth > spawn.m_maxOceanDepth)
			{
				return false;
			}
		}
		return true;
	}

	private bool FindBaseSpawnPoint(SpawnSystem.SpawnData spawn, List<Player> allPlayers, out Vector3 spawnCenter, out Player targetPlayer)
	{
		float min = (spawn.m_spawnRadiusMin > 0f) ? spawn.m_spawnRadiusMin : 40f;
		float max = (spawn.m_spawnRadiusMax > 0f) ? spawn.m_spawnRadiusMax : 80f;
		for (int i = 0; i < 20; i++)
		{
			Player player = allPlayers[UnityEngine.Random.Range(0, allPlayers.Count)];
			Vector3 a = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward;
			Vector3 vector = player.transform.position + a * UnityEngine.Random.Range(min, max);
			if (this.IsSpawnPointGood(spawn, ref vector))
			{
				spawnCenter = vector;
				targetPlayer = player;
				return true;
			}
		}
		spawnCenter = Vector3.zero;
		targetPlayer = null;
		return false;
	}

	private int GetNrOfInstances(string prefabName)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		int num = 0;
		foreach (Character character in allCharacters)
		{
			if (character.gameObject.name.StartsWith(prefabName) && this.InsideZone(character.transform.position, 0f))
			{
				num++;
			}
		}
		return num;
	}

	private void GetPlayersInZone(List<Player> players)
	{
		foreach (Player player in Player.GetAllPlayers())
		{
			if (this.InsideZone(player.transform.position, 0f))
			{
				players.Add(player);
			}
		}
	}

	private void GetPlayersNearZone(List<Player> players, float marginDistance)
	{
		foreach (Player player in Player.GetAllPlayers())
		{
			if (this.InsideZone(player.transform.position, marginDistance))
			{
				players.Add(player);
			}
		}
	}

	private bool IsPlayerTooClose(List<Player> players, Vector3 point, float minDistance)
	{
		using (List<Player>.Enumerator enumerator = players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (Vector3.Distance(enumerator.Current.transform.position, point) < minDistance)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool InPlayerRange(List<Player> players, Vector3 point, float minDistance, float maxDistance)
	{
		bool result = false;
		foreach (Player player in players)
		{
			float num = Utils.DistanceXZ(player.transform.position, point);
			if (num < minDistance)
			{
				return false;
			}
			if (num < maxDistance)
			{
				result = true;
			}
		}
		return result;
	}

	private static bool HaveInstanceInRange(GameObject prefab, Vector3 centerPoint, float minDistance)
	{
		string name = prefab.name;
		if (prefab.GetComponent<BaseAI>() != null)
		{
			foreach (BaseAI baseAI in BaseAI.GetAllInstances())
			{
				if (baseAI.gameObject.name.StartsWith(name) && Utils.DistanceXZ(baseAI.transform.position, centerPoint) < minDistance)
				{
					return true;
				}
			}
			return false;
		}
		foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("spawned"))
		{
			if (gameObject.gameObject.name.StartsWith(name) && Utils.DistanceXZ(gameObject.transform.position, centerPoint) < minDistance)
			{
				return true;
			}
		}
		return false;
	}

	public static int GetNrOfInstances(GameObject prefab)
	{
		return SpawnSystem.GetNrOfInstances(prefab, Vector3.zero, 0f, false, false);
	}

	public static int GetNrOfInstances(GameObject prefab, Vector3 center, float maxRange, bool eventCreaturesOnly = false, bool procreationOnly = false)
	{
		string text = prefab.name + "(Clone)";
		if (prefab.GetComponent<BaseAI>() != null)
		{
			List<BaseAI> allInstances = BaseAI.GetAllInstances();
			int num = 0;
			foreach (BaseAI baseAI in allInstances)
			{
				if (!(baseAI.gameObject.name != text) && (maxRange <= 0f || Vector3.Distance(center, baseAI.transform.position) <= maxRange))
				{
					if (eventCreaturesOnly)
					{
						MonsterAI monsterAI = baseAI as MonsterAI;
						if (monsterAI && !monsterAI.IsEventCreature())
						{
							continue;
						}
					}
					if (procreationOnly)
					{
						Procreation component = baseAI.GetComponent<Procreation>();
						if (component && !component.ReadyForProcreation())
						{
							continue;
						}
					}
					num++;
				}
			}
			return num;
		}
		GameObject[] array = GameObject.FindGameObjectsWithTag("spawned");
		int num2 = 0;
		foreach (GameObject gameObject in array)
		{
			if (gameObject.name.StartsWith(text) && (maxRange <= 0f || Vector3.Distance(center, gameObject.transform.position) <= maxRange))
			{
				num2++;
			}
		}
		return num2;
	}

	public void GetSpawners(Heightmap.Biome biome, List<SpawnSystem.SpawnData> spawners)
	{
		foreach (SpawnSystem.SpawnData spawnData in this.m_spawners)
		{
			if ((spawnData.m_biome & biome) != Heightmap.Biome.None || spawnData.m_biome == biome)
			{
				spawners.Add(spawnData);
			}
		}
	}

	private bool InsideZone(Vector3 point, float extra = 0f)
	{
		float num = ZoneSystem.instance.m_zoneSize * 0.5f + extra;
		Vector3 position = base.transform.position;
		return point.x >= position.x - num && point.x <= position.x + num && point.z >= position.z - num && point.z <= position.z + num;
	}

	private bool HaveGlobalKeys(SpawnSystem.SpawnData ev)
	{
		return string.IsNullOrEmpty(ev.m_requiredGlobalKey) || ZoneSystem.instance.GetGlobalKey(ev.m_requiredGlobalKey);
	}

	private static List<SpawnSystem> m_instances = new List<SpawnSystem>();

	private const float m_spawnDistanceMin = 40f;

	private const float m_spawnDistanceMax = 80f;

	public List<SpawnSystem.SpawnData> m_spawners = new List<SpawnSystem.SpawnData>();

	public float m_levelupChance = 10f;

	[HideInInspector]
	public List<Heightmap.Biome> m_biomeFolded = new List<Heightmap.Biome>();

	private List<Player> m_nearPlayers = new List<Player>();

	private ZNetView m_nview;

	private Heightmap m_heightmap;

	[Serializable]
	public class SpawnData
	{
		public SpawnSystem.SpawnData Clone()
		{
			SpawnSystem.SpawnData spawnData = base.MemberwiseClone() as SpawnSystem.SpawnData;
			spawnData.m_requiredEnvironments = new List<string>(this.m_requiredEnvironments);
			return spawnData;
		}

		public string m_name = "";

		public bool m_enabled = true;

		public GameObject m_prefab;

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		[BitMask(typeof(Heightmap.BiomeArea))]
		public Heightmap.BiomeArea m_biomeArea = Heightmap.BiomeArea.Everything;

		[Header("Total nr of instances (if near player is set, only instances within the max spawn radius is counted)")]
		public int m_maxSpawned = 1;

		[Header("How often do we spawn")]
		public float m_spawnInterval = 4f;

		[Header("Chanse to spawn each spawn interval")]
		[Range(0f, 100f)]
		public float m_spawnChance = 100f;

		[Header("Minimum distance to another instance")]
		public float m_spawnDistance = 10f;

		[Header("Spawn range ( 0 = use global setting )")]
		public float m_spawnRadiusMin;

		public float m_spawnRadiusMax;

		[Header("Only spawn if this key is set")]
		public string m_requiredGlobalKey = "";

		[Header("Only spawn if this environment is active")]
		public List<string> m_requiredEnvironments = new List<string>();

		[Header("Group spawning")]
		public int m_groupSizeMin = 1;

		public int m_groupSizeMax = 1;

		public float m_groupRadius = 3f;

		[Header("Time of day")]
		public bool m_spawnAtNight = true;

		public bool m_spawnAtDay = true;

		[Header("Altitude")]
		public float m_minAltitude = -1000f;

		public float m_maxAltitude = 1000f;

		[Header("Terrain tilt")]
		public float m_minTilt;

		public float m_maxTilt = 35f;

		[Header("Forest")]
		public bool m_inForest = true;

		public bool m_outsideForest = true;

		[Header("Ocean depth ")]
		public float m_minOceanDepth;

		public float m_maxOceanDepth;

		[Header("States")]
		public bool m_huntPlayer;

		public float m_groundOffset = 0.5f;

		[Header("Level")]
		public int m_maxLevel = 1;

		public int m_minLevel = 1;

		public float m_levelUpMinCenterDistance;

		[HideInInspector]
		public bool m_foldout;
	}
}
