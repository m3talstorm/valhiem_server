using System;
using UnityEngine;

public class Tameable : MonoBehaviour, Interactable
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_character = base.GetComponent<Character>();
		this.m_monsterAI = base.GetComponent<MonsterAI>();
		MonsterAI monsterAI = this.m_monsterAI;
		monsterAI.m_onConsumedItem = (Action<ItemDrop>)Delegate.Combine(monsterAI.m_onConsumedItem, new Action<ItemDrop>(this.OnConsumedItem));
		if (this.m_nview.IsValid())
		{
			this.m_nview.Register<ZDOID>("Command", new Action<long, ZDOID>(this.RPC_Command));
			base.InvokeRepeating("TamingUpdate", 3f, 3f);
		}
	}

	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		string text = Localization.instance.Localize(this.m_character.m_name);
		if (this.m_character.IsTamed())
		{
			text += Localization.instance.Localize(" ( $hud_tame, " + this.GetStatusString() + " )");
			return text + Localization.instance.Localize("\n[<color=yellow><b>$KEY_Use</b></color>] $hud_pet");
		}
		int tameness = this.GetTameness();
		if (tameness <= 0)
		{
			text += Localization.instance.Localize(" ( $hud_wild, " + this.GetStatusString() + " )");
		}
		else
		{
			text += Localization.instance.Localize(string.Concat(new object[]
			{
				" ( $hud_tameness  ",
				tameness,
				"%, ",
				this.GetStatusString(),
				" )"
			}));
		}
		return text;
	}

	private string GetStatusString()
	{
		if (this.m_monsterAI.IsAlerted())
		{
			return "$hud_tamefrightened";
		}
		if (this.IsHungry())
		{
			return "$hud_tamehungry";
		}
		if (this.m_character.IsTamed())
		{
			return "$hud_tamehappy";
		}
		return "$hud_tameinprogress";
	}

	public bool Interact(Humanoid user, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		string hoverName = this.m_character.GetHoverName();
		if (!this.m_character.IsTamed())
		{
			return false;
		}
		if (Time.time - this.m_lastPetTime > 1f)
		{
			this.m_lastPetTime = Time.time;
			this.m_petEffect.Create(this.m_character.GetCenterPoint(), Quaternion.identity, null, 1f);
			if (this.m_commandable)
			{
				this.Command(user);
			}
			else
			{
				user.Message(MessageHud.MessageType.Center, hoverName + " $hud_tamelove", 0, null);
			}
			return true;
		}
		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void TamingUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_character.IsTamed())
		{
			return;
		}
		if (this.IsHungry())
		{
			return;
		}
		if (this.m_monsterAI.IsAlerted())
		{
			return;
		}
		this.DecreaseRemainingTime(3f);
		if (this.GetRemainingTime() <= 0f)
		{
			this.Tame();
			return;
		}
		this.m_sootheEffect.Create(this.m_character.GetCenterPoint(), Quaternion.identity, null, 1f);
	}

	public void Tame()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_character.IsTamed())
		{
			return;
		}
		this.m_monsterAI.MakeTame();
		this.m_tamedEffect.Create(this.m_character.GetCenterPoint(), Quaternion.identity, null, 1f);
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 30f);
		if (closestPlayer)
		{
			closestPlayer.Message(MessageHud.MessageType.Center, this.m_character.m_name + " $hud_tamedone", 0, null);
		}
	}

	public static void TameAllInArea(Vector3 point, float radius)
	{
		foreach (Character character in Character.GetAllCharacters())
		{
			if (!character.IsPlayer())
			{
				Tameable component = character.GetComponent<Tameable>();
				if (component)
				{
					component.Tame();
				}
			}
		}
	}

	private void Command(Humanoid user)
	{
		this.m_nview.InvokeRPC("Command", new object[]
		{
			user.GetZDOID()
		});
	}

	private Player GetPlayer(ZDOID characterID)
	{
		GameObject gameObject = ZNetScene.instance.FindInstance(characterID);
		if (gameObject)
		{
			return gameObject.GetComponent<Player>();
		}
		return null;
	}

	private void RPC_Command(long sender, ZDOID characterID)
	{
		Player player = this.GetPlayer(characterID);
		if (player == null)
		{
			return;
		}
		if (this.m_monsterAI.GetFollowTarget())
		{
			this.m_monsterAI.SetFollowTarget(null);
			this.m_monsterAI.SetPatrolPoint();
			player.Message(MessageHud.MessageType.Center, this.m_character.GetHoverName() + " $hud_tamestay", 0, null);
			return;
		}
		this.m_monsterAI.ResetPatrolPoint();
		this.m_monsterAI.SetFollowTarget(player.gameObject);
		player.Message(MessageHud.MessageType.Center, this.m_character.GetHoverName() + " $hud_tamefollow", 0, null);
	}

	public bool IsHungry()
	{
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong("TameLastFeeding", 0L));
		return (ZNet.instance.GetTime() - d).TotalSeconds > (double)this.m_fedDuration;
	}

	private void ResetFeedingTimer()
	{
		this.m_nview.GetZDO().Set("TameLastFeeding", ZNet.instance.GetTime().Ticks);
	}

	private int GetTameness()
	{
		float remainingTime = this.GetRemainingTime();
		return (int)((1f - Mathf.Clamp01(remainingTime / this.m_tamingTime)) * 100f);
	}

	private void OnConsumedItem(ItemDrop item)
	{
		if (this.IsHungry())
		{
			this.m_sootheEffect.Create(this.m_character.GetCenterPoint(), Quaternion.identity, null, 1f);
		}
		this.ResetFeedingTimer();
	}

	private void DecreaseRemainingTime(float time)
	{
		float num = this.GetRemainingTime();
		num -= time;
		if (num < 0f)
		{
			num = 0f;
		}
		this.m_nview.GetZDO().Set("TameTimeLeft", num);
	}

	private float GetRemainingTime()
	{
		return this.m_nview.GetZDO().GetFloat("TameTimeLeft", this.m_tamingTime);
	}

	private const float m_playerMaxDistance = 15f;

	private const float m_tameDeltaTime = 3f;

	public float m_fedDuration = 30f;

	public float m_tamingTime = 1800f;

	public EffectList m_tamedEffect = new EffectList();

	public EffectList m_sootheEffect = new EffectList();

	public EffectList m_petEffect = new EffectList();

	public bool m_commandable;

	private Character m_character;

	private MonsterAI m_monsterAI;

	private ZNetView m_nview;

	private float m_lastPetTime;
}
