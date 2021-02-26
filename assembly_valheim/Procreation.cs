using System;
using UnityEngine;

public class Procreation : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_baseAI = base.GetComponent<BaseAI>();
		this.m_character = base.GetComponent<Character>();
		this.m_tameable = base.GetComponent<Tameable>();
		base.InvokeRepeating("Procreate", UnityEngine.Random.Range(this.m_updateInterval, this.m_updateInterval + this.m_updateInterval * 0.5f), this.m_updateInterval);
	}

	private void Procreate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_character.IsTamed())
		{
			return;
		}
		if (this.m_offspringPrefab == null)
		{
			string prefabName = ZNetView.GetPrefabName(this.m_offspring);
			this.m_offspringPrefab = ZNetScene.instance.GetPrefab(prefabName);
			int prefab = this.m_nview.GetZDO().GetPrefab();
			this.m_myPrefab = ZNetScene.instance.GetPrefab(prefab);
		}
		if (this.IsPregnant())
		{
			if (this.IsDue())
			{
				this.ResetPregnancy();
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_offspringPrefab, base.transform.position - base.transform.forward * this.m_spawnOffset, Quaternion.LookRotation(-base.transform.forward, Vector3.up));
				Character component = gameObject.GetComponent<Character>();
				if (component)
				{
					component.SetTamed(this.m_character.IsTamed());
					component.SetLevel(Mathf.Max(this.m_minOffspringLevel, this.m_character.GetLevel()));
				}
				this.m_birthEffects.Create(gameObject.transform.position, Quaternion.identity, null, 1f);
				return;
			}
		}
		else
		{
			if (UnityEngine.Random.value <= this.m_pregnancyChance)
			{
				return;
			}
			if (this.m_baseAI.IsAlerted())
			{
				return;
			}
			if (this.m_tameable.IsHungry())
			{
				return;
			}
			int nrOfInstances = SpawnSystem.GetNrOfInstances(this.m_myPrefab, base.transform.position, this.m_totalCheckRange, false, false);
			int nrOfInstances2 = SpawnSystem.GetNrOfInstances(this.m_offspringPrefab, base.transform.position, this.m_totalCheckRange, false, false);
			if (nrOfInstances + nrOfInstances2 >= this.m_maxCreatures)
			{
				return;
			}
			if (SpawnSystem.GetNrOfInstances(this.m_myPrefab, base.transform.position, this.m_partnerCheckRange, false, true) < 2)
			{
				return;
			}
			this.m_loveEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
			int num = this.m_nview.GetZDO().GetInt("lovePoints", 0);
			num++;
			this.m_nview.GetZDO().Set("lovePoints", num);
			if (num >= this.m_requiredLovePoints)
			{
				this.m_nview.GetZDO().Set("lovePoints", 0);
				this.MakePregnant();
			}
		}
	}

	public bool ReadyForProcreation()
	{
		return this.m_character.IsTamed() && !this.IsPregnant() && !this.m_tameable.IsHungry();
	}

	private void MakePregnant()
	{
		this.m_nview.GetZDO().Set("pregnant", ZNet.instance.GetTime().Ticks);
	}

	private void ResetPregnancy()
	{
		this.m_nview.GetZDO().Set("pregnant", 0L);
	}

	private bool IsDue()
	{
		long @long = this.m_nview.GetZDO().GetLong("pregnant", 0L);
		if (@long == 0L)
		{
			return false;
		}
		DateTime d = new DateTime(@long);
		return (ZNet.instance.GetTime() - d).TotalSeconds > (double)this.m_pregnancyDuration;
	}

	public bool IsPregnant()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetLong("pregnant", 0L) != 0L;
	}

	public float m_updateInterval = 10f;

	public float m_totalCheckRange = 10f;

	public int m_maxCreatures = 4;

	public float m_partnerCheckRange = 3f;

	public float m_pregnancyChance = 0.5f;

	public float m_pregnancyDuration = 10f;

	public int m_requiredLovePoints = 4;

	public GameObject m_offspring;

	public int m_minOffspringLevel;

	public float m_spawnOffset = 2f;

	public EffectList m_birthEffects = new EffectList();

	public EffectList m_loveEffects = new EffectList();

	private GameObject m_myPrefab;

	private GameObject m_offspringPrefab;

	private ZNetView m_nview;

	private BaseAI m_baseAI;

	private Character m_character;

	private Tameable m_tameable;
}
