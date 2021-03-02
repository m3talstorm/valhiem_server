using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Steamworks;

public class ZSteamMatchmaking
{
	public static ZSteamMatchmaking instance
	{
		get
		{
			return ZSteamMatchmaking.m_instance;
		}
	}

	public static void Initialize()
	{
		if (ZSteamMatchmaking.m_instance == null)
		{
			ZSteamMatchmaking.m_instance = new ZSteamMatchmaking();
		}
	}

	private ZSteamMatchmaking()
	{
		this.m_steamServerCallbackHandler = new ISteamMatchmakingServerListResponse(new ISteamMatchmakingServerListResponse.ServerResponded(this.OnServerResponded), new ISteamMatchmakingServerListResponse.ServerFailedToRespond(this.OnServerFailedToRespond), new ISteamMatchmakingServerListResponse.RefreshComplete(this.OnRefreshComplete));
		this.m_joinServerCallbackHandler = new ISteamMatchmakingPingResponse(new ISteamMatchmakingPingResponse.ServerResponded(this.OnJoinServerRespond), new ISteamMatchmakingPingResponse.ServerFailedToRespond(this.OnJoinServerFailed));
		this.m_steamServersConnected = Callback<SteamServersConnected_t>.CreateGameServer(new Callback<SteamServersConnected_t>.DispatchDelegate(this.OnSteamServersConnected));
		this.m_steamServersDisconnected = Callback<SteamServersDisconnected_t>.CreateGameServer(new Callback<SteamServersDisconnected_t>.DispatchDelegate(this.OnSteamServersDisconnected));
		this.m_steamServerConnectFailure = Callback<SteamServerConnectFailure_t>.CreateGameServer(new Callback<SteamServerConnectFailure_t>.DispatchDelegate(this.OnSteamServersConnectFail));
	}

	public byte[] RequestSessionTicket()
	{
		this.ReleaseSessionTicket();
		byte[] array = new byte[1024];
		uint num = 0U;
		this.m_authTicket = SteamUser.GetAuthSessionTicket(array, 1024, out num);
		if (this.m_authTicket == HAuthTicket.Invalid)
		{
			return null;
		}
		byte[] array2 = new byte[num];
		Buffer.BlockCopy(array, 0, array2, 0, (int)num);
		return array2;
	}

	public void ReleaseSessionTicket()
	{
		if (this.m_authTicket == HAuthTicket.Invalid)
		{
			return;
		}
		SteamUser.CancelAuthTicket(this.m_authTicket);
		this.m_authTicket = HAuthTicket.Invalid;
		ZLog.Log("Released session ticket");
	}

	public bool VerifySessionTicket(byte[] ticket, CSteamID steamID)
	{
		return SteamGameServer.BeginAuthSession(ticket, ticket.Length, steamID) == EBeginAuthSessionResult.k_EBeginAuthSessionResultOK;
	}

	private void OnAuthSessionTicketResponse(GetAuthSessionTicketResponse_t data)
	{
		ZLog.Log("Session auth respons callback");
	}

	private void OnSteamServersConnected(SteamServersConnected_t data)
	{
		ZLog.Log("Game server connected");
	}

	private void OnSteamServersDisconnected(SteamServersDisconnected_t data)
	{
		ZLog.LogWarning("Game server disconnected");
	}

	private void OnSteamServersConnectFail(SteamServerConnectFailure_t data)
	{
		ZLog.LogWarning("Game server connected failed");
	}

	private void OnChangeServerRequest(GameServerChangeRequested_t data)
	{
		ZLog.Log("ZSteamMatchmaking got change server request to:" + data.m_rgchServer);
		this.QueueServerJoin(data.m_rgchServer);
	}

