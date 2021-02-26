using System;
using System.Collections;
using UnityEngine;

public class BossStone : MonoBehaviour
{
	private void Start()
	{
		if (this.m_mesh.material.HasProperty("_EmissionColor"))
		{
			this.m_mesh.materials[this.m_emissiveMaterialIndex].SetColor("_EmissionColor", Color.black);
		}
		if (this.m_activeEffect)
		{
			this.m_activeEffect.SetActive(false);
		}
		this.SetActivated(this.m_itemStand.HaveAttachment(), false);
		base.InvokeRepeating("UpdateVisual", 1f, 1f);
	}

	private void UpdateVisual()
	{
		this.SetActivated(this.m_itemStand.HaveAttachment(), true);
	}

	private void SetActivated(bool active, bool triggerEffect)
	{
		if (active == this.m_active)
		{
			return;
		}
		this.m_active = active;
		if (triggerEffect && active)
		{
			base.Invoke("DelayedAttachEffects_Step1", 1f);
			base.Invoke("DelayedAttachEffects_Step2", 5f);
			base.Invoke("DelayedAttachEffects_Step3", 11f);
			return;
		}
		if (this.m_activeEffect)
		{
			this.m_activeEffect.SetActive(active);
		}
		base.StopCoroutine("FadeEmission");
		base.StartCoroutine("FadeEmission");
	}

	private void DelayedAttachEffects_Step1()
	{
		this.m_activateStep1.Create(this.m_itemStand.transform.position, base.transform.rotation, null, 1f);
	}

	private void DelayedAttachEffects_Step2()
	{
		this.m_activateStep2.Create(base.transform.position, base.transform.rotation, null, 1f);
	}

	private void DelayedAttachEffects_Step3()
	{
		if (this.m_activeEffect)
		{
			this.m_activeEffect.SetActive(true);
		}
		this.m_activateStep3.Create(base.transform.position, base.transform.rotation, null, 1f);
		base.StopCoroutine("FadeEmission");
		base.StartCoroutine("FadeEmission");
		Player.MessageAllInRange(base.transform.position, 20f, MessageHud.MessageType.Center, this.m_completedMessage, null);
	}

	private IEnumerator FadeEmission()
	{
		if (this.m_mesh && this.m_mesh.materials[this.m_emissiveMaterialIndex].HasProperty("_EmissionColor"))
		{
			Color startColor = this.m_mesh.materials[this.m_emissiveMaterialIndex].GetColor("_EmissionColor");
			Color targetColor = this.m_active ? this.m_activeEmissiveColor : Color.black;
			for (float t = 0f; t < 1f; t += Time.deltaTime)
			{
				Color value = Color.Lerp(startColor, targetColor, t / 1f);
				this.m_mesh.materials[this.m_emissiveMaterialIndex].SetColor("_EmissionColor", value);
				yield return null;
			}
			startColor = default(Color);
			targetColor = default(Color);
		}
		ZLog.Log("Done fading color");
		yield break;
	}

	public bool IsActivated()
	{
		return this.m_active;
	}

	public ItemStand m_itemStand;

	public GameObject m_activeEffect;

	public EffectList m_activateStep1 = new EffectList();

	public EffectList m_activateStep2 = new EffectList();

	public EffectList m_activateStep3 = new EffectList();

	public string m_completedMessage = "";

	public MeshRenderer m_mesh;

	public int m_emissiveMaterialIndex;

	public Color m_activeEmissiveColor = Color.white;

	private bool m_active;

	private ZNetView m_nview;
}
