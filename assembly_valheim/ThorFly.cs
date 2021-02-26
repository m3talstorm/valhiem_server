using System;
using UnityEngine;

public class ThorFly : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
		base.transform.position = base.transform.position + base.transform.forward * this.m_speed * Time.deltaTime;
		this.m_timer += Time.deltaTime;
		if (this.m_timer > this.m_ttl)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public float m_speed = 100f;

	public float m_ttl = 10f;

	private float m_timer;
}
