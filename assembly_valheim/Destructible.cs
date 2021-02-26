using System;
using UnityEngine;

public class Destructible : MonoBehaviour, IDestructible
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		if (this.m_nview && this.m_nview.GetZDO() != null)
		{
			this.m_nview.Register<HitData>("Damage", new Action<long, HitData>(this.RPC_Damage));
			if (this.m_autoCreateFragments)
			{
				this.m_nview.Register("CreateFragments", new Action<long>(this.RPC_CreateFragments));
			}
			if (this.m_ttl > 0f)
			{
				base.InvokeRepeating("DestroyNow", this.m_ttl, 1f);
			}
		}
	}

	private void Start()
	{
		this.m_firstFrame = false;
	}

	public GameObject GetParentObject()
	{
		return null;
	}

	public DestructibleType GetDestructibleType()
	{
		return this.m_destructibleType;
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
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_destroyed)
		{
			return;
		}
		float num = this.m_nview.GetZDO().GetFloat("health", this.m_health);
		HitData.DamageModifier type;
		hit.ApplyResistance(this.m_damages, out type);
		float totalDamage = hit.GetTotalDamage();
		if (this.m_body)
		{
			this.m_body.AddForceAtPosition(hit.m_dir * hit.m_pushForce, hit.m_point, ForceMode.Impulse);
		}
		if (hit.m_toolTier < this.m_minToolTier)
		{
			DamageText.instance.ShowText(DamageText.TextType.TooHard, hit.m_point, 0f, false);
			return;
		}
		DamageText.instance.ShowText(type, hit.m_point, totalDamage, false);
		if (totalDamage <= 0f)
		{
			return;
		}
		num -= totalDamage;
		this.m_nview.GetZDO().Set("health", num);
		this.m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform, 1f);
		if (this.m_onDamaged != null)
		{
			this.m_onDamaged();
		}
		if (this.m_hitNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(hit.m_point, 10f);
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

	private void DestroyNow()
	{
		if (this.m_nview.IsValid() && this.m_nview.IsOwner())
		{
			this.Destroy();
		}
	}

	public void Destroy()
	{
		this.CreateDestructionEffects();
		if (this.m_destroyNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(this.m_destroyNoise);
			}
		}
		if (this.m_spawnWhenDestroyed)
		{
			ZNetView component = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnWhenDestroyed, base.transform.position, base.transform.rotation).GetComponent<ZNetView>();
			component.SetLocalScale(base.transform.localScale);
			component.GetZDO().SetPGWVersion(this.m_nview.GetZDO().GetPGWVersion());
		}
		if (this.m_onDestroyed != null)
		{
			this.m_onDestroyed();
		}
		ZNetScene.instance.Destroy(base.gameObject);
		this.m_destroyed = true;
	}

	private void CreateDestructionEffects()
	{
		this.m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f);
		if (this.m_autoCreateFragments)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "CreateFragments", Array.Empty<object>());
		}
	}

	private void RPC_CreateFragments(long peer)
	{
		Destructible.CreateFragments(base.gameObject, true);
	}

	public static void CreateFragments(GameObject rootObject, bool visibleOnly = true)
	{
		MeshRenderer[] componentsInChildren = rootObject.GetComponentsInChildren<MeshRenderer>(true);
		int layer = LayerMask.NameToLayer("effect");
		foreach (MeshRenderer meshRenderer in componentsInChildren)
		{
			if (meshRenderer.gameObject.activeInHierarchy && (!visibleOnly || meshRenderer.isVisible))
			{
				MeshFilter component = meshRenderer.gameObject.GetComponent<MeshFilter>();
				if (!(component == null))
				{
					if (component.sharedMesh == null)
					{
						ZLog.Log("Meshfilter missing mesh " + component.gameObject.name);
					}
					else
					{
						GameObject gameObject = new GameObject();
						gameObject.layer = layer;
						gameObject.transform.position = component.gameObject.transform.position;
						gameObject.transform.rotation = component.gameObject.transform.rotation;
						gameObject.transform.localScale = component.gameObject.transform.lossyScale * 0.9f;
						gameObject.AddComponent<MeshFilter>().sharedMesh = component.sharedMesh;
						MeshRenderer meshRenderer2 = gameObject.AddComponent<MeshRenderer>();
						meshRenderer2.sharedMaterials = meshRenderer.sharedMaterials;
						meshRenderer2.material.SetFloat("_RippleDistance", 0f);
						meshRenderer2.material.SetFloat("_ValueNoise", 0f);
						Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
						gameObject.AddComponent<BoxCollider>();
						rigidbody.AddForce(UnityEngine.Random.onUnitSphere * 2f, ForceMode.VelocityChange);
						gameObject.AddComponent<TimedDestruction>().Trigger((float)UnityEngine.Random.Range(2, 4));
					}
				}
			}
		}
	}

	public Action m_onDestroyed;

	public Action m_onDamaged;

	[Header("Destruction")]
	public DestructibleType m_destructibleType = DestructibleType.Default;

	public float m_health = 1f;

	public HitData.DamageModifiers m_damages;

	public float m_minDamageTreshold;

	public int m_minToolTier;

	public float m_hitNoise;

	public float m_destroyNoise;

	public float m_ttl;

	public GameObject m_spawnWhenDestroyed;

	[Header("Effects")]
	public EffectList m_destroyedEffect = new EffectList();

	public EffectList m_hitEffect = new EffectList();

	public bool m_autoCreateFragments;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private bool m_firstFrame = true;

	private bool m_destroyed;
}
