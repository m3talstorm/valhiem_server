using System;
using UnityEngine;

public class Menu : MonoBehaviour
{
	public static Menu instance
	{
		get
		{
			return Menu.m_instance;
		}
	}

	private void Start()
	{
		Menu.m_instance = this;
		this.m_root.gameObject.SetActive(false);
	}

	public static bool IsVisible()
	{
		return !(Menu.m_instance == null) && Menu.m_instance.m_hiddenFrames <= 2;
	}

	private void Update()
	{
		if (Game.instance.IsLoggingOut())
		{
			this.m_root.gameObject.SetActive(false);
			return;
		}
		if (this.m_root.gameObject.activeSelf)
		{
			this.m_hiddenFrames = 0;
			if ((Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyMenu")) && !this.m_settingsInstance && !Feedback.IsVisible())
			{
				if (this.m_quitDialog.gameObject.activeSelf)
				{
					this.OnQuitNo();
					return;
				}
				if (this.m_logoutDialog.gameObject.activeSelf)
				{
					this.OnLogoutNo();
					return;
				}
				this.m_root.gameObject.SetActive(false);
				return;
			}
		}
		else
		{
			this.m_hiddenFrames++;
			bool flag = !InventoryGui.IsVisible() && !Minimap.IsOpen() && !global::Console.IsVisible() && !TextInput.IsVisible() && !ZNet.instance.InPasswordDialog() && !StoreGui.IsVisible() && !Hud.IsPieceSelectionVisible();
			if ((Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyMenu")) && flag)
			{
				GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "Menu", 0L);
				this.m_root.gameObject.SetActive(true);
				this.m_menuDialog.gameObject.SetActive(true);
				this.m_logoutDialog.gameObject.SetActive(false);
				this.m_quitDialog.gameObject.SetActive(false);
			}
		}
	}

	public void OnSettings()
	{
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Enter", "Settings", 0L);
		this.m_settingsInstance = UnityEngine.Object.Instantiate<GameObject>(this.m_settingsPrefab, base.transform);
	}

	public void OnQuit()
	{
		this.m_quitDialog.gameObject.SetActive(true);
		this.m_menuDialog.gameObject.SetActive(false);
	}

	public void OnQuitYes()
	{
		GoogleAnalyticsV4.instance.LogEvent("Game", "Quit", "", 0L);
		Application.Quit();
	}

	public void OnQuitNo()
	{
		this.m_quitDialog.gameObject.SetActive(false);
		this.m_menuDialog.gameObject.SetActive(true);
	}

	public void OnLogout()
	{
		this.m_menuDialog.gameObject.SetActive(false);
		this.m_logoutDialog.gameObject.SetActive(true);
	}

	public void OnLogoutYes()
	{
		GoogleAnalyticsV4.instance.LogEvent("Game", "LogOut", "", 0L);
		Game.instance.Logout();
	}

	public void OnLogoutNo()
	{
		this.m_logoutDialog.gameObject.SetActive(false);
		this.m_menuDialog.gameObject.SetActive(true);
	}

	public void OnClose()
	{
		GoogleAnalyticsV4.instance.LogEvent("Screen", "Exit", "Menu", 0L);
		this.m_root.gameObject.SetActive(false);
	}

	public void OnButtonFeedback()
	{
		UnityEngine.Object.Instantiate<GameObject>(this.m_feedbackPrefab, base.transform);
	}

	private GameObject m_settingsInstance;

	private static Menu m_instance;

	public Transform m_root;

	public Transform m_menuDialog;

	public Transform m_quitDialog;

	public Transform m_logoutDialog;

	public GameObject m_settingsPrefab;

	public GameObject m_feedbackPrefab;

	private int m_hiddenFrames;
}
