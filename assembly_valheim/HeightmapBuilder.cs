using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

public class HeightmapBuilder
{
	public static HeightmapBuilder instance
	{
		get
		{
			if (HeightmapBuilder.m_instance == null)
			{
				HeightmapBuilder.m_instance = new HeightmapBuilder();
			}
			return HeightmapBuilder.m_instance;
		}
	}

	public HeightmapBuilder()
	{
		HeightmapBuilder.m_instance = this;
		this.m_builder = new Thread(new ThreadStart(this.BuildThread));
		this.m_builder.Start();
	}

	public void Dispose()
	{
		if (this.m_builder != null)
		{
			ZLog.Log("Stoping build thread");
			this.m_lock.WaitOne();
			this.m_stop = true;
			this.m_builder.Abort();
			this.m_lock.ReleaseMutex();
			this.m_builder = null;
		}
		if (this.m_lock != null)
		{
			this.m_lock.Close();
			this.m_lock = null;
		}
	}

	private void BuildThread()
	{
		ZLog.Log("Builder started");
		while (!this.m_stop)
		{
			this.m_lock.WaitOne();
			bool flag = this.m_toBuild.Count > 0;
			this.m_lock.ReleaseMutex();
			if (flag)
			{
				this.m_lock.WaitOne();
				HeightmapBuilder.HMBuildData hmbuildData = this.m_toBuild[0];
				this.m_lock.ReleaseMutex();
				new Stopwatch().Start();
				this.Build(hmbuildData);
				this.m_lock.WaitOne();
				this.m_toBuild.Remove(hmbuildData);
				this.m_ready.Add(hmbuildData);
				while (this.m_ready.Count > 16)
				{
					this.m_ready.RemoveAt(0);
				}
				this.m_lock.ReleaseMutex();
			}
			Thread.Sleep(10);
		}
	}

	private void Build(HeightmapBuilder.HMBuildData data)
	{
		int num = data.m_width + 1;
		int num2 = num * num;
		Vector3 vector = data.m_center + new Vector3((float)data.m_width * data.m_scale * -0.5f, 0f, (float)data.m_width * data.m_scale * -0.5f);
		WorldGenerator worldGen = data.m_worldGen;
		data.m_cornerBiomes = new Heightmap.Biome[4];
		data.m_cornerBiomes[0] = worldGen.GetBiome(vector.x, vector.z);
		data.m_cornerBiomes[1] = worldGen.GetBiome(vector.x + (float)data.m_width * data.m_scale, vector.z);
		data.m_cornerBiomes[2] = worldGen.GetBiome(vector.x, vector.z + (float)data.m_width * data.m_scale);
		data.m_cornerBiomes[3] = worldGen.GetBiome(vector.x + (float)data.m_width * data.m_scale, vector.z + (float)data.m_width * data.m_scale);
		Heightmap.Biome biome = data.m_cornerBiomes[0];
		Heightmap.Biome biome2 = data.m_cornerBiomes[1];
		Heightmap.Biome biome3 = data.m_cornerBiomes[2];
		Heightmap.Biome biome4 = data.m_cornerBiomes[3];
		data.m_baseHeights = new List<float>(num * num);
		for (int i = 0; i < num2; i++)
		{
			data.m_baseHeights.Add(0f);
		}
		for (int j = 0; j < num; j++)
		{
			float wy = vector.z + (float)j * data.m_scale;
			float t = Mathf.SmoothStep(0f, 1f, (float)j / (float)data.m_width);
			for (int k = 0; k < num; k++)
			{
				float wx = vector.x + (float)k * data.m_scale;
				float t2 = Mathf.SmoothStep(0f, 1f, (float)k / (float)data.m_width);
				float value;
				if (data.m_distantLod)
				{
					Heightmap.Biome biome5 = worldGen.GetBiome(wx, wy);
					value = worldGen.GetBiomeHeight(biome5, wx, wy);
				}
				else if (biome3 == biome && biome2 == biome && biome4 == biome)
				{
					value = worldGen.GetBiomeHeight(biome, wx, wy);
				}
				else
				{
					float biomeHeight = worldGen.GetBiomeHeight(biome, wx, wy);
					float biomeHeight2 = worldGen.GetBiomeHeight(biome2, wx, wy);
					float biomeHeight3 = worldGen.GetBiomeHeight(biome3, wx, wy);
					float biomeHeight4 = worldGen.GetBiomeHeight(biome4, wx, wy);
					float a = Mathf.Lerp(biomeHeight, biomeHeight2, t2);
					float b = Mathf.Lerp(biomeHeight3, biomeHeight4, t2);
					value = Mathf.Lerp(a, b, t);
				}
				data.m_baseHeights[j * num + k] = value;
			}
		}
		if (data.m_distantLod)
		{
			for (int l = 0; l < 4; l++)
			{
				List<float> list = new List<float>(data.m_baseHeights);
				for (int m = 1; m < num - 1; m++)
				{
					for (int n = 1; n < num - 1; n++)
					{
						float num3 = list[m * num + n];
						float num4 = list[(m - 1) * num + n];
						float num5 = list[(m + 1) * num + n];
						float num6 = list[m * num + n - 1];
						float num7 = list[m * num + n + 1];
						if (Mathf.Abs(num3 - num4) > 10f)
						{
							num3 = (num3 + num4) * 0.5f;
						}
						if (Mathf.Abs(num3 - num5) > 10f)
						{
							num3 = (num3 + num5) * 0.5f;
						}
						if (Mathf.Abs(num3 - num6) > 10f)
						{
							num3 = (num3 + num6) * 0.5f;
						}
						if (Mathf.Abs(num3 - num7) > 10f)
						{
							num3 = (num3 + num7) * 0.5f;
						}
						data.m_baseHeights[m * num + n] = num3;
					}
				}
			}
		}
	}

