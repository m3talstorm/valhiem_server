using System;
using System.Collections.Generic;
using UnityEngine;

public class RandomAnimation : MonoBehaviour
{
	private void Start()
	{
		this.m_anim = base.GetComponentInChildren<Animator>();
		this.m_nview = base.GetComponent<ZNetView>();
	}

	private void FixedUpdate()
	{
		if (this.m_nview != null && !this.m_nview.IsValid())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		for (int i = 0; i < this.m_values.Count; i++)
		{
			RandomAnimation.RandomValue randomValue = this.m_values[i];
			if (this.m_nview == null || this.m_nview.IsOwner())
			{
				randomValue.m_timer += fixedDeltaTime;
				if (randomValue.m_timer > randomValue.m_interval)
				{
					randomValue.m_timer = 0f;
					randomValue.m_value = UnityEngine.Random.Range(0, randomValue.m_values);
					if (this.m_nview)
					{
						this.m_nview.GetZDO().Set("RA_" + randomValue.m_name, randomValue.m_value);
					}
					if (!randomValue.m_floatValue)
					{
						this.m_anim.SetInteger(randomValue.m_name, randomValue.m_value);
					}
				}
			}
			if (this.m_nview && !this.m_nview.IsOwner())
			{
				int @int = this.m_nview.GetZDO().GetInt("RA_" + randomValue.m_name, 0);
				if (@int != randomValue.m_value)
				{
					randomValue.m_value = @int;
					if (!randomValue.m_floatValue)
					{
						this.m_anim.SetInteger(randomValue.m_name, randomValue.m_value);
					}
				}
			}
			if (randomValue.m_floatValue)
			{
				if (randomValue.m_hashValues == null || randomValue.m_hashValues.Length != randomValue.m_values)
				{
					randomValue.m_hashValues = new int[randomValue.m_values];
					for (int j = 0; j < randomValue.m_values; j++)
					{
						randomValue.m_hashValues[j] = Animator.StringToHash(randomValue.m_name + j.ToString());
					}
				}
				for (int k = 0; k < randomValue.m_values; k++)
				{
					float num = this.m_anim.GetFloat(randomValue.m_hashValues[k]);
					if (k == randomValue.m_value)
					{
						num = Mathf.MoveTowards(num, 1f, fixedDeltaTime / randomValue.m_floatTransition);
					}
					else
					{
						num = Mathf.MoveTowards(num, 0f, fixedDeltaTime / randomValue.m_floatTransition);
					}
					this.m_anim.SetFloat(randomValue.m_hashValues[k], num);
				}
			}
		}
	}

	public List<RandomAnimation.RandomValue> m_values = new List<RandomAnimation.RandomValue>();

	private Animator m_anim;

	private ZNetView m_nview;

	[Serializable]
	public class RandomValue
	{
		public string m_name;

		public int m_values;

		public float m_interval;

		public bool m_floatValue;

		public float m_floatTransition = 1f;

		[NonSerialized]
		public float m_timer;

		[NonSerialized]
		public int m_value;

		[NonSerialized]
		public int[] m_hashValues;
	}
}
