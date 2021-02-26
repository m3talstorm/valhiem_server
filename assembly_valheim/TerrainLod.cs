using System;
using UnityEngine;

public class TerrainLod : MonoBehaviour
{
	private void Awake()
	{
		this.m_hmap = base.GetComponent<Heightmap>();
	}

	private void Update()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		if (ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
		{
			return;
		}
		Vector3 position = mainCamera.transform.position;
		if (Utils.DistanceXZ(position, this.m_lastPoint) > this.m_updateStepDistance)
		{
			this.m_lastPoint = new Vector3(Mathf.Round(position.x / this.m_hmap.m_scale) * this.m_hmap.m_scale, 0f, Mathf.Round(position.z / this.m_hmap.m_scale) * this.m_hmap.m_scale);
			this.m_needRebuild = true;
		}
		if (this.m_needRebuild && HeightmapBuilder.instance.IsTerrainReady(this.m_lastPoint, this.m_hmap.m_width, this.m_hmap.m_scale, this.m_hmap.m_isDistantLod, WorldGenerator.instance))
		{
			base.transform.position = this.m_lastPoint;
			this.m_hmap.Regenerate();
			this.m_needRebuild = false;
		}
	}

	public float m_updateStepDistance = 256f;

	private Heightmap m_hmap;

	private Vector3 m_lastPoint = new Vector3(99999f, 0f, 99999f);

	private bool m_needRebuild = true;
}
