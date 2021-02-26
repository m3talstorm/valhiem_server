using System;
using System.Collections.Generic;
using UnityEngine;

public class MasterClient
{
	public static MasterClient instance
	{
		get
		{
			return MasterClient.m_instance;
		}
	}

	public static void Initialize()
	{
		if (MasterClient.m_instance == null)
		{
			MasterClient.m_instance = new MasterClient();
		}
	}

	public MasterClient()
	{
		this.m_sessionUID = Utils.GenerateUID();
	}

	public void Dispose()
	{
		if (this.m_socket != null)
		{
			this.m_socket.Dispose();
		}
		if (this.m_connector != null)
		{
			this.m_connector.Dispose();
		}
		if (this.m_rpc != null)
		{
			this.m_rpc.Dispose();
		}
		if (MasterClient.m_instance == this)
		{
			MasterClient.m_instance = null;
		}
	}

	public void Update(float dt)
	{
		if (this.m_rpc == null)
		{
			if (this.m_connector == null)
			{
				this.m_connector = new ZConnector2(this.m_msHost, this.m_msPort);
				return;
			}
			if (this.m_connector.UpdateStatus(dt, false))
			{
				this.m_socket = this.m_connector.Complete();
				if (this.m_socket != null)
				{
					this.m_rpc = new ZRpc(this.m_socket);
					this.m_rpc.Register<ZPackage>("ServerList", new Action<ZRpc, ZPackage>(this.RPC_ServerList));
					if (this.m_registerPkg != null)
					{
						this.m_rpc.Invoke("RegisterServer2", new object[]
						{
							this.m_registerPkg
						});
					}
				}
				this.m_connector.Dispose();
				this.m_connector = null;
			}
		}
		ZRpc rpc = this.m_rpc;
		if (rpc != null)
		{
			rpc.Update(dt);
			if (!rpc.IsConnected())
			{
				this.m_rpc.Dispose();
				this.m_rpc = null;
			}
		}
		if (this.m_rpc != null)
		{
			this.m_sendStatsTimer += dt;
			if (this.m_sendStatsTimer > 60f)
			{
				this.m_sendStatsTimer = 0f;
				this.SendStats(60f);
			}
		}
	}

