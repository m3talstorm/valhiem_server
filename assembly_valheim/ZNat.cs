using System;
using System.IO;

public class ZNat : IDisposable
{
	public void Dispose()
	{
	}

	public void SetPort(int port)
	{
		if (this.m_port == port)
		{
			return;
		}
		this.m_port = port;
	}

	public void Update(float dt)
	{
	}

	public bool GetStatus()
	{
		return this.m_mappingOK;
	}

	private FileStream m_output;

	private bool m_mappingOK;

	private int m_port;
}
