using System;
using System.IO;
using UnityEngine;

public class ServerCtrl
{
	public static ServerCtrl instance
	{
		get
		{
			return ServerCtrl.m_instance;
		}
	}

	public static void Initialize()
	{
		if (ServerCtrl.m_instance == null)
		{
			ServerCtrl.m_instance = new ServerCtrl();
		}
	}

	private ServerCtrl()
	{
		this.ClearExitFile();
	}

	public void Update(float dt)
	{
		this.CheckExit(dt);
	}

	private void CheckExit(float dt)
	{
		this.m_checkTimer += dt;
		if (this.m_checkTimer > 2f)
		{
			this.m_checkTimer = 0f;
			if (File.Exists("server_exit.drp"))
			{
				Application.Quit();
			}
		}
	}

	private void ClearExitFile()
	{
		try
		{
			File.Delete("server_exit.drp");
		}
		catch
		{
		}
	}

	private static ServerCtrl m_instance;

	private float m_checkTimer;
}
