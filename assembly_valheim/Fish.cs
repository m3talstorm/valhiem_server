using System;
using UnityEngine;

public class Fish : MonoBehaviour, IWaterInteractable, Hoverable, Interactable
{
	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_spawnPoint = this.m_nview.GetZDO().GetVec3("spawnpoint", base.transform.position);
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set("spawnpoint", this.m_spawnPoint);
		}
		if (this.m_nview.IsOwner())
		{
			this.RandomizeWaypoint(true);
		}
		if (this.m_nview && this.m_nview.IsValid())
		{
			this.m_nview.Register("RequestPickup", new Action<long>(this.RPC_RequestPickup));
			this.m_nview.Register("Pickup", new Action<long>(this.RPC_Pickup));
		}
	}

	public bool IsOwner()
	{
		return this.m_nview && this.m_nview.IsValid() && this.m_nview.IsOwner();
	}

	public string GetHoverText()
	{
		string text = this.m_name;
		if (this.IsOutOfWater())
		{
			text += "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup";
		}
		return Localization.instance.Localize(text);
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid character, bool repeat)
	{
		return !repeat && this.IsOutOfWater() && this.Pickup(character);
	}

	public bool Pickup(Humanoid character)
	{
		if (!character.GetInventory().CanAddItem(this.m_pickupItem, this.m_pickupItemStackSize))
		{
			character.Message(MessageHud.MessageType.Center, "$msg_noroom", 0, null);
			return false;
		}
		this.m_nview.InvokeRPC("RequestPickup", Array.Empty<object>());
		return true;
	}

	private void RPC_RequestPickup(long uid)
	{
		if (Time.time - this.m_pickupTime > 2f)
		{
			this.m_pickupTime = Time.time;
			this.m_nview.InvokeRPC(uid, "Pickup", Array.Empty<object>());
		}
	}

	private void RPC_Pickup(long uid)
	{
		if (Player.m_localPlayer && Player.m_localPlayer.PickupPrefab(this.m_pickupItem, this.m_pickupItemStackSize) != null)
		{
			this.m_nview.ClaimOwnership();
			this.m_nview.Destroy();
		}
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void SetInWater(float waterLevel)
	{
		this.m_inWater = waterLevel;
	}

	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	private bool IsOutOfWater()
	{
		return this.m_inWater < base.transform.position.y - this.m_height;
	}

	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		FishingFloat fishingFloat = FishingFloat.FindFloat(this);
		if (fishingFloat)
		{
			Utils.Pull(this.m_body, fishingFloat.transform.position, 1f, this.m_hookForce, 1f, 0.5f);
		}
		if (this.m_inWater <= -10000f || this.m_inWater < base.transform.position.y + this.m_height)
		{
			this.m_body.useGravity = true;
			if (this.IsOutOfWater())
			{
				return;
			}
		}
		this.m_body.useGravity = false;
		bool flag = false;
		Player playerNoiseRange = Player.GetPlayerNoiseRange(base.transform.position, 1f);
		if (playerNoiseRange)
		{
			if (Vector3.Distance(base.transform.position, playerNoiseRange.transform.position) > this.m_avoidRange / 2f)
			{
				Vector3 normalized = (base.transform.position - playerNoiseRange.transform.position).normalized;
				this.SwimDirection(normalized, true, true, fixedDeltaTime);
				return;
			}
			flag = true;
			if (this.m_swimTimer > 0.5f)
			{
				this.m_swimTimer = 0.5f;
			}
		}
		this.m_swimTimer -= fixedDeltaTime;
		if (this.m_swimTimer <= 0f)
		{
			this.RandomizeWaypoint(!flag);
		}
		if (this.m_haveWaypoint)
		{
			if (this.m_waypointFF)
			{
				this.m_waypoint = this.m_waypointFF.transform.position + Vector3.down;
			}
			if (Vector3.Distance(this.m_waypoint, base.transform.position) < 0.2f)
			{
				if (!this.m_waypointFF)
				{
					this.m_haveWaypoint = false;
					return;
				}
				if (Time.time - this.m_lastNibbleTime > 1f)
				{
					this.m_lastNibbleTime = Time.time;
					this.m_waypointFF.Nibble(this);
				}
			}
			Vector3 dir = Vector3.Normalize(this.m_waypoint - base.transform.position);
			this.SwimDirection(dir, flag, false, fixedDeltaTime);
			return;
		}
		this.Stop(fixedDeltaTime);
	}

	private void Stop(float dt)
	{
		if (this.m_inWater < base.transform.position.y + this.m_height)
		{
			return;
		}
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Quaternion to = Quaternion.LookRotation(forward, Vector3.up);
		Quaternion rot = Quaternion.RotateTowards(this.m_body.rotation, to, this.m_turnRate * dt);
		this.m_body.MoveRotation(rot);
		Vector3 force = -this.m_body.velocity * this.m_acceleration;
		this.m_body.AddForce(force, ForceMode.VelocityChange);
	}

	private void SwimDirection(Vector3 dir, bool fast, bool avoidLand, float dt)
	{
		Vector3 forward = dir;
		forward.y = 0f;
		forward.Normalize();
		float num = this.m_turnRate;
		if (fast)
		{
			num *= this.m_avoidSpeedScale;
		}
		Quaternion to = Quaternion.LookRotation(forward, Vector3.up);
		Quaternion rotation = Quaternion.RotateTowards(base.transform.rotation, to, num * dt);
		this.m_body.rotation = rotation;
		float num2 = this.m_speed;
		if (fast)
		{
			num2 *= this.m_avoidSpeedScale;
		}
		if (avoidLand && this.GetPointDepth(base.transform.position + base.transform.forward) < this.m_minDepth)
		{
			num2 = 0f;
		}
		if (fast && Vector3.Dot(dir, base.transform.forward) < 0f)
		{
			num2 = 0f;
		}
		Vector3 forward2 = base.transform.forward;
		forward2.y = dir.y;
		Vector3 vector = forward2 * num2 - this.m_body.velocity;
		if (this.m_inWater < base.transform.position.y + this.m_height && vector.y > 0f)
		{
			vector.y = 0f;
		}
		this.m_body.AddForce(vector * this.m_acceleration, ForceMode.VelocityChange);
	}

	private FishingFloat FindFloat()
	{
		foreach (FishingFloat fishingFloat in FishingFloat.GetAllInstances())
		{
			if (Vector3.Distance(base.transform.position, fishingFloat.transform.position) <= fishingFloat.m_range && fishingFloat.IsInWater() && !(fishingFloat.GetCatch() != null))
			{
				float baseHookChance = this.m_baseHookChance;
				if (UnityEngine.Random.value < baseHookChance)
				{
					return fishingFloat;
				}
			}
		}
		return null;
	}

	private void RandomizeWaypoint(bool canHook)
	{
		Vector2 vector = UnityEngine.Random.insideUnitCircle * this.m_swimRange;
		this.m_waypoint = this.m_spawnPoint + new Vector3(vector.x, 0f, vector.y);
		this.m_waypointFF = null;
		if (canHook)
		{
			FishingFloat fishingFloat = this.FindFloat();
			if (fishingFloat)
			{
				this.m_waypointFF = fishingFloat;
				this.m_waypoint = fishingFloat.transform.position + Vector3.down;
			}
		}
		float pointDepth = this.GetPointDepth(this.m_waypoint);
		if (pointDepth < this.m_minDepth)
		{
			return;
		}
		Vector3 p = (this.m_waypoint + base.transform.position) * 0.5f;
		if (this.GetPointDepth(p) < this.m_minDepth)
		{
			return;
		}
		float max = Mathf.Min(this.m_maxDepth, pointDepth - this.m_height);
		float waterLevel = WaterVolume.GetWaterLevel(this.m_waypoint, 1f);
		this.m_waypoint.y = waterLevel - UnityEngine.Random.Range(this.m_minDepth, max);
		this.m_haveWaypoint = true;
		this.m_swimTimer = UnityEngine.Random.Range(this.m_wpDurationMin, this.m_wpDurationMax);
	}

	private float GetPointDepth(Vector3 p)
	{
		float num;
		if (ZoneSystem.instance.GetSolidHeight(p, out num))
		{
			return ZoneSystem.instance.m_waterLevel - num;
		}
		return 0f;
	}

	private bool DangerNearby()
	{
		return Player.GetPlayerNoiseRange(base.transform.position, 1f) != null;
	}

	public ZDOID GetZDOID()
	{
		return this.m_nview.GetZDO().m_uid;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * this.m_height, new Vector3(1f, 0.02f, 1f));
	}

	public string m_name = "Fish";

	public float m_swimRange = 20f;

	public float m_minDepth = 1f;

	public float m_maxDepth = 4f;

	public float m_speed = 10f;

	public float m_acceleration = 5f;

	public float m_turnRate = 10f;

	public float m_wpDurationMin = 4f;

	public float m_wpDurationMax = 4f;

	public float m_avoidSpeedScale = 2f;

	public float m_avoidRange = 5f;

	public float m_height = 0.2f;

	public float m_eatDuration = 4f;

	public float m_hookForce = 4f;

	public float m_staminaUse = 1f;

	public float m_baseHookChance = 0.5f;

	public GameObject m_pickupItem;

	public int m_pickupItemStackSize = 1;

	private Vector3 m_spawnPoint;

	private Vector3 m_waypoint;

	private FishingFloat m_waypointFF;

	private bool m_haveWaypoint;

	private float m_swimTimer;

	private float m_lastNibbleTime;

	private float m_inWater = -10000f;

	private float m_pickupTime;

	private ZNetView m_nview;

	private Rigidbody m_body;
}
