using System;
using UnityEngine;

public class RandomIdle : StateMachineBehaviour
{
	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		int randomIdle = this.GetRandomIdle(animator);
		animator.SetFloat(this.m_valueName, (float)randomIdle);
		this.m_last = stateInfo.normalizedTime % 1f;
	}

	public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		float num = stateInfo.normalizedTime % 1f;
		if (num < this.m_last)
		{
			int randomIdle = this.GetRandomIdle(animator);
			animator.SetFloat(this.m_valueName, (float)randomIdle);
		}
		this.m_last = num;
	}

	private int GetRandomIdle(Animator animator)
	{
		if (!this.m_haveSetup)
		{
			this.m_haveSetup = true;
			this.m_baseAI = animator.GetComponentInParent<BaseAI>();
		}
		if (this.m_baseAI && this.m_alertedIdle >= 0 && this.m_baseAI.IsAlerted())
		{
			return this.m_alertedIdle;
		}
		return UnityEngine.Random.Range(0, this.m_animations);
	}

	public int m_animations = 4;

	public string m_valueName = "";

	public int m_alertedIdle = -1;

	private float m_last;

	private bool m_haveSetup;

	private BaseAI m_baseAI;
}