	private void SendStats(float duration)
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write(2);
		zpackage.Write(this.m_sessionUID);
		zpackage.Write(Time.time);
		bool flag = Player.m_localPlayer != null;
		zpackage.Write(flag ? duration : 0f);
		bool flag2 = ZNet.instance && !ZNet.instance.IsServer();
		zpackage.Write(flag2 ? duration : 0f);
		zpackage.Write(global::Version.GetVersionString());
		bool flag3 = ZNet.instance && ZNet.instance.IsServer();
		zpackage.Write(flag3);
		if (flag3)
		{
			zpackage.Write(ZNet.instance.GetWorldUID());
			zpackage.Write(duration);
			int num = ZNet.instance.GetPeerConnections();
			if (Player.m_localPlayer != null)
			{
				num++;
			}
			zpackage.Write(num);
			bool data = ZNet.instance.GetZNat() != null && ZNet.instance.GetZNat().GetStatus();
			zpackage.Write(data);
		}
		PlayerProfile playerProfile = (Game.instance != null) ? Game.instance.GetPlayerProfile() : null;
		if (playerProfile != null)
		{
			zpackage.Write(true);
			zpackage.Write(playerProfile.GetPlayerID());
			zpackage.Write(playerProfile.m_playerStats.m_kills);
			zpackage.Write(playerProfile.m_playerStats.m_deaths);
			zpackage.Write(playerProfile.m_playerStats.m_crafts);
			zpackage.Write(playerProfile.m_playerStats.m_builds);
		}
		else
		{
			zpackage.Write(false);
		}
		this.m_rpc.Invoke("Stats", new object[]
		{
			zpackage
		});
	}

	public void RegisterServer(string name, string host, int port, bool password, bool upnp, long worldUID, string version)
	{
		this.m_registerPkg = new ZPackage();
		this.m_registerPkg.Write(1);
		this.m_registerPkg.Write(name);
		this.m_registerPkg.Write(host);
		this.m_registerPkg.Write(port);
		this.m_registerPkg.Write(password);
		this.m_registerPkg.Write(upnp);
		this.m_registerPkg.Write(worldUID);
		this.m_registerPkg.Write(version);
		if (this.m_rpc != null)
		{
			this.m_rpc.Invoke("RegisterServer2", new object[]
			{
				this.m_registerPkg
			});
		}
		ZLog.Log(string.Concat(new object[]
		{
			"Registering server ",
			name,
			"  ",
			host,
			":",
			port
		}));
	}

	public void UnregisterServer()
	{
		if (this.m_registerPkg == null)
		{
			return;
		}
		if (this.m_rpc != null)
		{
			this.m_rpc.Invoke("UnregisterServer", Array.Empty<object>());
		}
		this.m_registerPkg = null;
	}

	public List<MasterClient.ServerData> GetServers()
	{
		return this.m_servers;
	}

	public bool GetServers(List<MasterClient.ServerData> servers)
	{
		if (!this.m_haveServerlist)
		{
			return false;
		}
		servers.Clear();
		servers.AddRange(this.m_servers);
		return true;
	}

	public void RequestServerlist()
	{
		if (this.m_rpc != null)
		{
			this.m_rpc.Invoke("RequestServerlist2", Array.Empty<object>());
		}
	}

	private void RPC_ServerList(ZRpc rpc, ZPackage pkg)
	{
		this.m_haveServerlist = true;
		this.m_serverListRevision++;
		pkg.ReadInt();
		int num = pkg.ReadInt();
		this.m_servers.Clear();
		for (int i = 0; i < num; i++)
		{
			MasterClient.ServerData serverData = new MasterClient.ServerData();
			serverData.m_name = pkg.ReadString();
			serverData.m_host = pkg.ReadString();
			serverData.m_port = pkg.ReadInt();
			serverData.m_password = pkg.ReadBool();
			serverData.m_upnp = pkg.ReadBool();
			pkg.ReadLong();
			serverData.m_version = pkg.ReadString();
			serverData.m_players = pkg.ReadInt();
			if (this.m_nameFilter.Length <= 0 || serverData.m_name.Contains(this.m_nameFilter))
			{
				this.m_servers.Add(serverData);
			}
		}
		if (this.m_onServerList != null)
		{
			this.m_onServerList(this.m_servers);
		}
	}

	public int GetServerListRevision()
	{
		return this.m_serverListRevision;
	}

	public bool IsConnected()
	{
		return this.m_rpc != null;
	}

	public void SetNameFilter(string filter)
	{
		this.m_nameFilter = filter;
		ZLog.Log("filter is " + filter);
	}

	private const int statVersion = 2;

	public Action<List<MasterClient.ServerData>> m_onServerList;

	private string m_msHost = "dvoid.noip.me";

	private int m_msPort = 9983;

	private long m_sessionUID;

	private ZConnector2 m_connector;

	private ZSocket2 m_socket;

	private ZRpc m_rpc;

	private bool m_haveServerlist;

	private List<MasterClient.ServerData> m_servers = new List<MasterClient.ServerData>();

	private ZPackage m_registerPkg;

	private float m_sendStatsTimer;

	private int m_serverListRevision;

	private string m_nameFilter = "";

	private static MasterClient m_instance;

	public class ServerData
	{
		public override bool Equals(object obj)
		{
			MasterClient.ServerData serverData = obj as MasterClient.ServerData;
			return serverData != null && (serverData.m_name == this.m_name && serverData.m_host == this.m_host && serverData.m_port == this.m_port) && serverData.m_steamHostID == this.m_steamHostID;
		}

		public string m_name;

		public string m_host;

		public int m_port;

		public bool m_password;

		public bool m_upnp;

		public string m_version;

		public int m_players;

		public ulong m_steamHostID;
	}
}
