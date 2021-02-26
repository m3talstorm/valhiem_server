using System;
using System.Collections.Generic;
using UnityEngine;

public class Tail : MonoBehaviour
{
	private void Awake()
	{
		foreach (Transform transform in this.m_tailJoints)
		{
			float distance = Vector3.Distance(transform.parent.position, transform.position);
			Vector3 position = transform.position;
			Tail.TailSegment tailSegment = new Tail.TailSegment();
			tailSegment.transform = transform;
			tailSegment.pos = position;
			tailSegment.rot = transform.rotation;
			tailSegment.distance = distance;
			this.m_positions.Add(tailSegment);
		}
	}

	private void LateUpdate()
	{
		float deltaTime = Time.deltaTime;
		if (this.m_character)
		{
			this.m_character.IsSwiming();
		}
		for (int i = 0; i < this.m_positions.Count; i++)
		{
			Tail.TailSegment tailSegment = this.m_positions[i];
			if (this.m_waterSurfaceCheck)
			{
				float waterLevel = WaterVolume.GetWaterLevel(tailSegment.pos, 1f);
				if (tailSegment.pos.y + this.m_tailRadius > waterLevel)
				{
					Tail.TailSegment tailSegment2 = tailSegment;
					tailSegment2.pos.y = tailSegment2.pos.y - this.m_gravity * deltaTime;
				}
				else
				{
					Tail.TailSegment tailSegment3 = tailSegment;
					tailSegment3.pos.y = tailSegment3.pos.y - this.m_gravityInWater * deltaTime;
				}
			}
			else
			{
				Tail.TailSegment tailSegment4 = tailSegment;
				tailSegment4.pos.y = tailSegment4.pos.y - this.m_gravity * deltaTime;
			}
			Vector3 a = tailSegment.transform.parent.position + tailSegment.transform.parent.up * tailSegment.distance * 0.5f;
			Vector3 vector = Vector3.Normalize(a - tailSegment.pos);
			vector = Vector3.RotateTowards(-tailSegment.transform.parent.up, vector, 0.017453292f * this.m_maxAngle, 1f);
			Vector3 vector2 = a - vector * tailSegment.distance * 0.5f;
			if (this.m_groundCheck)
			{
				float groundHeight = ZoneSystem.instance.GetGroundHeight(vector2);
				if (vector2.y - this.m_tailRadius < groundHeight)
				{
					vector2.y = groundHeight + this.m_tailRadius;
				}
			}
			vector2 = Vector3.Lerp(tailSegment.pos, vector2, this.m_smoothness);
			Vector3 normalized = (a - vector2).normalized;
			Vector3 rhs = Vector3.Cross(Vector3.up, -normalized);
			Quaternion quaternion = Quaternion.LookRotation(Vector3.Cross(-normalized, rhs), -normalized);
			quaternion = Quaternion.Slerp(tailSegment.rot, quaternion, this.m_smoothness);
			tailSegment.transform.position = vector2;
			tailSegment.transform.rotation = quaternion;
			tailSegment.pos = vector2;
			tailSegment.rot = quaternion;
		}
		if (this.m_tailBody)
		{
			this.m_tailBody.velocity = Vector3.zero;
			this.m_tailBody.angularVelocity = Vector3.zero;
		}
	}

	public List<Transform> m_tailJoints = new List<Transform>();

	public float m_yMovementDistance = 0.5f;

	public float m_yMovementFreq = 0.5f;

	public float m_yMovementOffset = 0.2f;

	public float m_maxAngle = 80f;

	public float m_gravity = 2f;

	public float m_gravityInWater = 0.1f;

	public bool m_waterSurfaceCheck;

	public bool m_groundCheck;

	public float m_smoothness = 0.1f;

	public float m_tailRadius;

	public Character m_character;

	public Rigidbody m_characterBody;

	public Rigidbody m_tailBody;

	private List<Tail.TailSegment> m_positions = new List<Tail.TailSegment>();

	private class TailSegment
	{
		public Transform transform;

		public Vector3 pos;

		public Quaternion rot;

		public float distance;
	}
}
