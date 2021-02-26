using System;
using UnityEngine;

public class InstantiatePrefab : MonoBehaviour
{
	private void Awake()
	{
		if (this.m_attach)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_prefab, base.transform).transform.SetAsFirstSibling();
			return;
		}
		UnityEngine.Object.Instantiate<GameObject>(this.m_prefab);
	}

	public GameObject m_prefab;

	public bool m_attach = true;

	public bool m_moveToTop;
}
