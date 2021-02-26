using System;
using System.Collections.Generic;
using UnityEngine;

public class StationExtension : MonoBehaviour, Hoverable
{
	private void Awake()
	{
		if (base.GetComponent<ZNetView>().GetZDO() == null)
		{
			return;
		}
		this.m_piece = base.GetComponent<Piece>();
		StationExtension.m_allExtensions.Add(this);
	}

	private void OnDestroy()
	{
		if (this.m_connection)
		{
			UnityEngine.Object.Destroy(this.m_connection);
			this.m_connection = null;
		}
		StationExtension.m_allExtensions.Remove(this);
	}

	public string GetHoverText()
	{
		this.PokeEffect();
		return Localization.instance.Localize(this.m_piece.m_name);
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_piece.m_name);
	}

	public string GetExtensionName()
	{
		return this.m_piece.m_name;
	}

	public static void FindExtensions(CraftingStation station, Vector3 pos, List<StationExtension> extensions)
	{
		foreach (StationExtension stationExtension in StationExtension.m_allExtensions)
		{
			if (Vector3.Distance(stationExtension.transform.position, pos) < stationExtension.m_maxStationDistance && stationExtension.m_craftingStation.m_name == station.m_name && !StationExtension.ExtensionInList(extensions, stationExtension))
			{
				extensions.Add(stationExtension);
			}
		}
	}

	private static bool ExtensionInList(List<StationExtension> extensions, StationExtension extension)
	{
		using (List<StationExtension>.Enumerator enumerator = extensions.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.GetExtensionName() == extension.GetExtensionName())
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool OtherExtensionInRange(float radius)
	{
		foreach (StationExtension stationExtension in StationExtension.m_allExtensions)
		{
			if (!(stationExtension == this) && Vector3.Distance(stationExtension.transform.position, base.transform.position) < radius)
			{
				return true;
			}
		}
		return false;
	}

	public List<CraftingStation> FindStationsInRange(Vector3 center)
	{
		List<CraftingStation> list = new List<CraftingStation>();
		CraftingStation.FindStationsInRange(this.m_craftingStation.m_name, center, this.m_maxStationDistance, list);
		return list;
	}

	public CraftingStation FindClosestStationInRange(Vector3 center)
	{
		return CraftingStation.FindClosestStationInRange(this.m_craftingStation.m_name, center, this.m_maxStationDistance);
	}

	private void PokeEffect()
	{
		CraftingStation craftingStation = this.FindClosestStationInRange(base.transform.position);
		if (craftingStation)
		{
			this.StartConnectionEffect(craftingStation);
		}
	}

	public void StartConnectionEffect(CraftingStation station)
	{
		this.StartConnectionEffect(station.GetConnectionEffectPoint());
	}

	public void StartConnectionEffect(Vector3 targetPos)
	{
		Vector3 center = this.GetCenter();
		if (this.m_connection == null)
		{
			this.m_connection = UnityEngine.Object.Instantiate<GameObject>(this.m_connectionPrefab, center, Quaternion.identity);
		}
		Vector3 vector = targetPos - center;
		Quaternion rotation = Quaternion.LookRotation(vector.normalized);
		this.m_connection.transform.position = center;
		this.m_connection.transform.rotation = rotation;
		this.m_connection.transform.localScale = new Vector3(1f, 1f, vector.magnitude);
		base.CancelInvoke("StopConnectionEffect");
		base.Invoke("StopConnectionEffect", 1f);
	}

	public void StopConnectionEffect()
	{
		if (this.m_connection)
		{
			UnityEngine.Object.Destroy(this.m_connection);
			this.m_connection = null;
		}
	}

	private Vector3 GetCenter()
	{
		if (this.m_colliders == null)
		{
			this.m_colliders = base.GetComponentsInChildren<Collider>();
		}
		Vector3 position = base.transform.position;
		foreach (Collider collider in this.m_colliders)
		{
			if (collider.bounds.max.y > position.y)
			{
				position.y = collider.bounds.max.y;
			}
		}
		return position;
	}

	public CraftingStation m_craftingStation;

	public float m_maxStationDistance = 5f;

	public GameObject m_connectionPrefab;

	private GameObject m_connection;

	private Piece m_piece;

	private Collider[] m_colliders;

	private static List<StationExtension> m_allExtensions = new List<StationExtension>();
}
