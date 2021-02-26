using System;
using System.Collections.Generic;
using System.Threading;
using Steamworks;

public class ZSteamSocketOLD : IDisposable, ISocket
{
	public ZSteamSocketOLD()
	{
		ZSteamSocketOLD.m_sockets.Add(this);
		ZSteamSocketOLD.RegisterGlobalCallbacks();
	}

	public ZSteamSocketOLD(CSteamID peerID)
	{
		ZSteamSocketOLD.m_sockets.Add(this);
		this.m_peerID = peerID;
		ZSteamSocketOLD.RegisterGlobalCallbacks();
	}

	private static void RegisterGlobalCallbacks()
	{
		if (ZSteamSocketOLD.m_connectionFailed == null)
		{
			ZLog.Log("ZSteamSocketOLD  Registering global callbacks");
			ZSteamSocketOLD.m_connectionFailed = Callback<P2PSessionConnectFail_t>.CreateGameServer(new Callback<P2PSessionConnectFail_t>.DispatchDelegate(ZSteamSocketOLD.OnConnectionFailed));
		}
		if (ZSteamSocketOLD.m_SessionRequest == null)
		{
			ZSteamSocketOLD.m_SessionRequest = Callback<P2PSessionRequest_t>.CreateGameServer(new Callback<P2PSessionRequest_t>.DispatchDelegate(ZSteamSocketOLD.OnSessionRequest));
		}
	}

	private static void UnregisterGlobalCallbacks()
	{
		ZLog.Log("ZSteamSocket  UnregisterGlobalCallbacks, existing sockets:" + ZSteamSocketOLD.m_sockets.Count);
		if (ZSteamSocketOLD.m_connectionFailed != null)
		{
			ZSteamSocketOLD.m_connectionFailed.Dispose();
			ZSteamSocketOLD.m_connectionFailed = null;
		}
		if (ZSteamSocketOLD.m_SessionRequest != null)
		{
			ZSteamSocketOLD.m_SessionRequest.Dispose();
			ZSteamSocketOLD.m_SessionRequest = null;
		}
	}

	private static void OnConnectionFailed(P2PSessionConnectFail_t data)
	{
		ZLog.Log("Got connection failed callback: " + data.m_steamIDRemote);
		foreach (ZSteamSocketOLD zsteamSocketOLD in ZSteamSocketOLD.m_sockets)
		{
			if (zsteamSocketOLD.IsPeer(data.m_steamIDRemote))
			{
				zsteamSocketOLD.Close();
			}
		}
	}

	private static void OnSessionRequest(P2PSessionRequest_t data)
	{
		ZLog.Log("Got session request from " + data.m_steamIDRemote);
		if (SteamGameServerNetworking.AcceptP2PSessionWithUser(data.m_steamIDRemote))
		{
			ZSteamSocketOLD listner = ZSteamSocketOLD.GetListner();
			if (listner != null)
			{
				listner.QueuePendingConnection(data.m_steamIDRemote);
			}
		}
	}

	public void Dispose()
	{
		ZLog.Log("Disposing socket");
		this.Close();
		this.m_pkgQueue.Clear();
		ZSteamSocketOLD.m_sockets.Remove(this);
		if (ZSteamSocketOLD.m_sockets.Count == 0)
		{
			ZLog.Log("Last socket, unregistering callback");
			ZSteamSocketOLD.UnregisterGlobalCallbacks();
		}
	}

	public void Close()
	{
		ZLog.Log("Closing socket " + this.GetEndPointString());
		if (this.m_peerID != CSteamID.Nil)
		{
			this.Flush();
			ZLog.Log("  send queue size:" + this.m_sendQueue.Count);
			Thread.Sleep(100);
			P2PSessionState_t p2PSessionState_t;
			SteamGameServerNetworking.GetP2PSessionState(this.m_peerID, out p2PSessionState_t);
			ZLog.Log("  P2P state, bytes in send queue:" + p2PSessionState_t.m_nBytesQueuedForSend);
			SteamGameServerNetworking.CloseP2PSessionWithUser(this.m_peerID);
			SteamGameServer.EndAuthSession(this.m_peerID);
			this.m_peerID = CSteamID.Nil;
		}
		this.m_listner = false;
	}

	public bool StartHost()
	{
		this.m_listner = true;
		this.m_pendingConnections.Clear();
		return true;
	}

	private ZSteamSocketOLD QueuePendingConnection(CSteamID id)
	{
		foreach (ZSteamSocketOLD zsteamSocketOLD in this.m_pendingConnections)
		{
			if (zsteamSocketOLD.IsPeer(id))
			{
				return zsteamSocketOLD;
			}
		}
		ZSteamSocketOLD zsteamSocketOLD2 = new ZSteamSocketOLD(id);
		this.m_pendingConnections.Enqueue(zsteamSocketOLD2);
		return zsteamSocketOLD2;
	}

	public ISocket Accept()
	{
		if (!this.m_listner)
		{
			return null;
		}
		if (this.m_pendingConnections.Count > 0)
		{
			return this.m_pendingConnections.Dequeue();
		}
		return null;
	}

	public bool IsConnected()
	{
		return this.m_peerID != CSteamID.Nil;
	}

