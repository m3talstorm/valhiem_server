using System;
using UnityEngine;

public class ZNetPeer : IDisposable
{
	public ZNetPeer(ISocket socket, bool server)
	{
		this.m_socket = socket;
		this.m_rpc = new ZRpc(this.m_socket);
		this.m_server = server;
	}

	public void Dispose()
	{
		this.m_socket.Dispose();
		this.m_rpc.Dispose();
	}

	public bool IsReady()
	{
		return this.m_uid != 0L;
	}

	public Vector3 GetRefPos()
	{
		return this.m_refPos;
	}

	public ZRpc m_rpc;

	public ISocket m_socket;

	public long m_uid;

	public bool m_server;

	public Vector3 m_refPos = Vector3.zero;

	public bool m_publicRefPos;

	public ZDOID m_characterID = ZDOID.None;

	public string m_playerName = "";
}
