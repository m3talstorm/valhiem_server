using System;
using System.Collections.Generic;
using UnityEngine;

public class SlowUpdate : MonoBehaviour
{
	public virtual void Awake()
	{
		SlowUpdate.m_allInstances.Add(this);
		this.m_myIndex = SlowUpdate.m_allInstances.Count - 1;
	}

	public virtual void OnDestroy()
	{
		if (this.m_myIndex != -1)
		{
			SlowUpdate.m_allInstances[this.m_myIndex] = SlowUpdate.m_allInstances[SlowUpdate.m_allInstances.Count - 1];
			SlowUpdate.m_allInstances[this.m_myIndex].m_myIndex = this.m_myIndex;
			SlowUpdate.m_allInstances.RemoveAt(SlowUpdate.m_allInstances.Count - 1);
		}
	}

	public virtual void SUpdate()
	{
	}

	public static List<SlowUpdate> GetAllInstaces()
	{
		return SlowUpdate.m_allInstances;
	}

	private static List<SlowUpdate> m_allInstances = new List<SlowUpdate>();

	private int m_myIndex = -1;
}
