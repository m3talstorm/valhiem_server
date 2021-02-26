using System;
using UnityEngine;

public class Valkyrie : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_animator = base.GetComponentInChildren<Animator>();
		if (!this.m_nview.IsOwner())
		{
			base.enabled = false;
			return;
		}
		ZLog.Log("Setting up valkyrie ");
		float f = UnityEngine.Random.value * 3.1415927f * 2f;
		Vector3 vector = new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f));
		Vector3 a = Vector3.Cross(vector, Vector3.up);
		Player.m_localPlayer.SetIntro(true);
		this.m_targetPoint = Player.m_localPlayer.transform.position + new Vector3(0f, this.m_dropHeight, 0f);
		Vector3 position = this.m_targetPoint + vector * this.m_startDistance;
		position.y = this.m_startAltitude;
		base.transform.position = position;
		this.m_descentStart = this.m_targetPoint + vector * this.m_startDescentDistance + a * 200f;
		this.m_descentStart.y = this.m_descentAltitude;
		Vector3 a2 = this.m_targetPoint - this.m_descentStart;
		a2.y = 0f;
		a2.Normalize();
		this.m_flyAwayPoint = this.m_targetPoint + a2 * this.m_startDescentDistance;
		this.m_flyAwayPoint.y = this.m_startAltitude;
		this.ShowText();
		this.SyncPlayer(true);
		ZLog.Log(string.Concat(new object[]
		{
			"World pos ",
			base.transform.position,
			"   ",
			ZNet.instance.GetReferencePosition()
		}));
	}

	private void ShowText()
	{
		TextViewer.instance.ShowText(TextViewer.Style.Intro, this.m_introTopic, this.m_introText, false);
	}

	private void HideText()
	{
	}

	private void OnDestroy()
	{
		ZLog.Log("Destroying valkyrie");
	}

	private void FixedUpdate()
	{
		this.UpdateValkyrie(Time.fixedDeltaTime);
		if (!this.m_droppedPlayer)
		{
			this.SyncPlayer(true);
		}
	}

	private void LateUpdate()
	{
		if (!this.m_droppedPlayer)
		{
			this.SyncPlayer(false);
		}
	}

	private void UpdateValkyrie(float dt)
	{
		this.m_timer += dt;
		if (this.m_timer < this.m_startPause)
		{
			return;
		}
		Vector3 vector;
		if (this.m_droppedPlayer)
		{
			vector = this.m_flyAwayPoint;
		}
		else if (this.m_descent)
		{
			vector = this.m_targetPoint;
		}
		else
		{
			vector = this.m_descentStart;
		}
		if (Utils.DistanceXZ(vector, base.transform.position) < 0.5f)
		{
			if (!this.m_descent)
			{
				this.m_descent = true;
				ZLog.Log("Starting descent");
			}
			else if (!this.m_droppedPlayer)
			{
				ZLog.Log("We are here");
				this.DropPlayer();
			}
			else
			{
				this.m_nview.Destroy();
			}
		}
		Vector3 normalized = (vector - base.transform.position).normalized;
		Vector3 vector2 = base.transform.position + normalized * 25f;
		float num;
		if (ZoneSystem.instance.GetGroundHeight(vector2, out num))
		{
			vector2.y = Mathf.Max(vector2.y, num + this.m_dropHeight);
		}
		Vector3 normalized2 = (vector2 - base.transform.position).normalized;
		Quaternion quaternion = Quaternion.LookRotation(normalized2);
		Vector3 to = normalized2;
		to.y = 0f;
		to.Normalize();
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		float num2 = Mathf.Clamp(Vector3.SignedAngle(forward, to, Vector3.up), -30f, 30f) / 30f;
		quaternion = Quaternion.Euler(0f, 0f, num2 * 45f) * quaternion;
		float num3 = this.m_droppedPlayer ? (this.m_turnRate * 4f) : this.m_turnRate;
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, num3 * dt);
		Vector3 a = base.transform.forward * this.m_speed;
		Vector3 vector3 = base.transform.position + a * dt;
		float num4;
		if (ZoneSystem.instance.GetGroundHeight(vector3, out num4))
		{
			vector3.y = Mathf.Max(vector3.y, num4 + this.m_dropHeight);
		}
		base.transform.position = vector3;
	}

	private void DropPlayer()
	{
		ZLog.Log("We are here");
		this.m_droppedPlayer = true;
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Player.m_localPlayer.transform.rotation = Quaternion.LookRotation(forward);
		Player.m_localPlayer.SetIntro(false);
		this.m_animator.SetBool("dropped", true);
	}

	private void SyncPlayer(bool doNetworkSync)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			ZLog.LogWarning("No local player");
			return;
		}
		localPlayer.transform.rotation = this.m_attachPoint.rotation;
		localPlayer.transform.position = this.m_attachPoint.position - localPlayer.transform.TransformVector(this.m_attachOffset);
		localPlayer.GetComponent<Rigidbody>().position = localPlayer.transform.position;
		if (doNetworkSync)
		{
			ZNet.instance.SetReferencePosition(localPlayer.transform.position);
			localPlayer.GetComponent<ZSyncTransform>().SyncNow();
			base.GetComponent<ZSyncTransform>().SyncNow();
		}
	}

	public float m_startPause = 10f;

	public float m_speed = 10f;

	public float m_turnRate = 5f;

	public float m_dropHeight = 10f;

	public float m_startAltitude = 500f;

	public float m_descentAltitude = 100f;

	public float m_startDistance = 500f;

	public float m_startDescentDistance = 200f;

	public Vector3 m_attachOffset = new Vector3(0f, 0f, 1f);

	public float m_textDuration = 5f;

	public string m_introTopic = "";

	[TextArea]
	public string m_introText = "";

	public Transform m_attachPoint;

	private Vector3 m_targetPoint;

	private Vector3 m_descentStart;

	private Vector3 m_flyAwayPoint;

	private bool m_descent;

	private bool m_droppedPlayer;

	private Animator m_animator;

	private ZNetView m_nview;

	private float m_timer;
}
