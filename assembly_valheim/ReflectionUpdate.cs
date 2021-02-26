using System;
using UnityEngine;

public class ReflectionUpdate : MonoBehaviour
{
	public static ReflectionUpdate instance
	{
		get
		{
			return ReflectionUpdate.m_instance;
		}
	}

	private void Start()
	{
		ReflectionUpdate.m_instance = this;
		this.m_current = this.m_probe1;
	}

	private void OnDestroy()
	{
		ReflectionUpdate.m_instance = null;
	}

	public void UpdateReflection()
	{
		Vector3 vector = ZNet.instance.GetReferencePosition();
		vector += Vector3.up * this.m_reflectionHeight;
		this.m_current = ((this.m_current == this.m_probe1) ? this.m_probe2 : this.m_probe1);
		this.m_current.transform.position = vector;
		this.m_renderID = this.m_current.RenderProbe();
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		this.m_updateTimer += deltaTime;
		if (this.m_updateTimer > this.m_interval)
		{
			this.m_updateTimer = 0f;
			this.UpdateReflection();
		}
		if (this.m_current.IsFinishedRendering(this.m_renderID))
		{
			float num = Mathf.Clamp01(this.m_updateTimer / this.m_transitionDuration);
			num = Mathf.Pow(num, this.m_power);
			if (this.m_probe1 == this.m_current)
			{
				this.m_probe1.importance = 1;
				this.m_probe2.importance = 0;
				Vector3 size = this.m_probe1.size;
				size.x = 2000f * num;
				size.y = 1000f * num;
				size.z = 2000f * num;
				this.m_probe1.size = size;
				this.m_probe2.size = new Vector3(2001f, 1001f, 2001f);
				return;
			}
			this.m_probe1.importance = 0;
			this.m_probe2.importance = 1;
			Vector3 size2 = this.m_probe2.size;
			size2.x = 2000f * num;
			size2.y = 1000f * num;
			size2.z = 2000f * num;
			this.m_probe2.size = size2;
			this.m_probe1.size = new Vector3(2001f, 1001f, 2001f);
		}
	}

	private static ReflectionUpdate m_instance;

	public ReflectionProbe m_probe1;

	public ReflectionProbe m_probe2;

	public float m_interval = 3f;

	public float m_reflectionHeight = 5f;

	public float m_transitionDuration = 3f;

	public float m_power = 1f;

	private ReflectionProbe m_current;

	private int m_renderID;

	private float m_updateTimer;
}
