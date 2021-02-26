using System;
using System.Collections.Generic;
using UnityEngine;

public class RandomSpawn : MonoBehaviour
{
	public void Randomize()
	{
		bool spawned = UnityEngine.Random.Range(0f, 100f) <= this.m_chanceToSpawn;
		this.SetSpawned(spawned);
	}

	public void Reset()
	{
		this.SetSpawned(true);
	}

	private void SetSpawned(bool doSpawn)
	{
		if (!doSpawn)
		{
			base.gameObject.SetActive(false);
			using (List<ZNetView>.Enumerator enumerator = this.m_childNetViews.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ZNetView znetView = enumerator.Current;
					znetView.gameObject.SetActive(false);
				}
				goto IL_62;
			}
		}
		if (this.m_nview == null)
		{
			base.gameObject.SetActive(true);
		}
		IL_62:
		if (this.m_OffObject != null)
		{
			this.m_OffObject.SetActive(!doSpawn);
		}
	}

	public void Prepare()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_childNetViews = new List<ZNetView>();
		foreach (ZNetView znetView in base.gameObject.GetComponentsInChildren<ZNetView>(true))
		{
			if (Utils.IsEnabledInheirarcy(znetView.gameObject, base.gameObject))
			{
				this.m_childNetViews.Add(znetView);
			}
		}
	}

	public GameObject m_OffObject;

	[Range(0f, 100f)]
	public float m_chanceToSpawn = 50f;

	private List<ZNetView> m_childNetViews;

	private ZNetView m_nview;
}
