using System;
using System.Collections.Generic;
using UnityEngine;

public class FishingFloat : MonoBehaviour, IProjectile
{
	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_floating = base.GetComponent<Floating>();
		this.m_nview.Register<ZDOID>("Nibble", new Action<long, ZDOID>(this.RPC_Nibble));
	}

	private void OnDestroy()
	{
		FishingFloat.m_allInstances.Remove(this);
	}

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item)
	{
		FishingFloat fishingFloat = FishingFloat.FindFloat(owner);
		if (fishingFloat)
		{
			ZNetScene.instance.Destroy(fishingFloat.gameObject);
		}
		ZDOID zdoid = owner.GetZDOID();
		this.m_nview.GetZDO().Set("RodOwner", zdoid);
		FishingFloat.m_allInstances.Add(this);
		Transform rodTop = this.GetRodTop(owner);
		if (rodTop == null)
		{
			ZLog.LogWarning("Failed to find fishing rod top");
			return;
		}
		this.m_rodLine.SetPeer(owner.GetZDOID());
		this.m_lineLength = Vector3.Distance(rodTop.position, base.transform.position);
		owner.Message(MessageHud.MessageType.Center, this.m_lineLength.ToString("0m"), 0, null);
	}

	public Character GetOwner()
	{
		if (!this.m_nview.IsValid())
		{
			return null;
		}
		ZDOID zdoid = this.m_nview.GetZDO().GetZDOID("RodOwner");
		GameObject gameObject = ZNetScene.instance.FindInstance(zdoid);
		if (gameObject == null)
		{
			return null;
		}
		return gameObject.GetComponent<Character>();
	}

	private Transform GetRodTop(Character owner)
	{
		Transform transform = Utils.FindChild(owner.transform, "_RodTop");
		if (transform == null)
		{
			ZLog.LogWarning("Failed to find fishing rod top");
			return null;
		}
		return transform;
	}

	private void FixedUpdate()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		Character owner = this.GetOwner();
		if (!owner)
		{
			ZLog.LogWarning("Fishing rod not found, destroying fishing float");
			this.m_nview.Destroy();
			return;
		}
		Transform rodTop = this.GetRodTop(owner);
		if (!rodTop)
		{
			ZLog.LogWarning("Fishing rod not found, destroying fishing float");
			this.m_nview.Destroy();
			return;
		}
		if (owner.InAttack() || owner.IsHoldingAttack())
		{
			this.m_nview.Destroy();
			return;
		}
		float magnitude = (rodTop.transform.position - base.transform.position).magnitude;
		Fish fish = this.GetCatch();
		if (!owner.HaveStamina(0f) && fish != null)
		{
			this.SetCatch(null);
			fish = null;
			this.Message("$msg_fishing_lost", true);
		}
		if (fish)
		{
			owner.UseStamina(this.m_hookedStaminaPerSec * fixedDeltaTime);
		}
		if (!fish && Utils.LengthXZ(this.m_body.velocity) > 2f)
		{
			this.TryToHook();
		}
		if (owner.IsBlocking() && owner.HaveStamina(0f))
		{
			float num = this.m_pullStaminaUse;
			if (fish != null)
			{
				num += fish.m_staminaUse;
			}
			owner.UseStamina(num * fixedDeltaTime);
			if (this.m_lineLength > magnitude - 0.2f)
			{
				float lineLength = this.m_lineLength;
				this.m_lineLength -= fixedDeltaTime * this.m_pullLineSpeed;
				this.TryToHook();
				if ((int)this.m_lineLength != (int)lineLength)
				{
					this.Message(this.m_lineLength.ToString("0m"), false);
				}
			}
			if (this.m_lineLength <= 0.5f)
			{
				if (fish)
				{
					if (fish.Pickup(owner as Humanoid))
					{
						this.Message("$msg_fishing_catched " + fish.GetHoverName(), true);
						this.SetCatch(null);
					}
					return;
				}
				this.m_nview.Destroy();
				return;
			}
		}
		this.m_rodLine.m_slack = (1f - Utils.LerpStep(this.m_lineLength / 2f, this.m_lineLength, magnitude)) * this.m_maxLineSlack;
		if (magnitude - this.m_lineLength > this.m_breakDistance || magnitude > this.m_maxDistance)
		{
			this.Message("$msg_fishing_linebroke", true);
			this.m_nview.Destroy();
			this.m_lineBreakEffect.Create(base.transform.position, Quaternion.identity, null, 1f);
			return;
		}
		if (fish)
		{
			Utils.Pull(this.m_body, fish.transform.position, 0.5f, this.m_moveForce, 0.5f, 0.3f);
		}
		Utils.Pull(this.m_body, rodTop.transform.position, this.m_lineLength, this.m_moveForce, 1f, 0.3f);
	}

	private void TryToHook()
	{
		if (this.m_nibbler != null && Time.time - this.m_nibbleTime < 0.5f && this.GetCatch() == null)
		{
			this.Message("$msg_fishing_hooked", true);
			this.SetCatch(this.m_nibbler);
			this.m_nibbler = null;
		}
	}

	private void SetCatch(Fish fish)
	{
		if (fish)
		{
			this.m_nview.GetZDO().Set("CatchID", fish.GetZDOID());
			this.m_hookLine.SetPeer(fish.GetZDOID());
			return;
		}
		this.m_nview.GetZDO().Set("CatchID", ZDOID.None);
		this.m_hookLine.SetPeer(ZDOID.None);
	}

	public Fish GetCatch()
	{
		if (!this.m_nview.IsValid())
		{
			return null;
		}
		ZDOID zdoid = this.m_nview.GetZDO().GetZDOID("CatchID");
		if (!zdoid.IsNone())
		{
			GameObject gameObject = ZNetScene.instance.FindInstance(zdoid);
			if (gameObject)
			{
				return gameObject.GetComponent<Fish>();
			}
		}
		return null;
	}

	public bool IsInWater()
	{
		return this.m_floating.IsInWater();
	}

	public void Nibble(Fish fish)
	{
		this.m_nview.InvokeRPC("Nibble", new object[]
		{
			fish.GetZDOID()
		});
	}

	public void RPC_Nibble(long sender, ZDOID fishID)
	{
		if (Time.time - this.m_nibbleTime < 1f)
		{
			return;
		}
		if (this.GetCatch() != null)
		{
			return;
		}
		this.m_nibbleEffect.Create(base.transform.position, Quaternion.identity, base.transform, 1f);
		this.m_body.AddForce(Vector3.down * this.m_nibbleForce, ForceMode.VelocityChange);
		GameObject gameObject = ZNetScene.instance.FindInstance(fishID);
		if (gameObject)
		{
			this.m_nibbler = gameObject.GetComponent<Fish>();
			this.m_nibbleTime = Time.time;
		}
	}

	public static List<FishingFloat> GetAllInstances()
	{
		return FishingFloat.m_allInstances;
	}

	private static FishingFloat FindFloat(Character owner)
	{
		foreach (FishingFloat fishingFloat in FishingFloat.m_allInstances)
		{
			if (owner == fishingFloat.GetOwner())
			{
				return fishingFloat;
			}
		}
		return null;
	}

	public static FishingFloat FindFloat(Fish fish)
	{
		foreach (FishingFloat fishingFloat in FishingFloat.m_allInstances)
		{
			if (fishingFloat.GetCatch() == fish)
			{
				return fishingFloat;
			}
		}
		return null;
	}

	private void Message(string msg, bool prioritized = false)
	{
		if (!prioritized && Time.time - this.m_msgTime < 1f)
		{
			return;
		}
		this.m_msgTime = Time.time;
		Character owner = this.GetOwner();
		if (owner)
		{
			owner.Message(MessageHud.MessageType.Center, Localization.instance.Localize(msg), 0, null);
		}
	}

	public float m_maxDistance = 30f;

	public float m_moveForce = 10f;

	public float m_pullLineSpeed = 1f;

	public float m_pullStaminaUse = 10f;

	public float m_hookedStaminaPerSec = 1f;

	public float m_breakDistance = 4f;

	public float m_range = 10f;

	public float m_nibbleForce = 10f;

	public EffectList m_nibbleEffect = new EffectList();

	public EffectList m_lineBreakEffect = new EffectList();

	public float m_maxLineSlack = 0.3f;

	public LineConnect m_rodLine;

	public LineConnect m_hookLine;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private Floating m_floating;

	private float m_lineLength;

	private float m_msgTime;

	private Fish m_nibbler;

	private float m_nibbleTime;

	private static List<FishingFloat> m_allInstances = new List<FishingFloat>();
}
