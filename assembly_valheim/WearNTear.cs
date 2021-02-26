using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WearNTear : MonoBehaviour, IDestructible
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_piece = base.GetComponent<Piece>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_nview.Register<HitData>("WNTDamage", new Action<long, HitData>(this.RPC_Damage));
		this.m_nview.Register<float>("WNTHealthChanged", new Action<long, float>(this.RPC_HealthChanged));
		if (this.m_autoCreateFragments)
		{
			this.m_nview.Register("WNTCreateFragments", new Action<long>(this.RPC_CreateFragments));
		}
		if (WearNTear.m_rayMask == 0)
		{
			WearNTear.m_rayMask = LayerMask.GetMask(new string[]
			{
				"piece",
				"Default",
				"static_solid",
				"Default_small",
				"terrain"
			});
		}
		WearNTear.m_allInstances.Add(this);
		this.m_myIndex = WearNTear.m_allInstances.Count - 1;
		this.m_createTime = Time.time;
		this.m_support = this.GetMaxSupport();
		if (WearNTear.m_randomInitialDamage)
		{
			float value = UnityEngine.Random.Range(0.1f * this.m_health, this.m_health * 0.6f);
			this.m_nview.GetZDO().Set("health", value);
		}
		this.UpdateVisual(false);
	}

	private void OnDestroy()
	{
		if (this.m_myIndex != -1)
		{
			WearNTear.m_allInstances[this.m_myIndex] = WearNTear.m_allInstances[WearNTear.m_allInstances.Count - 1];
			WearNTear.m_allInstances[this.m_myIndex].m_myIndex = this.m_myIndex;
			WearNTear.m_allInstances.RemoveAt(WearNTear.m_allInstances.Count - 1);
		}
	}

	public bool Repair()
	{
		if (this.m_nview.GetZDO().GetFloat("health", this.m_health) >= this.m_health)
		{
			return false;
		}
		this.m_nview.ClaimOwnership();
		this.m_nview.GetZDO().Set("health", this.m_health);
		this.m_nview.InvokeRPC(ZNetView.Everybody, "WNTHealthChanged", new object[]
		{
			this.m_health
		});
		return true;
	}

	private float GetSupport()
	{
		if (!this.m_nview.IsValid())
		{
			return this.GetMaxSupport();
		}
		if (!this.m_nview.HasOwner())
		{
			return this.GetMaxSupport();
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_support;
		}
		return this.m_nview.GetZDO().GetFloat("support", this.GetMaxSupport());
	}

	private float GetSupportColorValue()
	{
		float num = this.GetSupport();
		float num2;
		float num3;
		float num4;
		float num5;
		this.GetMaterialProperties(out num2, out num3, out num4, out num5);
		if (num >= num2)
		{
			return -1f;
		}
		num -= num3;
		return Mathf.Clamp01(num / (num2 * 0.5f - num3));
	}

	public void OnPlaced()
	{
		this.m_createTime = -1f;
	}

	private List<Renderer> GetHighlightRenderers()
	{
		MeshRenderer[] componentsInChildren = base.GetComponentsInChildren<MeshRenderer>(true);
		SkinnedMeshRenderer[] componentsInChildren2 = base.GetComponentsInChildren<SkinnedMeshRenderer>(true);
		List<Renderer> list = new List<Renderer>();
		list.AddRange(componentsInChildren);
		list.AddRange(componentsInChildren2);
		return list;
	}

	public void Highlight()
	{
		if (this.m_oldMaterials == null)
		{
			this.m_oldMaterials = new List<WearNTear.OldMeshData>();
			foreach (Renderer renderer in this.GetHighlightRenderers())
			{
				WearNTear.OldMeshData oldMeshData = default(WearNTear.OldMeshData);
				oldMeshData.m_materials = renderer.sharedMaterials;
				oldMeshData.m_color = new Color[oldMeshData.m_materials.Length];
				oldMeshData.m_emissiveColor = new Color[oldMeshData.m_materials.Length];
				for (int i = 0; i < oldMeshData.m_materials.Length; i++)
				{
					if (oldMeshData.m_materials[i].HasProperty("_Color"))
					{
						oldMeshData.m_color[i] = oldMeshData.m_materials[i].GetColor("_Color");
					}
					if (oldMeshData.m_materials[i].HasProperty("_EmissionColor"))
					{
						oldMeshData.m_emissiveColor[i] = oldMeshData.m_materials[i].GetColor("_EmissionColor");
					}
				}
				oldMeshData.m_renderer = renderer;
				this.m_oldMaterials.Add(oldMeshData);
			}
		}
		float supportColorValue = this.GetSupportColorValue();
		Color color = new Color(0.6f, 0.8f, 1f);
		if (supportColorValue >= 0f)
		{
			color = Color.Lerp(new Color(1f, 0f, 0f), new Color(0f, 1f, 0f), supportColorValue);
			float h;
			float s;
			float v;
			Color.RGBToHSV(color, out h, out s, out v);
			s = Mathf.Lerp(1f, 0.5f, supportColorValue);
			v = Mathf.Lerp(1.2f, 0.9f, supportColorValue);
			color = Color.HSVToRGB(h, s, v);
		}
		foreach (WearNTear.OldMeshData oldMeshData2 in this.m_oldMaterials)
		{
			if (oldMeshData2.m_renderer)
			{
				foreach (Material material in oldMeshData2.m_renderer.materials)
				{
					material.SetColor("_EmissionColor", color * 0.4f);
					material.color = color;
				}
			}
		}
		base.CancelInvoke("ResetHighlight");
		base.Invoke("ResetHighlight", 0.2f);
	}

	private void ResetHighlight()
	{
		if (this.m_oldMaterials != null)
		{
			foreach (WearNTear.OldMeshData oldMeshData in this.m_oldMaterials)
			{
				if (oldMeshData.m_renderer)
				{
					Material[] materials = oldMeshData.m_renderer.materials;
					if (materials.Length != 0)
					{
						if (materials[0] == oldMeshData.m_materials[0])
						{
							if (materials.Length == oldMeshData.m_color.Length)
							{
								for (int i = 0; i < materials.Length; i++)
								{
									if (materials[i].HasProperty("_Color"))
									{
										materials[i].SetColor("_Color", oldMeshData.m_color[i]);
									}
									if (materials[i].HasProperty("_EmissionColor"))
									{
										materials[i].SetColor("_EmissionColor", oldMeshData.m_emissiveColor[i]);
									}
								}
							}
						}
						else if (materials.Length == oldMeshData.m_materials.Length)
						{
							oldMeshData.m_renderer.materials = oldMeshData.m_materials;
						}
					}
				}
			}
			this.m_oldMaterials = null;
		}
	}

	private void SetupColliders()
	{
		this.m_colliders = base.GetComponentsInChildren<Collider>(true);
		this.m_bounds = new List<WearNTear.BoundData>();
		foreach (Collider collider in this.m_colliders)
		{
			if (!collider.isTrigger)
			{
				WearNTear.BoundData item = default(WearNTear.BoundData);
				if (collider is BoxCollider)
				{
					BoxCollider boxCollider = collider as BoxCollider;
					item.m_rot = boxCollider.transform.rotation;
					item.m_pos = boxCollider.transform.position + boxCollider.transform.TransformVector(boxCollider.center);
					item.m_size = new Vector3(boxCollider.transform.lossyScale.x * boxCollider.size.x, boxCollider.transform.lossyScale.y * boxCollider.size.y, boxCollider.transform.lossyScale.z * boxCollider.size.z);
				}
				else
				{
					item.m_rot = Quaternion.identity;
					item.m_pos = collider.bounds.center;
					item.m_size = collider.bounds.size;
				}
				item.m_size.x = item.m_size.x + 0.3f;
				item.m_size.y = item.m_size.y + 0.3f;
				item.m_size.z = item.m_size.z + 0.3f;
				item.m_size *= 0.5f;
				this.m_bounds.Add(item);
			}
		}
	}

	private bool ShouldUpdate()
	{
		return this.m_createTime < 0f || Time.time - this.m_createTime > 30f;
	}

	public void UpdateWear()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner() && this.ShouldUpdate())
		{
			if (ZNetScene.instance.OutsideActiveArea(base.transform.position))
			{
				this.m_support = this.GetMaxSupport();
				this.m_nview.GetZDO().Set("support", this.m_support);
				return;
			}
			float num = 0f;
			bool flag = this.HaveRoof();
			bool flag2 = EnvMan.instance.IsWet() && !flag;
			if (this.m_wet)
			{
				this.m_wet.SetActive(flag2);
			}
			if (this.m_noRoofWear && this.GetHealthPercentage() > 0.5f)
			{
				if (flag2 || this.IsUnderWater())
				{
					if (this.m_rainTimer == 0f)
					{
						this.m_rainTimer = Time.time;
					}
					else if (Time.time - this.m_rainTimer > 60f)
					{
						this.m_rainTimer = Time.time;
						num += 5f;
					}
				}
				else
				{
					this.m_rainTimer = 0f;
				}
			}
			if (this.m_noSupportWear)
			{
				this.UpdateSupport();
				if (!this.HaveSupport())
				{
					num = 100f;
				}
			}
			if (num > 0f && !this.CanBeRemoved())
			{
				num = 0f;
			}
			if (num > 0f)
			{
				float damage = num / 100f * this.m_health;
				this.ApplyDamage(damage);
			}
		}
		this.UpdateVisual(true);
	}

	private Vector3 GetCOM()
	{
		return base.transform.position + base.transform.rotation * this.m_comOffset;
	}

	private void UpdateSupport()
	{
		if (this.m_colliders == null)
		{
			this.SetupColliders();
		}
		float num;
		float num2;
		float num3;
		float num4;
		this.GetMaterialProperties(out num, out num2, out num3, out num4);
		WearNTear.m_tempSupportPoints.Clear();
		WearNTear.m_tempSupportPointValues.Clear();
		Vector3 com = this.GetCOM();
		float a = 0f;
		foreach (WearNTear.BoundData boundData in this.m_bounds)
		{
			int num5 = Physics.OverlapBoxNonAlloc(boundData.m_pos, boundData.m_size, WearNTear.m_tempColliders, boundData.m_rot, WearNTear.m_rayMask);
			for (int i = 0; i < num5; i++)
			{
				Collider collider = WearNTear.m_tempColliders[i];
				if (!this.m_colliders.Contains(collider) && !(collider.attachedRigidbody != null) && !collider.isTrigger)
				{
					WearNTear componentInParent = collider.GetComponentInParent<WearNTear>();
					if (componentInParent == null)
					{
						this.m_support = num;
						this.m_nview.GetZDO().Set("support", this.m_support);
						return;
					}
					if (componentInParent.m_supports)
					{
						float num6 = Vector3.Distance(com, componentInParent.transform.position) + 0.1f;
						float support = componentInParent.GetSupport();
						a = Mathf.Max(a, support - num3 * num6 * support);
						Vector3 vector = this.FindSupportPoint(com, componentInParent, collider);
						if (vector.y < com.y + 0.05f)
						{
							Vector3 normalized = (vector - com).normalized;
							if (normalized.y < 0f)
							{
								float t = Mathf.Acos(1f - Mathf.Abs(normalized.y)) / 1.5707964f;
								float num7 = Mathf.Lerp(num3, num4, t);
								float b = support - num7 * num6 * support;
								a = Mathf.Max(a, b);
							}
							float item = support - num4 * num6 * support;
							WearNTear.m_tempSupportPoints.Add(vector);
							WearNTear.m_tempSupportPointValues.Add(item);
						}
					}
				}
			}
		}
		if (WearNTear.m_tempSupportPoints.Count > 0 && WearNTear.m_tempSupportPoints.Count >= 2)
		{
			for (int j = 0; j < WearNTear.m_tempSupportPoints.Count; j++)
			{
				Vector3 from = WearNTear.m_tempSupportPoints[j] - com;
				from.y = 0f;
				for (int k = 0; k < WearNTear.m_tempSupportPoints.Count; k++)
				{
					if (j != k)
					{
						Vector3 to = WearNTear.m_tempSupportPoints[k] - com;
						to.y = 0f;
						if (Vector3.Angle(from, to) >= 100f)
						{
							float b2 = (WearNTear.m_tempSupportPointValues[j] + WearNTear.m_tempSupportPointValues[k]) * 0.5f;
							a = Mathf.Max(a, b2);
						}
					}
				}
			}
		}
		this.m_support = Mathf.Min(a, num);
		this.m_nview.GetZDO().Set("support", this.m_support);
	}

	private Vector3 FindSupportPoint(Vector3 com, WearNTear wnt, Collider otherCollider)
	{
		MeshCollider meshCollider = otherCollider as MeshCollider;
		if (!(meshCollider != null) || meshCollider.convex)
		{
			return otherCollider.ClosestPoint(com);
		}
		RaycastHit raycastHit;
		if (meshCollider.Raycast(new Ray(com, Vector3.down), out raycastHit, 10f))
		{
			return raycastHit.point;
		}
		return (com + wnt.GetCOM()) * 0.5f;
	}

	private bool HaveSupport()
	{
		return this.m_support >= this.GetMinSupport();
	}

	private bool IsUnderWater()
	{
		float waterLevel = WaterVolume.GetWaterLevel(base.transform.position, 1f);
		return base.transform.position.y < waterLevel;
	}

	private bool HaveRoof()
	{
		int num = Physics.SphereCastNonAlloc(base.transform.position, 0.1f, Vector3.up, WearNTear.m_raycastHits, 100f, WearNTear.m_rayMask);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = WearNTear.m_raycastHits[i];
			if (!raycastHit.collider.gameObject.CompareTag("leaky"))
			{
				return true;
			}
		}
		return false;
	}

	private void RPC_HealthChanged(long peer, float health)
	{
		float health2 = health / this.m_health;
		this.SetHealthVisual(health2, true);
	}

	private void UpdateVisual(bool triggerEffects)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.SetHealthVisual(this.GetHealthPercentage(), triggerEffects);
	}

	private void SetHealthVisual(float health, bool triggerEffects)
	{
		if (this.m_worn == null && this.m_broken == null && this.m_new == null)
		{
			return;
		}
		if (health > 0.75f)
		{
			if (this.m_worn != this.m_new)
			{
				this.m_worn.SetActive(false);
			}
			if (this.m_broken != this.m_new)
			{
				this.m_broken.SetActive(false);
			}
			this.m_new.SetActive(true);
			return;
		}
		if (health > 0.25f)
		{
			if (triggerEffects && !this.m_worn.activeSelf)
			{
				this.m_switchEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f);
			}
			if (this.m_new != this.m_worn)
			{
				this.m_new.SetActive(false);
			}
			if (this.m_broken != this.m_worn)
			{
				this.m_broken.SetActive(false);
			}
			this.m_worn.SetActive(true);
			return;
		}
		if (triggerEffects && !this.m_broken.activeSelf)
		{
			this.m_switchEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f);
		}
		if (this.m_new != this.m_broken)
		{
			this.m_new.SetActive(false);
		}
		if (this.m_worn != this.m_broken)
		{
			this.m_worn.SetActive(false);
		}
		this.m_broken.SetActive(true);
	}

	public float GetHealthPercentage()
	{
		if (!this.m_nview.IsValid())
		{
			return 1f;
		}
		return Mathf.Clamp01(this.m_nview.GetZDO().GetFloat("health", this.m_health) / this.m_health);
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Default;
	}

	public void Damage(HitData hit)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("WNTDamage", new object[]
		{
			hit
		});
	}

	private bool CanBeRemoved()
	{
		return !this.m_piece || this.m_piece.CanBeRemoved();
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_nview.GetZDO().GetFloat("health", this.m_health) <= 0f)
		{
			return;
		}
		HitData.DamageModifier type;
		hit.ApplyResistance(this.m_damages, out type);
		float totalDamage = hit.GetTotalDamage();
		if (this.m_piece && this.m_piece.IsPlacedByPlayer())
		{
			PrivateArea.CheckInPrivateArea(base.transform.position, true);
		}
		DamageText.instance.ShowText(type, hit.m_point, totalDamage, false);
		if (totalDamage <= 0f)
		{
			return;
		}
		this.ApplyDamage(totalDamage);
		this.m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform, 1f);
		if (this.m_hitNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(hit.m_point, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(this.m_hitNoise);
			}
		}
		if (this.m_onDamaged != null)
		{
			this.m_onDamaged();
		}
	}

	public bool ApplyDamage(float damage)
	{
		float num = this.m_nview.GetZDO().GetFloat("health", this.m_health);
		if (num <= 0f)
		{
			return false;
		}
		num -= damage;
		this.m_nview.GetZDO().Set("health", num);
		if (num <= 0f)
		{
			this.Destroy();
		}
		else
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "WNTHealthChanged", new object[]
			{
				num
			});
		}
		return true;
	}

	public void Destroy()
	{
		this.m_nview.GetZDO().Set("health", 0f);
		if (this.m_piece)
		{
			this.m_piece.DropResources();
		}
		if (this.m_onDestroyed != null)
		{
			this.m_onDestroyed();
		}
		if (this.m_destroyNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
			if (closestPlayer)
			{
				closestPlayer.AddNoise(this.m_destroyNoise);
			}
		}
		this.m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f);
		if (this.m_autoCreateFragments)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "WNTCreateFragments", Array.Empty<object>());
		}
		ZNetScene.instance.Destroy(base.gameObject);
	}

	private void RPC_CreateFragments(long peer)
	{
		this.ResetHighlight();
		if (this.m_fragmentRoots != null && this.m_fragmentRoots.Length != 0)
		{
			foreach (GameObject gameObject in this.m_fragmentRoots)
			{
				gameObject.SetActive(true);
				Destructible.CreateFragments(gameObject, false);
			}
			return;
		}
		Destructible.CreateFragments(base.gameObject, true);
	}

	private float GetMaxSupport()
	{
		float result;
		float num;
		float num2;
		float num3;
		this.GetMaterialProperties(out result, out num, out num2, out num3);
		return result;
	}

	private float GetMinSupport()
	{
		float num;
		float result;
		float num2;
		float num3;
		this.GetMaterialProperties(out num, out result, out num2, out num3);
		return result;
	}

	private void GetMaterialProperties(out float maxSupport, out float minSupport, out float horizontalLoss, out float verticalLoss)
	{
		switch (this.m_materialType)
		{
		case WearNTear.MaterialType.Wood:
			maxSupport = 100f;
			minSupport = 10f;
			verticalLoss = 0.125f;
			horizontalLoss = 0.2f;
			return;
		case WearNTear.MaterialType.Stone:
			maxSupport = 1000f;
			minSupport = 100f;
			verticalLoss = 0.125f;
			horizontalLoss = 1f;
			return;
		case WearNTear.MaterialType.Iron:
			maxSupport = 1500f;
			minSupport = 20f;
			verticalLoss = 0.07692308f;
			horizontalLoss = 0.07692308f;
			return;
		case WearNTear.MaterialType.HardWood:
			maxSupport = 140f;
			minSupport = 10f;
			verticalLoss = 0.1f;
			horizontalLoss = 0.16666667f;
			return;
		default:
			maxSupport = 0f;
			minSupport = 0f;
			verticalLoss = 0f;
			horizontalLoss = 0f;
			return;
		}
	}

	public static List<WearNTear> GetAllInstaces()
	{
		return WearNTear.m_allInstances;
	}

	public static bool m_randomInitialDamage = false;

	public Action m_onDestroyed;

	public Action m_onDamaged;

	[Header("Wear")]
	public GameObject m_new;

	public GameObject m_worn;

	public GameObject m_broken;

	public GameObject m_wet;

	public bool m_noRoofWear = true;

	public bool m_noSupportWear = true;

	public WearNTear.MaterialType m_materialType;

	public bool m_supports = true;

	public Vector3 m_comOffset = Vector3.zero;

	[Header("Destruction")]
	public float m_health = 100f;

	public HitData.DamageModifiers m_damages;

	public float m_minDamageTreshold;

	public float m_hitNoise;

	public float m_destroyNoise;

	[Header("Effects")]
	public EffectList m_destroyedEffect = new EffectList();

	public EffectList m_hitEffect = new EffectList();

	public EffectList m_switchEffect = new EffectList();

	public bool m_autoCreateFragments = true;

	public GameObject[] m_fragmentRoots;

	private const float m_noFireDrain = 0.0049603176f;

	private const float m_noSupportDrain = 25f;

	private const float m_rainDamageTime = 60f;

	private const float m_rainDamage = 5f;

	private const float m_comTestWidth = 0.2f;

	private const float m_comMinAngle = 100f;

	private const float m_minFireDistance = 20f;

	private const int m_wearUpdateIntervalMinutes = 60;

	private const float m_privateAreaModifier = 0.5f;

	private static RaycastHit[] m_raycastHits = new RaycastHit[128];

	private static Collider[] m_tempColliders = new Collider[128];

	private static int m_rayMask = 0;

	private static List<WearNTear> m_allInstances = new List<WearNTear>();

	private static List<Vector3> m_tempSupportPoints = new List<Vector3>();

	private static List<float> m_tempSupportPointValues = new List<float>();

	private ZNetView m_nview;

	private Collider[] m_colliders;

	private float m_support = 1f;

	private float m_createTime;

	private int m_myIndex = -1;

	private float m_rainTimer;

	private Piece m_piece;

	private List<WearNTear.BoundData> m_bounds;

	private List<WearNTear.OldMeshData> m_oldMaterials;

	public enum MaterialType
	{
		Wood,
		Stone,
		Iron,
		HardWood
	}

	private struct BoundData
	{
		public Vector3 m_pos;

		public Quaternion m_rot;

		public Vector3 m_size;
	}

	private struct OldMeshData
	{
		public Renderer m_renderer;

		public Material[] m_materials;

		public Color[] m_color;

		public Color[] m_emissiveColor;
	}
}
