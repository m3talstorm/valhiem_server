using System;
using Steamworks;

public class ServerData
{
	public override bool Equals(object obj)
	{
		ServerData serverData = obj as ServerData;
		return serverData != null && serverData.m_steamHostID == this.m_steamHostID && serverData.m_steamHostAddr.Equals(this.m_steamHostAddr);
	}

	public override string ToString()
	{
		if (this.m_steamHostID != 0UL)
		{
			return this.m_steamHostID.ToString();
		}
		string result;
		this.m_steamHostAddr.ToString(out result, true);
		return result;
	}

	public string m_name;

	public string m_host;

	public int m_port;

	public bool m_password;

	public bool m_upnp;

	public string m_version;

	public int m_players;

	public ulong m_steamHostID;

	public SteamNetworkingIPAddr m_steamHostAddr;
}
