using System;
using UnityEngine;

public class Ladder : MonoBehaviour, Interactable, Hoverable
{
	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (!this.InUseDistance(character))
		{
			return false;
		}
		character.transform.position = this.m_targetPos.position;
		character.transform.rotation = this.m_targetPos.rotation;
		character.SetLookDir(this.m_targetPos.forward);
		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public string GetHoverText()
	{
		if (!this.InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=grey>$piece_toofar</color>");
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, base.transform.position) < this.m_useDistance;
	}

	public Transform m_targetPos;

	public string m_name = "Ladder";

	public float m_useDistance = 2f;
}