	private void OnJoinRequest(GameLobbyJoinRequested_t data)
	{
		ZLog.Log(string.Concat(new object[]
		{
			"ZSteamMatchmaking got join request friend:",
			data.m_steamIDFriend,
			"  lobby:",
			data.m_steamIDLobby
		}));
		if (Game.instance)
		{
			return;
		}
		this.QueueLobbyJoin(data.m_steamIDLobby);
	}

	private IPAddress FindIP(string host)
	{
		IPAddress result;
		try
		{
			IPAddress ipaddress;
			if (IPAddress.TryParse(host, out ipaddress))
			{
				result = ipaddress;
			}
			else
			{
				ZLog.Log("Not an ip address " + host + " doing dns lookup");
				IPHostEntry hostEntry = Dns.GetHostEntry(host);
				if (hostEntry.AddressList.Length == 0)
				{
					ZLog.Log("Dns lookup failed");
					result = null;
				}
				else
				{
					ZLog.Log("Got dns entries: " + hostEntry.AddressList.Length);
					foreach (IPAddress ipaddress2 in hostEntry.AddressList)
					{
						if (ipaddress2.AddressFamily == AddressFamily.InterNetwork)
						{
							return ipaddress2;
						}
					}
					result = null;
				}
			}
		}
		catch (Exception ex)
		{
			ZLog.Log("Exception while finding ip:" + ex.ToString());
			result = null;
		}
		return result;
	}

	public void QueueServerJoin(string addr)
	{
		try
		{
			string[] array = addr.Split(new char[]
			{
				':'
			});
			if (array.Length >= 2)
			{
				IPAddress ipaddress = this.FindIP(array[0]);
				if (ipaddress == null)
				{
					ZLog.Log("Invalid address " + array[0]);
				}
				else
				{
					uint nIP = (uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(ipaddress.GetAddressBytes(), 0));
					int num = int.Parse(array[1]);
					ZLog.Log(string.Concat(new object[]
					{
						"connect to ip:",
						ipaddress.ToString(),
						" port:",
						num
					}));
					this.m_joinAddr.SetIPv4(nIP, (ushort)num);
					this.m_haveJoinAddr = true;
				}
			}
		}
		catch (Exception arg)
		{
			ZLog.Log("Server join exception:" + arg);
		}
	}

	private void OnJoinServerRespond(gameserveritem_t serverData)
	{
		ZLog.Log(string.Concat(new object[]
		{
			"Got join server data ",
			serverData.GetServerName(),
			"  ",
			serverData.m_steamID
		}));
		this.m_joinAddr.SetIPv4(serverData.m_NetAdr.GetIP(), serverData.m_NetAdr.GetConnectionPort());
		this.m_haveJoinAddr = true;
	}

	private void OnJoinServerFailed()
	{
		ZLog.Log("Failed to get join server data");
	}

	public void QueueLobbyJoin(CSteamID lobbyID)
	{
		uint num;
		ushort num2;
		CSteamID csteamID;
		if (SteamMatchmaking.GetLobbyGameServer(lobbyID, out num, out num2, out csteamID))
		{
			ZLog.Log("  hostid: " + csteamID);
			this.m_joinUserID = csteamID;
			this.m_queuedJoinLobby = CSteamID.Nil;
			return;
		}
		ZLog.Log("Failed to get lobby data for lobby " + lobbyID + ", requesting lobby data");
		this.m_queuedJoinLobby = lobbyID;
		SteamMatchmaking.RequestLobbyData(lobbyID);
	}

