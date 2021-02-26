using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEffect : MonoBehaviour
{
	private void Start()
	{
		this.m_animator = base.GetComponent<Animator>();
	}

	public void Effect(AnimationEvent e)
	{
		string stringParameter = e.stringParameter;
		GameObject original = e.objectReferenceParameter as GameObject;
		Transform transform = null;
		if (stringParameter.Length > 0)
		{
			transform = Utils.FindChild(base.transform, stringParameter);
		}
		if (transform == null)
		{
			transform = (this.m_effectRoot ? this.m_effectRoot : base.transform);
		}
		UnityEngine.Object.Instantiate<GameObject>(original, transform.position, transform.rotation);
	}

	public void Attach(AnimationEvent e)
	{
		string stringParameter = e.stringParameter;
		GameObject original = e.objectReferenceParameter as GameObject;
		Transform transform = Utils.FindChild(base.transform, stringParameter);
		if (transform == null)
		{
			ZLog.LogWarning("Failed to find attach joint " + stringParameter);
			return;
		}
		this.ClearAttachment(transform);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, transform.position, transform.rotation);
		gameObject.transform.SetParent(transform, true);
		if (this.m_attachments == null)
		{
			this.m_attachments = new List<GameObject>();
		}
		this.m_attachments.Add(gameObject);
		this.m_attachStateHash = e.animatorStateInfo.fullPathHash;
		base.CancelInvoke("UpdateAttachments");
		base.InvokeRepeating("UpdateAttachments", 0.1f, 0.1f);
	}

	private void ClearAttachment(Transform parent)
	{
		if (this.m_attachments == null)
		{
			return;
		}
		foreach (GameObject gameObject in this.m_attachments)
		{
			if (gameObject && gameObject.transform.parent == parent)
			{
				this.m_attachments.Remove(gameObject);
				UnityEngine.Object.Destroy(gameObject);
				break;
			}
		}
	}

	public void RemoveAttachments()
	{
		if (this.m_attachments == null)
		{
			return;
		}
		foreach (GameObject obj in this.m_attachments)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_attachments.Clear();
	}

	private void UpdateAttachments()
	{
		if (this.m_attachments != null && this.m_attachments.Count > 0)
		{
			if (this.m_attachStateHash != this.m_animator.GetCurrentAnimatorStateInfo(0).fullPathHash && this.m_attachStateHash != this.m_animator.GetNextAnimatorStateInfo(0).fullPathHash)
			{
				this.RemoveAttachments();
				return;
			}
		}
		else
		{
			base.CancelInvoke("UpdateAttachments");
		}
	}

	public Transform m_effectRoot;

	private Animator m_animator;

	private List<GameObject> m_attachments;

	private int m_attachStateHash;
}
