using System;
using System.Collections.Generic;
using UnityEngine;

public class Smoke : MonoBehaviour
{
	private void Awake()
	{
		this.m_body = base.GetComponent<Rigidbody>();
		Smoke.m_smoke.Add(this);
		this.m_added = true;
		this.m_mr = base.GetComponent<MeshRenderer>();
		this.m_vel += Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * this.m_randomVel;
	}

	private void OnDestroy()
	{
		if (this.m_added)
		{
			Smoke.m_smoke.Remove(this);
			this.m_added = false;
		}
	}

	public void StartFadeOut()
	{
		if (this.m_fadeTimer >= 0f)
		{
			return;
		}
		if (this.m_added)
		{
			Smoke.m_smoke.Remove(this);
			this.m_added = false;
		}
		this.m_fadeTimer = 0f;
	}

	public static int GetTotalSmoke()
	{
		return Smoke.m_smoke.Count;
	}

	public static void FadeOldest()
	{
		if (Smoke.m_smoke.Count == 0)
		{
			return;
		}
		Smoke.m_smoke[0].StartFadeOut();
	}

	public static void FadeMostDistant()
	{
		if (Smoke.m_smoke.Count == 0)
		{
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 position = mainCamera.transform.position;
		int num = -1;
		float num2 = 0f;
		for (int i = 0; i < Smoke.m_smoke.Count; i++)
		{
			float num3 = Vector3.Distance(Smoke.m_smoke[i].transform.position, position);
			if (num3 > num2)
			{
				num = i;
				num2 = num3;
			}
		}
		if (num != -1)
		{
			Smoke.m_smoke[num].StartFadeOut();
		}
	}

	private void Update()
	{
		this.m_time += Time.deltaTime;
		if (this.m_time > this.m_ttl && this.m_fadeTimer < 0f)
		{
			this.StartFadeOut();
		}
		float num = 1f - Mathf.Clamp01(this.m_time / this.m_ttl);
		this.m_body.mass = num * num;
		Vector3 velocity = this.m_body.velocity;
		Vector3 vel = this.m_vel;
		vel.y *= num;
		Vector3 a = vel - velocity;
		this.m_body.AddForce(a * this.m_force * Time.deltaTime, ForceMode.VelocityChange);
		if (this.m_fadeTimer >= 0f)
		{
			this.m_fadeTimer += Time.deltaTime;
			float a2 = 1f - Mathf.Clamp01(this.m_fadeTimer / this.m_fadetime);
			Color color = this.m_mr.material.color;
			color.a = a2;
			this.m_mr.material.color = color;
			if (this.m_fadeTimer >= this.m_fadetime)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}
	}

	public Vector3 m_vel = Vector3.up;

	public float m_randomVel = 0.1f;

	public float m_force = 0.1f;

	public float m_ttl = 10f;

	public float m_fadetime = 3f;

	private Rigidbody m_body;

	private float m_time;

	private float m_fadeTimer = -1f;

	private bool m_added;

	private MeshRenderer m_mr;

	private static List<Smoke> m_smoke = new List<Smoke>();
}
