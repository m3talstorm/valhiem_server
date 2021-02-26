using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Heightmap : MonoBehaviour
{
	private void Awake()
	{
		if (!this.m_isDistantLod)
		{
			Heightmap.m_heightmaps.Add(this);
		}
		this.m_collider = base.GetComponent<MeshCollider>();
	}

	private void OnDestroy()
	{
		if (!this.m_isDistantLod)
		{
			Heightmap.m_heightmaps.Remove(this);
		}
		if (this.m_materialInstance)
		{
			UnityEngine.Object.DestroyImmediate(this.m_materialInstance);
		}
	}

	private void OnEnable()
	{
		if (this.m_isDistantLod && Application.isPlaying && !this.m_distantLodEditorHax)
		{
			return;
		}
		this.Regenerate();
	}

	private void Update()
	{
		this.Render();
	}

	private void Render()
	{
		if (!this.IsVisible())
		{
			return;
		}
		if (this.m_dirty)
		{
			this.m_dirty = false;
			this.m_materialInstance.SetTexture("_ClearedMaskTex", this.m_clearedMask);
			this.RebuildRenderMesh();
		}
		if (this.m_renderMesh)
		{
			Matrix4x4 matrix = Matrix4x4.TRS(base.transform.position, Quaternion.identity, Vector3.one);
			Graphics.DrawMesh(this.m_renderMesh, matrix, this.m_materialInstance, base.gameObject.layer);
		}
	}

	private bool IsVisible()
	{
		return Utils.InsideMainCamera(this.m_boundingSphere) && Utils.InsideMainCamera(this.m_bounds);
	}

	public static void ForceGenerateAll()
	{
		foreach (Heightmap heightmap in Heightmap.m_heightmaps)
		{
			if (heightmap.HaveQueuedRebuild())
			{
				ZLog.Log("Force generaeting hmap " + heightmap.transform.position);
				heightmap.Regenerate();
			}
		}
	}

	public void Poke(bool delayed)
	{
		if (delayed)
		{
			if (this.HaveQueuedRebuild())
			{
				base.CancelInvoke("Regenerate");
			}
			base.InvokeRepeating("Regenerate", 0.1f, 0f);
			return;
		}
		this.Regenerate();
	}

	public bool HaveQueuedRebuild()
	{
		return base.IsInvoking("Regenerate");
	}

	public void Regenerate()
	{
		if (this.HaveQueuedRebuild())
		{
			base.CancelInvoke("Regenerate");
		}
		this.Generate();
		this.RebuildCollisionMesh();
		this.UpdateCornerDepths();
		this.m_dirty = true;
	}

	private void UpdateCornerDepths()
	{
		float num = ZoneSystem.instance ? ZoneSystem.instance.m_waterLevel : 30f;
		this.m_oceanDepth[0] = this.GetHeight(0, this.m_width);
		this.m_oceanDepth[1] = this.GetHeight(this.m_width, this.m_width);
		this.m_oceanDepth[2] = this.GetHeight(this.m_width, 0);
		this.m_oceanDepth[3] = this.GetHeight(0, 0);
		this.m_oceanDepth[0] = Mathf.Max(0f, num - this.m_oceanDepth[0]);
		this.m_oceanDepth[1] = Mathf.Max(0f, num - this.m_oceanDepth[1]);
		this.m_oceanDepth[2] = Mathf.Max(0f, num - this.m_oceanDepth[2]);
		this.m_oceanDepth[3] = Mathf.Max(0f, num - this.m_oceanDepth[3]);
		this.m_materialInstance.SetFloatArray("_depth", this.m_oceanDepth);
	}

	public float[] GetOceanDepth()
	{
		return this.m_oceanDepth;
	}

	public static float GetOceanDepthAll(Vector3 worldPos)
	{
		Heightmap heightmap = Heightmap.FindHeightmap(worldPos);
		if (heightmap)
		{
			return heightmap.GetOceanDepth(worldPos);
		}
		return 0f;
	}

	public float GetOceanDepth(Vector3 worldPos)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		float t = (float)num / (float)this.m_width;
		float t2 = (float)num2 / (float)this.m_width;
		float a = Mathf.Lerp(this.m_oceanDepth[3], this.m_oceanDepth[2], t);
		float b = Mathf.Lerp(this.m_oceanDepth[0], this.m_oceanDepth[1], t);
		return Mathf.Lerp(a, b, t2);
	}

	private void Initialize()
	{
		int num = this.m_width + 1;
		int num2 = num * num;
		if (this.m_heights.Count != num2)
		{
			this.m_heights.Clear();
			for (int i = 0; i < num2; i++)
			{
				this.m_heights.Add(0f);
			}
			this.m_clearedMask = new Texture2D(this.m_width, this.m_width);
			this.m_clearedMask.wrapMode = TextureWrapMode.Clamp;
			this.m_materialInstance = new Material(this.m_material);
			this.m_materialInstance.SetTexture("_ClearedMaskTex", this.m_clearedMask);
		}
	}

	private void Generate()
	{
		this.Initialize();
		int num = this.m_width + 1;
		int num2 = num * num;
		Vector3 position = base.transform.position;
		if (this.m_buildData == null || this.m_buildData.m_baseHeights.Count != num2 || this.m_buildData.m_center != position || this.m_buildData.m_scale != this.m_scale || this.m_buildData.m_worldGen != WorldGenerator.instance)
		{
			this.m_buildData = HeightmapBuilder.instance.RequestTerrainSync(position, this.m_width, this.m_scale, this.m_isDistantLod, WorldGenerator.instance);
			this.m_cornerBiomes = this.m_buildData.m_cornerBiomes;
		}
		for (int i = 0; i < num2; i++)
		{
			this.m_heights[i] = this.m_buildData.m_baseHeights[i];
		}
		Color[] pixels = new Color[this.m_clearedMask.width * this.m_clearedMask.height];
		this.m_clearedMask.SetPixels(pixels);
		this.ApplyModifiers();
	}

	private float Distance(float x, float y, float rx, float ry)
	{
		float num = x - rx;
		float num2 = y - ry;
		float num3 = Mathf.Sqrt(num * num + num2 * num2);
		float num4 = 1.414f - num3;
		return num4 * num4 * num4;
	}

	public List<Heightmap.Biome> GetBiomes()
	{
		List<Heightmap.Biome> list = new List<Heightmap.Biome>();
		foreach (Heightmap.Biome item in this.m_cornerBiomes)
		{
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public bool HaveBiome(Heightmap.Biome biome)
	{
		return (this.m_cornerBiomes[0] & biome) != Heightmap.Biome.None || (this.m_cornerBiomes[1] & biome) != Heightmap.Biome.None || (this.m_cornerBiomes[2] & biome) != Heightmap.Biome.None || (this.m_cornerBiomes[3] & biome) > Heightmap.Biome.None;
	}

	public Heightmap.Biome GetBiome(Vector3 point)
	{
		if (this.m_isDistantLod)
		{
			return WorldGenerator.instance.GetBiome(point.x, point.z);
		}
		if (this.m_cornerBiomes[0] == this.m_cornerBiomes[1] && this.m_cornerBiomes[0] == this.m_cornerBiomes[2] && this.m_cornerBiomes[0] == this.m_cornerBiomes[3])
		{
			return this.m_cornerBiomes[0];
		}
		float x = point.x;
		float z = point.z;
		this.WorldToNormalizedHM(point, out x, out z);
		for (int i = 1; i < Heightmap.tempBiomeWeights.Length; i++)
		{
			Heightmap.tempBiomeWeights[i] = 0f;
		}
		Heightmap.tempBiomeWeights[(int)this.m_cornerBiomes[0]] += this.Distance(x, z, 0f, 0f);
		Heightmap.tempBiomeWeights[(int)this.m_cornerBiomes[1]] += this.Distance(x, z, 1f, 0f);
		Heightmap.tempBiomeWeights[(int)this.m_cornerBiomes[2]] += this.Distance(x, z, 0f, 1f);
		Heightmap.tempBiomeWeights[(int)this.m_cornerBiomes[3]] += this.Distance(x, z, 1f, 1f);
		int result = 0;
		float num = -99999f;
		for (int j = 1; j < Heightmap.tempBiomeWeights.Length; j++)
		{
			if (Heightmap.tempBiomeWeights[j] > num)
			{
				result = j;
				num = Heightmap.tempBiomeWeights[j];
			}
		}
		return (Heightmap.Biome)result;
	}

	public Heightmap.BiomeArea GetBiomeArea()
	{
		if (this.IsBiomeEdge())
		{
			return Heightmap.BiomeArea.Edge;
		}
		return Heightmap.BiomeArea.Median;
	}

	public bool IsBiomeEdge()
	{
		return this.m_cornerBiomes[0] != this.m_cornerBiomes[1] || this.m_cornerBiomes[0] != this.m_cornerBiomes[2] || this.m_cornerBiomes[0] != this.m_cornerBiomes[3];
	}

	private void ApplyModifiers()
	{
		List<TerrainModifier> allInstances = TerrainModifier.GetAllInstances();
		float[] array = null;
		float[] levelOnly = null;
		foreach (TerrainModifier terrainModifier in allInstances)
		{
			if (terrainModifier.enabled && this.TerrainVSModifier(terrainModifier))
			{
				if (terrainModifier.m_playerModifiction && array == null)
				{
					array = this.m_heights.ToArray();
					levelOnly = this.m_heights.ToArray();
				}
				this.ApplyModifier(terrainModifier, array, levelOnly);
			}
		}
		this.m_clearedMask.Apply();
	}

	private void ApplyModifier(TerrainModifier modifier, float[] baseHeights, float[] levelOnly)
	{
		if (modifier.m_level)
		{
			this.LevelTerrain(modifier.transform.position + Vector3.up * modifier.m_levelOffset, modifier.m_levelRadius, modifier.m_square, baseHeights, levelOnly, modifier.m_playerModifiction);
		}
		if (modifier.m_smooth)
		{
			this.SmoothTerrain2(modifier.transform.position + Vector3.up * modifier.m_levelOffset, modifier.m_smoothRadius, modifier.m_square, levelOnly, modifier.m_smoothPower, modifier.m_playerModifiction);
		}
		if (modifier.m_paintCleared)
		{
			this.PaintCleared(modifier.transform.position, modifier.m_paintRadius, modifier.m_paintType, modifier.m_paintHeightCheck, false);
		}
	}

	public bool TerrainVSModifier(TerrainModifier modifier)
	{
		Vector3 position = modifier.transform.position;
		float num = modifier.GetRadius() + 4f;
		Vector3 position2 = base.transform.position;
		float num2 = (float)this.m_width * this.m_scale * 0.5f;
		return position.x + num >= position2.x - num2 && position.x - num <= position2.x + num2 && position.z + num >= position2.z - num2 && position.z - num <= position2.z + num2;
	}

	private Vector3 CalcNormal2(List<Vector3> vertises, int x, int y)
	{
		int num = this.m_width + 1;
		Vector3 vector = vertises[y * num + x];
		Vector3 rhs;
		if (x == this.m_width)
		{
			Vector3 b = vertises[y * num + x - 1];
			rhs = vector - b;
		}
		else if (x == 0)
		{
			rhs = vertises[y * num + x + 1] - vector;
		}
		else
		{
			rhs = vertises[y * num + x + 1] - vertises[y * num + x - 1];
		}
		Vector3 lhs;
		if (y == this.m_width)
		{
			Vector3 b2 = this.CalcVertex(x, y - 1);
			lhs = vector - b2;
		}
		else if (y == 0)
		{
			lhs = this.CalcVertex(x, y + 1) - vector;
		}
		else
		{
			lhs = vertises[(y + 1) * num + x] - vertises[(y - 1) * num + x];
		}
		Vector3 result = Vector3.Cross(lhs, rhs);
		result.Normalize();
		return result;
	}

	private Vector3 CalcNormal(int x, int y)
	{
		Vector3 vector = this.CalcVertex(x, y);
		Vector3 rhs;
		if (x == this.m_width)
		{
			Vector3 b = this.CalcVertex(x - 1, y);
			rhs = vector - b;
		}
		else
		{
			rhs = this.CalcVertex(x + 1, y) - vector;
		}
		Vector3 lhs;
		if (y == this.m_width)
		{
			Vector3 b2 = this.CalcVertex(x, y - 1);
			lhs = vector - b2;
		}
		else
		{
			lhs = this.CalcVertex(x, y + 1) - vector;
		}
		return Vector3.Cross(lhs, rhs).normalized;
	}

	private Vector3 CalcVertex(int x, int y)
	{
		int num = this.m_width + 1;
		Vector3 a = new Vector3((float)this.m_width * this.m_scale * -0.5f, 0f, (float)this.m_width * this.m_scale * -0.5f);
		float y2 = this.m_heights[y * num + x];
		return a + new Vector3((float)x * this.m_scale, y2, (float)y * this.m_scale);
	}

	private Color GetBiomeColor(float ix, float iy)
	{
		if (this.m_cornerBiomes[0] == this.m_cornerBiomes[1] && this.m_cornerBiomes[0] == this.m_cornerBiomes[2] && this.m_cornerBiomes[0] == this.m_cornerBiomes[3])
		{
			return Heightmap.GetBiomeColor(this.m_cornerBiomes[0]);
		}
		Color32 biomeColor = Heightmap.GetBiomeColor(this.m_cornerBiomes[0]);
		Color32 biomeColor2 = Heightmap.GetBiomeColor(this.m_cornerBiomes[1]);
		Color32 biomeColor3 = Heightmap.GetBiomeColor(this.m_cornerBiomes[2]);
		Color32 biomeColor4 = Heightmap.GetBiomeColor(this.m_cornerBiomes[3]);
		Color32 a = Color32.Lerp(biomeColor, biomeColor2, ix);
		Color32 b = Color32.Lerp(biomeColor3, biomeColor4, ix);
		return Color32.Lerp(a, b, iy);
	}

	public static Color32 GetBiomeColor(Heightmap.Biome biome)
	{
		if (biome <= Heightmap.Biome.Plains)
		{
			switch (biome)
			{
			case Heightmap.Biome.Meadows:
			case (Heightmap.Biome)3:
				break;
			case Heightmap.Biome.Swamp:
				return new Color32(byte.MaxValue, 0, 0, 0);
			case Heightmap.Biome.Mountain:
				return new Color32(0, byte.MaxValue, 0, 0);
			default:
				if (biome == Heightmap.Biome.BlackForest)
				{
					return new Color32(0, 0, byte.MaxValue, 0);
				}
				if (biome == Heightmap.Biome.Plains)
				{
					return new Color32(0, 0, 0, byte.MaxValue);
				}
				break;
			}
		}
		else
		{
			if (biome == Heightmap.Biome.AshLands)
			{
				return new Color32(byte.MaxValue, 0, 0, byte.MaxValue);
			}
			if (biome == Heightmap.Biome.DeepNorth)
			{
				return new Color32(0, byte.MaxValue, 0, 0);
			}
			if (biome == Heightmap.Biome.Mistlands)
			{
				return new Color32(0, 0, byte.MaxValue, byte.MaxValue);
			}
		}
		return new Color32(0, 0, 0, 0);
	}

	private void RebuildCollisionMesh()
	{
		if (this.m_collisionMesh == null)
		{
			this.m_collisionMesh = new Mesh();
		}
		int num = this.m_width + 1;
		float num2 = -999999f;
		float num3 = 999999f;
		Heightmap.m_tempVertises.Clear();
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector3 vector = this.CalcVertex(j, i);
				Heightmap.m_tempVertises.Add(vector);
				if (vector.y > num2)
				{
					num2 = vector.y;
				}
				if (vector.y < num3)
				{
					num3 = vector.y;
				}
			}
		}
		this.m_collisionMesh.SetVertices(Heightmap.m_tempVertises);
		int num4 = (num - 1) * (num - 1) * 6;
		if ((ulong)this.m_collisionMesh.GetIndexCount(0) != (ulong)((long)num4))
		{
			Heightmap.m_tempIndices.Clear();
			for (int k = 0; k < num - 1; k++)
			{
				for (int l = 0; l < num - 1; l++)
				{
					int item = k * num + l;
					int item2 = k * num + l + 1;
					int item3 = (k + 1) * num + l + 1;
					int item4 = (k + 1) * num + l;
					Heightmap.m_tempIndices.Add(item);
					Heightmap.m_tempIndices.Add(item4);
					Heightmap.m_tempIndices.Add(item2);
					Heightmap.m_tempIndices.Add(item2);
					Heightmap.m_tempIndices.Add(item4);
					Heightmap.m_tempIndices.Add(item3);
				}
			}
			this.m_collisionMesh.SetIndices(Heightmap.m_tempIndices.ToArray(), MeshTopology.Triangles, 0);
		}
		if (this.m_collider)
		{
			this.m_collider.sharedMesh = this.m_collisionMesh;
		}
		float num5 = (float)this.m_width * this.m_scale * 0.5f;
		this.m_bounds.SetMinMax(base.transform.position + new Vector3(-num5, num3, -num5), base.transform.position + new Vector3(num5, num2, num5));
		this.m_boundingSphere.position = this.m_bounds.center;
		this.m_boundingSphere.radius = Vector3.Distance(this.m_boundingSphere.position, this.m_bounds.max);
	}

	private void RebuildRenderMesh()
	{
		if (this.m_renderMesh == null)
		{
			this.m_renderMesh = new Mesh();
		}
		WorldGenerator instance = WorldGenerator.instance;
		int num = this.m_width + 1;
		Vector3 vector = base.transform.position + new Vector3((float)this.m_width * this.m_scale * -0.5f, 0f, (float)this.m_width * this.m_scale * -0.5f);
		Heightmap.m_tempVertises.Clear();
		Heightmap.m_tempUVs.Clear();
		Heightmap.m_tempIndices.Clear();
		Heightmap.m_tempColors.Clear();
		for (int i = 0; i < num; i++)
		{
			float iy = Mathf.SmoothStep(0f, 1f, (float)i / (float)this.m_width);
			for (int j = 0; j < num; j++)
			{
				float ix = Mathf.SmoothStep(0f, 1f, (float)j / (float)this.m_width);
				Heightmap.m_tempUVs.Add(new Vector2((float)j / (float)this.m_width, (float)i / (float)this.m_width));
				if (this.m_isDistantLod)
				{
					float wx = vector.x + (float)j * this.m_scale;
					float wy = vector.z + (float)i * this.m_scale;
					Heightmap.Biome biome = instance.GetBiome(wx, wy);
					Heightmap.m_tempColors.Add(Heightmap.GetBiomeColor(biome));
				}
				else
				{
					Heightmap.m_tempColors.Add(this.GetBiomeColor(ix, iy));
				}
			}
		}
		this.m_collisionMesh.GetVertices(Heightmap.m_tempVertises);
		this.m_collisionMesh.GetIndices(Heightmap.m_tempIndices, 0);
		this.m_renderMesh.Clear();
		this.m_renderMesh.SetVertices(Heightmap.m_tempVertises);
		this.m_renderMesh.SetColors(Heightmap.m_tempColors);
		this.m_renderMesh.SetUVs(0, Heightmap.m_tempUVs);
		this.m_renderMesh.SetIndices(Heightmap.m_tempIndices.ToArray(), MeshTopology.Triangles, 0, true);
		this.m_renderMesh.RecalculateNormals();
		this.m_renderMesh.RecalculateTangents();
	}

	private void SmoothTerrain2(Vector3 worldPos, float radius, bool square, float[] levelOnlyHeights, float power, bool playerModifiction)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		float b = worldPos.y - base.transform.position.y;
		float num3 = radius / this.m_scale;
		int num4 = Mathf.CeilToInt(num3);
		Vector2 a = new Vector2((float)num, (float)num2);
		int num5 = this.m_width + 1;
		for (int i = num2 - num4; i <= num2 + num4; i++)
		{
			for (int j = num - num4; j <= num + num4; j++)
			{
				float num6 = Vector2.Distance(a, new Vector2((float)j, (float)i));
				if (num6 <= num3)
				{
					float num7 = num6 / num3;
					if (j >= 0 && i >= 0 && j < num5 && i < num5)
					{
						if (power == 3f)
						{
							num7 = num7 * num7 * num7;
						}
						else
						{
							num7 = Mathf.Pow(num7, power);
						}
						float height = this.GetHeight(j, i);
						float t = 1f - num7;
						float num8 = Mathf.Lerp(height, b, t);
						if (playerModifiction)
						{
							float num9 = levelOnlyHeights[i * num5 + j];
							num8 = Mathf.Clamp(num8, num9 - 1f, num9 + 1f);
						}
						this.SetHeight(j, i, num8);
					}
				}
			}
		}
	}

	private bool AtMaxWorldLevelDepth(Vector3 worldPos)
	{
		float num;
		this.GetWorldHeight(worldPos, out num);
		float num2;
		this.GetWorldBaseHeight(worldPos, out num2);
		return Mathf.Max(-(num - num2), 0f) >= 7.95f;
	}

	private bool GetWorldBaseHeight(Vector3 worldPos, out float height)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		int num3 = this.m_width + 1;
		if (num < 0 || num2 < 0 || num >= num3 || num2 >= num3)
		{
			height = 0f;
			return false;
		}
		height = this.m_buildData.m_baseHeights[num2 * num3 + num] + base.transform.position.y;
		return true;
	}

	private bool GetWorldHeight(Vector3 worldPos, out float height)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		int num3 = this.m_width + 1;
		if (num < 0 || num2 < 0 || num >= num3 || num2 >= num3)
		{
			height = 0f;
			return false;
		}
		height = this.m_heights[num2 * num3 + num] + base.transform.position.y;
		return true;
	}

	private bool GetAverageWorldHeight(Vector3 worldPos, float radius, out float height)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		float num3 = radius / this.m_scale;
		int num4 = Mathf.CeilToInt(num3);
		Vector2 a = new Vector2((float)num, (float)num2);
		int num5 = this.m_width + 1;
		float num6 = 0f;
		int num7 = 0;
		for (int i = num2 - num4; i <= num2 + num4; i++)
		{
			for (int j = num - num4; j <= num + num4; j++)
			{
				if (Vector2.Distance(a, new Vector2((float)j, (float)i)) <= num3 && j >= 0 && i >= 0 && j < num5 && i < num5)
				{
					num6 += this.GetHeight(j, i);
					num7++;
				}
			}
		}
		if (num7 == 0)
		{
			height = 0f;
			return false;
		}
		height = num6 / (float)num7 + base.transform.position.y;
		return true;
	}

	private bool GetMinWorldHeight(Vector3 worldPos, float radius, out float height)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		float num3 = radius / this.m_scale;
		int num4 = Mathf.CeilToInt(num3);
		Vector2 a = new Vector2((float)num, (float)num2);
		int num5 = this.m_width + 1;
		height = 99999f;
		for (int i = num2 - num4; i <= num2 + num4; i++)
		{
			for (int j = num - num4; j <= num + num4; j++)
			{
				if (Vector2.Distance(a, new Vector2((float)j, (float)i)) <= num3 && j >= 0 && i >= 0 && j < num5 && i < num5)
				{
					float height2 = this.GetHeight(j, i);
					if (height2 < height)
					{
						height = height2;
					}
				}
			}
		}
		return height != 99999f;
	}

	private bool GetMaxWorldHeight(Vector3 worldPos, float radius, out float height)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		float num3 = radius / this.m_scale;
		int num4 = Mathf.CeilToInt(num3);
		Vector2 a = new Vector2((float)num, (float)num2);
		int num5 = this.m_width + 1;
		height = -99999f;
		for (int i = num2 - num4; i <= num2 + num4; i++)
		{
			for (int j = num - num4; j <= num + num4; j++)
			{
				if (Vector2.Distance(a, new Vector2((float)j, (float)i)) <= num3 && j >= 0 && i >= 0 && j < num5 && i < num5)
				{
					float height2 = this.GetHeight(j, i);
					if (height2 > height)
					{
						height = height2;
					}
				}
			}
		}
		return height != -99999f;
	}

	public static bool AtMaxLevelDepth(Vector3 worldPos)
	{
		Heightmap heightmap = Heightmap.FindHeightmap(worldPos);
		return heightmap && heightmap.AtMaxWorldLevelDepth(worldPos);
	}

	public static bool GetHeight(Vector3 worldPos, out float height)
	{
		Heightmap heightmap = Heightmap.FindHeightmap(worldPos);
		if (heightmap && heightmap.GetWorldHeight(worldPos, out height))
		{
			return true;
		}
		height = 0f;
		return false;
	}

	public static bool GetAverageHeight(Vector3 worldPos, float radius, out float height)
	{
		List<Heightmap> list = new List<Heightmap>();
		Heightmap.FindHeightmap(worldPos, radius, list);
		float num = 0f;
		int num2 = 0;
		using (List<Heightmap>.Enumerator enumerator = list.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				float num3;
				if (enumerator.Current.GetAverageWorldHeight(worldPos, radius, out num3))
				{
					num += num3;
					num2++;
				}
			}
		}
		if (num2 > 0)
		{
			height = num / (float)num2;
			return true;
		}
		height = 0f;
		return false;
	}

	private void SmoothTerrain(Vector3 worldPos, float radius, bool square, float intensity)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		float num3 = radius / this.m_scale;
		int num4 = Mathf.CeilToInt(num3);
		Vector2 a = new Vector2((float)num, (float)num2);
		List<KeyValuePair<Vector2i, float>> list = new List<KeyValuePair<Vector2i, float>>();
		for (int i = num2 - num4; i <= num2 + num4; i++)
		{
			for (int j = num - num4; j <= num + num4; j++)
			{
				if ((square || Vector2.Distance(a, new Vector2((float)j, (float)i)) <= num3) && j != 0 && i != 0 && j != this.m_width && i != this.m_width)
				{
					list.Add(new KeyValuePair<Vector2i, float>(new Vector2i(j, i), this.GetAvgHeight(j, i, 1)));
				}
			}
		}
		foreach (KeyValuePair<Vector2i, float> keyValuePair in list)
		{
			float h = Mathf.Lerp(this.GetHeight(keyValuePair.Key.x, keyValuePair.Key.y), keyValuePair.Value, intensity);
			this.SetHeight(keyValuePair.Key.x, keyValuePair.Key.y, h);
		}
	}

	private float GetAvgHeight(int cx, int cy, int w)
	{
		int num = this.m_width + 1;
		float num2 = 0f;
		int num3 = 0;
		for (int i = cy - w; i <= cy + w; i++)
		{
			for (int j = cx - w; j <= cx + w; j++)
			{
				if (j >= 0 && i >= 0 && j < num && i < num)
				{
					num2 += this.GetHeight(j, i);
					num3++;
				}
			}
		}
		if (num3 == 0)
		{
			return 0f;
		}
		return num2 / (float)num3;
	}

	private float GroundHeight(Vector3 point)
	{
		Ray ray = new Ray(point + Vector3.up * 100f, Vector3.down);
		RaycastHit raycastHit;
		if (this.m_collider.Raycast(ray, out raycastHit, 300f))
		{
			return raycastHit.point.y;
		}
		return -10000f;
	}

	private void FindObjectsToMove(Vector3 worldPos, float area, List<Rigidbody> objects)
	{
		if (this.m_collider == null)
		{
			return;
		}
		foreach (Collider collider in Physics.OverlapBox(worldPos, new Vector3(area / 2f, 500f, area / 2f)))
		{
			if (!(collider == this.m_collider) && collider.attachedRigidbody)
			{
				Rigidbody attachedRigidbody = collider.attachedRigidbody;
				ZNetView component = attachedRigidbody.GetComponent<ZNetView>();
				if (!component || component.IsOwner())
				{
					objects.Add(attachedRigidbody);
				}
			}
		}
	}

	private void PaintCleared(Vector3 worldPos, float radius, TerrainModifier.PaintType paintType, bool heightCheck, bool apply)
	{
		worldPos.x -= 0.5f;
		worldPos.z -= 0.5f;
		float num = worldPos.y - base.transform.position.y;
		int num2;
		int num3;
		this.WorldToVertex(worldPos, out num2, out num3);
		float num4 = radius / this.m_scale;
		int num5 = Mathf.CeilToInt(num4);
		Vector2 a = new Vector2((float)num2, (float)num3);
		for (int i = num3 - num5; i <= num3 + num5; i++)
		{
			for (int j = num2 - num5; j <= num2 + num5; j++)
			{
				float num6 = Vector2.Distance(a, new Vector2((float)j, (float)i));
				if (j >= 0 && i >= 0 && j < this.m_clearedMask.width && i < this.m_clearedMask.height && (!heightCheck || this.GetHeight(j, i) <= num))
				{
					float num7 = 1f - Mathf.Clamp01(num6 / num4);
					num7 = Mathf.Pow(num7, 0.1f);
					Color color = this.m_clearedMask.GetPixel(j, i);
					switch (paintType)
					{
					case TerrainModifier.PaintType.Dirt:
						color = Color.Lerp(color, Color.red, num7);
						break;
					case TerrainModifier.PaintType.Cultivate:
						color = Color.Lerp(color, Color.green, num7);
						break;
					case TerrainModifier.PaintType.Paved:
						color = Color.Lerp(color, Color.blue, num7);
						break;
					case TerrainModifier.PaintType.Reset:
						color = Color.Lerp(color, Color.black, num7);
						break;
					}
					this.m_clearedMask.SetPixel(j, i, color);
				}
			}
		}
		if (apply)
		{
			this.m_clearedMask.Apply();
		}
	}

	public bool IsCleared(Vector3 worldPos)
	{
		worldPos.x -= 0.5f;
		worldPos.z -= 0.5f;
		int x;
		int y;
		this.WorldToVertex(worldPos, out x, out y);
		Color pixel = this.m_clearedMask.GetPixel(x, y);
		return pixel.r > 0.5f || pixel.g > 0.5f || pixel.b > 0.5f;
	}

	public bool IsCultivated(Vector3 worldPos)
	{
		int x;
		int y;
		this.WorldToVertex(worldPos, out x, out y);
		return this.m_clearedMask.GetPixel(x, y).g > 0.5f;
	}

	private void WorldToVertex(Vector3 worldPos, out int x, out int y)
	{
		Vector3 vector = worldPos - base.transform.position;
		x = Mathf.FloorToInt(vector.x / this.m_scale + 0.5f) + this.m_width / 2;
		y = Mathf.FloorToInt(vector.z / this.m_scale + 0.5f) + this.m_width / 2;
	}

	private void WorldToNormalizedHM(Vector3 worldPos, out float x, out float y)
	{
		float num = (float)this.m_width * this.m_scale;
		Vector3 vector = worldPos - base.transform.position;
		x = vector.x / num + 0.5f;
		y = vector.z / num + 0.5f;
	}

	private void LevelTerrain(Vector3 worldPos, float radius, bool square, float[] baseHeights, float[] levelOnly, bool playerModifiction)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		Vector3 vector = worldPos - base.transform.position;
		float num3 = radius / this.m_scale;
		int num4 = Mathf.CeilToInt(num3);
		int num5 = this.m_width + 1;
		Vector2 a = new Vector2((float)num, (float)num2);
		for (int i = num2 - num4; i <= num2 + num4; i++)
		{
			for (int j = num - num4; j <= num + num4; j++)
			{
				if ((square || Vector2.Distance(a, new Vector2((float)j, (float)i)) <= num3) && j >= 0 && i >= 0 && j < num5 && i < num5)
				{
					float num6 = vector.y;
					if (playerModifiction)
					{
						float num7 = baseHeights[i * num5 + j];
						num6 = Mathf.Clamp(num6, num7 - 8f, num7 + 8f);
						levelOnly[i * num5 + j] = num6;
					}
					this.SetHeight(j, i, num6);
				}
			}
		}
	}

	private float GetHeight(int x, int y)
	{
		int num = this.m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			return 0f;
		}
		return this.m_heights[y * num + x];
	}

	private float GetBaseHeight(int x, int y)
	{
		int num = this.m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			return 0f;
		}
		return this.m_buildData.m_baseHeights[y * num + x];
	}

	private void SetHeight(int x, int y, float h)
	{
		int num = this.m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			return;
		}
		this.m_heights[y * num + x] = h;
	}

	public bool IsPointInside(Vector3 point, float radius = 0f)
	{
		float num = (float)this.m_width * this.m_scale * 0.5f;
		Vector3 position = base.transform.position;
		return point.x + radius >= position.x - num && point.x - radius <= position.x + num && point.z + radius >= position.z - num && point.z - radius <= position.z + num;
	}

	public static List<Heightmap> GetAllHeightmaps()
	{
		return Heightmap.m_heightmaps;
	}

	public static Heightmap FindHeightmap(Vector3 point)
	{
		foreach (Heightmap heightmap in Heightmap.m_heightmaps)
		{
			if (heightmap.IsPointInside(point, 0f))
			{
				return heightmap;
			}
		}
		return null;
	}

	public static void FindHeightmap(Vector3 point, float radius, List<Heightmap> heightmaps)
	{
		foreach (Heightmap heightmap in Heightmap.m_heightmaps)
		{
			if (heightmap.IsPointInside(point, radius))
			{
				heightmaps.Add(heightmap);
			}
		}
	}

	public static Heightmap.Biome FindBiome(Vector3 point)
	{
		Heightmap heightmap = Heightmap.FindHeightmap(point);
		if (heightmap)
		{
			return heightmap.GetBiome(point);
		}
		return Heightmap.Biome.None;
	}

	public static bool HaveQueuedRebuild(Vector3 point, float radius)
	{
		Heightmap.tempHmaps.Clear();
		Heightmap.FindHeightmap(point, radius, Heightmap.tempHmaps);
		using (List<Heightmap>.Enumerator enumerator = Heightmap.tempHmaps.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.HaveQueuedRebuild())
				{
					return true;
				}
			}
		}
		return false;
	}

	public static Heightmap.Biome FindBiomeClutter(Vector3 point)
	{
		if (ZoneSystem.instance && !ZoneSystem.instance.IsZoneLoaded(point))
		{
			return Heightmap.Biome.None;
		}
		Heightmap heightmap = Heightmap.FindHeightmap(point);
		if (heightmap)
		{
			return heightmap.GetBiome(point);
		}
		return Heightmap.Biome.None;
	}

	public void Clear()
	{
		this.m_heights.Clear();
		this.m_clearedMask = null;
		this.m_materialInstance = null;
		this.m_buildData = null;
		if (this.m_collisionMesh)
		{
			this.m_collisionMesh.Clear();
		}
		if (this.m_renderMesh)
		{
			this.m_renderMesh.Clear();
		}
		if (this.m_collider)
		{
			this.m_collider.sharedMesh = null;
		}
	}

	private static float[] tempBiomeWeights = new float[513];

	private static List<Heightmap> tempHmaps = new List<Heightmap>();

	public int m_width = 32;

	public float m_scale = 1f;

	public Material m_material;

	private const float m_levelMaxDelta = 8f;

	private const float m_smoothMaxDelta = 1f;

	public bool m_isDistantLod;

	public bool m_distantLodEditorHax;

	private List<float> m_heights = new List<float>();

	private HeightmapBuilder.HMBuildData m_buildData;

	private Texture2D m_clearedMask;

	private Material m_materialInstance;

	private MeshCollider m_collider;

	private float[] m_oceanDepth = new float[4];

	private Heightmap.Biome[] m_cornerBiomes = new Heightmap.Biome[]
	{
		Heightmap.Biome.Meadows,
		Heightmap.Biome.Meadows,
		Heightmap.Biome.Meadows,
		Heightmap.Biome.Meadows
	};

	private Bounds m_bounds;

	private BoundingSphere m_boundingSphere;

	private Mesh m_collisionMesh;

	private Mesh m_renderMesh;

	private bool m_dirty;

	private static List<Heightmap> m_heightmaps = new List<Heightmap>();

	private static List<Vector3> m_tempVertises = new List<Vector3>();

	private static List<Vector2> m_tempUVs = new List<Vector2>();

	private static List<int> m_tempIndices = new List<int>();

	private static List<Color32> m_tempColors = new List<Color32>();

	public enum Biome
	{
		None,
		Meadows,
		Swamp,
		Mountain = 4,
		BlackForest = 8,
		Plains = 16,
		AshLands = 32,
		DeepNorth = 64,
		Ocean = 256,
		Mistlands = 512,
		BiomesMax
	}

	public enum BiomeArea
	{
		Edge = 1,
		Median,
		Everything
	}
}
