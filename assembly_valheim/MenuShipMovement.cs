using System;
using UnityEngine;

public class MenuShipMovement : MonoBehaviour
{
	private void Start()
	{
		this.m_time = (float)UnityEngine.Random.Range(0, 10);
	}

	private void Update()
	{
		this.m_time += Time.deltaTime;
		base.transform.rotation = Quaternion.Euler(Mathf.Sin(this.m_time * this.m_freq) * this.m_xAngle, 0f, Mathf.Sin(this.m_time * 1.5341234f * this.m_freq) * this.m_zAngle);
	}

	public float m_freq = 1f;

	public float m_xAngle = 5f;

	public float m_zAngle = 5f;

	private float m_time;
}
