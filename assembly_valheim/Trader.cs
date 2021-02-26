using System;
using System.Collections.Generic;
using UnityEngine;

public class Trader : MonoBehaviour, Hoverable, Interactable
{
	private void Start()
	{
		this.m_animator = base.GetComponentInChildren<Animator>();
		this.m_lookAt = base.GetComponentInChildren<LookAt>();
		base.InvokeRepeating("RandomTalk", this.m_randomTalkInterval, this.m_randomTalkInterval);
	}

	private void Update()
	{
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, this.m_standRange);
		if (closestPlayer)
		{
			this.m_animator.SetBool("Stand", true);
			this.m_lookAt.SetLoockAtTarget(closestPlayer.GetHeadPoint());
			float num = Vector3.Distance(closestPlayer.transform.position, base.transform.position);
			if (!this.m_didGreet && num < this.m_greetRange)
			{
				this.m_didGreet = true;
				this.Say(this.m_randomGreets, "Greet");
				this.m_randomGreetFX.Create(base.transform.position, Quaternion.identity, null, 1f);
			}
			if (this.m_didGreet && !this.m_didGoodbye && num > this.m_byeRange)
			{
				this.m_didGoodbye = true;
				this.Say(this.m_randomGoodbye, "Greet");
				this.m_randomGoodbyeFX.Create(base.transform.position, Quaternion.identity, null, 1f);
				return;
			}
		}
		else
		{
			this.m_animator.SetBool("Stand", false);
			this.m_lookAt.ResetTarget();
		}
	}

	private void RandomTalk()
	{
		if (this.m_animator.GetBool("Stand") && !StoreGui.IsVisible() && Player.IsPlayerInRange(base.transform.position, this.m_greetRange))
		{
			this.Say(this.m_randomTalk, "Talk");
			this.m_randomTalkFX.Create(base.transform.position, Quaternion.identity, null, 1f);
		}
	}

	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact");
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_name);
	}

	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			return false;
		}
		StoreGui.instance.Show(this);
		this.Say(this.m_randomStartTrade, "Talk");
		this.m_randomStartTradeFX.Create(base.transform.position, Quaternion.identity, null, 1f);
		return false;
	}

	private void DiscoverItems(Player player)
	{
		foreach (Trader.TradeItem tradeItem in this.m_items)
		{
			player.AddKnownItem(tradeItem.m_prefab.m_itemData);
		}
	}

	private void Say(List<string> texts, string trigger)
	{
		this.Say(texts[UnityEngine.Random.Range(0, texts.Count)], trigger);
	}

	private void Say(string text, string trigger)
	{
		Chat.instance.SetNpcText(base.gameObject, Vector3.up * 1.5f, 20f, this.m_hideDialogDelay, "", text, false);
		if (trigger.Length > 0)
		{
			this.m_animator.SetTrigger(trigger);
		}
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void OnBought(Trader.TradeItem item)
	{
		this.Say(this.m_randomBuy, "Buy");
		this.m_randomBuyFX.Create(base.transform.position, Quaternion.identity, null, 1f);
	}

	public void OnSold()
	{
		this.Say(this.m_randomSell, "Sell");
		this.m_randomSellFX.Create(base.transform.position, Quaternion.identity, null, 1f);
	}

	public string m_name = "Haldor";

	public float m_standRange = 15f;

	public float m_greetRange = 5f;

	public float m_byeRange = 5f;

	public List<Trader.TradeItem> m_items = new List<Trader.TradeItem>();

	[Header("Dialog")]
	public float m_hideDialogDelay = 5f;

	public float m_randomTalkInterval = 30f;

	public List<string> m_randomTalk = new List<string>();

	public List<string> m_randomGreets = new List<string>();

	public List<string> m_randomGoodbye = new List<string>();

	public List<string> m_randomStartTrade = new List<string>();

	public List<string> m_randomBuy = new List<string>();

	public List<string> m_randomSell = new List<string>();

	public EffectList m_randomTalkFX = new EffectList();

	public EffectList m_randomGreetFX = new EffectList();

	public EffectList m_randomGoodbyeFX = new EffectList();

	public EffectList m_randomStartTradeFX = new EffectList();

	public EffectList m_randomBuyFX = new EffectList();

	public EffectList m_randomSellFX = new EffectList();

	private bool m_didGreet;

	private bool m_didGoodbye;

	private Animator m_animator;

	private LookAt m_lookAt;

	[Serializable]
	public class TradeItem
	{
		public ItemDrop m_prefab;

		public int m_stack = 1;

		public int m_price = 100;
	}
}
