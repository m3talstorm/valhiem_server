using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Location : MonoBehaviour
{
	private void Awake()
	{
		Location.m_allLocations.Add(this);
		if (this.m_hasInterior)
		{
			Vector3 zoneCenter = this.GetZoneCenter();
			Vector3 position = new Vector3(zoneCenter.x, base.transform.position.y + 5000f, zoneCenter.z);
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_interiorPrefab, position, Quaternion.identity, base.transform);
			gameObject.transform.localScale = new Vector3(ZoneSystem.instance.m_zoneSize, 500f, ZoneSystem.instance.m_zoneSize);
			gameObject.GetComponent<EnvZone>().m_environment = this.m_interiorEnvironment;
		}
	}

	private Vector3 GetZoneCenter()
	{
		Vector2i zone = ZoneSystem.instance.GetZone(base.transform.position);
		return ZoneSystem.instance.GetZonePos(zone);
	}

	private void OnDestroy()
	{
		Location.m_allLocations.Remove(this);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position + new Vector3(0f, -0.01f, 0f), Quaternion.identity, new Vector3(1f, 0.001f, 1f));
		Gizmos.DrawSphere(Vector3.zero, this.m_exteriorRadius);
		Gizmos.matrix = Matrix4x4.identity;
		Utils.DrawGizmoCircle(base.transform.position, this.m_exteriorRadius, 32);
		if (this.m_hasInterior)
		{
			Utils.DrawGizmoCircle(base.transform.position + new Vector3(0f, 5000f, 0f), this.m_interiorRadius, 32);
			Utils.DrawGizmoCircle(base.transform.position, this.m_interiorRadius, 32);
			Gizmos.matrix = Matrix4x4.TRS(base.transform.position + new Vector3(0f, 5000f, 0f), Quaternion.identity, new Vector3(1f, 0.001f, 1f));
			Gizmos.DrawSphere(Vector3.zero, this.m_interiorRadius);
			Gizmos.matrix = Matrix4x4.identity;
		}
	}

	private float GetMaxRadius()
	{
		if (!this.m_hasInterior)
		{
			return this.m_exteriorRadius;
		}
		return Mathf.Max(this.m_exteriorRadius, this.m_interiorRadius);
	}

	public bool IsInside(Vector3 point, float radius)
	{
		float maxRadius = this.GetMaxRadius();
		return Utils.DistanceXZ(base.transform.position, point) < maxRadius;
	}

	public static bool IsInsideLocation(Vector3 point, float distance)
	{
		using (List<Location>.Enumerator enumerator = Location.m_allLocations.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.IsInside(point, distance))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static Location GetLocation(Vector3 point)
	{
		foreach (Location location in Location.m_allLocations)
		{
			if (location.IsInside(point, 0f))
			{
				return location;
			}
		}
		return null;
	}

	public static bool IsInsideNoBuildLocation(Vector3 point)
	{
		foreach (Location location in Location.m_allLocations)
		{
			if (location.m_noBuild && location.IsInside(point, 0f))
			{
				return true;
			}
		}
		return false;
	}

	[FormerlySerializedAs("m_radius")]
	public float m_exteriorRadius = 20f;

	public bool m_noBuild = true;

	public bool m_clearArea = true;

	[Header("Other")]
	public bool m_applyRandomDamage;

	[Header("Interior")]
	public bool m_hasInterior;

	public float m_interiorRadius = 20f;

	public string m_interiorEnvironment = "";

	public GameObject m_interiorPrefab;

	private static List<Location> m_allLocations = new List<Location>();
}
