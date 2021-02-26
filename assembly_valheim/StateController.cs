using System;
using UnityEngine;

public class StateController : StateMachineBehaviour
{
	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (this.m_enterEffect.HasEffects())
		{
			this.m_enterEffect.Create(this.GetEffectPos(animator), animator.transform.rotation, null, 1f);
		}
		if (this.m_enterDisableChildren)
		{
			for (int i = 0; i < animator.transform.childCount; i++)
			{
				animator.transform.GetChild(i).gameObject.SetActive(false);
			}
		}
		if (this.m_enterEnableChildren)
		{
			for (int j = 0; j < animator.transform.childCount; j++)
			{
				animator.transform.GetChild(j).gameObject.SetActive(true);
			}
		}
	}

	private Vector3 GetEffectPos(Animator animator)
	{
		if (this.m_effectJoint.Length == 0)
		{
			return animator.transform.position;
		}
		if (this.m_effectJoinT == null)
		{
			this.m_effectJoinT = Utils.FindChild(animator.transform, this.m_effectJoint);
		}
		return this.m_effectJoinT.position;
	}

	public string m_effectJoint = "";

	public EffectList m_enterEffect = new EffectList();

	public bool m_enterDisableChildren;

	public bool m_enterEnableChildren;

	public GameObject[] m_enterDisable = new GameObject[0];

	public GameObject[] m_enterEnable = new GameObject[0];

	private Transform m_effectJoinT;
}
