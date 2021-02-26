using System;
using UnityEngine;

public class WayStone : MonoBehaviour, Hoverable, Interactable
{
	private void Awake()
	{
		this.m_activeObject.SetActive(false);
	}

	public string GetHoverText()
	{
		if (this.m_activeObject.activeSelf)
		{
			return "Activated waystone";
		}
		return Localization.instance.Localize("Waystone\n[<color=yellow><b>$KEY_Use</b></color>] Activate");
	}

	public string GetHoverName()
	{
		return "Waystone";
	}

	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (!this.m_activeObject.activeSelf)
		{
			character.Message(MessageHud.MessageType.Center, this.m_activateMessage, 0, null);
			this.m_activeObject.SetActive(true);
			this.m_activeEffect.Create(base.gameObject.transform.position, base.gameObject.transform.rotation, null, 1f);
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void FixedUpdate()
	{
		if (this.m_activeObject.activeSelf && Game.instance != null)
		{
			Vector3 forward = this.GetSpawnPoint() - base.transform.position;
			forward.y = 0f;
			forward.Normalize();
			this.m_activeObject.transform.rotation = Quaternion.LookRotation(forward);
		}
	}

	private Vector3 GetSpawnPoint()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		if (playerProfile.HaveCustomSpawnPoint())
		{
			return playerProfile.GetCustomSpawnPoint();
		}
		return playerProfile.GetHomePoint();
	}

	[TextArea]
	public string m_activateMessage = "You touch the cold stone surface and you think of home.";

	public GameObject m_activeObject;

	public EffectList m_activeEffect;
}
