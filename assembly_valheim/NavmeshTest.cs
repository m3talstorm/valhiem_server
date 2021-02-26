using System;
using System.Collections.Generic;
using UnityEngine;

public class NavmeshTest : MonoBehaviour
{
	private void Awake()
	{
	}

	private void Update()
	{
		if (Pathfinding.instance.GetPath(base.transform.position, this.m_target.position, this.m_path, this.m_agentType, false, this.m_cleanPath))
		{
			this.m_havePath = true;
			return;
		}
		this.m_havePath = false;
	}

	private void OnDrawGizmos()
	{
		if (this.m_target == null)
		{
			return;
		}
		if (this.m_havePath)
		{
			Gizmos.color = Color.yellow;
			for (int i = 0; i < this.m_path.Count - 1; i++)
			{
				Vector3 a = this.m_path[i];
				Vector3 a2 = this.m_path[i + 1];
				Gizmos.DrawLine(a + Vector3.up * 0.2f, a2 + Vector3.up * 0.2f);
			}
			foreach (Vector3 a3 in this.m_path)
			{
				Gizmos.DrawSphere(a3 + Vector3.up * 0.2f, 0.1f);
			}
			Gizmos.color = Color.green;
			Gizmos.DrawSphere(base.transform.position, 0.3f);
			Gizmos.DrawSphere(this.m_target.position, 0.3f);
			return;
		}
		Gizmos.color = Color.red;
		Gizmos.DrawLine(base.transform.position + Vector3.up * 0.2f, this.m_target.position + Vector3.up * 0.2f);
		Gizmos.DrawSphere(base.transform.position, 0.3f);
		Gizmos.DrawSphere(this.m_target.position, 0.3f);
	}

	public Transform m_target;

	public Pathfinding.AgentType m_agentType = Pathfinding.AgentType.Humanoid;

	public bool m_cleanPath = true;

	private List<Vector3> m_path = new List<Vector3>();

	private bool m_havePath;
}
