using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ZNtp : IDisposable
{
	public static ZNtp instance
	{
		get
		{
			return ZNtp.m_instance;
		}
	}

	public ZNtp()
	{
		ZNtp.m_instance = this;
		this.m_ntpTime = DateTime.UtcNow;
		this.m_ntpThread = new Thread(new ThreadStart(this.NtpThread));
		this.m_ntpThread.Start();
	}

	public void Dispose()
	{
		if (this.m_ntpThread != null)
		{
			ZLog.Log("Stoping ntp thread");
			this.m_lock.WaitOne();
			this.m_stop = true;
			this.m_ntpThread.Abort();
			this.m_lock.ReleaseMutex();
			this.m_ntpThread = null;
		}
		if (this.m_lock != null)
		{
			this.m_lock.Close();
			this.m_lock = null;
		}
	}

	public bool GetStatus()
	{
		return this.m_status;
	}

	public void Update(float dt)
	{
		this.m_lock.WaitOne();
		this.m_ntpTime = this.m_ntpTime.AddSeconds((double)dt);
		this.m_lock.ReleaseMutex();
	}

	private void NtpThread()
	{
		while (!this.m_stop)
		{
			DateTime ntpTime;
			if (this.GetNetworkTime("pool.ntp.org", out ntpTime))
			{
				this.m_status = true;
				this.m_lock.WaitOne();
				this.m_ntpTime = ntpTime;
				this.m_lock.ReleaseMutex();
			}
			else
			{
				this.m_status = false;
			}
			Thread.Sleep(60000);
		}
	}

	public DateTime GetTime()
	{
		return this.m_ntpTime;
	}

	private bool GetNetworkTime(string ntpServer, out DateTime time)
	{
		byte[] array = new byte[48];
		array[0] = 27;
		IPAddress[] addressList;
		try
		{
			addressList = Dns.GetHostEntry(ntpServer).AddressList;
			if (addressList.Length == 0)
			{
				ZLog.Log("Dns lookup failed");
				time = DateTime.UtcNow;
				return false;
			}
		}
		catch
		{
			ZLog.Log("Failed ntp dns lookup");
			time = DateTime.UtcNow;
			return false;
		}
		IPEndPoint remoteEP = new IPEndPoint(addressList[0], 123);
		Socket socket = null;
		try
		{
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			socket.ReceiveTimeout = 3000;
			socket.SendTimeout = 3000;
			socket.Connect(remoteEP);
			if (!socket.Connected)
			{
				ZLog.Log("Failed to connect to ntp");
				time = DateTime.UtcNow;
				socket.Close();
				return false;
			}
			socket.Send(array);
			socket.Receive(array);
			socket.Shutdown(SocketShutdown.Both);
			socket.Close();
		}
		catch
		{
			if (socket != null)
			{
				socket.Close();
			}
			time = DateTime.UtcNow;
			return false;
		}
		ulong num = (ulong)array[40] << 24 | (ulong)array[41] << 16 | (ulong)array[42] << 8 | (ulong)array[43];
		ulong num2 = (ulong)array[44] << 24 | (ulong)array[45] << 16 | (ulong)array[46] << 8 | (ulong)array[47];
		ulong num3 = num * 1000UL + num2 * 1000UL / 4294967296UL;
		time = new DateTime(1900, 1, 1).AddMilliseconds((double)num3);
		return true;
	}

	private static ZNtp m_instance;

	private DateTime m_ntpTime;

	private bool m_status;

	private bool m_stop;

	private Thread m_ntpThread;

	private Mutex m_lock = new Mutex();
}