	private void OnLobbyDataUpdate(LobbyDataUpdate_t data)
	{
		CSteamID csteamID = new CSteamID(data.m_ulSteamIDLobby);
		if (csteamID == this.m_queuedJoinLobby)
		{
			ZLog.Log("Got lobby data, for queued lobby");
			uint num;
			ushort num2;
			CSteamID joinUserID;
			if (SteamMatchmaking.GetLobbyGameServer(csteamID, out num, out num2, out joinUserID))
			{
				this.m_joinUserID = joinUserID;
			}
			this.m_queuedJoinLobby = CSteamID.Nil;
			return;
		}
		ZLog.Log("Got requested lobby data");
		foreach (KeyValuePair<CSteamID, string> keyValuePair in this.m_requestedFriendGames)
		{
			if (keyValuePair.Key == csteamID)
			{
				ServerData lobbyServerData = this.GetLobbyServerData(csteamID);
				if (lobbyServerData != null)
				{
					lobbyServerData.m_name = keyValuePair.Value + " [" + lobbyServerData.m_name + "]";
					this.m_friendServers.Add(lobbyServerData);
					this.m_serverListRevision++;
				}
			}
		}
	}

	public void RegisterServer(string name, bool password, string version, bool publicServer, string worldName)
	{
		this.UnregisterServer();
		SteamGameServer.SetServerName(name);
		SteamGameServer.SetMapName(name);
		SteamGameServer.SetPasswordProtected(password);
		SteamGameServer.SetGameTags(version);
		if (publicServer)
		{
			SteamGameServer.EnableHeartbeats(true);
		}
		this.m_registerServerName = name;
		this.m_registerPassword = password;
		this.m_registerVerson = version;
		ZLog.Log("Registering lobby");
	}

	private void OnLobbyCreated(LobbyCreated_t data, bool ioError)
	{
		ZLog.Log(string.Concat(new object[]
		{
			"Lobby was created ",
			data.m_eResult,
			"  ",
			data.m_ulSteamIDLobby,
			"  error:",
			ioError.ToString()
		}));
		if (ioError)
		{
			return;
		}
		this.m_myLobby = new CSteamID(data.m_ulSteamIDLobby);
		SteamMatchmaking.SetLobbyData(this.m_myLobby, "name", this.m_registerServerName);
		SteamMatchmaking.SetLobbyData(this.m_myLobby, "password", this.m_registerPassword ? "1" : "0");
		SteamMatchmaking.SetLobbyData(this.m_myLobby, "version", this.m_registerVerson);
		SteamMatchmaking.SetLobbyGameServer(this.m_myLobby, 0U, 0, SteamUser.GetSteamID());
	}

	private void OnLobbyEnter(LobbyEnter_t data, bool ioError)
	{
		ZLog.LogWarning("Entering lobby " + data.m_ulSteamIDLobby);
	}

	public void UnregisterServer()
	{
		SteamGameServer.EnableHeartbeats(false);
	}

	public void RequestServerlist()
	{
		this.RequestFriendGames();
		this.RequestPublicLobbies();
		this.RequestDedicatedServers();
	}

	private void RequestFriendGames()
	{
		this.m_friendServers.Clear();
		this.m_requestedFriendGames.Clear();
		int num = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
		if (num == -1)
		{
			ZLog.Log("GetFriendCount returned -1, the current user is not logged in.");
			num = 0;
		}
		for (int i = 0; i < num; i++)
		{
			CSteamID friendByIndex = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
			string friendPersonaName = SteamFriends.GetFriendPersonaName(friendByIndex);
			FriendGameInfo_t friendGameInfo_t;
			if (SteamFriends.GetFriendGamePlayed(friendByIndex, out friendGameInfo_t) && friendGameInfo_t.m_gameID == (CGameID)((ulong)SteamManager.APP_ID) && friendGameInfo_t.m_steamIDLobby != CSteamID.Nil)
			{
				ZLog.Log("Friend is in our game");
				this.m_requestedFriendGames.Add(new KeyValuePair<CSteamID, string>(friendGameInfo_t.m_steamIDLobby, friendPersonaName));
				SteamMatchmaking.RequestLobbyData(friendGameInfo_t.m_steamIDLobby);
			}
		}
		this.m_serverListRevision++;
	}

	private void RequestPublicLobbies()
	{
		SteamAPICall_t hAPICall = SteamMatchmaking.RequestLobbyList();
		this.m_lobbyMatchList.Set(hAPICall, null);
		this.m_refreshingPublicGames = true;
	}

