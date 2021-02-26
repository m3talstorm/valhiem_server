using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
	public static Minimap instance
	{
		get
		{
			return Minimap.m_instance;
		}
	}

	private void Awake()
	{
		Minimap.m_instance = this;
		this.m_largeRoot.SetActive(false);
		this.m_smallRoot.SetActive(true);
	}

	private void OnDestroy()
	{
		Minimap.m_instance = null;
	}

	public static bool IsOpen()
	{
		return Minimap.m_instance && Minimap.m_instance.m_largeRoot.activeSelf;
	}

	public static bool InTextInput()
	{
		return Minimap.m_instance && Minimap.m_instance.m_mode == Minimap.MapMode.Large && Minimap.m_instance.m_wasFocused;
	}

	private void Start()
	{
		this.m_mapTexture = new Texture2D(this.m_textureSize, this.m_textureSize, TextureFormat.RGBA32, false);
		this.m_mapTexture.wrapMode = TextureWrapMode.Clamp;
		this.m_forestMaskTexture = new Texture2D(this.m_textureSize, this.m_textureSize, TextureFormat.RGBA32, false);
		this.m_forestMaskTexture.wrapMode = TextureWrapMode.Clamp;
		this.m_heightTexture = new Texture2D(this.m_textureSize, this.m_textureSize, TextureFormat.RFloat, false);
		this.m_heightTexture.wrapMode = TextureWrapMode.Clamp;
		this.m_fogTexture = new Texture2D(this.m_textureSize, this.m_textureSize, TextureFormat.RGBA32, false);
		this.m_fogTexture.wrapMode = TextureWrapMode.Clamp;
		this.m_explored = new bool[this.m_textureSize * this.m_textureSize];
		this.m_mapImageLarge.material = UnityEngine.Object.Instantiate<Material>(this.m_mapImageLarge.material);
		this.m_mapImageSmall.material = UnityEngine.Object.Instantiate<Material>(this.m_mapImageSmall.material);
		this.m_mapImageLarge.material.SetTexture("_MainTex", this.m_mapTexture);
		this.m_mapImageLarge.material.SetTexture("_MaskTex", this.m_forestMaskTexture);
		this.m_mapImageLarge.material.SetTexture("_HeightTex", this.m_heightTexture);
		this.m_mapImageLarge.material.SetTexture("_FogTex", this.m_fogTexture);
		this.m_mapImageSmall.material.SetTexture("_MainTex", this.m_mapTexture);
		this.m_mapImageSmall.material.SetTexture("_MaskTex", this.m_forestMaskTexture);
		this.m_mapImageSmall.material.SetTexture("_HeightTex", this.m_heightTexture);
		this.m_mapImageSmall.material.SetTexture("_FogTex", this.m_fogTexture);
		this.m_nameInput.gameObject.SetActive(false);
		UIInputHandler component = this.m_mapImageLarge.GetComponent<UIInputHandler>();
		component.m_onRightClick = (Action<UIInputHandler>)Delegate.Combine(component.m_onRightClick, new Action<UIInputHandler>(this.OnMapRightClick));
		component.m_onMiddleClick = (Action<UIInputHandler>)Delegate.Combine(component.m_onMiddleClick, new Action<UIInputHandler>(this.OnMapMiddleClick));
		component.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftDown, new Action<UIInputHandler>(this.OnMapLeftDown));
		component.m_onLeftUp = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftUp, new Action<UIInputHandler>(this.OnMapLeftUp));
		this.SelectIcon(Minimap.PinType.Icon0);
		this.Reset();
	}

	public void Reset()
	{
		Color32[] array = new Color32[this.m_textureSize * this.m_textureSize];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		}
		this.m_fogTexture.SetPixels32(array);
		this.m_fogTexture.Apply();
		for (int j = 0; j < this.m_explored.Length; j++)
		{
			this.m_explored[j] = false;
		}
	}

	public void ForceRegen()
	{
		if (WorldGenerator.instance != null)
		{
			this.GenerateWorldMap();
		}
	}

	private void Update()
	{
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			return;
		}
		if (Utils.GetMainCamera() == null)
		{
			return;
		}
		if (!this.m_hasGenerated)
		{
			if (WorldGenerator.instance == null)
			{
				return;
			}
			this.GenerateWorldMap();
			this.LoadMapData();
			this.m_hasGenerated = true;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		this.UpdateExplore(deltaTime, localPlayer);
		if (localPlayer.IsDead())
		{
			this.SetMapMode(Minimap.MapMode.None);
			return;
		}
		if (this.m_mode == Minimap.MapMode.None)
		{
			this.SetMapMode(Minimap.MapMode.Small);
		}
		bool flag = (Chat.instance == null || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !TextInput.IsVisible() && !Menu.IsVisible() && !InventoryGui.IsVisible();
		if (flag)
		{
			if (Minimap.InTextInput())
			{
				if (Input.GetKeyDown(KeyCode.Escape))
				{
					this.m_namePin = null;
				}
			}
			else if (ZInput.GetButtonDown("Map") || ZInput.GetButtonDown("JoyMap") || (this.m_mode == Minimap.MapMode.Large && (Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))))
			{
				switch (this.m_mode)
				{
				case Minimap.MapMode.None:
					this.SetMapMode(Minimap.MapMode.Small);
					break;
				case Minimap.MapMode.Small:
					this.SetMapMode(Minimap.MapMode.Large);
					break;
				case Minimap.MapMode.Large:
					this.SetMapMode(Minimap.MapMode.Small);
					break;
				}
			}
		}
		if (this.m_mode == Minimap.MapMode.Large)
		{
			this.m_publicPosition.isOn = ZNet.instance.IsReferencePositionPublic();
			this.m_gamepadCrosshair.gameObject.SetActive(ZInput.IsGamepadActive());
		}
		this.UpdateMap(localPlayer, deltaTime, flag);
		this.UpdateDynamicPins(deltaTime);
		this.UpdatePins();
		this.UpdateBiome(localPlayer);
		this.UpdateNameInput();
	}

	private void ShowPinNameInput(Minimap.PinData pin)
	{
		this.m_namePin = pin;
		this.m_nameInput.text = "";
	}

	private void UpdateNameInput()
	{
		if (this.m_namePin == null)
		{
			this.m_wasFocused = false;
		}
		if (this.m_namePin != null && this.m_mode == Minimap.MapMode.Large)
		{
			this.m_nameInput.gameObject.SetActive(true);
			if (!this.m_nameInput.isFocused)
			{
				EventSystem.current.SetSelectedGameObject(this.m_nameInput.gameObject);
			}
			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				string text = this.m_nameInput.text;
				text = text.Replace('$', ' ');
				text = text.Replace('<', ' ');
				text = text.Replace('>', ' ');
				this.m_namePin.m_name = text;
				this.m_namePin = null;
			}
			this.m_wasFocused = true;
			return;
		}
		this.m_nameInput.gameObject.SetActive(false);
	}

	private void UpdateMap(Player player, float dt, bool takeInput)
	{
		if (takeInput)
		{
			if (this.m_mode == Minimap.MapMode.Large)
			{
				float num = 0f;
				num += Input.GetAxis("Mouse ScrollWheel") * this.m_largeZoom * 2f;
				if (ZInput.GetButton("JoyButtonX"))
				{
					Vector3 viewCenterWorldPoint = this.GetViewCenterWorldPoint();
					Chat.instance.SendPing(viewCenterWorldPoint);
				}
				if (ZInput.GetButton("JoyLTrigger"))
				{
					num -= this.m_largeZoom * dt * 2f;
				}
				if (ZInput.GetButton("JoyRTrigger"))
				{
					num += this.m_largeZoom * dt * 2f;
				}
				if (ZInput.GetButtonDown("MapZoomOut") && !Minimap.InTextInput())
				{
					num -= this.m_largeZoom * 0.5f;
				}
				if (ZInput.GetButtonDown("MapZoomIn") && !Minimap.InTextInput())
				{
					num += this.m_largeZoom * 0.5f;
				}
				this.m_largeZoom = Mathf.Clamp(this.m_largeZoom - num, this.m_minZoom, this.m_maxZoom);
			}
			else
			{
				float num2 = 0f;
				if (ZInput.GetButtonDown("MapZoomOut"))
				{
					num2 -= this.m_smallZoom * 0.5f;
				}
				if (ZInput.GetButtonDown("MapZoomIn"))
				{
					num2 += this.m_smallZoom * 0.5f;
				}
				this.m_smallZoom = Mathf.Clamp(this.m_smallZoom - num2, this.m_minZoom, this.m_maxZoom);
			}
		}
		if (this.m_mode == Minimap.MapMode.Large)
		{
			if (this.m_leftDownTime != 0f && this.m_leftDownTime > this.m_clickDuration && !this.m_dragView)
			{
				this.m_dragWorldPos = this.ScreenToWorldPoint(Input.mousePosition);
				this.m_dragView = true;
				this.m_namePin = null;
			}
			this.m_mapOffset.x = this.m_mapOffset.x + ZInput.GetJoyLeftStickX() * dt * 50000f * this.m_largeZoom;
			this.m_mapOffset.z = this.m_mapOffset.z - ZInput.GetJoyLeftStickY() * dt * 50000f * this.m_largeZoom;
			if (this.m_dragView)
			{
				Vector3 b = this.ScreenToWorldPoint(Input.mousePosition) - this.m_dragWorldPos;
				this.m_mapOffset -= b;
				this.CenterMap(player.transform.position + this.m_mapOffset);
				this.m_dragWorldPos = this.ScreenToWorldPoint(Input.mousePosition);
			}
			else
			{
				this.CenterMap(player.transform.position + this.m_mapOffset);
			}
		}
		else
		{
			this.CenterMap(player.transform.position);
		}
		this.UpdateWindMarker();
		this.UpdatePlayerMarker(player, Utils.GetMainCamera().transform.rotation);
	}

	private void SetMapMode(Minimap.MapMode mode)
	{
		if (mode == this.m_mode)
		{
			return;
		}
		this.m_mode = mode;
		switch (mode)
		{
		case Minimap.MapMode.None:
			this.m_largeRoot.SetActive(false);
			this.m_smallRoot.SetActive(false);
			return;
		case Minimap.MapMode.Small:
			this.m_largeRoot.SetActive(false);
			this.m_smallRoot.SetActive(true);
			return;
		case Minimap.MapMode.Large:
			this.m_largeRoot.SetActive(true);
			this.m_smallRoot.SetActive(false);
			this.m_dragView = false;
			this.m_mapOffset = Vector3.zero;
			this.m_namePin = null;
			return;
		default:
			return;
		}
	}

	private void CenterMap(Vector3 centerPoint)
	{
		float x;
		float y;
		this.WorldToMapPoint(centerPoint, out x, out y);
		Rect uvRect = this.m_mapImageSmall.uvRect;
		uvRect.width = this.m_smallZoom;
		uvRect.height = this.m_smallZoom;
		uvRect.center = new Vector2(x, y);
		this.m_mapImageSmall.uvRect = uvRect;
		RectTransform rectTransform = this.m_mapImageLarge.transform as RectTransform;
		float num = rectTransform.rect.width / rectTransform.rect.height;
		Rect uvRect2 = this.m_mapImageSmall.uvRect;
		uvRect2.width = this.m_largeZoom * num;
		uvRect2.height = this.m_largeZoom;
		uvRect2.center = new Vector2(x, y);
		this.m_mapImageLarge.uvRect = uvRect2;
		if (this.m_mode == Minimap.MapMode.Large)
		{
			this.m_mapImageLarge.material.SetFloat("_zoom", this.m_largeZoom);
			this.m_mapImageLarge.material.SetFloat("_pixelSize", 200f / this.m_largeZoom);
			this.m_mapImageLarge.material.SetVector("_mapCenter", centerPoint);
			return;
		}
		this.m_mapImageSmall.material.SetFloat("_zoom", this.m_smallZoom);
		this.m_mapImageSmall.material.SetFloat("_pixelSize", 200f / this.m_smallZoom);
		this.m_mapImageSmall.material.SetVector("_mapCenter", centerPoint);
	}

	private void UpdateDynamicPins(float dt)
	{
		this.UpdateProfilePins();
		this.UpdateShoutPins();
		this.UpdatePingPins();
		this.UpdatePlayerPins(dt);
		this.UpdateLocationPins(dt);
		this.UpdateEventPin(dt);
	}

	private void UpdateProfilePins()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		if (playerProfile.HaveDeathPoint())
		{
			if (this.m_deathPin == null)
			{
				this.m_deathPin = this.AddPin(playerProfile.GetDeathPoint(), Minimap.PinType.Death, "", false, false);
			}
			this.m_deathPin.m_pos = playerProfile.GetDeathPoint();
		}
		else if (this.m_deathPin != null)
		{
			this.RemovePin(this.m_deathPin);
			this.m_deathPin = null;
		}
		if (playerProfile.HaveCustomSpawnPoint())
		{
			if (this.m_spawnPointPin == null)
			{
				this.m_spawnPointPin = this.AddPin(playerProfile.GetCustomSpawnPoint(), Minimap.PinType.Bed, "", false, false);
			}
			this.m_spawnPointPin.m_pos = playerProfile.GetCustomSpawnPoint();
			return;
		}
		if (this.m_spawnPointPin != null)
		{
			this.RemovePin(this.m_spawnPointPin);
			this.m_spawnPointPin = null;
		}
	}

	private void UpdateEventPin(float dt)
	{
		if (Time.time - this.m_updateEventTime < 1f)
		{
			return;
		}
		this.m_updateEventTime = Time.time;
		RandomEvent currentRandomEvent = RandEventSystem.instance.GetCurrentRandomEvent();
		if (currentRandomEvent != null)
		{
			if (this.m_randEventAreaPin == null)
			{
				this.m_randEventAreaPin = this.AddPin(currentRandomEvent.m_pos, Minimap.PinType.EventArea, "", false, false);
				this.m_randEventAreaPin.m_worldSize = RandEventSystem.instance.m_randomEventRange * 2f;
				this.m_randEventAreaPin.m_worldSize *= 0.9f;
			}
			if (this.m_randEventPin == null)
			{
				this.m_randEventPin = this.AddPin(currentRandomEvent.m_pos, Minimap.PinType.RandomEvent, "", false, false);
				this.m_randEventPin.m_animate = true;
				this.m_randEventPin.m_doubleSize = true;
			}
			this.m_randEventAreaPin.m_pos = currentRandomEvent.m_pos;
			this.m_randEventPin.m_pos = currentRandomEvent.m_pos;
			this.m_randEventPin.m_name = Localization.instance.Localize(currentRandomEvent.GetHudText());
			return;
		}
		if (this.m_randEventPin != null)
		{
			this.RemovePin(this.m_randEventPin);
			this.m_randEventPin = null;
		}
		if (this.m_randEventAreaPin != null)
		{
			this.RemovePin(this.m_randEventAreaPin);
			this.m_randEventAreaPin = null;
		}
	}

	private void UpdateLocationPins(float dt)
	{
		this.m_updateLocationsTimer -= dt;
		if (this.m_updateLocationsTimer <= 0f)
		{
			this.m_updateLocationsTimer = 5f;
			Dictionary<Vector3, string> dictionary = new Dictionary<Vector3, string>();
			ZoneSystem.instance.GetLocationIcons(dictionary);
			bool flag = false;
			while (!flag)
			{
				flag = true;
				foreach (KeyValuePair<Vector3, Minimap.PinData> keyValuePair in this.m_locationPins)
				{
					if (!dictionary.ContainsKey(keyValuePair.Key))
					{
						ZLog.DevLog("Minimap: Removing location " + keyValuePair.Value.m_name);
						this.RemovePin(keyValuePair.Value);
						this.m_locationPins.Remove(keyValuePair.Key);
						flag = false;
						break;
					}
				}
			}
			foreach (KeyValuePair<Vector3, string> keyValuePair2 in dictionary)
			{
				if (!this.m_locationPins.ContainsKey(keyValuePair2.Key))
				{
					Sprite locationIcon = this.GetLocationIcon(keyValuePair2.Value);
					if (locationIcon)
					{
						Minimap.PinData pinData = this.AddPin(keyValuePair2.Key, Minimap.PinType.None, "", false, false);
						pinData.m_icon = locationIcon;
						pinData.m_doubleSize = true;
						this.m_locationPins.Add(keyValuePair2.Key, pinData);
						ZLog.Log("Minimap: Adding unique location " + keyValuePair2.Key);
					}
				}
			}
		}
	}

	private Sprite GetLocationIcon(string name)
	{
		foreach (Minimap.LocationSpriteData locationSpriteData in this.m_locationIcons)
		{
			if (locationSpriteData.m_name == name)
			{
				return locationSpriteData.m_icon;
			}
		}
		return null;
	}

	private void UpdatePlayerPins(float dt)
	{
		this.m_tempPlayerInfo.Clear();
		ZNet.instance.GetOtherPublicPlayers(this.m_tempPlayerInfo);
		if (this.m_playerPins.Count != this.m_tempPlayerInfo.Count)
		{
			foreach (Minimap.PinData pin in this.m_playerPins)
			{
				this.RemovePin(pin);
			}
			this.m_playerPins.Clear();
			foreach (ZNet.PlayerInfo playerInfo in this.m_tempPlayerInfo)
			{
				Minimap.PinData item = this.AddPin(Vector3.zero, Minimap.PinType.Player, "", false, false);
				this.m_playerPins.Add(item);
			}
		}
		for (int i = 0; i < this.m_tempPlayerInfo.Count; i++)
		{
			Minimap.PinData pinData = this.m_playerPins[i];
			ZNet.PlayerInfo playerInfo2 = this.m_tempPlayerInfo[i];
			if (pinData.m_name == playerInfo2.m_name)
			{
				pinData.m_pos = Vector3.MoveTowards(pinData.m_pos, playerInfo2.m_position, 200f * dt);
			}
			else
			{
				pinData.m_name = playerInfo2.m_name;
				pinData.m_pos = playerInfo2.m_position;
			}
		}
	}

	private void UpdatePingPins()
	{
		this.m_tempShouts.Clear();
		Chat.instance.GetPingWorldTexts(this.m_tempShouts);
		if (this.m_pingPins.Count != this.m_tempShouts.Count)
		{
			foreach (Minimap.PinData pin in this.m_pingPins)
			{
				this.RemovePin(pin);
			}
			this.m_pingPins.Clear();
			foreach (Chat.WorldTextInstance worldTextInstance in this.m_tempShouts)
			{
				Minimap.PinData pinData = this.AddPin(Vector3.zero, Minimap.PinType.Ping, "", false, false);
				pinData.m_doubleSize = true;
				pinData.m_animate = true;
				this.m_pingPins.Add(pinData);
			}
		}
		for (int i = 0; i < this.m_tempShouts.Count; i++)
		{
			Minimap.PinData pinData2 = this.m_pingPins[i];
			Chat.WorldTextInstance worldTextInstance2 = this.m_tempShouts[i];
			pinData2.m_pos = worldTextInstance2.m_position;
			pinData2.m_name = worldTextInstance2.m_name + ": " + worldTextInstance2.m_text;
		}
	}

	private void UpdateShoutPins()
	{
		this.m_tempShouts.Clear();
		Chat.instance.GetShoutWorldTexts(this.m_tempShouts);
		if (this.m_shoutPins.Count != this.m_tempShouts.Count)
		{
			foreach (Minimap.PinData pin in this.m_shoutPins)
			{
				this.RemovePin(pin);
			}
			this.m_shoutPins.Clear();
			foreach (Chat.WorldTextInstance worldTextInstance in this.m_tempShouts)
			{
				Minimap.PinData pinData = this.AddPin(Vector3.zero, Minimap.PinType.Shout, "", false, false);
				pinData.m_doubleSize = true;
				pinData.m_animate = true;
				this.m_shoutPins.Add(pinData);
			}
		}
		for (int i = 0; i < this.m_tempShouts.Count; i++)
		{
			Minimap.PinData pinData2 = this.m_shoutPins[i];
			Chat.WorldTextInstance worldTextInstance2 = this.m_tempShouts[i];
			pinData2.m_pos = worldTextInstance2.m_position;
			pinData2.m_name = worldTextInstance2.m_name + ": " + worldTextInstance2.m_text;
		}
	}

	private void UpdatePins()
	{
		RawImage rawImage = (this.m_mode == Minimap.MapMode.Large) ? this.m_mapImageLarge : this.m_mapImageSmall;
		float num = (this.m_mode == Minimap.MapMode.Large) ? this.m_pinSizeLarge : this.m_pinSizeSmall;
		RectTransform rectTransform = (this.m_mode == Minimap.MapMode.Large) ? this.m_pinRootLarge : this.m_pinRootSmall;
		if (this.m_mode != Minimap.MapMode.Large)
		{
			float smallZoom = this.m_smallZoom;
		}
		else
		{
			float largeZoom = this.m_largeZoom;
		}
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			if (this.IsPointVisible(pinData.m_pos, rawImage))
			{
				if (pinData.m_uiElement == null || pinData.m_uiElement.parent != rectTransform)
				{
					if (pinData.m_uiElement != null)
					{
						UnityEngine.Object.Destroy(pinData.m_uiElement.gameObject);
					}
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_pinPrefab);
					gameObject.GetComponent<Image>().sprite = pinData.m_icon;
					pinData.m_uiElement = (gameObject.transform as RectTransform);
					pinData.m_uiElement.SetParent(rectTransform);
					float size = pinData.m_doubleSize ? (num * 2f) : num;
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
					pinData.m_checkedElement = gameObject.transform.Find("Checked").gameObject;
					pinData.m_nameElement = gameObject.transform.Find("Name").GetComponent<Text>();
				}
				float mx;
				float my;
				this.WorldToMapPoint(pinData.m_pos, out mx, out my);
				Vector2 anchoredPosition = this.MapPointToLocalGuiPos(mx, my, rawImage);
				pinData.m_uiElement.anchoredPosition = anchoredPosition;
				if (pinData.m_animate)
				{
					float num2 = pinData.m_doubleSize ? (num * 2f) : num;
					num2 *= 0.8f + Mathf.Sin(Time.time * 5f) * 0.2f;
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num2);
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num2);
				}
				if (pinData.m_worldSize > 0f)
				{
					Vector2 size2 = new Vector2(pinData.m_worldSize / this.m_pixelSize / (float)this.m_textureSize, pinData.m_worldSize / this.m_pixelSize / (float)this.m_textureSize);
					Vector2 vector = this.MapSizeToLocalGuiSize(size2, rawImage);
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, vector.x);
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, vector.y);
				}
				pinData.m_checkedElement.SetActive(pinData.m_checked);
				if (pinData.m_name.Length > 0 && this.m_mode == Minimap.MapMode.Large && this.m_largeZoom < this.m_showNamesZoom)
				{
					pinData.m_nameElement.gameObject.SetActive(true);
					pinData.m_nameElement.text = Localization.instance.Localize(pinData.m_name);
				}
				else
				{
					pinData.m_nameElement.gameObject.SetActive(false);
				}
			}
			else if (pinData.m_uiElement != null)
			{
				UnityEngine.Object.Destroy(pinData.m_uiElement.gameObject);
				pinData.m_uiElement = null;
			}
		}
	}

	private void UpdateWindMarker()
	{
		Quaternion quaternion = Quaternion.LookRotation(EnvMan.instance.GetWindDir());
		this.m_windMarker.rotation = Quaternion.Euler(0f, 0f, -quaternion.eulerAngles.y);
	}

	private void UpdatePlayerMarker(Player player, Quaternion playerRot)
	{
		Vector3 position = player.transform.position;
		Vector3 eulerAngles = playerRot.eulerAngles;
		this.m_smallMarker.rotation = Quaternion.Euler(0f, 0f, -eulerAngles.y);
		if (this.m_mode == Minimap.MapMode.Large && this.IsPointVisible(position, this.m_mapImageLarge))
		{
			this.m_largeMarker.gameObject.SetActive(true);
			this.m_largeMarker.rotation = this.m_smallMarker.rotation;
			float mx;
			float my;
			this.WorldToMapPoint(position, out mx, out my);
			Vector2 anchoredPosition = this.MapPointToLocalGuiPos(mx, my, this.m_mapImageLarge);
			this.m_largeMarker.anchoredPosition = anchoredPosition;
		}
		else
		{
			this.m_largeMarker.gameObject.SetActive(false);
		}
		Ship controlledShip = player.GetControlledShip();
		if (controlledShip)
		{
			this.m_smallShipMarker.gameObject.SetActive(true);
			Vector3 eulerAngles2 = controlledShip.transform.rotation.eulerAngles;
			this.m_smallShipMarker.rotation = Quaternion.Euler(0f, 0f, -eulerAngles2.y);
			if (this.m_mode == Minimap.MapMode.Large)
			{
				this.m_largeShipMarker.gameObject.SetActive(true);
				Vector3 position2 = controlledShip.transform.position;
				float mx2;
				float my2;
				this.WorldToMapPoint(position2, out mx2, out my2);
				Vector2 anchoredPosition2 = this.MapPointToLocalGuiPos(mx2, my2, this.m_mapImageLarge);
				this.m_largeShipMarker.anchoredPosition = anchoredPosition2;
				this.m_largeShipMarker.rotation = this.m_smallShipMarker.rotation;
				return;
			}
		}
		else
		{
			this.m_smallShipMarker.gameObject.SetActive(false);
			this.m_largeShipMarker.gameObject.SetActive(false);
		}
	}

	private Vector2 MapPointToLocalGuiPos(float mx, float my, RawImage img)
	{
		Vector2 result = default(Vector2);
		result.x = (mx - img.uvRect.xMin) / img.uvRect.width;
		result.y = (my - img.uvRect.yMin) / img.uvRect.height;
		result.x *= img.rectTransform.rect.width;
		result.y *= img.rectTransform.rect.height;
		return result;
	}

	private Vector2 MapSizeToLocalGuiSize(Vector2 size, RawImage img)
	{
		size.x /= img.uvRect.width;
		size.y /= img.uvRect.height;
		return new Vector2(size.x * img.rectTransform.rect.width, size.y * img.rectTransform.rect.height);
	}

	private bool IsPointVisible(Vector3 p, RawImage map)
	{
		float num;
		float num2;
		this.WorldToMapPoint(p, out num, out num2);
		return num > map.uvRect.xMin && num < map.uvRect.xMax && num2 > map.uvRect.yMin && num2 < map.uvRect.yMax;
	}

	public void ExploreAll()
	{
		for (int i = 0; i < this.m_textureSize; i++)
		{
			for (int j = 0; j < this.m_textureSize; j++)
			{
				this.Explore(j, i);
			}
		}
		this.m_fogTexture.Apply();
	}

	private void WorldToMapPoint(Vector3 p, out float mx, out float my)
	{
		int num = this.m_textureSize / 2;
		mx = p.x / this.m_pixelSize + (float)num;
		my = p.z / this.m_pixelSize + (float)num;
		mx /= (float)this.m_textureSize;
		my /= (float)this.m_textureSize;
	}

	private Vector3 MapPointToWorld(float mx, float my)
	{
		int num = this.m_textureSize / 2;
		mx *= (float)this.m_textureSize;
		my *= (float)this.m_textureSize;
		mx -= (float)num;
		my -= (float)num;
		mx *= this.m_pixelSize;
		my *= this.m_pixelSize;
		return new Vector3(mx, 0f, my);
	}

	private void WorldToPixel(Vector3 p, out int px, out int py)
	{
		int num = this.m_textureSize / 2;
		px = Mathf.RoundToInt(p.x / this.m_pixelSize + (float)num);
		py = Mathf.RoundToInt(p.z / this.m_pixelSize + (float)num);
	}

	private void UpdateExplore(float dt, Player player)
	{
		this.m_exploreTimer += Time.deltaTime;
		if (this.m_exploreTimer > this.m_exploreInterval)
		{
			this.m_exploreTimer = 0f;
			this.Explore(player.transform.position, this.m_exploreRadius);
		}
	}

	private void Explore(Vector3 p, float radius)
	{
		int num = (int)Mathf.Ceil(radius / this.m_pixelSize);
		bool flag = false;
		int num2;
		int num3;
		this.WorldToPixel(p, out num2, out num3);
		for (int i = num3 - num; i <= num3 + num; i++)
		{
			for (int j = num2 - num; j <= num2 + num; j++)
			{
				if (j >= 0 && i >= 0 && j < this.m_textureSize && i < this.m_textureSize && new Vector2((float)(j - num2), (float)(i - num3)).magnitude <= (float)num && this.Explore(j, i))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.m_fogTexture.Apply();
		}
	}

	private bool Explore(int x, int y)
	{
		if (this.m_explored[y * this.m_textureSize + x])
		{
			return false;
		}
		this.m_fogTexture.SetPixel(x, y, new Color32(0, 0, 0, 0));
		this.m_explored[y * this.m_textureSize + x] = true;
		return true;
	}

	private bool IsExplored(Vector3 worldPos)
	{
		int num;
		int num2;
		this.WorldToPixel(worldPos, out num, out num2);
		return num >= 0 && num < this.m_textureSize && num2 >= 0 && num2 < this.m_textureSize && this.m_explored[num2 * this.m_textureSize + num];
	}

	private float GetHeight(int x, int y)
	{
		return this.m_heightTexture.GetPixel(x, y).r;
	}

	private void GenerateWorldMap()
	{
		int num = this.m_textureSize / 2;
		float num2 = this.m_pixelSize / 2f;
		Color32[] array = new Color32[this.m_textureSize * this.m_textureSize];
		Color32[] array2 = new Color32[this.m_textureSize * this.m_textureSize];
		Color[] array3 = new Color[this.m_textureSize * this.m_textureSize];
		for (int i = 0; i < this.m_textureSize; i++)
		{
			for (int j = 0; j < this.m_textureSize; j++)
			{
				float wx = (float)(j - num) * this.m_pixelSize + num2;
				float wy = (float)(i - num) * this.m_pixelSize + num2;
				Heightmap.Biome biome = WorldGenerator.instance.GetBiome(wx, wy);
				float biomeHeight = WorldGenerator.instance.GetBiomeHeight(biome, wx, wy);
				array[i * this.m_textureSize + j] = this.GetPixelColor(biome);
				array2[i * this.m_textureSize + j] = this.GetMaskColor(wx, wy, biomeHeight, biome);
				array3[i * this.m_textureSize + j] = new Color(biomeHeight, 0f, 0f);
			}
		}
		this.m_forestMaskTexture.SetPixels32(array2);
		this.m_forestMaskTexture.Apply();
		this.m_mapTexture.SetPixels32(array);
		this.m_mapTexture.Apply();
		this.m_heightTexture.SetPixels(array3);
		this.m_heightTexture.Apply();
	}

	private Color GetMaskColor(float wx, float wy, float height, Heightmap.Biome biome)
	{
		if (height < ZoneSystem.instance.m_waterLevel)
		{
			return this.noForest;
		}
		if (biome == Heightmap.Biome.Meadows)
		{
			if (!WorldGenerator.InForest(new Vector3(wx, 0f, wy)))
			{
				return this.noForest;
			}
			return this.forest;
		}
		else if (biome == Heightmap.Biome.Plains)
		{
			if (WorldGenerator.GetForestFactor(new Vector3(wx, 0f, wy)) >= 0.8f)
			{
				return this.noForest;
			}
			return this.forest;
		}
		else
		{
			if (biome == Heightmap.Biome.BlackForest || biome == Heightmap.Biome.Mistlands)
			{
				return this.forest;
			}
			return this.noForest;
		}
	}

	private Color GetPixelColor(Heightmap.Biome biome)
	{
		if (biome <= Heightmap.Biome.Plains)
		{
			switch (biome)
			{
			case Heightmap.Biome.Meadows:
				return this.m_meadowsColor;
			case Heightmap.Biome.Swamp:
				return this.m_swampColor;
			case (Heightmap.Biome)3:
				break;
			case Heightmap.Biome.Mountain:
				return this.m_mountainColor;
			default:
				if (biome == Heightmap.Biome.BlackForest)
				{
					return this.m_blackforestColor;
				}
				if (biome == Heightmap.Biome.Plains)
				{
					return this.m_heathColor;
				}
				break;
			}
		}
		else if (biome <= Heightmap.Biome.DeepNorth)
		{
			if (biome == Heightmap.Biome.AshLands)
			{
				return this.m_ashlandsColor;
			}
			if (biome == Heightmap.Biome.DeepNorth)
			{
				return this.m_deepnorthColor;
			}
		}
		else
		{
			if (biome == Heightmap.Biome.Ocean)
			{
				return Color.white;
			}
			if (biome == Heightmap.Biome.Mistlands)
			{
				return this.m_mistlandsColor;
			}
		}
		return Color.white;
	}

	private void LoadMapData()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		if (playerProfile.GetMapData() != null)
		{
			this.SetMapData(playerProfile.GetMapData());
		}
	}

	public void SaveMapData()
	{
		Game.instance.GetPlayerProfile().SetMapData(this.GetMapData());
	}

	private byte[] GetMapData()
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write(Minimap.MAPVERSION);
		zpackage.Write(this.m_textureSize);
		for (int i = 0; i < this.m_explored.Length; i++)
		{
			zpackage.Write(this.m_explored[i]);
		}
		int num = 0;
		using (List<Minimap.PinData>.Enumerator enumerator = this.m_pins.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_save)
				{
					num++;
				}
			}
		}
		zpackage.Write(num);
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			if (pinData.m_save)
			{
				zpackage.Write(pinData.m_name);
				zpackage.Write(pinData.m_pos);
				zpackage.Write((int)pinData.m_type);
				zpackage.Write(pinData.m_checked);
			}
		}
		zpackage.Write(ZNet.instance.IsReferencePositionPublic());
		return zpackage.GetArray();
	}

	private void SetMapData(byte[] data)
	{
		ZPackage zpackage = new ZPackage(data);
		int num = zpackage.ReadInt();
		int num2 = zpackage.ReadInt();
		if (this.m_textureSize != num2)
		{
			ZLog.LogWarning(string.Concat(new object[]
			{
				"Missmatching mapsize ",
				this.m_mapTexture,
				" vs ",
				num2
			}));
			return;
		}
		this.Reset();
		for (int i = 0; i < this.m_explored.Length; i++)
		{
			if (zpackage.ReadBool())
			{
				int x = i % num2;
				int y = i / num2;
				this.Explore(x, y);
			}
		}
		if (num >= 2)
		{
			int num3 = zpackage.ReadInt();
			this.ClearPins();
			for (int j = 0; j < num3; j++)
			{
				string name = zpackage.ReadString();
				Vector3 pos = zpackage.ReadVector3();
				Minimap.PinType type = (Minimap.PinType)zpackage.ReadInt();
				bool isChecked = num >= 3 && zpackage.ReadBool();
				this.AddPin(pos, type, name, true, isChecked);
			}
		}
		if (num >= 4)
		{
			bool publicReferencePosition = zpackage.ReadBool();
			ZNet.instance.SetPublicReferencePosition(publicReferencePosition);
		}
		this.m_fogTexture.Apply();
	}

	public bool RemovePin(Vector3 pos, float radius)
	{
		Minimap.PinData closestPin = this.GetClosestPin(pos, radius);
		if (closestPin != null)
		{
			this.RemovePin(closestPin);
			return true;
		}
		return false;
	}

	private Minimap.PinData GetClosestPin(Vector3 pos, float radius)
	{
		Minimap.PinData pinData = null;
		float num = 999999f;
		foreach (Minimap.PinData pinData2 in this.m_pins)
		{
			if (pinData2.m_save)
			{
				float num2 = Utils.DistanceXZ(pos, pinData2.m_pos);
				if (num2 < radius && (num2 < num || pinData == null))
				{
					pinData = pinData2;
					num = num2;
				}
			}
		}
		return pinData;
	}

	public void RemovePin(Minimap.PinData pin)
	{
		if (pin.m_uiElement)
		{
			UnityEngine.Object.Destroy(pin.m_uiElement.gameObject);
		}
		this.m_pins.Remove(pin);
	}

	public void ShowPointOnMap(Vector3 point)
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		this.SetMapMode(Minimap.MapMode.Large);
		this.m_mapOffset = point - Player.m_localPlayer.transform.position;
	}

	public bool DiscoverLocation(Vector3 pos, Minimap.PinType type, string name)
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		if (this.HaveSimilarPin(pos, type, name, true))
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_pin_exist", 0, null);
			this.ShowPointOnMap(pos);
			return false;
		}
		Sprite sprite = this.GetSprite(type);
		Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "$msg_pin_added: " + name, 0, sprite);
		this.AddPin(pos, type, name, true, false);
		this.ShowPointOnMap(pos);
		return true;
	}

	private bool HaveSimilarPin(Vector3 pos, Minimap.PinType type, string name, bool save)
	{
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			if (pinData.m_name == name && pinData.m_type == type && pinData.m_save == save && Utils.DistanceXZ(pos, pinData.m_pos) < 1f)
			{
				return true;
			}
		}
		return false;
	}

	public Minimap.PinData AddPin(Vector3 pos, Minimap.PinType type, string name, bool save, bool isChecked)
	{
		Minimap.PinData pinData = new Minimap.PinData();
		pinData.m_type = type;
		pinData.m_name = name;
		pinData.m_pos = pos;
		pinData.m_icon = this.GetSprite(type);
		pinData.m_save = save;
		pinData.m_checked = isChecked;
		this.m_pins.Add(pinData);
		return pinData;
	}

	private Sprite GetSprite(Minimap.PinType type)
	{
		if (type == Minimap.PinType.None)
		{
			return null;
		}
		return this.m_icons.Find((Minimap.SpriteData x) => x.m_name == type).m_icon;
	}

	private Vector3 GetViewCenterWorldPoint()
	{
		Rect uvRect = this.m_mapImageLarge.uvRect;
		float mx = uvRect.xMin + 0.5f * uvRect.width;
		float my = uvRect.yMin + 0.5f * uvRect.height;
		return this.MapPointToWorld(mx, my);
	}

	private Vector3 ScreenToWorldPoint(Vector3 mousePos)
	{
		Vector2 screenPoint = mousePos;
		RectTransform rectTransform = this.m_mapImageLarge.transform as RectTransform;
		Vector2 point;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out point))
		{
			Vector2 vector = Rect.PointToNormalized(rectTransform.rect, point);
			Rect uvRect = this.m_mapImageLarge.uvRect;
			float mx = uvRect.xMin + vector.x * uvRect.width;
			float my = uvRect.yMin + vector.y * uvRect.height;
			return this.MapPointToWorld(mx, my);
		}
		return Vector3.zero;
	}

	private void OnMapLeftDown(UIInputHandler handler)
	{
		if (Time.time - this.m_leftClickTime < 0.3f)
		{
			this.OnMapDblClick();
			this.m_leftClickTime = 0f;
			this.m_leftDownTime = 0f;
			return;
		}
		this.m_leftClickTime = Time.time;
		this.m_leftDownTime = Time.time;
	}

	private void OnMapLeftUp(UIInputHandler handler)
	{
		if (this.m_leftDownTime != 0f)
		{
			if (Time.time - this.m_leftDownTime < this.m_clickDuration)
			{
				this.OnMapLeftClick();
			}
			this.m_leftDownTime = 0f;
		}
		this.m_dragView = false;
	}

	public void OnMapDblClick()
	{
		Vector3 pos = this.ScreenToWorldPoint(Input.mousePosition);
		Minimap.PinData pin = this.AddPin(pos, this.m_selectedType, "", true, false);
		this.ShowPinNameInput(pin);
	}

	public void OnMapLeftClick()
	{
		ZLog.Log("Left click");
		Vector3 pos = this.ScreenToWorldPoint(Input.mousePosition);
		Minimap.PinData closestPin = this.GetClosestPin(pos, this.m_removeRadius * (this.m_largeZoom * 2f));
		if (closestPin != null)
		{
			closestPin.m_checked = !closestPin.m_checked;
		}
	}

	public void OnMapMiddleClick(UIInputHandler handler)
	{
		Vector3 position = this.ScreenToWorldPoint(Input.mousePosition);
		Chat.instance.SendPing(position);
	}

	public void OnMapRightClick(UIInputHandler handler)
	{
		ZLog.Log("Right click");
		Vector3 pos = this.ScreenToWorldPoint(Input.mousePosition);
		this.RemovePin(pos, this.m_removeRadius * (this.m_largeZoom * 2f));
		this.m_namePin = null;
	}

	public void OnPressedIcon0()
	{
		this.SelectIcon(Minimap.PinType.Icon0);
	}

	public void OnPressedIcon1()
	{
		this.SelectIcon(Minimap.PinType.Icon1);
	}

	public void OnPressedIcon2()
	{
		this.SelectIcon(Minimap.PinType.Icon2);
	}

	public void OnPressedIcon3()
	{
		this.SelectIcon(Minimap.PinType.Icon3);
	}

	public void OnPressedIcon4()
	{
		this.SelectIcon(Minimap.PinType.Icon4);
	}

	public void OnTogglePublicPosition()
	{
		ZNet.instance.SetPublicReferencePosition(this.m_publicPosition.isOn);
	}

	private void SelectIcon(Minimap.PinType type)
	{
		this.m_selectedType = type;
		this.m_selectedIcon0.enabled = false;
		this.m_selectedIcon1.enabled = false;
		this.m_selectedIcon2.enabled = false;
		this.m_selectedIcon3.enabled = false;
		this.m_selectedIcon4.enabled = false;
		switch (type)
		{
		case Minimap.PinType.Icon0:
			this.m_selectedIcon0.enabled = true;
			return;
		case Minimap.PinType.Icon1:
			this.m_selectedIcon1.enabled = true;
			return;
		case Minimap.PinType.Icon2:
			this.m_selectedIcon2.enabled = true;
			return;
		case Minimap.PinType.Icon3:
			this.m_selectedIcon3.enabled = true;
			return;
		case Minimap.PinType.Death:
		case Minimap.PinType.Bed:
			break;
		case Minimap.PinType.Icon4:
			this.m_selectedIcon4.enabled = true;
			break;
		default:
			return;
		}
	}

	private void ClearPins()
	{
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			if (pinData.m_uiElement != null)
			{
				UnityEngine.Object.Destroy(pinData.m_uiElement);
			}
		}
		this.m_pins.Clear();
		this.m_deathPin = null;
	}

	private void UpdateBiome(Player player)
	{
		if (this.m_mode != Minimap.MapMode.Large || !ZInput.IsMouseActive())
		{
			Heightmap.Biome currentBiome = player.GetCurrentBiome();
			if (currentBiome != this.m_biome)
			{
				this.m_biome = currentBiome;
				string text = Localization.instance.Localize("$biome_" + currentBiome.ToString().ToLower());
				this.m_biomeNameSmall.text = text;
				this.m_biomeNameLarge.text = text;
				this.m_biomeNameSmall.GetComponent<Animator>().SetTrigger("pulse");
			}
			return;
		}
		Vector3 vector = this.ScreenToWorldPoint(Input.mousePosition);
		if (this.IsExplored(vector))
		{
			Heightmap.Biome biome = WorldGenerator.instance.GetBiome(vector);
			string text2 = Localization.instance.Localize("$biome_" + biome.ToString().ToLower());
			this.m_biomeNameLarge.text = text2;
			return;
		}
		this.m_biomeNameLarge.text = "";
	}

	private Color forest = new Color(1f, 0f, 0f, 0f);

	private Color noForest = new Color(0f, 0f, 0f, 0f);

	private static int MAPVERSION = 4;

	private static Minimap m_instance;

	public GameObject m_smallRoot;

	public GameObject m_largeRoot;

	public RawImage m_mapImageSmall;

	public RawImage m_mapImageLarge;

	public RectTransform m_pinRootSmall;

	public RectTransform m_pinRootLarge;

	public Text m_biomeNameSmall;

	public Text m_biomeNameLarge;

	public RectTransform m_smallShipMarker;

	public RectTransform m_largeShipMarker;

	public RectTransform m_smallMarker;

	public RectTransform m_largeMarker;

	public RectTransform m_windMarker;

	public RectTransform m_gamepadCrosshair;

	public Toggle m_publicPosition;

	public Image m_selectedIcon0;

	public Image m_selectedIcon1;

	public Image m_selectedIcon2;

	public Image m_selectedIcon3;

	public Image m_selectedIcon4;

	public GameObject m_pinPrefab;

	public InputField m_nameInput;

	public int m_textureSize = 256;

	public float m_pixelSize = 64f;

	public float m_minZoom = 0.01f;

	public float m_maxZoom = 1f;

	public float m_showNamesZoom = 0.5f;

	public float m_exploreInterval = 2f;

	public float m_exploreRadius = 100f;

	public float m_removeRadius = 128f;

	public float m_pinSizeSmall = 32f;

	public float m_pinSizeLarge = 48f;

	public float m_clickDuration = 0.25f;

	public List<Minimap.SpriteData> m_icons = new List<Minimap.SpriteData>();

	public List<Minimap.LocationSpriteData> m_locationIcons = new List<Minimap.LocationSpriteData>();

	public Color m_meadowsColor = new Color(0.45f, 1f, 0.43f);

	public Color m_ashlandsColor = new Color(1f, 0.2f, 0.2f);

	public Color m_blackforestColor = new Color(0f, 0.7f, 0f);

	public Color m_deepnorthColor = new Color(1f, 1f, 1f);

	public Color m_heathColor = new Color(1f, 1f, 0.2f);

	public Color m_swampColor = new Color(0.6f, 0.5f, 0.5f);

	public Color m_mountainColor = new Color(1f, 1f, 1f);

	public Color m_mistlandsColor = new Color(0.5f, 0.5f, 0.5f);

	private Minimap.PinData m_namePin;

	private Minimap.PinType m_selectedType;

	private Minimap.PinData m_deathPin;

	private Minimap.PinData m_spawnPointPin;

	private Dictionary<Vector3, Minimap.PinData> m_locationPins = new Dictionary<Vector3, Minimap.PinData>();

	private float m_updateLocationsTimer;

	private List<Minimap.PinData> m_pingPins = new List<Minimap.PinData>();

	private List<Minimap.PinData> m_shoutPins = new List<Minimap.PinData>();

	private List<Chat.WorldTextInstance> m_tempShouts = new List<Chat.WorldTextInstance>();

	private List<Minimap.PinData> m_playerPins = new List<Minimap.PinData>();

	private List<ZNet.PlayerInfo> m_tempPlayerInfo = new List<ZNet.PlayerInfo>();

	private Minimap.PinData m_randEventPin;

	private Minimap.PinData m_randEventAreaPin;

	private float m_updateEventTime;

	private bool[] m_explored;

	private List<Minimap.PinData> m_pins = new List<Minimap.PinData>();

	private Texture2D m_forestMaskTexture;

	private Texture2D m_mapTexture;

	private Texture2D m_heightTexture;

	private Texture2D m_fogTexture;

	private float m_largeZoom = 0.1f;

	private float m_smallZoom = 0.01f;

	private Heightmap.Biome m_biome;

	private Minimap.MapMode m_mode = Minimap.MapMode.Small;

	private float m_exploreTimer;

	private bool m_hasGenerated;

	private bool m_dragView = true;

	private Vector3 m_mapOffset = Vector3.zero;

	private float m_leftDownTime;

	private float m_leftClickTime;

	private Vector3 m_dragWorldPos = Vector3.zero;

	private bool m_wasFocused;

	private enum MapMode
	{
		None,
		Small,
		Large
	}

	public enum PinType
	{
		Icon0,
		Icon1,
		Icon2,
		Icon3,
		Death,
		Bed,
		Icon4,
		Shout,
		None,
		Boss,
		Player,
		RandomEvent,
		Ping,
		EventArea
	}

	public class PinData
	{
		public string m_name;

		public Minimap.PinType m_type;

		public Sprite m_icon;

		public Vector3 m_pos;

		public bool m_save;

		public bool m_checked;

		public bool m_doubleSize;

		public bool m_animate;

		public float m_worldSize;

		public RectTransform m_uiElement;

		public GameObject m_checkedElement;

		public Text m_nameElement;
	}

	[Serializable]
	public struct SpriteData
	{
		public Minimap.PinType m_name;

		public Sprite m_icon;
	}

	[Serializable]
	public struct LocationSpriteData
	{
		public string m_name;

		public Sprite m_icon;
	}
}
