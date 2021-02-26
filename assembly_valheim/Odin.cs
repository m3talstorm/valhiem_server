using System;
using UnityEngine;

public class Odin : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
	}

	private void Update()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, this.m_despawnFarDistance);
		if (closestPlayer == null)
		{
			this.m_despawn.Create(base.transform.position, base.transform.rotation, null, 1f);
			this.m_nview.Destroy();
			ZLog.Log("No player in range, despawning");
			return;
		}
		Vector3 forward = closestPlayer.transform.position - base.transform.position;
		forward.y = 0f;
		forward.Normalize();
		base.transform.rotation = Quaternion.LookRotation(forward);
		if (Vector3.Distance(closestPlayer.transform.position, base.transform.position) < this.m_despawnCloseDistance)
		{
			this.m_despawn.Create(base.transform.position, base.transform.rotation, null, 1f);
			this.m_nview.Destroy();
			ZLog.Log("Player go too close,despawning");
			return;
		}
		this.m_time += Time.deltaTime;
		if (this.m_time > this.m_ttl)
		{
			this.m_despawn.Create(base.transform.position, base.transform.rotation, null, 1f);
			this.m_nview.Destroy();
			ZLog.Log("timeout " + this.m_time + " , despawning");
			return;
		}
	}

	public float m_despawnCloseDistance = 20f;

	public float m_despawnFarDistance = 50f;

	public EffectList m_despawn = new EffectList();

	public float m_ttl = 300f;

	private float m_time;

	private ZNetView m_nview;
}
