using System;
using UnityEngine;

public class Leviathan : MonoBehaviour
{
	private void Awake()
	{
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_zanimator = base.GetComponent<ZSyncAnimation>();
		this.m_animator = base.GetComponentInChildren<Animator>();
		if (base.GetComponent<MineRock>())
		{
			MineRock mineRock = this.m_mineRock;
			mineRock.m_onHit = (Action)Delegate.Combine(mineRock.m_onHit, new Action(this.OnHit));
		}
	}

	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		float waterLevel = WaterVolume.GetWaterLevel(base.transform.position, this.m_waveScale);
		if (waterLevel > -100f)
		{
			Vector3 position = this.m_body.position;
			float num = Mathf.Clamp((waterLevel - (position.y + this.m_floatOffset)) * this.m_movementSpeed * Time.fixedDeltaTime, -this.m_maxSpeed, this.m_maxSpeed);
			position.y += num;
			this.m_body.MovePosition(position);
		}
		else
		{
			Vector3 position2 = this.m_body.position;
			position2.y = 0f;
			this.m_body.MovePosition(Vector3.MoveTowards(this.m_body.position, position2, Time.deltaTime));
		}
		if (this.m_animator.GetCurrentAnimatorStateInfo(0).IsTag("submerged"))
		{
			this.m_nview.Destroy();
		}
	}

	private void OnHit()
	{
		if (UnityEngine.Random.value <= this.m_hitReactionChance)
		{
			if (this.m_left)
			{
				return;
			}
			this.m_reactionEffects.Create(base.transform.position, base.transform.rotation, null, 1f);
			this.m_zanimator.SetTrigger("shake");
			base.Invoke("Leave", (float)this.m_leaveDelay);
		}
	}

	private void Leave()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_left)
		{
			return;
		}
		this.m_left = true;
		this.m_leaveEffects.Create(base.transform.position, base.transform.rotation, null, 1f);
		this.m_zanimator.SetTrigger("dive");
	}

	public float m_waveScale = 0.5f;

	public float m_floatOffset;

	public float m_movementSpeed = 0.1f;

	public float m_maxSpeed = 1f;

	public MineRock m_mineRock;

	public float m_hitReactionChance = 0.25f;

	public int m_leaveDelay = 5;

	public EffectList m_reactionEffects = new EffectList();

	public EffectList m_leaveEffects = new EffectList();

	private Rigidbody m_body;

	private ZNetView m_nview;

	private ZSyncAnimation m_zanimator;

	private Animator m_animator;

	private bool m_left;
}
