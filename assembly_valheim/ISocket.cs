using System;

public interface ISocket
{
	bool IsConnected();

	void Send(ZPackage pkg);

	ZPackage Recv();

	bool IsSending();

	bool IsHost();

	void Dispose();

	bool GotNewData();

	void Close();

	string GetEndPointString();

	void GetAndResetStats(out int totalSent, out int totalRecv);

	void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec);

	ISocket Accept();

	int GetHostPort();

	bool Flush();

	string GetHostName();
}