	public HeightmapBuilder.HMBuildData RequestTerrainSync(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
	{
		HeightmapBuilder.HMBuildData hmbuildData;
		do
		{
			hmbuildData = this.RequestTerrain(center, width, scale, distantLod, worldGen);
		}
		while (hmbuildData == null);
		return hmbuildData;
	}

	public HeightmapBuilder.HMBuildData RequestTerrain(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
	{
		this.m_lock.WaitOne();
		for (int i = 0; i < this.m_ready.Count; i++)
		{
			HeightmapBuilder.HMBuildData hmbuildData = this.m_ready[i];
			if (hmbuildData.IsEqual(center, width, scale, distantLod, worldGen))
			{
				this.m_ready.RemoveAt(i);
				this.m_lock.ReleaseMutex();
				return hmbuildData;
			}
		}
		for (int j = 0; j < this.m_toBuild.Count; j++)
		{
			if (this.m_toBuild[j].IsEqual(center, width, scale, distantLod, worldGen))
			{
				this.m_lock.ReleaseMutex();
				return null;
			}
		}
		this.m_toBuild.Add(new HeightmapBuilder.HMBuildData(center, width, scale, distantLod, worldGen));
		this.m_lock.ReleaseMutex();
		return null;
	}

	public bool IsTerrainReady(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
	{
		this.m_lock.WaitOne();
		for (int i = 0; i < this.m_ready.Count; i++)
		{
			if (this.m_ready[i].IsEqual(center, width, scale, distantLod, worldGen))
			{
				this.m_lock.ReleaseMutex();
				return true;
			}
		}
		for (int j = 0; j < this.m_toBuild.Count; j++)
		{
			if (this.m_toBuild[j].IsEqual(center, width, scale, distantLod, worldGen))
			{
				this.m_lock.ReleaseMutex();
				return false;
			}
		}
		this.m_toBuild.Add(new HeightmapBuilder.HMBuildData(center, width, scale, distantLod, worldGen));
		this.m_lock.ReleaseMutex();
		return false;
	}

	private static HeightmapBuilder m_instance;

	private const int m_maxReadyQueue = 16;

	private List<HeightmapBuilder.HMBuildData> m_toBuild = new List<HeightmapBuilder.HMBuildData>();

	private List<HeightmapBuilder.HMBuildData> m_ready = new List<HeightmapBuilder.HMBuildData>();

	private Thread m_builder;

	private Mutex m_lock = new Mutex();

	private bool m_stop;

	public class HMBuildData
	{
		public HMBuildData(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
		{
			this.m_center = center;
			this.m_width = width;
			this.m_scale = scale;
			this.m_distantLod = distantLod;
			this.m_worldGen = worldGen;
		}

		public bool IsEqual(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
		{
			return this.m_center == center && this.m_width == width && this.m_scale == scale && this.m_distantLod == distantLod && this.m_worldGen == worldGen;
		}

		public Vector3 m_center;

		public int m_width;

		public float m_scale;

		public bool m_distantLod;

		public bool m_menu;

		public WorldGenerator m_worldGen;

		public Heightmap.Biome[] m_cornerBiomes;

		public List<float> m_baseHeights;
	}
}
