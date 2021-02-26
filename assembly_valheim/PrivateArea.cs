using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PrivateArea : MonoBehaviour, Hoverable, Interactable
{
	private void Awake()
	{
		if (this.m_areaMarker)
		{
			this.m_areaMarker.m_radius = this.m_radius;
		}
		this.m_nview = base.GetComponent<ZNetView>();
		if (!this.m_nview.IsValid())
		{
			return;
		}
		WearNTear component = base.GetComponent<WearNTear>();
		component.m_onDamaged = (Action)Delegate.Combine(component.m_onDamaged, new Action(this.OnDamaged));
		this.m_piece = base.GetComponent<Piece>();
		if (this.m_areaMarker)
		{
			this.m_areaMarker.gameObject.SetActive(false);
		}
		if (this.m_inRangeEffect)
		{
			this.m_inRangeEffect.SetActive(false);
		}
		PrivateArea.m_allAreas.Add(this);
		base.InvokeRepeating("UpdateStatus", 0f, 1f);
		this.m_nview.Register<long>("ToggleEnabled", new Action<long, long>(this.RPC_ToggleEnabled));
		this.m_nview.Register<long, string>("TogglePermitted", new Action<long, long, string>(this.RPC_TogglePermitted));
		this.m_nview.Register("FlashShield", new Action<long>(this.RPC_FlashShield));
	}

	private void OnDestroy()
	{
		PrivateArea.m_allAreas.Remove(this);
	}

	private void UpdateStatus()
	{
		bool flag = this.IsEnabled();
		this.m_enabledEffect.SetActive(flag);
		this.m_flashAvailable = true;
		foreach (Material material in this.m_model.materials)
		{
			if (flag)
			{
				material.EnableKeyword("_EMISSION");
			}
			else
			{
				material.DisableKeyword("_EMISSION");
			}
		}
	}

	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		if (Player.m_localPlayer == null)
		{
			return "";
		}
		this.ShowAreaMarker();
		StringBuilder stringBuilder = new StringBuilder(256);
		if (this.m_piece.IsCreator())
		{
			if (this.IsEnabled())
			{
				stringBuilder.Append(this.m_name + " ( $piece_guardstone_active )");
				stringBuilder.Append("\n$piece_guardstone_owner:" + this.GetCreatorName());
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_deactivate");
			}
			else
			{
				stringBuilder.Append(this.m_name + " ($piece_guardstone_inactive )");
				stringBuilder.Append("\n$piece_guardstone_owner:" + this.GetCreatorName());
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_activate");
			}
		}
		else if (this.IsEnabled())
		{
			stringBuilder.Append(this.m_name + " ( $piece_guardstone_active )");
			stringBuilder.Append("\n$piece_guardstone_owner:" + this.GetCreatorName());
		}
		else
		{
			stringBuilder.Append(this.m_name + " ( $piece_guardstone_inactive )");
			stringBuilder.Append("\n$piece_guardstone_owner:" + this.GetCreatorName());
			if (this.IsPermitted(Player.m_localPlayer.GetPlayerID()))
			{
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_remove");
			}
			else
			{
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_add");
			}
		}
		this.AddUserList(stringBuilder);
		return Localization.instance.Localize(stringBuilder.ToString());
	}

	private void AddUserList(StringBuilder text)
	{
		List<KeyValuePair<long, string>> permittedPlayers = this.GetPermittedPlayers();
		text.Append("\n$piece_guardstone_additional: ");
		for (int i = 0; i < permittedPlayers.Count; i++)
		{
			text.Append(permittedPlayers[i].Value);
			if (i != permittedPlayers.Count - 1)
			{
				text.Append(", ");
			}
		}
	}

	private void RemovePermitted(long playerID)
	{
		List<KeyValuePair<long, string>> permittedPlayers = this.GetPermittedPlayers();
		if (permittedPlayers.RemoveAll((KeyValuePair<long, string> x) => x.Key == playerID) > 0)
		{
			this.SetPermittedPlayers(permittedPlayers);
			this.m_removedPermittedEffect.Create(base.transform.position, base.transform.rotation, null, 1f);
		}
	}

	private bool IsPermitted(long playerID)
	{
		foreach (KeyValuePair<long, string> keyValuePair in this.GetPermittedPlayers())
		{
			if (keyValuePair.Key == playerID)
			{
				return true;
			}
		}
		return false;
	}

	private void AddPermitted(long playerID, string playerName)
	{
		List<KeyValuePair<long, string>> permittedPlayers = this.GetPermittedPlayers();
		foreach (KeyValuePair<long, string> keyValuePair in permittedPlayers)
		{
			if (keyValuePair.Key == playerID)
			{
				return;
			}
		}
		permittedPlayers.Add(new KeyValuePair<long, string>(playerID, playerName));
		this.SetPermittedPlayers(permittedPlayers);
		this.m_addPermittedEffect.Create(base.transform.position, base.transform.rotation, null, 1f);
	}

	private void SetPermittedPlayers(List<KeyValuePair<long, string>> users)
	{
		this.m_nview.GetZDO().Set("permitted", users.Count);
		for (int i = 0; i < users.Count; i++)
		{
			KeyValuePair<long, string> keyValuePair = users[i];
			this.m_nview.GetZDO().Set("pu_id" + i, keyValuePair.Key);
			this.m_nview.GetZDO().Set("pu_name" + i, keyValuePair.Value);
		}
	}

	private List<KeyValuePair<long, string>> GetPermittedPlayers()
	{
		List<KeyValuePair<long, string>> list = new List<KeyValuePair<long, string>>();
		int @int = this.m_nview.GetZDO().GetInt("permitted", 0);
		for (int i = 0; i < @int; i++)
		{
			long @long = this.m_nview.GetZDO().GetLong("pu_id" + i, 0L);
			string @string = this.m_nview.GetZDO().GetString("pu_name" + i, "");
			if (@long != 0L)
			{
				list.Add(new KeyValuePair<long, string>(@long, @string));
			}
		}
		return list;
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid human, bool hold)
	{
		if (hold)
		{
			return false;
		}
		Player player = human as Player;
		if (this.m_piece.IsCreator())
		{
			this.m_nview.InvokeRPC("ToggleEnabled", new object[]
			{
				player.GetPlayerID()
			});
			return true;
		}
		if (this.IsEnabled())
		{
			return false;
		}
		this.m_nview.InvokeRPC("TogglePermitted", new object[]
		{
			player.GetPlayerID(),
			player.GetPlayerName()
		});
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void RPC_TogglePermitted(long uid, long playerID, string name)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.IsEnabled())
		{
			return;
		}
		if (this.IsPermitted(playerID))
		{
			this.RemovePermitted(playerID);
			return;
		}
		this.AddPermitted(playerID, name);
	}

	private void RPC_ToggleEnabled(long uid, long playerID)
	{
		ZLog.Log(string.Concat(new object[]
		{
			"Toggle enabled from ",
			playerID,
			"  creator is ",
			this.m_piece.GetCreator()
		}));
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_piece.GetCreator() != playerID)
		{
			return;
		}
		this.SetEnabled(!this.IsEnabled());
	}

	public bool IsEnabled()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool("enabled", false);
	}

	private void SetEnabled(bool enabled)
	{
		this.m_nview.GetZDO().Set("enabled", enabled);
		this.UpdateStatus();
		if (enabled)
		{
			this.m_activateEffect.Create(base.transform.position, base.transform.rotation, null, 1f);
			return;
		}
		this.m_deactivateEffect.Create(base.transform.position, base.transform.rotation, null, 1f);
	}

	public void Setup(string name)
	{
		this.m_nview.GetZDO().Set("creatorName", name);
	}

	public void PokeAllAreasInRange()
	{
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			if (!(privateArea == this) && this.IsInside(privateArea.transform.position, 0f))
			{
				privateArea.StartInRangeEffect();
			}
		}
	}

	private void StartInRangeEffect()
	{
		this.m_inRangeEffect.SetActive(true);
		base.CancelInvoke("StopInRangeEffect");
		base.Invoke("StopInRangeEffect", 0.2f);
	}

	private void StopInRangeEffect()
	{
		this.m_inRangeEffect.SetActive(false);
	}

	public void PokeConnectionEffects()
	{
		List<PrivateArea> connectedAreas = this.GetConnectedAreas(false);
		this.StartConnectionEffects();
		foreach (PrivateArea privateArea in connectedAreas)
		{
			privateArea.StartConnectionEffects();
		}
	}

	private void StartConnectionEffects()
	{
		List<PrivateArea> list = new List<PrivateArea>();
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			if (!(privateArea == this) && this.IsInside(privateArea.transform.position, 0f))
			{
				list.Add(privateArea);
			}
		}
		Vector3 vector = base.transform.position + Vector3.up * 1.4f;
		if (this.m_connectionInstances.Count != list.Count)
		{
			this.StopConnectionEffects();
			for (int i = 0; i < list.Count; i++)
			{
				GameObject item = UnityEngine.Object.Instantiate<GameObject>(this.m_connectEffect, vector, Quaternion.identity, base.transform);
				this.m_connectionInstances.Add(item);
			}
		}
		if (this.m_connectionInstances.Count == 0)
		{
			return;
		}
		for (int j = 0; j < list.Count; j++)
		{
			Vector3 vector2 = list[j].transform.position + Vector3.up * 1.4f - vector;
			Quaternion rotation = Quaternion.LookRotation(vector2.normalized);
			GameObject gameObject = this.m_connectionInstances[j];
			gameObject.transform.position = vector;
			gameObject.transform.rotation = rotation;
			gameObject.transform.localScale = new Vector3(1f, 1f, vector2.magnitude);
		}
		base.CancelInvoke("StopConnectionEffects");
		base.Invoke("StopConnectionEffects", 0.3f);
	}

	private void StopConnectionEffects()
	{
		foreach (GameObject obj in this.m_connectionInstances)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_connectionInstances.Clear();
	}

	private string GetCreatorName()
	{
		return this.m_nview.GetZDO().GetString("creatorName", "");
	}

	public static bool CheckInPrivateArea(Vector3 point, bool flash = false)
	{
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			if (privateArea.IsEnabled() && privateArea.IsInside(point, 0f))
			{
				if (flash)
				{
					privateArea.FlashShield(false);
				}
				return true;
			}
		}
		return false;
	}

	public static bool CheckAccess(Vector3 point, float radius = 0f, bool flash = true)
	{
		bool flag = false;
		List<PrivateArea> list = new List<PrivateArea>();
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			if (privateArea.IsEnabled() && privateArea.IsInside(point, radius))
			{
				if (privateArea.HaveLocalAccess())
				{
					flag = true;
				}
				else
				{
					list.Add(privateArea);
				}
			}
		}
		if (!flag && list.Count > 0)
		{
			if (flash)
			{
				foreach (PrivateArea privateArea2 in list)
				{
					privateArea2.FlashShield(false);
				}
			}
			return false;
		}
		return true;
	}

	private bool HaveLocalAccess()
	{
		return this.m_piece.IsCreator() || this.IsPermitted(Player.m_localPlayer.GetPlayerID());
	}

	private List<PrivateArea> GetConnectedAreas(bool forceUpdate = false)
	{
		if (Time.time - this.m_connectionUpdateTime > this.m_updateConnectionsInterval || forceUpdate)
		{
			this.GetAllConnectedAreas(this.m_connectedAreas);
			this.m_connectionUpdateTime = Time.time;
		}
		return this.m_connectedAreas;
	}

	private void GetAllConnectedAreas(List<PrivateArea> areas)
	{
		Queue<PrivateArea> queue = new Queue<PrivateArea>();
		queue.Enqueue(this);
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			privateArea.m_tempChecked = false;
		}
		this.m_tempChecked = true;
		while (queue.Count > 0)
		{
			PrivateArea privateArea2 = queue.Dequeue();
			foreach (PrivateArea privateArea3 in PrivateArea.m_allAreas)
			{
				if (!privateArea3.m_tempChecked && privateArea3.IsEnabled() && privateArea3.IsInside(privateArea2.transform.position, 0f))
				{
					privateArea3.m_tempChecked = true;
					queue.Enqueue(privateArea3);
					areas.Add(privateArea3);
				}
			}
		}
	}

	private void FlashShield(bool flashConnected)
	{
		if (!this.m_flashAvailable)
		{
			return;
		}
		this.m_flashAvailable = false;
		this.m_nview.InvokeRPC(ZNetView.Everybody, "FlashShield", Array.Empty<object>());
		if (flashConnected)
		{
			foreach (PrivateArea privateArea in this.GetConnectedAreas(false))
			{
				if (privateArea.m_nview.IsValid())
				{
					privateArea.m_nview.InvokeRPC(ZNetView.Everybody, "FlashShield", Array.Empty<object>());
				}
			}
		}
	}

	private void RPC_FlashShield(long uid)
	{
		this.m_flashEffect.Create(base.transform.position, Quaternion.identity, null, 1f);
	}

	private bool IsInside(Vector3 point, float radius)
	{
		return Utils.DistanceXZ(base.transform.position, point) < this.m_radius + radius;
	}

	public void ShowAreaMarker()
	{
		if (this.m_areaMarker)
		{
			this.m_areaMarker.gameObject.SetActive(true);
			base.CancelInvoke("HideMarker");
			base.Invoke("HideMarker", 0.5f);
		}
	}

	private void HideMarker()
	{
		this.m_areaMarker.gameObject.SetActive(false);
	}

	private void OnDamaged()
	{
		if (this.IsEnabled())
		{
			this.FlashShield(false);
		}
	}

	private void OnDrawGizmosSelected()
	{
	}

	public string m_name = "Guard stone";

	public float m_radius = 10f;

	public float m_updateConnectionsInterval = 5f;

	public GameObject m_enabledEffect;

	public CircleProjector m_areaMarker;

	public EffectList m_flashEffect = new EffectList();

	public EffectList m_activateEffect = new EffectList();

	public EffectList m_deactivateEffect = new EffectList();

	public EffectList m_addPermittedEffect = new EffectList();

	public EffectList m_removedPermittedEffect = new EffectList();

	public GameObject m_connectEffect;

	public GameObject m_inRangeEffect;

	public MeshRenderer m_model;

	private ZNetView m_nview;

	private Piece m_piece;

	private bool m_flashAvailable = true;

	private bool m_tempChecked;

	private List<GameObject> m_connectionInstances = new List<GameObject>();

	private float m_connectionUpdateTime = -1000f;

	private List<PrivateArea> m_connectedAreas = new List<PrivateArea>();

	private static List<PrivateArea> m_allAreas = new List<PrivateArea>();
}
