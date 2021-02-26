using System;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
	private void LateUpdate()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (Player.m_localPlayer == null || mainCamera == null)
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		if (this.m_follow == FollowPlayer.Type.Camera || GameCamera.InFreeFly())
		{
			vector = mainCamera.transform.position;
		}
		else
		{
			vector = Player.m_localPlayer.transform.position;
		}
		if (this.m_lockYPos)
		{
			vector.y = base.transform.position.y;
		}
		if (vector.y > this.m_maxYPos)
		{
			vector.y = this.m_maxYPos;
		}
		base.transform.position = vector;
	}

	public FollowPlayer.Type m_follow = FollowPlayer.Type.Camera;

	public bool m_lockYPos;

	public bool m_followCameraInFreefly;

	public float m_maxYPos = 1000000f;

	public enum Type
	{
		Player,
		Camera
	}
}
