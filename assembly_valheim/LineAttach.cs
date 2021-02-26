using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LineAttach : MonoBehaviour
{
	private void Start()
	{
		this.m_lineRenderer = base.GetComponent<LineRenderer>();
	}

	private void LateUpdate()
	{
		for (int i = 0; i < this.m_attachments.Count; i++)
		{
			Transform transform = this.m_attachments[i];
			if (transform)
			{
				this.m_lineRenderer.SetPosition(i, base.transform.InverseTransformPoint(transform.position));
			}
		}
	}

	public List<Transform> m_attachments = new List<Transform>();

	private LineRenderer m_lineRenderer;
}
