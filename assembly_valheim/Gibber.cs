using System;
using UnityEngine;

public class Gibber : MonoBehaviour
{
	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (!this.m_done)
		{
			this.Explode(base.transform.position, Vector3.zero);
		}
	}

	public void Setup(Vector3 hitPoint, Vector3 hitDirection)
	{
		this.Explode(hitPoint, hitDirection);
	}

	private void DestroyAll()
	{
		if (this.m_nview)
		{
			if (this.m_nview.GetZDO().m_owner == 0L)
			{
				this.m_nview.ClaimOwnership();
			}
			if (this.m_nview.IsOwner())
			{
				ZNetScene.instance.Destroy(base.gameObject);
				return;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void CreateBodies()
	{
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			GameObject gameObject = componentsInChildren[i].gameObject;
			if (!gameObject.GetComponent<Rigidbody>())
			{
				gameObject.AddComponent<BoxCollider>();
				gameObject.AddComponent<Rigidbody>();
			}
		}
	}

	private void Explode(Vector3 hitPoint, Vector3 hitDirection)
	{
		this.m_done = true;
		base.InvokeRepeating("DestroyAll", this.m_timeout, 1f);
		Vector3 position = base.transform.position;
		float t = ((double)hitDirection.magnitude > 0.01) ? this.m_impactDirectionMix : 0f;
		this.CreateBodies();
		foreach (Rigidbody rigidbody in base.gameObject.GetComponentsInChildren<Rigidbody>())
		{
			float d = UnityEngine.Random.Range(this.m_minVel, this.m_maxVel);
			Vector3 a = Vector3.Lerp(Vector3.Normalize(rigidbody.worldCenterOfMass - position), hitDirection, t);
			rigidbody.velocity = a * d;
			rigidbody.angularVelocity = new Vector3(UnityEngine.Random.Range(-this.m_maxRotVel, this.m_maxRotVel), UnityEngine.Random.Range(-this.m_maxRotVel, this.m_maxRotVel), UnityEngine.Random.Range(-this.m_maxRotVel, this.m_maxRotVel));
		}
		foreach (Gibber.GibbData gibbData in this.m_gibbs)
		{
			if (gibbData.m_object && gibbData.m_chanceToSpawn < 1f && UnityEngine.Random.value > gibbData.m_chanceToSpawn)
			{
				UnityEngine.Object.Destroy(gibbData.m_object);
			}
		}
		if ((double)hitDirection.magnitude > 0.01)
		{
			Quaternion rot = Quaternion.LookRotation(hitDirection);
			this.m_punchEffector.Create(hitPoint, rot, null, 1f);
		}
	}

	public EffectList m_punchEffector = new EffectList();

	public GameObject m_gibHitEffect;

	public GameObject m_gibDestroyEffect;

	public float m_gibHitDestroyChance;

	public Gibber.GibbData[] m_gibbs = new Gibber.GibbData[0];

	public float m_minVel = 10f;

	public float m_maxVel = 20f;

	public float m_maxRotVel = 20f;

	public float m_impactDirectionMix = 0.5f;

	public float m_timeout = 5f;

	private bool m_done;

	private ZNetView m_nview;

	[Serializable]
	public class GibbData
	{
		public GameObject m_object;

		public float m_chanceToSpawn = 1f;
	}
}
