using System;
using UnityEngine;

public class GuidePoint : MonoBehaviour
{
	private void Start()
	{
		if (!Raven.IsInstantiated())
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_ravenPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
		}
		this.m_text.m_static = true;
		this.m_text.m_guidePoint = this;
		Raven.RegisterStaticText(this.m_text);
	}

	private void OnDestroy()
	{
		Raven.UnregisterStaticText(this.m_text);
	}

	private void OnDrawGizmos()
	{
	}

	public Raven.RavenText m_text = new Raven.RavenText();

	public GameObject m_ravenPrefab;
}
