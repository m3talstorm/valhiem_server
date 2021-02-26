using System;
using System.Collections.Generic;
using UnityEngine;

public class CircleProjector : MonoBehaviour
{
	private void Start()
	{
		this.CreateSegments();
	}

	private void Update()
	{
		this.CreateSegments();
		float num = 6.2831855f / (float)this.m_segments.Count;
		for (int i = 0; i < this.m_segments.Count; i++)
		{
			float f = (float)i * num + Time.time * 0.1f;
			Vector3 vector = base.transform.position + new Vector3(Mathf.Sin(f) * this.m_radius, 0f, Mathf.Cos(f) * this.m_radius);
			GameObject gameObject = this.m_segments[i];
			RaycastHit raycastHit;
			if (Physics.Raycast(vector + Vector3.up * 500f, Vector3.down, out raycastHit, 1000f, this.m_mask.value))
			{
				vector.y = raycastHit.point.y;
			}
			gameObject.transform.position = vector;
		}
		for (int j = 0; j < this.m_segments.Count; j++)
		{
			GameObject gameObject2 = this.m_segments[j];
			GameObject gameObject3 = (j == 0) ? this.m_segments[this.m_segments.Count - 1] : this.m_segments[j - 1];
			Vector3 normalized = (((j == this.m_segments.Count - 1) ? this.m_segments[0] : this.m_segments[j + 1]).transform.position - gameObject3.transform.position).normalized;
			gameObject2.transform.rotation = Quaternion.LookRotation(normalized, Vector3.up);
		}
	}

	private void CreateSegments()
	{
		if (this.m_segments.Count == this.m_nrOfSegments)
		{
			return;
		}
		foreach (GameObject obj in this.m_segments)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_segments.Clear();
		for (int i = 0; i < this.m_nrOfSegments; i++)
		{
			GameObject item = UnityEngine.Object.Instantiate<GameObject>(this.m_prefab, base.transform.position, Quaternion.identity, base.transform);
			this.m_segments.Add(item);
		}
	}

	public float m_radius = 5f;

	public int m_nrOfSegments = 20;

	public GameObject m_prefab;

	public LayerMask m_mask;

	private List<GameObject> m_segments = new List<GameObject>();
}
