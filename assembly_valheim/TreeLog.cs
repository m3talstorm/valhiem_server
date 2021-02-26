using System;
using System.Collections.Generic;
using UnityEngine;

public class TreeLog : MonoBehaviour, IDestructible
{
	private void Awake()
	{
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_body.maxDepenetrationVelocity = 1f;
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_nview.Register<HitData>("Damage", new Action<long, HitData>(this.RPC_Damage));
		if (this.m_nview.IsOwner())
		{
			float @float = this.m_nview.GetZDO().GetFloat("health", -1f);
			if (@float == -1f)
			{
				this.m_nview.GetZDO().Set("health", this.m_health);
			}
			else if (@float <= 0f)
			{
				this.m_nview.Destroy();
			}
		}
		base.Invoke("EnableDamage", 0.2f);
	}

	private void EnableDamage()
	{
		this.m_firstFrame = false;
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Tree;
	}

	public void Damage(HitData hit)
	{
		if (this.m_firstFrame)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("Damage", new object[]
		{
			hit
		});
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		float num = this.m_nview.GetZDO().GetFloat("health", 0f);
		if (num <= 0f)
		{
			return;
		}
		HitData.DamageModifier type;
		hit.ApplyResistance(this.m_damages, out type);
		float totalDamage = hit.GetTotalDamage();
		if (hit.m_toolTier < this.m_minToolTier)
		{
			DamageText.instance.ShowText(DamageText.TextType.TooHard, hit.m_point, 0f, false);
			return;
		}
		if (this.m_body)
		{
			this.m_body.AddForceAtPosition(hit.m_dir * hit.m_pushForce * 2f, hit.m_point, ForceMode.Impulse);
		}
		DamageText.instance.ShowText(type, hit.m_point, totalDamage, false);
		if (totalDamage <= 0f)
		{
			return;
		}
		num -= totalDamage;
		if (num < 0f)
		{
			num = 0f;
		}
		this.m_nview.GetZDO().Set("health", num);
		this.m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform, 1f);
		if (this.m_hitNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(this.m_hitNoise);
			}
		}
		if (num <= 0f)
		{
			this.Destroy();
		}
	}

	private void Destroy()
	{
		ZNetScene.instance.Destroy(base.gameObject);
		this.m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f);
		List<GameObject> dropList = this.m_dropWhenDestroyed.GetDropList();
		for (int i = 0; i < dropList.Count; i++)
		{
			Vector3 position = base.transform.position + base.transform.up * UnityEngine.Random.Range(-this.m_spawnDistance, this.m_spawnDistance) + Vector3.up * 0.3f * (float)i;
			Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
			UnityEngine.Object.Instantiate<GameObject>(dropList[i], position, rotation);
		}
		if (this.m_subLogPrefab != null)
		{
			foreach (Transform transform in this.m_subLogPoints)
			{
				UnityEngine.Object.Instantiate<GameObject>(this.m_subLogPrefab, transform.position, base.transform.rotation).GetComponent<ZNetView>().SetLocalScale(base.transform.localScale);
			}
		}
	}

	public float m_health = 60f;

	public HitData.DamageModifiers m_damages;

	public int m_minToolTier;

	public EffectList m_destroyedEffect = new EffectList();

	public EffectList m_hitEffect = new EffectList();

	public DropTable m_dropWhenDestroyed = new DropTable();

	public GameObject m_subLogPrefab;

	public Transform[] m_subLogPoints = new Transform[0];

	public float m_spawnDistance = 2f;

	public float m_hitNoise = 100f;

	private Rigidbody m_body;

	private ZNetView m_nview;

	private bool m_firstFrame = true;
}
