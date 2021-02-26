using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryGrid : MonoBehaviour
{
	protected void Awake()
	{
	}

	public void ResetView()
	{
		RectTransform rectTransform = base.transform as RectTransform;
		if (this.m_gridRoot.rect.height > rectTransform.rect.height)
		{
			this.m_gridRoot.pivot = new Vector2(this.m_gridRoot.pivot.x, 1f);
		}
		else
		{
			this.m_gridRoot.pivot = new Vector2(this.m_gridRoot.pivot.x, 0.5f);
		}
		this.m_gridRoot.anchoredPosition = new Vector2(0f, 0f);
	}

	public void UpdateInventory(Inventory inventory, Player player, ItemDrop.ItemData dragItem)
	{
		this.m_inventory = inventory;
		this.UpdateGamepad();
		this.UpdateGui(player, dragItem);
	}

	private void UpdateGamepad()
	{
		if (!this.m_uiGroup.IsActive())
		{
			return;
		}
		if (ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyLStickLeft"))
		{
			this.m_selected.x = Mathf.Max(0, this.m_selected.x - 1);
		}
		if (ZInput.GetButtonDown("JoyDPadRight") || ZInput.GetButtonDown("JoyLStickRight"))
		{
			this.m_selected.x = Mathf.Min(this.m_width - 1, this.m_selected.x + 1);
		}
		if (ZInput.GetButtonDown("JoyDPadUp") || ZInput.GetButtonDown("JoyLStickUp"))
		{
			this.m_selected.y = Mathf.Max(0, this.m_selected.y - 1);
		}
		if (ZInput.GetButtonDown("JoyDPadDown") || ZInput.GetButtonDown("JoyLStickDown"))
		{
			this.m_selected.y = Mathf.Min(this.m_width - 1, this.m_selected.y + 1);
		}
		if (ZInput.GetButtonDown("JoyButtonA"))
		{
			InventoryGrid.Modifier arg = InventoryGrid.Modifier.Select;
			if (ZInput.GetButton("JoyLTrigger"))
			{
				arg = InventoryGrid.Modifier.Split;
			}
			if (ZInput.GetButton("JoyRTrigger"))
			{
				arg = InventoryGrid.Modifier.Move;
			}
			ItemDrop.ItemData gamepadSelectedItem = this.GetGamepadSelectedItem();
			this.m_onSelected(this, gamepadSelectedItem, this.m_selected, arg);
		}
		if (ZInput.GetButtonDown("JoyButtonX"))
		{
			ItemDrop.ItemData gamepadSelectedItem2 = this.GetGamepadSelectedItem();
			this.m_onRightClick(this, gamepadSelectedItem2, this.m_selected);
		}
	}

	private void UpdateGui(Player player, ItemDrop.ItemData dragItem)
	{
		RectTransform rectTransform = base.transform as RectTransform;
		int width = this.m_inventory.GetWidth();
		int height = this.m_inventory.GetHeight();
		if (this.m_selected.x >= width - 1)
		{
			this.m_selected.x = width - 1;
		}
		if (this.m_selected.y >= height - 1)
		{
			this.m_selected.y = height - 1;
		}
		if (this.m_width != width || this.m_height != height)
		{
			this.m_width = width;
			this.m_height = height;
			foreach (InventoryGrid.Element element in this.m_elements)
			{
				UnityEngine.Object.Destroy(element.m_go);
			}
			this.m_elements.Clear();
			Vector2 widgetSize = this.GetWidgetSize();
			Vector2 a = new Vector2(rectTransform.rect.width / 2f, 0f) - new Vector2(widgetSize.x, 0f) * 0.5f;
			for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					Vector2 b = new Vector3((float)j * this.m_elementSpace, (float)i * -this.m_elementSpace);
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, this.m_gridRoot);
					(gameObject.transform as RectTransform).anchoredPosition = a + b;
					UIInputHandler componentInChildren = gameObject.GetComponentInChildren<UIInputHandler>();
					componentInChildren.m_onRightDown = (Action<UIInputHandler>)Delegate.Combine(componentInChildren.m_onRightDown, new Action<UIInputHandler>(this.OnRightClick));
					componentInChildren.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(componentInChildren.m_onLeftDown, new Action<UIInputHandler>(this.OnLeftClick));
					Text component = gameObject.transform.Find("binding").GetComponent<Text>();
					if (player && i == 0)
					{
						component.text = (j + 1).ToString();
					}
					else
					{
						component.enabled = false;
					}
					InventoryGrid.Element element2 = new InventoryGrid.Element();
					element2.m_pos = new Vector2i(j, i);
					element2.m_go = gameObject;
					element2.m_icon = gameObject.transform.Find("icon").GetComponent<Image>();
					element2.m_amount = gameObject.transform.Find("amount").GetComponent<Text>();
					element2.m_quality = gameObject.transform.Find("quality").GetComponent<Text>();
					element2.m_equiped = gameObject.transform.Find("equiped").GetComponent<Image>();
					element2.m_queued = gameObject.transform.Find("queued").GetComponent<Image>();
					element2.m_noteleport = gameObject.transform.Find("noteleport").GetComponent<Image>();
					element2.m_selected = gameObject.transform.Find("selected").gameObject;
					element2.m_tooltip = gameObject.GetComponent<UITooltip>();
					element2.m_durability = gameObject.transform.Find("durability").GetComponent<GuiBar>();
					this.m_elements.Add(element2);
				}
			}
		}
		foreach (InventoryGrid.Element element3 in this.m_elements)
		{
			element3.m_used = false;
		}
		bool flag = this.m_uiGroup.IsActive() && ZInput.IsGamepadActive();
		foreach (ItemDrop.ItemData itemData in this.m_inventory.GetAllItems())
		{
			InventoryGrid.Element element4 = this.GetElement(itemData.m_gridPos.x, itemData.m_gridPos.y, width);
			element4.m_used = true;
			element4.m_icon.enabled = true;
			element4.m_icon.sprite = itemData.GetIcon();
			element4.m_icon.color = ((itemData == dragItem) ? Color.grey : Color.white);
			element4.m_durability.gameObject.SetActive(itemData.m_shared.m_useDurability);
			if (itemData.m_shared.m_useDurability)
			{
				if (itemData.m_durability <= 0f)
				{
					element4.m_durability.SetValue(1f);
					element4.m_durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
				}
				else
				{
					element4.m_durability.SetValue(itemData.GetDurabilityPercentage());
					element4.m_durability.ResetColor();
				}
			}
			element4.m_equiped.enabled = (player && itemData.m_equiped);
			element4.m_queued.enabled = (player && player.IsItemQueued(itemData));
			element4.m_noteleport.enabled = !itemData.m_shared.m_teleportable;
			if (dragItem == null)
			{
				this.CreateItemTooltip(itemData, element4.m_tooltip);
			}
			element4.m_quality.enabled = (itemData.m_shared.m_maxQuality > 1);
			if (itemData.m_shared.m_maxQuality > 1)
			{
				element4.m_quality.text = itemData.m_quality.ToString();
			}
			element4.m_amount.enabled = (itemData.m_shared.m_maxStackSize > 1);
			if (itemData.m_shared.m_maxStackSize > 1)
			{
				element4.m_amount.text = itemData.m_stack.ToString() + "/" + itemData.m_shared.m_maxStackSize.ToString();
			}
		}
		foreach (InventoryGrid.Element element5 in this.m_elements)
		{
			element5.m_selected.SetActive(flag && element5.m_pos == this.m_selected);
			if (!element5.m_used)
			{
				element5.m_durability.gameObject.SetActive(false);
				element5.m_icon.enabled = false;
				element5.m_amount.enabled = false;
				element5.m_quality.enabled = false;
				element5.m_equiped.enabled = false;
				element5.m_queued.enabled = false;
				element5.m_noteleport.enabled = false;
				element5.m_tooltip.m_text = "";
				element5.m_tooltip.m_topic = "";
			}
		}
		float size = (float)height * this.m_elementSpace;
		this.m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
	}

	private void CreateItemTooltip(ItemDrop.ItemData item, UITooltip tooltip)
	{
		tooltip.Set(item.m_shared.m_name, item.GetTooltip());
	}

	public Vector2 GetWidgetSize()
	{
		return new Vector2((float)this.m_width * this.m_elementSpace, (float)this.m_height * this.m_elementSpace);
	}

	private void OnRightClick(UIInputHandler element)
	{
		GameObject gameObject = element.gameObject;
		Vector2i buttonPos = this.GetButtonPos(gameObject);
		ItemDrop.ItemData itemAt = this.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
		if (this.m_onRightClick != null)
		{
			this.m_onRightClick(this, itemAt, buttonPos);
		}
	}

	private void OnLeftClick(UIInputHandler clickHandler)
	{
		GameObject gameObject = clickHandler.gameObject;
		Vector2i buttonPos = this.GetButtonPos(gameObject);
		ItemDrop.ItemData itemAt = this.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
		InventoryGrid.Modifier arg = InventoryGrid.Modifier.Select;
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
		{
			arg = InventoryGrid.Modifier.Split;
		}
		if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
		{
			arg = InventoryGrid.Modifier.Move;
		}
		if (this.m_onSelected != null)
		{
			this.m_onSelected(this, itemAt, buttonPos, arg);
		}
	}

	private InventoryGrid.Element GetElement(int x, int y, int width)
	{
		int index = y * width + x;
		return this.m_elements[index];
	}

	private Vector2i GetButtonPos(GameObject go)
	{
		for (int i = 0; i < this.m_elements.Count; i++)
		{
			if (this.m_elements[i].m_go == go)
			{
				int num = i / this.m_width;
				return new Vector2i(i - num * this.m_width, num);
			}
		}
		return new Vector2i(-1, -1);
	}

	public bool DropItem(Inventory fromInventory, ItemDrop.ItemData item, int amount, Vector2i pos)
	{
		ItemDrop.ItemData itemAt = this.m_inventory.GetItemAt(pos.x, pos.y);
		if (itemAt == item)
		{
			return true;
		}
		if (itemAt != null && (itemAt.m_shared.m_name != item.m_shared.m_name || (item.m_shared.m_maxQuality > 1 && itemAt.m_quality != item.m_quality) || itemAt.m_shared.m_maxStackSize == 1) && item.m_stack == amount)
		{
			fromInventory.RemoveItem(item);
			fromInventory.MoveItemToThis(this.m_inventory, itemAt, itemAt.m_stack, item.m_gridPos.x, item.m_gridPos.y);
			this.m_inventory.MoveItemToThis(fromInventory, item, amount, pos.x, pos.y);
			return true;
		}
		return this.m_inventory.MoveItemToThis(fromInventory, item, amount, pos.x, pos.y);
	}

	public ItemDrop.ItemData GetItem(Vector2i cursorPosition)
	{
		foreach (InventoryGrid.Element element in this.m_elements)
		{
			if (RectTransformUtility.RectangleContainsScreenPoint(element.m_go.transform as RectTransform, cursorPosition.ToVector2()))
			{
				Vector2i buttonPos = this.GetButtonPos(element.m_go);
				return this.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
			}
		}
		return null;
	}

	public Inventory GetInventory()
	{
		return this.m_inventory;
	}

	public void SetSelection(Vector2i pos)
	{
		this.m_selected = pos;
	}

	public ItemDrop.ItemData GetGamepadSelectedItem()
	{
		if (!this.m_uiGroup.IsActive())
		{
			return null;
		}
		return this.m_inventory.GetItemAt(this.m_selected.x, this.m_selected.y);
	}

	public RectTransform GetGamepadSelectedElement()
	{
		if (!this.m_uiGroup.IsActive())
		{
			return null;
		}
		if (this.m_selected.x < 0 || this.m_selected.x >= this.m_width || this.m_selected.y < 0 || this.m_selected.y >= this.m_height)
		{
			return null;
		}
		return this.GetElement(this.m_selected.x, this.m_selected.y, this.m_width).m_go.transform as RectTransform;
	}

	public Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier> m_onSelected;

	public Action<InventoryGrid, ItemDrop.ItemData, Vector2i> m_onRightClick;

	public GameObject m_elementPrefab;

	public RectTransform m_gridRoot;

	public Scrollbar m_scrollbar;

	public UIGroupHandler m_uiGroup;

	public float m_elementSpace = 10f;

	private int m_width = 4;

	private int m_height = 4;

	private Vector2i m_selected = new Vector2i(0, 0);

	private Inventory m_inventory;

	private List<InventoryGrid.Element> m_elements = new List<InventoryGrid.Element>();

	private class Element
	{
		public Vector2i m_pos;

		public GameObject m_go;

		public Image m_icon;

		public Text m_amount;

		public Text m_quality;

		public Image m_equiped;

		public Image m_queued;

		public GameObject m_selected;

		public Image m_noteleport;

		public UITooltip m_tooltip;

		public GuiBar m_durability;

		public bool m_used;
	}

	public enum Modifier
	{
		Select,
		Split,
		Move
	}
}
