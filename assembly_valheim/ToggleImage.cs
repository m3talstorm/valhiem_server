using System;
using UnityEngine;
using UnityEngine.UI;

public class ToggleImage : MonoBehaviour
{
	private void Awake()
	{
		this.m_toggle = base.GetComponent<Toggle>();
	}

	private void Update()
	{
		if (this.m_toggle.isOn)
		{
			this.m_targetImage.sprite = this.m_onImage;
			return;
		}
		this.m_targetImage.sprite = this.m_offImage;
	}

	private Toggle m_toggle;

	public Image m_targetImage;

	public Sprite m_onImage;

	public Sprite m_offImage;
}
