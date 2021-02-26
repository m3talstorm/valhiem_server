using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoreGui : MonoBehaviour
{
	public static StoreGui instance
	{
		get
		{
			return StoreGui.m_instance;
		}
	}

	private void Awake()
	{
		StoreGui.m_instance = this;
		this.m_rootPanel.SetActive(false);
		this.m_itemlistBaseSize = this.m_listRoot.rect.height;
	}

	private void OnDestroy()
	{
		if (StoreGui.m_instance == this)
		{
			StoreGui.m_instance = null;
		}
	}

	private void Update()
	{
		if (!this.m_rootPanel.activeSelf)
		{
			this.m_hiddenFrames++;
			return;
		}
		this.m_hiddenFrames = 0;
		if (!this.m_trader)
		{
			this.Hide();
			return;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene())
		{
			this.Hide();
			return;
		}
		if (Vector3.Distance(this.m_trader.transform.position, Player.m_localPlayer.transform.position) > this.m_hideDistance)
		{
			this.Hide();
			return;
		}
		if (InventoryGui.IsVisible() || Minimap.IsOpen())
		{
			this.Hide();
			return;
		}
		if ((Chat.instance == null || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !Menu.IsVisible() && TextViewer.instance && !TextViewer.instance.IsVisible() && !localPlayer.InCutscene() && (ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("Use")))
		{
			ZInput.ResetButtonStatus("JoyButtonB");
			this.Hide();
		}
		this.UpdateBuyButton();
		this.UpdateSellButton();
		this.UpdateRecipeGamepadInput();
		this.m_coinText.text = this.GetPlayerCoins().ToString();
	}

	public void Show(Trader trader)
	{
		if (this.m_trader == trader && StoreGui.IsVisible())
		{
			return;
		}
		this.m_trader = trader;
		this.m_rootPanel.SetActive(true);
		this.FillList();
	}

	public void Hide()
	{
		this.m_trader = null;
		this.m_rootPanel.SetActive(false);
	}

	public static bool IsVisible()
	{
		return StoreGui.m_instance && StoreGui.m_instance.m_hiddenFrames <= 1;
	}

	public void OnBuyItem()
	{
		this.BuySelectedItem();
	}

	private void BuySelectedItem()
	{
		if (this.m_selectedItem == null || !this.CanAfford(this.m_selectedItem))
		{
			return;
		}
		int stack = Mathf.Min(this.m_selectedItem.m_stack, this.m_selectedItem.m_prefab.m_itemData.m_shared.m_maxStackSize);
		int quality = this.m_selectedItem.m_prefab.m_itemData.m_quality;
		int variant = this.m_selectedItem.m_prefab.m_itemData.m_variant;
		if (Player.m_localPlayer.GetInventory().AddItem(this.m_selectedItem.m_prefab.name, stack, quality, variant, 0L, "") != null)
		{
			Player.m_localPlayer.GetInventory().RemoveItem(this.m_coinPrefab.m_itemData.m_shared.m_name, this.m_selectedItem.m_price);
			this.m_trader.OnBought(this.m_selectedItem);
			this.m_buyEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
			Player.m_localPlayer.ShowPickupMessage(this.m_selectedItem.m_prefab.m_itemData, this.m_selectedItem.m_prefab.m_itemData.m_stack);
			this.FillList();
			GoogleAnalyticsV4.instance.LogEvent("Game", "BoughtItem", this.m_selectedItem.m_prefab.name, 0L);
		}
	}

	public void OnSellItem()
	{
		this.SellItem();
	}

	private void SellItem()
	{
		ItemDrop.ItemData sellableItem = this.GetSellableItem();
		if (sellableItem == null)
		{
			return;
		}
		int stack = sellableItem.m_shared.m_value * sellableItem.m_stack;
		Player.m_localPlayer.GetInventory().RemoveItem(sellableItem);
		Player.m_localPlayer.GetInventory().AddItem(this.m_coinPrefab.gameObject.name, stack, this.m_coinPrefab.m_itemData.m_quality, this.m_coinPrefab.m_itemData.m_variant, 0L, "");
		string text;
		if (sellableItem.m_stack > 1)
		{
			text = sellableItem.m_stack + "x" + sellableItem.m_shared.m_name;
		}
		else
		{
			text = sellableItem.m_shared.m_name;
		}
		this.m_sellEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
		Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_sold", new string[]
		{
			text,
			stack.ToString()
		}), 0, sellableItem.m_shared.m_icons[0]);
		this.m_trader.OnSold();
		this.FillList();
		GoogleAnalyticsV4.instance.LogEvent("Game", "SoldItem", text, 0L);
	}

	private int GetPlayerCoins()
	{
		return Player.m_localPlayer.GetInventory().CountItems(this.m_coinPrefab.m_itemData.m_shared.m_name);
	}

	private bool CanAfford(Trader.TradeItem item)
	{
		int playerCoins = this.GetPlayerCoins();
		return item.m_price <= playerCoins;
	}

	private void FillList()
	{
		int playerCoins = this.GetPlayerCoins();
		int num = this.GetSelectedItemIndex();
		List<Trader.TradeItem> items = this.m_trader.m_items;
		foreach (GameObject obj in this.m_itemList)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_itemList.Clear();
		float num2 = (float)items.Count * this.m_itemSpacing;
		num2 = Mathf.Max(this.m_itemlistBaseSize, num2);
		this.m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num2);
		for (int i = 0; i < items.Count; i++)
		{
			Trader.TradeItem tradeItem = items[i];
			GameObject element = UnityEngine.Object.Instantiate<GameObject>(this.m_listElement, this.m_listRoot);
			element.SetActive(true);
			(element.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)i * -this.m_itemSpacing);
			bool flag = tradeItem.m_price <= playerCoins;
			Image component = element.transform.Find("icon").GetComponent<Image>();
			component.sprite = tradeItem.m_prefab.m_itemData.m_shared.m_icons[0];
			component.color = (flag ? Color.white : new Color(1f, 0f, 1f, 0f));
			string text = Localization.instance.Localize(tradeItem.m_prefab.m_itemData.m_shared.m_name);
			if (tradeItem.m_stack > 1)
			{
				text = text + " x" + tradeItem.m_stack;
			}
			Text component2 = element.transform.Find("name").GetComponent<Text>();
			component2.text = text;
			component2.color = (flag ? Color.white : Color.grey);
			UITooltip component3 = element.GetComponent<UITooltip>();
			component3.m_topic = tradeItem.m_prefab.m_itemData.m_shared.m_name;
			component3.m_text = tradeItem.m_prefab.m_itemData.GetTooltip();
			Text component4 = Utils.FindChild(element.transform, "price").GetComponent<Text>();
			component4.text = tradeItem.m_price.ToString();
			if (!flag)
			{
				component4.color = Color.grey;
			}
			element.GetComponent<Button>().onClick.AddListener(delegate
			{
				this.OnSelectedItem(element);
			});
			this.m_itemList.Add(element);
		}
		if (num < 0)
		{
			num = 0;
		}
		this.SelectItem(num, false);
	}

	private void OnSelectedItem(GameObject button)
	{
		int index = this.FindSelectedRecipe(button);
		this.SelectItem(index, false);
	}

	private int FindSelectedRecipe(GameObject button)
	{
		for (int i = 0; i < this.m_itemList.Count; i++)
		{
			if (this.m_itemList[i] == button)
			{
				return i;
			}
		}
		return -1;
	}

	private void SelectItem(int index, bool center)
	{
		ZLog.Log("Setting selected recipe " + index);
		for (int i = 0; i < this.m_itemList.Count; i++)
		{
			bool active = i == index;
			this.m_itemList[i].transform.Find("selected").gameObject.SetActive(active);
		}
		if (center && index >= 0)
		{
			this.m_itemEnsureVisible.CenterOnItem(this.m_itemList[index].transform as RectTransform);
		}
		if (index < 0)
		{
			this.m_selectedItem = null;
			return;
		}
		this.m_selectedItem = this.m_trader.m_items[index];
	}

	private void UpdateSellButton()
	{
		this.m_sellButton.interactable = (this.GetSellableItem() != null);
	}

	private ItemDrop.ItemData GetSellableItem()
	{
		this.m_tempItems.Clear();
		Player.m_localPlayer.GetInventory().GetValuableItems(this.m_tempItems);
		foreach (ItemDrop.ItemData itemData in this.m_tempItems)
		{
			if (itemData.m_shared.m_name != this.m_coinPrefab.m_itemData.m_shared.m_name)
			{
				return itemData;
			}
		}
		return null;
	}

	private int GetSelectedItemIndex()
	{
		int result = 0;
		for (int i = 0; i < this.m_trader.m_items.Count; i++)
		{
			if (this.m_trader.m_items[i] == this.m_selectedItem)
			{
				result = i;
			}
		}
		return result;
	}

	private void UpdateBuyButton()
	{
		UITooltip component = this.m_buyButton.GetComponent<UITooltip>();
		if (this.m_selectedItem == null)
		{
			this.m_buyButton.interactable = false;
			component.m_text = "";
			return;
		}
		bool flag = this.CanAfford(this.m_selectedItem);
		bool flag2 = Player.m_localPlayer.GetInventory().HaveEmptySlot();
		this.m_buyButton.interactable = (flag && flag2);
		if (!flag)
		{
			component.m_text = Localization.instance.Localize("$msg_missingrequirement");
			return;
		}
		if (!flag2)
		{
			component.m_text = Localization.instance.Localize("$inventory_full");
			return;
		}
		component.m_text = "";
	}

	private void UpdateRecipeGamepadInput()
	{
		if (this.m_itemList.Count > 0)
		{
			if (ZInput.GetButtonDown("JoyLStickDown"))
			{
				this.SelectItem(Mathf.Min(this.m_itemList.Count - 1, this.GetSelectedItemIndex() + 1), true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp"))
			{
				this.SelectItem(Mathf.Max(0, this.GetSelectedItemIndex() - 1), true);
			}
		}
	}

	private static StoreGui m_instance;

	public GameObject m_rootPanel;

	public Button m_buyButton;

	public Button m_sellButton;

	public RectTransform m_listRoot;

	public GameObject m_listElement;

	public Scrollbar m_listScroll;

	public ScrollRectEnsureVisible m_itemEnsureVisible;

	public Text m_coinText;

	public EffectList m_buyEffects = new EffectList();

	public EffectList m_sellEffects = new EffectList();

	public float m_hideDistance = 5f;

	public float m_itemSpacing = 64f;

	public ItemDrop m_coinPrefab;

	private List<GameObject> m_itemList = new List<GameObject>();

	private Trader.TradeItem m_selectedItem;

	private Trader m_trader;

	private float m_itemlistBaseSize;

	private int m_hiddenFrames;

	private List<ItemDrop.ItemData> m_tempItems = new List<ItemDrop.ItemData>();
}
