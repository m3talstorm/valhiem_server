using System;
using UnityEngine;

public class MovementTest : MonoBehaviour
{
	private void Start()
	{
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_center = base.transform.position;
	}

	private void FixedUpdate()
	{
		this.m_timer += Time.fixedDeltaTime;
		float num = 5f;
		Vector3 vector = this.m_center + new Vector3(Mathf.Sin(this.m_timer * this.m_speed) * num, 0f, Mathf.Cos(this.m_timer * this.m_speed) * num);
		this.m_vel = (vector - this.m_body.position) / Time.fixedDeltaTime;
		this.m_body.position = vector;
		this.m_body.velocity = this.m_vel;
	}

	public float m_speed = 10f;

	private float m_timer;

	private Rigidbody m_body;

	private Vector3 m_center;

	private Vector3 m_vel;
}
