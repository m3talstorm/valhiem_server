using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageHud : MonoBehaviour
{
	private void Awake()
	{
		MessageHud.m_instance = this;
	}

	private void OnDestroy()
	{
		MessageHud.m_instance = null;
	}

	public static MessageHud instance
	{
		get
		{
			return MessageHud.m_instance;
		}
	}

	private void Start()
	{
		this.m_messageText.canvasRenderer.SetAlpha(0f);
		this.m_messageIcon.canvasRenderer.SetAlpha(0f);
		this.m_messageCenterText.canvasRenderer.SetAlpha(0f);
		for (int i = 0; i < this.m_maxUnlockMessages; i++)
		{
			this.m_unlockMessages.Add(null);
		}
		ZRoutedRpc.instance.Register<int, string>("ShowMessage", new Action<long, int, string>(this.RPC_ShowMessage));
	}

	private void Update()
	{
		if (Hud.IsUserHidden())
		{
			this.HideAll();
			return;
		}
		this.UpdateUnlockMsg(Time.deltaTime);
		this.UpdateMessage(Time.deltaTime);
		this.UpdateBiomeFound(Time.deltaTime);
	}

	private void HideAll()
	{
		for (int i = 0; i < this.m_maxUnlockMessages; i++)
		{
			if (this.m_unlockMessages[i] != null)
			{
				UnityEngine.Object.Destroy(this.m_unlockMessages[i]);
				this.m_unlockMessages[i] = null;
			}
		}
		this.m_messageText.canvasRenderer.SetAlpha(0f);
		this.m_messageIcon.canvasRenderer.SetAlpha(0f);
		this.m_messageCenterText.canvasRenderer.SetAlpha(0f);
		if (this.m_biomeMsgInstance)
		{
			UnityEngine.Object.Destroy(this.m_biomeMsgInstance);
			this.m_biomeMsgInstance = null;
		}
	}

	public void MessageAll(MessageHud.MessageType type, string text)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ShowMessage", new object[]
		{
			(int)type,
			text
		});
	}

	private void RPC_ShowMessage(long sender, int type, string text)
	{
		this.ShowMessage((MessageHud.MessageType)type, text, 0, null);
	}

	public void ShowMessage(MessageHud.MessageType type, string text, int amount = 0, Sprite icon = null)
	{
		if (Hud.IsUserHidden())
		{
			return;
		}
		text = Localization.instance.Localize(text);
		if (type == MessageHud.MessageType.TopLeft)
		{
			MessageHud.MsgData msgData = new MessageHud.MsgData();
			msgData.m_icon = icon;
			msgData.m_text = text;
			msgData.m_amount = amount;
			this.m_msgQeue.Enqueue(msgData);
			this.AddLog(text);
			return;
		}
		if (type != MessageHud.MessageType.Center)
		{
			return;
		}
		this.m_messageCenterText.text = text;
		this.m_messageCenterText.canvasRenderer.SetAlpha(1f);
		this.m_messageCenterText.CrossFadeAlpha(0f, 4f, true);
	}

	private void UpdateMessage(float dt)
	{
		this.m_msgQueueTimer += dt;
		if (this.m_msgQeue.Count > 0)
		{
			MessageHud.MsgData msgData = this.m_msgQeue.Peek();
			bool flag = this.m_msgQueueTimer < 4f && msgData.m_text == this.currentMsg.m_text && msgData.m_icon == this.currentMsg.m_icon;
			if (this.m_msgQueueTimer >= 1f || flag)
			{
				MessageHud.MsgData msgData2 = this.m_msgQeue.Dequeue();
				this.m_messageText.text = msgData2.m_text;
				if (flag)
				{
					msgData2.m_amount += this.currentMsg.m_amount;
				}
				if (msgData2.m_amount > 1)
				{
					Text messageText = this.m_messageText;
					messageText.text = messageText.text + " x" + msgData2.m_amount;
				}
				this.m_messageText.canvasRenderer.SetAlpha(1f);
				this.m_messageText.CrossFadeAlpha(0f, 4f, true);
				if (msgData2.m_icon != null)
				{
					this.m_messageIcon.sprite = msgData2.m_icon;
					this.m_messageIcon.canvasRenderer.SetAlpha(1f);
					this.m_messageIcon.CrossFadeAlpha(0f, 4f, true);
				}
				else
				{
					this.m_messageIcon.canvasRenderer.SetAlpha(0f);
				}
				this.currentMsg = msgData2;
				this.m_msgQueueTimer = 0f;
			}
		}
	}

	private void UpdateBiomeFound(float dt)
	{
		if (this.m_biomeMsgInstance != null && this.m_biomeMsgInstance.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsTag("done"))
		{
			UnityEngine.Object.Destroy(this.m_biomeMsgInstance);
			this.m_biomeMsgInstance = null;
		}
		if (this.m_biomeFoundQueue.Count > 0 && this.m_biomeMsgInstance == null && this.m_msgQeue.Count == 0 && this.m_msgQueueTimer > 2f)
		{
			MessageHud.BiomeMessage biomeMessage = this.m_biomeFoundQueue.Dequeue();
			this.m_biomeMsgInstance = UnityEngine.Object.Instantiate<GameObject>(this.m_biomeFoundPrefab, base.transform);
			Text component = Utils.FindChild(this.m_biomeMsgInstance.transform, "Title").GetComponent<Text>();
			string text = Localization.instance.Localize(biomeMessage.m_text);
			component.text = text;
			if (biomeMessage.m_playStinger && this.m_biomeFoundStinger)
			{
				UnityEngine.Object.Instantiate<GameObject>(this.m_biomeFoundStinger);
			}
		}
	}

	public void ShowBiomeFoundMsg(string text, bool playStinger)
	{
		MessageHud.BiomeMessage biomeMessage = new MessageHud.BiomeMessage();
		biomeMessage.m_text = text;
		biomeMessage.m_playStinger = playStinger;
		this.m_biomeFoundQueue.Enqueue(biomeMessage);
	}

	public void QueueUnlockMsg(Sprite icon, string topic, string description)
	{
		MessageHud.UnlockMsg unlockMsg = new MessageHud.UnlockMsg();
		unlockMsg.m_icon = icon;
		unlockMsg.m_topic = Localization.instance.Localize(topic);
		unlockMsg.m_description = Localization.instance.Localize(description);
		this.m_unlockMsgQueue.Enqueue(unlockMsg);
		this.AddLog(topic + ":" + description);
		ZLog.Log("Queue unlock msg:" + topic + ":" + description);
	}

	private int GetFreeUnlockMsgSlot()
	{
		for (int i = 0; i < this.m_unlockMessages.Count; i++)
		{
			if (this.m_unlockMessages[i] == null)
			{
				return i;
			}
		}
		return -1;
	}

	private void UpdateUnlockMsg(float dt)
	{
		for (int i = 0; i < this.m_unlockMessages.Count; i++)
		{
			GameObject gameObject = this.m_unlockMessages[i];
			if (!(gameObject == null) && gameObject.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsTag("done"))
			{
				UnityEngine.Object.Destroy(gameObject);
				this.m_unlockMessages[i] = null;
				break;
			}
		}
		if (this.m_unlockMsgQueue.Count > 0)
		{
			int freeUnlockMsgSlot = this.GetFreeUnlockMsgSlot();
			if (freeUnlockMsgSlot != -1)
			{
				Transform transform = base.transform;
				GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(this.m_unlockMsgPrefab, transform);
				this.m_unlockMessages[freeUnlockMsgSlot] = gameObject2;
				RectTransform rectTransform = gameObject2.transform as RectTransform;
				Vector3 v = rectTransform.anchoredPosition;
				v.y -= (float)(this.m_maxUnlockMsgSpace * freeUnlockMsgSlot);
				rectTransform.anchoredPosition = v;
				MessageHud.UnlockMsg unlockMsg = this.m_unlockMsgQueue.Dequeue();
				Image component = rectTransform.Find("UnlockMessage/icon_bkg/UnlockIcon").GetComponent<Image>();
				Text component2 = rectTransform.Find("UnlockMessage/UnlockTitle").GetComponent<Text>();
				Text component3 = rectTransform.Find("UnlockMessage/UnlockDescription").GetComponent<Text>();
				component.sprite = unlockMsg.m_icon;
				component2.text = unlockMsg.m_topic;
				component3.text = unlockMsg.m_description;
			}
		}
	}

	private void AddLog(string logText)
	{
		this.m_messageLog.Add(logText);
		while (this.m_messageLog.Count > this.m_maxLogMessages)
		{
			this.m_messageLog.RemoveAt(0);
		}
	}

	public List<string> GetLog()
	{
		return this.m_messageLog;
	}

	private MessageHud.MsgData currentMsg = new MessageHud.MsgData();

	private static MessageHud m_instance;

	public Text m_messageText;

	public Image m_messageIcon;

	public Text m_messageCenterText;

	public GameObject m_unlockMsgPrefab;

	public int m_maxUnlockMsgSpace = 110;

	public int m_maxUnlockMessages = 4;

	public int m_maxLogMessages = 50;

	public GameObject m_biomeFoundPrefab;

	public GameObject m_biomeFoundStinger;

	private Queue<MessageHud.BiomeMessage> m_biomeFoundQueue = new Queue<MessageHud.BiomeMessage>();

	private List<string> m_messageLog = new List<string>();

	private List<GameObject> m_unlockMessages = new List<GameObject>();

	private Queue<MessageHud.UnlockMsg> m_unlockMsgQueue = new Queue<MessageHud.UnlockMsg>();

	private Queue<MessageHud.MsgData> m_msgQeue = new Queue<MessageHud.MsgData>();

	private float m_msgQueueTimer = -1f;

	private GameObject m_biomeMsgInstance;

	public enum MessageType
	{
		TopLeft = 1,
		Center
	}

	private class UnlockMsg
	{
		public Sprite m_icon;

		public string m_topic;

		public string m_description;
	}

	private class MsgData
	{
		public Sprite m_icon;

		public string m_text;

		public int m_amount;
	}

	private class BiomeMessage
	{
		public string m_text;

		public bool m_playStinger;
	}
}
