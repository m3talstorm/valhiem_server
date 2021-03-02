using System;
using System.Collections.Generic;
using UnityEngine;

public class ZSyncTransform : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_projectile = base.GetComponent<Projectile>();
		this.m_character = base.GetComponent<Character>();
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		if (this.m_body)
		{
			this.m_isKinematicBody = this.m_body.isKinematic;
			this.m_useGravity = this.m_body.useGravity;
		}
	}

	private Vector3 GetVelocity()
	{
		if (this.m_body != null)
		{
			return this.m_body.velocity;
		}
		if (this.m_projectile != null)
		{
			return this.m_projectile.GetVelocity();
		}
		return Vector3.zero;
	}

	private Vector3 GetPosition()
	{
		if (!this.m_body)
		{
			return base.transform.position;
		}
		return this.m_body.position;
	}

	private void OwnerSync()
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (!zdo.IsOwner())
		{
			return;
		}
		if (base.transform.position.y < -5000f)
		{
			if (this.m_body)
			{
				this.m_body.velocity = Vector3.zero;
			}
			ZLog.Log("Object fell out of world:" + base.gameObject.name);
			float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
			Vector3 position = base.transform.position;
			position.y = groundHeight + 1f;
			base.transform.position = position;
			return;
		}
		if (this.m_syncPosition)
		{
			zdo.SetPosition(this.GetPosition());
			zdo.Set(ZSyncTransform.m_velHash, this.GetVelocity());
			if (this.m_characterParentSync)
			{
				ZDOID id;
				Vector3 value;
				Vector3 value2;
				if (this.m_character.GetRelativePosition(out id, out value, out value2))
				{
					zdo.Set(ZSyncTransform.m_parentIDHash, id);
					zdo.Set(ZSyncTransform.m_relPos, value);
					zdo.Set(ZSyncTransform.m_velHash, value2);
				}
				else
				{
					zdo.Set(ZSyncTransform.m_parentIDHash, ZDOID.None);
				}
			}
		}
		if (this.m_syncRotation && base.transform.hasChanged)
		{
			Quaternion rotation = this.m_body ? this.m_body.rotation : base.transform.rotation;
			zdo.SetRotation(rotation);
		}
		if (this.m_syncScale && base.transform.hasChanged)
		{
			zdo.Set(ZSyncTransform.m_scaleHash, base.transform.localScale);
		}
		if (this.m_body)
		{
			if (this.m_syncBodyVelocity)
			{
				this.m_nview.GetZDO().Set(ZSyncTransform.m_bodyVel, this.m_body.velocity);
				this.m_nview.GetZDO().Set(ZSyncTransform.m_bodyAVel, this.m_body.angularVelocity);
			}
			this.m_body.useGravity = this.m_useGravity;
		}
		base.transform.hasChanged = false;
	}

	private void SyncPosition(ZDO zdo, float dt)
	{
		if (this.m_characterParentSync && zdo.HasOwner())
		{
			ZDOID zdoid = zdo.GetZDOID(ZSyncTransform.m_parentIDHash);
			if (!zdoid.IsNone())
			{
				GameObject gameObject = ZNetScene.instance.FindInstance(zdoid);
				if (gameObject)
				{
					ZSyncTransform component = gameObject.GetComponent<ZSyncTransform>();
					if (component)
					{
						component.ClientSync(dt);
					}
					Vector3 vector = zdo.GetVec3(ZSyncTransform.m_relPos, Vector3.zero);
					Vector3 vec = zdo.GetVec3(ZSyncTransform.m_velHash, Vector3.zero);
					if (zdo.m_dataRevision != this.m_posRevision)
					{
						this.m_posRevision = zdo.m_dataRevision;
						this.m_targetPosTimer = 0f;
					}
					this.m_targetPosTimer += dt;
					this.m_targetPosTimer = Mathf.Min(this.m_targetPosTimer, 2f);
					vector += vec * this.m_targetPosTimer;
					if (!this.m_haveTempRelPos)
					{
						this.m_haveTempRelPos = true;
						this.m_tempRelPos = vector;
					}
					if (Vector3.Distance(this.m_tempRelPos, vector) > 0.001f)
					{
						this.m_tempRelPos = Vector3.Lerp(this.m_tempRelPos, vector, 0.2f);
						vector = this.m_tempRelPos;
					}
					Vector3 vector2 = gameObject.transform.TransformPoint(vector);
					if (Vector3.Distance(base.transform.position, vector2) > 0.001f)
					{
						base.transform.position = vector2;
					}
					return;
				}
			}
		}
		this.m_haveTempRelPos = false;
		Vector3 vector3 = zdo.GetPosition();
		if (zdo.m_dataRevision != this.m_posRevision)
		{
			this.m_posRevision = zdo.m_dataRevision;
			this.m_targetPosTimer = 0f;
		}
		if (zdo.HasOwner())
		{
			this.m_targetPosTimer += dt;
			this.m_targetPosTimer = Mathf.Min(this.m_targetPosTimer, 2f);
			Vector3 vec2 = zdo.GetVec3(ZSyncTransform.m_velHash, Vector3.zero);
			vector3 += vec2 * this.m_targetPosTimer;
		}
		float num = Vector3.Distance(base.transform.position, vector3);
		if (num > 0.001f)
		{
			base.transform.position = ((num < 5f) ? Vector3.Lerp(base.transform.position, vector3, 0.2f) : vector3);
		}
	}

	private void ClientSync(float dt)
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo.IsOwner())
		{
			return;
		}
		int frameCount = Time.frameCount;
		if (this.m_lastUpdateFrame == frameCount)
		{
			return;
		}
		this.m_lastUpdateFrame = frameCount;
		if (this.m_isKinematicBody)
		{
			if (this.m_syncPosition)
			{
				Vector3 vector = zdo.GetPosition();
				if (Vector3.Distance(this.m_body.position, vector) > 5f)
				{
					this.m_body.position = vector;
				}
				else
				{
					if (Vector3.Distance(this.m_body.position, vector) > 0.01f)
					{
						vector = Vector3.Lerp(this.m_body.position, vector, 0.2f);
					}
					this.m_body.MovePosition(vector);
				}
			}
			if (this.m_syncRotation)
			{
				Quaternion rotation = zdo.GetRotation();
				if (Quaternion.Angle(this.m_body.rotation, rotation) > 45f)
				{
					this.m_body.rotation = rotation;
				}
				else
				{
					this.m_body.MoveRotation(rotation);
				}
			}
		}
		else
		{
			if (this.m_syncPosition)
			{
				this.SyncPosition(zdo, dt);
			}
			if (this.m_syncRotation)
			{
				Quaternion rotation2 = zdo.GetRotation();
				if (Quaternion.Angle(base.transform.rotation, rotation2) > 0.001f)
				{
					base.transform.rotation = Quaternion.Slerp(base.transform.rotation, rotation2, 0.2f);
				}
			}
			if (this.m_body)
			{
				this.m_body.useGravity = false;
				if (this.m_syncBodyVelocity && this.m_nview.HasOwner())
				{
					Vector3 vec = zdo.GetVec3(ZSyncTransform.m_bodyVel, Vector3.zero);
					Vector3 vec2 = zdo.GetVec3(ZSyncTransform.m_bodyAVel, Vector3.zero);
					if (vec.magnitude > 0.01f || vec2.magnitude > 0.01f)
					{
						this.m_body.velocity = vec;
						this.m_body.angularVelocity = vec2;
					}
					else
					{
						this.m_body.Sleep();
					}
				}
				else if (!this.m_body.IsSleeping())
				{
					this.m_body.velocity = Vector3.zero;
					this.m_body.angularVelocity = Vector3.zero;
					this.m_body.Sleep();
				}
			}
		}
		if (this.m_syncScale)
		{
			Vector3 vec3 = zdo.GetVec3(ZSyncTransform.m_scaleHash, base.transform.localScale);
			base.transform.localScale = vec3;
		}
	}

	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.ClientSync(Time.fixedDeltaTime);
	}

	private void LateUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.OwnerSync();
	}

	public void SyncNow()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.OwnerSync();
	}

	public bool m_syncPosition = true;

	public bool m_syncRotation = true;

	public bool m_syncScale;

	public bool m_syncBodyVelocity;

	public bool m_characterParentSync;

	private const float m_smoothness = 0.2f;

	private bool m_isKinematicBody;

	private bool m_useGravity = true;

	private Vector3 m_tempRelPos;

	private bool m_haveTempRelPos;

	private float m_targetPosTimer;

	private uint m_posRevision;

	private int m_lastUpdateFrame = -1;

	private static int m_velHash = "vel".GetStableHashCode();

	private static int m_scaleHash = "scale".GetStableHashCode();

	private static int m_bodyVel = "body_vel".GetStableHashCode();

	private static int m_bodyAVel = "body_avel".GetStableHashCode();

	private static int m_relPos = "relPos".GetStableHashCode();

	private static KeyValuePair<int, int> m_parentIDHash = ZDO.GetHashZDOID("parentID");

	private ZNetView m_nview;

	private Rigidbody m_body;

	private Projectile m_projectile;

	private Character m_character;
}
