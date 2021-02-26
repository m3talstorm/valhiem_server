using System;
using UnityEngine;
using UnityEngine.UI;

public class WaterMark : MonoBehaviour
{
	private void Awake()
	{
		this.m_text.text = "Version: " + global::Version.GetVersionString();
	}

	public Text m_text;
}
