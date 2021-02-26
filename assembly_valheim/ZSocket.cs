using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ZSocket : IDisposable
{
	public ZSocket()
	{
		this.m_socket = ZSocket.CreateSocket();
	}

	public static Socket CreateSocket()
	{
		return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
		{
			NoDelay = true
		};
	}

	public ZSocket(Socket socket, string originalHostName = null)
	{
		this.m_socket = socket;
		this.m_originalHostName = originalHostName;
		try
		{
			this.m_endpoint = (this.m_socket.RemoteEndPoint as IPEndPoint);
		}
		catch
		{
			this.Close();
			return;
		}
		this.BeginReceive();
	}

	public void Dispose()
	{
		this.Close();
		this.m_mutex.Close();
		this.m_sendMutex.Close();
		this.m_recvBuffer = null;
	}

	public void Close()
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
			catch (Exception)
			{
			}
			this.m_socket.Close();
		}
		this.m_socket = null;
		this.m_endpoint = null;
	}

	public static IPEndPoint GetEndPoint(string host, int port)
	{
		return new IPEndPoint(Dns.GetHostEntry(host).AddressList[0], port);
	}

	public bool Connect(string host, int port)
	{
		ZLog.Log(string.Concat(new object[]
		{
			"Connecting to ",
			host,
			" : ",
			port
		}));
		IPEndPoint endPoint = ZSocket.GetEndPoint(host, port);
		this.m_socket.BeginConnect(endPoint, null, null).AsyncWaitHandle.WaitOne(3000, true);
		if (!this.m_socket.Connected)
		{
			return false;
		}
		try
		{
			this.m_endpoint = (this.m_socket.RemoteEndPoint as IPEndPoint);
		}
		catch
		{
			this.Close();
			return false;
		}
		this.BeginReceive();
		ZLog.Log(" connected");
		return true;
	}

	public bool StartHost(int port)
	{
		if (this.m_listenPort != 0)
		{
			this.Close();
		}
		if (!this.BindSocket(this.m_socket, IPAddress.Any, port, port + 10))
		{
			ZLog.LogWarning("Failed to bind socket");
			return false;
		}
		this.m_socket.Listen(100);
		this.m_socket.BeginAccept(new AsyncCallback(this.AcceptCallback), this.m_socket);
		return true;
	}

	private bool BindSocket(Socket socket, IPAddress ipAddress, int startPort, int endPort)
	{
		for (int i = startPort; i <= endPort; i++)
		{
			try
			{
				IPEndPoint localEP = new IPEndPoint(ipAddress, i);
				this.m_socket.Bind(localEP);
				this.m_listenPort = i;
				ZLog.Log("Bound socket port " + i);
				return true;
			}
			catch
			{
				ZLog.Log("Failed to bind port:" + i);
			}
		}
		return false;
	}

	private void BeginReceive()
	{
		this.m_socket.BeginReceive(this.m_recvSizeBuffer, 0, this.m_recvSizeBuffer.Length, SocketFlags.None, new AsyncCallback(this.PkgSizeReceived), this.m_socket);
	}

	private void PkgSizeReceived(IAsyncResult res)
	{
		int num;
		try
		{
			num = this.m_socket.EndReceive(res);
		}
		catch (Exception)
		{
			this.Disconnect();
			return;
		}
		this.m_totalRecv += num;
		if (num != 4)
		{
			this.Disconnect();
			return;
		}
		int num2 = BitConverter.ToInt32(this.m_recvSizeBuffer, 0);
		if (num2 == 0 || num2 > 10485760)
		{
			ZLog.LogError("Invalid pkg size " + num2);
			return;
		}
		this.m_lastRecvPkgSize = num2;
		this.m_recvOffset = 0;
		this.m_lastRecvPkgSize = num2;
		if (this.m_recvBuffer == null)
		{
			this.m_recvBuffer = new byte[ZSocket.m_maxRecvBuffer];
		}
		this.m_socket.BeginReceive(this.m_recvBuffer, this.m_recvOffset, this.m_lastRecvPkgSize, SocketFlags.None, new AsyncCallback(this.PkgReceived), this.m_socket);
	}

	private void Disconnect()
	{
		if (this.m_socket != null)
		{
			try
			{
				this.m_socket.Disconnect(true);
			}
			catch
			{
			}
		}
	}

	private void PkgReceived(IAsyncResult res)
	{
		int num;
		try
		{
			num = this.m_socket.EndReceive(res);
		}
		catch (Exception)
		{
			this.Disconnect();
			return;
		}
		this.m_totalRecv += num;
		this.m_recvOffset += num;
		if (this.m_recvOffset < this.m_lastRecvPkgSize)
		{
			int size = this.m_lastRecvPkgSize - this.m_recvOffset;
			if (this.m_recvBuffer == null)
			{
				this.m_recvBuffer = new byte[ZSocket.m_maxRecvBuffer];
			}
			this.m_socket.BeginReceive(this.m_recvBuffer, this.m_recvOffset, size, SocketFlags.None, new AsyncCallback(this.PkgReceived), this.m_socket);
			return;
		}
		ZPackage item = new ZPackage(this.m_recvBuffer, this.m_lastRecvPkgSize);
		this.m_mutex.WaitOne();
		this.m_pkgQueue.Enqueue(item);
		this.m_mutex.ReleaseMutex();
		this.BeginReceive();
	}

	private void AcceptCallback(IAsyncResult res)
	{
		Socket item;
		try
		{
			item = this.m_socket.EndAccept(res);
		}
		catch
		{
			this.Disconnect();
			return;
		}
		this.m_mutex.WaitOne();
		this.m_newConnections.Enqueue(item);
		this.m_mutex.ReleaseMutex();
		this.m_socket.BeginAccept(new AsyncCallback(this.AcceptCallback), this.m_socket);
	}

	public ZSocket Accept()
	{
		if (this.m_newConnections.Count == 0)
		{
			return null;
		}
		Socket socket = null;
		this.m_mutex.WaitOne();
		if (this.m_newConnections.Count > 0)
		{
			socket = this.m_newConnections.Dequeue();
		}
		this.m_mutex.ReleaseMutex();
		if (socket != null)
		{
			return new ZSocket(socket, null);
		}
		return null;
	}

	public bool IsConnected()
	{
		return this.m_socket != null && this.m_socket.Connected;
	}

	public void Send(ZPackage pkg)
	{
		if (pkg.Size() == 0)
		{
			return;
		}
		if (this.m_socket == null || !this.m_socket.Connected)
		{
			return;
		}
		byte[] array = pkg.GetArray();
		byte[] bytes = BitConverter.GetBytes(array.Length);
		this.m_sendMutex.WaitOne();
		if (!this.m_isSending)
		{
			if (array.Length > 10485760)
			{
				ZLog.LogError("Too big data package: " + array.Length);
			}
			try
			{
				this.m_totalSent += bytes.Length;
				this.m_socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, new AsyncCallback(this.PkgSent), null);
				this.m_isSending = true;
				this.m_sendQueue.Enqueue(array);
				goto IL_DA;
			}
			catch (Exception arg)
			{
				ZLog.Log("Handled exception in ZSocket:Send:" + arg);
				this.Disconnect();
				goto IL_DA;
			}
		}
		this.m_sendQueue.Enqueue(bytes);
		this.m_sendQueue.Enqueue(array);
		IL_DA:
		this.m_sendMutex.ReleaseMutex();
	}

	private void PkgSent(IAsyncResult res)
	{
		this.m_sendMutex.WaitOne();
		if (this.m_sendQueue.Count > 0 && this.IsConnected())
		{
			byte[] array = this.m_sendQueue.Dequeue();
			try
			{
				this.m_totalSent += array.Length;
				this.m_socket.BeginSend(array, 0, array.Length, SocketFlags.None, new AsyncCallback(this.PkgSent), null);
				goto IL_86;
			}
			catch (Exception arg)
			{
				ZLog.Log("Handled exception in pkgsent:" + arg);
				this.m_isSending = false;
				this.Disconnect();
				goto IL_86;
			}
		}
		this.m_isSending = false;
		IL_86:
		this.m_sendMutex.ReleaseMutex();
	}

	public ZPackage Recv()
	{
		if (this.m_socket == null)
		{
			return null;
		}
		if (this.m_pkgQueue.Count == 0)
		{
			return null;
		}
		ZPackage result = null;
		this.m_mutex.WaitOne();
		if (this.m_pkgQueue.Count > 0)
		{
			result = this.m_pkgQueue.Dequeue();
		}
		this.m_mutex.ReleaseMutex();
		return result;
	}

	public string GetEndPointString()
	{
		if (this.m_endpoint != null)
		{
			return this.m_endpoint.ToString();
		}
		return "None";
	}

	public string GetEndPointHost()
	{
		if (this.m_endpoint != null)
		{
			return this.m_endpoint.Address.ToString();
		}
		return "None";
	}

	public IPEndPoint GetEndPoint()
	{
		return this.m_endpoint;
	}

	public bool IsPeer(string host, int port)
	{
		if (!this.IsConnected())
		{
			return false;
		}
		if (this.m_endpoint == null)
		{
			return false;
		}
		IPEndPoint endpoint = this.m_endpoint;
		return (endpoint.Address.ToString() == host && endpoint.Port == port) || (this.m_originalHostName != null && this.m_originalHostName == host && endpoint.Port == port);
	}

	public bool IsHost()
	{
		return this.m_listenPort != 0;
	}

	public int GetHostPort()
	{
		return this.m_listenPort;
	}

	public bool IsSending()
	{
		return this.m_isSending || this.m_sendQueue.Count > 0;
	}

	public void GetAndResetStats(out int totalSent, out int totalRecv)
	{
		totalSent = this.m_totalSent;
		totalRecv = this.m_totalRecv;
		this.m_totalSent = 0;
		this.m_totalRecv = 0;
	}

	private Socket m_socket;

	private Mutex m_mutex = new Mutex();

	private Mutex m_sendMutex = new Mutex();

	private Queue<Socket> m_newConnections = new Queue<Socket>();

	private static int m_maxRecvBuffer = 10485760;

	private int m_recvOffset;

	private byte[] m_recvBuffer;

	private byte[] m_recvSizeBuffer = new byte[4];

	private Queue<ZPackage> m_pkgQueue = new Queue<ZPackage>();

	private bool m_isSending;

	private Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	private IPEndPoint m_endpoint;

	private string m_originalHostName;

	private int m_listenPort;

	private int m_lastRecvPkgSize;

	private int m_totalSent;

	private int m_totalRecv;
}
