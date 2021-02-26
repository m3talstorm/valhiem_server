using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainModifier : MonoBehaviour
{
	private void Awake()
	{
		TerrainModifier.m_instances.Add(this);
		TerrainModifier.m_needsSorting = true;
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_wasEnabled = base.enabled;
		if (base.enabled)
		{
			if (TerrainModifier.m_triggerOnPlaced)
			{
				this.OnPlaced();
			}
			this.PokeHeightmaps();
		}
	}

	private void OnDestroy()
	{
		TerrainModifier.m_instances.Remove(this);
		TerrainModifier.m_needsSorting = true;
		if (this.m_wasEnabled)
		{
			this.PokeHeightmaps();
		}
	}

	private void PokeHeightmaps()
	{
		bool delayed = !TerrainModifier.m_triggerOnPlaced;
		foreach (Heightmap heightmap in Heightmap.GetAllHeightmaps())
		{
			if (heightmap.TerrainVSModifier(this))
			{
				heightmap.Poke(delayed);
			}
		}
		if (ClutterSystem.instance)
		{
			ClutterSystem.instance.ResetGrass(base.transform.position, this.GetRadius());
		}
	}

	public float GetRadius()
	{
		float num = 0f;
		if (this.m_level && this.m_levelRadius > num)
		{
			num = this.m_levelRadius;
		}
		if (this.m_smooth && this.m_smoothRadius > num)
		{
			num = this.m_smoothRadius;
		}
		if (this.m_paintCleared && this.m_paintRadius > num)
		{
			num = this.m_paintRadius;
		}
		return num;
	}

	public static void SetTriggerOnPlaced(bool trigger)
	{
		TerrainModifier.m_triggerOnPlaced = trigger;
	}

	private void OnPlaced()
	{
		this.RemoveOthers(base.transform.position, this.GetRadius() / 4f);
		this.m_onPlacedEffect.Create(base.transform.position, Quaternion.identity, null, 1f);
		if (this.m_spawnOnPlaced)
		{
			if (!this.m_spawnAtMaxLevelDepth && Heightmap.AtMaxLevelDepth(base.transform.position + Vector3.up * this.m_levelOffset))
			{
				return;
			}
			if (UnityEngine.Random.value <= this.m_chanceToSpawn)
			{
				Vector3 b = UnityEngine.Random.insideUnitCircle * 0.2f;
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnOnPlaced, base.transform.position + Vector3.up * 0.5f + b, Quaternion.identity);
				gameObject.GetComponent<ItemDrop>().m_itemData.m_stack = UnityEngine.Random.Range(1, this.m_maxSpawned + 1);
				gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
			}
		}
	}

	private static void GetModifiers(Vector3 point, float range, List<TerrainModifier> modifiers, TerrainModifier ignore = null)
	{
		foreach (TerrainModifier terrainModifier in TerrainModifier.m_instances)
		{
			if (!(terrainModifier == ignore) && Utils.DistanceXZ(point, terrainModifier.transform.position) < range)
			{
				modifiers.Add(terrainModifier);
			}
		}
	}

	public static Piece FindClosestModifierPieceInRange(Vector3 point, float range)
	{
		float num = 999999f;
		TerrainModifier terrainModifier = null;
		foreach (TerrainModifier terrainModifier2 in TerrainModifier.m_instances)
		{
			if (!(terrainModifier2.m_nview == null))
			{
				float num2 = Utils.DistanceXZ(point, terrainModifier2.transform.position);
				if (num2 <= range && num2 <= num)
				{
					num = num2;
					terrainModifier = terrainModifier2;
				}
			}
		}
		if (terrainModifier)
		{
			return terrainModifier.GetComponent<Piece>();
		}
		return null;
	}

	private void RemoveOthers(Vector3 point, float range)
	{
		List<TerrainModifier> list = new List<TerrainModifier>();
		TerrainModifier.GetModifiers(point, range, list, this);
		int num = 0;
		foreach (TerrainModifier terrainModifier in list)
		{
			if ((this.m_level || !terrainModifier.m_level) && (!this.m_paintCleared || this.m_paintType != TerrainModifier.PaintType.Reset || (terrainModifier.m_paintCleared && terrainModifier.m_paintType == TerrainModifier.PaintType.Reset)) && terrainModifier.m_nview)
			{
				num++;
				terrainModifier.m_nview.ClaimOwnership();
				terrainModifier.m_nview.Destroy();
			}
		}
	}

	private static int SortByModifiers(TerrainModifier a, TerrainModifier b)
	{
		if (a.m_playerModifiction != b.m_playerModifiction)
		{
			return a.m_playerModifiction.CompareTo(b.m_playerModifiction);
		}
		if (a.m_sortOrder == b.m_sortOrder)
		{
			return a.GetCreationTime().CompareTo(b.GetCreationTime());
		}
		return a.m_sortOrder.CompareTo(b.m_sortOrder);
	}

	public static List<TerrainModifier> GetAllInstances()
	{
		if (TerrainModifier.m_needsSorting)
		{
			TerrainModifier.m_instances.Sort(new Comparison<TerrainModifier>(TerrainModifier.SortByModifiers));
			TerrainModifier.m_needsSorting = false;
		}
		return TerrainModifier.m_instances;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position + Vector3.up * this.m_levelOffset, Quaternion.identity, new Vector3(1f, 0f, 1f));
		if (this.m_level)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(Vector3.zero, this.m_levelRadius);
		}
		if (this.m_smooth)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(Vector3.zero, this.m_smoothRadius);
		}
		if (this.m_paintCleared)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(Vector3.zero, this.m_paintRadius);
		}
		Gizmos.matrix = Matrix4x4.identity;
	}

	public long GetCreationTime()
	{
		if (this.m_nview && this.m_nview.GetZDO() != null)
		{
			return this.m_nview.GetZDO().m_timeCreated;
		}
		return 0L;
	}

	private static bool m_triggerOnPlaced = false;

	public int m_sortOrder;

	public bool m_playerModifiction;

	public float m_levelOffset;

	[Header("Level")]
	public bool m_level;

	public float m_levelRadius = 2f;

	public bool m_square = true;

	[Header("Smooth")]
	public bool m_smooth;

	public float m_smoothRadius = 2f;

	public float m_smoothPower = 3f;

	[Header("Paint")]
	public bool m_paintCleared = true;

	public bool m_paintHeightCheck;

	public TerrainModifier.PaintType m_paintType;

	public float m_paintRadius = 2f;

	[Header("Effects")]
	public EffectList m_onPlacedEffect = new EffectList();

	[Header("Spawn items")]
	public GameObject m_spawnOnPlaced;

	public float m_chanceToSpawn = 1f;

	public int m_maxSpawned = 1;

	public bool m_spawnAtMaxLevelDepth = true;

	private bool m_wasEnabled;

	private ZNetView m_nview;

	private static List<TerrainModifier> m_instances = new List<TerrainModifier>();

	private static bool m_needsSorting = false;

	public enum PaintType
	{
		Dirt,
		Cultivate,
		Paved,
		Reset
	}
}
