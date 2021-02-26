using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ZBroastcast : IDisposable
{
	public static ZBroastcast instance
	{
		get
		{
			return ZBroastcast.m_instance;
		}
	}

	public static void Initialize()
	{
		if (ZBroastcast.m_instance == null)
		{
			ZBroastcast.m_instance = new ZBroastcast();
		}
	}

	private ZBroastcast()
	{
		ZLog.Log("opening zbroadcast");
		this.m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		this.m_socket.EnableBroadcast = true;
		try
		{
			this.m_listner = new UdpClient(6542);
			this.m_listner.EnableBroadcast = true;
			this.m_listner.BeginReceive(new AsyncCallback(this.GotPackage), null);
		}
		catch (Exception ex)
		{
			this.m_listner = null;
			ZLog.Log("Error creating zbroadcast socket " + ex.ToString());
		}
	}

	public void SetServerPort(int port)
	{
		this.m_myPort = port;
	}

	public void Dispose()
	{
		ZLog.Log("Clozing zbroadcast");
		if (this.m_listner != null)
		{
			this.m_listner.Close();
		}
		this.m_socket.Close();
		this.m_lock.Close();
		if (ZBroastcast.m_instance == this)
		{
			ZBroastcast.m_instance = null;
		}
	}

	public void Update(float dt)
	{
		this.m_timer -= dt;
		if (this.m_timer <= 0f)
		{
			this.m_timer = 5f;
			if (this.m_myPort != 0)
			{
				this.Ping();
			}
		}
		this.TimeoutHosts(dt);
	}

	private void GotPackage(IAsyncResult ar)
	{
		IPEndPoint ipendPoint = new IPEndPoint(0L, 0);
		byte[] array;
		try
		{
			array = this.m_listner.EndReceive(ar, ref ipendPoint);
		}
		catch (ObjectDisposedException)
		{
			return;
		}
		if (array.Length < 5)
		{
			return;
		}
		ZPackage zpackage = new ZPackage(array);
		if (zpackage.ReadChar() != 'F')
		{
			return;
		}
		if (zpackage.ReadChar() != 'E')
		{
			return;
		}
		if (zpackage.ReadChar() != 'J')
		{
			return;
		}
		if (zpackage.ReadChar() != 'D')
		{
			return;
		}
		int port = zpackage.ReadInt();
		this.m_lock.WaitOne();
		this.AddHost(ipendPoint.Address.ToString(), port);
		this.m_lock.ReleaseMutex();
		this.m_listner.BeginReceive(new AsyncCallback(this.GotPackage), null);
	}

	private void Ping()
	{
		IPEndPoint remoteEP = new IPEndPoint(IPAddress.Broadcast, 6542);
		ZPackage zpackage = new ZPackage();
		zpackage.Write('F');
		zpackage.Write('E');
		zpackage.Write('J');
		zpackage.Write('D');
		zpackage.Write(this.m_myPort);
		this.m_socket.SendTo(zpackage.GetArray(), remoteEP);
	}

	private void AddHost(string host, int port)
	{
		foreach (ZBroastcast.HostData hostData in this.m_hosts)
		{
			if (hostData.m_port == port && hostData.m_host == host)
			{
				hostData.m_timeout = 0f;
				return;
			}
		}
		ZBroastcast.HostData hostData2 = new ZBroastcast.HostData();
		hostData2.m_host = host;
		hostData2.m_port = port;
		hostData2.m_timeout = 0f;
		this.m_hosts.Add(hostData2);
	}

	private void TimeoutHosts(float dt)
	{
		this.m_lock.WaitOne();
		foreach (ZBroastcast.HostData hostData in this.m_hosts)
		{
			hostData.m_timeout += dt;
			if (hostData.m_timeout > 10f)
			{
				this.m_hosts.Remove(hostData);
				return;
			}
		}
		this.m_lock.ReleaseMutex();
	}

	public void GetHostList(List<ZBroastcast.HostData> hosts)
	{
		hosts.AddRange(this.m_hosts);
	}

	private List<ZBroastcast.HostData> m_hosts = new List<ZBroastcast.HostData>();

	private static ZBroastcast m_instance;

	private const int m_port = 6542;

	private const float m_pingInterval = 5f;

	private const float m_hostTimeout = 10f;

	private float m_timer;

	private int m_myPort;

	private Socket m_socket;

	private UdpClient m_listner;

	private Mutex m_lock = new Mutex();

	public class HostData
	{
		public string m_host;

		public int m_port;

		public float m_timeout;
	}
}