	private void RequestDedicatedServers()
	{
		if (!this.m_refreshingDedicatedServers)
		{
			if (this.m_haveListRequest)
			{
				SteamMatchmakingServers.ReleaseRequest(this.m_serverListRequest);
				this.m_haveListRequest = false;
			}
			this.m_dedicatedServers.Clear();
			this.m_serverListRequest = SteamMatchmakingServers.RequestInternetServerList(SteamUtils.GetAppID(), new MatchMakingKeyValuePair_t[0], 0U, this.m_steamServerCallbackHandler);
			this.m_refreshingDedicatedServers = true;
			this.m_haveListRequest = true;
		}
	}

	private void OnLobbyMatchList(LobbyMatchList_t data, bool ioError)
	{
		this.m_refreshingPublicGames = false;
		this.m_matchmakingServers.Clear();
		int num = 0;
		while ((long)num < (long)((ulong)data.m_nLobbiesMatching))
		{
			CSteamID lobbyByIndex = SteamMatchmaking.GetLobbyByIndex(num);
			ServerData lobbyServerData = this.GetLobbyServerData(lobbyByIndex);
			if (lobbyServerData != null)
			{
				this.m_matchmakingServers.Add(lobbyServerData);
			}
			num++;
		}
		this.m_serverListRevision++;
	}

	private ServerData GetLobbyServerData(CSteamID lobbyID)
	{
		string lobbyData = SteamMatchmaking.GetLobbyData(lobbyID, "name");
		bool password = SteamMatchmaking.GetLobbyData(lobbyID, "password") == "1";
		string lobbyData2 = SteamMatchmaking.GetLobbyData(lobbyID, "version");
		int numLobbyMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
		uint num;
		ushort num2;
		CSteamID that;
		if (SteamMatchmaking.GetLobbyGameServer(lobbyID, out num, out num2, out that))
		{
			return new ServerData
			{
				m_name = lobbyData,
				m_password = password,
				m_version = lobbyData2,
				m_players = numLobbyMembers,
				m_steamHostID = (ulong)that
			};
		}
		ZLog.Log("Failed to get lobby gameserver");
		return null;
	}

	public void GetServers(List<ServerData> allServers)
	{
		if (this.m_friendsFilter)
		{
			this.FilterServers(this.m_friendServers, allServers);
			return;
		}
		this.FilterServers(this.m_matchmakingServers, allServers);
		this.FilterServers(this.m_dedicatedServers, allServers);
	}

	private void FilterServers(List<ServerData> input, List<ServerData> allServers)
	{
		string text = this.m_nameFilter.ToLowerInvariant();
		foreach (ServerData serverData in input)
		{
			if (text.Length == 0 || serverData.m_name.ToLowerInvariant().Contains(text))
			{
				allServers.Add(serverData);
			}
			if (allServers.Count >= 200)
			{
				break;
			}
		}
	}

	public bool GetJoinHost(out CSteamID steamID, out SteamNetworkingIPAddr addr)
	{
		steamID = this.m_joinUserID;
		addr = this.m_joinAddr;
		if (this.m_joinUserID.IsValid() || this.m_haveJoinAddr)
		{
			this.m_joinUserID = CSteamID.Nil;
			this.m_haveJoinAddr = false;
			this.m_joinAddr.Clear();
			return true;
		}
		return false;
	}

	private void OnServerResponded(HServerListRequest request, int iServer)
	{
		gameserveritem_t serverDetails = SteamMatchmakingServers.GetServerDetails(request, iServer);
		string serverName = serverDetails.GetServerName();
		ServerData serverData = new ServerData();
		serverData.m_name = serverName;
		serverData.m_steamHostAddr.SetIPv4(serverDetails.m_NetAdr.GetIP(), serverDetails.m_NetAdr.GetConnectionPort());
		serverData.m_password = serverDetails.m_bPassword;
		serverData.m_players = serverDetails.m_nPlayers;
		serverData.m_version = serverDetails.GetGameTags();
		this.m_dedicatedServers.Add(serverData);
		this.m_updateTriggerAccumulator++;
		if (this.m_updateTriggerAccumulator > 100)
		{
			this.m_updateTriggerAccumulator = 0;
			this.m_serverListRevision++;
		}
	}

