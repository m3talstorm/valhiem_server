using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	private void Awake()
	{
		this.m_character = base.GetComponent<Player>();
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		PlayerController.m_mouseSens = PlayerPrefs.GetFloat("MouseSensitivity", PlayerController.m_mouseSens);
		PlayerController.m_invertMouse = (PlayerPrefs.GetInt("InvertMouse", 0) == 1);
	}

	private void FixedUpdate()
	{
		if (this.m_nview && !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.TakeInput())
		{
			this.m_character.SetControls(Vector3.zero, false, false, false, false, false, false, false, false, false);
			return;
		}
		bool flag = this.InInventoryEtc();
		Vector3 zero = Vector3.zero;
		if (ZInput.GetButton("Forward"))
		{
			zero.z += 1f;
		}
		if (ZInput.GetButton("Backward"))
		{
			zero.z -= 1f;
		}
		if (ZInput.GetButton("Left"))
		{
			zero.x -= 1f;
		}
		if (ZInput.GetButton("Right"))
		{
			zero.x += 1f;
		}
		zero.x += ZInput.GetJoyLeftStickX();
		zero.z += -ZInput.GetJoyLeftStickY();
		if (zero.magnitude > 1f)
		{
			zero.Normalize();
		}
		bool flag2 = (ZInput.GetButton("Attack") || ZInput.GetButton("JoyAttack")) && !flag;
		bool attackHold = flag2;
		bool attack = flag2 && !this.m_attackWasPressed;
		this.m_attackWasPressed = flag2;
		bool flag3 = (ZInput.GetButton("SecondAttack") || ZInput.GetButton("JoySecondAttack")) && !flag;
		bool secondaryAttack = flag3 && !this.m_secondAttackWasPressed;
		this.m_secondAttackWasPressed = flag3;
		bool flag4 = (ZInput.GetButton("Block") || ZInput.GetButton("JoyBlock")) && !flag;
		bool blockHold = flag4;
		bool block = flag4 && !this.m_blockWasPressed;
		this.m_blockWasPressed = flag4;
		bool button = ZInput.GetButton("Jump");
		bool jump = (button && !this.m_lastJump) || ZInput.GetButtonDown("JoyJump");
		this.m_lastJump = button;
		bool flag5 = InventoryGui.IsVisible();
		bool flag6 = (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch")) && !flag5;
		bool crouch = flag6 && !this.m_lastCrouch;
		this.m_lastCrouch = flag6;
		bool run = ZInput.GetButton("Run") || ZInput.GetButton("JoyRun");
		bool button2 = ZInput.GetButton("AutoRun");
		this.m_character.SetControls(zero, attack, attackHold, secondaryAttack, block, blockHold, jump, crouch, run, button2);
	}

	private static bool DetectTap(bool pressed, float dt, float minPressTime, bool run, ref float pressTimer, ref float releasedTimer, ref bool tapPressed)
	{
		bool result = false;
		if (pressed)
		{
			if ((releasedTimer > 0f && releasedTimer < minPressTime) & tapPressed)
			{
				tapPressed = false;
				result = true;
			}
			pressTimer += dt;
			releasedTimer = 0f;
		}
		else
		{
			if (pressTimer > 0f)
			{
				tapPressed = (pressTimer < minPressTime);
				if (run & tapPressed)
				{
					tapPressed = false;
					result = true;
				}
			}
			releasedTimer += dt;
			pressTimer = 0f;
		}
		return result;
	}

	private bool TakeInput()
	{
		return !GameCamera.InFreeFly() && ((!Chat.instance || !Chat.instance.HasFocus()) && !Menu.IsVisible() && !global::Console.IsVisible() && !TextInput.IsVisible() && !Minimap.InTextInput() && (!ZInput.IsGamepadActive() || !Minimap.IsOpen()) && (!ZInput.IsGamepadActive() || !InventoryGui.IsVisible()) && (!ZInput.IsGamepadActive() || !StoreGui.IsVisible())) && (!ZInput.IsGamepadActive() || !Hud.IsPieceSelectionVisible());
	}

	private bool InInventoryEtc()
	{
		return InventoryGui.IsVisible() || Minimap.IsOpen() || StoreGui.IsVisible() || Hud.IsPieceSelectionVisible();
	}

	private void LateUpdate()
	{
		if (!this.TakeInput() || this.InInventoryEtc())
		{
			this.m_character.SetMouseLook(Vector2.zero);
			return;
		}
		Vector2 zero = Vector2.zero;
		zero.x = Input.GetAxis("Mouse X") * PlayerController.m_mouseSens;
		zero.y = Input.GetAxis("Mouse Y") * PlayerController.m_mouseSens;
		if (!this.m_character.InPlaceMode() || !ZInput.GetButton("JoyRotate"))
		{
			zero.x += ZInput.GetJoyRightStickX() * 110f * Time.deltaTime;
			zero.y += -ZInput.GetJoyRightStickY() * 110f * Time.deltaTime;
		}
		if (PlayerController.m_invertMouse)
		{
			zero.y *= -1f;
		}
		this.m_character.SetMouseLook(zero);
	}

	private Player m_character;

	private ZNetView m_nview;

	public static float m_mouseSens = 1f;

	public static bool m_invertMouse = false;

	public float m_minDodgeTime = 0.2f;

	private bool m_attackWasPressed;

	private bool m_secondAttackWasPressed;

	private bool m_blockWasPressed;

	private bool m_lastJump;

	private bool m_lastCrouch;
}
