using System;
using System.IO;
using System.Linq;
using System.Text;
using Steamworks;
using UnityEngine;

[DisallowMultipleComponent]
public class SteamManager : MonoBehaviour
{
	public static SteamManager instance
	{
		get
		{
			return SteamManager.s_instance;
		}
	}

	public static bool Initialize()
	{
		if (SteamManager.s_instance == null)
		{
			new GameObject("SteamManager").AddComponent<SteamManager>();
		}
		return SteamManager.Initialized;
	}

	public static bool Initialized
	{
		get
		{
			return SteamManager.s_instance != null && SteamManager.s_instance.m_bInitialized;
		}
	}

	private static void SteamAPIDebugTextHook(int nSeverity, StringBuilder pchDebugText)
	{
		Debug.LogWarning(pchDebugText);
	}

	public static void SetServerPort(int port)
	{
		SteamManager.m_serverPort = port;
	}

	private uint LoadAPPID()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("SteamAppId");
		if (environmentVariable != null)
		{
			ZLog.Log("Using environment steamid " + environmentVariable);
			return uint.Parse(environmentVariable);
		}
		try
		{
			string s = File.ReadAllText("steam_appid.txt");
			ZLog.Log("Using steam_appid.txt");
			return uint.Parse(s);
		}
		catch
		{
		}
		ZLog.LogWarning("Failed to find APPID");
		return 0U;
	}

	private void Awake()
	{
		if (SteamManager.s_instance != null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		SteamManager.s_instance = this;
		SteamManager.APP_ID = this.LoadAPPID();
		ZLog.Log("Using steam APPID:" + SteamManager.APP_ID.ToString());
		if (!SteamManager.ACCEPTED_APPIDs.Contains(SteamManager.APP_ID))
		{
			ZLog.Log("Invalid APPID");
			Application.Quit();
			return;
		}
		if (SteamManager.s_EverInialized)
		{
			throw new Exception("Tried to Initialize the SteamAPI twice in one session!");
		}
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		if (!Packsize.Test())
		{
			Debug.LogError("[Steamworks.NET] Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.", this);
		}
		if (!DllCheck.Test())
		{
			Debug.LogError("[Steamworks.NET] DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.", this);
		}
		try
		{
			if (SteamAPI.RestartAppIfNecessary((AppId_t)SteamManager.APP_ID))
			{
				Application.Quit();
				return;
			}
		}
		catch (DllNotFoundException arg)
		{
			Debug.LogError("[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" + arg, this);
			Application.Quit();
			return;
		}
		this.m_bInitialized = GameServer.Init(0U, (ushort)(SteamManager.m_serverPort + 2), (ushort)SteamManager.m_serverPort, (ushort)(SteamManager.m_serverPort + 1), EServerMode.eServerModeNoAuthentication, "1.0.0.0");
		if (!this.m_bInitialized)
		{
			Debug.LogError("[Steamworks.NET] GameServer.Init() failed.", this);
			return;
		}
		SteamGameServer.SetProduct("valheim");
		SteamGameServer.SetModDir("valheim");
		SteamGameServer.SetDedicatedServer(true);
		SteamGameServer.SetMaxPlayerCount(64);
		SteamGameServer.LogOnAnonymous();
		ZLog.Log("Server ID " + SteamGameServer.GetSteamID());
		ZSteamMatchmaking.Initialize();
		ZLog.Log("Steam game server initialized");
		SteamManager.s_EverInialized = true;
	}

	private void OnEnable()
	{
		if (SteamManager.s_instance == null)
		{
			SteamManager.s_instance = this;
		}
		bool bInitialized = this.m_bInitialized;
	}

	private void OnDestroy()
	{
		ZLog.Log("Steam manager on destroy");
		if (SteamManager.s_instance != this)
		{
			return;
		}
		SteamManager.s_instance = null;
		if (!this.m_bInitialized)
		{
			return;
		}
		GameServer.Shutdown();
	}

	private void Update()
	{
		if (!this.m_bInitialized)
		{
			return;
		}
		GameServer.RunCallbacks();
	}

	public static uint[] ACCEPTED_APPIDs = new uint[]
	{
		1223920U,
		892970U
	};

	public static uint APP_ID = 0U;

	private static int m_serverPort = 2456;

	private static SteamManager s_instance;

	private static bool s_EverInialized;

	private bool m_bInitialized;

	private SteamAPIWarningMessageHook_t m_SteamAPIWarningMessageHook;
}
