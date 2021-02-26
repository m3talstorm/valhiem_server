using System;
using UnityEngine;

public class Talker : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_character = base.GetComponent<Character>();
		this.m_nview.Register<int, string, string>("Say", new Action<long, int, string, string>(this.RPC_Say));
	}

	public void Say(Talker.Type type, string text)
	{
		ZLog.Log(string.Concat(new object[]
		{
			"Saying ",
			type,
			"  ",
			text
		}));
		this.m_nview.InvokeRPC(ZNetView.Everybody, "Say", new object[]
		{
			(int)type,
			Game.instance.GetPlayerProfile().GetName(),
			text
		});
	}

	private void RPC_Say(long sender, int ctype, string user, string text)
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		float num = 0f;
		switch (ctype)
		{
		case 0:
			num = this.m_visperDistance;
			break;
		case 1:
			num = this.m_normalDistance;
			break;
		case 2:
			num = this.m_shoutDistance;
			break;
		}
		if (Vector3.Distance(base.transform.position, Player.m_localPlayer.transform.position) < num && Chat.instance)
		{
			Vector3 headPoint = this.m_character.GetHeadPoint();
			Chat.instance.OnNewChatMessage(base.gameObject, sender, headPoint, (Talker.Type)ctype, user, text);
		}
	}

	public float m_visperDistance = 4f;

	public float m_normalDistance = 15f;

	public float m_shoutDistance = 70f;

	private ZNetView m_nview;

	private Character m_character;

	public enum Type
	{
		Whisper,
		Normal,
		Shout,
		Ping
	}
}
