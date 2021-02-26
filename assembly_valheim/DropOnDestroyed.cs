using System;
using System.Collections.Generic;
using UnityEngine;

public class DropOnDestroyed : MonoBehaviour
{
	private void Awake()
	{
		IDestructible component = base.GetComponent<IDestructible>();
		Destructible destructible = component as Destructible;
		if (destructible)
		{
			Destructible destructible2 = destructible;
			destructible2.m_onDestroyed = (Action)Delegate.Combine(destructible2.m_onDestroyed, new Action(this.OnDestroyed));
		}
		WearNTear wearNTear = component as WearNTear;
		if (wearNTear)
		{
			WearNTear wearNTear2 = wearNTear;
			wearNTear2.m_onDestroyed = (Action)Delegate.Combine(wearNTear2.m_onDestroyed, new Action(this.OnDestroyed));
		}
	}

	private void OnDestroyed()
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		Vector3 position = base.transform.position;
		if (position.y < groundHeight)
		{
			position.y = groundHeight + 0.1f;
		}
		List<GameObject> dropList = this.m_dropWhenDestroyed.GetDropList();
		for (int i = 0; i < dropList.Count; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.5f;
			Vector3 position2 = position + Vector3.up * this.m_spawnYOffset + new Vector3(vector.x, this.m_spawnYStep * (float)i, vector.y);
			Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
			UnityEngine.Object.Instantiate<GameObject>(dropList[i], position2, rotation);
		}
	}

	[Header("Drops")]
	public DropTable m_dropWhenDestroyed = new DropTable();

	public float m_spawnYOffset = 0.5f;

	public float m_spawnYStep = 0.3f;
}
