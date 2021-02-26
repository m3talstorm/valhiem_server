using System;
using UnityEngine;

public class RandomMovement : MonoBehaviour
{
	private void Start()
	{
		this.m_basePosition = base.transform.localPosition;
	}

	private void Update()
	{
		float num = Time.time * this.m_frequency;
		Vector3 b = new Vector3(Mathf.Sin(num) * Mathf.Sin(num * 0.56436f), Mathf.Sin(num * 0.56436f) * Mathf.Sin(num * 0.688742f), Mathf.Cos(num * 0.758348f) * Mathf.Cos(num * 0.4563696f)) * this.m_movement;
		base.transform.localPosition = this.m_basePosition + b;
	}

	public float m_frequency = 10f;

	public float m_movement = 0.1f;

	private Vector3 m_basePosition = Vector3.zero;
}
