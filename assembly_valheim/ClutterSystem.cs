using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ClutterSystem : MonoBehaviour
{
	public static ClutterSystem instance
	{
		get
		{
			return ClutterSystem.m_instance;
		}
	}

	private void Awake()
	{
		ClutterSystem.m_instance = this;
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			return;
		}
		this.ApplySettings();
		this.m_placeRayMask = LayerMask.GetMask(new string[]
		{
			"terrain"
		});
		this.m_grassRoot = new GameObject("grassroot");
		this.m_grassRoot.transform.SetParent(base.transform);
	}

	public void ApplySettings()
	{
		ClutterSystem.Quality @int = (ClutterSystem.Quality)PlayerPrefs.GetInt("ClutterQuality", 2);
		if (this.m_quality == @int)
		{
			return;
		}
		this.m_quality = @int;
		this.ClearAll();
	}

	private void LateUpdate()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 center = (!GameCamera.InFreeFly() && Player.m_localPlayer) ? Player.m_localPlayer.transform.position : mainCamera.transform.position;
		if (this.m_forceRebuild)
		{
			if (this.IsHeightmapReady())
			{
				this.m_forceRebuild = false;
				this.UpdateGrass(Time.deltaTime, true, center);
			}
		}
		else if (this.IsHeightmapReady())
		{
			this.UpdateGrass(Time.deltaTime, false, center);
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer != null)
		{
			this.m_oldPlayerPos = Vector3.Lerp(this.m_oldPlayerPos, localPlayer.transform.position, this.m_playerPushFade);
			Shader.SetGlobalVector("_PlayerPosition", localPlayer.transform.position);
			Shader.SetGlobalVector("_PlayerOldPosition", this.m_oldPlayerPos);
			return;
		}
		Shader.SetGlobalVector("_PlayerPosition", new Vector3(999999f, 999999f, 999999f));
		Shader.SetGlobalVector("_PlayerOldPosition", new Vector3(999999f, 999999f, 999999f));
	}

	public Vector2Int GetVegPatch(Vector3 point)
	{
		int x = Mathf.FloorToInt((point.x + this.m_grassPatchSize / 2f) / this.m_grassPatchSize);
		int y = Mathf.FloorToInt((point.z + this.m_grassPatchSize / 2f) / this.m_grassPatchSize);
		return new Vector2Int(x, y);
	}

	public Vector3 GetVegPatchCenter(Vector2Int p)
	{
		return new Vector3((float)p.x * this.m_grassPatchSize, 0f, (float)p.y * this.m_grassPatchSize);
	}

	private bool IsHeightmapReady()
	{
		Camera mainCamera = Utils.GetMainCamera();
		return mainCamera && !Heightmap.HaveQueuedRebuild(mainCamera.transform.position, this.m_distance);
	}

	private void UpdateGrass(float dt, bool rebuildAll, Vector3 center)
	{
		if (this.m_quality == ClutterSystem.Quality.Off)
		{
			return;
		}
		this.GeneratePatches(rebuildAll, center);
		this.TimeoutPatches(dt);
	}

	private void GeneratePatches(bool rebuildAll, Vector3 center)
	{
		bool flag = false;
		Vector2Int vegPatch = this.GetVegPatch(center);
		this.GeneratePatch(center, vegPatch, ref flag, rebuildAll);
		int num = Mathf.CeilToInt((this.m_distance - this.m_grassPatchSize / 2f) / this.m_grassPatchSize);
		for (int i = 1; i <= num; i++)
		{
			for (int j = vegPatch.x - i; j <= vegPatch.x + i; j++)
			{
				this.GeneratePatch(center, new Vector2Int(j, vegPatch.y - i), ref flag, rebuildAll);
				this.GeneratePatch(center, new Vector2Int(j, vegPatch.y + i), ref flag, rebuildAll);
			}
			for (int k = vegPatch.y - i + 1; k <= vegPatch.y + i - 1; k++)
			{
				this.GeneratePatch(center, new Vector2Int(vegPatch.x - i, k), ref flag, rebuildAll);
				this.GeneratePatch(center, new Vector2Int(vegPatch.x + i, k), ref flag, rebuildAll);
			}
		}
	}

	private void GeneratePatch(Vector3 camPos, Vector2Int p, ref bool generated, bool rebuildAll)
	{
		if (Utils.DistanceXZ(this.GetVegPatchCenter(p), camPos) > this.m_distance)
		{
			return;
		}
		ClutterSystem.PatchData patchData;
		if (this.m_patches.TryGetValue(p, out patchData) && !patchData.m_reset)
		{
			patchData.m_timer = 0f;
			return;
		}
		if (rebuildAll || !generated || this.m_menuHack)
		{
			ClutterSystem.PatchData patchData2 = this.GenerateVegPatch(p, this.m_grassPatchSize);
			if (patchData2 != null)
			{
				ClutterSystem.PatchData patchData3;
				if (this.m_patches.TryGetValue(p, out patchData3))
				{
					foreach (GameObject obj in patchData3.m_objects)
					{
						UnityEngine.Object.Destroy(obj);
					}
					this.FreePatch(patchData3);
					this.m_patches.Remove(p);
				}
				this.m_patches.Add(p, patchData2);
				generated = true;
			}
		}
	}

	private void TimeoutPatches(float dt)
	{
		this.m_tempToRemovePair.Clear();
		foreach (KeyValuePair<Vector2Int, ClutterSystem.PatchData> item in this.m_patches)
		{
			item.Value.m_timer += dt;
			if (item.Value.m_timer >= 2f)
			{
				this.m_tempToRemovePair.Add(item);
			}
		}
		foreach (KeyValuePair<Vector2Int, ClutterSystem.PatchData> keyValuePair in this.m_tempToRemovePair)
		{
			foreach (GameObject obj in keyValuePair.Value.m_objects)
			{
				UnityEngine.Object.Destroy(obj);
			}
			this.m_patches.Remove(keyValuePair.Key);
			this.FreePatch(keyValuePair.Value);
		}
	}

	private void ClearAll()
	{
		foreach (KeyValuePair<Vector2Int, ClutterSystem.PatchData> keyValuePair in this.m_patches)
		{
			foreach (GameObject obj in keyValuePair.Value.m_objects)
			{
				UnityEngine.Object.Destroy(obj);
			}
			this.FreePatch(keyValuePair.Value);
		}
		this.m_patches.Clear();
		this.m_forceRebuild = true;
	}

	public void ResetGrass(Vector3 center, float radius)
	{
		float num = this.m_grassPatchSize / 2f;
		foreach (KeyValuePair<Vector2Int, ClutterSystem.PatchData> keyValuePair in this.m_patches)
		{
			Vector3 center2 = keyValuePair.Value.center;
			if (center2.x + num >= center.x - radius && center2.x - num <= center.x + radius && center2.z + num >= center.z - radius && center2.z - num <= center.z + radius)
			{
				keyValuePair.Value.m_reset = true;
				this.m_forceRebuild = true;
			}
		}
	}

	public bool GetGroundInfo(Vector3 p, out Vector3 point, out Vector3 normal, out Heightmap hmap, out Heightmap.Biome biome)
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(p + Vector3.up * 500f, Vector3.down, out raycastHit, 1000f, this.m_placeRayMask))
		{
			point = raycastHit.point;
			normal = raycastHit.normal;
			hmap = raycastHit.collider.GetComponent<Heightmap>();
			biome = hmap.GetBiome(point);
			return true;
		}
		point = p;
		normal = Vector3.up;
		hmap = null;
		biome = Heightmap.Biome.Meadows;
		return false;
	}

	private Heightmap.Biome GetPatchBiomes(Vector3 center, float halfSize)
	{
		Heightmap.Biome biome = Heightmap.FindBiomeClutter(new Vector3(center.x - halfSize, 0f, center.z - halfSize));
		Heightmap.Biome biome2 = Heightmap.FindBiomeClutter(new Vector3(center.x + halfSize, 0f, center.z - halfSize));
		Heightmap.Biome biome3 = Heightmap.FindBiomeClutter(new Vector3(center.x - halfSize, 0f, center.z + halfSize));
		Heightmap.Biome biome4 = Heightmap.FindBiomeClutter(new Vector3(center.x + halfSize, 0f, center.z + halfSize));
		if (biome == Heightmap.Biome.None || biome2 == Heightmap.Biome.None || biome3 == Heightmap.Biome.None || biome4 == Heightmap.Biome.None)
		{
			return Heightmap.Biome.None;
		}
		return biome | biome2 | biome3 | biome4;
	}

	private ClutterSystem.PatchData GenerateVegPatch(Vector2Int patchID, float size)
	{
		Vector3 vegPatchCenter = this.GetVegPatchCenter(patchID);
		float num = size / 2f;
		Heightmap.Biome patchBiomes = this.GetPatchBiomes(vegPatchCenter, num);
		if (patchBiomes == Heightmap.Biome.None)
		{
			return null;
		}
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		UnityEngine.Random.State state = UnityEngine.Random.state;
		ClutterSystem.PatchData patchData = this.AllocatePatch();
		patchData.center = vegPatchCenter;
		for (int i = 0; i < this.m_clutter.Count; i++)
		{
			ClutterSystem.Clutter clutter = this.m_clutter[i];
			if (clutter.m_enabled && (patchBiomes & clutter.m_biome) != Heightmap.Biome.None)
			{
				InstanceRenderer instanceRenderer = null;
				UnityEngine.Random.InitState(patchID.x * (patchID.y * 1374) + i * 9321);
				Vector3 b = new Vector3(clutter.m_fractalOffset, 0f, 0f);
				float num2 = Mathf.Cos(0.017453292f * clutter.m_maxTilt);
				int num3 = (this.m_quality == ClutterSystem.Quality.High) ? clutter.m_amount : (clutter.m_amount / 2);
				num3 = (int)((float)num3 * this.m_amountScale);
				int j = 0;
				while (j < num3)
				{
					Vector3 vector = new Vector3(UnityEngine.Random.Range(vegPatchCenter.x - num, vegPatchCenter.x + num), 0f, UnityEngine.Random.Range(vegPatchCenter.z - num, vegPatchCenter.z + num));
					float num4 = (float)UnityEngine.Random.Range(0, 360);
					if (!clutter.m_inForest)
					{
						goto IL_161;
					}
					float forestFactor = WorldGenerator.GetForestFactor(vector);
					if (forestFactor >= clutter.m_forestTresholdMin && forestFactor <= clutter.m_forestTresholdMax)
					{
						goto IL_161;
					}
					IL_3D5:
					j++;
					continue;
					IL_161:
					if (clutter.m_fractalScale > 0f)
					{
						float num5 = Utils.Fbm(vector * 0.01f * clutter.m_fractalScale + b, 3, 1.6f, 0.7f);
						if (num5 < clutter.m_fractalTresholdMin || num5 > clutter.m_fractalTresholdMax)
						{
							goto IL_3D5;
						}
					}
					Vector3 vector2;
					Vector3 vector3;
					Heightmap heightmap;
					Heightmap.Biome biome;
					if (!this.GetGroundInfo(vector, out vector2, out vector3, out heightmap, out biome) || (clutter.m_biome & biome) == Heightmap.Biome.None)
					{
						goto IL_3D5;
					}
					float num6 = vector2.y - this.m_waterLevel;
					if (num6 < clutter.m_minAlt || num6 > clutter.m_maxAlt || vector3.y < num2)
					{
						goto IL_3D5;
					}
					if (clutter.m_minOceanDepth != clutter.m_maxOceanDepth)
					{
						float oceanDepth = heightmap.GetOceanDepth(vector);
						if (oceanDepth < clutter.m_minOceanDepth || oceanDepth > clutter.m_maxOceanDepth)
						{
							goto IL_3D5;
						}
					}
					if (!clutter.m_onCleared || !clutter.m_onUncleared)
					{
						bool flag = heightmap.IsCleared(vector2);
						if ((clutter.m_onCleared && !flag) || (clutter.m_onUncleared && flag))
						{
							goto IL_3D5;
						}
					}
					vector = vector2;
					if (clutter.m_snapToWater)
					{
						vector.y = this.m_waterLevel;
					}
					if (clutter.m_randomOffset != 0f)
					{
						vector.y += UnityEngine.Random.Range(-clutter.m_randomOffset, clutter.m_randomOffset);
					}
					Quaternion quaternion = Quaternion.identity;
					if (clutter.m_terrainTilt)
					{
						quaternion = Quaternion.AngleAxis(num4, vector3);
					}
					else
					{
						quaternion = Quaternion.Euler(0f, num4, 0f);
					}
					if (clutter.m_instanced)
					{
						if (instanceRenderer == null)
						{
							GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(clutter.m_prefab, vegPatchCenter, Quaternion.identity, this.m_grassRoot.transform);
							instanceRenderer = gameObject.GetComponent<InstanceRenderer>();
							if (instanceRenderer.m_lodMaxDistance > this.m_distance - this.m_grassPatchSize / 2f)
							{
								instanceRenderer.m_lodMaxDistance = this.m_distance - this.m_grassPatchSize / 2f;
							}
							patchData.m_objects.Add(gameObject);
						}
						float scale = UnityEngine.Random.Range(clutter.m_scaleMin, clutter.m_scaleMax);
						instanceRenderer.AddInstance(vector, quaternion, scale);
						goto IL_3D5;
					}
					GameObject item = UnityEngine.Object.Instantiate<GameObject>(clutter.m_prefab, vector, quaternion, this.m_grassRoot.transform);
					patchData.m_objects.Add(item);
					goto IL_3D5;
				}
			}
		}
		UnityEngine.Random.state = state;
		return patchData;
	}

	private ClutterSystem.PatchData AllocatePatch()
	{
		if (this.m_freePatches.Count > 0)
		{
			return this.m_freePatches.Pop();
		}
		return new ClutterSystem.PatchData();
	}

	private void FreePatch(ClutterSystem.PatchData patch)
	{
		patch.center = Vector3.zero;
		patch.m_objects.Clear();
		patch.m_timer = 0f;
		patch.m_reset = false;
		this.m_freePatches.Push(patch);
	}

	private static ClutterSystem m_instance;

	private int m_placeRayMask;

	public List<ClutterSystem.Clutter> m_clutter = new List<ClutterSystem.Clutter>();

	public float m_grassPatchSize = 8f;

	public float m_distance = 40f;

	public float m_waterLevel = 27f;

	public float m_playerPushFade = 0.05f;

	public float m_amountScale = 1f;

	public bool m_menuHack;

	private Dictionary<Vector2Int, ClutterSystem.PatchData> m_patches = new Dictionary<Vector2Int, ClutterSystem.PatchData>();

	private Stack<ClutterSystem.PatchData> m_freePatches = new Stack<ClutterSystem.PatchData>();

	private GameObject m_grassRoot;

	private Vector3 m_oldPlayerPos = Vector3.zero;

	private List<Vector2Int> m_tempToRemove = new List<Vector2Int>();

	private List<KeyValuePair<Vector2Int, ClutterSystem.PatchData>> m_tempToRemovePair = new List<KeyValuePair<Vector2Int, ClutterSystem.PatchData>>();

	private ClutterSystem.Quality m_quality = ClutterSystem.Quality.High;

	private bool m_forceRebuild;

	[Serializable]
	public class Clutter
	{
		public string m_name = "";

		public bool m_enabled = true;

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		public bool m_instanced;

		public GameObject m_prefab;

		public int m_amount = 80;

		public bool m_onUncleared = true;

		public bool m_onCleared;

		public float m_scaleMin = 1f;

		public float m_scaleMax = 1f;

		public float m_maxTilt = 18f;

		public float m_maxAlt = 1000f;

		public float m_minAlt = 27f;

		public bool m_snapToWater;

		public bool m_terrainTilt;

		public float m_randomOffset;

		[Header("Ocean depth ")]
		public float m_minOceanDepth;

		public float m_maxOceanDepth;

		[Header("Forest fractal 0-1 inside forest")]
		public bool m_inForest;

		public float m_forestTresholdMin;

		public float m_forestTresholdMax = 1f;

		[Header("Fractal placement (m_fractalScale > 0 == enabled) ")]
		public float m_fractalScale;

		public float m_fractalOffset;

		public float m_fractalTresholdMin = 0.5f;

		public float m_fractalTresholdMax = 1f;
	}

	private class PatchData
	{
		public Vector3 center;

		public List<GameObject> m_objects = new List<GameObject>();

		public float m_timer;

		public bool m_reset;
	}

	public enum Quality
	{
		Off,
		Med,
		High
	}
}
