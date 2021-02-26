using System;
using UnityEngine;

public class TeleportWorld : MonoBehaviour, Hoverable, Interactable, TextReceiver
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		this.m_hadTarget = this.HaveTarget();
		this.m_nview.Register<string>("SetTag", new Action<long, string>(this.RPC_SetTag));
		base.InvokeRepeating("UpdatePortal", 0.5f, 0.5f);
	}

	public string GetHoverText()
	{
		string text = this.GetText();
		string text2 = this.HaveTarget() ? "$piece_portal_connected" : "$piece_portal_unconnected";
		return Localization.instance.Localize(string.Concat(new string[]
		{
			"$piece_portal $piece_portal_tag:\"",
			text,
			"\"  [",
			text2,
			"]\n[<color=yellow><b>$KEY_Use</b></color>] $piece_portal_settag"
		}));
	}

	public string GetHoverName()
	{
		return "Teleport";
	}

	public bool Interact(Humanoid human, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true))
		{
			human.Message(MessageHud.MessageType.Center, "$piece_noaccess", 0, null);
			return true;
		}
		TextInput.instance.RequestText(this, "$piece_portal_tag", 10);
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void UpdatePortal()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		Player closestPlayer = Player.GetClosestPlayer(this.m_proximityRoot.position, this.m_activationRange);
		bool flag = this.HaveTarget();
		if (flag && !this.m_hadTarget)
		{
			this.m_connected.Create(base.transform.position, base.transform.rotation, null, 1f);
		}
		this.m_hadTarget = flag;
		this.m_target_found.SetActive(closestPlayer && closestPlayer.IsTeleportable() && this.TargetFound());
	}

	private void Update()
	{
		this.m_colorAlpha = Mathf.MoveTowards(this.m_colorAlpha, this.m_hadTarget ? 1f : 0f, Time.deltaTime);
		this.m_model.material.SetColor("_EmissionColor", Color.Lerp(this.m_colorUnconnected, this.m_colorTargetfound, this.m_colorAlpha));
	}

	public void Teleport(Player player)
	{
		if (!this.TargetFound())
		{
			return;
		}
		if (!player.IsTeleportable())
		{
			player.Message(MessageHud.MessageType.Center, "$msg_noteleport", 0, null);
			return;
		}
		ZLog.Log("Teleporting " + player.GetPlayerName());
		ZDOID zdoid = this.m_nview.GetZDO().GetZDOID("target");
		if (zdoid == ZDOID.None)
		{
			return;
		}
		ZDO zdo = ZDOMan.instance.GetZDO(zdoid);
		Vector3 position = zdo.GetPosition();
		Quaternion rotation = zdo.GetRotation();
		Vector3 a = rotation * Vector3.forward;
		Vector3 pos = position + a * this.m_exitDistance + Vector3.up;
		player.TeleportTo(pos, rotation, true);
	}

	public string GetText()
	{
		return this.m_nview.GetZDO().GetString("tag", "");
	}

	public void SetText(string text)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("SetTag", new object[]
		{
			text
		});
	}

	private void RPC_SetTag(long sender, string tag)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetText() == tag)
		{
			return;
		}
		this.m_nview.GetZDO().Set("tag", tag);
	}

	private bool HaveTarget()
	{
		return this.m_nview.GetZDO().GetZDOID("target") != ZDOID.None;
	}

	private bool TargetFound()
	{
		ZDOID zdoid = this.m_nview.GetZDO().GetZDOID("target");
		if (zdoid == ZDOID.None)
		{
			return false;
		}
		if (ZDOMan.instance.GetZDO(zdoid) == null)
		{
			ZDOMan.instance.RequestZDO(zdoid);
			return false;
		}
		return true;
	}

	public float m_activationRange = 5f;

	public float m_exitDistance = 1f;

	public Transform m_proximityRoot;

	[ColorUsage(true, true)]
	public Color m_colorUnconnected = Color.white;

	[ColorUsage(true, true)]
	public Color m_colorTargetfound = Color.white;

	public EffectFade m_target_found;

	public MeshRenderer m_model;

	public EffectList m_connected;

	private ZNetView m_nview;

	private bool m_hadTarget;

	private float m_colorAlpha;
}
