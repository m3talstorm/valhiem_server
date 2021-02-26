using System;
using UnityEngine;

public class LodFadeInOut : MonoBehaviour
{
	private void Awake()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		if (Vector3.Distance(mainCamera.transform.position, base.transform.position) > 20f)
		{
			this.m_lodGroup = base.GetComponent<LODGroup>();
			if (this.m_lodGroup)
			{
				this.m_originalLocalRef = this.m_lodGroup.localReferencePoint;
				this.m_lodGroup.localReferencePoint = new Vector3(999999f, 999999f, 999999f);
				base.Invoke("FadeIn", UnityEngine.Random.Range(0.1f, 0.3f));
			}
		}
	}

	private void FadeIn()
	{
		this.m_lodGroup.localReferencePoint = this.m_originalLocalRef;
	}

	private Vector3 m_originalLocalRef;

	private LODGroup m_lodGroup;

	private const float m_minTriggerDistance = 20f;
}
