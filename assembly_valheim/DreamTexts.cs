using System;
using System.Collections.Generic;
using UnityEngine;

public class DreamTexts : MonoBehaviour
{
	public DreamTexts.DreamText GetRandomDreamText()
	{
		List<DreamTexts.DreamText> list = new List<DreamTexts.DreamText>();
		foreach (DreamTexts.DreamText dreamText in this.m_texts)
		{
			if (this.HaveGlobalKeys(dreamText))
			{
				list.Add(dreamText);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		DreamTexts.DreamText dreamText2 = list[UnityEngine.Random.Range(0, list.Count)];
		if (UnityEngine.Random.value <= dreamText2.m_chanceToDream)
		{
			return dreamText2;
		}
		return null;
	}

	private bool HaveGlobalKeys(DreamTexts.DreamText dream)
	{
		foreach (string name in dream.m_trueKeys)
		{
			if (!ZoneSystem.instance.GetGlobalKey(name))
			{
				return false;
			}
		}
		foreach (string name2 in dream.m_falseKeys)
		{
			if (ZoneSystem.instance.GetGlobalKey(name2))
			{
				return false;
			}
		}
		return true;
	}

	public List<DreamTexts.DreamText> m_texts = new List<DreamTexts.DreamText>();

	[Serializable]
	public class DreamText
	{
		public string m_text = "Fluffy sheep";

		public float m_chanceToDream = 0.1f;

		public List<string> m_trueKeys = new List<string>();

		public List<string> m_falseKeys = new List<string>();
	}
}
