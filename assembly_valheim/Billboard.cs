using System;
using UnityEngine;

public class Billboard : MonoBehaviour
{
	private void Awake()
	{
		this.m_normal = base.transform.up;
	}

	private void LateUpdate()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 vector = mainCamera.transform.position;
		if (this.m_invert)
		{
			vector = base.transform.position - (vector - base.transform.position);
		}
		if (this.m_vertical)
		{
			vector.y = base.transform.position.y;
			base.transform.LookAt(vector, this.m_normal);
			return;
		}
		base.transform.LookAt(vector);
	}

	public bool m_vertical = true;

	public bool m_invert;

	private Vector3 m_normal;
}
