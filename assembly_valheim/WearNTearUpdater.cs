using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WearNTearUpdater : MonoBehaviour
{
	private void Awake()
	{
		base.StartCoroutine("UpdateWear");
	}

	private IEnumerator UpdateWear()
	{
		for (;;)
		{
			List<WearNTear> instances = WearNTear.GetAllInstaces();
			int index = 0;
			while (index < instances.Count)
			{
				int num = 0;
				while (num < 50 && instances.Count != 0 && index < instances.Count)
				{
					instances[index].UpdateWear();
					int num2 = index + 1;
					index = num2;
					num++;
				}
				yield return null;
			}
			yield return new WaitForSeconds(0.5f);
			instances = null;
		}
		yield break;
	}

	private const int m_updatesPerFrame = 50;
}
