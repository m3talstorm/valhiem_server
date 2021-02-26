using System;
using System.Collections.Generic;
using UnityEngine;

public class Vagon : MonoBehaviour, Hoverable, Interactable
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		Vagon.m_instances.Add(this);
		Heightmap.ForceGenerateAll();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_bodies = base.GetComponentsInChildren<Rigidbody>();
		this.m_lineRenderer = base.GetComponent<LineRenderer>();
		Rigidbody[] bodies = this.m_bodies;
		for (int i = 0; i < bodies.Length; i++)
		{
			bodies[i].maxDepenetrationVelocity = 2f;
		}
		this.m_nview.Register("RequestOwn", new Action<long>(this.RPC_RequestOwn));
		this.m_nview.Register("RequestDenied", new Action<long>(this.RPC_RequestDenied));
		base.InvokeRepeating("UpdateMass", 0f, 5f);
		base.InvokeRepeating("UpdateLoadVisualization", 0f, 3f);
	}

	private void OnDestroy()
	{
		Vagon.m_instances.Remove(this);
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] Use");
	}

	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			return false;
		}
		this.m_useRequester = character;
		if (!this.m_nview.IsOwner())
		{
			this.m_nview.InvokeRPC("RequestOwn", Array.Empty<object>());
		}
		return false;
	}

	public void RPC_RequestOwn(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.InUse())
		{
			ZLog.Log("Requested use, but is already in use");
			this.m_nview.InvokeRPC(sender, "RequestDenied", Array.Empty<object>());
			return;
		}
		this.m_nview.GetZDO().SetOwner(sender);
	}

	private void RPC_RequestDenied(long sender)
	{
		ZLog.Log("Got request denied");
		if (this.m_useRequester)
		{
			this.m_useRequester.Message(MessageHud.MessageType.Center, this.m_name + " is in use by someone else", 0, null);
			this.m_useRequester = null;
		}
	}

	private void FixedUpdate()
	{
		this.UpdateAudio(Time.fixedDeltaTime);
		if (this.m_nview.IsOwner())
		{
			if (this.m_useRequester)
			{
				if (this.IsAttached())
				{
					this.Detach();
				}
				else if (this.CanAttach(this.m_useRequester.gameObject))
				{
					this.AttachTo(this.m_useRequester.gameObject);
				}
				else
				{
					this.m_useRequester.Message(MessageHud.MessageType.Center, "Not in the right position", 0, null);
				}
				this.m_useRequester = null;
			}
			if (this.IsAttached() && !this.CanAttach(this.m_attachJoin.connectedBody.gameObject))
			{
				this.Detach();
				return;
			}
		}
		else if (this.IsAttached())
		{
			this.Detach();
		}
	}

	private void LateUpdate()
	{
		if (this.IsAttached())
		{
			this.m_lineRenderer.enabled = true;
			this.m_lineRenderer.SetPosition(0, this.m_lineAttachPoints0.position);
			this.m_lineRenderer.SetPosition(1, this.m_attachJoin.connectedBody.transform.position + this.m_lineAttachOffset);
			this.m_lineRenderer.SetPosition(2, this.m_lineAttachPoints1.position);
			return;
		}
		this.m_lineRenderer.enabled = false;
	}

	public bool IsAttached(Character character)
	{
		return this.m_attachJoin && this.m_attachJoin.connectedBody.gameObject == character.gameObject;
	}

	public bool InUse()
	{
		return (this.m_container && this.m_container.IsInUse()) || this.IsAttached();
	}

	private bool IsAttached()
	{
		return this.m_attachJoin != null;
	}

	private bool CanAttach(GameObject go)
	{
		if (base.transform.up.y < 0.1f)
		{
			return false;
		}
		Humanoid component = go.GetComponent<Humanoid>();
		return (!component || (!component.InDodge() && !component.IsTeleporting())) && Vector3.Distance(go.transform.position + this.m_attachOffset, this.m_attachPoint.position) < this.m_detachDistance;
	}

	private void AttachTo(GameObject go)
	{
		Vagon.DetachAll();
		this.m_attachJoin = base.gameObject.AddComponent<ConfigurableJoint>();
		this.m_attachJoin.autoConfigureConnectedAnchor = false;
		this.m_attachJoin.anchor = this.m_attachPoint.localPosition;
		this.m_attachJoin.connectedAnchor = this.m_attachOffset;
		this.m_attachJoin.breakForce = this.m_breakForce;
		this.m_attachJoin.xMotion = ConfigurableJointMotion.Limited;
		this.m_attachJoin.yMotion = ConfigurableJointMotion.Limited;
		this.m_attachJoin.zMotion = ConfigurableJointMotion.Limited;
		SoftJointLimit linearLimit = default(SoftJointLimit);
		linearLimit.limit = 0.001f;
		this.m_attachJoin.linearLimit = linearLimit;
		SoftJointLimitSpring linearLimitSpring = default(SoftJointLimitSpring);
		linearLimitSpring.spring = this.m_spring;
		linearLimitSpring.damper = this.m_springDamping;
		this.m_attachJoin.linearLimitSpring = linearLimitSpring;
		this.m_attachJoin.zMotion = ConfigurableJointMotion.Locked;
		this.m_attachJoin.connectedBody = go.GetComponent<Rigidbody>();
	}

	private static void DetachAll()
	{
		foreach (Vagon vagon in Vagon.m_instances)
		{
			vagon.Detach();
		}
	}

	private void Detach()
	{
		if (this.m_attachJoin)
		{
			UnityEngine.Object.Destroy(this.m_attachJoin);
			this.m_attachJoin = null;
			this.m_body.WakeUp();
			this.m_body.AddForce(0f, 1f, 0f);
		}
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void UpdateMass()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_container == null)
		{
			return;
		}
		float totalWeight = this.m_container.GetInventory().GetTotalWeight();
		float mass = this.m_baseMass + totalWeight * this.m_itemWeightMassFactor;
		this.SetMass(mass);
	}

	private void SetMass(float mass)
	{
		float mass2 = mass / (float)this.m_bodies.Length;
		Rigidbody[] bodies = this.m_bodies;
		for (int i = 0; i < bodies.Length; i++)
		{
			bodies[i].mass = mass2;
		}
	}

	private void UpdateLoadVisualization()
	{
		if (this.m_container == null)
		{
			return;
		}
		float num = this.m_container.GetInventory().SlotsUsedPercentage();
		foreach (Vagon.LoadData loadData in this.m_loadVis)
		{
			loadData.m_gameobject.SetActive(num >= loadData.m_minPercentage);
		}
	}

	private void UpdateAudio(float dt)
	{
		float num = 0f;
		foreach (Rigidbody rigidbody in this.m_wheels)
		{
			num += rigidbody.angularVelocity.magnitude;
		}
		num /= (float)this.m_wheels.Length;
		float target = Mathf.Lerp(this.m_minPitch, this.m_maxPitch, Mathf.Clamp01(num / this.m_maxPitchVel));
		float target2 = this.m_maxVol * Mathf.Clamp01(num / this.m_maxVolVel);
		foreach (AudioSource audioSource in this.m_wheelLoops)
		{
			audioSource.volume = Mathf.MoveTowards(audioSource.volume, target2, this.m_audioChangeSpeed * dt);
			audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, target, this.m_audioChangeSpeed * dt);
		}
	}

	private static List<Vagon> m_instances = new List<Vagon>();

	public Transform m_attachPoint;

	public string m_name = "Wagon";

	public float m_detachDistance = 2f;

	public Vector3 m_attachOffset = new Vector3(0f, 0.8f, 0f);

	public Container m_container;

	public Transform m_lineAttachPoints0;

	public Transform m_lineAttachPoints1;

	public Vector3 m_lineAttachOffset = new Vector3(0f, 1f, 0f);

	public float m_breakForce = 10000f;

	public float m_spring = 5000f;

	public float m_springDamping = 1000f;

	public float m_baseMass = 20f;

	public float m_itemWeightMassFactor = 1f;

	public AudioSource[] m_wheelLoops;

	public float m_minPitch = 1f;

	public float m_maxPitch = 1.5f;

	public float m_maxPitchVel = 10f;

	public float m_maxVol = 1f;

	public float m_maxVolVel = 10f;

	public float m_audioChangeSpeed = 2f;

	public Rigidbody[] m_wheels = new Rigidbody[0];

	public List<Vagon.LoadData> m_loadVis = new List<Vagon.LoadData>();

	private ZNetView m_nview;

	private ConfigurableJoint m_attachJoin;

	private Rigidbody m_body;

	private LineRenderer m_lineRenderer;

	private Rigidbody[] m_bodies;

	private Humanoid m_useRequester;

	[Serializable]
	public class LoadData
	{
		public GameObject m_gameobject;

		public float m_minPercentage;
	}
}
