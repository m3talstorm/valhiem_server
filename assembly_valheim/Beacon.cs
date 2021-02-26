using System;
using System.Collections.Generic;
using UnityEngine;

public class Beacon : MonoBehaviour
{
	private void Awake()
	{
		Beacon.m_instances.Add(this);
	}

	private void OnDestroy()
	{
		Beacon.m_instances.Remove(this);
	}

	public static Beacon FindClosestBeaconInRange(Vector3 point)
	{
		Beacon beacon = null;
		float num = 999999f;
		foreach (Beacon beacon2 in Beacon.m_instances)
		{
			float num2 = Vector3.Distance(point, beacon2.transform.position);
			if (num2 < beacon2.m_range && (beacon == null || num2 < num))
			{
				beacon = beacon2;
				num = num2;
			}
		}
		return beacon;
	}

	public static void FindBeaconsInRange(Vector3 point, List<Beacon> becons)
	{
		foreach (Beacon beacon in Beacon.m_instances)
		{
			if (Vector3.Distance(point, beacon.transform.position) < beacon.m_range)
			{
				becons.Add(beacon);
			}
		}
	}

	public float m_range = 20f;

	private static List<Beacon> m_instances = new List<Beacon>();
}
