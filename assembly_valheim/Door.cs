using System;
using UnityEngine;

public class Door : MonoBehaviour, Hoverable, Interactable
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_animator = base.GetComponent<Animator>();
		if (this.m_nview)
		{
			this.m_nview.Register<bool>("UseDoor", new Action<long, bool>(this.RPC_UseDoor));
		}
		base.InvokeRepeating("UpdateState", 0f, 0.2f);
	}

	private void UpdateState()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		int @int = this.m_nview.GetZDO().GetInt("state", 0);
		this.SetState(@int);
	}

	private void SetState(int state)
	{
		if (this.m_animator.GetInteger("state") != state)
		{
			if (state != 0)
			{
				this.m_openEffects.Create(base.transform.position, base.transform.rotation, null, 1f);
			}
			else
			{
				this.m_closeEffects.Create(base.transform.position, base.transform.rotation, null, 1f);
			}
			this.m_animator.SetInteger("state", state);
		}
	}

	private bool CanInteract()
	{
		return (!(this.m_keyItem != null) || this.m_nview.GetZDO().GetInt("state", 0) == 0) && (this.m_animator.GetCurrentAnimatorStateInfo(0).IsTag("open") || this.m_animator.GetCurrentAnimatorStateInfo(0).IsTag("closed"));
	}

	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		if (!this.CanInteract())
		{
			return Localization.instance.Localize(this.m_name);
		}
		if (this.m_nview.GetZDO().GetInt("state", 0) != 0)
		{
			return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_door_close");
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_door_open");
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (!this.CanInteract())
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true))
		{
			return true;
		}
		if (this.m_keyItem != null)
		{
			if (!this.HaveKey(character))
			{
				this.m_lockedEffects.Create(base.transform.position, base.transform.rotation, null, 1f);
				character.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_needkey", new string[]
				{
					this.m_keyItem.m_itemData.m_shared.m_name
				}), 0, null);
				return true;
			}
			character.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_usingkey", new string[]
			{
				this.m_keyItem.m_itemData.m_shared.m_name
			}), 0, null);
		}
		Vector3 normalized = (character.transform.position - base.transform.position).normalized;
		bool flag = Vector3.Dot(base.transform.forward, normalized) < 0f;
		this.m_nview.InvokeRPC("UseDoor", new object[]
		{
			flag
		});
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private bool HaveKey(Humanoid player)
	{
		return this.m_keyItem == null || player.GetInventory().HaveItem(this.m_keyItem.m_itemData.m_shared.m_name);
	}

	private void RPC_UseDoor(long uid, bool forward)
	{
		if (!this.CanInteract())
		{
			return;
		}
		if (this.m_nview.GetZDO().GetInt("state", 0) == 0)
		{
			if (forward)
			{
				this.m_nview.GetZDO().Set("state", 1);
			}
			else
			{
				this.m_nview.GetZDO().Set("state", -1);
			}
		}
		else
		{
			this.m_nview.GetZDO().Set("state", 0);
		}
		this.UpdateState();
	}

	public string m_name = "door";

	public GameObject m_doorObject;

	public ItemDrop m_keyItem;

	public EffectList m_openEffects = new EffectList();

	public EffectList m_closeEffects = new EffectList();

	public EffectList m_lockedEffects = new EffectList();

	private ZNetView m_nview;

	private Animator m_animator;
}
