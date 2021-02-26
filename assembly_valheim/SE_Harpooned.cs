using System;
using UnityEngine;

public class SE_Harpooned : StatusEffect
{
	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override void SetAttacker(Character attacker)
	{
		ZLog.Log("Setting attacker " + attacker.m_name);
		this.m_attacker = attacker;
		this.m_time = 0f;
		if (Vector3.Distance(this.m_attacker.transform.position, this.m_character.transform.position) > this.m_maxDistance)
		{
			this.m_attacker.Message(MessageHud.MessageType.Center, "Target too far", 0, null);
			this.m_broken = true;
			return;
		}
		this.m_attacker.Message(MessageHud.MessageType.Center, this.m_character.m_name + " harpooned", 0, null);
		foreach (GameObject gameObject in this.m_startEffectInstances)
		{
			if (gameObject)
			{
				LineConnect component = gameObject.GetComponent<LineConnect>();
				if (component)
				{
					component.SetPeer(this.m_attacker.GetComponent<ZNetView>());
				}
			}
		}
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (!this.m_attacker)
		{
			return;
		}
		Rigidbody component = this.m_character.GetComponent<Rigidbody>();
		if (component)
		{
			Vector3 vector = this.m_attacker.transform.position - this.m_character.transform.position;
			Vector3 normalized = vector.normalized;
			float radius = this.m_character.GetRadius();
			float magnitude = vector.magnitude;
			float num = Mathf.Clamp01(Vector3.Dot(normalized, component.velocity));
			float t = Utils.LerpStep(this.m_minDistance, this.m_maxDistance, magnitude);
			float num2 = Mathf.Lerp(this.m_minForce, this.m_maxForce, t);
			float num3 = Mathf.Clamp01(this.m_maxMass / component.mass);
			float num4 = num2 * num3;
			if (magnitude - radius > this.m_minDistance && num < num4)
			{
				normalized.y = 0f;
				normalized.Normalize();
				if (this.m_character.GetStandingOnShip() == null && !this.m_character.IsAttached())
				{
					component.AddForce(normalized * num4, ForceMode.VelocityChange);
				}
				this.m_drainStaminaTimer += dt;
				if (this.m_drainStaminaTimer > this.m_staminaDrainInterval)
				{
					this.m_drainStaminaTimer = 0f;
					float num5 = 1f - Mathf.Clamp01(num / num2);
					this.m_attacker.UseStamina(this.m_staminaDrain * num5);
				}
			}
			if (magnitude > this.m_maxDistance)
			{
				this.m_broken = true;
				this.m_attacker.Message(MessageHud.MessageType.Center, "Line broke", 0, null);
			}
			if (!this.m_attacker.HaveStamina(0f))
			{
				this.m_broken = true;
				this.m_attacker.Message(MessageHud.MessageType.Center, this.m_character.m_name + " escaped", 0, null);
			}
		}
	}

	public override bool IsDone()
	{
		if (base.IsDone())
		{
			return true;
		}
		if (this.m_broken)
		{
			return true;
		}
		if (!this.m_attacker)
		{
			return true;
		}
		if (this.m_time > 2f && (this.m_attacker.IsBlocking() || this.m_attacker.InAttack()))
		{
			this.m_attacker.Message(MessageHud.MessageType.Center, this.m_character.m_name + " released", 0, null);
			return true;
		}
		return false;
	}

	[Header("SE_Harpooned")]
	public float m_minForce = 2f;

	public float m_maxForce = 10f;

	public float m_minDistance = 6f;

	public float m_maxDistance = 30f;

	public float m_staminaDrain = 10f;

	public float m_staminaDrainInterval = 0.1f;

	public float m_maxMass = 50f;

	private bool m_broken;

	private Character m_attacker;

	private float m_drainStaminaTimer;
}
