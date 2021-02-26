using System;
using System.Collections.Generic;
using UnityEngine;

public class StaticTarget : MonoBehaviour
{
	public virtual bool IsValidMonsterTarget()
	{
		return true;
	}

	public Vector3 GetCenter()
	{
		if (!this.m_haveCenter)
		{
			List<Collider> allColliders = this.GetAllColliders();
			this.m_center = Vector3.zero;
			foreach (Collider collider in allColliders)
			{
				if (collider)
				{
					this.m_center += collider.bounds.center;
				}
			}
			this.m_center /= (float)this.m_colliders.Count;
		}
		return this.m_center;
	}

	public List<Collider> GetAllColliders()
	{
		if (this.m_colliders == null)
		{
			Collider[] componentsInChildren = base.GetComponentsInChildren<Collider>();
			this.m_colliders = new List<Collider>();
			this.m_colliders.Capacity = componentsInChildren.Length;
			foreach (Collider collider in componentsInChildren)
			{
				if (collider.enabled && collider.gameObject.activeInHierarchy && !collider.isTrigger)
				{
					this.m_colliders.Add(collider);
				}
			}
		}
		return this.m_colliders;
	}

	public Vector3 FindClosestPoint(Vector3 point)
	{
		List<Collider> allColliders = this.GetAllColliders();
		if (allColliders.Count == 0)
		{
			return base.transform.position;
		}
		float num = 9999999f;
		Vector3 result = Vector3.zero;
		foreach (Collider collider in allColliders)
		{
			MeshCollider meshCollider = collider as MeshCollider;
			Vector3 vector = (meshCollider && !meshCollider.convex) ? collider.ClosestPointOnBounds(point) : collider.ClosestPoint(point);
			float num2 = Vector3.Distance(point, vector);
			if (num2 < num)
			{
				result = vector;
				num = num2;
			}
		}
		return result;
	}

	public bool m_primaryTarget;

	public bool m_randomTarget = true;

	private List<Collider> m_colliders;

	private Vector3 m_center;

	private bool m_haveCenter;
}
