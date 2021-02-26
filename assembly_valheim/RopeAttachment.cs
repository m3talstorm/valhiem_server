using System;
using UnityEngine;

public class RopeAttachment : MonoBehaviour, Interactable, Hoverable
{
	private void Awake()
	{
		this.m_boatBody = base.GetComponentInParent<Rigidbody>();
	}

	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_puller)
		{
			this.m_puller = null;
			ZLog.Log("Detached rope");
		}
		else
		{
			this.m_puller = character;
			ZLog.Log("Attached rope");
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public string GetHoverText()
	{
		return this.m_hoverText;
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	private void FixedUpdate()
	{
		if (this.m_puller && Vector3.Distance(this.m_puller.transform.position, base.transform.position) > this.m_pullDistance)
		{
			Vector3 position = ((this.m_puller.transform.position - base.transform.position).normalized * this.m_maxPullVel - this.m_boatBody.GetPointVelocity(base.transform.position)) * this.m_pullForce;
			this.m_boatBody.AddForceAtPosition(base.transform.position, position);
		}
	}

	public string m_name = "Rope";

	public string m_hoverText = "Pull";

	public float m_pullDistance = 5f;

	public float m_pullForce = 1f;

	public float m_maxPullVel = 1f;

	private Rigidbody m_boatBody;

	private Character m_puller;
}
