using System;
using System.Collections.Generic;
using System.Threading;
using Steamworks;

public class ZSteamSocket : IDisposable, ISocket
{
	public ZSteamSocket()
	{
		ZSteamSocket.m_sockets.Add(this);
		ZSteamSocket.RegisterGlobalCallbacks();
	}

	public ZSteamSocket(CSteamID peerID)
	{
		ZSteamSocket.m_sockets.Add(this);
		this.m_peerID = peerID;
		ZSteamSocket.RegisterGlobalCallbacks();
	}

	private static void RegisterGlobalCallbacks()
	{
		if (ZSteamSocket.m_connectionFailed == null)
		{
			ZLog.Log("ZSteamSocket  Registering global callbacks");
			ZSteamSocket.m_connectionFailed = Callback<P2PSessionConnectFail_t>.CreateGameServer(new Callback<P2PSessionConnectFail_t>.DispatchDelegate(ZSteamSocket.OnConnectionFailed));
		}
		if (ZSteamSocket.m_SessionRequest == null)
		{
			ZSteamSocket.m_SessionRequest = Callback<P2PSessionRequest_t>.CreateGameServer(new Callback<P2PSessionRequest_t>.DispatchDelegate(ZSteamSocket.OnSessionRequest));
		}
	}

	private static void UnregisterGlobalCallbacks()
	{
		ZLog.Log("ZSteamSocket  UnregisterGlobalCallbacks, existing sockets:" + ZSteamSocket.m_sockets.Count);
		if (ZSteamSocket.m_connectionFailed != null)
		{
			ZSteamSocket.m_connectionFailed.Dispose();
			ZSteamSocket.m_connectionFailed = null;
		}
		if (ZSteamSocket.m_SessionRequest != null)
		{
			ZSteamSocket.m_SessionRequest.Dispose();
			ZSteamSocket.m_SessionRequest = null;
		}
	}

	private static void OnConnectionFailed(P2PSessionConnectFail_t data)
	{
		ZLog.Log("Got connection failed callback: " + data.m_steamIDRemote);
		foreach (ZSteamSocket zsteamSocket in ZSteamSocket.m_sockets)
		{
			if (zsteamSocket.IsPeer(data.m_steamIDRemote))
			{
				zsteamSocket.Close();
			}
		}
	}

	private static void OnSessionRequest(P2PSessionRequest_t data)
	{
		ZLog.Log("Got session request from " + data.m_steamIDRemote);
		if (SteamGameServerNetworking.AcceptP2PSessionWithUser(data.m_steamIDRemote))
		{
			ZSteamSocket listner = ZSteamSocket.GetListner();
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
		ZSteamSocket.m_sockets.Remove(this);
		if (ZSteamSocket.m_sockets.Count == 0)
		{
			ZLog.Log("Last socket, unregistering callback");
			ZSteamSocket.UnregisterGlobalCallbacks();
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

	private ZSteamSocket QueuePendingConnection(CSteamID id)
	{
		foreach (ZSteamSocket zsteamSocket in this.m_pendingConnections)
		{
			if (zsteamSocket.IsPeer(id))
			{
				return zsteamSocket;
			}
		}
		ZSteamSocket zsteamSocket2 = new ZSteamSocket(id);
		this.m_pendingConnections.Enqueue(zsteamSocket2);
		return zsteamSocket2;
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
			if (!SteamGameServerNetworking.SendP2PPacket(this.m_peerID, array, (uint)array.Length, EP2PSend.k_EP2PSendReliable, 0))
			{
				break;
			}
			this.m_totalSent += array.Length;
			this.m_sendQueue.Dequeue();
		}
	}

	public static void Update()
	{
		foreach (ZSteamSocket zsteamSocket in ZSteamSocket.m_sockets)
		{
			zsteamSocket.SendQueuedPackages();
		}
		ZSteamSocket.ReceivePackages();
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
			ZSteamSocket.QueueNewPkg(sender, array);
		}
	}

	private static void QueueNewPkg(CSteamID sender, byte[] data)
	{
		foreach (ZSteamSocket zsteamSocket in ZSteamSocket.m_sockets)
		{
			if (zsteamSocket.IsPeer(sender))
			{
				zsteamSocket.QueuePackage(data);
				return;
			}
		}
		ZSteamSocket listner = ZSteamSocket.GetListner();
		if (listner != null)
		{
			ZLog.Log("Got package from unconnected peer " + sender);
			listner.QueuePendingConnection(sender).QueuePackage(data);
			return;
		}
		ZLog.Log("Got package from unkown peer " + sender + " but no active listner");
	}

	private static ZSteamSocket GetListner()
	{
		foreach (ZSteamSocket zsteamSocket in ZSteamSocket.m_sockets)
		{
			if (zsteamSocket.IsHost())
			{
				return zsteamSocket;
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

	private static List<ZSteamSocket> m_sockets = new List<ZSteamSocket>();

	private static Callback<P2PSessionRequest_t> m_SessionRequest;

	private static Callback<P2PSessionConnectFail_t> m_connectionFailed;

	private Queue<ZSteamSocket> m_pendingConnections = new Queue<ZSteamSocket>();

	private CSteamID m_peerID = CSteamID.Nil;

	private bool m_listner;

	private Queue<ZPackage> m_pkgQueue = new Queue<ZPackage>();

	private Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	private int m_totalSent;

	private int m_totalRecv;

	private bool m_gotData;
}
