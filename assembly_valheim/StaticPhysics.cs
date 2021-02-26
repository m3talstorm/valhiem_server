using System;
using UnityEngine;

public class StaticPhysics : SlowUpdate
{
	public override void Awake()
	{
		base.Awake();
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_createTime = Time.time;
	}

	private bool ShouldUpdate()
	{
		return Time.time - this.m_createTime > 20f;
	}

	public override void SUpdate()
	{
		if (!this.ShouldUpdate() || ZNetScene.instance.OutsideActiveArea(base.transform.position) || this.m_falling)
		{
			return;
		}
		if (this.m_fall)
		{
			this.CheckFall();
		}
		if (this.m_pushUp)
		{
			this.PushUp();
		}
	}

	private void CheckFall()
	{
		float fallHeight = this.GetFallHeight();
		if (base.transform.position.y > fallHeight + 0.05f)
		{
			this.Fall();
		}
	}

	private float GetFallHeight()
	{
		if (this.m_checkSolids)
		{
			float result;
			if (ZoneSystem.instance.GetSolidHeight(base.transform.position, this.m_fallCheckRadius, out result, base.transform))
			{
				return result;
			}
			return base.transform.position.y;
		}
		else
		{
			float result2;
			if (ZoneSystem.instance.GetGroundHeight(base.transform.position, out result2))
			{
				return result2;
			}
			return base.transform.position.y;
		}
	}

	private void Fall()
	{
		this.m_falling = true;
		base.gameObject.isStatic = false;
		base.InvokeRepeating("FallUpdate", 0.05f, 0.05f);
	}

	private void FallUpdate()
	{
		float fallHeight = this.GetFallHeight();
		Vector3 position = base.transform.position;
		position.y -= 0.2f;
		if (position.y <= fallHeight)
		{
			position.y = fallHeight;
			this.StopFalling();
		}
		base.transform.position = position;
		if (this.m_nview.IsValid() && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().SetPosition(base.transform.position);
		}
	}

	private void StopFalling()
	{
		base.gameObject.isStatic = true;
		this.m_falling = false;
		base.CancelInvoke("FallUpdate");
	}

	private void PushUp()
	{
		float num;
		if (ZoneSystem.instance.GetGroundHeight(base.transform.position, out num) && base.transform.position.y < num - 0.05f)
		{
			base.gameObject.isStatic = false;
			Vector3 position = base.transform.position;
			position.y = num;
			base.transform.position = position;
			base.gameObject.isStatic = true;
			if (this.m_nview.IsValid() && this.m_nview.IsOwner())
			{
				this.m_nview.GetZDO().SetPosition(base.transform.position);
			}
		}
	}

	public bool m_pushUp = true;

	public bool m_fall = true;

	public bool m_checkSolids;

	public float m_fallCheckRadius;

	private ZNetView m_nview;

	private const float m_fallSpeed = 4f;

	private const float m_fallStep = 0.05f;

	private float m_createTime;

	private bool m_falling;
}
