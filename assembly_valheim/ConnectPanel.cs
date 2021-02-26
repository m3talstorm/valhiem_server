using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectPanel : MonoBehaviour
{
	public static ConnectPanel instance
	{
		get
		{
			return ConnectPanel.m_instance;
		}
	}

	private void Start()
	{
		ConnectPanel.m_instance = this;
		this.m_root.gameObject.SetActive(false);
		this.m_playerListBaseSize = this.m_playerList.rect.height;
	}

	public static bool IsVisible()
	{
		return ConnectPanel.m_instance && ConnectPanel.m_instance.m_root.gameObject.activeSelf;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.F2))
		{
			this.m_root.gameObject.SetActive(!this.m_root.gameObject.activeSelf);
		}
		if (this.m_root.gameObject.activeInHierarchy)
		{
			if (!ZNet.instance.IsServer() && ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)
			{
				this.m_serverField.gameObject.SetActive(true);
				this.m_serverField.text = ZNet.GetServerString();
			}
			else
			{
				this.m_serverField.gameObject.SetActive(false);
			}
			this.m_worldField.text = ZNet.instance.GetWorldName();
			this.UpdateFps();
			this.m_myPort.gameObject.SetActive(ZNet.instance.IsServer());
			this.m_myPort.text = ZNet.instance.GetHostPort().ToString();
			this.m_myUID.text = ZNet.instance.GetUID().ToString();
			if (ZDOMan.instance != null)
			{
				this.m_zdos.text = ZDOMan.instance.NrOfObjects().ToString();
				float num;
				float num2;
				ZDOMan.instance.GetAverageStats(out num, out num2);
				this.m_zdosSent.text = num.ToString("0.0");
				this.m_zdosRecv.text = num2.ToString("0.0");
				this.m_activePeers.text = ZNet.instance.GetNrOfPlayers().ToString();
			}
			this.m_zdosPool.text = string.Concat(new object[]
			{
				ZDOPool.GetPoolActive(),
				" / ",
				ZDOPool.GetPoolSize(),
				" / ",
				ZDOPool.GetPoolTotal()
			});
			if (ZNetScene.instance)
			{
				this.m_zdosInstances.text = ZNetScene.instance.NrOfInstances().ToString();
			}
			float num3;
			float num4;
			int num5;
			float num6;
			float num7;
			ZNet.instance.GetNetStats(out num3, out num4, out num5, out num6, out num7);
			this.m_dataSent.text = (num6 / 1024f).ToString("0.0") + "kb/s";
			this.m_dataRecv.text = (num7 / 1024f).ToString("0.0") + "kb/s";
			this.m_ping.text = num5.ToString("0") + "ms";
			this.m_quality.text = ((int)(num3 * 100f)).ToString() + "% / " + ((int)(num4 * 100f)).ToString() + "%";
			this.m_clientSendQueue.text = ZDOMan.instance.GetClientChangeQueue().ToString();
			this.m_nrOfConnections.text = ZNet.instance.GetPeerConnections().ToString();
			string text = "";
			foreach (ZNetPeer znetPeer in ZNet.instance.GetConnectedPeers())
			{
				if (znetPeer.IsReady())
				{
					text = string.Concat(new object[]
					{
						text,
						znetPeer.m_socket.GetEndPointString(),
						" UID: ",
						znetPeer.m_uid,
						"\n"
					});
				}
				else
				{
					text = text + znetPeer.m_socket.GetEndPointString() + " connecting \n";
				}
			}
			this.m_connections.text = text;
			List<ZNet.PlayerInfo> playerList = ZNet.instance.GetPlayerList();
			float num8 = 16f;
			if (playerList.Count != this.m_playerListElements.Count)
			{
				foreach (GameObject obj in this.m_playerListElements)
				{
					UnityEngine.Object.Destroy(obj);
				}
				this.m_playerListElements.Clear();
				for (int i = 0; i < playerList.Count; i++)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_playerElement, this.m_playerList);
					(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)i * -num8);
					this.m_playerListElements.Add(gameObject);
				}
				float num9 = (float)playerList.Count * num8;
				num9 = Mathf.Max(this.m_playerListBaseSize, num9);
				this.m_playerList.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num9);
				this.m_playerListScroll.value = 1f;
			}
			for (int j = 0; j < playerList.Count; j++)
			{
				ZNet.PlayerInfo playerInfo = playerList[j];
				Text component = this.m_playerListElements[j].transform.Find("name").GetComponent<Text>();
				Text component2 = this.m_playerListElements[j].transform.Find("hostname").GetComponent<Text>();
				Component component3 = this.m_playerListElements[j].transform.Find("KickButton").GetComponent<Button>();
				component.text = playerInfo.m_name;
				component2.text = playerInfo.m_host;
				component3.gameObject.SetActive(false);
			}
			this.m_connectButton.interactable = this.ValidHost();
		}
	}

	private void UpdateFps()
	{
		this.m_frameTimer += Time.deltaTime;
		this.m_frameSamples++;
		if (this.m_frameTimer > 1f)
		{
			float num = this.m_frameTimer / (float)this.m_frameSamples;
			this.m_fps.text = (1f / num).ToString("0.0");
			this.m_frameTime.text = "( " + (num * 1000f).ToString("00.0") + "ms )";
			this.m_frameSamples = 0;
			this.m_frameTimer = 0f;
		}
	}

	private bool ValidHost()
	{
		int num = 0;
		try
		{
			num = int.Parse(this.m_hostPort.text);
		}
		catch
		{
			return false;
		}
		return !string.IsNullOrEmpty(this.m_hostName.text) && num != 0;
	}

	private static ConnectPanel m_instance;

	public Transform m_root;

	public Text m_serverField;

	public Text m_worldField;

	public Text m_statusField;

	public Text m_connections;

	public RectTransform m_playerList;

	public Scrollbar m_playerListScroll;

	public GameObject m_playerElement;

	public InputField m_hostName;

	public InputField m_hostPort;

	public Button m_connectButton;

	public Text m_myPort;

	public Text m_myUID;

	public Text m_knownHosts;

	public Text m_nrOfConnections;

	public Text m_pendingConnections;

	public Toggle m_autoConnect;

	public Text m_zdos;

	public Text m_zdosPool;

	public Text m_zdosSent;

	public Text m_zdosRecv;

	public Text m_zdosInstances;

	public Text m_activePeers;

	public Text m_ntp;

	public Text m_upnp;

	public Text m_dataSent;

	public Text m_dataRecv;

	public Text m_clientSendQueue;

	public Text m_fps;

	public Text m_frameTime;

	public Text m_ping;

	public Text m_quality;

	private float m_playerListBaseSize;

	private List<GameObject> m_playerListElements = new List<GameObject>();

	private int m_frameSamples;

	private float m_frameTimer;
}
