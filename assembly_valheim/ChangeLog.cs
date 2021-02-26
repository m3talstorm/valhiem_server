using System;
using UnityEngine;
using UnityEngine.UI;

public class ChangeLog : MonoBehaviour
{
	private void Start()
	{
		string text = this.m_changeLog.text;
		this.m_textField.text = text;
	}

	public Text m_textField;

	public TextAsset m_changeLog;
}
