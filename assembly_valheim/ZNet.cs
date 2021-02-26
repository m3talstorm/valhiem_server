using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Steamworks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ZNet : MonoBehaviour
{
	public static ZNet instance
	{
		get
		{
			return ZNet.m_instance;
		}
	}

	private void Awake()
	{
		ZNet.m_instance = this;
		this.m_routedRpc = new ZRoutedRpc(ZNet.m_isServer);
		this.m_zdoMan = new ZDOMan(this.m_zdoSectorsWidth);
		this.m_passwordDialog.gameObject.SetActive(false);
		WorldGenerator.Deitialize();
		if (!SteamManager.Initialize())
		{
			return;
		}
		ZSteamMatchmaking.Initialize();
		if (ZNet.m_isServer)
		{
			this.m_adminList = new SyncedList(Utils.GetSaveDataPath() + "/adminlist.txt", "List admin players ID  ONE per line");
			this.m_bannedList = new SyncedList(Utils.GetSaveDataPath() + "/bannedlist.txt", "List banned players ID  ONE per line");
			this.m_permittedList = new SyncedList(Utils.GetSaveDataPath() + "/permittedlist.txt", "List permitted players ID ONE per line");
			if (ZNet.m_world == null)
			{
				ZNet.m_publicServer = false;
				ZNet.m_world = World.GetDevWorld();
			}
			if (ZNet.m_openServer)
			{
				ZSteamSocket zsteamSocket = new ZSteamSocket();
				zsteamSocket.StartHost();
				this.m_hostSocket = zsteamSocket;
				bool password = ZNet.m_serverPassword != "";
				string versionString = global::Version.GetVersionString();
				ZSteamMatchmaking.instance.RegisterServer(ZNet.m_ServerName, password, versionString, ZNet.m_publicServer, ZNet.m_world.m_seedName);
			}
			WorldGenerator.Initialize(ZNet.m_world);
			this.LoadWorld();
			ZNet.m_connectionStatus = ZNet.ConnectionStatus.Connected;
		}
		else
		{
			ZLog.Log("Connecting to server " + ZNet.m_serverSteamID);
			this.Connect(new CSteamID(ZNet.m_serverSteamID));
		}
		this.m_routedRpc.SetUID(this.m_zdoMan.GetMyID());
		if (this.IsServer())
		{
			this.SendPlayerList();
		}
	}

	private string GetPublicIP()
	{
		string result;
		try
		{
			string text = Utils.DownloadString("http://checkip.dyndns.org/", 5000);
			text = new Regex("\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}").Matches(text)[0].ToString();
			ZLog.Log("Got public ip respons:" + text);
			result = text;
		}
		catch (Exception ex)
		{
			ZLog.Log("Failed to get public ip:" + ex.ToString());
			result = "";
		}
		return result;
	}

	public void Shutdown()
	{
		ZLog.Log("ZNet Shutdown");
		this.Save();
		this.StopAll();
		base.enabled = false;
	}

	private void StopAll()
	{
		if (this.m_haveStoped)
		{
			return;
		}
		this.m_haveStoped = true;
		if (this.m_saveThread != null && this.m_saveThread.IsAlive)
		{
			this.m_saveThread.Join();
			this.m_saveThread = null;
		}
		this.m_zdoMan.ShutDown();
		this.SendDisconnect();
		ZSteamMatchmaking.instance.ReleaseSessionTicket();
		ZSteamMatchmaking.instance.UnregisterServer();
		if (this.m_hostSocket != null)
		{
			this.m_hostSocket.Dispose();
		}
		if (this.m_serverConnector != null)
		{
			this.m_serverConnector.Dispose();
		}
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			znetPeer.Dispose();
		}
		this.m_peers.Clear();
	}

	private void OnDestroy()
	{
		ZLog.Log("ZNet OnDestroy");
		if (ZNet.m_instance == this)
		{
			ZNet.m_instance = null;
		}
	}

	public void Connect(CSteamID hostID)
	{
		ZNetPeer peer = new ZNetPeer(new ZSteamSocket(hostID), true);
		this.OnNewConnection(peer);
		ZNet.m_connectionStatus = ZNet.ConnectionStatus.Connecting;
	}

	public void Connect(string host, int port)
	{
		this.m_serverConnector = new ZConnector2(host, port);
		ZNet.m_connectionStatus = ZNet.ConnectionStatus.Connecting;
	}

	private void UpdateClientConnector(float dt)
	{
		if (this.m_serverConnector != null && this.m_serverConnector.UpdateStatus(dt, true))
		{
			ZSocket2 zsocket = this.m_serverConnector.Complete();
			if (zsocket != null)
			{
				ZLog.Log("Connection established to " + this.m_serverConnector.GetEndPointString());
				ZNetPeer peer = new ZNetPeer(zsocket, true);
				this.OnNewConnection(peer);
			}
			else
			{
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorConnectFailed;
				ZLog.Log("Failed to connect to server");
			}
			this.m_serverConnector.Dispose();
			this.m_serverConnector = null;
		}
	}

	private void OnNewConnection(ZNetPeer peer)
	{
		this.m_peers.Add(peer);
		peer.m_rpc.Register<ZPackage>("PeerInfo", new Action<ZRpc, ZPackage>(this.RPC_PeerInfo));
		peer.m_rpc.Register("Disconnect", new ZRpc.RpcMethod.Method(this.RPC_Disconnect));
		if (ZNet.m_isServer)
		{
			peer.m_rpc.Register("ServerHandshake", new ZRpc.RpcMethod.Method(this.RPC_ServerHandshake));
			return;
		}
		peer.m_rpc.Register<int>("Error", new Action<ZRpc, int>(this.RPC_Error));
		peer.m_rpc.Register<bool>("ClientHandshake", new Action<ZRpc, bool>(this.RPC_ClientHandshake));
		peer.m_rpc.Invoke("ServerHandshake", Array.Empty<object>());
	}

	private void RPC_ServerHandshake(ZRpc rpc)
	{
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer == null)
		{
			return;
		}
		ZLog.Log("Got handshake from client " + peer.m_socket.GetEndPointString());
		this.ClearPlayerData(peer);
		bool flag = !string.IsNullOrEmpty(ZNet.m_serverPassword);
		peer.m_rpc.Invoke("ClientHandshake", new object[]
		{
			flag
		});
	}

	private void UpdatePassword()
	{
		if (this.m_passwordDialog.gameObject.activeSelf)
		{
			this.m_passwordDialog.GetComponentInChildren<InputField>().ActivateInputField();
		}
	}

	public bool InPasswordDialog()
	{
		return this.m_passwordDialog.gameObject.activeSelf;
	}

	private void RPC_ClientHandshake(ZRpc rpc, bool needPassword)
	{
		if (needPassword)
		{
			this.m_passwordDialog.gameObject.SetActive(true);
			InputField componentInChildren = this.m_passwordDialog.GetComponentInChildren<InputField>();
			componentInChildren.text = "";
			componentInChildren.ActivateInputField();
			this.m_passwordDialog.GetComponentInChildren<InputFieldSubmit>().m_onSubmit = new Action<string>(this.OnPasswordEnter);
			this.m_tempPasswordRPC = rpc;
			return;
		}
		this.SendPeerInfo(rpc, "");
	}

	private void OnPasswordEnter(string pwd)
	{
		if (!this.m_tempPasswordRPC.IsConnected())
		{
			return;
		}
		this.m_passwordDialog.gameObject.SetActive(false);
		this.SendPeerInfo(this.m_tempPasswordRPC, pwd);
		this.m_tempPasswordRPC = null;
	}

	private void SendPeerInfo(ZRpc rpc, string password = "")
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write(this.GetUID());
		zpackage.Write(global::Version.GetVersionString());
		zpackage.Write(this.m_referencePosition);
		zpackage.Write(Game.instance.GetPlayerProfile().GetName());
		if (this.IsServer())
		{
			zpackage.Write(ZNet.m_world.m_name);
			zpackage.Write(ZNet.m_world.m_seed);
			zpackage.Write(ZNet.m_world.m_seedName);
			zpackage.Write(ZNet.m_world.m_uid);
			zpackage.Write(ZNet.m_world.m_worldGenVersion);
			zpackage.Write(this.m_netTime);
		}
		else
		{
			string data = string.IsNullOrEmpty(password) ? "" : ZNet.HashPassword(password);
			zpackage.Write(data);
			byte[] array = ZSteamMatchmaking.instance.RequestSessionTicket();
			if (array == null)
			{
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorConnectFailed;
				return;
			}
			zpackage.Write(array);
		}
		rpc.Invoke("PeerInfo", new object[]
		{
			zpackage
		});
	}

	private void RPC_PeerInfo(ZRpc rpc, ZPackage pkg)
	{
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer == null)
		{
			return;
		}
		long num = pkg.ReadLong();
		string text = pkg.ReadString();
		string endPointString = peer.m_socket.GetEndPointString();
		string hostName = peer.m_socket.GetHostName();
		ZLog.Log("VERSION check their:" + text + "  mine:" + global::Version.GetVersionString());
		if (text != global::Version.GetVersionString())
		{
			if (ZNet.m_isServer)
			{
				rpc.Invoke("Error", new object[]
				{
					3
				});
			}
			else
			{
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorVersion;
			}
			ZLog.Log(string.Concat(new string[]
			{
				"Peer ",
				endPointString,
				" has incompatible version, mine:",
				global::Version.GetVersionString(),
				" remote ",
				text
			}));
			return;
		}
		Vector3 refPos = pkg.ReadVector3();
		string text2 = pkg.ReadString();
		if (ZNet.m_isServer)
		{
			if (!this.IsAllowed(hostName, text2))
			{
				rpc.Invoke("Error", new object[]
				{
					8
				});
				ZLog.Log(string.Concat(new string[]
				{
					"Player ",
					text2,
					" : ",
					hostName,
					" is blacklisted or not in whitelist."
				}));
				return;
			}
			string b = pkg.ReadString();
			ZSteamSocket zsteamSocket = peer.m_socket as ZSteamSocket;
			byte[] ticket = pkg.ReadByteArray();
			if (!ZSteamMatchmaking.instance.VerifySessionTicket(ticket, zsteamSocket.GetPeerID()))
			{
				ZLog.Log("Peer " + endPointString + " has invalid session ticket");
				rpc.Invoke("Error", new object[]
				{
					8
				});
				return;
			}
			if (this.GetNrOfPlayers() >= this.m_serverPlayerLimit)
			{
				rpc.Invoke("Error", new object[]
				{
					9
				});
				ZLog.Log("Peer " + endPointString + " disconnected due to server is full");
				return;
			}
			if (ZNet.m_serverPassword != b)
			{
				rpc.Invoke("Error", new object[]
				{
					6
				});
				ZLog.Log("Peer " + endPointString + " has wrong password");
				return;
			}
			if (this.IsConnected(num))
			{
				rpc.Invoke("Error", new object[]
				{
					7
				});
				ZLog.Log(string.Concat(new object[]
				{
					"Already connected to peer with UID:",
					num,
					"  ",
					endPointString
				}));
				return;
			}
		}
		else
		{
			ZNet.m_world = new World();
			ZNet.m_world.m_name = pkg.ReadString();
			ZNet.m_world.m_seed = pkg.ReadInt();
			ZNet.m_world.m_seedName = pkg.ReadString();
			ZNet.m_world.m_uid = pkg.ReadLong();
			ZNet.m_world.m_worldGenVersion = pkg.ReadInt();
			WorldGenerator.Initialize(ZNet.m_world);
			this.m_netTime = pkg.ReadDouble();
		}
		peer.m_refPos = refPos;
		peer.m_uid = num;
		peer.m_playerName = text2;
		rpc.Register<Vector3, bool>("RefPos", new Action<ZRpc, Vector3, bool>(this.RPC_RefPos));
		rpc.Register<ZPackage>("PlayerList", new Action<ZRpc, ZPackage>(this.RPC_PlayerList));
		rpc.Register<string>("RemotePrint", new Action<ZRpc, string>(this.RPC_RemotePrint));
		if (ZNet.m_isServer)
		{
			rpc.Register<ZDOID>("CharacterID", new Action<ZRpc, ZDOID>(this.RPC_CharacterID));
			rpc.Register<string>("Kick", new Action<ZRpc, string>(this.RPC_Kick));
			rpc.Register<string>("Ban", new Action<ZRpc, string>(this.RPC_Ban));
			rpc.Register<string>("Unban", new Action<ZRpc, string>(this.RPC_Unban));
			rpc.Register("PrintBanned", new ZRpc.RpcMethod.Method(this.RPC_PrintBanned));
		}
		else
		{
			rpc.Register<double>("NetTime", new Action<ZRpc, double>(this.RPC_NetTime));
		}
		if (ZNet.m_isServer)
		{
			this.SendPeerInfo(rpc, "");
			this.SendPlayerList();
		}
		else
		{
			ZNet.m_connectionStatus = ZNet.ConnectionStatus.Connected;
		}
		this.m_zdoMan.AddPeer(peer);
		this.m_routedRpc.AddPeer(peer);
	}

	private void SendDisconnect()
	{
		ZLog.Log("Sending disconnect msg");
		foreach (ZNetPeer peer in this.m_peers)
		{
			this.SendDisconnect(peer);
		}
	}

	private void SendDisconnect(ZNetPeer peer)
	{
		if (peer.m_rpc != null)
		{
			ZLog.Log("Sent to " + peer.m_socket.GetEndPointString());
			peer.m_rpc.Invoke("Disconnect", Array.Empty<object>());
		}
	}

	private void RPC_Disconnect(ZRpc rpc)
	{
		ZLog.Log("RPC_Disconnect ");
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer != null)
		{
			if (peer.m_server)
			{
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorDisconnected;
			}
			this.Disconnect(peer);
		}
	}

	private void RPC_Error(ZRpc rpc, int error)
	{
		ZNet.m_connectionStatus = (ZNet.ConnectionStatus)error;
		ZLog.Log("Got connectoin error msg " + (ZNet.ConnectionStatus)error);
	}

	public bool IsConnected(long uid)
	{
		if (uid == this.GetUID())
		{
			return true;
		}
		using (List<ZNetPeer>.Enumerator enumerator = this.m_peers.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_uid == uid)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void ClearPlayerData(ZNetPeer peer)
	{
		this.m_routedRpc.RemovePeer(peer);
		this.m_zdoMan.RemovePeer(peer);
	}

	public void Disconnect(ZNetPeer peer)
	{
		this.ClearPlayerData(peer);
		this.m_peers.Remove(peer);
		peer.Dispose();
		if (ZNet.m_isServer)
		{
			this.SendPlayerList();
		}
	}

	private void FixedUpdate()
	{
		this.UpdateNetTime(Time.fixedDeltaTime);
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		ZSteamSocket.Update();
		if (this.IsServer())
		{
			this.UpdateBanList(deltaTime);
		}
		this.CheckForIncommingServerConnections();
		this.UpdatePeers(deltaTime);
		this.UpdateStats(deltaTime);
		this.SendPeriodicData(deltaTime);
		this.m_zdoMan.Update(deltaTime);
		this.UpdateSave();
		this.UpdatePassword();
	}

	private void UpdateNetTime(float dt)
	{
		if (this.IsServer())
		{
			if (this.GetNrOfPlayers() > 0)
			{
				this.m_netTime += (double)dt;
				return;
			}
		}
		else
		{
			this.m_netTime += (double)dt;
		}
	}

	private void UpdateBanList(float dt)
	{
		this.m_banlistTimer += dt;
		if (this.m_banlistTimer > 5f)
		{
			this.m_banlistTimer = 0f;
			this.CheckWhiteList();
			foreach (string user in this.m_bannedList.GetList())
			{
				this.InternalKick(user);
			}
		}
	}

	private void CheckWhiteList()
	{
		if (this.m_permittedList.Count() == 0)
		{
			return;
		}
		bool flag = false;
		while (!flag)
		{
			flag = true;
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					string hostName = znetPeer.m_socket.GetHostName();
					if (!this.m_permittedList.Contains(hostName))
					{
						ZLog.Log("Kicking player not in permitted list " + znetPeer.m_playerName + " host: " + hostName);
						this.InternalKick(znetPeer);
						flag = false;
						break;
					}
				}
			}
		}
	}

	public bool IsSaving()
	{
		return this.m_saveThread != null;
	}

	private void UpdateStats(float dt)
	{
		this.m_statTimer += dt;
		if (this.m_statTimer >= 1f)
		{
			this.m_statTimer = 0f;
			this.m_totalRecv = 0;
			this.m_totalSent = 0;
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.m_socket != null)
				{
					int num;
					int num2;
					znetPeer.m_socket.GetAndResetStats(out num, out num2);
					this.m_totalRecv += num2;
					this.m_totalSent += num;
				}
			}
		}
	}

	public void Save()
	{
		if (ZoneSystem.instance.SkipSaving() || DungeonDB.instance.SkipSaving())
		{
			ZLog.LogWarning("Skipping world save");
			return;
		}
		if (ZNet.m_isServer && ZNet.m_world != null)
		{
			this.SaveWorldAsync();
		}
	}

	private void SendPeriodicData(float dt)
	{
		this.m_periodicSendTimer += dt;
		if (this.m_periodicSendTimer >= 2f)
		{
			this.m_periodicSendTimer = 0f;
			if (this.IsServer())
			{
				this.SendNetTime();
				this.SendPlayerList();
				return;
			}
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					znetPeer.m_rpc.Invoke("RefPos", new object[]
					{
						this.m_referencePosition,
						this.m_publicReferencePosition
					});
				}
			}
		}
	}

	private void SendNetTime()
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady())
			{
				znetPeer.m_rpc.Invoke("NetTime", new object[]
				{
					this.m_netTime
				});
			}
		}
	}

	private void RPC_NetTime(ZRpc rpc, double time)
	{
		this.m_netTime = time;
	}

	private void RPC_RefPos(ZRpc rpc, Vector3 pos, bool publicRefPos)
	{
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer != null)
		{
			peer.m_refPos = pos;
			peer.m_publicRefPos = publicRefPos;
		}
	}

	private void UpdatePeers(float dt)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (!znetPeer.m_rpc.IsConnected())
			{
				if (znetPeer.m_server)
				{
					ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorDisconnected;
				}
				this.Disconnect(znetPeer);
				break;
			}
		}
		ZNetPeer[] array = this.m_peers.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].m_rpc.Update(dt);
		}
	}

	private void CheckForIncommingServerConnections()
	{
		if (this.m_hostSocket == null)
		{
			return;
		}
		ISocket socket = this.m_hostSocket.Accept();
		if (socket != null)
		{
			if (!socket.IsConnected())
			{
				socket.Dispose();
				return;
			}
			ZNetPeer peer = new ZNetPeer(socket, false);
			this.OnNewConnection(peer);
		}
	}

	public ZNetPeer GetPeerByPlayerName(string name)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady() && znetPeer.m_playerName == name)
			{
				return znetPeer;
			}
		}
		return null;
	}

	public ZNetPeer GetPeerByHostName(string endpoint)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady() && znetPeer.m_socket.GetHostName() == endpoint)
			{
				return znetPeer;
			}
		}
		return null;
	}

	public ZNetPeer GetPeer(long uid)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.m_uid == uid)
			{
				return znetPeer;
			}
		}
		return null;
	}

	private ZNetPeer GetPeer(ZRpc rpc)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.m_rpc == rpc)
			{
				return znetPeer;
			}
		}
		return null;
	}

	public List<ZNetPeer> GetConnectedPeers()
	{
		return new List<ZNetPeer>(this.m_peers);
	}

	private void SaveWorldAsync()
	{
		if (this.m_saveThread != null && this.m_saveThread.IsAlive)
		{
			this.m_saveThread.Join();
			this.m_saveThread = null;
		}
		this.m_saveStartTime = Time.realtimeSinceStartup;
		this.m_zdoMan.PrepareSave();
		ZoneSystem.instance.PrepareSave();
		RandEventSystem.instance.PrepareSave();
		this.m_saveThreadStartTime = Time.realtimeSinceStartup;
		this.m_saveThread = new Thread(new ThreadStart(this.SaveWorldThread));
		this.m_saveThread.Start();
	}

	private void UpdateSave()
	{
		if (this.m_saveThread != null && !this.m_saveThread.IsAlive)
		{
			this.m_saveThread = null;
			float num = this.m_saveThreadStartTime - this.m_saveStartTime;
			float num2 = Time.realtimeSinceStartup - this.m_saveThreadStartTime;
			MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, string.Concat(new string[]
			{
				"$msg_worldsaved ( ",
				num.ToString("0.00"),
				"+",
				num2.ToString("0.00"),
				"s )"
			}), 0, null);
		}
	}

	private void SaveWorldThread()
	{
		DateTime now = DateTime.Now;
		string dbpath = ZNet.m_world.GetDBPath();
		string text = dbpath + ".backup";
		if (File.Exists(dbpath))
		{
			if (File.Exists(text))
			{
				File.Delete(text);
			}
			File.Move(dbpath, text);
		}
		FileStream fileStream = File.Create(dbpath);
		BinaryWriter binaryWriter = new BinaryWriter(fileStream);
		binaryWriter.Write(global::Version.m_worldVersion);
		binaryWriter.Write(this.m_netTime);
		this.m_zdoMan.SaveAsync(binaryWriter);
		ZoneSystem.instance.SaveASync(binaryWriter);
		RandEventSystem.instance.SaveAsync(binaryWriter);
		binaryWriter.Close();
		fileStream.Dispose();
		ZNet.m_world.SaveWorldMetaData();
		if (File.Exists(text))
		{
			File.Delete(text);
		}
		ZLog.Log("World saved ( " + (DateTime.Now - now).TotalMilliseconds.ToString() + "ms )");
	}

	private bool LoadWorld()
	{
		ZLog.Log("Load world " + ZNet.m_world.m_name);
		string dbpath = ZNet.m_world.GetDBPath();
		FileStream fileStream;
		try
		{
			fileStream = File.OpenRead(dbpath);
		}
		catch
		{
			ZLog.Log("  missing world.dat");
			return false;
		}
		BinaryReader binaryReader = new BinaryReader(fileStream);
		int num;
		if (!this.CheckDataVersion(binaryReader, out num))
		{
			ZLog.Log("  incompatible data version " + num);
			binaryReader.Close();
			fileStream.Dispose();
			return false;
		}
		if (num >= 4)
		{
			this.m_netTime = binaryReader.ReadDouble();
		}
		this.m_zdoMan.Load(binaryReader, num);
		if (num >= 12)
		{
			ZoneSystem.instance.Load(binaryReader, num);
		}
		if (num >= 15)
		{
			RandEventSystem.instance.Load(binaryReader, num);
		}
		binaryReader.Close();
		fileStream.Dispose();
		GC.Collect();
		return true;
	}

	private bool CheckDataVersion(BinaryReader reader, out int version)
	{
		try
		{
			version = reader.ReadInt32();
			if (!global::Version.IsWorldVersionCompatible(version))
			{
				return false;
			}
		}
		catch
		{
			version = 0;
			return false;
		}
		return true;
	}

	public int GetHostPort()
	{
		if (this.m_hostSocket != null)
		{
			return this.m_hostSocket.GetHostPort();
		}
		return 0;
	}

	public long GetUID()
	{
		return this.m_zdoMan.GetMyID();
	}

	public long GetWorldUID()
	{
		return ZNet.m_world.m_uid;
	}

	public string GetWorldName()
	{
		if (ZNet.m_world != null)
		{
			return ZNet.m_world.m_name;
		}
		return null;
	}

	public void SetCharacterID(ZDOID id)
	{
		this.m_characterID = id;
		if (!ZNet.m_isServer)
		{
			this.m_peers[0].m_rpc.Invoke("CharacterID", new object[]
			{
				id
			});
		}
	}

	private void RPC_CharacterID(ZRpc rpc, ZDOID characterID)
	{
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer != null)
		{
			peer.m_characterID = characterID;
			ZLog.Log(string.Concat(new object[]
			{
				"Got character ZDOID from ",
				peer.m_playerName,
				" : ",
				characterID
			}));
		}
	}

	public void SetPublicReferencePosition(bool pub)
	{
		this.m_publicReferencePosition = pub;
	}

	public bool IsReferencePositionPublic()
	{
		return this.m_publicReferencePosition;
	}

	public void SetReferencePosition(Vector3 pos)
	{
		this.m_referencePosition = pos;
	}

	public Vector3 GetReferencePosition()
	{
		return this.m_referencePosition;
	}

	public List<ZDO> GetAllCharacterZDOS()
	{
		List<ZDO> list = new List<ZDO>();
		ZDO zdo = this.m_zdoMan.GetZDO(this.m_characterID);
		if (zdo != null)
		{
			list.Add(zdo);
		}
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady() && !znetPeer.m_characterID.IsNone())
			{
				ZDO zdo2 = this.m_zdoMan.GetZDO(znetPeer.m_characterID);
				if (zdo2 != null)
				{
					list.Add(zdo2);
				}
			}
		}
		return list;
	}

	public int GetPeerConnections()
	{
		int num = 0;
		for (int i = 0; i < this.m_peers.Count; i++)
		{
			if (this.m_peers[i].IsReady())
			{
				num++;
			}
		}
		return num;
	}

	public ZNat GetZNat()
	{
		return this.m_nat;
	}

	public static void SetServer(bool server, bool openServer, bool publicServer, string serverName, string password, World world)
	{
		ZNet.m_isServer = server;
		ZNet.m_openServer = openServer;
		ZNet.m_publicServer = publicServer;
		ZNet.m_serverPassword = (string.IsNullOrEmpty(password) ? "" : ZNet.HashPassword(password));
		ZNet.m_ServerName = serverName;
		ZNet.m_world = world;
	}

	private static string HashPassword(string password)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(password);
		byte[] bytes2 = new MD5CryptoServiceProvider().ComputeHash(bytes);
		return Encoding.ASCII.GetString(bytes2);
	}

	public static void SetServerHost(ulong serverID)
	{
		ZNet.m_serverHost = "";
		ZNet.m_serverHostPort = 0;
		ZNet.m_serverSteamID = serverID;
	}

	public static void SetServerHost(string host, int port)
	{
		ZNet.m_serverHost = host;
		ZNet.m_serverHostPort = port;
		ZNet.m_serverSteamID = 0UL;
	}

	public static string GetServerString()
	{
		return ZNet.m_serverHost + ":" + ZNet.m_serverHostPort;
	}

	public bool IsServer()
	{
		return ZNet.m_isServer;
	}

	public bool IsDedicated()
	{
		return true;
	}

	private void UpdatePlayerList()
	{
		this.m_players.Clear();
		if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
		{
			ZNet.PlayerInfo playerInfo = new ZNet.PlayerInfo
			{
				m_name = Game.instance.GetPlayerProfile().GetName(),
				m_host = "",
				m_characterID = this.m_characterID,
				m_publicPosition = this.m_publicReferencePosition
			};
			if (playerInfo.m_publicPosition)
			{
				playerInfo.m_position = this.m_referencePosition;
			}
			this.m_players.Add(playerInfo);
		}
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady())
			{
				ZNet.PlayerInfo playerInfo2 = new ZNet.PlayerInfo
				{
					m_characterID = znetPeer.m_characterID,
					m_name = znetPeer.m_playerName,
					m_host = znetPeer.m_socket.GetHostName(),
					m_publicPosition = znetPeer.m_publicRefPos
				};
				if (playerInfo2.m_publicPosition)
				{
					playerInfo2.m_position = znetPeer.m_refPos;
				}
				this.m_players.Add(playerInfo2);
			}
		}
	}

	private void SendPlayerList()
	{
		this.UpdatePlayerList();
		if (this.m_peers.Count > 0)
		{
			ZPackage zpackage = new ZPackage();
			zpackage.Write(this.m_players.Count);
			foreach (ZNet.PlayerInfo playerInfo in this.m_players)
			{
				zpackage.Write(playerInfo.m_name);
				zpackage.Write(playerInfo.m_host);
				zpackage.Write(playerInfo.m_characterID);
				zpackage.Write(playerInfo.m_publicPosition);
				if (playerInfo.m_publicPosition)
				{
					zpackage.Write(playerInfo.m_position);
				}
			}
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					znetPeer.m_rpc.Invoke("PlayerList", new object[]
					{
						zpackage
					});
				}
			}
		}
	}

	private void RPC_PlayerList(ZRpc rpc, ZPackage pkg)
	{
		this.m_players.Clear();
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			ZNet.PlayerInfo playerInfo = new ZNet.PlayerInfo
			{
				m_name = pkg.ReadString(),
				m_host = pkg.ReadString(),
				m_characterID = pkg.ReadZDOID(),
				m_publicPosition = pkg.ReadBool()
			};
			if (playerInfo.m_publicPosition)
			{
				playerInfo.m_position = pkg.ReadVector3();
			}
			this.m_players.Add(playerInfo);
		}
	}

	public List<ZNet.PlayerInfo> GetPlayerList()
	{
		return this.m_players;
	}

	public void GetOtherPublicPlayers(List<ZNet.PlayerInfo> playerList)
	{
		foreach (ZNet.PlayerInfo playerInfo in this.m_players)
		{
			if (playerInfo.m_publicPosition)
			{
				ZDOID characterID = playerInfo.m_characterID;
				if (!characterID.IsNone() && !(playerInfo.m_characterID == this.m_characterID))
				{
					playerList.Add(playerInfo);
				}
			}
		}
	}

	public int GetNrOfPlayers()
	{
		return this.m_players.Count;
	}

	public void GetNetStats(out int totalSent, out int totalRecv)
	{
		totalSent = this.m_totalSent;
		totalRecv = this.m_totalRecv;
	}

	public void SetNetTime(double time)
	{
		this.m_netTime = time;
	}

	public DateTime GetTime()
	{
		long ticks = (long)(this.m_netTime * 1000.0 * 10000.0);
		return new DateTime(ticks);
	}

	public float GetWrappedDayTimeSeconds()
	{
		return (float)(this.m_netTime % 86400.0);
	}

	public double GetTimeSeconds()
	{
		return this.m_netTime;
	}

	public static ZNet.ConnectionStatus GetConnectionStatus()
	{
		if (ZNet.m_instance != null && ZNet.m_instance.IsServer())
		{
			return ZNet.ConnectionStatus.Connected;
		}
		return ZNet.m_connectionStatus;
	}

	public bool HasBadConnection()
	{
		return this.GetServerPing() > this.m_badConnectionPing;
	}

	public float GetServerPing()
	{
		if (this.IsServer())
		{
			return 0f;
		}
		if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connecting || ZNet.m_connectionStatus == ZNet.ConnectionStatus.None)
		{
			return 0f;
		}
		if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
		{
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					return znetPeer.m_rpc.GetTimeSinceLastData();
				}
			}
		}
		return 0f;
	}

	public ZNetPeer GetServerPeer()
	{
		if (this.IsServer())
		{
			return null;
		}
		if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connecting || ZNet.m_connectionStatus == ZNet.ConnectionStatus.None)
		{
			return null;
		}
		if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
		{
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					return znetPeer;
				}
			}
		}
		return null;
	}

	public ZRpc GetServerRPC()
	{
		ZNetPeer serverPeer = this.GetServerPeer();
		if (serverPeer != null)
		{
			return serverPeer.m_rpc;
		}
		return null;
	}

	public List<ZNetPeer> GetPeers()
	{
		return this.m_peers;
	}

	public void RemotePrint(ZRpc rpc, string text)
	{
		if (rpc == null)
		{
			if (global::Console.instance)
			{
				global::Console.instance.Print(text);
				return;
			}
		}
		else
		{
			rpc.Invoke("RemotePrint", new object[]
			{
				text
			});
		}
	}

	private void RPC_RemotePrint(ZRpc rpc, string text)
	{
		if (global::Console.instance)
		{
			global::Console.instance.Print(text);
		}
	}

	public void Kick(string user)
	{
		if (this.IsServer())
		{
			this.InternalKick(user);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("Kick", new object[]
			{
				user
			});
		}
	}

	private void RPC_Kick(ZRpc rpc, string user)
	{
		if (!this.m_adminList.Contains(rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.RemotePrint(rpc, "Kicking user " + user);
		this.InternalKick(user);
	}

	private void InternalKick(string user)
	{
		if (user == "")
		{
			return;
		}
		ZNetPeer znetPeer = this.GetPeerByHostName(user);
		if (znetPeer == null)
		{
			znetPeer = this.GetPeerByPlayerName(user);
		}
		if (znetPeer != null)
		{
			this.InternalKick(znetPeer);
		}
	}

	private void InternalKick(ZNetPeer peer)
	{
		if (!this.IsServer())
		{
			return;
		}
		if (peer != null)
		{
			ZLog.Log("Kicking " + peer.m_playerName);
			this.SendDisconnect(peer);
			this.Disconnect(peer);
		}
	}

	public bool IsAllowed(string hostName, string playerName)
	{
		return !this.m_bannedList.Contains(hostName) && !this.m_bannedList.Contains(playerName) && (this.m_permittedList.Count() <= 0 || this.m_permittedList.Contains(hostName));
	}

	public void Ban(string user)
	{
		if (this.IsServer())
		{
			this.InternalBan(null, user);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("Ban", new object[]
			{
				user
			});
		}
	}

	private void RPC_Ban(ZRpc rpc, string user)
	{
		if (!this.m_adminList.Contains(rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.InternalBan(rpc, user);
	}

	private void InternalBan(ZRpc rpc, string user)
	{
		if (!this.IsServer())
		{
			return;
		}
		if (user == "")
		{
			return;
		}
		ZNetPeer peerByPlayerName = this.GetPeerByPlayerName(user);
		if (peerByPlayerName != null)
		{
			user = peerByPlayerName.m_socket.GetHostName();
		}
		this.RemotePrint(rpc, "Banning user " + user);
		this.m_bannedList.Add(user);
	}

	public void Unban(string user)
	{
		if (this.IsServer())
		{
			this.InternalUnban(null, user);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("Unban", new object[]
			{
				user
			});
		}
	}

	private void RPC_Unban(ZRpc rpc, string user)
	{
		if (!this.m_adminList.Contains(rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.InternalUnban(rpc, user);
	}

	private void InternalUnban(ZRpc rpc, string user)
	{
		if (!this.IsServer())
		{
			return;
		}
		if (user == "")
		{
			return;
		}
		this.RemotePrint(rpc, "Unbanning user " + user);
		this.m_bannedList.Remove(user);
	}

	public void PrintBanned()
	{
		if (this.IsServer())
		{
			this.InternalPrintBanned(null);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("PrintBanned", Array.Empty<object>());
		}
	}

	private void RPC_PrintBanned(ZRpc rpc)
	{
		if (!this.m_adminList.Contains(rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.InternalPrintBanned(rpc);
	}

	private void InternalPrintBanned(ZRpc rpc)
	{
		this.RemotePrint(rpc, "Banned users");
		List<string> list = this.m_bannedList.GetList();
		if (list.Count == 0)
		{
			this.RemotePrint(rpc, "-");
		}
		else
		{
			for (int i = 0; i < list.Count; i++)
			{
				this.RemotePrint(rpc, i.ToString() + ": " + list[i]);
			}
		}
		this.RemotePrint(rpc, "");
		this.RemotePrint(rpc, "Permitted users");
		List<string> list2 = this.m_permittedList.GetList();
		if (list2.Count == 0)
		{
			this.RemotePrint(rpc, "All");
			return;
		}
		for (int j = 0; j < list2.Count; j++)
		{
			this.RemotePrint(rpc, j.ToString() + ": " + list2[j]);
		}
	}

	private float m_banlistTimer;

	private static ZNet m_instance;

	public int m_hostPort = 2456;

	public RectTransform m_passwordDialog;

	public float m_badConnectionPing = 5f;

	public int m_zdoSectorsWidth = 512;

	public int m_serverPlayerLimit = 10;

	private ZConnector2 m_serverConnector;

	private ISocket m_hostSocket;

	private List<ZNetPeer> m_peers = new List<ZNetPeer>();

	private Thread m_saveThread;

	private float m_saveStartTime;

	private float m_saveThreadStartTime;

	private ZDOMan m_zdoMan;

	private ZRoutedRpc m_routedRpc;

	private ZNat m_nat;

	private double m_netTime = 2040.0;

	private ZDOID m_characterID = ZDOID.None;

	private Vector3 m_referencePosition = Vector3.zero;

	private bool m_publicReferencePosition;

	private float m_periodicSendTimer;

	private float m_statTimer;

	private int m_totalSent;

	private int m_totalRecv;

	private bool m_haveStoped;

	private static bool m_isServer = true;

	private static World m_world = null;

	private static string m_serverHost = "";

	private static int m_serverHostPort = 0;

	private static ulong m_serverSteamID = 0UL;

	private static bool m_openServer = true;

	private static bool m_publicServer = true;

	private static string m_serverPassword = "";

	private static string m_ServerName = "";

	private static ZNet.ConnectionStatus m_connectionStatus = ZNet.ConnectionStatus.None;

	private SyncedList m_adminList;

	private SyncedList m_bannedList;

	private SyncedList m_permittedList;

	private List<ZNet.PlayerInfo> m_players = new List<ZNet.PlayerInfo>();

	private ZRpc m_tempPasswordRPC;

	public enum ConnectionStatus
	{
		None,
		Connecting,
		Connected,
		ErrorVersion,
		ErrorDisconnected,
		ErrorConnectFailed,
		ErrorPassword,
		ErrorAlreadyConnected,
		ErrorBanned,
		ErrorFull
	}

	public struct PlayerInfo
	{
		public string m_name;

		public string m_host;

		public ZDOID m_characterID;

		public bool m_publicPosition;

		public Vector3 m_position;
	}
}
