using System;
using System.Net;
using System.Net.Sockets;

public class ZConnector : IDisposable
{
	public ZConnector(string host, int port)
	{
		this.m_host = host;
		this.m_port = port;
		ZLog.Log(string.Concat(new object[]
		{
			"Zconnect ",
			host,
			" ",
			port
		}));
		Dns.BeginGetHostEntry(host, new AsyncCallback(this.OnHostLookupDone), null);
	}

	public void Dispose()
	{
		this.Close();
	}

	private void Close()
	{
		if (this.m_socket != null)
		{
			try
			{
				if (this.m_socket.Connected)
				{
					this.m_socket.Shutdown(SocketShutdown.Both);
				}
			}
			catch (Exception arg)
			{
				ZLog.Log("Some excepetion when shuting down ZConnector socket, ignoring:" + arg);
			}
			this.m_socket.Close();
			this.m_socket = null;
		}
		this.m_abort = true;
	}

	public bool IsPeer(string host, int port)
	{
		return this.m_host == host && this.m_port == port;
	}

	public bool UpdateStatus(float dt, bool logErrors = false)
	{
		if (this.m_abort)
		{
			ZLog.Log("ZConnector - Abort");
			return true;
		}
		if (this.m_dnsError)
		{
			ZLog.Log("ZConnector - dns error");
			return true;
		}
		if (this.m_result != null && this.m_result.IsCompleted)
		{
			ZLog.Log("ZConnector - result completed");
			return true;
		}
		this.m_timer += dt;
		if (this.m_timer > ZConnector.m_timeout)
		{
			ZLog.Log("ZConnector - timeout");
			this.Close();
			return true;
		}
		return false;
	}

	public ZSocket Complete()
	{
		if (this.m_socket != null && this.m_socket.Connected)
		{
			ZSocket result = new ZSocket(this.m_socket, this.m_host);
			this.m_socket = null;
			return result;
		}
		this.Close();
		return null;
	}

	public bool CompareEndPoint(IPEndPoint endpoint)
	{
		return this.m_endPoint.Equals(endpoint);
	}

	private void OnHostLookupDone(IAsyncResult res)
	{
		IPHostEntry iphostEntry = Dns.EndGetHostEntry(res);
		if (this.m_abort)
		{
			ZLog.Log("Host lookup abort");
			return;
		}
		if (iphostEntry.AddressList.Length == 0)
		{
			this.m_dnsError = true;
			ZLog.Log("Host lookup adress list empty");
			return;
		}
		ZLog.Log("Host lookup done , addresses: " + iphostEntry.AddressList.Length);
		foreach (IPAddress arg in iphostEntry.AddressList)
		{
			ZLog.Log(" " + arg);
		}
		this.m_socket = ZSocket.CreateSocket();
		this.m_result = this.m_socket.BeginConnect(iphostEntry.AddressList, this.m_port, null, null);
	}

	public string GetEndPointString()
	{
		return this.m_host + ":" + this.m_port;
	}

	public string GetHostName()
	{
		return this.m_host;
	}

	public int GetHostPort()
	{
		return this.m_port;
	}

	private Socket m_socket;

	private IAsyncResult m_result;

	private IPEndPoint m_endPoint;

	private string m_host;

	private int m_port;

	private bool m_dnsError;

	private bool m_abort;

	private float m_timer;

	private static float m_timeout = 5f;
}
