using System;
using UnityEngine;

public class EmitterRotation : MonoBehaviour
{
	private void Start()
	{
		this.m_lastPos = base.transform.position;
		this.m_ps = base.GetComponentInChildren<ParticleSystem>();
	}

	private void Update()
	{
		if (!this.m_ps.emission.enabled)
		{
			return;
		}
		Vector3 position = base.transform.position;
		Vector3 vector = position - this.m_lastPos;
		this.m_lastPos = position;
		float t = Mathf.Clamp01(vector.magnitude / Time.deltaTime / this.m_maxSpeed);
		if (vector == Vector3.zero)
		{
			vector = Vector3.up;
		}
		Quaternion a = Quaternion.LookRotation(Vector3.up);
		Quaternion b = Quaternion.LookRotation(vector);
		Quaternion to = Quaternion.Lerp(a, b, t);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, Time.deltaTime * this.m_rotSpeed);
	}

	public float m_maxSpeed = 10f;

	public float m_rotSpeed = 90f;

	private Vector3 m_lastPos;

	private ParticleSystem m_ps;
}
