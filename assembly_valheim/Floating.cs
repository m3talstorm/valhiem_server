using System;
using UnityEngine;

public class Floating : MonoBehaviour, IWaterInteractable
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_collider = base.GetComponentInChildren<Collider>();
		this.SetSurfaceEffect(false);
		base.InvokeRepeating("TerrainCheck", UnityEngine.Random.Range(10f, 30f), 30f);
	}

	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	public bool IsOwner()
	{
		return this.m_nview.IsValid() && this.m_nview.IsOwner();
	}

	private void TerrainCheck()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y - groundHeight < -1f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 1f;
			base.transform.position = position;
			Rigidbody component = base.GetComponent<Rigidbody>();
			if (component)
			{
				component.velocity = Vector3.zero;
			}
			ZLog.Log("Moved up item " + base.gameObject.name);
		}
	}

	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.IsInWater())
		{
			this.SetSurfaceEffect(false);
			return;
		}
		this.UpdateImpactEffect();
		float floatDepth = this.GetFloatDepth();
		if (floatDepth > 0f)
		{
			this.SetSurfaceEffect(false);
			return;
		}
		this.SetSurfaceEffect(true);
		Vector3 position = this.m_collider.ClosestPoint(base.transform.position + Vector3.down * 1000f);
		Vector3 worldCenterOfMass = this.m_body.worldCenterOfMass;
		float d = Mathf.Clamp01(Mathf.Abs(floatDepth) / this.m_forceDistance);
		Vector3 vector = Vector3.up * this.m_force * d * (Time.fixedDeltaTime * 50f);
		this.m_body.WakeUp();
		this.m_body.AddForceAtPosition(vector * this.m_balanceForceFraction, position, ForceMode.VelocityChange);
		this.m_body.AddForceAtPosition(vector, worldCenterOfMass, ForceMode.VelocityChange);
		this.m_body.velocity = this.m_body.velocity - this.m_body.velocity * this.m_damping * d;
		this.m_body.angularVelocity = this.m_body.angularVelocity - this.m_body.angularVelocity * this.m_damping * d;
	}

	public bool IsInWater()
	{
		return this.m_inWater > -10000f;
	}

	private void SetSurfaceEffect(bool enabled)
	{
		if (this.m_surfaceEffects != null)
		{
			this.m_surfaceEffects.SetActive(enabled);
		}
	}

	private void UpdateImpactEffect()
	{
		if (!this.m_body.IsSleeping() && this.m_impactEffects.HasEffects())
		{
			Vector3 vector = this.m_collider.ClosestPoint(base.transform.position + Vector3.down * 1000f);
			if (vector.y < this.m_inWater)
			{
				if (!this.m_wasInWater)
				{
					this.m_wasInWater = true;
					Vector3 pos = vector;
					pos.y = this.m_inWater;
					if (this.m_body.GetPointVelocity(vector).magnitude > Floating.m_minImpactEffectVelocity)
					{
						this.m_impactEffects.Create(pos, Quaternion.identity, null, 1f);
						return;
					}
				}
			}
			else
			{
				this.m_wasInWater = false;
			}
		}
	}

	private float GetFloatDepth()
	{
		return this.m_body.worldCenterOfMass.y - this.m_inWater - this.m_waterLevelOffset;
	}

	public void SetInWater(float waterLevel)
	{
		this.m_inWater = waterLevel;
		if (!this.m_beenInWater && waterLevel > -10000f && this.GetFloatDepth() < 0f)
		{
			this.m_beenInWater = true;
		}
	}

	public bool BeenInWater()
	{
		return this.m_beenInWater;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.down * this.m_waterLevelOffset, new Vector3(1f, 0.05f, 1f));
	}

	public float m_waterLevelOffset;

	public float m_forceDistance = 1f;

	public float m_force = 0.5f;

	public float m_balanceForceFraction = 0.02f;

	public float m_damping = 0.05f;

	private static float m_minImpactEffectVelocity = 0.5f;

	public EffectList m_impactEffects = new EffectList();

	public GameObject m_surfaceEffects;

	private float m_inWater = -10000f;

	private bool m_beenInWater;

	private bool m_wasInWater = true;

	private Rigidbody m_body;

	private Collider m_collider;

	private ZNetView m_nview;
}
