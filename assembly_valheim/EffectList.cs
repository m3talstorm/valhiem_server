using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EffectList
{
	public GameObject[] Create(Vector3 pos, Quaternion rot, Transform parent = null, float scale = 1f)
	{
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < this.m_effectPrefabs.Length; i++)
		{
			EffectList.EffectData effectData = this.m_effectPrefabs[i];
			if (effectData.m_enabled)
			{
				if (parent && this.m_effectPrefabs[i].m_inheritParentRotation)
				{
					rot = parent.rotation;
				}
				if (effectData.m_randomRotation)
				{
					rot = UnityEngine.Random.rotation;
				}
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(effectData.m_prefab, pos, rot);
				if (effectData.m_scale)
				{
					if (parent && this.m_effectPrefabs[i].m_inheritParentScale)
					{
						Vector3 localScale = parent.localScale * scale;
						gameObject.transform.localScale = localScale;
					}
					else
					{
						gameObject.transform.localScale = new Vector3(scale, scale, scale);
					}
				}
				else if (parent && this.m_effectPrefabs[i].m_inheritParentScale)
				{
					gameObject.transform.localScale = parent.localScale;
				}
				if (effectData.m_attach && parent != null)
				{
					gameObject.transform.SetParent(parent);
				}
				list.Add(gameObject);
			}
		}
		return list.ToArray();
	}

	public bool HasEffects()
	{
		if (this.m_effectPrefabs == null || this.m_effectPrefabs.Length == 0)
		{
			return false;
		}
		EffectList.EffectData[] effectPrefabs = this.m_effectPrefabs;
		for (int i = 0; i < effectPrefabs.Length; i++)
		{
			if (effectPrefabs[i].m_enabled)
			{
				return true;
			}
		}
		return false;
	}

	public EffectList.EffectData[] m_effectPrefabs = new EffectList.EffectData[0];

	[Serializable]
	public class EffectData
	{
		public GameObject m_prefab;

		public bool m_enabled = true;

		public bool m_attach;

		public bool m_inheritParentRotation;

		public bool m_inheritParentScale;

		public bool m_randomRotation;

		public bool m_scale;
	}
}