	private void OnServerFailedToRespond(HServerListRequest request, int iServer)
	{
	}

	private void OnRefreshComplete(HServerListRequest request, EMatchMakingServerResponse response)
	{
		ZLog.Log(string.Concat(new object[]
		{
			"Refresh complete ",
			this.m_dedicatedServers.Count,
			"  ",
			response
		}));
		this.m_refreshingDedicatedServers = false;
		this.m_serverListRevision++;
	}

	public void SetNameFilter(string filter)
	{
		if (this.m_nameFilter == filter)
		{
			return;
		}
		this.m_nameFilter = filter;
		this.m_serverListRevision++;
	}

	public void SetFriendFilter(bool enabled)
	{
		if (this.m_friendsFilter == enabled)
		{
			return;
		}
		this.m_friendsFilter = enabled;
		this.m_serverListRevision++;
	}

	public int GetServerListRevision()
	{
		return this.m_serverListRevision;
	}

	public bool IsUpdating()
	{
		return this.m_refreshingDedicatedServers || this.m_refreshingPublicGames;
	}

	public int GetTotalNrOfServers()
	{
		return this.m_matchmakingServers.Count + this.m_dedicatedServers.Count + this.m_friendServers.Count;
	}

	private static ZSteamMatchmaking m_instance;

	private const int maxServers = 200;

	private List<ServerData> m_matchmakingServers = new List<ServerData>();

	private List<ServerData> m_dedicatedServers = new List<ServerData>();

	private List<ServerData> m_friendServers = new List<ServerData>();

	private int m_serverListRevision;

	private int m_updateTriggerAccumulator;

	private CallResult<LobbyCreated_t> m_lobbyCreated;

	private CallResult<LobbyMatchList_t> m_lobbyMatchList;

	private CallResult<LobbyEnter_t> m_lobbyEntered;

	private Callback<GameServerChangeRequested_t> m_changeServer;

	private Callback<GameLobbyJoinRequested_t> m_joinRequest;

	private Callback<LobbyDataUpdate_t> m_lobbyDataUpdate;

	private Callback<GetAuthSessionTicketResponse_t> m_authSessionTicketResponse;

	private Callback<SteamServerConnectFailure_t> m_steamServerConnectFailure;

	private Callback<SteamServersConnected_t> m_steamServersConnected;

	private Callback<SteamServersDisconnected_t> m_steamServersDisconnected;

	private CSteamID m_myLobby = CSteamID.Nil;

	private CSteamID m_joinUserID = CSteamID.Nil;

	private CSteamID m_queuedJoinLobby = CSteamID.Nil;

	private bool m_haveJoinAddr;

	private SteamNetworkingIPAddr m_joinAddr;

	private List<KeyValuePair<CSteamID, string>> m_requestedFriendGames = new List<KeyValuePair<CSteamID, string>>();

	private ISteamMatchmakingServerListResponse m_steamServerCallbackHandler;

	private ISteamMatchmakingPingResponse m_joinServerCallbackHandler;

	private HServerQuery m_joinQuery;

	private HServerListRequest m_serverListRequest;

	private bool m_haveListRequest;

	private bool m_refreshingDedicatedServers;

	private bool m_refreshingPublicGames;

	private string m_registerServerName = "";

	private bool m_registerPassword;

	private string m_registerVerson = "";

	private string m_nameFilter = "";

	private bool m_friendsFilter = true;

	private HAuthTicket m_authTicket = HAuthTicket.Invalid;
}
