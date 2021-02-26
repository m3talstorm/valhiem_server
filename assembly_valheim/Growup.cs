using System;
using UnityEngine;

public class Growup : MonoBehaviour
{
	private void Start()
	{
		this.m_baseAI = base.GetComponent<BaseAI>();
		this.m_nview = base.GetComponent<ZNetView>();
		base.InvokeRepeating("GrowUpdate", UnityEngine.Random.Range(10f, 15f), 10f);
	}

	private void GrowUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_baseAI.GetTimeSinceSpawned().TotalSeconds > (double)this.m_growTime)
		{
			Character component = base.GetComponent<Character>();
			Character component2 = UnityEngine.Object.Instantiate<GameObject>(this.m_grownPrefab, base.transform.position, base.transform.rotation).GetComponent<Character>();
			if (component && component2)
			{
				component2.SetTamed(component.IsTamed());
				component2.SetLevel(component.GetLevel());
			}
			this.m_nview.Destroy();
		}
	}

	public float m_growTime = 60f;

	public GameObject m_grownPrefab;

	private BaseAI m_baseAI;

	private ZNetView m_nview;
}
