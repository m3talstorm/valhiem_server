using System;
using System.Collections.Generic;
using UnityEngine;

public class ZSyncAnimation : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_animator = base.GetComponentInChildren<Animator>();
		this.m_animator.logWarnings = false;
		this.m_nview.Register<string>("SetTrigger", new Action<long, string>(this.RPC_SetTrigger));
		this.m_boolHashes = new int[this.m_syncBools.Count];
		this.m_boolDefaults = new bool[this.m_syncBools.Count];
		for (int i = 0; i < this.m_syncBools.Count; i++)
		{
			this.m_boolHashes[i] = ZSyncAnimation.GetHash(this.m_syncBools[i]);
			this.m_boolDefaults[i] = this.m_animator.GetBool(this.m_boolHashes[i]);
		}
		this.m_floatHashes = new int[this.m_syncFloats.Count];
		this.m_floatDefaults = new float[this.m_syncFloats.Count];
		for (int j = 0; j < this.m_syncFloats.Count; j++)
		{
			this.m_floatHashes[j] = ZSyncAnimation.GetHash(this.m_syncFloats[j]);
			this.m_floatDefaults[j] = this.m_animator.GetFloat(this.m_floatHashes[j]);
		}
		this.m_intHashes = new int[this.m_syncInts.Count];
		this.m_intDefaults = new int[this.m_syncInts.Count];
		for (int k = 0; k < this.m_syncInts.Count; k++)
		{
			this.m_intHashes[k] = ZSyncAnimation.GetHash(this.m_syncInts[k]);
			this.m_intDefaults[k] = this.m_animator.GetInteger(this.m_intHashes[k]);
		}
		if (ZSyncAnimation.m_forwardSpeedID == 0)
		{
			ZSyncAnimation.m_forwardSpeedID = ZSyncAnimation.GetHash("forward_speed");
			ZSyncAnimation.m_sidewaySpeedID = ZSyncAnimation.GetHash("sideway_speed");
			ZSyncAnimation.m_animSpeedID = ZSyncAnimation.GetHash("anim_speed");
		}
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		this.SyncParameters();
	}

	public static int GetHash(string name)
	{
		return Animator.StringToHash(name);
	}

	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.SyncParameters();
	}

	private void SyncParameters()
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (!this.m_nview.IsOwner())
		{
			for (int i = 0; i < this.m_boolHashes.Length; i++)
			{
				int num = this.m_boolHashes[i];
				bool @bool = zdo.GetBool(438569 + num, this.m_boolDefaults[i]);
				this.m_animator.SetBool(num, @bool);
			}
			for (int j = 0; j < this.m_floatHashes.Length; j++)
			{
				int num2 = this.m_floatHashes[j];
				float @float = zdo.GetFloat(438569 + num2, this.m_floatDefaults[j]);
				if (this.m_smoothCharacterSpeeds && (num2 == ZSyncAnimation.m_forwardSpeedID || num2 == ZSyncAnimation.m_sidewaySpeedID))
				{
					this.m_animator.SetFloat(num2, @float, 0.2f, Time.fixedDeltaTime);
				}
				else
				{
					this.m_animator.SetFloat(num2, @float);
				}
			}
			for (int k = 0; k < this.m_intHashes.Length; k++)
			{
				int num3 = this.m_intHashes[k];
				int @int = zdo.GetInt(438569 + num3, this.m_intDefaults[k]);
				this.m_animator.SetInteger(num3, @int);
			}
			float float2 = zdo.GetFloat(ZSyncAnimation.m_animSpeedID, 1f);
			this.m_animator.speed = float2;
			return;
		}
		zdo.Set(ZSyncAnimation.m_animSpeedID, this.m_animator.speed);
	}

	public void SetTrigger(string name)
	{
		this.m_nview.InvokeRPC(ZNetView.Everybody, "SetTrigger", new object[]
		{
			name
		});
	}

	public void SetBool(string name, bool value)
	{
		int hash = ZSyncAnimation.GetHash(name);
		this.SetBool(hash, value);
	}

	public void SetBool(int hash, bool value)
	{
		if (this.m_animator.GetBool(hash) == value)
		{
			return;
		}
		this.m_animator.SetBool(hash, value);
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(438569 + hash, value);
		}
	}

	public void SetFloat(string name, float value)
	{
		int hash = ZSyncAnimation.GetHash(name);
		this.SetFloat(hash, value);
	}

	public void SetFloat(int hash, float value)
	{
		if (Mathf.Abs(this.m_animator.GetFloat(hash) - value) < 0.01f)
		{
			return;
		}
		if (this.m_smoothCharacterSpeeds && (hash == ZSyncAnimation.m_forwardSpeedID || hash == ZSyncAnimation.m_sidewaySpeedID))
		{
			this.m_animator.SetFloat(hash, value, 0.2f, Time.fixedDeltaTime);
		}
		else
		{
			this.m_animator.SetFloat(hash, value);
		}
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(438569 + hash, value);
		}
	}

	public void SetInt(string name, int value)
	{
		int hash = ZSyncAnimation.GetHash(name);
		this.SetInt(hash, value);
	}

	public void SetInt(int hash, int value)
	{
		if (this.m_animator.GetInteger(hash) == value)
		{
			return;
		}
		this.m_animator.SetInteger(hash, value);
		if (this.m_nview.GetZDO() != null && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(438569 + hash, value);
		}
	}

	private void RPC_SetTrigger(long sender, string name)
	{
		this.m_animator.SetTrigger(name);
	}

	public void SetSpeed(float speed)
	{
		this.m_animator.speed = speed;
	}

	public bool IsOwner()
	{
		return this.m_nview.IsValid() && this.m_nview.IsOwner();
	}

	private ZNetView m_nview;

	private Animator m_animator;

	public List<string> m_syncBools = new List<string>();

	public List<string> m_syncFloats = new List<string>();

	public List<string> m_syncInts = new List<string>();

	public bool m_smoothCharacterSpeeds = true;

	private static int m_forwardSpeedID;

	private static int m_sidewaySpeedID;

	private static int m_animSpeedID;

	private int[] m_boolHashes;

	private bool[] m_boolDefaults;

	private int[] m_floatHashes;

	private float[] m_floatDefaults;

	private int[] m_intHashes;

	private int[] m_intDefaults;

	private const int m_zdoSalt = 438569;
}
