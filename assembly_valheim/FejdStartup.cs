using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FejdStartup : MonoBehaviour
{
	public static FejdStartup instance
	{
		get
		{
			return FejdStartup.m_instance;
		}
	}

	private void Awake()
	{
		FejdStartup.m_instance = this;
		QualitySettings.maxQueuedFrames = 1;
		Settings.ApplyStartupSettings();
		if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
		{
			ZLog.LogWarning("Server can only run in headless moed");
			Application.Quit();
			return;
		}
		ServerCtrl.Initialize();
		WorldGenerator.Initialize(World.GetMenuWorld());
		if (!global::Console.instance)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_consolePrefab);
		}
		this.m_mainCamera.transform.position = this.m_cameraMarkerMain.transform.position;
		this.m_mainCamera.transform.rotation = this.m_cameraMarkerMain.transform.rotation;
		ZLog.Log("Render threading mode:" + SystemInfo.renderingThreadingMode);
		GoogleAnalyticsV4.instance.StartSession();
		GoogleAnalyticsV4.instance.LogEvent("Game", "Version", global::Version.GetVersionString(), 0L);
		GoogleAnalyticsV4.instance.LogEvent("Game", "SteamID", SteamManager.APP_ID.ToString(), 0L);
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "StartMenu", 0L);
		if (!this.ParseServerArguments())
		{
			return;
		}
		this.InitializeSteam();
	}

	private void OnDestroy()
	{
		FejdStartup.m_instance = null;
	}

	private void Start()
	{
		Application.targetFrameRate = 60;
		this.SetupGui();
		this.SetupObjectDB();
		ZInput.Initialize();
		MusicMan.instance.Reset();
		MusicMan.instance.TriggerMusic("menu");
		this.LoadMainScene();
		this.m_menuAnimator.SetBool("FirstStartup", FejdStartup.m_firstStartup);
		FejdStartup.m_firstStartup = false;
		string @string = PlayerPrefs.GetString("profile");
		if (@string.Length > 0)
		{
			this.SetSelectedProfile(@string);
			return;
		}
		this.m_profiles = PlayerProfile.GetAllPlayerProfiles();
		if (this.m_profiles.Count > 0)
		{
			this.SetSelectedProfile(this.m_profiles[0].GetFilename());
			return;
		}
		this.UpdateCharacterList();
	}

	private void SetupGui()
	{
		this.HideAll();
		this.m_mainMenu.SetActive(true);
		if (SteamManager.APP_ID == 1223920U)
		{
			this.m_betaText.SetActive(true);
			if (!Debug.isDebugBuild && !this.AcceptedNDA())
			{
				this.m_ndaPanel.SetActive(true);
				this.m_mainMenu.SetActive(false);
			}
		}
		this.m_manualIPButton.gameObject.SetActive(false);
		this.m_serverListBaseSize = this.m_serverListRoot.rect.height;
		this.m_worldListBaseSize = this.m_worldListRoot.rect.height;
		this.m_versionLabel.text = "version " + global::Version.GetVersionString();
		Localization.instance.Localize(base.transform);
	}

	private void HideAll()
	{
		this.m_worldVersionPanel.SetActive(false);
		this.m_playerVersionPanel.SetActive(false);
		this.m_newGameVersionPanel.SetActive(false);
		this.m_loading.SetActive(false);
		this.m_characterSelectScreen.SetActive(false);
		this.m_creditsPanel.SetActive(false);
		this.m_startGamePanel.SetActive(false);
		this.m_joinIPPanel.SetActive(false);
		this.m_createWorldPanel.SetActive(false);
		this.m_mainMenu.SetActive(false);
		this.m_ndaPanel.SetActive(false);
		this.m_betaText.SetActive(false);
	}

	private bool InitializeSteam()
	{
		if (SteamManager.Initialize())
		{
			return true;
		}
		ZLog.LogError("Steam is not initialized");
		Application.Quit();
		return false;
	}

	private void HandleStartupJoin()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			string text = commandLineArgs[i];
			ZLog.Log(string.Concat(new object[]
			{
				"ARG ",
				i,
				" ",
				text
			}));
			if (text == "+connect" && i < commandLineArgs.Length - 1)
			{
				string text2 = commandLineArgs[i + 1];
				ZLog.Log("JOIN " + text2);
				ZSteamMatchmaking.instance.QueueServerJoin(text2);
			}
			else if (text == "+connect_lobby" && i < commandLineArgs.Length - 1)
			{
				string s = commandLineArgs[i + 1];
				CSteamID lobbyID = new CSteamID(ulong.Parse(s));
				ZSteamMatchmaking.instance.QueueLobbyJoin(lobbyID);
			}
		}
	}

	private bool ParseServerArguments()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		string text = "Dedicated";
		string password = "";
		string text2 = "";
		int serverPort = 2456;
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			string a = commandLineArgs[i];
			if (a == "-world")
			{
				string text3 = commandLineArgs[i + 1];
				if (text3 != "")
				{
					text = text3;
				}
				i++;
			}
			else if (a == "-name")
			{
				string text4 = commandLineArgs[i + 1];
				if (text4 != "")
				{
					text2 = text4;
				}
				i++;
			}
			else if (a == "-port")
			{
				string text5 = commandLineArgs[i + 1];
				if (text5 != "")
				{
					serverPort = int.Parse(text5);
				}
				i++;
			}
			else if (a == "-password")
			{
				password = commandLineArgs[i + 1];
				i++;
			}
		}
		if (text2 == "")
		{
			text2 = text;
		}
		World createWorld = World.GetCreateWorld(text);
		if (!this.IsPublicPasswordValid(password, createWorld))
		{
			string publicPasswordError = this.GetPublicPasswordError(password, createWorld);
			ZLog.LogError("Error bad password:" + publicPasswordError);
			Application.Quit();
			return false;
		}
		ZNet.SetServer(true, true, true, text2, password, createWorld);
		ZNet.SetServerHost("", 0);
		SteamManager.SetServerPort(serverPort);
		return true;
	}

	private void SetupObjectDB()
	{
		ObjectDB objectDB = base.gameObject.AddComponent<ObjectDB>();
		ObjectDB component = this.m_gameMainPrefab.GetComponent<ObjectDB>();
		objectDB.CopyOtherDB(component);
	}

	private void ShowConnectError()
	{
		ZNet.ConnectionStatus connectionStatus = ZNet.GetConnectionStatus();
		if (connectionStatus != ZNet.ConnectionStatus.Connected && connectionStatus != ZNet.ConnectionStatus.Connecting && connectionStatus != ZNet.ConnectionStatus.None)
		{
			this.m_connectionFailedPanel.SetActive(true);
			switch (connectionStatus)
			{
			case ZNet.ConnectionStatus.ErrorVersion:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_incompatibleversion");
				return;
			case ZNet.ConnectionStatus.ErrorDisconnected:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_disconnected");
				return;
			case ZNet.ConnectionStatus.ErrorConnectFailed:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_failedconnect");
				return;
			case ZNet.ConnectionStatus.ErrorPassword:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_password");
				return;
			case ZNet.ConnectionStatus.ErrorAlreadyConnected:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_alreadyconnected");
				return;
			case ZNet.ConnectionStatus.ErrorBanned:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_banned");
				return;
			case ZNet.ConnectionStatus.ErrorFull:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_serverfull");
				break;
			default:
				return;
			}
		}
	}

	public void OnNewVersionButtonDownload()
	{
		Application.OpenURL(this.m_downloadUrl);
		Application.Quit();
	}

	public void OnNewVersionButtonContinue()
	{
		this.m_newGameVersionPanel.SetActive(false);
	}

	public void OnStartGame()
	{
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "StartGame", 0L);
		this.m_mainMenu.SetActive(false);
		this.ShowCharacterSelection();
	}

	private void ShowStartGame()
	{
		this.m_mainMenu.SetActive(false);
		this.m_startGamePanel.SetActive(true);
		this.m_createWorldPanel.SetActive(false);
	}

	public void OnSelectWorldTab()
	{
		this.UpdateWorldList(true);
		if (this.m_world == null)
		{
			string @string = PlayerPrefs.GetString("world");
			if (@string.Length > 0)
			{
				this.m_world = this.FindWorld(@string);
			}
			if (this.m_world == null)
			{
				this.m_world = ((this.m_worlds.Count > 0) ? this.m_worlds[0] : null);
			}
			if (this.m_world != null)
			{
				this.UpdateWorldList(true);
			}
		}
	}

	private World FindWorld(string name)
	{
		foreach (World world in this.m_worlds)
		{
			if (world.m_name == name)
			{
				return world;
			}
		}
		return null;
	}

	private void UpdateWorldList(bool centerSelection)
	{
		this.m_worlds = World.GetWorldList();
		foreach (GameObject obj in this.m_worldListElements)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_worldListElements.Clear();
		float num = (float)this.m_worlds.Count * this.m_worldListElementStep;
		num = Mathf.Max(this.m_worldListBaseSize, num);
		this.m_worldListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		for (int i = 0; i < this.m_worlds.Count; i++)
		{
			World world = this.m_worlds[i];
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_worldListElement, this.m_worldListRoot);
			gameObject.SetActive(true);
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)i * -this.m_worldListElementStep);
			gameObject.GetComponent<Button>().onClick.AddListener(new UnityAction(this.OnSelectWorld));
			Text component = gameObject.transform.Find("seed").GetComponent<Text>();
			component.text = "Seed:" + world.m_seedName;
			gameObject.transform.Find("name").GetComponent<Text>().text = world.m_name;
			if (world.m_loadError)
			{
				component.text = " [LOAD ERROR]";
			}
			else if (world.m_versionError)
			{
				component.text = " [BAD VERSION]";
			}
			RectTransform rectTransform = gameObject.transform.Find("selected") as RectTransform;
			bool flag = this.m_world != null && world.m_name == this.m_world.m_name;
			rectTransform.gameObject.SetActive(flag);
			if (flag && centerSelection)
			{
				this.m_worldListEnsureVisible.CenterOnItem(rectTransform);
			}
			this.m_worldListElements.Add(gameObject);
		}
	}

	public void OnWorldRemove()
	{
		if (this.m_world == null)
		{
			return;
		}
		this.m_removeWorldName.text = this.m_world.m_name;
		this.m_removeWorldDialog.SetActive(true);
	}

	public void OnButtonRemoveWorldYes()
	{
		World.RemoveWorld(this.m_world.m_name);
		this.m_world = null;
		this.SetSelectedWorld(0, true);
		this.m_removeWorldDialog.SetActive(false);
	}

	public void OnButtonRemoveWorldNo()
	{
		this.m_removeWorldDialog.SetActive(false);
	}

	private void OnSelectWorld()
	{
		GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
		int index = this.FindSelectedWorld(currentSelectedGameObject);
		this.SetSelectedWorld(index, false);
	}

	private void SetSelectedWorld(int index, bool centerSelection)
	{
		if (this.m_worlds.Count == 0)
		{
			return;
		}
		index = Mathf.Clamp(index, 0, this.m_worlds.Count - 1);
		this.m_world = this.m_worlds[index];
		this.UpdateWorldList(centerSelection);
	}

	private int GetSelectedWorld()
	{
		if (this.m_world == null)
		{
			return -1;
		}
		for (int i = 0; i < this.m_worlds.Count; i++)
		{
			if (this.m_worlds[i].m_name == this.m_world.m_name)
			{
				return i;
			}
		}
		return -1;
	}

	private int FindSelectedWorld(GameObject button)
	{
		for (int i = 0; i < this.m_worldListElements.Count; i++)
		{
			if (this.m_worldListElements[i] == button)
			{
				return i;
			}
		}
		return -1;
	}

	public void OnWorldNew()
	{
		this.m_createWorldPanel.SetActive(true);
		this.m_newWorldName.text = "";
		this.m_newWorldSeed.text = World.GenerateSeed();
	}

	public void OnNewWorldDone()
	{
		string text = this.m_newWorldName.text;
		string text2 = this.m_newWorldSeed.text;
		if (World.HaveWorld(text))
		{
			return;
		}
		this.m_world = new World(text, text2);
		this.m_world.SaveWorldMetaData();
		this.UpdateWorldList(true);
		this.ShowStartGame();
		GoogleAnalyticsV4.instance.LogEvent("Menu", "NewWorld", text, 0L);
	}

	public void OnNewWorldBack()
	{
		this.ShowStartGame();
	}

	public void OnWorldStart()
	{
		if (this.m_world == null || this.m_world.m_versionError || this.m_world.m_loadError)
		{
			return;
		}
		PlayerPrefs.SetString("world", this.m_world.m_name);
		bool isOn = this.m_publicServerToggle.isOn;
		bool isOn2 = this.m_openServerToggle.isOn;
		string text = this.m_serverPassword.text;
		ZNet.SetServer(true, isOn2, isOn, this.m_world.m_name, text, this.m_world);
		ZNet.SetServerHost("", 0);
		string eventLabel = "open:" + isOn2.ToString() + ",public:" + isOn.ToString();
		GoogleAnalyticsV4.instance.LogEvent("Menu", "WorldStart", eventLabel, 0L);
		this.TransitionToMainScene();
	}

	private void ShowCharacterSelection()
	{
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "CharacterSelection", 0L);
		ZLog.Log("show character selection");
		this.m_characterSelectScreen.SetActive(true);
		this.m_selectCharacterPanel.SetActive(true);
		this.m_newCharacterPanel.SetActive(false);
	}

	public void OnJoinGame()
	{
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "JoinGame", 0L);
		this.HideAll();
		this.ShowCharacterSelection();
	}

	public void OnServerFilterChanged()
	{
		ZSteamMatchmaking.instance.SetNameFilter(this.m_filterInputField.text);
		ZSteamMatchmaking.instance.SetFriendFilter(this.m_friendFilterSwitch.isOn);
		PlayerPrefs.SetInt("publicfilter", this.m_publicFilterSwitch.isOn ? 1 : 0);
	}

	public void QueueServerListUpdate()
	{
		ZLog.DevLog("Queue serverlist");
		base.CancelInvoke("RequestServerList");
		base.Invoke("RequestServerList", 1f);
		this.m_serverRefreshButton.interactable = false;
	}

	private void RequestServerList()
	{
		ZLog.DevLog("Request serverlist");
	}

	private void UpdateServerList()
	{
		this.m_serverRefreshButton.interactable = !ZSteamMatchmaking.instance.IsUpdating();
		this.m_serverCount.text = this.m_serverListElements.Count.ToString() + " / " + ZSteamMatchmaking.instance.GetTotalNrOfServers();
		if (this.m_serverListRevision == ZSteamMatchmaking.instance.GetServerListRevision())
		{
			return;
		}
		this.m_serverListRevision = ZSteamMatchmaking.instance.GetServerListRevision();
		this.m_serverList.Clear();
		ZSteamMatchmaking.instance.GetServers(this.m_serverList);
		this.m_serverList.Sort((MasterClient.ServerData a, MasterClient.ServerData b) => a.m_name.CompareTo(b.m_name));
		if (!this.m_serverList.Contains(this.m_joinServer))
		{
			ZLog.Log("Serverlist does not contain selected server, clearing");
			if (this.m_serverList.Count > 0)
			{
				this.m_joinServer = this.m_serverList[0];
			}
			else
			{
				this.m_joinServer = null;
			}
		}
		this.UpdateServerListGui(false);
	}

	private void UpdateServerListGui(bool centerSelection)
	{
		if (this.m_serverList.Count != this.m_serverListElements.Count)
		{
			foreach (GameObject obj in this.m_serverListElements)
			{
				UnityEngine.Object.Destroy(obj);
			}
			this.m_serverListElements.Clear();
			float num = (float)this.m_serverList.Count * this.m_serverListElementStep;
			num = Mathf.Max(this.m_serverListBaseSize, num);
			this.m_serverListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
			for (int i = 0; i < this.m_serverList.Count; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_serverListElement, this.m_serverListRoot);
				gameObject.SetActive(true);
				(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)i * -this.m_serverListElementStep);
				gameObject.GetComponent<Button>().onClick.AddListener(new UnityAction(this.OnSelectedServer));
				this.m_serverListElements.Add(gameObject);
			}
		}
		for (int j = 0; j < this.m_serverList.Count; j++)
		{
			MasterClient.ServerData serverData = this.m_serverList[j];
			GameObject gameObject2 = this.m_serverListElements[j];
			gameObject2.GetComponentInChildren<Text>().text = j + ". " + serverData.m_name;
			UITooltip componentInChildren = gameObject2.GetComponentInChildren<UITooltip>();
			if (!string.IsNullOrEmpty(serverData.m_host))
			{
				componentInChildren.m_text = serverData.m_host + ":" + serverData.m_port;
			}
			else
			{
				componentInChildren.m_text = serverData.m_steamHostID.ToString();
			}
			gameObject2.transform.Find("version").GetComponent<Text>().text = serverData.m_version;
			gameObject2.transform.Find("players").GetComponent<Text>().text = string.Concat(new object[]
			{
				"Players:",
				serverData.m_players,
				" / ",
				this.m_serverPlayerLimit
			});
			gameObject2.transform.Find("Private").gameObject.SetActive(serverData.m_password);
			Transform transform = gameObject2.transform.Find("selected");
			bool flag = this.m_joinServer != null && this.m_joinServer.Equals(serverData);
			transform.gameObject.SetActive(flag);
			if (centerSelection && flag)
			{
				this.m_serverListEnsureVisible.CenterOnItem(transform as RectTransform);
			}
		}
	}

	private void OnSelectedServer()
	{
		GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
		int index = this.FindSelectedServer(currentSelectedGameObject);
		this.m_joinServer = this.m_serverList[index];
		this.UpdateServerListGui(false);
	}

	private void SetSelectedServer(int index, bool centerSelection)
	{
		if (this.m_serverList.Count == 0)
		{
			return;
		}
		index = Mathf.Clamp(index, 0, this.m_serverList.Count - 1);
		this.m_joinServer = this.m_serverList[index];
		this.UpdateServerListGui(centerSelection);
	}

	private int GetSelectedServer()
	{
		if (this.m_joinServer == null)
		{
			return -1;
		}
		for (int i = 0; i < this.m_serverList.Count; i++)
		{
			if (this.m_joinServer.Equals(this.m_serverList[i]))
			{
				return i;
			}
		}
		return -1;
	}

	private int FindSelectedServer(GameObject button)
	{
		for (int i = 0; i < this.m_serverListElements.Count; i++)
		{
			if (this.m_serverListElements[i] == button)
			{
				return i;
			}
		}
		return -1;
	}

	public void OnJoinStart()
	{
		this.JoinServer();
	}

	private void JoinServer()
	{
		ZNet.SetServer(false, false, false, "", "", null);
		ZNet.SetServerHost(this.m_joinServer.m_steamHostID);
		GoogleAnalyticsV4.instance.LogEvent("Menu", "JoinServer", "", 0L);
		this.TransitionToMainScene();
	}

	public void OnJoinIPOpen()
	{
		this.m_joinIPPanel.SetActive(true);
	}

	public void OnJoinIPConnect()
	{
		this.m_joinIPPanel.SetActive(true);
		string[] array = this.m_joinIPAddress.text.Split(new char[]
		{
			':'
		});
		if (array.Length == 0)
		{
			return;
		}
		string text = array[0];
		int port = this.m_joinHostPort;
		int num;
		if (array.Length > 1 && int.TryParse(array[1], out num))
		{
			port = num;
		}
		if (text.Length == 0)
		{
			return;
		}
		this.m_joinServer = new MasterClient.ServerData();
		this.m_joinServer.m_host = text;
		this.m_joinServer.m_port = port;
		this.JoinServer();
	}

	public void OnJoinIPBack()
	{
		this.m_joinIPPanel.SetActive(false);
	}

	public void OnServerListTab()
	{
		bool publicFilter = PlayerPrefs.GetInt("publicfilter", 0) == 1;
		this.SetPublicFilter(publicFilter);
		this.QueueServerListUpdate();
		this.UpdateServerListGui(true);
		this.m_filterInputField.ActivateInputField();
	}

	private void SetPublicFilter(bool enabled)
	{
		this.m_friendFilterSwitch.isOn = !enabled;
		this.m_publicFilterSwitch.isOn = enabled;
	}

	public void OnStartGameBack()
	{
		this.m_startGamePanel.SetActive(false);
		this.ShowCharacterSelection();
	}

	public void OnCredits()
	{
		this.m_creditsPanel.SetActive(true);
		this.m_mainMenu.SetActive(false);
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "Credits", 0L);
	}

	public void OnCreditsBack()
	{
		this.m_mainMenu.SetActive(true);
		this.m_creditsPanel.SetActive(false);
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "StartMenu", 0L);
	}

	public void OnSelelectCharacterBack()
	{
		this.m_characterSelectScreen.SetActive(false);
		this.m_mainMenu.SetActive(true);
		this.m_queuedJoinServer = null;
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "StartMenu", 0L);
	}

	public void OnAbort()
	{
		Application.Quit();
	}

	public void OnWorldVersionYes()
	{
		this.m_worldVersionPanel.SetActive(false);
	}

	public void OnPlayerVersionOk()
	{
		this.m_playerVersionPanel.SetActive(false);
	}

	private void FixedUpdate()
	{
		ZInput.FixedUpdate(Time.fixedDeltaTime);
	}

	private void UpdateCursor()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = ZInput.IsMouseActive();
	}

	private void Update()
	{
		ZInput.Update(Time.deltaTime);
		this.UpdateCursor();
		this.UpdateGamepad();
		this.CheckPendingSteamJoinRequest();
		if (MasterClient.instance != null)
		{
			MasterClient.instance.Update(Time.deltaTime);
		}
		if (ZBroastcast.instance != null)
		{
			ZBroastcast.instance.Update(Time.deltaTime);
		}
		this.UpdateCharacterRotation(Time.deltaTime);
		this.UpdateCamera(Time.deltaTime);
		if (this.m_newCharacterPanel.activeInHierarchy)
		{
			this.m_csNewCharacterDone.interactable = (this.m_csNewCharacterName.text.Length >= 3);
		}
		if (this.m_serverListPanel.activeInHierarchy)
		{
			this.m_joinGameButton.interactable = (this.m_joinServer != null);
		}
		if (this.m_createWorldPanel.activeInHierarchy)
		{
			this.m_newWorldDone.interactable = (this.m_newWorldName.text.Length >= 5);
		}
		if (this.m_startGamePanel.activeInHierarchy)
		{
			this.m_worldStart.interactable = this.CanStartServer();
			this.m_worldRemove.interactable = (this.m_world != null);
			this.UpdatePasswordError();
		}
		if (this.m_joinIPPanel.activeInHierarchy)
		{
			this.m_joinIPJoinButton.interactable = (this.m_joinIPAddress.text.Length > 0);
		}
		if (this.m_startGamePanel.activeInHierarchy)
		{
			this.m_publicServerToggle.interactable = this.m_openServerToggle.isOn;
			this.m_serverPassword.interactable = this.m_openServerToggle.isOn;
		}
	}

	private void LateUpdate()
	{
		if (Input.GetKeyDown(KeyCode.F11))
		{
			GameCamera.ScreenShot();
		}
	}

	private void UpdateGamepad()
	{
		if (!ZInput.IsGamepadActive())
		{
			return;
		}
		if (this.m_worldListPanel.activeInHierarchy)
		{
			if (ZInput.GetButtonDown("JoyLStickDown"))
			{
				this.SetSelectedWorld(this.GetSelectedWorld() + 1, true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp"))
			{
				this.SetSelectedWorld(this.GetSelectedWorld() - 1, true);
				return;
			}
		}
		else if (this.m_serverListPanel.activeInHierarchy)
		{
			if (ZInput.GetButtonDown("JoyLStickDown"))
			{
				this.SetSelectedServer(this.GetSelectedServer() + 1, true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp"))
			{
				this.SetSelectedServer(this.GetSelectedServer() - 1, true);
			}
		}
	}

	private void CheckPendingSteamJoinRequest()
	{
		if (ZSteamMatchmaking.instance != null)
		{
			CSteamID joinUserID = ZSteamMatchmaking.instance.GetJoinUserID();
			if (joinUserID != CSteamID.Nil)
			{
				this.m_queuedJoinServer = new MasterClient.ServerData();
				this.m_queuedJoinServer.m_steamHostID = (ulong)joinUserID;
				this.OnJoinGame();
			}
		}
	}

	private void UpdateCharacterRotation(float dt)
	{
		if (this.m_playerInstance == null)
		{
			return;
		}
		if (!this.m_characterSelectScreen.activeInHierarchy)
		{
			return;
		}
		if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
		{
			float axis = Input.GetAxis("Mouse X");
			this.m_playerInstance.transform.Rotate(0f, -axis * this.m_characterRotateSpeed, 0f);
		}
		float joyRightStickX = ZInput.GetJoyRightStickX();
		if (joyRightStickX != 0f)
		{
			this.m_playerInstance.transform.Rotate(0f, -joyRightStickX * this.m_characterRotateSpeedGamepad * dt, 0f);
		}
	}

	private void UpdatePasswordError()
	{
		string text = "";
		if (this.m_publicServerToggle.isOn)
		{
			text = this.GetPublicPasswordError(this.m_serverPassword.text, this.m_world);
		}
		this.m_passwordError.text = text;
	}

	private string GetPublicPasswordError(string password, World world)
	{
		if (password.Length < this.m_minimumPasswordLength)
		{
			return Localization.instance.Localize("$menu_passwordshort");
		}
		if (world != null && (world.m_name.Contains(password) || world.m_seedName.Contains(password)))
		{
			return Localization.instance.Localize("$menu_passwordinvalid");
		}
		return "";
	}

	private bool IsPublicPasswordValid(string password, World world)
	{
		return password.Length >= this.m_minimumPasswordLength && !world.m_name.Contains(password) && !world.m_seedName.Contains(password);
	}

	private bool CanStartServer()
	{
		return this.m_world != null && !this.m_world.m_loadError && !this.m_world.m_versionError && (!this.m_publicServerToggle.isOn || this.IsPublicPasswordValid(this.m_serverPassword.text, this.m_world));
	}

	private void UpdateCamera(float dt)
	{
		Transform transform = this.m_cameraMarkerMain;
		if (this.m_characterSelectScreen.activeSelf)
		{
			transform = this.m_cameraMarkerCharacter;
		}
		else if (this.m_creditsPanel.activeSelf)
		{
			transform = this.m_cameraMarkerCredits;
		}
		else if (this.m_startGamePanel.activeSelf || this.m_joinIPPanel.activeSelf)
		{
			transform = this.m_cameraMarkerGame;
		}
		this.m_mainCamera.transform.position = Vector3.SmoothDamp(this.m_mainCamera.transform.position, transform.position, ref this.camSpeed, 1.5f, 1000f, dt);
		Vector3 forward = Vector3.SmoothDamp(this.m_mainCamera.transform.forward, transform.forward, ref this.camRotSpeed, 1.5f, 1000f, dt);
		forward.Normalize();
		this.m_mainCamera.transform.rotation = Quaternion.LookRotation(forward);
	}

	private void UpdateCharacterList()
	{
		if (this.m_profiles == null)
		{
			this.m_profiles = PlayerProfile.GetAllPlayerProfiles();
		}
		if (this.m_profileIndex >= this.m_profiles.Count)
		{
			this.m_profileIndex = this.m_profiles.Count - 1;
		}
		this.m_csRemoveButton.gameObject.SetActive(this.m_profiles.Count > 0);
		this.m_csStartButton.gameObject.SetActive(this.m_profiles.Count > 0);
		this.m_csNewButton.gameObject.SetActive(this.m_profiles.Count > 0);
		this.m_csNewBigButton.gameObject.SetActive(this.m_profiles.Count == 0);
		this.m_csLeftButton.interactable = (this.m_profileIndex > 0);
		this.m_csRightButton.interactable = (this.m_profileIndex < this.m_profiles.Count - 1);
		if (this.m_profileIndex >= 0 && this.m_profileIndex < this.m_profiles.Count)
		{
			PlayerProfile playerProfile = this.m_profiles[this.m_profileIndex];
			this.m_csName.text = playerProfile.GetName();
			this.m_csName.gameObject.SetActive(true);
			this.SetupCharacterPreview(playerProfile);
			return;
		}
		this.m_csName.gameObject.SetActive(false);
		this.ClearCharacterPreview();
	}

	private void SetSelectedProfile(string filename)
	{
		if (this.m_profiles == null)
		{
			this.m_profiles = PlayerProfile.GetAllPlayerProfiles();
		}
		this.m_profileIndex = 0;
		for (int i = 0; i < this.m_profiles.Count; i++)
		{
			if (this.m_profiles[i].GetFilename() == filename)
			{
				this.m_profileIndex = i;
				break;
			}
		}
		this.UpdateCharacterList();
	}

	public void OnNewCharacterDone()
	{
		string text = this.m_csNewCharacterName.text;
		string text2 = text.ToLower();
		if (PlayerProfile.HaveProfile(text2))
		{
			this.m_newCharacterError.SetActive(true);
			return;
		}
		Player component = this.m_playerInstance.GetComponent<Player>();
		component.GiveDefaultItems();
		PlayerProfile playerProfile = new PlayerProfile(text2);
		playerProfile.SetName(text);
		playerProfile.SavePlayerData(component);
		playerProfile.Save();
		this.m_selectCharacterPanel.SetActive(true);
		this.m_newCharacterPanel.SetActive(false);
		this.m_profiles = null;
		this.SetSelectedProfile(text2);
		GoogleAnalyticsV4.instance.LogEvent("Menu", "NewCharacter", text, 0L);
	}

	public void OnNewCharacterCancel()
	{
		this.m_selectCharacterPanel.SetActive(true);
		this.m_newCharacterPanel.SetActive(false);
		this.UpdateCharacterList();
	}

	public void OnCharacterNew()
	{
		this.m_newCharacterPanel.SetActive(true);
		this.m_selectCharacterPanel.SetActive(false);
		this.m_csNewCharacterName.text = "";
		this.m_newCharacterError.SetActive(false);
		this.SetupCharacterPreview(null);
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "CreateCharacter", 0L);
	}

	public void OnCharacterRemove()
	{
		if (this.m_profileIndex < 0 || this.m_profileIndex >= this.m_profiles.Count)
		{
			return;
		}
		PlayerProfile playerProfile = this.m_profiles[this.m_profileIndex];
		this.m_removeCharacterName.text = playerProfile.GetName();
		this.m_tempRemoveCharacterName = playerProfile.GetFilename();
		this.m_tempRemoveCharacterIndex = this.m_profileIndex;
		this.m_removeCharacterDialog.SetActive(true);
	}

	public void OnButtonRemoveCharacterYes()
	{
		ZLog.Log("Remove character");
		PlayerProfile.RemoveProfile(this.m_tempRemoveCharacterName);
		this.m_profiles.RemoveAt(this.m_tempRemoveCharacterIndex);
		this.UpdateCharacterList();
		this.m_removeCharacterDialog.SetActive(false);
	}

	public void OnButtonRemoveCharacterNo()
	{
		this.m_removeCharacterDialog.SetActive(false);
	}

	public void OnCharacterLeft()
	{
		if (this.m_profileIndex > 0)
		{
			this.m_profileIndex--;
		}
		this.UpdateCharacterList();
	}

	public void OnCharacterRight()
	{
		if (this.m_profileIndex < this.m_profiles.Count - 1)
		{
			this.m_profileIndex++;
		}
		this.UpdateCharacterList();
	}

	public void OnCharacterStart()
	{
		ZLog.Log("OnCharacterStart");
		if (this.m_profileIndex < 0 || this.m_profileIndex >= this.m_profiles.Count)
		{
			return;
		}
		PlayerProfile playerProfile = this.m_profiles[this.m_profileIndex];
		PlayerPrefs.SetString("profile", playerProfile.GetFilename());
		Game.SetProfile(playerProfile.GetFilename());
		this.m_characterSelectScreen.SetActive(false);
		if (this.m_queuedJoinServer != null)
		{
			this.m_joinServer = this.m_queuedJoinServer;
			this.m_queuedJoinServer = null;
			this.JoinServer();
			return;
		}
		this.ShowStartGame();
		if (this.m_worlds.Count == 0)
		{
			this.OnWorldNew();
		}
	}

	private void TransitionToMainScene()
	{
		this.m_menuAnimator.SetTrigger("FadeOut");
		base.Invoke("LoadMainScene", 1.5f);
	}

	private void LoadMainScene()
	{
		this.m_loading.SetActive(true);
		SceneManager.LoadScene("main");
	}

	public void OnButtonSettings()
	{
		UnityEngine.Object.Instantiate<GameObject>(this.m_settingsPrefab, base.transform);
	}

	public void OnButtonFeedback()
	{
		UnityEngine.Object.Instantiate<GameObject>(this.m_feedbackPrefab, base.transform);
	}

	public void OnButtonTwitter()
	{
		Application.OpenURL("https://twitter.com/valheimgame");
	}

	public void OnButtonWebPage()
	{
		Application.OpenURL("http://valheimgame.com/");
	}

	public void OnButtonDiscord()
	{
		Application.OpenURL("https://discord.gg/44qXMJH");
	}

	public void OnButtonFacebook()
	{
		Application.OpenURL("https://www.facebook.com/valheimgame/");
	}

	public void OnButtonShowLog()
	{
		Application.OpenURL(Application.persistentDataPath + "/");
	}

	private bool AcceptedNDA()
	{
		return PlayerPrefs.GetInt("accepted_nda", 0) == 1;
	}

	public void OnButtonNDAAccept()
	{
		PlayerPrefs.SetInt("accepted_nda", 1);
		this.m_ndaPanel.SetActive(false);
		this.m_mainMenu.SetActive(true);
	}

	public void OnButtonNDADecline()
	{
		Application.Quit();
	}

	public void OnConnectionFailedOk()
	{
		this.m_connectionFailedPanel.SetActive(false);
	}

	public Player GetPreviewPlayer()
	{
		if (this.m_playerInstance != null)
		{
			return this.m_playerInstance.GetComponent<Player>();
		}
		return null;
	}

	private void ClearCharacterPreview()
	{
		if (this.m_playerInstance)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_changeEffectPrefab, this.m_characterPreviewPoint.position, this.m_characterPreviewPoint.rotation);
			UnityEngine.Object.Destroy(this.m_playerInstance);
			this.m_playerInstance = null;
		}
	}

	private void SetupCharacterPreview(PlayerProfile profile)
	{
		this.ClearCharacterPreview();
		ZNetView.m_forceDisableInit = true;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_playerPrefab, this.m_characterPreviewPoint.position, this.m_characterPreviewPoint.rotation);
		ZNetView.m_forceDisableInit = false;
		UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
		Animator[] componentsInChildren = gameObject.GetComponentsInChildren<Animator>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].updateMode = AnimatorUpdateMode.Normal;
		}
		Player component = gameObject.GetComponent<Player>();
		if (profile != null)
		{
			profile.LoadPlayerData(component);
		}
		this.m_playerInstance = gameObject;
	}

	private Vector3 camSpeed = Vector3.zero;

	private Vector3 camRotSpeed = Vector3.zero;

	private static FejdStartup m_instance;

	[Header("Start")]
	public Animator m_menuAnimator;

	public GameObject m_worldVersionPanel;

	public GameObject m_playerVersionPanel;

	public GameObject m_newGameVersionPanel;

	public GameObject m_connectionFailedPanel;

	public Text m_connectionFailedError;

	public Text m_newVersionName;

	public GameObject m_loading;

	public Text m_versionLabel;

	public GameObject m_mainMenu;

	public GameObject m_ndaPanel;

	public GameObject m_betaText;

	public GameObject m_characterSelectScreen;

	public GameObject m_selectCharacterPanel;

	public GameObject m_newCharacterPanel;

	public GameObject m_creditsPanel;

	public GameObject m_startGamePanel;

	public GameObject m_createWorldPanel;

	[Header("Camera")]
	public GameObject m_mainCamera;

	public Transform m_cameraMarkerStart;

	public Transform m_cameraMarkerMain;

	public Transform m_cameraMarkerCharacter;

	public Transform m_cameraMarkerCredits;

	public Transform m_cameraMarkerGame;

	public float m_cameraMoveSpeed = 1.5f;

	public float m_cameraMoveSpeedStart = 1.5f;

	[Header("Join")]
	public GameObject m_serverListPanel;

	public Toggle m_publicServerToggle;

	public Toggle m_openServerToggle;

	public InputField m_serverPassword;

	public RectTransform m_serverListRoot;

	public GameObject m_serverListElement;

	public ScrollRectEnsureVisible m_serverListEnsureVisible;

	public float m_serverListElementStep = 28f;

	public Text m_serverCount;

	public Button m_serverRefreshButton;

	public InputField m_filterInputField;

	public Text m_passwordError;

	public Button m_manualIPButton;

	public GameObject m_joinIPPanel;

	public Button m_joinIPJoinButton;

	public InputField m_joinIPAddress;

	public Button m_joinGameButton;

	public Toggle m_friendFilterSwitch;

	public Toggle m_publicFilterSwitch;

	public int m_minimumPasswordLength = 5;

	public float m_characterRotateSpeed = 4f;

	public float m_characterRotateSpeedGamepad = 200f;

	public int m_joinHostPort = 2456;

	public int m_serverPlayerLimit = 10;

	[Header("World")]
	public GameObject m_worldListPanel;

	public RectTransform m_worldListRoot;

	public GameObject m_worldListElement;

	public ScrollRectEnsureVisible m_worldListEnsureVisible;

	public float m_worldListElementStep = 28f;

	public InputField m_newWorldName;

	public InputField m_newWorldSeed;

	public Button m_newWorldDone;

	public Button m_worldStart;

	public Button m_worldRemove;

	public GameObject m_removeWorldDialog;

	public Text m_removeWorldName;

	public GameObject m_removeCharacterDialog;

	public Text m_removeCharacterName;

	[Header("Character selectoin")]
	public Button m_csStartButton;

	public Button m_csNewBigButton;

	public Button m_csNewButton;

	public Button m_csRemoveButton;

	public Button m_csLeftButton;

	public Button m_csRightButton;

	public Button m_csNewCharacterDone;

	public GameObject m_newCharacterError;

	public Text m_csName;

	public InputField m_csNewCharacterName;

	[Header("Misc")]
	public Transform m_characterPreviewPoint;

	public GameObject m_playerPrefab;

	public GameObject m_gameMainPrefab;

	public GameObject m_settingsPrefab;

	public GameObject m_consolePrefab;

	public GameObject m_feedbackPrefab;

	public GameObject m_changeEffectPrefab;

	private string m_downloadUrl = "";

	[TextArea]
	public string m_versionXmlUrl = "https://dl.dropboxusercontent.com/s/5ibm05oelbqt8zq/fejdversion.xml?dl=0";

	private World m_world;

	private MasterClient.ServerData m_joinServer;

	private MasterClient.ServerData m_queuedJoinServer;

	private float m_serverListBaseSize;

	private float m_worldListBaseSize;

	private List<PlayerProfile> m_profiles;

	private int m_profileIndex;

	private string m_tempRemoveCharacterName = "";

	private int m_tempRemoveCharacterIndex = -1;

	private List<GameObject> m_serverListElements = new List<GameObject>();

	private List<MasterClient.ServerData> m_serverList = new List<MasterClient.ServerData>();

	private int m_serverListRevision = -1;

	private List<GameObject> m_worldListElements = new List<GameObject>();

	private List<World> m_worlds;

	private GameObject m_playerInstance;

	private static bool m_firstStartup = true;
}
