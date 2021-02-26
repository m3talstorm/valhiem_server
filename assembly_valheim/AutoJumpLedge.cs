using System;
using UnityEngine;

public class AutoJumpLedge : MonoBehaviour
{
	private void OnTriggerStay(Collider collider)
	{
		Character component = collider.GetComponent<Character>();
		if (component)
		{
			component.OnAutoJump(base.transform.forward, this.m_upVel, this.m_forwardVel);
		}
	}

	public bool m_forwardOnly = true;

	public float m_upVel = 1f;

	public float m_forwardVel = 1f;
}
