using System;
using System.Collections.Generic;

public class ZRoutedRpc
{
	public static ZRoutedRpc instance
	{
		get
		{
			return ZRoutedRpc.m_instance;
		}
	}

	public ZRoutedRpc(bool server)
	{
		ZRoutedRpc.m_instance = this;
		this.m_server = server;
	}

	public void SetUID(long uid)
	{
		this.m_id = uid;
	}

	public void AddPeer(ZNetPeer peer)
	{
		this.m_peers.Add(peer);
		peer.m_rpc.Register<ZPackage>("RoutedRPC", new Action<ZRpc, ZPackage>(this.RPC_RoutedRPC));
		if (this.m_onNewPeer != null)
		{
			this.m_onNewPeer(peer.m_uid);
		}
	}

	public void RemovePeer(ZNetPeer peer)
	{
		this.m_peers.Remove(peer);
	}

	private ZNetPeer GetPeer(long uid)
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

	public void InvokeRoutedRPC(long targetPeerID, string methodName, params object[] parameters)
	{
		this.InvokeRoutedRPC(targetPeerID, ZDOID.None, methodName, parameters);
	}

	public void InvokeRoutedRPC(string methodName, params object[] parameters)
	{
		this.InvokeRoutedRPC(this.GetServerPeerID(), methodName, parameters);
	}

	private long GetServerPeerID()
	{
		if (this.m_server)
		{
			return this.m_id;
		}
		if (this.m_peers.Count > 0)
		{
			return this.m_peers[0].m_uid;
		}
		return 0L;
	}

	public void InvokeRoutedRPC(long targetPeerID, ZDOID targetZDO, string methodName, params object[] parameters)
	{
		ZRoutedRpc.RoutedRPCData routedRPCData = new ZRoutedRpc.RoutedRPCData();
		ZRoutedRpc.RoutedRPCData routedRPCData2 = routedRPCData;
		long id = this.m_id;
		int rpcMsgID = this.m_rpcMsgID;
		this.m_rpcMsgID = rpcMsgID + 1;
		routedRPCData2.m_msgID = id + (long)rpcMsgID;
		routedRPCData.m_senderPeerID = this.m_id;
		routedRPCData.m_targetPeerID = targetPeerID;
		routedRPCData.m_targetZDO = targetZDO;
		routedRPCData.m_methodHash = methodName.GetStableHashCode();
		ZRpc.Serialize(parameters, ref routedRPCData.m_parameters);
		routedRPCData.m_parameters.SetPos(0);
		if (targetPeerID == this.m_id || targetPeerID == 0L)
		{
			this.HandleRoutedRPC(routedRPCData);
		}
		if (targetPeerID != this.m_id)
		{
			this.RouteRPC(routedRPCData);
		}
	}

	private void RouteRPC(ZRoutedRpc.RoutedRPCData rpcData)
	{
		ZPackage zpackage = new ZPackage();
		rpcData.Serialize(zpackage);
		if (this.m_server)
		{
			if (rpcData.m_targetPeerID != 0L)
			{
				ZNetPeer peer = this.GetPeer(rpcData.m_targetPeerID);
				if (peer != null && peer.IsReady())
				{
					peer.m_rpc.Invoke("RoutedRPC", new object[]
					{
						zpackage
					});
					return;
				}
				return;
			}
			else
			{
				using (List<ZNetPeer>.Enumerator enumerator = this.m_peers.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						ZNetPeer znetPeer = enumerator.Current;
						if (rpcData.m_senderPeerID != znetPeer.m_uid && znetPeer.IsReady())
						{
							znetPeer.m_rpc.Invoke("RoutedRPC", new object[]
							{
								zpackage
							});
						}
					}
					return;
				}
			}
		}
		foreach (ZNetPeer znetPeer2 in this.m_peers)
		{
			if (znetPeer2.IsReady())
			{
				znetPeer2.m_rpc.Invoke("RoutedRPC", new object[]
				{
					zpackage
				});
			}
		}
	}

	private void RPC_RoutedRPC(ZRpc rpc, ZPackage pkg)
	{
		ZRoutedRpc.RoutedRPCData routedRPCData = new ZRoutedRpc.RoutedRPCData();
		routedRPCData.Deserialize(pkg);
		if (routedRPCData.m_targetPeerID == this.m_id || routedRPCData.m_targetPeerID == 0L)
		{
			this.HandleRoutedRPC(routedRPCData);
		}
		if (this.m_server && routedRPCData.m_targetPeerID != this.m_id)
		{
			this.RouteRPC(routedRPCData);
		}
	}

	private void HandleRoutedRPC(ZRoutedRpc.RoutedRPCData data)
	{
		if (data.m_targetZDO.IsNone())
		{
			RoutedMethodBase routedMethodBase;
			if (this.m_functions.TryGetValue(data.m_methodHash, out routedMethodBase))
			{
				routedMethodBase.Invoke(data.m_senderPeerID, data.m_parameters);
				return;
			}
		}
		else
		{
			ZDO zdo = ZDOMan.instance.GetZDO(data.m_targetZDO);
			if (zdo != null)
			{
				ZNetView znetView = ZNetScene.instance.FindInstance(zdo);
				if (znetView != null)
				{
					znetView.HandleRoutedRPC(data);
				}
			}
		}
	}

	public void Register(string name, Action<long> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod(f));
	}

	public void Register<T>(string name, Action<long, T> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T>(f));
	}

	public void Register<T, U>(string name, Action<long, T, U> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U>(f));
	}

	public void Register<T, U, V>(string name, Action<long, T, U, V> f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V>(f));
	}

	public void Register<T, U, V, B>(string name, RoutedMethod<T, U, V, B>.Method f)
	{
		this.m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V, B>(f));
	}

	public static long Everybody;

	public Action<long> m_onNewPeer;

	private int m_rpcMsgID = 1;

	private bool m_server;

	private long m_id;

	private List<ZNetPeer> m_peers = new List<ZNetPeer>();

	private Dictionary<int, RoutedMethodBase> m_functions = new Dictionary<int, RoutedMethodBase>();

	private static ZRoutedRpc m_instance;

	public class RoutedRPCData
	{
		public void Serialize(ZPackage pkg)
		{
			pkg.Write(this.m_msgID);
			pkg.Write(this.m_senderPeerID);
			pkg.Write(this.m_targetPeerID);
			pkg.Write(this.m_targetZDO);
			pkg.Write(this.m_methodHash);
			pkg.Write(this.m_parameters);
		}

		public void Deserialize(ZPackage pkg)
		{
			this.m_msgID = pkg.ReadLong();
			this.m_senderPeerID = pkg.ReadLong();
			this.m_targetPeerID = pkg.ReadLong();
			this.m_targetZDO = pkg.ReadZDOID();
			this.m_methodHash = pkg.ReadInt();
			this.m_parameters = pkg.ReadPackage();
		}

		public long m_msgID;

		public long m_senderPeerID;

		public long m_targetPeerID;

		public ZDOID m_targetZDO;

		public int m_methodHash;

		public ZPackage m_parameters = new ZPackage();
	}
}
