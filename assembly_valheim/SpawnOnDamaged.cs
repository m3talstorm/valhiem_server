using System;
using UnityEngine;

public class SpawnOnDamaged : MonoBehaviour
{
	private void Start()
	{
		WearNTear component = base.GetComponent<WearNTear>();
		if (component)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDamaged = (Action)Delegate.Combine(wearNTear.m_onDamaged, new Action(this.OnDamaged));
		}
		Destructible component2 = base.GetComponent<Destructible>();
		if (component2)
		{
			Destructible destructible = component2;
			destructible.m_onDamaged = (Action)Delegate.Combine(destructible.m_onDamaged, new Action(this.OnDamaged));
		}
	}

	private void OnDamaged()
	{
		if (this.m_spawnOnDamage)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_spawnOnDamage, base.transform.position, Quaternion.identity);
		}
	}

	public GameObject m_spawnOnDamage;
}
