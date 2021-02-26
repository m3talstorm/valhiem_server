using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowUpdater : MonoBehaviour
{
	private void Awake()
	{
		base.StartCoroutine("UpdateLoop");
	}

	private IEnumerator UpdateLoop()
	{
		for (;;)
		{
			List<SlowUpdate> instances = SlowUpdate.GetAllInstaces();
			int index = 0;
			while (index < instances.Count)
			{
				int num = 0;
				while (num < 100 && instances.Count != 0 && index < instances.Count)
				{
					instances[index].SUpdate();
					int num2 = index + 1;
					index = num2;
					num++;
				}
				yield return null;
			}
			yield return new WaitForSeconds(0.1f);
			instances = null;
		}
		yield break;
	}

	private const int m_updatesPerFrame = 100;
}
