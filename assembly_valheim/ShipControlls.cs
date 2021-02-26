using System;
using UnityEngine;

public class ShipControlls : MonoBehaviour, Interactable, Hoverable
{
	private void Awake()
	{
		this.m_nview = this.m_ship.GetComponent<ZNetView>();
		this.m_nview.Register<ZDOID>("RequestControl", new Action<long, ZDOID>(this.RPC_RequestControl));
		this.m_nview.Register<ZDOID>("ReleaseControl", new Action<long, ZDOID>(this.RPC_ReleaseControl));
		this.m_nview.Register<bool>("RequestRespons", new Action<long, bool>(this.RPC_RequestRespons));
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public bool Interact(Humanoid character, bool repeat)
	{
		if (repeat)
		{
			return false;
		}
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!this.InUseDistance(character))
		{
			return false;
		}
		Player player = character as Player;
		if (player == null)
		{
			return false;
		}
		if (player.IsEncumbered())
		{
			return false;
		}
		if (player.GetStandingOnShip() != this.m_ship)
		{
			return false;
		}
		this.m_nview.InvokeRPC("RequestControl", new object[]
		{
			player.GetZDOID()
		});
		return false;
	}

	public Ship GetShip()
	{
		return this.m_ship;
	}

	public string GetHoverText()
	{
		if (!this.InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=grey>$piece_toofar</color>");
		}
		return Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] " + this.m_hoverText);
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_hoverText);
	}

	private void RPC_RequestControl(long sender, ZDOID playerID)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_ship.IsPlayerInBoat(playerID))
		{
			return;
		}
		if (this.GetUser() == playerID || !this.HaveValidUser())
		{
			this.m_nview.GetZDO().Set("user", playerID);
			this.m_nview.InvokeRPC(sender, "RequestRespons", new object[]
			{
				true
			});
			return;
		}
		this.m_nview.InvokeRPC(sender, "RequestRespons", new object[]
		{
			false
		});
	}

	private void RPC_ReleaseControl(long sender, ZDOID playerID)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetUser() == playerID)
		{
			this.m_nview.GetZDO().Set("user", ZDOID.None);
		}
	}

	private void RPC_RequestRespons(long sender, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			Player.m_localPlayer.StartShipControl(this);
			if (this.m_attachPoint != null)
			{
				Player.m_localPlayer.AttachStart(this.m_attachPoint, false, false, this.m_attachAnimation, this.m_detachOffset);
				return;
			}
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, null);
		}
	}

	public void OnUseStop(Player player)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("ReleaseControl", new object[]
		{
			player.GetZDOID()
		});
		if (this.m_attachPoint != null)
		{
			player.AttachStop();
		}
	}

	public bool HaveValidUser()
	{
		ZDOID user = this.GetUser();
		return !user.IsNone() && this.m_ship.IsPlayerInBoat(user);
	}

	public bool IsLocalUser()
	{
		if (!Player.m_localPlayer)
		{
			return false;
		}
		ZDOID user = this.GetUser();
		return !user.IsNone() && user == Player.m_localPlayer.GetZDOID();
	}

	private ZDOID GetUser()
	{
		if (!this.m_nview.IsValid())
		{
			return ZDOID.None;
		}
		return this.m_nview.GetZDO().GetZDOID("user");
	}

	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, this.m_attachPoint.position) < this.m_maxUseRange;
	}

	public string m_hoverText = "";

	public Ship m_ship;

	public float m_maxUseRange = 10f;

	public Transform m_attachPoint;

	public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);

	public string m_attachAnimation = "attach_chair";

	private ZNetView m_nview;
}
