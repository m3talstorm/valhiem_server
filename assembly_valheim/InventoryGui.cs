using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InventoryGui : MonoBehaviour
{
	public static InventoryGui instance
	{
		get
		{
			return InventoryGui.m_instance;
		}
	}

	private void Awake()
	{
		InventoryGui.m_instance = this;
		this.m_animator = base.GetComponent<Animator>();
		this.m_inventoryRoot.gameObject.SetActive(true);
		this.m_container.gameObject.SetActive(false);
		this.m_splitPanel.gameObject.SetActive(false);
		this.m_trophiesPanel.SetActive(false);
		this.m_variantDialog.gameObject.SetActive(false);
		this.m_skillsDialog.gameObject.SetActive(false);
		this.m_textsDialog.gameObject.SetActive(false);
		this.m_playerGrid = this.m_player.GetComponentInChildren<InventoryGrid>();
		this.m_containerGrid = this.m_container.GetComponentInChildren<InventoryGrid>();
		InventoryGrid playerGrid = this.m_playerGrid;
		playerGrid.m_onSelected = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>)Delegate.Combine(playerGrid.m_onSelected, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>(this.OnSelectedItem));
		InventoryGrid playerGrid2 = this.m_playerGrid;
		playerGrid2.m_onRightClick = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i>)Delegate.Combine(playerGrid2.m_onRightClick, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i>(this.OnRightClickItem));
		InventoryGrid containerGrid = this.m_containerGrid;
		containerGrid.m_onSelected = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>)Delegate.Combine(containerGrid.m_onSelected, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>(this.OnSelectedItem));
		InventoryGrid containerGrid2 = this.m_containerGrid;
		containerGrid2.m_onRightClick = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i>)Delegate.Combine(containerGrid2.m_onRightClick, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i>(this.OnRightClickItem));
		this.m_craftButton.onClick.AddListener(new UnityAction(this.OnCraftPressed));
		this.m_craftCancelButton.onClick.AddListener(new UnityAction(this.OnCraftCancelPressed));
		this.m_dropButton.onClick.AddListener(new UnityAction(this.OnDropOutside));
		this.m_takeAllButton.onClick.AddListener(new UnityAction(this.OnTakeAll));
		this.m_repairButton.onClick.AddListener(new UnityAction(this.OnRepairPressed));
		this.m_splitSlider.onValueChanged.AddListener(new UnityAction<float>(this.OnSplitSliderChanged));
		this.m_splitCancelButton.onClick.AddListener(new UnityAction(this.OnSplitCancel));
		this.m_splitOkButton.onClick.AddListener(new UnityAction(this.OnSplitOk));
		VariantDialog variantDialog = this.m_variantDialog;
		variantDialog.m_selected = (Action<int>)Delegate.Combine(variantDialog.m_selected, new Action<int>(this.OnVariantSelected));
		this.m_recipeListBaseSize = this.m_recipeListRoot.rect.height;
		this.m_trophieListBaseSize = this.m_trophieListRoot.rect.height;
		this.m_minStationLevelBasecolor = this.m_minStationLevelText.color;
		this.m_tabCraft.interactable = false;
		this.m_tabUpgrade.interactable = true;
	}

	private void OnDestroy()
	{
		InventoryGui.m_instance = null;
	}

	private void Update()
	{
		bool @bool = this.m_animator.GetBool("visible");
		if (!@bool)
		{
			this.m_hiddenFrames++;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene() || localPlayer.IsTeleporting())
		{
			this.Hide();
			return;
		}
		if (this.m_craftTimer < 0f && (Chat.instance == null || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !Menu.IsVisible() && TextViewer.instance && !TextViewer.instance.IsVisible() && !localPlayer.InCutscene() && !GameCamera.InFreeFly() && !Minimap.IsOpen())
		{
			if (this.m_trophiesPanel.activeSelf && (ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape)))
			{
				this.m_trophiesPanel.SetActive(false);
			}
			else if (this.m_skillsDialog.gameObject.activeSelf && (ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape)))
			{
				this.m_skillsDialog.gameObject.SetActive(false);
			}
			else if (this.m_textsDialog.gameObject.activeSelf && (ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape)))
			{
				this.m_textsDialog.gameObject.SetActive(false);
			}
			else if (@bool)
			{
				if (ZInput.GetButtonDown("Inventory") || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("JoyButtonY") || Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("Use"))
				{
					ZInput.ResetButtonStatus("Inventory");
					ZInput.ResetButtonStatus("JoyButtonB");
					ZInput.ResetButtonStatus("JoyButtonY");
					ZInput.ResetButtonStatus("Use");
					this.Hide();
				}
			}
			else if (ZInput.GetButtonDown("Inventory") || ZInput.GetButtonDown("JoyButtonY"))
			{
				ZInput.ResetButtonStatus("Inventory");
				ZInput.ResetButtonStatus("JoyButtonY");
				localPlayer.ShowTutorial("inventory", true);
				this.Show(null);
			}
		}
		if (@bool)
		{
			this.m_hiddenFrames = 0;
			this.UpdateGamepad();
			this.UpdateInventory(localPlayer);
			this.UpdateContainer(localPlayer);
			this.UpdateItemDrag();
			this.UpdateCharacterStats(localPlayer);
			this.UpdateInventoryWeight(localPlayer);
			this.UpdateContainerWeight();
			this.UpdateRecipe(localPlayer, Time.deltaTime);
			this.UpdateRepair();
		}
	}

	private void UpdateGamepad()
	{
		if (!this.m_inventoryGroup.IsActive())
		{
			return;
		}
		if (ZInput.GetButtonDown("JoyTabLeft"))
		{
			this.SetActiveGroup(this.m_activeGroup - 1);
		}
		if (ZInput.GetButtonDown("JoyTabRight"))
		{
			this.SetActiveGroup(this.m_activeGroup + 1);
		}
		if (this.m_activeGroup == 0 && !this.IsContainerOpen())
		{
			this.SetActiveGroup(1);
		}
		if (this.m_activeGroup == 3)
		{
			this.UpdateRecipeGamepadInput();
		}
	}

	private void SetActiveGroup(int index)
	{
		index = Mathf.Clamp(index, 0, this.m_uiGroups.Length - 1);
		this.m_activeGroup = index;
		for (int i = 0; i < this.m_uiGroups.Length; i++)
		{
			this.m_uiGroups[i].SetActive(i == this.m_activeGroup);
		}
	}

	private void UpdateCharacterStats(Player player)
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		this.m_playerName.text = playerProfile.GetName();
		float bodyArmor = player.GetBodyArmor();
		this.m_armor.text = bodyArmor.ToString();
		this.m_pvp.interactable = player.CanSwitchPVP();
		player.SetPVP(this.m_pvp.isOn);
	}

	private void UpdateInventoryWeight(Player player)
	{
		int num = Mathf.CeilToInt(player.GetInventory().GetTotalWeight());
		int num2 = Mathf.CeilToInt(player.GetMaxCarryWeight());
		if (num <= num2)
		{
			this.m_weight.text = num + "/" + num2;
			return;
		}
		if (Mathf.Sin(Time.time * 10f) > 0f)
		{
			this.m_weight.text = string.Concat(new object[]
			{
				"<color=red>",
				num,
				"</color>/",
				num2
			});
			return;
		}
		this.m_weight.text = num + "/" + num2;
	}

	private void UpdateContainerWeight()
	{
		if (this.m_currentContainer == null)
		{
			return;
		}
		int num = Mathf.CeilToInt(this.m_currentContainer.GetInventory().GetTotalWeight());
		this.m_containerWeight.text = num.ToString();
	}

	private void UpdateInventory(Player player)
	{
		Inventory inventory = player.GetInventory();
		this.m_playerGrid.UpdateInventory(inventory, player, this.m_dragItem);
	}

	private void UpdateContainer(Player player)
	{
		if (!this.m_animator.GetBool("visible"))
		{
			return;
		}
		if (this.m_currentContainer && this.m_currentContainer.IsOwner())
		{
			this.m_currentContainer.SetInUse(true);
			this.m_container.gameObject.SetActive(true);
			this.m_containerGrid.UpdateInventory(this.m_currentContainer.GetInventory(), null, this.m_dragItem);
			this.m_containerName.text = Localization.instance.Localize(this.m_currentContainer.GetInventory().GetName());
			if (this.m_firstContainerUpdate)
			{
				this.m_containerGrid.ResetView();
				this.m_firstContainerUpdate = false;
			}
			if (Vector3.Distance(this.m_currentContainer.transform.position, player.transform.position) > this.m_autoCloseDistance)
			{
				this.CloseContainer();
				return;
			}
		}
		else
		{
			this.m_container.gameObject.SetActive(false);
		}
	}

	private RectTransform GetSelectedGamepadElement()
	{
		RectTransform gamepadSelectedElement = this.m_playerGrid.GetGamepadSelectedElement();
		if (gamepadSelectedElement)
		{
			return gamepadSelectedElement;
		}
		if (this.m_container.gameObject.activeSelf)
		{
			return this.m_containerGrid.GetGamepadSelectedElement();
		}
		return null;
	}

	private void UpdateItemDrag()
	{
		if (this.m_dragGo)
		{
			if (ZInput.IsGamepadActive() && !ZInput.IsMouseActive())
			{
				RectTransform selectedGamepadElement = this.GetSelectedGamepadElement();
				if (selectedGamepadElement)
				{
					Vector3[] array = new Vector3[4];
					selectedGamepadElement.GetWorldCorners(array);
					this.m_dragGo.transform.position = array[2] + new Vector3(0f, 32f, 0f);
				}
				else
				{
					this.m_dragGo.transform.position = new Vector3(-99999f, 0f, 0f);
				}
			}
			else
			{
				this.m_dragGo.transform.position = Input.mousePosition;
			}
			Image component = this.m_dragGo.transform.Find("icon").GetComponent<Image>();
			Text component2 = this.m_dragGo.transform.Find("name").GetComponent<Text>();
			Text component3 = this.m_dragGo.transform.Find("amount").GetComponent<Text>();
			component.sprite = this.m_dragItem.GetIcon();
			component2.text = this.m_dragItem.m_shared.m_name;
			component3.text = ((this.m_dragAmount > 1) ? this.m_dragAmount.ToString() : "");
			if (Input.GetMouseButton(1))
			{
				this.SetupDragItem(null, null, 1);
			}
		}
	}

	private void OnTakeAll()
	{
		if (Player.m_localPlayer.IsTeleporting())
		{
			return;
		}
		if (this.m_currentContainer)
		{
			this.SetupDragItem(null, null, 1);
			Inventory inventory = this.m_currentContainer.GetInventory();
			Player.m_localPlayer.GetInventory().MoveAll(inventory);
		}
	}

	private void OnDropOutside()
	{
		if (this.m_dragGo)
		{
			ZLog.Log("Drop item " + this.m_dragItem.m_shared.m_name);
			if (!this.m_dragInventory.ContainsItem(this.m_dragItem))
			{
				this.SetupDragItem(null, null, 1);
				return;
			}
			if (Player.m_localPlayer.DropItem(this.m_dragInventory, this.m_dragItem, this.m_dragAmount))
			{
				this.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
				this.SetupDragItem(null, null, 1);
				this.UpdateCraftingPanel(false);
			}
		}
	}

	private void OnRightClickItem(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos)
	{
		if (item != null && Player.m_localPlayer)
		{
			Player.m_localPlayer.UseItem(grid.GetInventory(), item, true);
		}
	}

	private void OnSelectedItem(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer.IsTeleporting())
		{
			return;
		}
		if (this.m_dragGo)
		{
			this.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
			bool flag = localPlayer.IsItemEquiped(this.m_dragItem);
			bool flag2 = item != null && localPlayer.IsItemEquiped(item);
			Vector2i gridPos = this.m_dragItem.m_gridPos;
			if ((this.m_dragItem.m_shared.m_questItem || (item != null && item.m_shared.m_questItem)) && this.m_dragInventory != grid.GetInventory())
			{
				return;
			}
			if (!this.m_dragInventory.ContainsItem(this.m_dragItem))
			{
				this.SetupDragItem(null, null, 1);
				return;
			}
			localPlayer.RemoveFromEquipQueue(item);
			localPlayer.RemoveFromEquipQueue(this.m_dragItem);
			localPlayer.UnequipItem(this.m_dragItem, false);
			localPlayer.UnequipItem(item, false);
			bool flag3 = grid.DropItem(this.m_dragInventory, this.m_dragItem, this.m_dragAmount, pos);
			if (this.m_dragItem.m_stack < this.m_dragAmount)
			{
				this.m_dragAmount = this.m_dragItem.m_stack;
			}
			if (flag)
			{
				ItemDrop.ItemData itemAt = grid.GetInventory().GetItemAt(pos.x, pos.y);
				if (itemAt != null)
				{
					localPlayer.EquipItem(itemAt, false);
				}
				if (localPlayer.GetInventory().ContainsItem(this.m_dragItem))
				{
					localPlayer.EquipItem(this.m_dragItem, false);
				}
			}
			if (flag2)
			{
				ItemDrop.ItemData itemAt2 = this.m_dragInventory.GetItemAt(gridPos.x, gridPos.y);
				if (itemAt2 != null)
				{
					localPlayer.EquipItem(itemAt2, false);
				}
				if (localPlayer.GetInventory().ContainsItem(item))
				{
					localPlayer.EquipItem(item, false);
				}
			}
			if (flag3)
			{
				this.SetupDragItem(null, null, 1);
				this.UpdateCraftingPanel(false);
				return;
			}
		}
		else if (item != null)
		{
			if (mod == InventoryGrid.Modifier.Move)
			{
				if (item.m_shared.m_questItem)
				{
					return;
				}
				if (this.m_currentContainer != null)
				{
					localPlayer.RemoveFromEquipQueue(item);
					localPlayer.UnequipItem(item, true);
					if (grid.GetInventory() == this.m_currentContainer.GetInventory())
					{
						localPlayer.GetInventory().MoveItemToThis(grid.GetInventory(), item);
					}
					else
					{
						this.m_currentContainer.GetInventory().MoveItemToThis(localPlayer.GetInventory(), item);
					}
					this.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
					return;
				}
				if (Player.m_localPlayer.DropItem(localPlayer.GetInventory(), item, item.m_stack))
				{
					this.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
					return;
				}
			}
			else
			{
				if (mod == InventoryGrid.Modifier.Split && item.m_stack > 1)
				{
					this.ShowSplitDialog(item, grid.GetInventory());
					return;
				}
				this.SetupDragItem(item, grid.GetInventory(), item.m_stack);
			}
		}
	}

	public static bool IsVisible()
	{
		return InventoryGui.m_instance && InventoryGui.m_instance.m_hiddenFrames <= 1;
	}

	public bool IsContainerOpen()
	{
		return this.m_currentContainer != null;
	}

	public void Show(Container container)
	{
		Hud.HidePieceSelection();
		this.m_animator.SetBool("visible", true);
		this.SetActiveGroup(1);
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			this.SetupCrafting();
		}
		this.m_currentContainer = container;
		this.m_hiddenFrames = 0;
		if (localPlayer)
		{
			this.m_openInventoryEffects.Create(localPlayer.transform.position, Quaternion.identity, null, 1f);
		}
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "Inventory", 0L);
	}

	public void Hide()
	{
		if (!this.m_animator.GetBool("visible"))
		{
			return;
		}
		this.m_craftTimer = -1f;
		this.m_animator.SetBool("visible", false);
		this.m_trophiesPanel.SetActive(false);
		this.m_variantDialog.gameObject.SetActive(false);
		this.m_skillsDialog.gameObject.SetActive(false);
		this.m_textsDialog.gameObject.SetActive(false);
		this.m_splitPanel.gameObject.SetActive(false);
		this.SetupDragItem(null, null, 1);
		if (this.m_currentContainer)
		{
			this.m_currentContainer.SetInUse(false);
			this.m_currentContainer = null;
		}
		if (Player.m_localPlayer)
		{
			this.m_closeInventoryEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity, null, 1f);
		}
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Exit", "Inventory", 0L);
	}

	private void CloseContainer()
	{
		if (this.m_dragInventory != null && this.m_dragInventory != Player.m_localPlayer.GetInventory())
		{
			this.SetupDragItem(null, null, 1);
		}
		if (this.m_currentContainer)
		{
			this.m_currentContainer.SetInUse(false);
			this.m_currentContainer = null;
		}
		this.m_splitPanel.gameObject.SetActive(false);
		this.m_firstContainerUpdate = true;
		this.m_container.gameObject.SetActive(false);
	}

	private void SetupCrafting()
	{
		this.UpdateCraftingPanel(true);
	}

	private void UpdateCraftingPanel(bool focusView = false)
	{
		Player localPlayer = Player.m_localPlayer;
		if (!localPlayer.GetCurrentCraftingStation() && !localPlayer.NoCostCheat())
		{
			this.m_tabCraft.interactable = false;
			this.m_tabUpgrade.interactable = true;
			this.m_tabUpgrade.gameObject.SetActive(false);
		}
		else
		{
			this.m_tabUpgrade.gameObject.SetActive(true);
		}
		List<Recipe> recipes = new List<Recipe>();
		localPlayer.GetAvailableRecipes(ref recipes);
		this.UpdateRecipeList(recipes);
		if (this.m_availableRecipes.Count <= 0)
		{
			this.SetRecipe(-1, focusView);
			return;
		}
		if (this.m_selectedRecipe.Key != null)
		{
			int selectedRecipeIndex = this.GetSelectedRecipeIndex();
			this.SetRecipe(selectedRecipeIndex, focusView);
			return;
		}
		this.SetRecipe(0, focusView);
	}

	private void UpdateRecipeList(List<Recipe> recipes)
	{
		Player localPlayer = Player.m_localPlayer;
		this.m_availableRecipes.Clear();
		foreach (GameObject obj in this.m_recipeList)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_recipeList.Clear();
		if (this.InCraftTab())
		{
			bool[] array = new bool[recipes.Count];
			for (int i = 0; i < recipes.Count; i++)
			{
				Recipe recipe = recipes[i];
				array[i] = localPlayer.HaveRequirements(recipe, false, 1);
			}
			for (int j = 0; j < recipes.Count; j++)
			{
				if (array[j])
				{
					this.AddRecipeToList(localPlayer, recipes[j], null, true);
				}
			}
			for (int k = 0; k < recipes.Count; k++)
			{
				if (!array[k])
				{
					this.AddRecipeToList(localPlayer, recipes[k], null, false);
				}
			}
		}
		else
		{
			List<KeyValuePair<Recipe, ItemDrop.ItemData>> list = new List<KeyValuePair<Recipe, ItemDrop.ItemData>>();
			List<KeyValuePair<Recipe, ItemDrop.ItemData>> list2 = new List<KeyValuePair<Recipe, ItemDrop.ItemData>>();
			for (int l = 0; l < recipes.Count; l++)
			{
				Recipe recipe2 = recipes[l];
				if (recipe2.m_item.m_itemData.m_shared.m_maxQuality > 1)
				{
					this.m_tempItemList.Clear();
					localPlayer.GetInventory().GetAllItems(recipe2.m_item.m_itemData.m_shared.m_name, this.m_tempItemList);
					foreach (ItemDrop.ItemData itemData in this.m_tempItemList)
					{
						if (itemData.m_quality < itemData.m_shared.m_maxQuality && localPlayer.HaveRequirements(recipe2, false, itemData.m_quality + 1))
						{
							list.Add(new KeyValuePair<Recipe, ItemDrop.ItemData>(recipe2, itemData));
						}
						else
						{
							list2.Add(new KeyValuePair<Recipe, ItemDrop.ItemData>(recipe2, itemData));
						}
					}
				}
			}
			foreach (KeyValuePair<Recipe, ItemDrop.ItemData> keyValuePair in list)
			{
				this.AddRecipeToList(localPlayer, keyValuePair.Key, keyValuePair.Value, true);
			}
			foreach (KeyValuePair<Recipe, ItemDrop.ItemData> keyValuePair2 in list2)
			{
				this.AddRecipeToList(localPlayer, keyValuePair2.Key, keyValuePair2.Value, false);
			}
		}
		float num = (float)this.m_recipeList.Count * this.m_recipeListSpace;
		num = Mathf.Max(this.m_recipeListBaseSize, num);
		this.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
	}

	private void AddRecipeToList(Player player, Recipe recipe, ItemDrop.ItemData item, bool canCraft)
	{
		int count = this.m_recipeList.Count;
		GameObject element = UnityEngine.Object.Instantiate<GameObject>(this.m_recipeElementPrefab, this.m_recipeListRoot);
		element.SetActive(true);
		(element.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)count * -this.m_recipeListSpace);
		Image component = element.transform.Find("icon").GetComponent<Image>();
		component.sprite = recipe.m_item.m_itemData.GetIcon();
		component.color = (canCraft ? Color.white : new Color(1f, 0f, 1f, 0f));
		Text component2 = element.transform.Find("name").GetComponent<Text>();
		string text = Localization.instance.Localize(recipe.m_item.m_itemData.m_shared.m_name);
		if (recipe.m_amount > 1)
		{
			text = text + " x" + recipe.m_amount;
		}
		component2.text = text;
		component2.color = (canCraft ? Color.white : new Color(0.66f, 0.66f, 0.66f, 1f));
		GuiBar component3 = element.transform.Find("Durability").GetComponent<GuiBar>();
		if (item != null && item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability())
		{
			component3.gameObject.SetActive(true);
			component3.SetValue(item.GetDurabilityPercentage());
		}
		else
		{
			component3.gameObject.SetActive(false);
		}
		Text component4 = element.transform.Find("QualityLevel").GetComponent<Text>();
		if (item != null)
		{
			component4.gameObject.SetActive(true);
			component4.text = item.m_quality.ToString();
		}
		else
		{
			component4.gameObject.SetActive(false);
		}
		element.GetComponent<Button>().onClick.AddListener(delegate
		{
			this.OnSelectedRecipe(element);
		});
		this.m_recipeList.Add(element);
		this.m_availableRecipes.Add(new KeyValuePair<Recipe, ItemDrop.ItemData>(recipe, item));
	}

	private void OnSelectedRecipe(GameObject button)
	{
		int index = this.FindSelectedRecipe(button);
		this.SetRecipe(index, false);
	}

	private void UpdateRecipeGamepadInput()
	{
		if (this.m_availableRecipes.Count > 0)
		{
			if (ZInput.GetButtonDown("JoyLStickDown"))
			{
				this.SetRecipe(Mathf.Min(this.m_availableRecipes.Count - 1, this.GetSelectedRecipeIndex() + 1), true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp"))
			{
				this.SetRecipe(Mathf.Max(0, this.GetSelectedRecipeIndex() - 1), true);
			}
		}
	}

	private int GetSelectedRecipeIndex()
	{
		int result = 0;
		for (int i = 0; i < this.m_availableRecipes.Count; i++)
		{
			if (this.m_availableRecipes[i].Key == this.m_selectedRecipe.Key && this.m_availableRecipes[i].Value == this.m_selectedRecipe.Value)
			{
				result = i;
			}
		}
		return result;
	}

	private void SetRecipe(int index, bool center)
	{
		ZLog.Log("Setting selected recipe " + index);
		for (int i = 0; i < this.m_recipeList.Count; i++)
		{
			bool active = i == index;
			this.m_recipeList[i].transform.Find("selected").gameObject.SetActive(active);
		}
		if (center && index >= 0)
		{
			this.m_recipeEnsureVisible.CenterOnItem(this.m_recipeList[index].transform as RectTransform);
		}
		if (index < 0)
		{
			this.m_selectedRecipe = new KeyValuePair<Recipe, ItemDrop.ItemData>(null, null);
			this.m_selectedVariant = 0;
			return;
		}
		KeyValuePair<Recipe, ItemDrop.ItemData> selectedRecipe = this.m_availableRecipes[index];
		if (selectedRecipe.Key != this.m_selectedRecipe.Key || selectedRecipe.Value != this.m_selectedRecipe.Value)
		{
			this.m_selectedRecipe = selectedRecipe;
			this.m_selectedVariant = 0;
		}
	}

	private void UpdateRecipe(Player player, float dt)
	{
		CraftingStation currentCraftingStation = player.GetCurrentCraftingStation();
		if (currentCraftingStation)
		{
			this.m_craftingStationName.text = Localization.instance.Localize(currentCraftingStation.m_name);
			this.m_craftingStationIcon.gameObject.SetActive(true);
			this.m_craftingStationIcon.sprite = currentCraftingStation.m_icon;
			int level = currentCraftingStation.GetLevel();
			this.m_craftingStationLevel.text = level.ToString();
			this.m_craftingStationLevelRoot.gameObject.SetActive(true);
		}
		else
		{
			this.m_craftingStationName.text = Localization.instance.Localize("$hud_crafting");
			this.m_craftingStationIcon.gameObject.SetActive(false);
			this.m_craftingStationLevelRoot.gameObject.SetActive(false);
		}
		if (this.m_selectedRecipe.Key)
		{
			this.m_recipeIcon.enabled = true;
			this.m_recipeName.enabled = true;
			this.m_recipeDecription.enabled = true;
			ItemDrop.ItemData value = this.m_selectedRecipe.Value;
			int num = (value != null) ? (value.m_quality + 1) : 1;
			bool flag = num <= this.m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_maxQuality;
			int num2 = (value != null) ? value.m_variant : this.m_selectedVariant;
			this.m_recipeIcon.sprite = this.m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_icons[num2];
			string text = Localization.instance.Localize(this.m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_name);
			if (this.m_selectedRecipe.Key.m_amount > 1)
			{
				text = text + " x" + this.m_selectedRecipe.Key.m_amount;
			}
			this.m_recipeName.text = text;
			this.m_recipeDecription.text = Localization.instance.Localize(ItemDrop.ItemData.GetTooltip(this.m_selectedRecipe.Key.m_item.m_itemData, num, true));
			if (value != null)
			{
				this.m_itemCraftType.gameObject.SetActive(true);
				if (value.m_quality >= value.m_shared.m_maxQuality)
				{
					this.m_itemCraftType.text = Localization.instance.Localize("$inventory_maxquality");
				}
				else
				{
					string text2 = Localization.instance.Localize(value.m_shared.m_name);
					this.m_itemCraftType.text = Localization.instance.Localize("$inventory_upgrade", new string[]
					{
						text2,
						(value.m_quality + 1).ToString()
					});
				}
			}
			else
			{
				this.m_itemCraftType.gameObject.SetActive(false);
			}
			this.m_variantButton.gameObject.SetActive(this.m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_variants > 1 && this.m_selectedRecipe.Value == null);
			this.SetupRequirementList(num, player, flag);
			int requiredStationLevel = this.m_selectedRecipe.Key.GetRequiredStationLevel(num);
			CraftingStation requiredStation = this.m_selectedRecipe.Key.GetRequiredStation(num);
			if (requiredStation != null && flag)
			{
				this.m_minStationLevelIcon.gameObject.SetActive(true);
				this.m_minStationLevelText.text = requiredStationLevel.ToString();
				if (currentCraftingStation == null || currentCraftingStation.GetLevel() < requiredStationLevel)
				{
					this.m_minStationLevelText.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : this.m_minStationLevelBasecolor);
				}
				else
				{
					this.m_minStationLevelText.color = this.m_minStationLevelBasecolor;
				}
			}
			else
			{
				this.m_minStationLevelIcon.gameObject.SetActive(false);
			}
			bool flag2 = player.HaveRequirements(this.m_selectedRecipe.Key, false, num);
			bool flag3 = this.m_selectedRecipe.Value != null || player.GetInventory().HaveEmptySlot();
			bool flag4 = !requiredStation || (currentCraftingStation && currentCraftingStation.CheckUsable(player, false));
			this.m_craftButton.interactable = (((flag2 && flag4) || player.NoCostCheat()) && flag3 && flag);
			Text componentInChildren = this.m_craftButton.GetComponentInChildren<Text>();
			if (num > 1)
			{
				componentInChildren.text = Localization.instance.Localize("$inventory_upgradebutton");
			}
			else
			{
				componentInChildren.text = Localization.instance.Localize("$inventory_craftbutton");
			}
			UITooltip component = this.m_craftButton.GetComponent<UITooltip>();
			if (!flag3)
			{
				component.m_text = Localization.instance.Localize("$inventory_full");
			}
			else if (!flag2)
			{
				component.m_text = Localization.instance.Localize("$msg_missingrequirement");
			}
			else if (!flag4)
			{
				component.m_text = Localization.instance.Localize("$msg_missingstation");
			}
			else
			{
				component.m_text = "";
			}
		}
		else
		{
			this.m_recipeIcon.enabled = false;
			this.m_recipeName.enabled = false;
			this.m_recipeDecription.enabled = false;
			this.m_qualityPanel.gameObject.SetActive(false);
			this.m_minStationLevelIcon.gameObject.SetActive(false);
			this.m_craftButton.GetComponent<UITooltip>().m_text = "";
			this.m_variantButton.gameObject.SetActive(false);
			this.m_itemCraftType.gameObject.SetActive(false);
			for (int i = 0; i < this.m_recipeRequirementList.Length; i++)
			{
				InventoryGui.HideRequirement(this.m_recipeRequirementList[i].transform);
			}
			this.m_craftButton.interactable = false;
		}
		if (this.m_craftTimer < 0f)
		{
			this.m_craftProgressPanel.gameObject.SetActive(false);
			this.m_craftButton.gameObject.SetActive(true);
			return;
		}
		this.m_craftButton.gameObject.SetActive(false);
		this.m_craftProgressPanel.gameObject.SetActive(true);
		this.m_craftProgressBar.SetMaxValue(this.m_craftDuration);
		this.m_craftProgressBar.SetValue(this.m_craftTimer);
		this.m_craftTimer += dt;
		if (this.m_craftTimer >= this.m_craftDuration)
		{
			this.DoCrafting(player);
			this.m_craftTimer = -1f;
		}
	}

	private void SetupRequirementList(int quality, Player player, bool allowedQuality)
	{
		int i = 0;
		if (allowedQuality)
		{
			foreach (Piece.Requirement req in this.m_selectedRecipe.Key.m_resources)
			{
				if (InventoryGui.SetupRequirement(this.m_recipeRequirementList[i].transform, req, player, true, quality))
				{
					i++;
				}
			}
		}
		while (i < this.m_recipeRequirementList.Length)
		{
			InventoryGui.HideRequirement(this.m_recipeRequirementList[i].transform);
			i++;
		}
	}

	private void SetupUpgradeItem(Recipe recipe, ItemDrop.ItemData item)
	{
		if (item == null)
		{
			this.m_upgradeItemIcon.sprite = recipe.m_item.m_itemData.m_shared.m_icons[this.m_selectedVariant];
			this.m_upgradeItemName.text = Localization.instance.Localize(recipe.m_item.m_itemData.m_shared.m_name);
			this.m_upgradeItemNextQuality.text = ((recipe.m_item.m_itemData.m_shared.m_maxQuality > 1) ? "1" : "");
			this.m_itemCraftType.text = Localization.instance.Localize("$inventory_new");
			this.m_upgradeItemDurability.gameObject.SetActive(recipe.m_item.m_itemData.m_shared.m_useDurability);
			if (recipe.m_item.m_itemData.m_shared.m_useDurability)
			{
				this.m_upgradeItemDurability.SetValue(1f);
				return;
			}
		}
		else
		{
			this.m_upgradeItemIcon.sprite = item.GetIcon();
			this.m_upgradeItemName.text = Localization.instance.Localize(item.m_shared.m_name);
			this.m_upgradeItemNextQuality.text = item.m_quality.ToString();
			this.m_upgradeItemDurability.gameObject.SetActive(item.m_shared.m_useDurability);
			if (item.m_shared.m_useDurability)
			{
				this.m_upgradeItemDurability.SetValue(item.GetDurabilityPercentage());
			}
			if (item.m_quality >= item.m_shared.m_maxQuality)
			{
				this.m_itemCraftType.text = Localization.instance.Localize("$inventory_maxquality");
				return;
			}
			this.m_itemCraftType.text = Localization.instance.Localize("$inventory_upgrade");
		}
	}

	public static bool SetupRequirement(Transform elementRoot, Piece.Requirement req, Player player, bool craft, int quality)
	{
		Image component = elementRoot.transform.Find("res_icon").GetComponent<Image>();
		Text component2 = elementRoot.transform.Find("res_name").GetComponent<Text>();
		Text component3 = elementRoot.transform.Find("res_amount").GetComponent<Text>();
		UITooltip component4 = elementRoot.GetComponent<UITooltip>();
		if (req.m_resItem != null)
		{
			component.gameObject.SetActive(true);
			component2.gameObject.SetActive(true);
			component3.gameObject.SetActive(true);
			component.sprite = req.m_resItem.m_itemData.GetIcon();
			component.color = Color.white;
			component4.m_text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);
			component2.text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);
			int num = player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name);
			int amount = req.GetAmount(quality);
			if (amount <= 0)
			{
				InventoryGui.HideRequirement(elementRoot);
				return false;
			}
			component3.text = amount.ToString();
			if (num < amount)
			{
				component3.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : Color.white);
			}
			else
			{
				component3.color = Color.white;
			}
		}
		return true;
	}

	public static void HideRequirement(Transform elementRoot)
	{
		Image component = elementRoot.transform.Find("res_icon").GetComponent<Image>();
		Text component2 = elementRoot.transform.Find("res_name").GetComponent<Text>();
		Component component3 = elementRoot.transform.Find("res_amount").GetComponent<Text>();
		elementRoot.GetComponent<UITooltip>().m_text = "";
		component.gameObject.SetActive(false);
		component2.gameObject.SetActive(false);
		component3.gameObject.SetActive(false);
	}

	private void DoCrafting(Player player)
	{
		if (this.m_craftRecipe == null)
		{
			return;
		}
		int num = (this.m_craftUpgradeItem != null) ? (this.m_craftUpgradeItem.m_quality + 1) : 1;
		if (num > this.m_craftRecipe.m_item.m_itemData.m_shared.m_maxQuality)
		{
			return;
		}
		if (!player.HaveRequirements(this.m_craftRecipe, false, num) && !player.NoCostCheat())
		{
			return;
		}
		if (this.m_craftUpgradeItem != null && !player.GetInventory().ContainsItem(this.m_craftUpgradeItem))
		{
			return;
		}
		if (this.m_craftUpgradeItem == null && !player.GetInventory().HaveEmptySlot())
		{
			return;
		}
		if (this.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(this.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc))
		{
			player.Message(MessageHud.MessageType.Center, "$msg_dlcrequired", 0, null);
			return;
		}
		int variant = this.m_craftVariant;
		if (this.m_craftUpgradeItem != null)
		{
			variant = this.m_craftUpgradeItem.m_variant;
			player.UnequipItem(this.m_craftUpgradeItem, true);
			player.GetInventory().RemoveItem(this.m_craftUpgradeItem);
		}
		long playerID = player.GetPlayerID();
		string playerName = player.GetPlayerName();
		if (player.GetInventory().AddItem(this.m_craftRecipe.m_item.gameObject.name, this.m_craftRecipe.m_amount, num, variant, playerID, playerName) != null)
		{
			if (!player.NoCostCheat())
			{
				player.ConsumeResources(this.m_craftRecipe.m_resources, num);
			}
			this.UpdateCraftingPanel(false);
		}
		CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
		if (currentCraftingStation)
		{
			currentCraftingStation.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity, null, 1f);
		}
		else
		{
			this.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity, null, 1f);
		}
		Game.instance.GetPlayerProfile().m_playerStats.m_crafts++;
		GoogleAnalyticsV4.instance.LogEvent("Game", "Crafted", this.m_craftRecipe.m_item.m_itemData.m_shared.m_name, (long)num);
	}

	private int FindSelectedRecipe(GameObject button)
	{
		for (int i = 0; i < this.m_recipeList.Count; i++)
		{
			if (this.m_recipeList[i] == button)
			{
				return i;
			}
		}
		return -1;
	}

	private void OnCraftCancelPressed()
	{
		if (this.m_craftTimer >= 0f)
		{
			this.m_craftTimer = -1f;
		}
	}

	private void OnCraftPressed()
	{
		if (!this.m_selectedRecipe.Key)
		{
			return;
		}
		this.m_craftRecipe = this.m_selectedRecipe.Key;
		this.m_craftUpgradeItem = this.m_selectedRecipe.Value;
		this.m_craftVariant = this.m_selectedVariant;
		this.m_craftTimer = 0f;
		if (this.m_craftRecipe.m_craftingStation)
		{
			CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
			if (currentCraftingStation)
			{
				currentCraftingStation.m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity, null, 1f);
				return;
			}
		}
		else
		{
			this.m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity, null, 1f);
		}
	}

	private void OnRepairPressed()
	{
		this.RepairOneItem();
		this.UpdateRepair();
	}

	private void UpdateRepair()
	{
		if (Player.m_localPlayer.GetCurrentCraftingStation() == null && !Player.m_localPlayer.NoCostCheat())
		{
			this.m_repairPanel.gameObject.SetActive(false);
			this.m_repairPanelSelection.gameObject.SetActive(false);
			this.m_repairButton.gameObject.SetActive(false);
			return;
		}
		this.m_repairButton.gameObject.SetActive(true);
		this.m_repairPanel.gameObject.SetActive(true);
		this.m_repairPanelSelection.gameObject.SetActive(true);
		if (this.HaveRepairableItems())
		{
			this.m_repairButton.interactable = true;
			this.m_repairButtonGlow.gameObject.SetActive(true);
			Color color = this.m_repairButtonGlow.color;
			color.a = 0.5f + Mathf.Sin(Time.time * 5f) * 0.5f;
			this.m_repairButtonGlow.color = color;
			return;
		}
		this.m_repairButton.interactable = false;
		this.m_repairButtonGlow.gameObject.SetActive(false);
	}

	private void RepairOneItem()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
		if (currentCraftingStation == null && !Player.m_localPlayer.NoCostCheat())
		{
			return;
		}
		if (currentCraftingStation && !currentCraftingStation.CheckUsable(Player.m_localPlayer, false))
		{
			return;
		}
		this.m_tempWornItems.Clear();
		Player.m_localPlayer.GetInventory().GetWornItems(this.m_tempWornItems);
		foreach (ItemDrop.ItemData itemData in this.m_tempWornItems)
		{
			if (this.CanRepair(itemData))
			{
				itemData.m_durability = itemData.GetMaxDurability();
				if (currentCraftingStation)
				{
					currentCraftingStation.m_repairItemDoneEffects.Create(currentCraftingStation.transform.position, Quaternion.identity, null, 1f);
				}
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_repaired", new string[]
				{
					itemData.m_shared.m_name
				}), 0, null);
				return;
			}
		}
		Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No more item to repair", 0, null);
	}

	private bool HaveRepairableItems()
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
		if (currentCraftingStation == null && !Player.m_localPlayer.NoCostCheat())
		{
			return false;
		}
		if (currentCraftingStation && !currentCraftingStation.CheckUsable(Player.m_localPlayer, false))
		{
			return false;
		}
		this.m_tempWornItems.Clear();
		Player.m_localPlayer.GetInventory().GetWornItems(this.m_tempWornItems);
		foreach (ItemDrop.ItemData item in this.m_tempWornItems)
		{
			if (this.CanRepair(item))
			{
				return true;
			}
		}
		return false;
	}

	private bool CanRepair(ItemDrop.ItemData item)
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		if (!item.m_shared.m_canBeReparied)
		{
			return false;
		}
		if (Player.m_localPlayer.NoCostCheat())
		{
			return true;
		}
		CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
		if (currentCraftingStation == null)
		{
			return false;
		}
		Recipe recipe = ObjectDB.instance.GetRecipe(item);
		return !(recipe == null) && (!(recipe.m_craftingStation == null) || !(recipe.m_repairStation == null)) && ((recipe.m_repairStation != null && recipe.m_repairStation.m_name == currentCraftingStation.m_name) || (recipe.m_craftingStation != null && recipe.m_craftingStation.m_name == currentCraftingStation.m_name)) && currentCraftingStation.GetLevel() >= recipe.m_minStationLevel;
	}

	private void SetupDragItem(ItemDrop.ItemData item, Inventory inventory, int amount)
	{
		if (this.m_dragGo)
		{
			UnityEngine.Object.Destroy(this.m_dragGo);
			this.m_dragGo = null;
			this.m_dragItem = null;
			this.m_dragInventory = null;
			this.m_dragAmount = 0;
		}
		if (item != null)
		{
			this.m_dragGo = UnityEngine.Object.Instantiate<GameObject>(this.m_dragItemPrefab, base.transform);
			this.m_dragItem = item;
			this.m_dragInventory = inventory;
			this.m_dragAmount = amount;
			this.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
			UITooltip.HideTooltip();
		}
	}

	private void ShowSplitDialog(ItemDrop.ItemData item, Inventory fromIventory)
	{
		this.m_splitSlider.minValue = 1f;
		this.m_splitSlider.maxValue = (float)item.m_stack;
		this.m_splitSlider.value = (float)Mathf.CeilToInt((float)item.m_stack / 2f);
		this.m_splitIcon.sprite = item.GetIcon();
		this.m_splitIconName.text = Localization.instance.Localize(item.m_shared.m_name);
		this.m_splitPanel.gameObject.SetActive(true);
		this.m_splitItem = item;
		this.m_splitInventory = fromIventory;
		this.OnSplitSliderChanged(this.m_splitSlider.value);
	}

	private void OnSplitSliderChanged(float value)
	{
		this.m_splitAmount.text = (int)value + "/" + (int)this.m_splitSlider.maxValue;
	}

	private void OnSplitCancel()
	{
		this.m_splitItem = null;
		this.m_splitInventory = null;
		this.m_splitPanel.gameObject.SetActive(false);
	}

	private void OnSplitOk()
	{
		this.SetupDragItem(this.m_splitItem, this.m_splitInventory, (int)this.m_splitSlider.value);
		this.m_splitItem = null;
		this.m_splitInventory = null;
		this.m_splitPanel.gameObject.SetActive(false);
	}

	public void OnOpenSkills()
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		this.m_skillsDialog.Setup(Player.m_localPlayer);
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "Skills", 0L);
	}

	public void OnOpenTexts()
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		this.m_textsDialog.Setup(Player.m_localPlayer);
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "Texts", 0L);
	}

	public void OnOpenTrophies()
	{
		this.m_trophiesPanel.SetActive(true);
		this.UpdateTrophyList();
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "Trophies", 0L);
	}

	public void OnCloseTrophies()
	{
		this.m_trophiesPanel.SetActive(false);
	}

	private void UpdateTrophyList()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		foreach (GameObject obj in this.m_trophyList)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_trophyList.Clear();
		List<string> trophies = Player.m_localPlayer.GetTrophies();
		float num = 0f;
		for (int i = 0; i < trophies.Count; i++)
		{
			string text = trophies[i];
			GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(text);
			if (itemPrefab == null)
			{
				ZLog.LogWarning("Missing trophy prefab:" + text);
			}
			else
			{
				ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_trophieElementPrefab, this.m_trophieListRoot);
				gameObject.SetActive(true);
				RectTransform rectTransform = gameObject.transform as RectTransform;
				rectTransform.anchoredPosition = new Vector2((float)component.m_itemData.m_shared.m_trophyPos.x * this.m_trophieListSpace, (float)component.m_itemData.m_shared.m_trophyPos.y * -this.m_trophieListSpace);
				num = Mathf.Min(num, rectTransform.anchoredPosition.y - this.m_trophieListSpace);
				string text2 = Localization.instance.Localize(component.m_itemData.m_shared.m_name);
				if (text2.EndsWith(" trophy"))
				{
					text2 = text2.Remove(text2.Length - 7);
				}
				rectTransform.Find("icon_bkg/icon").GetComponent<Image>().sprite = component.m_itemData.GetIcon();
				rectTransform.Find("name").GetComponent<Text>().text = text2;
				rectTransform.Find("description").GetComponent<Text>().text = Localization.instance.Localize(component.m_itemData.m_shared.m_name + "_lore");
				this.m_trophyList.Add(gameObject);
			}
		}
		ZLog.Log("SIZE " + num);
		float size = Mathf.Max(this.m_trophieListBaseSize, -num);
		this.m_trophieListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		this.m_trophyListScroll.value = 1f;
	}

	public void OnShowVariantSelection()
	{
		this.m_variantDialog.Setup(this.m_selectedRecipe.Key.m_item.m_itemData);
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "VariantSelection", 0L);
	}

	private void OnVariantSelected(int index)
	{
		ZLog.Log("Item variant selected " + index);
		this.m_selectedVariant = index;
	}

	public bool InUpradeTab()
	{
		return !this.m_tabUpgrade.interactable;
	}

	public bool InCraftTab()
	{
		return !this.m_tabCraft.interactable;
	}

	public void OnTabCraftPressed()
	{
		this.m_tabCraft.interactable = false;
		this.m_tabUpgrade.interactable = true;
		this.UpdateCraftingPanel(false);
	}

	public void OnTabUpgradePressed()
	{
		this.m_tabCraft.interactable = true;
		this.m_tabUpgrade.interactable = false;
		this.UpdateCraftingPanel(false);
	}

	private List<ItemDrop.ItemData> m_tempItemList = new List<ItemDrop.ItemData>();

	private List<ItemDrop.ItemData> m_tempWornItems = new List<ItemDrop.ItemData>();

	private static InventoryGui m_instance;

	[Header("Gamepad")]
	public UIGroupHandler m_inventoryGroup;

	public UIGroupHandler[] m_uiGroups = new UIGroupHandler[0];

	private int m_activeGroup = 1;

	[Header("Other")]
	public Transform m_inventoryRoot;

	public RectTransform m_player;

	public RectTransform m_container;

	public GameObject m_dragItemPrefab;

	public Text m_containerName;

	public Button m_dropButton;

	public Button m_takeAllButton;

	public float m_autoCloseDistance = 4f;

	[Header("Crafting dialog")]
	public Button m_tabCraft;

	public Button m_tabUpgrade;

	public GameObject m_recipeElementPrefab;

	public RectTransform m_recipeListRoot;

	public Scrollbar m_recipeListScroll;

	public float m_recipeListSpace = 30f;

	public float m_craftDuration = 2f;

	public Text m_craftingStationName;

	public Image m_craftingStationIcon;

	public RectTransform m_craftingStationLevelRoot;

	public Text m_craftingStationLevel;

	public Text m_recipeName;

	public Text m_recipeDecription;

	public Image m_recipeIcon;

	public GameObject[] m_recipeRequirementList = new GameObject[0];

	public Button m_variantButton;

	public Button m_craftButton;

	public Button m_craftCancelButton;

	public Transform m_craftProgressPanel;

	public GuiBar m_craftProgressBar;

	[Header("Repair")]
	public Button m_repairButton;

	public Transform m_repairPanel;

	public Image m_repairButtonGlow;

	public Transform m_repairPanelSelection;

	[Header("Upgrade")]
	public Image m_upgradeItemIcon;

	public GuiBar m_upgradeItemDurability;

	public Text m_upgradeItemName;

	public Text m_upgradeItemQuality;

	public GameObject m_upgradeItemQualityArrow;

	public Text m_upgradeItemNextQuality;

	public Text m_upgradeItemIndex;

	public Text m_itemCraftType;

	public RectTransform m_qualityPanel;

	public Button m_qualityLevelDown;

	public Button m_qualityLevelUp;

	public Text m_qualityLevel;

	public Image m_minStationLevelIcon;

	private Color m_minStationLevelBasecolor;

	public Text m_minStationLevelText;

	public ScrollRectEnsureVisible m_recipeEnsureVisible;

	[Header("Variants dialog")]
	public VariantDialog m_variantDialog;

	[Header("Skills dialog")]
	public SkillsDialog m_skillsDialog;

	[Header("Texts dialog")]
	public TextsDialog m_textsDialog;

	[Header("Split dialog")]
	public Transform m_splitPanel;

	public Slider m_splitSlider;

	public Text m_splitAmount;

	public Button m_splitCancelButton;

	public Button m_splitOkButton;

	public Image m_splitIcon;

	public Text m_splitIconName;

	[Header("Character stats")]
	public Transform m_infoPanel;

	public Text m_playerName;

	public Text m_armor;

	public Text m_weight;

	public Text m_containerWeight;

	public Toggle m_pvp;

	[Header("Trophies")]
	public GameObject m_trophiesPanel;

	public RectTransform m_trophieListRoot;

	public float m_trophieListSpace = 30f;

	public GameObject m_trophieElementPrefab;

	public Scrollbar m_trophyListScroll;

	[Header("Effects")]
	public EffectList m_moveItemEffects = new EffectList();

	public EffectList m_craftItemEffects = new EffectList();

	public EffectList m_craftItemDoneEffects = new EffectList();

	public EffectList m_openInventoryEffects = new EffectList();

	public EffectList m_closeInventoryEffects = new EffectList();

	private InventoryGrid m_playerGrid;

	private InventoryGrid m_containerGrid;

	private Animator m_animator;

	private Container m_currentContainer;

	private bool m_firstContainerUpdate = true;

	private KeyValuePair<Recipe, ItemDrop.ItemData> m_selectedRecipe;

	private List<ItemDrop.ItemData> m_upgradeItems = new List<ItemDrop.ItemData>();

	private int m_selectedVariant;

	private Recipe m_craftRecipe;

	private ItemDrop.ItemData m_craftUpgradeItem;

	private int m_craftVariant;

	private List<GameObject> m_recipeList = new List<GameObject>();

	private List<KeyValuePair<Recipe, ItemDrop.ItemData>> m_availableRecipes = new List<KeyValuePair<Recipe, ItemDrop.ItemData>>();

	private GameObject m_dragGo;

	private ItemDrop.ItemData m_dragItem;

	private Inventory m_dragInventory;

	private int m_dragAmount = 1;

	private ItemDrop.ItemData m_splitItem;

	private Inventory m_splitInventory;

	private float m_craftTimer = -1f;

	private float m_recipeListBaseSize;

	private int m_hiddenFrames = 9999;

	private List<GameObject> m_trophyList = new List<GameObject>();

	private float m_trophieListBaseSize;
}
