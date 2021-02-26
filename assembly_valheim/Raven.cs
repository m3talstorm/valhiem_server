using System;
using System.Collections.Generic;
using UnityEngine;

public class Raven : MonoBehaviour, Hoverable, Interactable, IDestructible
{
	public static bool IsInstantiated()
	{
		return Raven.m_instance != null;
	}

	private void Awake()
	{
		base.transform.position = new Vector3(0f, 100000f, 0f);
		Raven.m_instance = this;
		this.m_animator = this.m_visual.GetComponentInChildren<Animator>();
		this.m_collider = base.GetComponent<Collider>();
		base.InvokeRepeating("IdleEffect", UnityEngine.Random.Range(this.m_idleEffectIntervalMin, this.m_idleEffectIntervalMax), UnityEngine.Random.Range(this.m_idleEffectIntervalMin, this.m_idleEffectIntervalMax));
		base.InvokeRepeating("CheckSpawn", 1f, 1f);
	}

	private void OnDestroy()
	{
		if (Raven.m_instance == this)
		{
			Raven.m_instance = null;
		}
	}

	public string GetHoverText()
	{
		if (this.IsSpawned())
		{
			return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact");
		}
		return "";
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_name);
	}

	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_hasTalked && Chat.instance.IsDialogVisible(base.gameObject))
		{
			Chat.instance.ClearNpcText(base.gameObject);
		}
		else
		{
			this.Talk();
		}
		return false;
	}

	private void Talk()
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (this.m_currentText == null)
		{
			return;
		}
		if (this.m_currentText.m_key.Length > 0)
		{
			Player.m_localPlayer.SetSeenTutorial(this.m_currentText.m_key);
			GoogleAnalyticsV4.instance.LogEvent("Game", "Raven", this.m_currentText.m_key, 0L);
		}
		else
		{
			GoogleAnalyticsV4.instance.LogEvent("Game", "Raven", this.m_currentText.m_topic, 0L);
		}
		this.m_hasTalked = true;
		if (this.m_currentText.m_label.Length > 0)
		{
			Player.m_localPlayer.AddKnownText(this.m_currentText.m_label, this.m_currentText.m_text);
		}
		this.Say(this.m_currentText.m_topic, this.m_currentText.m_text, false, true, true);
	}

	private void Say(string topic, string text, bool showName, bool longTimeout, bool large)
	{
		if (topic.Length > 0)
		{
			text = "<color=orange>" + topic + "</color>\n" + text;
		}
		Chat.instance.SetNpcText(base.gameObject, Vector3.up * this.m_textOffset, this.m_textCullDistance, longTimeout ? this.m_longDialogVisibleTime : this.m_dialogVisibleTime, showName ? this.m_name : "", text, large);
		this.m_animator.SetTrigger("talk");
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void IdleEffect()
	{
		if (!this.IsSpawned())
		{
			return;
		}
		this.m_idleEffect.Create(base.transform.position, base.transform.rotation, null, 1f);
		base.CancelInvoke("IdleEffect");
		base.InvokeRepeating("IdleEffect", UnityEngine.Random.Range(this.m_idleEffectIntervalMin, this.m_idleEffectIntervalMax), UnityEngine.Random.Range(this.m_idleEffectIntervalMin, this.m_idleEffectIntervalMax));
	}

	private bool CanHide()
	{
		return Player.m_localPlayer == null || !Chat.instance.IsDialogVisible(base.gameObject);
	}

	private void Update()
	{
		this.m_timeSinceTeleport += Time.deltaTime;
		if (!this.IsAway() && !this.IsFlying() && Player.m_localPlayer)
		{
			Vector3 vector = Player.m_localPlayer.transform.position - base.transform.position;
			vector.y = 0f;
			vector.Normalize();
			float f = Vector3.SignedAngle(base.transform.forward, vector, Vector3.up);
			if (Mathf.Abs(f) > this.m_minRotationAngle)
			{
				this.m_animator.SetFloat("anglevel", this.m_rotateSpeed * Mathf.Sign(f), 0.4f, Time.deltaTime);
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, Quaternion.LookRotation(vector), Time.deltaTime * this.m_rotateSpeed);
			}
			else
			{
				this.m_animator.SetFloat("anglevel", 0f, 0.4f, Time.deltaTime);
			}
		}
		if (this.IsSpawned())
		{
			if (Player.m_localPlayer != null && !Chat.instance.IsDialogVisible(base.gameObject) && Vector3.Distance(Player.m_localPlayer.transform.position, base.transform.position) < this.m_autoTalkDistance)
			{
				this.m_randomTextTimer += Time.deltaTime;
				float num = this.m_hasTalked ? this.m_randomTextInterval : this.m_randomTextIntervalImportant;
				if (this.m_randomTextTimer >= num)
				{
					this.m_randomTextTimer = 0f;
					if (this.m_hasTalked)
					{
						this.Say("", this.m_randomTexts[UnityEngine.Random.Range(0, this.m_randomTexts.Count)], false, false, false);
					}
					else
					{
						this.Say("", this.m_randomTextsImportant[UnityEngine.Random.Range(0, this.m_randomTextsImportant.Count)], false, false, false);
					}
				}
			}
			if ((Player.m_localPlayer == null || Vector3.Distance(Player.m_localPlayer.transform.position, base.transform.position) > this.m_despawnDistance || this.EnemyNearby(base.transform.position) || RandEventSystem.InEvent() || this.m_currentText == null || this.m_groundObject == null || this.m_hasTalked) && this.CanHide())
			{
				bool forceTeleport = this.GetBestText() != null || this.m_groundObject == null;
				this.FlyAway(forceTeleport);
				this.RestartSpawnCheck(3f);
			}
			this.m_exclamation.SetActive(!this.m_hasTalked);
			return;
		}
		this.m_exclamation.SetActive(false);
	}

	private bool FindSpawnPoint(out Vector3 point, out GameObject landOn)
	{
		Vector3 position = Player.m_localPlayer.transform.position;
		Vector3 forward = Utils.GetMainCamera().transform.forward;
		forward.y = 0f;
		forward.Normalize();
		point = new Vector3(0f, -999f, 0f);
		landOn = null;
		bool result = false;
		for (int i = 0; i < 20; i++)
		{
			Vector3 a = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(-30, 30), 0f) * forward;
			Vector3 vector = position + a * UnityEngine.Random.Range(this.m_spawnDistance - 5f, this.m_spawnDistance);
			float num;
			Vector3 vector2;
			GameObject gameObject;
			if (ZoneSystem.instance.GetSolidHeight(vector, out num, out vector2, out gameObject) && num > ZoneSystem.instance.m_waterLevel && num > point.y && num < 2000f && vector2.y > 0.5f && Mathf.Abs(num - position.y) < 2f)
			{
				vector.y = num;
				point = vector;
				landOn = gameObject;
				result = true;
			}
		}
		return result;
	}

	private bool EnemyNearby(Vector3 point)
	{
		return LootSpawner.IsMonsterInRange(point, this.m_enemyCheckDistance);
	}

	private bool InState(string name)
	{
		return this.m_animator.isInitialized && (this.m_animator.GetCurrentAnimatorStateInfo(0).IsTag(name) || this.m_animator.GetNextAnimatorStateInfo(0).IsTag(name));
	}

	private Raven.RavenText GetBestText()
	{
		Raven.RavenText ravenText = this.GetTempText();
		Raven.RavenText closestStaticText = this.GetClosestStaticText(this.m_spawnDistance);
		if (closestStaticText != null && (ravenText == null || closestStaticText.m_priority >= ravenText.m_priority))
		{
			ravenText = closestStaticText;
		}
		return ravenText;
	}

	private Raven.RavenText GetTempText()
	{
		foreach (Raven.RavenText ravenText in Raven.m_tempTexts)
		{
			if (ravenText.m_munin == this.m_isMunin)
			{
				return ravenText;
			}
		}
		return null;
	}

	private Raven.RavenText GetClosestStaticText(float maxDistance)
	{
		if (Player.m_localPlayer == null)
		{
			return null;
		}
		Raven.RavenText ravenText = null;
		float num = 9999f;
		bool flag = false;
		Vector3 position = Player.m_localPlayer.transform.position;
		foreach (Raven.RavenText ravenText2 in Raven.m_staticTexts)
		{
			if (ravenText2.m_munin == this.m_isMunin && ravenText2.m_guidePoint)
			{
				float num2 = Vector3.Distance(position, ravenText2.m_guidePoint.transform.position);
				if (num2 < maxDistance)
				{
					bool flag2 = ravenText2.m_key.Length > 0 && Player.m_localPlayer.HaveSeenTutorial(ravenText2.m_key);
					if (ravenText2.m_alwaysSpawn || !flag2)
					{
						if (ravenText == null)
						{
							ravenText = ravenText2;
							num = num2;
							flag = flag2;
						}
						else if (flag2 == flag)
						{
							if (ravenText2.m_priority == ravenText.m_priority || flag2)
							{
								if (num2 < num)
								{
									ravenText = ravenText2;
									num = num2;
									flag = flag2;
								}
							}
							else if (ravenText2.m_priority > ravenText.m_priority)
							{
								ravenText = ravenText2;
								num = num2;
								flag = flag2;
							}
						}
						else if (!flag2 && flag)
						{
							ravenText = ravenText2;
							num = num2;
							flag = flag2;
						}
					}
				}
			}
		}
		return ravenText;
	}

	private void RemoveSeendTempTexts()
	{
		for (int i = 0; i < Raven.m_tempTexts.Count; i++)
		{
			if (Player.m_localPlayer.HaveSeenTutorial(Raven.m_tempTexts[i].m_key))
			{
				Raven.m_tempTexts.RemoveAt(i);
				return;
			}
		}
	}

	private void FlyAway(bool forceTeleport = false)
	{
		Chat.instance.ClearNpcText(base.gameObject);
		if (forceTeleport || this.IsUnderRoof())
		{
			this.m_animator.SetTrigger("poff");
			this.m_timeSinceTeleport = 0f;
			return;
		}
		this.m_animator.SetTrigger("flyaway");
	}

	private void CheckSpawn()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		this.RemoveSeendTempTexts();
		Raven.RavenText bestText = this.GetBestText();
		if (this.IsSpawned() && this.CanHide() && bestText != null && bestText != this.m_currentText)
		{
			this.FlyAway(true);
			this.m_currentText = null;
		}
		if (this.IsAway() && bestText != null)
		{
			if (this.EnemyNearby(base.transform.position))
			{
				return;
			}
			if (RandEventSystem.InEvent())
			{
				return;
			}
			bool forceTeleport = this.m_timeSinceTeleport < 6f;
			this.Spawn(bestText, forceTeleport);
		}
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Character;
	}

	public void Damage(HitData hit)
	{
		if (!this.IsSpawned())
		{
			return;
		}
		this.m_animator.SetTrigger("poff");
		this.RestartSpawnCheck(4f);
	}

	private void RestartSpawnCheck(float delay)
	{
		base.CancelInvoke("CheckSpawn");
		base.InvokeRepeating("CheckSpawn", delay, 1f);
	}

	private bool IsSpawned()
	{
		return this.InState("visible");
	}

	public bool IsAway()
	{
		return this.InState("away");
	}

	public bool IsFlying()
	{
		return this.InState("flying");
	}

	private void Spawn(Raven.RavenText text, bool forceTeleport)
	{
		if (Utils.GetMainCamera() == null)
		{
			return;
		}
		if (text.m_static)
		{
			this.m_groundObject = text.m_guidePoint.gameObject;
			base.transform.position = text.m_guidePoint.transform.position;
		}
		else
		{
			Vector3 position;
			GameObject groundObject;
			if (!this.FindSpawnPoint(out position, out groundObject))
			{
				return;
			}
			base.transform.position = position;
			this.m_groundObject = groundObject;
		}
		this.m_currentText = text;
		this.m_hasTalked = false;
		this.m_randomTextTimer = 99999f;
		if (this.m_currentText.m_key.Length > 0 && Player.m_localPlayer.HaveSeenTutorial(this.m_currentText.m_key))
		{
			this.m_hasTalked = true;
		}
		Vector3 forward = Player.m_localPlayer.transform.position - base.transform.position;
		forward.y = 0f;
		forward.Normalize();
		base.transform.rotation = Quaternion.LookRotation(forward);
		if (forceTeleport)
		{
			this.m_animator.SetTrigger("teleportin");
			return;
		}
		if (!text.m_static)
		{
			this.m_animator.SetTrigger("flyin");
			return;
		}
		if (this.IsUnderRoof())
		{
			this.m_animator.SetTrigger("teleportin");
			return;
		}
		this.m_animator.SetTrigger("flyin");
	}

	private bool IsUnderRoof()
	{
		return Physics.Raycast(base.transform.position + Vector3.up * 0.2f, Vector3.up, 20f, LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"piece"
		}));
	}

	public static void RegisterStaticText(Raven.RavenText text)
	{
		Raven.m_staticTexts.Add(text);
	}

	public static void UnregisterStaticText(Raven.RavenText text)
	{
		Raven.m_staticTexts.Remove(text);
	}

	public static void AddTempText(string key, string topic, string text, string label, bool munin)
	{
		if (key.Length > 0)
		{
			using (List<Raven.RavenText>.Enumerator enumerator = Raven.m_tempTexts.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.m_key == key)
					{
						return;
					}
				}
			}
		}
		Raven.RavenText ravenText = new Raven.RavenText();
		ravenText.m_key = key;
		ravenText.m_topic = topic;
		ravenText.m_label = label;
		ravenText.m_text = text;
		ravenText.m_static = false;
		ravenText.m_munin = munin;
		Raven.m_tempTexts.Add(ravenText);
	}

	public GameObject m_visual;

	public GameObject m_exclamation;

	public string m_name = "Name";

	public bool m_isMunin;

	public bool m_autoTalk = true;

	public float m_idleEffectIntervalMin = 10f;

	public float m_idleEffectIntervalMax = 20f;

	public float m_spawnDistance = 15f;

	public float m_despawnDistance = 20f;

	public float m_autoTalkDistance = 3f;

	public float m_enemyCheckDistance = 10f;

	public float m_rotateSpeed = 10f;

	public float m_minRotationAngle = 15f;

	public float m_dialogVisibleTime = 10f;

	public float m_longDialogVisibleTime = 10f;

	public float m_dontFlyDistance = 3f;

	public float m_textOffset = 1.5f;

	public float m_textCullDistance = 20f;

	public float m_randomTextInterval = 30f;

	public float m_randomTextIntervalImportant = 10f;

	public List<string> m_randomTextsImportant = new List<string>();

	public List<string> m_randomTexts = new List<string>();

	public EffectList m_idleEffect = new EffectList();

	public EffectList m_despawnEffect = new EffectList();

	private Raven.RavenText m_currentText;

	private GameObject m_groundObject;

	private Animator m_animator;

	private Collider m_collider;

	private bool m_hasTalked;

	private float m_randomTextTimer = 9999f;

	private float m_timeSinceTeleport = 9999f;

	private static List<Raven.RavenText> m_tempTexts = new List<Raven.RavenText>();

	private static List<Raven.RavenText> m_staticTexts = new List<Raven.RavenText>();

	private static Raven m_instance = null;

	[Serializable]
	public class RavenText
	{
		public bool m_alwaysSpawn = true;

		public bool m_munin;

		public int m_priority;

		public string m_key = "";

		public string m_topic = "";

		public string m_label = "";

		[TextArea]
		public string m_text = "";

		[NonSerialized]
		public bool m_static;

		[NonSerialized]
		public GuidePoint m_guidePoint;
	}
}
