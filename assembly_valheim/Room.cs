using System;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, new Vector3(1f, 1f, 1f));
		Gizmos.DrawWireCube(Vector3.zero, new Vector3((float)this.m_size.x, (float)this.m_size.y, (float)this.m_size.z));
		Gizmos.matrix = Matrix4x4.identity;
	}

	public int GetHash()
	{
		return ZNetView.GetPrefabName(base.gameObject).GetStableHashCode();
	}

	private void OnEnable()
	{
		this.m_roomConnections = null;
	}

	public RoomConnection[] GetConnections()
	{
		if (this.m_roomConnections == null)
		{
			this.m_roomConnections = base.GetComponentsInChildren<RoomConnection>(false);
		}
		return this.m_roomConnections;
	}

	public RoomConnection GetConnection(RoomConnection other)
	{
		RoomConnection[] connections = this.GetConnections();
		Room.tempConnections.Clear();
		foreach (RoomConnection roomConnection in connections)
		{
			if (roomConnection.m_type == other.m_type)
			{
				Room.tempConnections.Add(roomConnection);
			}
		}
		if (Room.tempConnections.Count == 0)
		{
			return null;
		}
		return Room.tempConnections[UnityEngine.Random.Range(0, Room.tempConnections.Count)];
	}

	public RoomConnection GetEntrance()
	{
		RoomConnection[] connections = this.GetConnections();
		ZLog.Log("Connections " + connections.Length);
		foreach (RoomConnection roomConnection in connections)
		{
			if (roomConnection.m_entrance)
			{
				return roomConnection;
			}
		}
		return null;
	}

	public bool HaveConnection(RoomConnection other)
	{
		RoomConnection[] connections = this.GetConnections();
		for (int i = 0; i < connections.Length; i++)
		{
			if (connections[i].m_type == other.m_type)
			{
				return true;
			}
		}
		return false;
	}

	private static List<RoomConnection> tempConnections = new List<RoomConnection>();

	public Vector3Int m_size = new Vector3Int(8, 4, 8);

	[BitMask(typeof(Room.Theme))]
	public Room.Theme m_theme = Room.Theme.Crypt;

	public bool m_enabled = true;

	public bool m_entrance;

	public bool m_endCap;

	public int m_endCapPrio;

	public int m_minPlaceOrder;

	public float m_weight = 1f;

	public bool m_faceCenter;

	public bool m_perimeter;

	[NonSerialized]
	public int m_placeOrder;

	private RoomConnection[] m_roomConnections;

	public enum Theme
	{
		Crypt = 1,
		SunkenCrypt,
		Cave = 4,
		ForestCrypt = 8,
		GoblinCamp = 16,
		MeadowsVillage = 32,
		MeadowsFarm = 64
	}
}
