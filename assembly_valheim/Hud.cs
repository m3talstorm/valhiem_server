using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hud : MonoBehaviour
{
	private void OnDestroy()
	{
		Hud.m_instance = null;
	}

	public static Hud instance
	{
		get
		{
			return Hud.m_instance;
		}
	}

	private void Awake()
	{
		Hud.m_instance = this;
		this.m_pieceSelectionWindow.SetActive(false);
		this.m_loadingScreen.gameObject.SetActive(false);
		this.m_statusEffectTemplate.gameObject.SetActive(false);
		this.m_eventBar.SetActive(false);
		this.m_gpRoot.gameObject.SetActive(false);
		this.m_betaText.SetActive(false);
		UIInputHandler closePieceSelectionButton = this.m_closePieceSelectionButton;
		closePieceSelectionButton.m_onLeftClick = (Action<UIInputHandler>)Delegate.Combine(closePieceSelectionButton.m_onLeftClick, new Action<UIInputHandler>(this.OnClosePieceSelection));
		UIInputHandler closePieceSelectionButton2 = this.m_closePieceSelectionButton;
		closePieceSelectionButton2.m_onRightClick = (Action<UIInputHandler>)Delegate.Combine(closePieceSelectionButton2.m_onRightClick, new Action<UIInputHandler>(this.OnClosePieceSelection));
		if (SteamManager.APP_ID == 1223920U)
		{
			this.m_betaText.SetActive(true);
		}
		foreach (GameObject gameObject in this.m_pieceCategoryTabs)
		{
			this.m_buildCategoryNames.Add(gameObject.transform.Find("Text").GetComponent<Text>().text);
			UIInputHandler component = gameObject.GetComponent<UIInputHandler>();
			component.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftDown, new Action<UIInputHandler>(this.OnLeftClickCategory));
		}
	}

	private void SetVisible(bool visible)
	{
		if (visible == this.IsVisible())
		{
			return;
		}
		if (visible)
		{
			this.m_rootObject.transform.localPosition = new Vector3(0f, 0f, 0f);
			return;
		}
		this.m_rootObject.transform.localPosition = new Vector3(10000f, 0f, 0f);
	}

	private bool IsVisible()
	{
		return this.m_rootObject.transform.localPosition.x < 1000f;
	}

	private void Update()
	{
		this.m_saveIcon.SetActive(ZNet.instance != null && ZNet.instance.IsSaving());
		this.m_badConnectionIcon.SetActive(ZNet.instance != null && ZNet.instance.HasBadConnection() && Mathf.Sin(Time.time * 10f) > 0f);
		Player localPlayer = Player.m_localPlayer;
		this.UpdateDamageFlash(Time.deltaTime);
		if (localPlayer)
		{
			if (Input.GetKeyDown(KeyCode.F3) && Input.GetKey(KeyCode.LeftControl))
			{
				this.m_userHidden = !this.m_userHidden;
			}
			this.SetVisible(!this.m_userHidden && !localPlayer.InCutscene());
			this.UpdateBuild(localPlayer, false);
			this.m_tempStatusEffects.Clear();
			localPlayer.GetSEMan().GetHUDStatusEffects(this.m_tempStatusEffects);
			this.UpdateStatusEffects(this.m_tempStatusEffects);
			this.UpdateGuardianPower(localPlayer);
			float attackDrawPercentage = localPlayer.GetAttackDrawPercentage();
			this.UpdateFood(localPlayer);
			this.UpdateHealth(localPlayer);
			this.UpdateStamina(localPlayer);
			this.UpdateStealth(localPlayer, attackDrawPercentage);
			this.UpdateCrosshair(localPlayer, attackDrawPercentage);
			this.UpdateEvent(localPlayer);
			this.UpdateActionProgress(localPlayer);
		}
	}

	private void LateUpdate()
	{
		this.UpdateBlackScreen(Player.m_localPlayer, Time.deltaTime);
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			this.UpdateShipHud(localPlayer, Time.deltaTime);
		}
	}

	private float GetFadeDuration(Player player)
	{
		if (player != null)
		{
			if (player.IsDead())
			{
				return 9.5f;
			}
			if (player.IsSleeping())
			{
				return 3f;
			}
		}
		return 1f;
	}

	private void UpdateBlackScreen(Player player, float dt)
	{
		if (!(player == null) && !player.IsDead() && !player.IsTeleporting() && !Game.instance.IsLoggingOut() && !player.IsSleeping())
		{
			this.m_haveSetupLoadScreen = false;
			float fadeDuration = this.GetFadeDuration(player);
			float num = this.m_loadingScreen.alpha;
			num = Mathf.MoveTowards(num, 0f, dt / fadeDuration);
			this.m_loadingScreen.alpha = num;
			if (this.m_loadingScreen.alpha <= 0f)
			{
				this.m_loadingScreen.gameObject.SetActive(false);
			}
			return;
		}
		this.m_loadingScreen.gameObject.SetActive(true);
		float num2 = this.m_loadingScreen.alpha;
		float fadeDuration2 = this.GetFadeDuration(player);
		num2 = Mathf.MoveTowards(num2, 1f, dt / fadeDuration2);
		if (Game.instance.IsLoggingOut())
		{
			num2 = 1f;
		}
		this.m_loadingScreen.alpha = num2;
		if (player != null && player.IsSleeping())
		{
			this.m_sleepingProgress.SetActive(true);
			this.m_loadingProgress.SetActive(false);
			this.m_teleportingProgress.SetActive(false);
			return;
		}
		if (player != null && player.ShowTeleportAnimation())
		{
			this.m_loadingProgress.SetActive(false);
			this.m_sleepingProgress.SetActive(false);
			this.m_teleportingProgress.SetActive(true);
			return;
		}
		if (Game.instance && Game.instance.WaitingForRespawn())
		{
			if (!this.m_haveSetupLoadScreen)
			{
				this.m_haveSetupLoadScreen = true;
				if (this.m_useRandomImages)
				{
					int num3 = UnityEngine.Random.Range(0, this.m_loadingImages);
					string text = this.m_loadingImagePath + "loading" + num3.ToString();
					ZLog.Log("Loading image:" + text);
					this.m_loadingImage.sprite = Resources.Load<Sprite>(text);
				}
				string text2 = this.m_loadingTips[UnityEngine.Random.Range(0, this.m_loadingTips.Count)];
				ZLog.Log("tip:" + text2);
				this.m_loadingTip.text = Localization.instance.Localize(text2);
			}
			this.m_loadingProgress.SetActive(true);
			this.m_sleepingProgress.SetActive(false);
			this.m_teleportingProgress.SetActive(false);
			return;
		}
		this.m_loadingProgress.SetActive(false);
		this.m_sleepingProgress.SetActive(false);
		this.m_teleportingProgress.SetActive(false);
	}

	private void UpdateShipHud(Player player, float dt)
	{
		Ship controlledShip = player.GetControlledShip();
		if (controlledShip == null)
		{
			this.m_shipHudRoot.gameObject.SetActive(false);
			return;
		}
		Ship.Speed speedSetting = controlledShip.GetSpeedSetting();
		float rudder = controlledShip.GetRudder();
		float rudderValue = controlledShip.GetRudderValue();
		this.m_shipHudRoot.SetActive(true);
		this.m_rudderSlow.SetActive(speedSetting == Ship.Speed.Slow);
		this.m_rudderForward.SetActive(speedSetting == Ship.Speed.Half);
		this.m_rudderFastForward.SetActive(speedSetting == Ship.Speed.Full);
		this.m_rudderBackward.SetActive(speedSetting == Ship.Speed.Back);
		this.m_rudderLeft.SetActive(false);
		this.m_rudderRight.SetActive(false);
		this.m_fullSail.SetActive(speedSetting == Ship.Speed.Full);
		this.m_halfSail.SetActive(speedSetting == Ship.Speed.Half);
		this.m_rudder.SetActive(speedSetting == Ship.Speed.Slow || speedSetting == Ship.Speed.Back || (speedSetting == Ship.Speed.Stop && Mathf.Abs(rudderValue) > 0.2f));
		if ((rudder > 0f && rudderValue < 1f) || (rudder < 0f && rudderValue > -1f))
		{
			this.m_shipRudderIcon.transform.Rotate(new Vector3(0f, 0f, 200f * -rudder * dt));
		}
		if (Mathf.Abs(rudderValue) < 0.02f)
		{
			this.m_shipRudderIndicator.gameObject.SetActive(false);
		}
		else
		{
			this.m_shipRudderIndicator.gameObject.SetActive(true);
			if (rudderValue > 0f)
			{
				this.m_shipRudderIndicator.fillClockwise = true;
				this.m_shipRudderIndicator.fillAmount = rudderValue * 0.25f;
			}
			else
			{
				this.m_shipRudderIndicator.fillClockwise = false;
				this.m_shipRudderIndicator.fillAmount = -rudderValue * 0.25f;
			}
		}
		float shipYawAngle = controlledShip.GetShipYawAngle();
		this.m_shipWindIndicatorRoot.localRotation = Quaternion.Euler(0f, 0f, shipYawAngle);
		float windAngle = controlledShip.GetWindAngle();
		this.m_shipWindIconRoot.localRotation = Quaternion.Euler(0f, 0f, windAngle);
		float windAngleFactor = controlledShip.GetWindAngleFactor();
		this.m_shipWindIcon.color = Color.Lerp(new Color(0.2f, 0.2f, 0.2f, 1f), Color.white, windAngleFactor);
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		this.m_shipControlsRoot.transform.position = mainCamera.WorldToScreenPoint(controlledShip.m_controlGuiPos.position);
	}

	private void UpdateActionProgress(Player player)
	{
		string text;
		float value;
		player.GetActionProgress(out text, out value);
		if (!string.IsNullOrEmpty(text))
		{
			this.m_actionBarRoot.SetActive(true);
			this.m_actionProgress.SetValue(value);
			this.m_actionName.text = Localization.instance.Localize(text);
			return;
		}
		this.m_actionBarRoot.SetActive(false);
	}

	private void UpdateCrosshair(Player player, float bowDrawPercentage)
	{
		GameObject hoverObject = player.GetHoverObject();
		Hoverable hoverable = hoverObject ? hoverObject.GetComponentInParent<Hoverable>() : null;
		if (hoverable != null && !TextViewer.instance.IsVisible())
		{
			this.m_hoverName.text = hoverable.GetHoverText();
			this.m_crosshair.color = ((this.m_hoverName.text.Length > 0) ? Color.yellow : new Color(1f, 1f, 1f, 0.5f));
		}
		else
		{
			this.m_crosshair.color = new Color(1f, 1f, 1f, 0.5f);
			this.m_hoverName.text = "";
		}
		Piece hoveringPiece = player.GetHoveringPiece();
		if (hoveringPiece)
		{
			WearNTear component = hoveringPiece.GetComponent<WearNTear>();
			if (component)
			{
				this.m_pieceHealthRoot.gameObject.SetActive(true);
				this.m_pieceHealthBar.SetValue(component.GetHealthPercentage());
			}
			else
			{
				this.m_pieceHealthRoot.gameObject.SetActive(false);
			}
		}
		else
		{
			this.m_pieceHealthRoot.gameObject.SetActive(false);
		}
		if (bowDrawPercentage > 0f)
		{
			float num = Mathf.Lerp(1f, 0.15f, bowDrawPercentage);
			this.m_crosshairBow.gameObject.SetActive(true);
			this.m_crosshairBow.transform.localScale = new Vector3(num, num, num);
			this.m_crosshairBow.color = Color.Lerp(new Color(1f, 1f, 1f, 0f), Color.yellow, bowDrawPercentage);
			return;
		}
		this.m_crosshairBow.gameObject.SetActive(false);
	}

	private void FixedUpdate()
	{
		this.UpdatePieceBar(Time.fixedDeltaTime);
	}

	private void UpdateStealth(Player player, float bowDrawPercentage)
	{
		float stealthFactor = player.GetStealthFactor();
		if ((player.IsCrouching() || stealthFactor < 1f) && bowDrawPercentage == 0f)
		{
			if (player.IsSensed())
			{
				this.m_targetedAlert.SetActive(true);
				this.m_targeted.SetActive(false);
				this.m_hidden.SetActive(false);
			}
			else if (player.IsTargeted())
			{
				this.m_targetedAlert.SetActive(false);
				this.m_targeted.SetActive(true);
				this.m_hidden.SetActive(false);
			}
			else
			{
				this.m_targetedAlert.SetActive(false);
				this.m_targeted.SetActive(false);
				this.m_hidden.SetActive(true);
			}
			this.m_stealthBar.gameObject.SetActive(true);
			this.m_stealthBar.SetValue(stealthFactor);
			return;
		}
		this.m_targetedAlert.SetActive(false);
		this.m_hidden.SetActive(false);
		this.m_targeted.SetActive(false);
		this.m_stealthBar.gameObject.SetActive(false);
	}

	private void SetHealthBarSize(float size)
	{
		size = Mathf.Ceil(size);
		float size2 = Mathf.Max(size + 56f, 138f);
		this.m_healthPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size2);
		this.m_healthBarRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
		this.m_healthBarSlow.SetWidth(size);
		this.m_healthBarFast.SetWidth(size);
	}

	private void SetStaminaBarSize(float size)
	{
		this.m_staminaBar2Root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size + this.m_staminaBarBorderBuffer);
		this.m_staminaBar2Slow.SetWidth(size);
		this.m_staminaBar2Fast.SetWidth(size);
	}

	private void UpdateFood(Player player)
	{
		List<Player.Food> foods = player.GetFoods();
		float num = player.GetBaseFoodHP() / 25f * 32f;
		this.m_foodBaseBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num);
		float num2 = num;
		for (int i = 0; i < this.m_foodBars.Length; i++)
		{
			Image image = this.m_foodBars[i];
			Image image2 = this.m_foodIcons[i];
			if (i < foods.Count)
			{
				image.gameObject.SetActive(true);
				Player.Food food = foods[i];
				float num3 = food.m_health / 25f * 32f;
				image.color = food.m_item.m_shared.m_foodColor;
				image.rectTransform.anchoredPosition = new Vector2(num2, 0f);
				image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Ceil(num3));
				num2 += num3;
				image2.gameObject.SetActive(true);
				image2.sprite = food.m_item.GetIcon();
				if (food.CanEatAgain())
				{
					image2.color = new Color(1f, 1f, 1f, 0.6f + Mathf.Sin(Time.time * 10f) * 0.4f);
				}
				else
				{
					image2.color = Color.white;
				}
			}
			else
			{
				image.gameObject.SetActive(false);
				image2.gameObject.SetActive(false);
			}
		}
		float size = Mathf.Ceil(player.GetMaxHealth() / 25f * 32f);
		this.m_foodBarRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
	}

	private void UpdateHealth(Player player)
	{
		float maxHealth = player.GetMaxHealth();
		this.SetHealthBarSize(maxHealth / 25f * 32f);
		float health = player.GetHealth();
		this.m_healthBarFast.SetMaxValue(maxHealth);
		this.m_healthBarFast.SetValue(health);
		this.m_healthBarSlow.SetMaxValue(maxHealth);
		this.m_healthBarSlow.SetValue(health);
		string text = Mathf.CeilToInt(player.GetHealth()).ToString();
		string text2 = Mathf.CeilToInt(player.GetMaxHealth()).ToString();
		this.m_healthText.text = text.ToString();
		this.m_healthMaxText.text = text2.ToString();
	}

	private void UpdateStamina(Player player)
	{
		float stamina = player.GetStamina();
		float maxStamina = player.GetMaxStamina();
		this.m_staminaBar.SetActive(false);
		this.m_staminaAnimator.SetBool("Visible", stamina < maxStamina);
		this.SetStaminaBarSize(player.GetMaxStamina() / 25f * 32f);
		RectTransform rectTransform = this.m_staminaBar2Root.transform as RectTransform;
		if (this.m_buildHud.activeSelf || this.m_shipHudRoot.activeSelf)
		{
			rectTransform.anchoredPosition = new Vector2(0f, 190f);
		}
		else
		{
			rectTransform.anchoredPosition = new Vector2(0f, 130f);
		}
		this.m_staminaBar2Slow.SetValue(stamina / maxStamina);
		this.m_staminaBar2Fast.SetValue(stamina / maxStamina);
	}

	public void DamageFlash()
	{
		Color color = this.m_damageScreen.color;
		color.a = 1f;
		this.m_damageScreen.color = color;
		this.m_damageScreen.gameObject.SetActive(true);
	}

	private void UpdateDamageFlash(float dt)
	{
		Color color = this.m_damageScreen.color;
		color.a = Mathf.MoveTowards(color.a, 0f, dt * 4f);
		this.m_damageScreen.color = color;
		if (color.a <= 0f)
		{
			this.m_damageScreen.gameObject.SetActive(false);
		}
	}

	private void UpdatePieceList(Player player, Vector2Int selectedNr, Piece.PieceCategory category, bool updateAllBuildStatuses)
	{
		List<Piece> buildPieces = player.GetBuildPieces();
		int num = 10;
		int num2 = 5;
		if (buildPieces.Count <= 1)
		{
			num = 1;
			num2 = 1;
		}
		if (this.m_pieceIcons.Count != num * num2)
		{
			foreach (Hud.PieceIconData pieceIconData in this.m_pieceIcons)
			{
				UnityEngine.Object.Destroy(pieceIconData.m_go);
			}
			this.m_pieceIcons.Clear();
			for (int i = 0; i < num2; i++)
			{
				for (int j = 0; j < num; j++)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_pieceIconPrefab, this.m_pieceListRoot);
					(gameObject.transform as RectTransform).anchoredPosition = new Vector2((float)j * this.m_pieceIconSpacing, (float)(-(float)i) * this.m_pieceIconSpacing);
					Hud.PieceIconData pieceIconData2 = new Hud.PieceIconData();
					pieceIconData2.m_go = gameObject;
					pieceIconData2.m_tooltip = gameObject.GetComponent<UITooltip>();
					pieceIconData2.m_icon = gameObject.transform.Find("icon").GetComponent<Image>();
					pieceIconData2.m_marker = gameObject.transform.Find("selected").gameObject;
					pieceIconData2.m_upgrade = gameObject.transform.Find("upgrade").gameObject;
					pieceIconData2.m_icon.color = new Color(1f, 0f, 1f, 0f);
					UIInputHandler component = gameObject.GetComponent<UIInputHandler>();
					component.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftDown, new Action<UIInputHandler>(this.OnLeftClickPiece));
					component.m_onRightDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onRightDown, new Action<UIInputHandler>(this.OnRightClickPiece));
					component.m_onPointerEnter = (Action<UIInputHandler>)Delegate.Combine(component.m_onPointerEnter, new Action<UIInputHandler>(this.OnHoverPiece));
					component.m_onPointerExit = (Action<UIInputHandler>)Delegate.Combine(component.m_onPointerExit, new Action<UIInputHandler>(this.OnHoverPieceExit));
					this.m_pieceIcons.Add(pieceIconData2);
				}
			}
		}
		for (int k = 0; k < num2; k++)
		{
			for (int l = 0; l < num; l++)
			{
				int num3 = k * num + l;
				Hud.PieceIconData pieceIconData3 = this.m_pieceIcons[num3];
				pieceIconData3.m_marker.SetActive(new Vector2Int(l, k) == selectedNr);
				if (num3 < buildPieces.Count)
				{
					Piece piece = buildPieces[num3];
					pieceIconData3.m_icon.sprite = piece.m_icon;
					pieceIconData3.m_icon.enabled = true;
					pieceIconData3.m_tooltip.m_text = piece.m_name;
					pieceIconData3.m_upgrade.SetActive(piece.m_isUpgrade);
				}
				else
				{
					pieceIconData3.m_icon.enabled = false;
					pieceIconData3.m_tooltip.m_text = "";
					pieceIconData3.m_upgrade.SetActive(false);
				}
			}
		}
		this.UpdatePieceBuildStatus(buildPieces, player);
		if (updateAllBuildStatuses)
		{
			this.UpdatePieceBuildStatusAll(buildPieces, player);
		}
		if (this.m_lastPieceCategory != category)
		{
			this.m_lastPieceCategory = category;
			this.m_pieceBarPosX = this.m_pieceBarTargetPosX;
			this.UpdatePieceBuildStatusAll(buildPieces, player);
		}
	}

	private void OnLeftClickCategory(UIInputHandler ih)
	{
		for (int i = 0; i < this.m_pieceCategoryTabs.Length; i++)
		{
			if (this.m_pieceCategoryTabs[i] == ih.gameObject)
			{
				Player.m_localPlayer.SetBuildCategory(i);
				return;
			}
		}
	}

	private void OnLeftClickPiece(UIInputHandler ih)
	{
		this.SelectPiece(ih);
		Hud.HidePieceSelection();
	}

	private void OnRightClickPiece(UIInputHandler ih)
	{
		if (this.IsQuickPieceSelectEnabled())
		{
			this.SelectPiece(ih);
			Hud.HidePieceSelection();
		}
	}

	private void OnHoverPiece(UIInputHandler ih)
	{
		Vector2Int selectedGrid = this.GetSelectedGrid(ih);
		if (selectedGrid.x != -1)
		{
			this.m_hoveredPiece = Player.m_localPlayer.GetPiece(selectedGrid);
		}
	}

	private void OnHoverPieceExit(UIInputHandler ih)
	{
		this.m_hoveredPiece = null;
	}

	public bool IsQuickPieceSelectEnabled()
	{
		return PlayerPrefs.GetInt("QuickPieceSelect", 0) == 1;
	}

	private Vector2Int GetSelectedGrid(UIInputHandler ih)
	{
		int num = 10;
		int num2 = 5;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				int index = i * num + j;
				if (this.m_pieceIcons[index].m_go == ih.gameObject)
				{
					return new Vector2Int(j, i);
				}
			}
		}
		return new Vector2Int(-1, -1);
	}

	private void SelectPiece(UIInputHandler ih)
	{
		Vector2Int selectedGrid = this.GetSelectedGrid(ih);
		if (selectedGrid.x != -1)
		{
			Player.m_localPlayer.SetSelectedPiece(selectedGrid);
			this.m_selectItemEffect.Create(base.transform.position, Quaternion.identity, null, 1f);
		}
	}

	private void UpdatePieceBuildStatus(List<Piece> pieces, Player player)
	{
		if (this.m_pieceIcons.Count == 0)
		{
			return;
		}
		if (this.m_pieceIconUpdateIndex >= this.m_pieceIcons.Count)
		{
			this.m_pieceIconUpdateIndex = 0;
		}
		Hud.PieceIconData pieceIconData = this.m_pieceIcons[this.m_pieceIconUpdateIndex];
		if (this.m_pieceIconUpdateIndex < pieces.Count)
		{
			Piece piece = pieces[this.m_pieceIconUpdateIndex];
			bool flag = player.HaveRequirements(piece, Player.RequirementMode.CanBuild);
			pieceIconData.m_icon.color = (flag ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 0f, 1f, 0f));
		}
		this.m_pieceIconUpdateIndex++;
	}

	private void UpdatePieceBuildStatusAll(List<Piece> pieces, Player player)
	{
		for (int i = 0; i < this.m_pieceIcons.Count; i++)
		{
			Hud.PieceIconData pieceIconData = this.m_pieceIcons[i];
			if (i < pieces.Count)
			{
				Piece piece = pieces[i];
				bool flag = player.HaveRequirements(piece, Player.RequirementMode.CanBuild);
				pieceIconData.m_icon.color = (flag ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 0f, 1f, 0f));
			}
			else
			{
				pieceIconData.m_icon.color = Color.white;
			}
		}
		this.m_pieceIconUpdateIndex = 0;
	}

	private void UpdatePieceBar(float dt)
	{
		this.m_pieceBarPosX = Mathf.Lerp(this.m_pieceBarPosX, this.m_pieceBarTargetPosX, 0.1f);
		this.m_pieceListRoot.anchoredPosition.x = Mathf.Round(this.m_pieceBarPosX);
	}

	public void TogglePieceSelection()
	{
		this.m_hoveredPiece = null;
		if (this.m_pieceSelectionWindow.activeSelf)
		{
			this.m_pieceSelectionWindow.SetActive(false);
			return;
		}
		this.m_pieceSelectionWindow.SetActive(true);
		this.UpdateBuild(Player.m_localPlayer, true);
	}

	private void OnClosePieceSelection(UIInputHandler ih)
	{
		Hud.HidePieceSelection();
	}

	public static void HidePieceSelection()
	{
		if (Hud.m_instance == null)
		{
			return;
		}
		Hud.m_instance.m_closePieceSelection = 2;
	}

	public static bool IsPieceSelectionVisible()
	{
		return !(Hud.m_instance == null) && Hud.m_instance.m_buildHud.activeSelf && Hud.m_instance.m_pieceSelectionWindow.activeSelf;
	}

	private void UpdateBuild(Player player, bool forceUpdateAllBuildStatuses)
	{
		if (!player.InPlaceMode())
		{
			this.m_buildHud.SetActive(false);
			this.m_pieceSelectionWindow.SetActive(false);
			return;
		}
		if (this.m_closePieceSelection > 0)
		{
			this.m_closePieceSelection--;
			if (this.m_closePieceSelection <= 0 && this.m_pieceSelectionWindow.activeSelf)
			{
				this.m_pieceSelectionWindow.SetActive(false);
			}
		}
		Piece piece;
		Vector2Int selectedNr;
		int num;
		Piece.PieceCategory pieceCategory;
		bool flag;
		player.GetBuildSelection(out piece, out selectedNr, out num, out pieceCategory, out flag);
		this.m_buildHud.SetActive(true);
		if (this.m_pieceSelectionWindow.activeSelf)
		{
			this.UpdatePieceList(player, selectedNr, pieceCategory, forceUpdateAllBuildStatuses);
			this.m_pieceCategoryRoot.SetActive(flag);
			if (flag)
			{
				for (int i = 0; i < this.m_pieceCategoryTabs.Length; i++)
				{
					GameObject gameObject = this.m_pieceCategoryTabs[i];
					Transform transform = gameObject.transform.Find("Selected");
					string text = string.Concat(new object[]
					{
						this.m_buildCategoryNames[i],
						" [<color=yellow>",
						player.GetAvailableBuildPiecesInCategory((Piece.PieceCategory)i),
						"</color>]"
					});
					if (i == (int)pieceCategory)
					{
						transform.gameObject.SetActive(true);
						transform.GetComponentInChildren<Text>().text = text;
					}
					else
					{
						transform.gameObject.SetActive(false);
						gameObject.GetComponentInChildren<Text>().text = text;
					}
				}
			}
		}
		if (this.m_hoveredPiece && (ZInput.IsGamepadActive() || !player.IsPieceAvailable(this.m_hoveredPiece)))
		{
			this.m_hoveredPiece = null;
		}
		if (this.m_hoveredPiece)
		{
			this.SetupPieceInfo(this.m_hoveredPiece);
			return;
		}
		this.SetupPieceInfo(piece);
	}

	private void SetupPieceInfo(Piece piece)
	{
		if (piece == null)
		{
			this.m_buildSelection.text = Localization.instance.Localize("$hud_nothingtobuild");
			this.m_pieceDescription.text = "";
			this.m_buildIcon.enabled = false;
			for (int i = 0; i < this.m_requirementItems.Length; i++)
			{
				this.m_requirementItems[i].SetActive(false);
			}
			return;
		}
		Player localPlayer = Player.m_localPlayer;
		this.m_buildSelection.text = Localization.instance.Localize(piece.m_name);
		this.m_pieceDescription.text = Localization.instance.Localize(piece.m_description);
		this.m_buildIcon.enabled = true;
		this.m_buildIcon.sprite = piece.m_icon;
		for (int j = 0; j < this.m_requirementItems.Length; j++)
		{
			if (j < piece.m_resources.Length)
			{
				Piece.Requirement req = piece.m_resources[j];
				this.m_requirementItems[j].SetActive(true);
				InventoryGui.SetupRequirement(this.m_requirementItems[j].transform, req, localPlayer, false, 0);
			}
			else
			{
				this.m_requirementItems[j].SetActive(false);
			}
		}
		if (piece.m_craftingStation)
		{
			CraftingStation craftingStation = CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, localPlayer.transform.position);
			GameObject gameObject = this.m_requirementItems[piece.m_resources.Length];
			gameObject.SetActive(true);
			Image component = gameObject.transform.Find("res_icon").GetComponent<Image>();
			Text component2 = gameObject.transform.Find("res_name").GetComponent<Text>();
			Text component3 = gameObject.transform.Find("res_amount").GetComponent<Text>();
			UITooltip component4 = gameObject.GetComponent<UITooltip>();
			component.sprite = piece.m_craftingStation.m_icon;
			component2.text = Localization.instance.Localize(piece.m_craftingStation.m_name);
			component4.m_text = piece.m_craftingStation.m_name;
			if (craftingStation != null)
			{
				craftingStation.ShowAreaMarker();
				component.color = Color.white;
				component3.text = "";
				component3.color = Color.white;
				return;
			}
			component.color = Color.gray;
			component3.text = "None";
			component3.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : Color.white);
		}
	}

	private void UpdateGuardianPower(Player player)
	{
		StatusEffect statusEffect;
		float num;
		player.GetGuardianPowerHUD(out statusEffect, out num);
		if (!statusEffect)
		{
			this.m_gpRoot.gameObject.SetActive(false);
			return;
		}
		this.m_gpRoot.gameObject.SetActive(true);
		this.m_gpIcon.sprite = statusEffect.m_icon;
		this.m_gpIcon.color = ((num <= 0f) ? Color.white : new Color(1f, 0f, 1f, 0f));
		this.m_gpName.text = Localization.instance.Localize(statusEffect.m_name);
		if (num > 0f)
		{
			this.m_gpCooldown.text = StatusEffect.GetTimeString(num, false, false);
			return;
		}
		this.m_gpCooldown.text = Localization.instance.Localize("$hud_ready");
	}

	private void UpdateStatusEffects(List<StatusEffect> statusEffects)
	{
		if (this.m_statusEffects.Count != statusEffects.Count)
		{
			foreach (RectTransform rectTransform in this.m_statusEffects)
			{
				UnityEngine.Object.Destroy(rectTransform.gameObject);
			}
			this.m_statusEffects.Clear();
			for (int i = 0; i < statusEffects.Count; i++)
			{
				RectTransform rectTransform2 = UnityEngine.Object.Instantiate<RectTransform>(this.m_statusEffectTemplate, this.m_statusEffectListRoot);
				rectTransform2.gameObject.SetActive(true);
				rectTransform2.anchoredPosition = new Vector3(-4f - (float)i * this.m_statusEffectSpacing, 0f, 0f);
				this.m_statusEffects.Add(rectTransform2);
			}
		}
		for (int j = 0; j < statusEffects.Count; j++)
		{
			StatusEffect statusEffect = statusEffects[j];
			RectTransform rectTransform3 = this.m_statusEffects[j];
			Image component = rectTransform3.Find("Icon").GetComponent<Image>();
			component.sprite = statusEffect.m_icon;
			if (statusEffect.m_flashIcon)
			{
				component.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? new Color(1f, 0.5f, 0.5f, 1f) : Color.white);
			}
			else
			{
				component.color = Color.white;
			}
			rectTransform3.Find("Cooldown").gameObject.SetActive(statusEffect.m_cooldownIcon);
			rectTransform3.GetComponentInChildren<Text>().text = Localization.instance.Localize(statusEffect.m_name);
			Text component2 = rectTransform3.Find("TimeText").GetComponent<Text>();
			string iconText = statusEffect.GetIconText();
			if (!string.IsNullOrEmpty(iconText))
			{
				component2.gameObject.SetActive(true);
				component2.text = iconText;
			}
			else
			{
				component2.gameObject.SetActive(false);
			}
			if (statusEffect.m_isNew)
			{
				statusEffect.m_isNew = false;
				rectTransform3.GetComponentInChildren<Animator>().SetTrigger("flash");
			}
		}
	}

	private void UpdateEvent(Player player)
	{
		RandomEvent activeEvent = RandEventSystem.instance.GetActiveEvent();
		if (activeEvent != null && !EnemyHud.instance.ShowingBossHud() && activeEvent.GetTime() > 3f)
		{
			this.m_eventBar.SetActive(true);
			this.m_eventName.text = Localization.instance.Localize(activeEvent.GetHudText());
			return;
		}
		this.m_eventBar.SetActive(false);
	}

	public void ToggleBetaTextVisible()
	{
		this.m_betaText.SetActive(!this.m_betaText.activeSelf);
	}

	public void FlashHealthBar()
	{
		this.m_healthAnimator.SetTrigger("Flash");
	}

	public void StaminaBarUppgradeFlash()
	{
		this.m_staminaAnimator.SetBool("Visible", true);
		this.m_staminaAnimator.SetTrigger("Flash");
	}

	public void StaminaBarNoStaminaFlash()
	{
		if (this.m_staminaAnimator.GetCurrentAnimatorStateInfo(0).IsTag("nostamina"))
		{
			return;
		}
		this.m_staminaAnimator.SetBool("Visible", true);
		this.m_staminaAnimator.SetTrigger("NoStamina");
	}

	public static bool IsUserHidden()
	{
		return Hud.m_instance && Hud.m_instance.m_userHidden;
	}

	private static Hud m_instance;

	public GameObject m_rootObject;

	public Text m_buildSelection;

	public Text m_pieceDescription;

	public Image m_buildIcon;

	public GameObject m_buildHud;

	public GameObject m_saveIcon;

	public GameObject m_badConnectionIcon;

	public GameObject m_betaText;

	[Header("Piece")]
	public GameObject[] m_requirementItems = new GameObject[0];

	public GameObject[] m_pieceCategoryTabs = new GameObject[0];

	public GameObject m_pieceSelectionWindow;

	public GameObject m_pieceCategoryRoot;

	public RectTransform m_pieceListRoot;

	public RectTransform m_pieceListMask;

	public GameObject m_pieceIconPrefab;

	public UIInputHandler m_closePieceSelectionButton;

	public EffectList m_selectItemEffect = new EffectList();

	public float m_pieceIconSpacing = 64f;

	private float m_pieceBarPosX;

	private float m_pieceBarTargetPosX;

	private Piece.PieceCategory m_lastPieceCategory = Piece.PieceCategory.Max;

	[Header("Health")]
	public RectTransform m_healthBarRoot;

	public RectTransform m_healthPanel;

	private const float m_healthPanelBuffer = 56f;

	private const float m_healthPanelMinSize = 138f;

	public Animator m_healthAnimator;

	public GuiBar m_healthBarFast;

	public GuiBar m_healthBarSlow;

	public Text m_healthText;

	public Text m_healthMaxText;

	[Header("Food")]
	public Image[] m_foodBars;

	public Image[] m_foodIcons;

	public RectTransform m_foodBarRoot;

	public RectTransform m_foodBaseBar;

	public Image m_foodIcon;

	public Color m_foodColorHungry = Color.white;

	public Color m_foodColorFull = Color.white;

	public Text m_foodText;

	[Header("Action bar")]
	public GameObject m_actionBarRoot;

	public GuiBar m_actionProgress;

	public Text m_actionName;

	[Header("Guardian power")]
	public RectTransform m_gpRoot;

	public Text m_gpName;

	public Text m_gpCooldown;

	public Image m_gpIcon;

	[Header("Stamina")]
	public GameObject m_staminaBar;

	public GuiBar m_staminaBarFast;

	public GuiBar m_staminaBarSlow;

	public Animator m_staminaAnimator;

	private float m_staminaBarBorderBuffer = 16f;

	public RectTransform m_staminaBar2Root;

	public GuiBar m_staminaBar2Fast;

	public GuiBar m_staminaBar2Slow;

	[Header("Loading")]
	public CanvasGroup m_loadingScreen;

	public GameObject m_loadingProgress;

	public GameObject m_sleepingProgress;

	public GameObject m_teleportingProgress;

	public Image m_loadingImage;

	public Text m_loadingTip;

	public bool m_useRandomImages = true;

	public string m_loadingImagePath = "/loadingscreens/";

	public int m_loadingImages = 2;

	public List<string> m_loadingTips = new List<string>();

	[Header("Crosshair")]
	public Image m_crosshair;

	public Image m_crosshairBow;

	public Text m_hoverName;

	public RectTransform m_pieceHealthRoot;

	public GuiBar m_pieceHealthBar;

	public Image m_damageScreen;

	[Header("Target")]
	public GameObject m_targetedAlert;

	public GameObject m_targeted;

	public GameObject m_hidden;

	public GuiBar m_stealthBar;

	[Header("Status effect")]
	public RectTransform m_statusEffectListRoot;

	public RectTransform m_statusEffectTemplate;

	public float m_statusEffectSpacing = 55f;

	private List<RectTransform> m_statusEffects = new List<RectTransform>();

	[Header("Ship hud")]
	public GameObject m_shipHudRoot;

	public GameObject m_shipControlsRoot;

	public GameObject m_rudderLeft;

	public GameObject m_rudderRight;

	public GameObject m_rudderSlow;

	public GameObject m_rudderForward;

	public GameObject m_rudderFastForward;

	public GameObject m_rudderBackward;

	public GameObject m_halfSail;

	public GameObject m_fullSail;

	public GameObject m_rudder;

	public RectTransform m_shipWindIndicatorRoot;

	public Image m_shipWindIcon;

	public RectTransform m_shipWindIconRoot;

	public Image m_shipRudderIndicator;

	public Image m_shipRudderIcon;

	[Header("Event")]
	public GameObject m_eventBar;

	public Text m_eventName;

	private bool m_userHidden;

	private CraftingStation m_currentCraftingStation;

	private List<string> m_buildCategoryNames = new List<string>();

	private List<StatusEffect> m_tempStatusEffects = new List<StatusEffect>();

	private List<Hud.PieceIconData> m_pieceIcons = new List<Hud.PieceIconData>();

	private int m_pieceIconUpdateIndex;

	private bool m_haveSetupLoadScreen;

	private int m_closePieceSelection;

	private Piece m_hoveredPiece;

	private class PieceIconData
	{
		public GameObject m_go;

		public Image m_icon;

		public GameObject m_marker;

		public GameObject m_upgrade;

		public UITooltip m_tooltip;
	}
}
