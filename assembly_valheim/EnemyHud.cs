using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHud : MonoBehaviour
{
	public static EnemyHud instance
	{
		get
		{
			return EnemyHud.m_instance;
		}
	}

	private void Awake()
	{
		EnemyHud.m_instance = this;
		this.m_baseHud.SetActive(false);
		this.m_baseHudBoss.SetActive(false);
		this.m_baseHudPlayer.SetActive(false);
	}

	private void OnDestroy()
	{
		EnemyHud.m_instance = null;
	}

	private void LateUpdate()
	{
		this.m_hudRoot.SetActive(!Hud.IsUserHidden());
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer != null)
		{
			this.m_refPoint = localPlayer.transform.position;
		}
		foreach (Character character in Character.GetAllCharacters())
		{
			if (!(character == localPlayer) && this.TestShow(character))
			{
				this.ShowHud(character);
			}
		}
		this.UpdateHuds(localPlayer, Time.deltaTime);
	}

	private bool TestShow(Character c)
	{
		float num = Vector3.SqrMagnitude(c.transform.position - this.m_refPoint);
		if (c.IsBoss() && num < this.m_maxShowDistanceBoss * this.m_maxShowDistanceBoss)
		{
			if (num < this.m_maxShowDistanceBoss * this.m_maxShowDistanceBoss && c.GetComponent<BaseAI>().IsAlerted())
			{
				return true;
			}
		}
		else if (num < this.m_maxShowDistance * this.m_maxShowDistance)
		{
			return !c.IsPlayer() || !c.IsCrouching();
		}
		return false;
	}

	private void ShowHud(Character c)
	{
		EnemyHud.HudData hudData;
		if (this.m_huds.TryGetValue(c, out hudData))
		{
			return;
		}
		GameObject original;
		if (c.IsPlayer())
		{
			original = this.m_baseHudPlayer;
		}
		else if (c.IsBoss())
		{
			original = this.m_baseHudBoss;
		}
		else
		{
			original = this.m_baseHud;
		}
		hudData = new EnemyHud.HudData();
		hudData.m_character = c;
		hudData.m_ai = c.GetComponent<BaseAI>();
		hudData.m_gui = UnityEngine.Object.Instantiate<GameObject>(original, this.m_hudRoot.transform);
		hudData.m_gui.SetActive(true);
		hudData.m_healthRoot = hudData.m_gui.transform.Find("Health").gameObject;
		hudData.m_healthFast = hudData.m_healthRoot.transform.Find("health_fast").GetComponent<GuiBar>();
		hudData.m_healthSlow = hudData.m_healthRoot.transform.Find("health_slow").GetComponent<GuiBar>();
		hudData.m_level2 = (hudData.m_gui.transform.Find("level_2") as RectTransform);
		hudData.m_level3 = (hudData.m_gui.transform.Find("level_3") as RectTransform);
		hudData.m_alerted = (hudData.m_gui.transform.Find("Alerted") as RectTransform);
		hudData.m_aware = (hudData.m_gui.transform.Find("Aware") as RectTransform);
		hudData.m_name = hudData.m_gui.transform.Find("Name").GetComponent<Text>();
		hudData.m_name.text = Localization.instance.Localize(c.GetHoverName());
		this.m_huds.Add(c, hudData);
	}

	private void UpdateHuds(Player player, float dt)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (!mainCamera)
		{
			return;
		}
		Character y = player ? player.GetHoverCreature() : null;
		if (player)
		{
			player.IsCrouching();
		}
		Character character = null;
		foreach (KeyValuePair<Character, EnemyHud.HudData> keyValuePair in this.m_huds)
		{
			EnemyHud.HudData value = keyValuePair.Value;
			if (!value.m_character || !this.TestShow(value.m_character))
			{
				if (character == null)
				{
					character = value.m_character;
					UnityEngine.Object.Destroy(value.m_gui);
				}
			}
			else
			{
				if (value.m_character == y)
				{
					value.m_hoverTimer = 0f;
				}
				value.m_hoverTimer += dt;
				float healthPercentage = value.m_character.GetHealthPercentage();
				if (value.m_character.IsPlayer() || value.m_character.IsBoss() || value.m_hoverTimer < this.m_hoverShowDuration)
				{
					value.m_gui.SetActive(true);
					int level = value.m_character.GetLevel();
					if (value.m_level2)
					{
						value.m_level2.gameObject.SetActive(level == 2);
					}
					if (value.m_level3)
					{
						value.m_level3.gameObject.SetActive(level == 3);
					}
					if (!value.m_character.IsBoss() && !value.m_character.IsPlayer())
					{
						bool flag = value.m_character.GetBaseAI().HaveTarget();
						bool flag2 = value.m_character.GetBaseAI().IsAlerted();
						value.m_alerted.gameObject.SetActive(flag2);
						value.m_aware.gameObject.SetActive(!flag2 && flag);
					}
				}
				else
				{
					value.m_gui.SetActive(false);
				}
				value.m_healthSlow.SetValue(healthPercentage);
				value.m_healthFast.SetValue(healthPercentage);
				if (!value.m_character.IsBoss() && value.m_gui.activeSelf)
				{
					Vector3 position = Vector3.zero;
					if (value.m_character.IsPlayer())
					{
						position = value.m_character.GetHeadPoint() + Vector3.up * 0.3f;
					}
					else
					{
						position = value.m_character.GetTopPoint();
					}
					Vector3 vector = mainCamera.WorldToScreenPoint(position);
					if (vector.x < 0f || vector.x > (float)Screen.width || vector.y < 0f || vector.y > (float)Screen.height || vector.z > 0f)
					{
						value.m_gui.transform.position = vector;
						value.m_gui.SetActive(true);
					}
					else
					{
						value.m_gui.SetActive(false);
					}
				}
			}
		}
		if (character != null)
		{
			this.m_huds.Remove(character);
		}
	}

	public bool ShowingBossHud()
	{
		foreach (KeyValuePair<Character, EnemyHud.HudData> keyValuePair in this.m_huds)
		{
			if (keyValuePair.Value.m_character && keyValuePair.Value.m_character.IsBoss())
			{
				return true;
			}
		}
		return false;
	}

	public Character GetActiveBoss()
	{
		foreach (KeyValuePair<Character, EnemyHud.HudData> keyValuePair in this.m_huds)
		{
			if (keyValuePair.Value.m_character && keyValuePair.Value.m_character.IsBoss())
			{
				return keyValuePair.Value.m_character;
			}
		}
		return null;
	}

	private static EnemyHud m_instance;

	public GameObject m_hudRoot;

	public GameObject m_baseHud;

	public GameObject m_baseHudBoss;

	public GameObject m_baseHudPlayer;

	public float m_maxShowDistance = 10f;

	public float m_maxShowDistanceBoss = 100f;

	public float m_hoverShowDuration = 60f;

	private Vector3 m_refPoint = Vector3.zero;

	private Dictionary<Character, EnemyHud.HudData> m_huds = new Dictionary<Character, EnemyHud.HudData>();

	private class HudData
	{
		public Character m_character;

		public BaseAI m_ai;

		public GameObject m_gui;

		public GameObject m_healthRoot;

		public RectTransform m_level2;

		public RectTransform m_level3;

		public RectTransform m_alerted;

		public RectTransform m_aware;

		public GuiBar m_healthFast;

		public GuiBar m_healthSlow;

		public Text m_name;

		public float m_hoverTimer = 99999f;
	}
}
