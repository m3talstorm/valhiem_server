using System;
using System.Collections.Generic;
using UnityEngine;

public class SmokeRenderer : MonoBehaviour
{
	private void Start()
	{
		this.m_instanceRenderer = base.GetComponent<InstanceRenderer>();
	}

	private void Update()
	{
		if (Utils.GetMainCamera() == null)
		{
			return;
		}
		this.UpdateInstances();
	}

	private void UpdateInstances()
	{
	}

	private InstanceRenderer m_instanceRenderer;

	private List<Vector4> tempTransforms = new List<Vector4>();
}
