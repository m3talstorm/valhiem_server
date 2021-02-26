using System;
using UnityEngine;

public class KeyHints : MonoBehaviour
{
	private void OnDestroy()
	{
		KeyHints.m_instance = null;
	}

	public static KeyHints instance
	{
		get
		{
			return KeyHints.m_instance;
		}
	}

	private void Awake()
	{
		KeyHints.m_instance = this;
		this.ApplySettings();
	}

	private void Start()
	{
	}

	public void ApplySettings()
	{
		this.m_keyHintsEnabled = (PlayerPrefs.GetInt("KeyHints", 1) == 1);
	}

	private void Update()
	{
		this.UpdateHints();
	}

	private void UpdateHints()
	{
		Player localPlayer = Player.m_localPlayer;
		if (!this.m_keyHintsEnabled || localPlayer == null || localPlayer.IsDead() || Chat.instance.IsChatDialogWindowVisible())
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(false);
			return;
		}
		bool activeSelf = this.m_buildHints.activeSelf;
		bool activeSelf2 = this.m_buildHints.activeSelf;
		ItemDrop.ItemData currentWeapon = localPlayer.GetCurrentWeapon();
		if (localPlayer.InPlaceMode())
		{
			this.m_buildHints.SetActive(true);
			this.m_combatHints.SetActive(false);
			return;
		}
		if (localPlayer.GetShipControl())
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(false);
			return;
		}
		if (currentWeapon != null && (currentWeapon != localPlayer.m_unarmedWeapon.m_itemData || localPlayer.IsTargeted()))
		{
			this.m_buildHints.SetActive(false);
			this.m_combatHints.SetActive(true);
			bool flag = currentWeapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow;
			bool active = !flag && currentWeapon.HavePrimaryAttack();
			bool active2 = !flag && currentWeapon.HaveSecondaryAttack();
			this.m_bowDrawGP.SetActive(flag);
			this.m_bowDrawKB.SetActive(flag);
			this.m_primaryAttackGP.SetActive(active);
			this.m_primaryAttackKB.SetActive(active);
			this.m_secondaryAttackGP.SetActive(active2);
			this.m_secondaryAttackKB.SetActive(active2);
			return;
		}
		this.m_buildHints.SetActive(false);
		this.m_combatHints.SetActive(false);
	}

	private static KeyHints m_instance;

	[Header("Key hints")]
	public GameObject m_buildHints;

	public GameObject m_combatHints;

	public GameObject m_primaryAttackGP;

	public GameObject m_primaryAttackKB;

	public GameObject m_secondaryAttackGP;

	public GameObject m_secondaryAttackKB;

	public GameObject m_bowDrawGP;

	public GameObject m_bowDrawKB;

	private bool m_keyHintsEnabled = true;
}
