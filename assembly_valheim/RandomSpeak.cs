using System;
using UnityEngine;

public class RandomSpeak : MonoBehaviour
{
	private void Start()
	{
		base.InvokeRepeating("Speak", UnityEngine.Random.Range(0f, this.m_interval), this.m_interval);
	}

	private void Speak()
	{
		if (UnityEngine.Random.value > this.m_chance)
		{
			return;
		}
		if (this.m_texts.Length == 0)
		{
			return;
		}
		if (Player.m_localPlayer == null || Vector3.Distance(base.transform.position, Player.m_localPlayer.transform.position) > this.m_triggerDistance)
		{
			return;
		}
		this.m_speakEffects.Create(base.transform.position, base.transform.rotation, null, 1f);
		string text = this.m_texts[UnityEngine.Random.Range(0, this.m_texts.Length)];
		Chat.instance.SetNpcText(base.gameObject, this.m_offset, this.m_cullDistance, this.m_ttl, this.m_topic, text, this.m_useLargeDialog);
		if (this.m_onlyOnce)
		{
			base.CancelInvoke("Speak");
		}
	}

	public float m_interval = 5f;

	public float m_chance = 0.5f;

	public float m_triggerDistance = 5f;

	public float m_cullDistance = 10f;

	public float m_ttl = 10f;

	public Vector3 m_offset = new Vector3(0f, 0f, 0f);

	public EffectList m_speakEffects = new EffectList();

	public bool m_useLargeDialog;

	public bool m_onlyOnce;

	public string m_topic = "";

	public string[] m_texts = new string[0];
}
