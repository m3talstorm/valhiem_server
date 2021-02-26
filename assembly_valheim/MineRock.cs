using System;
using UnityEngine;

public class MineRock : MonoBehaviour, IDestructible, Hoverable
{
	private void Start()
	{
		this.m_hitAreas = ((this.m_areaRoot != null) ? this.m_areaRoot.GetComponentsInChildren<Collider>() : base.gameObject.GetComponentsInChildren<Collider>());
		if (this.m_baseModel)
		{
			this.m_areaMeshes = new MeshRenderer[this.m_hitAreas.Length][];
			for (int i = 0; i < this.m_hitAreas.Length; i++)
			{
				this.m_areaMeshes[i] = this.m_hitAreas[i].GetComponents<MeshRenderer>();
			}
		}
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview && this.m_nview.GetZDO() != null)
		{
			this.m_nview.Register<HitData, int>("Hit", new Action<long, HitData, int>(this.RPC_Hit));
			this.m_nview.Register<int>("Hide", new Action<long, int>(this.RPC_Hide));
		}
		base.InvokeRepeating("UpdateVisability", UnityEngine.Random.Range(1f, 2f), 10f);
	}

	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_name);
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	private void UpdateVisability()
	{
		bool flag = false;
		for (int i = 0; i < this.m_hitAreas.Length; i++)
		{
			Collider collider = this.m_hitAreas[i];
			if (collider)
			{
				string name = "Health" + i.ToString();
				bool flag2 = this.m_nview.GetZDO().GetFloat(name, this.m_health) > 0f;
				collider.gameObject.SetActive(flag2);
				if (!flag2)
				{
					flag = true;
				}
			}
		}
		if (this.m_baseModel)
		{
			this.m_baseModel.SetActive(!flag);
			foreach (MeshRenderer[] array in this.m_areaMeshes)
			{
				for (int k = 0; k < array.Length; k++)
				{
					array[k].enabled = flag;
				}
			}
		}
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Default;
	}

	public void Damage(HitData hit)
	{
		if (hit.m_hitCollider == null)
		{
			ZLog.Log("Minerock hit has no collider");
			return;
		}
		int areaIndex = this.GetAreaIndex(hit.m_hitCollider);
		if (areaIndex == -1)
		{
			ZLog.Log("Invalid hit area on " + base.gameObject.name);
			return;
		}
		ZLog.Log("Hit mine rock area " + areaIndex);
		this.m_nview.InvokeRPC("Hit", new object[]
		{
			hit,
			areaIndex
		});
	}

	private void RPC_Hit(long sender, HitData hit, int hitAreaIndex)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		Collider hitArea = this.GetHitArea(hitAreaIndex);
		if (hitArea == null)
		{
			ZLog.Log("Missing hit area " + hitAreaIndex);
			return;
		}
		string name = "Health" + hitAreaIndex.ToString();
		float num = this.m_nview.GetZDO().GetFloat(name, this.m_health);
		if (num <= 0f)
		{
			ZLog.Log("Already destroyed");
			return;
		}
		HitData.DamageModifier type;
		hit.ApplyResistance(this.m_damageModifiers, out type);
		float totalDamage = hit.GetTotalDamage();
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
		this.m_nview.GetZDO().Set(name, num);
		this.m_hitEffect.Create(hit.m_point, Quaternion.identity, null, 1f);
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
		if (closestPlayer)
		{
			closestPlayer.AddNoise(100f);
		}
		if (this.m_onHit != null)
		{
			this.m_onHit();
		}
		if (num <= 0f)
		{
			this.m_destroyedEffect.Create(hitArea.bounds.center, Quaternion.identity, null, 1f);
			this.m_nview.InvokeRPC(ZNetView.Everybody, "Hide", new object[]
			{
				hitAreaIndex
			});
			foreach (GameObject original in this.m_dropItems.GetDropList())
			{
				Vector3 position = hit.m_point - hit.m_dir * 0.2f + UnityEngine.Random.insideUnitSphere * 0.3f;
				UnityEngine.Object.Instantiate<GameObject>(original, position, Quaternion.identity);
			}
			if (this.m_removeWhenDestroyed && this.AllDestroyed())
			{
				this.m_nview.Destroy();
			}
		}
	}

	private bool AllDestroyed()
	{
		for (int i = 0; i < this.m_hitAreas.Length; i++)
		{
			string name = "Health" + i.ToString();
			if (this.m_nview.GetZDO().GetFloat(name, this.m_health) > 0f)
			{
				return false;
			}
		}
		return true;
	}

	private void RPC_Hide(long sender, int index)
	{
		Collider hitArea = this.GetHitArea(index);
		if (hitArea)
		{
			hitArea.gameObject.SetActive(false);
		}
		if (this.m_baseModel && this.m_baseModel.activeSelf)
		{
			this.m_baseModel.SetActive(false);
			foreach (MeshRenderer[] array in this.m_areaMeshes)
			{
				for (int j = 0; j < array.Length; j++)
				{
					array[j].enabled = true;
				}
			}
		}
	}

	private int GetAreaIndex(Collider area)
	{
		for (int i = 0; i < this.m_hitAreas.Length; i++)
		{
			if (this.m_hitAreas[i] == area)
			{
				return i;
			}
		}
		return -1;
	}

	private Collider GetHitArea(int index)
	{
		if (index < 0 || index >= this.m_hitAreas.Length)
		{
			return null;
		}
		return this.m_hitAreas[index];
	}

	public string m_name = "";

	public float m_health = 2f;

	public bool m_removeWhenDestroyed = true;

	public HitData.DamageModifiers m_damageModifiers;

	public int m_minToolTier;

	public GameObject m_areaRoot;

	public GameObject m_baseModel;

	public EffectList m_destroyedEffect = new EffectList();

	public EffectList m_hitEffect = new EffectList();

	public DropTable m_dropItems;

	public Action m_onHit;

	private Collider[] m_hitAreas;

	private MeshRenderer[][] m_areaMeshes;

	private ZNetView m_nview;
}