	public void Send(ZPackage pkg)
	{
		if (pkg.Size() == 0)
		{
			return;
		}
		if (!this.IsConnected())
		{
			return;
		}
		byte[] array = pkg.GetArray();
		byte[] bytes = BitConverter.GetBytes(array.Length);
		byte[] array2 = new byte[array.Length + bytes.Length];
		bytes.CopyTo(array2, 0);
		array.CopyTo(array2, 4);
		this.m_sendQueue.Enqueue(array);
		this.SendQueuedPackages();
	}

	public bool Flush()
	{
		this.SendQueuedPackages();
		return this.m_sendQueue.Count == 0;
	}

	private void SendQueuedPackages()
	{
		if (!this.IsConnected())
		{
			return;
		}
		while (this.m_sendQueue.Count > 0)
		{
			byte[] array = this.m_sendQueue.Peek();
			EP2PSend eP2PSendType = EP2PSend.k_EP2PSendReliable;
			if (!SteamGameServerNetworking.SendP2PPacket(this.m_peerID, array, (uint)array.Length, eP2PSendType, 0))
			{
				break;
			}
			this.m_totalSent += array.Length;
			this.m_sendQueue.Dequeue();
		}
	}

	public static void Update()
	{
		foreach (ZSteamSocketOLD zsteamSocketOLD in ZSteamSocketOLD.m_sockets)
		{
			zsteamSocketOLD.SendQueuedPackages();
		}
		ZSteamSocketOLD.ReceivePackages();
	}

	private static void ReceivePackages()
	{
		uint num;
		while (SteamGameServerNetworking.IsP2PPacketAvailable(out num, 0))
		{
			byte[] array = new byte[num];
			uint num2;
			CSteamID sender;
			if (!SteamGameServerNetworking.ReadP2PPacket(array, num, out num2, out sender, 0))
			{
				break;
			}
			ZSteamSocketOLD.QueueNewPkg(sender, array);
		}
	}

	private static void QueueNewPkg(CSteamID sender, byte[] data)
	{
		foreach (ZSteamSocketOLD zsteamSocketOLD in ZSteamSocketOLD.m_sockets)
		{
			if (zsteamSocketOLD.IsPeer(sender))
			{
				zsteamSocketOLD.QueuePackage(data);
				return;
			}
		}
		ZSteamSocketOLD listner = ZSteamSocketOLD.GetListner();
		if (listner != null)
		{
			ZLog.Log("Got package from unconnected peer " + sender);
			listner.QueuePendingConnection(sender).QueuePackage(data);
			return;
		}
		ZLog.Log("Got package from unkown peer " + sender + " but no active listner");
	}

	private static ZSteamSocketOLD GetListner()
	{
		foreach (ZSteamSocketOLD zsteamSocketOLD in ZSteamSocketOLD.m_sockets)
		{
			if (zsteamSocketOLD.IsHost())
			{
				return zsteamSocketOLD;
			}
		}
		return null;
	}

	private void QueuePackage(byte[] data)
	{
		ZPackage item = new ZPackage(data);
		this.m_pkgQueue.Enqueue(item);
		this.m_gotData = true;
		this.m_totalRecv += data.Length;
	}

	public ZPackage Recv()
	{
		if (!this.IsConnected())
		{
			return null;
		}
		if (this.m_pkgQueue.Count > 0)
		{
			return this.m_pkgQueue.Dequeue();
		}
		return null;
	}

	public string GetEndPointString()
	{
		return this.m_peerID.ToString();
	}

	public string GetHostName()
	{
		return this.m_peerID.ToString();
	}

	public CSteamID GetPeerID()
	{
		return this.m_peerID;
	}

	public bool IsPeer(CSteamID peer)
	{
		return this.IsConnected() && peer == this.m_peerID;
	}

	public bool IsHost()
	{
		return this.m_listner;
	}

	public bool IsSending()
	{
		return this.IsConnected() && this.m_sendQueue.Count > 0;
	}

	public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		localQuality = 0f;
		remoteQuality = 0f;
		ping = 0;
		outByteSec = 0f;
		inByteSec = 0f;
	}

	public void GetAndResetStats(out int totalSent, out int totalRecv)
	{
		totalSent = this.m_totalSent;
		totalRecv = this.m_totalRecv;
		this.m_totalSent = 0;
		this.m_totalRecv = 0;
	}

	public bool GotNewData()
	{
		bool gotData = this.m_gotData;
		this.m_gotData = false;
		return gotData;
	}

	public int GetHostPort()
	{
		if (this.IsHost())
		{
			return 1;
		}
		return -1;
	}

	private static List<ZSteamSocketOLD> m_sockets = new List<ZSteamSocketOLD>();

	private static Callback<P2PSessionRequest_t> m_SessionRequest;

	private static Callback<P2PSessionConnectFail_t> m_connectionFailed;

	private Queue<ZSteamSocketOLD> m_pendingConnections = new Queue<ZSteamSocketOLD>();

	private CSteamID m_peerID = CSteamID.Nil;

	private bool m_listner;

	private Queue<ZPackage> m_pkgQueue = new Queue<ZPackage>();

	private Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	private int m_totalSent;

	private int m_totalRecv;

	private bool m_gotData;
}
