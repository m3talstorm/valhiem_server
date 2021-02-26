using System;
using UnityEngine;

public class DepthCamera : MonoBehaviour
{
	private void Start()
	{
	}

	private void RenderDepth()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 vector = (Player.m_localPlayer ? Player.m_localPlayer.transform.position : mainCamera.transform.position) + Vector3.up * this.m_offset;
		vector.x = Mathf.Round(vector.x);
		vector.y = Mathf.Round(vector.y);
		vector.z = Mathf.Round(vector.z);
		base.transform.position = vector;
		float lodBias = QualitySettings.lodBias;
		QualitySettings.lodBias = 10f;
		this.m_camera.RenderWithShader(this.m_depthShader, "RenderType");
		QualitySettings.lodBias = lodBias;
		Shader.SetGlobalTexture("_SkyAlphaTexture", this.m_texture);
		Shader.SetGlobalVector("_SkyAlphaPosition", base.transform.position);
	}

	public Shader m_depthShader;

	public float m_offset = 50f;

	public RenderTexture m_texture;

	public float m_updateInterval = 1f;

	private Camera m_camera;
}
