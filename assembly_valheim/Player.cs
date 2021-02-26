using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class Player : Humanoid
{
	protected override void Awake()
	{
		base.Awake();
		Player.m_players.Add(this);
		this.m_skills = base.GetComponent<Skills>();
		this.SetupAwake();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_placeRayMask = LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"Default_small",
			"piece",
			"piece_nonsolid",
			"terrain",
			"vehicle"
		});
		this.m_placeWaterRayMask = LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"Default_small",
			"piece",
			"piece_nonsolid",
			"terrain",
			"Water",
			"vehicle"
		});
		this.m_removeRayMask = LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"Default_small",
			"piece",
			"piece_nonsolid",
			"terrain",
			"vehicle"
		});
		this.m_interactMask = LayerMask.GetMask(new string[]
		{
			"item",
			"piece",
			"piece_nonsolid",
			"Default",
			"static_solid",
			"Default_small",
			"character",
			"character_net",
			"terrain",
			"vehicle"
		});
		this.m_autoPickupMask = LayerMask.GetMask(new string[]
		{
			"item"
		});
		Inventory inventory = this.m_inventory;
		inventory.m_onChanged = (Action)Delegate.Combine(inventory.m_onChanged, new Action(this.OnInventoryChanged));
		if (Player.m_attackMask == 0)
		{
			Player.m_attackMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"piece_nonsolid",
				"terrain",
				"character",
				"character_net",
				"character_ghost",
				"hitbox",
				"character_noenv",
				"vehicle"
			});
		}
		if (Player.crouching == 0)
		{
			Player.crouching = ZSyncAnimation.GetHash("crouching");
		}
		this.m_nview.Register("OnDeath", new Action<long>(this.RPC_OnDeath));
		if (this.m_nview.IsOwner())
		{
			this.m_nview.Register<int, string, int>("Message", new Action<long, int, string, int>(this.RPC_Message));
			this.m_nview.Register<bool, bool>("OnTargeted", new Action<long, bool, bool>(this.RPC_OnTargeted));
			this.m_nview.Register<float>("UseStamina", new Action<long, float>(this.RPC_UseStamina));
			if (MusicMan.instance)
			{
				MusicMan.instance.TriggerMusic("Wakeup");
			}
			this.UpdateKnownRecipesList();
			this.UpdateAvailablePiecesList();
			this.SetupPlacementGhost();
		}
	}

	public void SetLocalPlayer()
	{
		if (Player.m_localPlayer == this)
		{
			return;
		}
		Player.m_localPlayer = this;
		ZNet.instance.SetReferencePosition(base.transform.position);
		EnvMan.instance.SetForceEnvironment("");
	}

	public void SetPlayerID(long playerID, string name)
	{
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (this.GetPlayerID() != 0L)
		{
			return;
		}
		this.m_nview.GetZDO().Set("playerID", playerID);
		this.m_nview.GetZDO().Set("playerName", name);
	}

	public long GetPlayerID()
	{
		if (this.m_nview.IsValid())
		{
			return this.m_nview.GetZDO().GetLong("playerID", 0L);
		}
		return 0L;
	}

	public string GetPlayerName()
	{
		if (this.m_nview.IsValid())
		{
			return this.m_nview.GetZDO().GetString("playerName", "...");
		}
		return "";
	}

	public override string GetHoverText()
	{
		return "";
	}

	public override string GetHoverName()
	{
		return this.GetPlayerName();
	}

	protected override void Start()
	{
		base.Start();
		this.m_nview.GetZDO();
	}

	public override void OnDestroy()
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo != null && ZNet.instance != null)
		{
			ZLog.LogWarning(string.Concat(new object[]
			{
				"Player destroyed sec:",
				zdo.GetSector(),
				"  pos:",
				base.transform.position,
				"  zdopos:",
				zdo.GetPosition(),
				"  ref ",
				ZNet.instance.GetReferencePosition()
			}));
		}
		if (this.m_placementGhost)
		{
			UnityEngine.Object.Destroy(this.m_placementGhost);
			this.m_placementGhost = null;
		}
		base.OnDestroy();
		Player.m_players.Remove(this);
		if (Player.m_localPlayer == this)
		{
			ZLog.LogWarning("Local player destroyed");
			Player.m_localPlayer = null;
		}
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		float fixedDeltaTime = Time.fixedDeltaTime;
		this.UpdateAwake(fixedDeltaTime);
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.UpdateTargeted(fixedDeltaTime);
		if (this.m_nview.IsOwner())
		{
			if (Player.m_localPlayer != this)
			{
				ZLog.Log("Destroying old local player");
				ZNetScene.instance.Destroy(base.gameObject);
				return;
			}
			if (this.IsDead())
			{
				return;
			}
			this.UpdateEquipQueue(fixedDeltaTime);
			this.PlayerAttackInput(fixedDeltaTime);
			this.UpdateAttach();
			this.UpdateShipControl(fixedDeltaTime);
			this.UpdateCrouch(fixedDeltaTime);
			this.UpdateDodge(fixedDeltaTime);
			this.UpdateCover(fixedDeltaTime);
			this.UpdateStations(fixedDeltaTime);
			this.UpdateGuardianPower(fixedDeltaTime);
			this.UpdateBaseValue(fixedDeltaTime);
			this.UpdateStats(fixedDeltaTime);
			this.UpdateTeleport(fixedDeltaTime);
			this.AutoPickup(fixedDeltaTime);
			this.EdgeOfWorldKill(fixedDeltaTime);
			this.UpdateBiome(fixedDeltaTime);
			this.UpdateStealth(fixedDeltaTime);
			if (GameCamera.instance && Vector3.Distance(GameCamera.instance.transform.position, base.transform.position) < 2f)
			{
				base.SetVisible(false);
			}
			AudioMan.instance.SetIndoor(this.InShelter());
		}
	}

	private void Update()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		bool flag = this.TakeInput();
		this.UpdateHover();
		if (flag)
		{
			if (Player.m_debugMode && global::Console.instance.IsCheatsEnabled())
			{
				if (Input.GetKeyDown(KeyCode.Z))
				{
					this.m_debugFly = !this.m_debugFly;
					this.m_nview.GetZDO().Set("DebugFly", this.m_debugFly);
					this.Message(MessageHud.MessageType.TopLeft, "Debug fly:" + this.m_debugFly.ToString(), 0, null);
				}
				if (Input.GetKeyDown(KeyCode.B))
				{
					this.m_noPlacementCost = !this.m_noPlacementCost;
					this.Message(MessageHud.MessageType.TopLeft, "No placement cost:" + this.m_noPlacementCost.ToString(), 0, null);
					this.UpdateAvailablePiecesList();
				}
				if (Input.GetKeyDown(KeyCode.K))
				{
					int num = 0;
					foreach (Character character in Character.GetAllCharacters())
					{
						if (!character.IsPlayer())
						{
							HitData hitData = new HitData();
							hitData.m_damage.m_damage = 99999f;
							character.Damage(hitData);
							num++;
						}
					}
					this.Message(MessageHud.MessageType.TopLeft, "Killing all the monsters:" + num, 0, null);
				}
			}
			if (ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse"))
			{
				if (this.m_hovering)
				{
					this.Interact(this.m_hovering, false);
				}
				else if (this.m_shipControl)
				{
					this.StopShipControl();
				}
			}
			else if ((ZInput.GetButton("Use") || ZInput.GetButton("JoyUse")) && this.m_hovering)
			{
				this.Interact(this.m_hovering, true);
			}
			if (ZInput.GetButtonDown("Hide") || ZInput.GetButtonDown("JoyHide"))
			{
				if (base.GetRightItem() != null || base.GetLeftItem() != null)
				{
					if (!this.InAttack())
					{
						base.HideHandItems();
					}
				}
				else if (!base.IsSwiming() || base.IsOnGround())
				{
					base.ShowHandItems();
				}
			}
			if (ZInput.GetButtonDown("ToggleWalk"))
			{
				base.SetWalk(!base.GetWalk());
				if (base.GetWalk())
				{
					this.Message(MessageHud.MessageType.TopLeft, "$msg_walk 1", 0, null);
				}
				else
				{
					this.Message(MessageHud.MessageType.TopLeft, "$msg_walk 0", 0, null);
				}
			}
			if (ZInput.GetButtonDown("Sit") || (!this.InPlaceMode() && ZInput.GetButtonDown("JoySit")))
			{
				if (this.InEmote() && base.IsSitting())
				{
					this.StopEmote();
				}
				else
				{
					this.StartEmote("sit", false);
				}
			}
			if (ZInput.GetButtonDown("GPower") || ZInput.GetButtonDown("JoyGPower"))
			{
				this.StartGuardianPower();
			}
			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				this.UseHotbarItem(1);
			}
			if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				this.UseHotbarItem(2);
			}
			if (Input.GetKeyDown(KeyCode.Alpha3))
			{
				this.UseHotbarItem(3);
			}
			if (Input.GetKeyDown(KeyCode.Alpha4))
			{
				this.UseHotbarItem(4);
			}
			if (Input.GetKeyDown(KeyCode.Alpha5))
			{
				this.UseHotbarItem(5);
			}
			if (Input.GetKeyDown(KeyCode.Alpha6))
			{
				this.UseHotbarItem(6);
			}
			if (Input.GetKeyDown(KeyCode.Alpha7))
			{
				this.UseHotbarItem(7);
			}
			if (Input.GetKeyDown(KeyCode.Alpha8))
			{
				this.UseHotbarItem(8);
			}
		}
		this.UpdatePlacement(flag, Time.deltaTime);
	}

	private void UpdatePlacement(bool takeInput, float dt)
	{
		this.UpdateWearNTearHover();
		if (!this.InPlaceMode())
		{
			if (this.m_placementGhost)
			{
				this.m_placementGhost.SetActive(false);
			}
			return;
		}
		if (!takeInput)
		{
			return;
		}
		this.UpdateBuildGuiInput();
		if (Hud.IsPieceSelectionVisible())
		{
			return;
		}
		ItemDrop.ItemData rightItem = base.GetRightItem();
		if ((ZInput.GetButtonDown("Remove") || ZInput.GetButtonDown("JoyRemove")) && rightItem.m_shared.m_buildPieces.m_canRemovePieces)
		{
			if (this.HaveStamina(rightItem.m_shared.m_attack.m_attackStamina))
			{
				if (this.RemovePiece())
				{
					base.AddNoise(50f);
					this.UseStamina(rightItem.m_shared.m_attack.m_attackStamina);
					if (rightItem.m_shared.m_useDurability)
					{
						rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain;
					}
				}
			}
			else
			{
				Hud.instance.StaminaBarNoStaminaFlash();
			}
		}
		if (ZInput.GetButtonDown("Attack") || ZInput.GetButtonDown("JoyPlace"))
		{
			Piece selectedPiece = this.m_buildPieces.GetSelectedPiece();
			if (selectedPiece != null)
			{
				if (this.HaveStamina(rightItem.m_shared.m_attack.m_attackStamina))
				{
					if (selectedPiece.m_repairPiece)
					{
						this.Repair(rightItem, selectedPiece);
					}
					else if (this.m_placementGhost != null)
					{
						if (this.m_noPlacementCost || this.HaveRequirements(selectedPiece, Player.RequirementMode.CanBuild))
						{
							if (this.PlacePiece(selectedPiece))
							{
								this.ConsumeResources(selectedPiece.m_resources, 0);
								this.UseStamina(rightItem.m_shared.m_attack.m_attackStamina);
								if (rightItem.m_shared.m_useDurability)
								{
									rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain;
								}
							}
						}
						else
						{
							this.Message(MessageHud.MessageType.Center, "$msg_missingrequirement", 0, null);
						}
					}
				}
				else
				{
					Hud.instance.StaminaBarNoStaminaFlash();
				}
			}
		}
		if (Input.GetAxis("Mouse ScrollWheel") < 0f)
		{
			this.m_placeRotation--;
		}
		if (Input.GetAxis("Mouse ScrollWheel") > 0f)
		{
			this.m_placeRotation++;
		}
		float joyRightStickX = ZInput.GetJoyRightStickX();
		if (ZInput.GetButton("JoyRotate") && Mathf.Abs(joyRightStickX) > 0.5f)
		{
			if (this.m_rotatePieceTimer == 0f)
			{
				if (joyRightStickX < 0f)
				{
					this.m_placeRotation++;
				}
				else
				{
					this.m_placeRotation--;
				}
			}
			else if (this.m_rotatePieceTimer > 0.25f)
			{
				if (joyRightStickX < 0f)
				{
					this.m_placeRotation++;
				}
				else
				{
					this.m_placeRotation--;
				}
				this.m_rotatePieceTimer = 0.17f;
			}
			this.m_rotatePieceTimer += dt;
			return;
		}
		this.m_rotatePieceTimer = 0f;
	}

	private void UpdateBuildGuiInput()
	{
		if (Hud.instance.IsQuickPieceSelectEnabled())
		{
			if (!Hud.IsPieceSelectionVisible() && ZInput.GetButtonDown("BuildMenu"))
			{
				Hud.instance.TogglePieceSelection();
			}
		}
		else if (ZInput.GetButtonDown("BuildMenu"))
		{
			Hud.instance.TogglePieceSelection();
		}
		if (ZInput.GetButtonDown("JoyUse"))
		{
			Hud.instance.TogglePieceSelection();
		}
		if (Hud.IsPieceSelectionVisible())
		{
			if (Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))
			{
				Hud.HidePieceSelection();
			}
			if (ZInput.GetButtonDown("JoyTabLeft") || ZInput.GetButtonDown("BuildPrev") || Input.GetAxis("Mouse ScrollWheel") > 0f)
			{
				this.m_buildPieces.PrevCategory();
				this.UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyTabRight") || ZInput.GetButtonDown("BuildNext") || Input.GetAxis("Mouse ScrollWheel") < 0f)
			{
				this.m_buildPieces.NextCategory();
				this.UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyLStickLeft"))
			{
				this.m_buildPieces.LeftPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickRight"))
			{
				this.m_buildPieces.RightPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickUp"))
			{
				this.m_buildPieces.UpPiece();
				this.SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickDown"))
			{
				this.m_buildPieces.DownPiece();
				this.SetupPlacementGhost();
			}
		}
	}

	public void SetSelectedPiece(Vector2Int p)
	{
		if (this.m_buildPieces && this.m_buildPieces.GetSelectedIndex() != p)
		{
			this.m_buildPieces.SetSelected(p);
			this.SetupPlacementGhost();
		}
	}

	public Piece GetPiece(Vector2Int p)
	{
		if (this.m_buildPieces)
		{
			return this.m_buildPieces.GetPiece(p);
		}
		return null;
	}

	public bool IsPieceAvailable(Piece piece)
	{
		return this.m_buildPieces && this.m_buildPieces.IsPieceAvailable(piece);
	}

	public Piece GetSelectedPiece()
	{
		if (this.m_buildPieces)
		{
			return this.m_buildPieces.GetSelectedPiece();
		}
		return null;
	}

	private void LateUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateEmote();
		if (this.m_nview.IsOwner())
		{
			ZNet.instance.SetReferencePosition(base.transform.position);
			this.UpdatePlacementGhost(false);
		}
	}

	private void SetupAwake()
	{
		if (this.m_nview.GetZDO() == null)
		{
			this.m_animator.SetBool("wakeup", false);
			return;
		}
		bool @bool = this.m_nview.GetZDO().GetBool("wakeup", true);
		this.m_animator.SetBool("wakeup", @bool);
		if (@bool)
		{
			this.m_wakeupTimer = 0f;
		}
	}

	private void UpdateAwake(float dt)
	{
		if (this.m_wakeupTimer >= 0f)
		{
			this.m_wakeupTimer += dt;
			if (this.m_wakeupTimer > 1f)
			{
				this.m_wakeupTimer = -1f;
				this.m_animator.SetBool("wakeup", false);
				if (this.m_nview.IsOwner())
				{
					this.m_nview.GetZDO().Set("wakeup", false);
				}
			}
		}
	}

	private void EdgeOfWorldKill(float dt)
	{
		if (this.IsDead())
		{
			return;
		}
		float magnitude = base.transform.position.magnitude;
		float num = 10420f;
		if (magnitude > num && (base.IsSwiming() || base.transform.position.y < ZoneSystem.instance.m_waterLevel))
		{
			Vector3 a = Vector3.Normalize(base.transform.position);
			float d = Utils.LerpStep(num, 10500f, magnitude) * 10f;
			this.m_body.MovePosition(this.m_body.position + a * d * dt);
		}
		if (magnitude > num && base.transform.position.y < ZoneSystem.instance.m_waterLevel - 40f)
		{
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = 99999f;
			base.Damage(hitData);
		}
	}

	private void AutoPickup(float dt)
	{
		if (this.IsTeleporting())
		{
			return;
		}
		Vector3 vector = base.transform.position + Vector3.up;
		foreach (Collider collider in Physics.OverlapSphere(vector, this.m_autoPickupRange, this.m_autoPickupMask))
		{
			if (collider.attachedRigidbody)
			{
				ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
				if (!(component == null) && component.m_autoPickup && !this.HaveUniqueKey(component.m_itemData.m_shared.m_name) && component.GetComponent<ZNetView>().IsValid())
				{
					if (!component.CanPickup())
					{
						component.RequestOwn();
					}
					else if (this.m_inventory.CanAddItem(component.m_itemData, -1) && component.m_itemData.GetWeight() + this.m_inventory.GetTotalWeight() <= this.GetMaxCarryWeight())
					{
						float num = Vector3.Distance(component.transform.position, vector);
						if (num <= this.m_autoPickupRange)
						{
							if (num < 0.3f)
							{
								base.Pickup(component.gameObject);
							}
							else
							{
								Vector3 a = Vector3.Normalize(vector - component.transform.position);
								float d = 15f;
								component.transform.position = component.transform.position + a * d * dt;
							}
						}
					}
				}
			}
		}
	}

	private void PlayerAttackInput(float dt)
	{
		if (this.InPlaceMode())
		{
			return;
		}
		ItemDrop.ItemData currentWeapon = base.GetCurrentWeapon();
		if (currentWeapon != null && currentWeapon.m_shared.m_holdDurationMin > 0f)
		{
			if (this.m_blocking || this.InMinorAction())
			{
				this.m_attackDrawTime = -1f;
				if (!string.IsNullOrEmpty(currentWeapon.m_shared.m_holdAnimationState))
				{
					this.m_zanim.SetBool(currentWeapon.m_shared.m_holdAnimationState, false);
				}
				return;
			}
			bool flag = currentWeapon.m_shared.m_holdStaminaDrain <= 0f || this.HaveStamina(0f);
			if (this.m_attackDrawTime < 0f)
			{
				if (!this.m_attackDraw)
				{
					this.m_attackDrawTime = 0f;
					return;
				}
			}
			else
			{
				if (this.m_attackDraw && flag && this.m_attackDrawTime >= 0f)
				{
					if (this.m_attackDrawTime == 0f)
					{
						if (!currentWeapon.m_shared.m_attack.StartDraw(this, currentWeapon))
						{
							this.m_attackDrawTime = -1f;
							return;
						}
						currentWeapon.m_shared.m_holdStartEffect.Create(base.transform.position, Quaternion.identity, base.transform, 1f);
					}
					this.m_attackDrawTime += Time.fixedDeltaTime;
					if (!string.IsNullOrEmpty(currentWeapon.m_shared.m_holdAnimationState))
					{
						this.m_zanim.SetBool(currentWeapon.m_shared.m_holdAnimationState, true);
					}
					this.UseStamina(currentWeapon.m_shared.m_holdStaminaDrain * dt);
					return;
				}
				if (this.m_attackDrawTime > 0f)
				{
					if (flag)
					{
						this.StartAttack(null, false);
					}
					if (!string.IsNullOrEmpty(currentWeapon.m_shared.m_holdAnimationState))
					{
						this.m_zanim.SetBool(currentWeapon.m_shared.m_holdAnimationState, false);
					}
					this.m_attackDrawTime = 0f;
					return;
				}
			}
		}
		else
		{
			if (this.m_attack)
			{
				this.m_queuedAttackTimer = 0.5f;
				this.m_queuedSecondAttackTimer = 0f;
			}
			if (this.m_secondaryAttack)
			{
				this.m_queuedSecondAttackTimer = 0.5f;
				this.m_queuedAttackTimer = 0f;
			}
			this.m_queuedAttackTimer -= Time.fixedDeltaTime;
			this.m_queuedSecondAttackTimer -= Time.fixedDeltaTime;
			if (this.m_queuedAttackTimer > 0f && this.StartAttack(null, false))
			{
				this.m_queuedAttackTimer = 0f;
			}
			if (this.m_queuedSecondAttackTimer > 0f && this.StartAttack(null, true))
			{
				this.m_queuedSecondAttackTimer = 0f;
			}
		}
	}

	protected override bool HaveQueuedChain()
	{
		return this.m_queuedAttackTimer > 0f && base.GetCurrentWeapon() != null && this.m_currentAttack != null && this.m_currentAttack.CanStartChainAttack();
	}

	private void UpdateBaseValue(float dt)
	{
		this.m_baseValueUpdatetimer += dt;
		if (this.m_baseValueUpdatetimer > 2f)
		{
			this.m_baseValueUpdatetimer = 0f;
			this.m_baseValue = EffectArea.GetBaseValue(base.transform.position, 20f);
			this.m_nview.GetZDO().Set("baseValue", this.m_baseValue);
			this.m_comfortLevel = SE_Rested.CalculateComfortLevel(this);
		}
	}

	public int GetComfortLevel()
	{
		return this.m_comfortLevel;
	}

	public int GetBaseValue()
	{
		if (!this.m_nview.IsValid())
		{
			return 0;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_baseValue;
		}
		return this.m_nview.GetZDO().GetInt("baseValue", 0);
	}

	public bool IsSafeInHome()
	{
		return this.m_safeInHome;
	}

	private void UpdateBiome(float dt)
	{
		if (this.InIntro())
		{
			return;
		}
		this.m_biomeTimer += dt;
		if (this.m_biomeTimer > 1f)
		{
			this.m_biomeTimer = 0f;
			Heightmap.Biome biome = Heightmap.FindBiome(base.transform.position);
			if (this.m_currentBiome != biome)
			{
				this.m_currentBiome = biome;
				this.AddKnownBiome(biome);
			}
		}
	}

	public Heightmap.Biome GetCurrentBiome()
	{
		return this.m_currentBiome;
	}

	public override void RaiseSkill(Skills.SkillType skill, float value = 1f)
	{
		float num = 1f;
		this.m_seman.ModifyRaiseSkill(skill, ref num);
		value *= num;
		this.m_skills.RaiseSkill(skill, value);
	}

	private void UpdateStats(float dt)
	{
		if (this.InIntro() || this.IsTeleporting())
		{
			return;
		}
		this.m_timeSinceDeath += dt;
		this.UpdateMovementModifier();
		this.UpdateFood(dt, false);
		bool flag = this.IsEncumbered();
		float maxStamina = this.GetMaxStamina();
		float num = 1f;
		if (this.IsBlocking())
		{
			num *= 0.8f;
		}
		if ((base.IsSwiming() && !base.IsOnGround()) || this.InAttack() || this.InDodge() || this.m_wallRunning || flag)
		{
			num = 0f;
		}
		float num2 = (this.m_staminaRegen + (1f - this.m_stamina / maxStamina) * this.m_staminaRegen * this.m_staminaRegenTimeMultiplier) * num;
		float num3 = 1f;
		this.m_seman.ModifyStaminaRegen(ref num3);
		num2 *= num3;
		this.m_staminaRegenTimer -= dt;
		if (this.m_stamina < maxStamina && this.m_staminaRegenTimer <= 0f)
		{
			this.m_stamina = Mathf.Min(maxStamina, this.m_stamina + num2 * dt);
		}
		this.m_nview.GetZDO().Set("stamina", this.m_stamina);
		if (flag)
		{
			if (this.m_moveDir.magnitude > 0.1f)
			{
				this.UseStamina(this.m_encumberedStaminaDrain * dt);
			}
			this.m_seman.AddStatusEffect("Encumbered", false);
			this.ShowTutorial("encumbered", false);
		}
		else
		{
			this.m_seman.RemoveStatusEffect("Encumbered", false);
		}
		if (!this.HardDeath())
		{
			this.m_seman.AddStatusEffect("SoftDeath", false);
		}
		else
		{
			this.m_seman.RemoveStatusEffect("SoftDeath", false);
		}
		this.UpdateEnvStatusEffects(dt);
	}

	private void UpdateEnvStatusEffects(float dt)
	{
		this.m_nearFireTimer += dt;
		HitData.DamageModifiers damageModifiers = base.GetDamageModifiers();
		bool flag = this.m_nearFireTimer < 0.25f;
		bool flag2 = this.m_seman.HaveStatusEffect("Burning");
		bool flag3 = this.InShelter();
		HitData.DamageModifier modifier = damageModifiers.GetModifier(HitData.DamageType.Frost);
		bool flag4 = EnvMan.instance.IsFreezing();
		bool flag5 = EnvMan.instance.IsCold();
		bool flag6 = EnvMan.instance.IsWet();
		bool flag7 = this.IsSensed();
		bool flag8 = this.m_seman.HaveStatusEffect("Wet");
		bool flag9 = base.IsSitting();
		bool flag10 = flag4 && !flag && !flag3;
		bool flag11 = (flag5 && !flag) || (flag4 && flag && !flag3) || (flag4 && !flag && flag3);
		if (modifier == HitData.DamageModifier.Resistant || modifier == HitData.DamageModifier.VeryResistant)
		{
			flag10 = false;
			flag11 = false;
		}
		if (flag6 && !this.m_underRoof)
		{
			this.m_seman.AddStatusEffect("Wet", true);
		}
		if (flag3)
		{
			this.m_seman.AddStatusEffect("Shelter", false);
		}
		else
		{
			this.m_seman.RemoveStatusEffect("Shelter", false);
		}
		if (flag)
		{
			this.m_seman.AddStatusEffect("CampFire", false);
		}
		else
		{
			this.m_seman.RemoveStatusEffect("CampFire", false);
		}
		bool flag12 = !flag7 && (flag9 || flag3) && (!flag11 & !flag10) && !flag8 && !flag2 && flag;
		if (flag12)
		{
			this.m_seman.AddStatusEffect("Resting", false);
		}
		else
		{
			this.m_seman.RemoveStatusEffect("Resting", false);
		}
		this.m_safeInHome = (flag12 && flag3);
		if (flag10)
		{
			if (!this.m_seman.RemoveStatusEffect("Cold", true))
			{
				this.m_seman.AddStatusEffect("Freezing", false);
				return;
			}
		}
		else if (flag11)
		{
			if (!this.m_seman.RemoveStatusEffect("Freezing", true) && this.m_seman.AddStatusEffect("Cold", false))
			{
				this.ShowTutorial("cold", false);
				return;
			}
		}
		else
		{
			this.m_seman.RemoveStatusEffect("Cold", false);
			this.m_seman.RemoveStatusEffect("Freezing", false);
		}
	}

	private bool CanEat(ItemDrop.ItemData item, bool showMessages)
	{
		foreach (Player.Food food in this.m_foods)
		{
			if (food.m_item.m_shared.m_name == item.m_shared.m_name)
			{
				if (food.CanEatAgain())
				{
					return true;
				}
				this.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_nomore", new string[]
				{
					item.m_shared.m_name
				}), 0, null);
				return false;
			}
		}
		using (List<Player.Food>.Enumerator enumerator = this.m_foods.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.CanEatAgain())
				{
					return true;
				}
			}
		}
		if (this.m_foods.Count >= 3)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_isfull", 0, null);
			return false;
		}
		return true;
	}

	private Player.Food GetMostDepletedFood()
	{
		Player.Food food = null;
		foreach (Player.Food food2 in this.m_foods)
		{
			if (food2.CanEatAgain() && (food == null || food2.m_health < food.m_health))
			{
				food = food2;
			}
		}
		return food;
	}

	public void ClearFood()
	{
		this.m_foods.Clear();
	}

	private bool EatFood(ItemDrop.ItemData item)
	{
		if (!this.CanEat(item, false))
		{
			return false;
		}
		foreach (Player.Food food in this.m_foods)
		{
			if (food.m_item.m_shared.m_name == item.m_shared.m_name)
			{
				if (food.CanEatAgain())
				{
					food.m_health = item.m_shared.m_food;
					food.m_stamina = item.m_shared.m_foodStamina;
					this.UpdateFood(0f, true);
					return true;
				}
				return false;
			}
		}
		if (this.m_foods.Count < 3)
		{
			Player.Food food2 = new Player.Food();
			food2.m_name = item.m_dropPrefab.name;
			food2.m_item = item;
			food2.m_health = item.m_shared.m_food;
			food2.m_stamina = item.m_shared.m_foodStamina;
			this.m_foods.Add(food2);
			this.UpdateFood(0f, true);
			return true;
		}
		Player.Food mostDepletedFood = this.GetMostDepletedFood();
		if (mostDepletedFood != null)
		{
			mostDepletedFood.m_name = item.m_dropPrefab.name;
			mostDepletedFood.m_item = item;
			mostDepletedFood.m_health = item.m_shared.m_food;
			mostDepletedFood.m_stamina = item.m_shared.m_foodStamina;
			return true;
		}
		return false;
	}

	private void UpdateFood(float dt, bool forceUpdate)
	{
		this.m_foodUpdateTimer += dt;
		if (this.m_foodUpdateTimer >= 1f || forceUpdate)
		{
			this.m_foodUpdateTimer = 0f;
			foreach (Player.Food food in this.m_foods)
			{
				food.m_health -= food.m_item.m_shared.m_food / food.m_item.m_shared.m_foodBurnTime;
				food.m_stamina -= food.m_item.m_shared.m_foodStamina / food.m_item.m_shared.m_foodBurnTime;
				if (food.m_health < 0f)
				{
					food.m_health = 0f;
				}
				if (food.m_stamina < 0f)
				{
					food.m_stamina = 0f;
				}
				if (food.m_health <= 0f)
				{
					this.Message(MessageHud.MessageType.Center, "$msg_food_done", 0, null);
					this.m_foods.Remove(food);
					break;
				}
			}
			float health;
			float stamina;
			this.GetTotalFoodValue(out health, out stamina);
			this.SetMaxHealth(health, true);
			this.SetMaxStamina(stamina, true);
		}
		if (!forceUpdate)
		{
			this.m_foodRegenTimer += dt;
			if (this.m_foodRegenTimer >= 10f)
			{
				this.m_foodRegenTimer = 0f;
				float num = 0f;
				foreach (Player.Food food2 in this.m_foods)
				{
					num += food2.m_item.m_shared.m_foodRegen;
				}
				if (num > 0f)
				{
					float num2 = 1f;
					this.m_seman.ModifyHealthRegen(ref num2);
					num *= num2;
					base.Heal(num, true);
				}
			}
		}
	}

	private void GetTotalFoodValue(out float hp, out float stamina)
	{
		hp = 25f;
		stamina = 75f;
		foreach (Player.Food food in this.m_foods)
		{
			hp += food.m_health;
			stamina += food.m_stamina;
		}
	}

	public float GetBaseFoodHP()
	{
		return 25f;
	}

	public List<Player.Food> GetFoods()
	{
		return this.m_foods;
	}

	public void OnSpawned()
	{
		this.m_spawnEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
		if (this.m_firstSpawn)
		{
			if (this.m_valkyrie != null)
			{
				UnityEngine.Object.Instantiate<GameObject>(this.m_valkyrie, base.transform.position, Quaternion.identity);
			}
			this.m_firstSpawn = false;
		}
	}

	protected override bool CheckRun(Vector3 moveDir, float dt)
	{
		if (!base.CheckRun(moveDir, dt))
		{
			return false;
		}
		bool flag = this.HaveStamina(0f);
		float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Run);
		float num = Mathf.Lerp(1f, 0.5f, skillFactor);
		float num2 = this.m_runStaminaDrain * num;
		this.m_seman.ModifyRunStaminaDrain(num2, ref num2);
		this.UseStamina(dt * num2);
		if (this.HaveStamina(0f))
		{
			this.m_runSkillImproveTimer += dt;
			if (this.m_runSkillImproveTimer > 1f)
			{
				this.m_runSkillImproveTimer = 0f;
				this.RaiseSkill(Skills.SkillType.Run, 1f);
			}
			this.AbortEquipQueue();
			return true;
		}
		if (flag)
		{
			Hud.instance.StaminaBarNoStaminaFlash();
		}
		return false;
	}

	private void UpdateMovementModifier()
	{
		this.m_equipmentMovementModifier = 0f;
		if (this.m_rightItem != null)
		{
			this.m_equipmentMovementModifier += this.m_rightItem.m_shared.m_movementModifier;
		}
		if (this.m_leftItem != null)
		{
			this.m_equipmentMovementModifier += this.m_leftItem.m_shared.m_movementModifier;
		}
		if (this.m_chestItem != null)
		{
			this.m_equipmentMovementModifier += this.m_chestItem.m_shared.m_movementModifier;
		}
		if (this.m_legItem != null)
		{
			this.m_equipmentMovementModifier += this.m_legItem.m_shared.m_movementModifier;
		}
		if (this.m_helmetItem != null)
		{
			this.m_equipmentMovementModifier += this.m_helmetItem.m_shared.m_movementModifier;
		}
		if (this.m_shoulderItem != null)
		{
			this.m_equipmentMovementModifier += this.m_shoulderItem.m_shared.m_movementModifier;
		}
		if (this.m_utilityItem != null)
		{
			this.m_equipmentMovementModifier += this.m_utilityItem.m_shared.m_movementModifier;
		}
	}

	public void OnSkillLevelup(Skills.SkillType skill, float level)
	{
		this.m_skillLevelupEffects.Create(this.m_head.position, this.m_head.rotation, this.m_head, 1f);
	}

	protected override void OnJump()
	{
		this.AbortEquipQueue();
		float num = this.m_jumpStaminaUsage - this.m_jumpStaminaUsage * this.m_equipmentMovementModifier;
		this.m_seman.ModifyJumpStaminaUsage(num, ref num);
		this.UseStamina(num);
	}

	protected override void OnSwiming(Vector3 targetVel, float dt)
	{
		base.OnSwiming(targetVel, dt);
		if (targetVel.magnitude > 0.1f)
		{
			float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Swim);
			float num = Mathf.Lerp(this.m_swimStaminaDrainMinSkill, this.m_swimStaminaDrainMaxSkill, skillFactor);
			this.UseStamina(dt * num);
			this.m_swimSkillImproveTimer += dt;
			if (this.m_swimSkillImproveTimer > 1f)
			{
				this.m_swimSkillImproveTimer = 0f;
				this.RaiseSkill(Skills.SkillType.Swim, 1f);
			}
		}
		if (!this.HaveStamina(0f))
		{
			this.m_drownDamageTimer += dt;
			if (this.m_drownDamageTimer > 1f)
			{
				this.m_drownDamageTimer = 0f;
				float damage = Mathf.Ceil(base.GetMaxHealth() / 20f);
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = damage;
				hitData.m_point = base.GetCenterPoint();
				hitData.m_dir = Vector3.down;
				hitData.m_pushForce = 10f;
				base.Damage(hitData);
				Vector3 position = base.transform.position;
				position.y = this.m_waterLevel;
				this.m_drownEffects.Create(position, base.transform.rotation, null, 1f);
			}
		}
	}

	protected override bool TakeInput()
	{
		bool result = (!Chat.instance || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !TextInput.IsVisible() && (!StoreGui.IsVisible() && !InventoryGui.IsVisible() && !Menu.IsVisible() && (!TextViewer.instance || !TextViewer.instance.IsVisible()) && !Minimap.IsOpen()) && !GameCamera.InFreeFly();
		if (this.IsDead() || this.InCutscene() || this.IsTeleporting())
		{
			result = false;
		}
		return result;
	}

	public void UseHotbarItem(int index)
	{
		ItemDrop.ItemData itemAt = this.m_inventory.GetItemAt(index - 1, 0);
		if (itemAt != null)
		{
			base.UseItem(null, itemAt, false);
		}
	}

	public bool RequiredCraftingStation(Recipe recipe, int qualityLevel, bool checkLevel)
	{
		CraftingStation requiredStation = recipe.GetRequiredStation(qualityLevel);
		if (requiredStation != null)
		{
			if (this.m_currentStation == null)
			{
				return false;
			}
			if (requiredStation.m_name != this.m_currentStation.m_name)
			{
				return false;
			}
			if (checkLevel)
			{
				int requiredStationLevel = recipe.GetRequiredStationLevel(qualityLevel);
				if (this.m_currentStation.GetLevel() < requiredStationLevel)
				{
					return false;
				}
			}
		}
		else if (this.m_currentStation != null && !this.m_currentStation.m_showBasicRecipies)
		{
			return false;
		}
		return true;
	}

	public bool HaveRequirements(Recipe recipe, bool discover, int qualityLevel)
	{
		if (discover)
		{
			if (recipe.m_craftingStation && !this.KnowStationLevel(recipe.m_craftingStation.m_name, recipe.m_minStationLevel))
			{
				return false;
			}
		}
		else if (!this.RequiredCraftingStation(recipe, qualityLevel, true))
		{
			return false;
		}
		return (recipe.m_item.m_itemData.m_shared.m_dlc.Length <= 0 || DLCMan.instance.IsDLCInstalled(recipe.m_item.m_itemData.m_shared.m_dlc)) && this.HaveRequirements(recipe.m_resources, discover, qualityLevel);
	}

	private bool HaveRequirements(Piece.Requirement[] resources, bool discover, int qualityLevel)
	{
		foreach (Piece.Requirement requirement in resources)
		{
			if (requirement.m_resItem)
			{
				if (discover)
				{
					if (requirement.m_amount > 0 && !this.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
					{
						return false;
					}
				}
				else
				{
					int amount = requirement.GetAmount(qualityLevel);
					if (this.m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < amount)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public bool HaveRequirements(Piece piece, Player.RequirementMode mode)
	{
		if (piece.m_craftingStation)
		{
			if (mode == Player.RequirementMode.IsKnown || mode == Player.RequirementMode.CanAlmostBuild)
			{
				if (!this.m_knownStations.ContainsKey(piece.m_craftingStation.m_name))
				{
					return false;
				}
			}
			else if (!CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, base.transform.position))
			{
				return false;
			}
		}
		if (piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc))
		{
			return false;
		}
		foreach (Piece.Requirement requirement in piece.m_resources)
		{
			if (requirement.m_resItem && requirement.m_amount > 0)
			{
				if (mode == Player.RequirementMode.IsKnown)
				{
					if (!this.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
					{
						return false;
					}
				}
				else if (mode == Player.RequirementMode.CanAlmostBuild)
				{
					if (!this.m_inventory.HaveItem(requirement.m_resItem.m_itemData.m_shared.m_name))
					{
						return false;
					}
				}
				else if (mode == Player.RequirementMode.CanBuild && this.m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < requirement.m_amount)
				{
					return false;
				}
			}
		}
		return true;
	}

	public void SetCraftingStation(CraftingStation station)
	{
		if (this.m_currentStation == station)
		{
			return;
		}
		if (station)
		{
			this.AddKnownStation(station);
			station.PokeInUse();
		}
		this.m_currentStation = station;
		base.HideHandItems();
		int value = this.m_currentStation ? this.m_currentStation.m_useAnimation : 0;
		this.m_zanim.SetInt("crafting", value);
	}

	public CraftingStation GetCurrentCraftingStation()
	{
		return this.m_currentStation;
	}

	public void ConsumeResources(Piece.Requirement[] requirements, int qualityLevel)
	{
		foreach (Piece.Requirement requirement in requirements)
		{
			if (requirement.m_resItem)
			{
				int amount = requirement.GetAmount(qualityLevel);
				if (amount > 0)
				{
					this.m_inventory.RemoveItem(requirement.m_resItem.m_itemData.m_shared.m_name, amount);
				}
			}
		}
	}

	private void UpdateHover()
	{
		if (this.InPlaceMode() || this.IsDead() || this.m_shipControl != null)
		{
			this.m_hovering = null;
			this.m_hoveringCreature = null;
			return;
		}
		this.FindHoverObject(out this.m_hovering, out this.m_hoveringCreature);
	}

	private bool CheckCanRemovePiece(Piece piece)
	{
		if (!this.m_noPlacementCost && piece.m_craftingStation != null && !CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, base.transform.position))
		{
			this.Message(MessageHud.MessageType.Center, "$msg_missingstation", 0, null);
			return false;
		}
		return true;
	}

	private bool RemovePiece()
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, 50f, this.m_removeRayMask) && Vector3.Distance(raycastHit.point, this.m_eye.position) < this.m_maxPlaceDistance)
		{
			Piece piece = raycastHit.collider.GetComponentInParent<Piece>();
			if (piece == null && raycastHit.collider.GetComponent<Heightmap>())
			{
				piece = TerrainModifier.FindClosestModifierPieceInRange(raycastHit.point, 2.5f);
			}
			if (piece)
			{
				if (!piece.m_canBeRemoved)
				{
					return false;
				}
				if (Location.IsInsideNoBuildLocation(piece.transform.position))
				{
					this.Message(MessageHud.MessageType.Center, "$msg_nobuildzone", 0, null);
					return false;
				}
				if (!PrivateArea.CheckAccess(piece.transform.position, 0f, true))
				{
					this.Message(MessageHud.MessageType.Center, "$msg_privatezone", 0, null);
					return false;
				}
				if (!this.CheckCanRemovePiece(piece))
				{
					return false;
				}
				ZNetView component = piece.GetComponent<ZNetView>();
				if (component == null)
				{
					return false;
				}
				if (!piece.CanBeRemoved())
				{
					this.Message(MessageHud.MessageType.Center, "$msg_cantremovenow", 0, null);
					return false;
				}
				component.ClaimOwnership();
				WearNTear component2 = piece.GetComponent<WearNTear>();
				if (component2)
				{
					component2.Destroy();
				}
				else
				{
					piece.DropResources();
					piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, piece.gameObject.transform, 1f);
					this.m_removeEffects.Create(piece.transform.position, Quaternion.identity, null, 1f);
					ZNetScene.instance.Destroy(piece.gameObject);
				}
				ItemDrop.ItemData rightItem = base.GetRightItem();
				if (rightItem != null)
				{
					this.FaceLookDirection();
					this.m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
				}
				return true;
			}
		}
		return false;
	}

	public void FaceLookDirection()
	{
		base.transform.rotation = base.GetLookYaw();
	}

	private bool PlacePiece(Piece piece)
	{
		this.UpdatePlacementGhost(true);
		Vector3 position = this.m_placementGhost.transform.position;
		Quaternion rotation = this.m_placementGhost.transform.rotation;
		GameObject gameObject = piece.gameObject;
		switch (this.m_placementStatus)
		{
		case Player.PlacementStatus.Invalid:
			this.Message(MessageHud.MessageType.Center, "$msg_invalidplacement", 0, null);
			return false;
		case Player.PlacementStatus.BlockedbyPlayer:
			this.Message(MessageHud.MessageType.Center, "$msg_blocked", 0, null);
			return false;
		case Player.PlacementStatus.NoBuildZone:
			this.Message(MessageHud.MessageType.Center, "$msg_nobuildzone", 0, null);
			return false;
		case Player.PlacementStatus.PrivateZone:
			this.Message(MessageHud.MessageType.Center, "$msg_privatezone", 0, null);
			return false;
		case Player.PlacementStatus.MoreSpace:
			this.Message(MessageHud.MessageType.Center, "$msg_needspace", 0, null);
			return false;
		case Player.PlacementStatus.NoTeleportArea:
			this.Message(MessageHud.MessageType.Center, "$msg_noteleportarea", 0, null);
			return false;
		case Player.PlacementStatus.ExtensionMissingStation:
			this.Message(MessageHud.MessageType.Center, "$msg_extensionmissingstation", 0, null);
			return false;
		case Player.PlacementStatus.WrongBiome:
			this.Message(MessageHud.MessageType.Center, "$msg_wrongbiome", 0, null);
			return false;
		case Player.PlacementStatus.NeedCultivated:
			this.Message(MessageHud.MessageType.Center, "$msg_needcultivated", 0, null);
			return false;
		case Player.PlacementStatus.NotInDungeon:
			this.Message(MessageHud.MessageType.Center, "$msg_notindungeon", 0, null);
			return false;
		default:
		{
			TerrainModifier.SetTriggerOnPlaced(true);
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, position, rotation);
			TerrainModifier.SetTriggerOnPlaced(false);
			CraftingStation componentInChildren = gameObject2.GetComponentInChildren<CraftingStation>();
			if (componentInChildren)
			{
				this.AddKnownStation(componentInChildren);
			}
			Piece component = gameObject2.GetComponent<Piece>();
			if (component)
			{
				component.SetCreator(this.GetPlayerID());
			}
			PrivateArea component2 = gameObject2.GetComponent<PrivateArea>();
			if (component2)
			{
				component2.Setup(Game.instance.GetPlayerProfile().GetName());
			}
			WearNTear component3 = gameObject2.GetComponent<WearNTear>();
			if (component3)
			{
				component3.OnPlaced();
			}
			ItemDrop.ItemData rightItem = base.GetRightItem();
			if (rightItem != null)
			{
				this.FaceLookDirection();
				this.m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
			}
			piece.m_placeEffect.Create(position, rotation, gameObject2.transform, 1f);
			base.AddNoise(50f);
			Game.instance.GetPlayerProfile().m_playerStats.m_builds++;
			ZLog.Log("Placed " + gameObject.name);
			GoogleAnalyticsV4.instance.LogEvent("Game", "PlacedPiece", gameObject.name, 0L);
			return true;
		}
		}
	}

	private void RemovePieces(Vector3 point, float range, string name, bool groundOnly)
	{
		Collider[] array = Physics.OverlapSphere(point, range + 3f);
		Piece item = null;
		float num = 0f;
		List<Piece> list = new List<Piece>();
		Collider[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Piece componentInParent = array2[i].GetComponentInParent<Piece>();
			if (componentInParent && (componentInParent.m_name == name || name == "") && (!groundOnly || componentInParent.m_groundPiece) && componentInParent.CanBeRemoved() && PrivateArea.CheckAccess(componentInParent.transform.position, 0f, true) && !(componentInParent.gameObject == this.m_placementGhost))
			{
				float num2 = Utils.DistanceXZ(point, componentInParent.transform.position);
				if (num2 <= range)
				{
					if (num2 > num)
					{
						num = num2;
						item = componentInParent;
					}
					list.Add(componentInParent);
				}
			}
		}
		list.Remove(item);
		foreach (Piece piece in list)
		{
			ZNetView component = piece.GetComponent<ZNetView>();
			component.ClaimOwnership();
			component.Destroy();
			GoogleAnalyticsV4.instance.LogEvent("Game", "RemovedPiece", piece.gameObject.name, 0L);
		}
	}

	public override bool IsPlayer()
	{
		return true;
	}

	public void GetBuildSelection(out Piece go, out Vector2Int id, out int total, out Piece.PieceCategory category, out bool useCategory)
	{
		category = this.m_buildPieces.m_selectedCategory;
		useCategory = this.m_buildPieces.m_useCategories;
		if (this.m_buildPieces.GetAvailablePiecesInSelectedCategory() == 0)
		{
			go = null;
			id = Vector2Int.zero;
			total = 0;
			return;
		}
		GameObject selectedPrefab = this.m_buildPieces.GetSelectedPrefab();
		go = (selectedPrefab ? selectedPrefab.GetComponent<Piece>() : null);
		id = this.m_buildPieces.GetSelectedIndex();
		total = this.m_buildPieces.GetAvailablePiecesInSelectedCategory();
	}

	public List<Piece> GetBuildPieces()
	{
		if (this.m_buildPieces)
		{
			return this.m_buildPieces.GetPiecesInSelectedCategory();
		}
		return null;
	}

	public int GetAvailableBuildPiecesInCategory(Piece.PieceCategory cat)
	{
		if (this.m_buildPieces)
		{
			return this.m_buildPieces.GetAvailablePiecesInCategory(cat);
		}
		return 0;
	}

	private void RPC_OnDeath(long sender)
	{
		this.m_visual.SetActive(false);
	}

	private void CreateDeathEffects()
	{
		GameObject[] array = this.m_deathEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f);
		for (int i = 0; i < array.Length; i++)
		{
			Ragdoll component = array[i].GetComponent<Ragdoll>();
			if (component)
			{
				Vector3 velocity = this.m_body.velocity;
				if (this.m_pushForce.magnitude * 0.5f > velocity.magnitude)
				{
					velocity = this.m_pushForce * 0.5f;
				}
				component.Setup(velocity, 0f, 0f, 0f, null);
				this.OnRagdollCreated(component);
				this.m_ragdoll = component;
			}
		}
	}

	public void UnequipDeathDropItems()
	{
		if (this.m_rightItem != null)
		{
			base.UnequipItem(this.m_rightItem, false);
		}
		if (this.m_leftItem != null)
		{
			base.UnequipItem(this.m_leftItem, false);
		}
		if (this.m_ammoItem != null)
		{
			base.UnequipItem(this.m_ammoItem, false);
		}
		if (this.m_utilityItem != null)
		{
			base.UnequipItem(this.m_utilityItem, false);
		}
	}

	private void CreateTombStone()
	{
		if (this.m_inventory.NrOfItems() == 0)
		{
			return;
		}
		base.UnequipAllItems();
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_tombstone, base.GetCenterPoint(), base.transform.rotation);
		gameObject.GetComponent<Container>().GetInventory().MoveInventoryToGrave(this.m_inventory);
		TombStone component = gameObject.GetComponent<TombStone>();
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
	}

	private bool HardDeath()
	{
		return this.m_timeSinceDeath > this.m_hardDeathCooldown;
	}

	protected override void OnDeath()
	{
		bool flag = this.HardDeath();
		this.m_nview.GetZDO().Set("dead", true);
		this.m_nview.InvokeRPC(ZNetView.Everybody, "OnDeath", Array.Empty<object>());
		Game.instance.GetPlayerProfile().m_playerStats.m_deaths++;
		Game.instance.GetPlayerProfile().SetDeathPoint(base.transform.position);
		this.CreateDeathEffects();
		this.CreateTombStone();
		this.m_foods.Clear();
		if (flag)
		{
			this.m_skills.OnDeath();
		}
		Game.instance.RequestRespawn(10f);
		this.m_timeSinceDeath = 0f;
		if (!flag)
		{
			this.Message(MessageHud.MessageType.TopLeft, "$msg_softdeath", 0, null);
		}
		this.Message(MessageHud.MessageType.Center, "$msg_youdied", 0, null);
		this.ShowTutorial("death", false);
		string eventLabel = "biome:" + this.GetCurrentBiome().ToString();
		GoogleAnalyticsV4.instance.LogEvent("Game", "Death", eventLabel, 0L);
	}

	public void OnRespawn()
	{
		this.m_nview.GetZDO().Set("dead", false);
		base.SetHealth(base.GetMaxHealth());
	}

	private void SetupPlacementGhost()
	{
		if (this.m_placementGhost)
		{
			UnityEngine.Object.Destroy(this.m_placementGhost);
			this.m_placementGhost = null;
		}
		if (this.m_buildPieces == null)
		{
			return;
		}
		GameObject selectedPrefab = this.m_buildPieces.GetSelectedPrefab();
		if (selectedPrefab == null)
		{
			return;
		}
		if (selectedPrefab.GetComponent<Piece>().m_repairPiece)
		{
			return;
		}
		bool enabled = false;
		TerrainModifier componentInChildren = selectedPrefab.GetComponentInChildren<TerrainModifier>();
		if (componentInChildren)
		{
			enabled = componentInChildren.enabled;
			componentInChildren.enabled = false;
		}
		ZNetView.m_forceDisableInit = true;
		this.m_placementGhost = UnityEngine.Object.Instantiate<GameObject>(selectedPrefab);
		ZNetView.m_forceDisableInit = false;
		this.m_placementGhost.name = selectedPrefab.name;
		if (componentInChildren)
		{
			componentInChildren.enabled = enabled;
		}
		Joint[] componentsInChildren = this.m_placementGhost.GetComponentsInChildren<Joint>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren[i]);
		}
		Rigidbody[] componentsInChildren2 = this.m_placementGhost.GetComponentsInChildren<Rigidbody>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren2[i]);
		}
		foreach (Collider collider in this.m_placementGhost.GetComponentsInChildren<Collider>())
		{
			if ((1 << collider.gameObject.layer & this.m_placeRayMask) == 0)
			{
				ZLog.Log("Disabling " + collider.gameObject.name + "  " + LayerMask.LayerToName(collider.gameObject.layer));
				collider.enabled = false;
			}
		}
		Transform[] componentsInChildren4 = this.m_placementGhost.GetComponentsInChildren<Transform>();
		int layer = LayerMask.NameToLayer("ghost");
		Transform[] array = componentsInChildren4;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.layer = layer;
		}
		TerrainModifier[] componentsInChildren5 = this.m_placementGhost.GetComponentsInChildren<TerrainModifier>();
		for (int i = 0; i < componentsInChildren5.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren5[i]);
		}
		GuidePoint[] componentsInChildren6 = this.m_placementGhost.GetComponentsInChildren<GuidePoint>();
		for (int i = 0; i < componentsInChildren6.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren6[i]);
		}
		Light[] componentsInChildren7 = this.m_placementGhost.GetComponentsInChildren<Light>();
		for (int i = 0; i < componentsInChildren7.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren7[i]);
		}
		AudioSource[] componentsInChildren8 = this.m_placementGhost.GetComponentsInChildren<AudioSource>();
		for (int i = 0; i < componentsInChildren8.Length; i++)
		{
			componentsInChildren8[i].enabled = false;
		}
		ZSFX[] componentsInChildren9 = this.m_placementGhost.GetComponentsInChildren<ZSFX>();
		for (int i = 0; i < componentsInChildren9.Length; i++)
		{
			componentsInChildren9[i].enabled = false;
		}
		Windmill componentInChildren2 = this.m_placementGhost.GetComponentInChildren<Windmill>();
		if (componentInChildren2)
		{
			componentInChildren2.enabled = false;
		}
		ParticleSystem[] componentsInChildren10 = this.m_placementGhost.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < componentsInChildren10.Length; i++)
		{
			componentsInChildren10[i].gameObject.SetActive(false);
		}
		Transform transform = this.m_placementGhost.transform.Find("_GhostOnly");
		if (transform)
		{
			transform.gameObject.SetActive(true);
		}
		this.m_placementGhost.transform.position = base.transform.position;
		this.m_placementGhost.transform.localScale = selectedPrefab.transform.localScale;
		foreach (MeshRenderer meshRenderer in this.m_placementGhost.GetComponentsInChildren<MeshRenderer>())
		{
			if (!(meshRenderer.sharedMaterial == null))
			{
				Material[] sharedMaterials = meshRenderer.sharedMaterials;
				for (int j = 0; j < sharedMaterials.Length; j++)
				{
					Material material = new Material(sharedMaterials[j]);
					material.SetFloat("_RippleDistance", 0f);
					material.SetFloat("_ValueNoise", 0f);
					sharedMaterials[j] = material;
				}
				meshRenderer.sharedMaterials = sharedMaterials;
				meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			}
		}
	}

	private void SetPlacementGhostValid(bool valid)
	{
		this.m_placementGhost.GetComponent<Piece>().SetInvalidPlacementHeightlight(!valid);
	}

	protected override void SetPlaceMode(PieceTable buildPieces)
	{
		base.SetPlaceMode(buildPieces);
		this.m_buildPieces = buildPieces;
		this.UpdateAvailablePiecesList();
	}

	public void SetBuildCategory(int index)
	{
		if (this.m_buildPieces != null)
		{
			this.m_buildPieces.SetCategory(index);
			this.UpdateAvailablePiecesList();
		}
	}

	public override bool InPlaceMode()
	{
		return this.m_buildPieces != null;
	}

	private void Repair(ItemDrop.ItemData toolItem, Piece repairPiece)
	{
		if (!this.InPlaceMode())
		{
			return;
		}
		Piece hoveringPiece = this.GetHoveringPiece();
		if (hoveringPiece)
		{
			if (!this.CheckCanRemovePiece(hoveringPiece))
			{
				return;
			}
			if (!PrivateArea.CheckAccess(hoveringPiece.transform.position, 0f, true))
			{
				return;
			}
			bool flag = false;
			WearNTear component = hoveringPiece.GetComponent<WearNTear>();
			if (component && component.Repair())
			{
				flag = true;
			}
			if (flag)
			{
				this.FaceLookDirection();
				this.m_zanim.SetTrigger(toolItem.m_shared.m_attack.m_attackAnimation);
				hoveringPiece.m_placeEffect.Create(hoveringPiece.transform.position, hoveringPiece.transform.rotation, null, 1f);
				this.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_repaired", new string[]
				{
					hoveringPiece.m_name
				}), 0, null);
				this.UseStamina(toolItem.m_shared.m_attack.m_attackStamina);
				if (toolItem.m_shared.m_useDurability)
				{
					toolItem.m_durability -= toolItem.m_shared.m_useDurabilityDrain;
					return;
				}
			}
			else
			{
				this.Message(MessageHud.MessageType.TopLeft, hoveringPiece.m_name + " $msg_doesnotneedrepair", 0, null);
			}
		}
	}

	private void UpdateWearNTearHover()
	{
		if (!this.InPlaceMode())
		{
			this.m_hoveringPiece = null;
			return;
		}
		this.m_hoveringPiece = null;
		RaycastHit raycastHit;
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, 50f, this.m_removeRayMask) && Vector3.Distance(this.m_eye.position, raycastHit.point) < this.m_maxPlaceDistance)
		{
			Piece componentInParent = raycastHit.collider.GetComponentInParent<Piece>();
			this.m_hoveringPiece = componentInParent;
			if (componentInParent)
			{
				WearNTear component = componentInParent.GetComponent<WearNTear>();
				if (component)
				{
					component.Highlight();
				}
			}
		}
	}

	public Piece GetHoveringPiece()
	{
		if (this.InPlaceMode())
		{
			return this.m_hoveringPiece;
		}
		return null;
	}

	private void UpdatePlacementGhost(bool flashGuardStone)
	{
		if (this.m_placementGhost == null)
		{
			if (this.m_placementMarkerInstance)
			{
				this.m_placementMarkerInstance.SetActive(false);
			}
			return;
		}
		bool flag = ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace");
		Piece component = this.m_placementGhost.GetComponent<Piece>();
		bool water = component.m_waterPiece || component.m_noInWater;
		Vector3 vector;
		Vector3 up;
		Piece piece;
		Heightmap heightmap;
		Collider x;
		if (this.PieceRayTest(out vector, out up, out piece, out heightmap, out x, water))
		{
			this.m_placementStatus = Player.PlacementStatus.Valid;
			if (this.m_placementMarkerInstance == null)
			{
				this.m_placementMarkerInstance = UnityEngine.Object.Instantiate<GameObject>(this.m_placeMarker, vector, Quaternion.identity);
			}
			this.m_placementMarkerInstance.SetActive(true);
			this.m_placementMarkerInstance.transform.position = vector;
			this.m_placementMarkerInstance.transform.rotation = Quaternion.LookRotation(up);
			if (component.m_groundOnly || component.m_groundPiece || component.m_cultivatedGroundOnly)
			{
				this.m_placementMarkerInstance.SetActive(false);
			}
			WearNTear wearNTear = (piece != null) ? piece.GetComponent<WearNTear>() : null;
			StationExtension component2 = component.GetComponent<StationExtension>();
			if (component2 != null)
			{
				CraftingStation craftingStation = component2.FindClosestStationInRange(vector);
				if (craftingStation)
				{
					component2.StartConnectionEffect(craftingStation);
				}
				else
				{
					component2.StopConnectionEffect();
					this.m_placementStatus = Player.PlacementStatus.ExtensionMissingStation;
				}
				if (component2.OtherExtensionInRange(component.m_spaceRequirement))
				{
					this.m_placementStatus = Player.PlacementStatus.MoreSpace;
				}
			}
			if (wearNTear && !wearNTear.m_supports)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_waterPiece && x == null && !flag)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_noInWater && x != null)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_groundPiece && heightmap == null)
			{
				this.m_placementGhost.SetActive(false);
				this.m_placementStatus = Player.PlacementStatus.Invalid;
				return;
			}
			if (component.m_groundOnly && heightmap == null)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_cultivatedGroundOnly && (heightmap == null || !heightmap.IsCultivated(vector)))
			{
				this.m_placementStatus = Player.PlacementStatus.NeedCultivated;
			}
			if (component.m_notOnWood && piece && wearNTear && wearNTear.m_materialType == WearNTear.MaterialType.Wood)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_notOnTiltingSurface && up.y < 0.8f)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_inCeilingOnly && up.y > -0.5f)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_notOnFloor && up.y > 0.1f)
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
			if (component.m_onlyInTeleportArea && !EffectArea.IsPointInsideArea(vector, EffectArea.Type.Teleport, 0f))
			{
				this.m_placementStatus = Player.PlacementStatus.NoTeleportArea;
			}
			if (!component.m_allowedInDungeons && base.InInterior())
			{
				this.m_placementStatus = Player.PlacementStatus.NotInDungeon;
			}
			if (heightmap)
			{
				up = Vector3.up;
			}
			this.m_placementGhost.SetActive(true);
			Quaternion rotation = Quaternion.Euler(0f, 22.5f * (float)this.m_placeRotation, 0f);
			if (((component.m_groundPiece || component.m_clipGround) && heightmap) || component.m_clipEverything)
			{
				if (this.m_buildPieces.GetSelectedPrefab().GetComponent<TerrainModifier>() && component.m_allowAltGroundPlacement && component.m_groundPiece && !ZInput.GetButton("AltPlace") && !ZInput.GetButton("JoyAltPlace"))
				{
					float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
					vector.y = groundHeight;
				}
				this.m_placementGhost.transform.position = vector;
				this.m_placementGhost.transform.rotation = rotation;
			}
			else
			{
				Collider[] componentsInChildren = this.m_placementGhost.GetComponentsInChildren<Collider>();
				if (componentsInChildren.Length != 0)
				{
					this.m_placementGhost.transform.position = vector + up * 50f;
					this.m_placementGhost.transform.rotation = rotation;
					Vector3 b = Vector3.zero;
					float num = 999999f;
					foreach (Collider collider in componentsInChildren)
					{
						if (!collider.isTrigger && collider.enabled)
						{
							MeshCollider meshCollider = collider as MeshCollider;
							if (!(meshCollider != null) || meshCollider.convex)
							{
								Vector3 vector2 = collider.ClosestPoint(vector);
								float num2 = Vector3.Distance(vector2, vector);
								if (num2 < num)
								{
									b = vector2;
									num = num2;
								}
							}
						}
					}
					Vector3 b2 = this.m_placementGhost.transform.position - b;
					if (component.m_waterPiece)
					{
						b2.y = 3f;
					}
					this.m_placementGhost.transform.position = vector + b2;
					this.m_placementGhost.transform.rotation = rotation;
				}
			}
			if (!flag)
			{
				this.m_tempPieces.Clear();
				Transform transform;
				Transform transform2;
				if (this.FindClosestSnapPoints(this.m_placementGhost.transform, 0.5f, out transform, out transform2, this.m_tempPieces))
				{
					Vector3 position = transform2.parent.position;
					Vector3 vector3 = transform2.position - (transform.position - this.m_placementGhost.transform.position);
					if (!this.IsOverlapingOtherPiece(vector3, this.m_placementGhost.name, this.m_tempPieces))
					{
						this.m_placementGhost.transform.position = vector3;
					}
				}
			}
			if (Location.IsInsideNoBuildLocation(this.m_placementGhost.transform.position))
			{
				this.m_placementStatus = Player.PlacementStatus.NoBuildZone;
			}
			float radius = component.GetComponent<PrivateArea>() ? component.GetComponent<PrivateArea>().m_radius : 0f;
			if (!PrivateArea.CheckAccess(this.m_placementGhost.transform.position, radius, flashGuardStone))
			{
				this.m_placementStatus = Player.PlacementStatus.PrivateZone;
			}
			if (this.CheckPlacementGhostVSPlayers())
			{
				this.m_placementStatus = Player.PlacementStatus.BlockedbyPlayer;
			}
			if (component.m_onlyInBiome != Heightmap.Biome.None && (Heightmap.FindBiome(this.m_placementGhost.transform.position) & component.m_onlyInBiome) == Heightmap.Biome.None)
			{
				this.m_placementStatus = Player.PlacementStatus.WrongBiome;
			}
			if (component.m_noClipping && this.TestGhostClipping(this.m_placementGhost, 0.2f))
			{
				this.m_placementStatus = Player.PlacementStatus.Invalid;
			}
		}
		else
		{
			if (this.m_placementMarkerInstance)
			{
				this.m_placementMarkerInstance.SetActive(false);
			}
			this.m_placementGhost.SetActive(false);
			this.m_placementStatus = Player.PlacementStatus.Invalid;
		}
		this.SetPlacementGhostValid(this.m_placementStatus == Player.PlacementStatus.Valid);
	}

	private bool IsOverlapingOtherPiece(Vector3 p, string pieceName, List<Piece> pieces)
	{
		foreach (Piece piece in this.m_tempPieces)
		{
			if (Vector3.Distance(p, piece.transform.position) < 0.05f && piece.gameObject.name.StartsWith(pieceName))
			{
				return true;
			}
		}
		return false;
	}

	private bool FindClosestSnapPoints(Transform ghost, float maxSnapDistance, out Transform a, out Transform b, List<Piece> pieces)
	{
		this.m_tempSnapPoints1.Clear();
		ghost.GetComponent<Piece>().GetSnapPoints(this.m_tempSnapPoints1);
		this.m_tempSnapPoints2.Clear();
		this.m_tempPieces.Clear();
		Piece.GetSnapPoints(ghost.transform.position, 10f, this.m_tempSnapPoints2, this.m_tempPieces);
		float num = 9999999f;
		a = null;
		b = null;
		foreach (Transform transform in this.m_tempSnapPoints1)
		{
			Transform transform2;
			float num2;
			if (this.FindClosestSnappoint(transform.position, this.m_tempSnapPoints2, maxSnapDistance, out transform2, out num2) && num2 < num)
			{
				num = num2;
				a = transform;
				b = transform2;
			}
		}
		return a != null;
	}

	private bool FindClosestSnappoint(Vector3 p, List<Transform> snapPoints, float maxDistance, out Transform closest, out float distance)
	{
		closest = null;
		distance = 999999f;
		foreach (Transform transform in snapPoints)
		{
			float num = Vector3.Distance(transform.position, p);
			if (num <= maxDistance && num < distance)
			{
				closest = transform;
				distance = num;
			}
		}
		return closest != null;
	}

	private bool TestGhostClipping(GameObject ghost, float maxPenetration)
	{
		Collider[] componentsInChildren = ghost.GetComponentsInChildren<Collider>();
		Collider[] array = Physics.OverlapSphere(ghost.transform.position, 10f, this.m_placeRayMask);
		foreach (Collider collider in componentsInChildren)
		{
			foreach (Collider collider2 in array)
			{
				Vector3 vector;
				float num;
				if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, collider2, collider2.transform.position, collider2.transform.rotation, out vector, out num) && num > maxPenetration)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool CheckPlacementGhostVSPlayers()
	{
		if (this.m_placementGhost == null)
		{
			return false;
		}
		List<Character> list = new List<Character>();
		Character.GetCharactersInRange(base.transform.position, 30f, list);
		foreach (Collider collider in this.m_placementGhost.GetComponentsInChildren<Collider>())
		{
			if (!collider.isTrigger && collider.enabled)
			{
				MeshCollider meshCollider = collider as MeshCollider;
				if (!(meshCollider != null) || meshCollider.convex)
				{
					foreach (Character character in list)
					{
						CapsuleCollider collider2 = character.GetCollider();
						Vector3 vector;
						float num;
						if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, collider2, collider2.transform.position, collider2.transform.rotation, out vector, out num))
						{
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	private bool PieceRayTest(out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
	{
		int layerMask = this.m_placeRayMask;
		if (water)
		{
			layerMask = this.m_placeWaterRayMask;
		}
		RaycastHit raycastHit;
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, 50f, layerMask) && raycastHit.collider && !raycastHit.collider.attachedRigidbody && Vector3.Distance(this.m_eye.position, raycastHit.point) < this.m_maxPlaceDistance)
		{
			point = raycastHit.point;
			normal = raycastHit.normal;
			piece = raycastHit.collider.GetComponentInParent<Piece>();
			heightmap = raycastHit.collider.GetComponent<Heightmap>();
			if (raycastHit.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
			{
				waterSurface = raycastHit.collider;
			}
			else
			{
				waterSurface = null;
			}
			return true;
		}
		point = Vector3.zero;
		normal = Vector3.zero;
		piece = null;
		heightmap = null;
		waterSurface = null;
		return false;
	}

	private void FindHoverObject(out GameObject hover, out Character hoverCreature)
	{
		hover = null;
		hoverCreature = null;
		RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, 50f, this.m_interactMask);
		Array.Sort<RaycastHit>(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
		RaycastHit[] array2 = array;
		int i = 0;
		while (i < array2.Length)
		{
			RaycastHit raycastHit = array2[i];
			if (!raycastHit.collider.attachedRigidbody || !(raycastHit.collider.attachedRigidbody.gameObject == base.gameObject))
			{
				if (hoverCreature == null)
				{
					Character character = raycastHit.collider.attachedRigidbody ? raycastHit.collider.attachedRigidbody.GetComponent<Character>() : raycastHit.collider.GetComponent<Character>();
					if (character != null)
					{
						hoverCreature = character;
					}
				}
				if (Vector3.Distance(this.m_eye.position, raycastHit.point) >= this.m_maxInteractDistance)
				{
					break;
				}
				if (raycastHit.collider.GetComponent<Hoverable>() != null)
				{
					hover = raycastHit.collider.gameObject;
					return;
				}
				if (raycastHit.collider.attachedRigidbody)
				{
					hover = raycastHit.collider.attachedRigidbody.gameObject;
					return;
				}
				hover = raycastHit.collider.gameObject;
				return;
			}
			else
			{
				i++;
			}
		}
	}

	private void Interact(GameObject go, bool hold)
	{
		if (this.InAttack() || this.InDodge())
		{
			return;
		}
		if (hold && Time.time - this.m_lastHoverInteractTime < 0.2f)
		{
			return;
		}
		Interactable componentInParent = go.GetComponentInParent<Interactable>();
		if (componentInParent != null)
		{
			this.m_lastHoverInteractTime = Time.time;
			if (componentInParent.Interact(this, hold))
			{
				Vector3 forward = go.transform.position - base.transform.position;
				forward.y = 0f;
				forward.Normalize();
				base.transform.rotation = Quaternion.LookRotation(forward);
				this.m_zanim.SetTrigger("interact");
			}
		}
	}

	private void UpdateStations(float dt)
	{
		this.m_stationDiscoverTimer += dt;
		if (this.m_stationDiscoverTimer > 1f)
		{
			this.m_stationDiscoverTimer = 0f;
			CraftingStation.UpdateKnownStationsInRange(this);
		}
		if (this.m_currentStation != null)
		{
			if (!this.m_currentStation.InUseDistance(this))
			{
				InventoryGui.instance.Hide();
				this.SetCraftingStation(null);
				return;
			}
			if (!InventoryGui.IsVisible())
			{
				this.SetCraftingStation(null);
				return;
			}
			this.m_currentStation.PokeInUse();
			if (this.m_currentStation && !this.AlwaysRotateCamera())
			{
				Vector3 normalized = (this.m_currentStation.transform.position - base.transform.position).normalized;
				normalized.y = 0f;
				normalized.Normalize();
				Quaternion to = Quaternion.LookRotation(normalized);
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, this.m_turnSpeed * dt);
			}
		}
	}

	private void UpdateCover(float dt)
	{
		this.m_updateCoverTimer += dt;
		if (this.m_updateCoverTimer > 1f)
		{
			this.m_updateCoverTimer = 0f;
			Cover.GetCoverForPoint(base.GetCenterPoint(), out this.m_coverPercentage, out this.m_underRoof);
		}
	}

	public Character GetHoverCreature()
	{
		return this.m_hoveringCreature;
	}

	public override GameObject GetHoverObject()
	{
		return this.m_hovering;
	}

	public override void OnNearFire(Vector3 point)
	{
		this.m_nearFireTimer = 0f;
	}

	public bool InShelter()
	{
		return this.m_coverPercentage >= 0.8f && this.m_underRoof;
	}

	public float GetStamina()
	{
		return this.m_stamina;
	}

	public override float GetMaxStamina()
	{
		return this.m_maxStamina;
	}

	public override float GetStaminaPercentage()
	{
		return this.m_stamina / this.m_maxStamina;
	}

	public void SetGodMode(bool godMode)
	{
		this.m_godMode = godMode;
	}

	public override bool InGodMode()
	{
		return this.m_godMode;
	}

	public void SetGhostMode(bool ghostmode)
	{
		this.m_ghostMode = ghostmode;
	}

	public override bool InGhostMode()
	{
		return this.m_ghostMode;
	}

	public override bool IsDebugFlying()
	{
		if (this.m_nview.IsOwner())
		{
			return this.m_debugFly;
		}
		return this.m_nview.GetZDO().GetBool("DebugFly", false);
	}

	public override void AddStamina(float v)
	{
		this.m_stamina += v;
		if (this.m_stamina > this.m_maxStamina)
		{
			this.m_stamina = this.m_maxStamina;
		}
	}

	public override void UseStamina(float v)
	{
		if (v == 0f)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.RPC_UseStamina(0L, v);
			return;
		}
		this.m_nview.InvokeRPC("UseStamina", new object[]
		{
			v
		});
	}

	private void RPC_UseStamina(long sender, float v)
	{
		if (v == 0f)
		{
			return;
		}
		this.m_stamina -= v;
		if (this.m_stamina < 0f)
		{
			this.m_stamina = 0f;
		}
		this.m_staminaRegenTimer = this.m_staminaRegenDelay;
	}

	public override bool HaveStamina(float amount = 0f)
	{
		if (this.m_nview.IsValid() && !this.m_nview.IsOwner())
		{
			return this.m_nview.GetZDO().GetFloat("stamina", this.m_maxStamina) > amount;
		}
		return this.m_stamina > amount;
	}

	public void Save(ZPackage pkg)
	{
		pkg.Write(24);
		pkg.Write(base.GetMaxHealth());
		pkg.Write(base.GetHealth());
		pkg.Write(this.GetMaxStamina());
		pkg.Write(this.m_firstSpawn);
		pkg.Write(this.m_timeSinceDeath);
		pkg.Write(this.m_guardianPower);
		pkg.Write(this.m_guardianPowerCooldown);
		this.m_inventory.Save(pkg);
		pkg.Write(this.m_knownRecipes.Count);
		foreach (string data in this.m_knownRecipes)
		{
			pkg.Write(data);
		}
		pkg.Write(this.m_knownStations.Count);
		foreach (KeyValuePair<string, int> keyValuePair in this.m_knownStations)
		{
			pkg.Write(keyValuePair.Key);
			pkg.Write(keyValuePair.Value);
		}
		pkg.Write(this.m_knownMaterial.Count);
		foreach (string data2 in this.m_knownMaterial)
		{
			pkg.Write(data2);
		}
		pkg.Write(this.m_shownTutorials.Count);
		foreach (string data3 in this.m_shownTutorials)
		{
			pkg.Write(data3);
		}
		pkg.Write(this.m_uniques.Count);
		foreach (string data4 in this.m_uniques)
		{
			pkg.Write(data4);
		}
		pkg.Write(this.m_trophies.Count);
		foreach (string data5 in this.m_trophies)
		{
			pkg.Write(data5);
		}
		pkg.Write(this.m_knownBiome.Count);
		foreach (Heightmap.Biome data6 in this.m_knownBiome)
		{
			pkg.Write((int)data6);
		}
		pkg.Write(this.m_knownTexts.Count);
		foreach (KeyValuePair<string, string> keyValuePair2 in this.m_knownTexts)
		{
			pkg.Write(keyValuePair2.Key);
			pkg.Write(keyValuePair2.Value);
		}
		pkg.Write(this.m_beardItem);
		pkg.Write(this.m_hairItem);
		pkg.Write(this.m_skinColor);
		pkg.Write(this.m_hairColor);
		pkg.Write(this.m_modelIndex);
		pkg.Write(this.m_foods.Count);
		foreach (Player.Food food in this.m_foods)
		{
			pkg.Write(food.m_name);
			pkg.Write(food.m_health);
			pkg.Write(food.m_stamina);
		}
		this.m_skills.Save(pkg);
	}

	public void Load(ZPackage pkg)
	{
		this.m_isLoading = true;
		base.UnequipAllItems();
		int num = pkg.ReadInt();
		if (num >= 7)
		{
			this.SetMaxHealth(pkg.ReadSingle(), false);
		}
		float num2 = pkg.ReadSingle();
		float maxHealth = base.GetMaxHealth();
		if (num2 <= 0f || num2 > maxHealth || float.IsNaN(num2))
		{
			num2 = maxHealth;
		}
		base.SetHealth(num2);
		if (num >= 10)
		{
			float stamina = pkg.ReadSingle();
			this.SetMaxStamina(stamina, false);
			this.m_stamina = stamina;
		}
		if (num >= 8)
		{
			this.m_firstSpawn = pkg.ReadBool();
		}
		if (num >= 20)
		{
			this.m_timeSinceDeath = pkg.ReadSingle();
		}
		if (num >= 23)
		{
			string guardianPower = pkg.ReadString();
			this.SetGuardianPower(guardianPower);
		}
		if (num >= 24)
		{
			this.m_guardianPowerCooldown = pkg.ReadSingle();
		}
		if (num == 2)
		{
			pkg.ReadZDOID();
		}
		this.m_inventory.Load(pkg);
		int num3 = pkg.ReadInt();
		for (int i = 0; i < num3; i++)
		{
			string item = pkg.ReadString();
			this.m_knownRecipes.Add(item);
		}
		if (num < 15)
		{
			int num4 = pkg.ReadInt();
			for (int j = 0; j < num4; j++)
			{
				pkg.ReadString();
			}
		}
		else
		{
			int num5 = pkg.ReadInt();
			for (int k = 0; k < num5; k++)
			{
				string key = pkg.ReadString();
				int value = pkg.ReadInt();
				this.m_knownStations.Add(key, value);
			}
		}
		int num6 = pkg.ReadInt();
		for (int l = 0; l < num6; l++)
		{
			string item2 = pkg.ReadString();
			this.m_knownMaterial.Add(item2);
		}
		if (num < 19 || num >= 21)
		{
			int num7 = pkg.ReadInt();
			for (int m = 0; m < num7; m++)
			{
				string item3 = pkg.ReadString();
				this.m_shownTutorials.Add(item3);
			}
		}
		if (num >= 6)
		{
			int num8 = pkg.ReadInt();
			for (int n = 0; n < num8; n++)
			{
				string item4 = pkg.ReadString();
				this.m_uniques.Add(item4);
			}
		}
		if (num >= 9)
		{
			int num9 = pkg.ReadInt();
			for (int num10 = 0; num10 < num9; num10++)
			{
				string item5 = pkg.ReadString();
				this.m_trophies.Add(item5);
			}
		}
		if (num >= 18)
		{
			int num11 = pkg.ReadInt();
			for (int num12 = 0; num12 < num11; num12++)
			{
				Heightmap.Biome item6 = (Heightmap.Biome)pkg.ReadInt();
				this.m_knownBiome.Add(item6);
			}
		}
		if (num >= 22)
		{
			int num13 = pkg.ReadInt();
			for (int num14 = 0; num14 < num13; num14++)
			{
				string key2 = pkg.ReadString();
				string value2 = pkg.ReadString();
				this.m_knownTexts.Add(key2, value2);
			}
		}
		if (num >= 4)
		{
			string beard = pkg.ReadString();
			string hair = pkg.ReadString();
			base.SetBeard(beard);
			base.SetHair(hair);
		}
		if (num >= 5)
		{
			Vector3 skinColor = pkg.ReadVector3();
			Vector3 hairColor = pkg.ReadVector3();
			this.SetSkinColor(skinColor);
			this.SetHairColor(hairColor);
		}
		if (num >= 11)
		{
			int playerModel = pkg.ReadInt();
			this.SetPlayerModel(playerModel);
		}
		if (num >= 12)
		{
			this.m_foods.Clear();
			int num15 = pkg.ReadInt();
			for (int num16 = 0; num16 < num15; num16++)
			{
				if (num >= 14)
				{
					Player.Food food = new Player.Food();
					food.m_name = pkg.ReadString();
					food.m_health = pkg.ReadSingle();
					if (num >= 16)
					{
						food.m_stamina = pkg.ReadSingle();
					}
					GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(food.m_name);
					if (itemPrefab == null)
					{
						ZLog.LogWarning("FAiled to find food item " + food.m_name);
					}
					else
					{
						food.m_item = itemPrefab.GetComponent<ItemDrop>().m_itemData;
						this.m_foods.Add(food);
					}
				}
				else
				{
					pkg.ReadString();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					if (num >= 13)
					{
						pkg.ReadSingle();
					}
				}
			}
		}
		if (num >= 17)
		{
			this.m_skills.Load(pkg);
		}
		this.m_isLoading = false;
		this.UpdateAvailablePiecesList();
		this.EquipIventoryItems();
	}

	private void EquipIventoryItems()
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory.GetEquipedtems())
		{
			if (!base.EquipItem(itemData, false))
			{
				itemData.m_equiped = false;
			}
		}
	}

	public override bool CanMove()
	{
		return !this.m_teleporting && !this.InCutscene() && (!this.IsEncumbered() || this.HaveStamina(0f)) && base.CanMove();
	}

	public override bool IsEncumbered()
	{
		return this.m_inventory.GetTotalWeight() > this.GetMaxCarryWeight();
	}

	public float GetMaxCarryWeight()
	{
		float maxCarryWeight = this.m_maxCarryWeight;
		this.m_seman.ModifyMaxCarryWeight(maxCarryWeight, ref maxCarryWeight);
		return maxCarryWeight;
	}

	public override bool HaveUniqueKey(string name)
	{
		return this.m_uniques.Contains(name);
	}

	public override void AddUniqueKey(string name)
	{
		if (!this.m_uniques.Contains(name))
		{
			this.m_uniques.Add(name);
		}
	}

	public bool IsBiomeKnown(Heightmap.Biome biome)
	{
		return this.m_knownBiome.Contains(biome);
	}

	public void AddKnownBiome(Heightmap.Biome biome)
	{
		if (!this.m_knownBiome.Contains(biome))
		{
			this.m_knownBiome.Add(biome);
			if (biome != Heightmap.Biome.Meadows && biome != Heightmap.Biome.None)
			{
				string text = "$biome_" + biome.ToString().ToLower();
				MessageHud.instance.ShowBiomeFoundMsg(text, true);
			}
			if (biome == Heightmap.Biome.BlackForest && !ZoneSystem.instance.GetGlobalKey("defeated_eikthyr"))
			{
				this.ShowTutorial("blackforest", false);
			}
			GoogleAnalyticsV4.instance.LogEvent("Game", "BiomeFound", biome.ToString(), 0L);
		}
	}

	public bool IsRecipeKnown(string name)
	{
		return this.m_knownRecipes.Contains(name);
	}

	public void AddKnownRecipe(Recipe recipe)
	{
		if (!this.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name))
		{
			this.m_knownRecipes.Add(recipe.m_item.m_itemData.m_shared.m_name);
			MessageHud.instance.QueueUnlockMsg(recipe.m_item.m_itemData.GetIcon(), "$msg_newrecipe", recipe.m_item.m_itemData.m_shared.m_name);
			GoogleAnalyticsV4.instance.LogEvent("Game", "RecipeFound", recipe.m_item.m_itemData.m_shared.m_name, 0L);
		}
	}

	public void AddKnownPiece(Piece piece)
	{
		if (!this.m_knownRecipes.Contains(piece.m_name))
		{
			this.m_knownRecipes.Add(piece.m_name);
			MessageHud.instance.QueueUnlockMsg(piece.m_icon, "$msg_newpiece", piece.m_name);
			GoogleAnalyticsV4.instance.LogEvent("Game", "PieceFound", piece.m_name, 0L);
		}
	}

	public void AddKnownStation(CraftingStation station)
	{
		int level = station.GetLevel();
		int num;
		if (this.m_knownStations.TryGetValue(station.m_name, out num))
		{
			if (num < level)
			{
				this.m_knownStations[station.m_name] = level;
				MessageHud.instance.QueueUnlockMsg(station.m_icon, "$msg_newstation_level", station.m_name + " $msg_level " + level);
				this.UpdateKnownRecipesList();
			}
			return;
		}
		this.m_knownStations.Add(station.m_name, level);
		MessageHud.instance.QueueUnlockMsg(station.m_icon, "$msg_newstation", station.m_name);
		GoogleAnalyticsV4.instance.LogEvent("Game", "StationFound", station.m_name, 0L);
		this.UpdateKnownRecipesList();
	}

	private bool KnowStationLevel(string name, int level)
	{
		int num;
		return this.m_knownStations.TryGetValue(name, out num) && num >= level;
	}

	public void AddKnownText(string label, string text)
	{
		if (label.Length == 0)
		{
			ZLog.LogWarning("Text " + text + " Is missing label");
			return;
		}
		if (!this.m_knownTexts.ContainsKey(label))
		{
			this.m_knownTexts.Add(label, text);
			this.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_newtext", new string[]
			{
				label
			}), 0, this.m_textIcon);
		}
	}

	public List<KeyValuePair<string, string>> GetKnownTexts()
	{
		return this.m_knownTexts.ToList<KeyValuePair<string, string>>();
	}

	public void AddKnownItem(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie)
		{
			this.AddTrophie(item);
		}
		if (!this.m_knownMaterial.Contains(item.m_shared.m_name))
		{
			this.m_knownMaterial.Add(item.m_shared.m_name);
			if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material)
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newmaterial", item.m_shared.m_name);
			}
			else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie)
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newtrophy", item.m_shared.m_name);
			}
			else
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newitem", item.m_shared.m_name);
			}
			GoogleAnalyticsV4.instance.LogEvent("Game", "ItemFound", item.m_shared.m_name, 0L);
			this.UpdateKnownRecipesList();
		}
	}

	private void AddTrophie(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophie)
		{
			return;
		}
		if (!this.m_trophies.Contains(item.m_dropPrefab.name))
		{
			this.m_trophies.Add(item.m_dropPrefab.name);
		}
	}

	public List<string> GetTrophies()
	{
		List<string> list = new List<string>();
		list.AddRange(this.m_trophies);
		return list;
	}

	private void UpdateKnownRecipesList()
	{
		if (Game.instance == null)
		{
			return;
		}
		foreach (Recipe recipe in ObjectDB.instance.m_recipes)
		{
			if (recipe.m_enabled && !this.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) && this.HaveRequirements(recipe, true, 0))
			{
				this.AddKnownRecipe(recipe);
			}
		}
		this.m_tempOwnedPieceTables.Clear();
		this.m_inventory.GetAllPieceTables(this.m_tempOwnedPieceTables);
		bool flag = false;
		foreach (PieceTable pieceTable in this.m_tempOwnedPieceTables)
		{
			foreach (GameObject gameObject in pieceTable.m_pieces)
			{
				Piece component = gameObject.GetComponent<Piece>();
				if (component.m_enabled && !this.m_knownRecipes.Contains(component.m_name) && this.HaveRequirements(component, Player.RequirementMode.IsKnown))
				{
					this.AddKnownPiece(component);
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.UpdateAvailablePiecesList();
		}
	}

	private void UpdateAvailablePiecesList()
	{
		if (this.m_buildPieces != null)
		{
			this.m_buildPieces.UpdateAvailable(this.m_knownRecipes, this, this.m_hideUnavailable, this.m_noPlacementCost);
		}
		this.SetupPlacementGhost();
	}

	public override void Message(MessageHud.MessageType type, string msg, int amount = 0, Sprite icon = null)
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			if (MessageHud.instance)
			{
				MessageHud.instance.ShowMessage(type, msg, amount, icon);
				return;
			}
		}
		else
		{
			this.m_nview.InvokeRPC("Message", new object[]
			{
				(int)type,
				msg,
				amount
			});
		}
	}

	private void RPC_Message(long sender, int type, string msg, int amount)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (MessageHud.instance)
		{
			MessageHud.instance.ShowMessage((MessageHud.MessageType)type, msg, amount, null);
		}
	}

	public static Player GetPlayer(long playerID)
	{
		foreach (Player player in Player.m_players)
		{
			if (player.GetPlayerID() == playerID)
			{
				return player;
			}
		}
		return null;
	}

	public static Player GetClosestPlayer(Vector3 point, float maxRange)
	{
		Player result = null;
		float num = 999999f;
		foreach (Player player in Player.m_players)
		{
			float num2 = Vector3.Distance(player.transform.position, point);
			if (num2 < num && num2 < maxRange)
			{
				num = num2;
				result = player;
			}
		}
		return result;
	}

	public static bool IsPlayerInRange(Vector3 point, float range, long playerID)
	{
		foreach (Player player in Player.m_players)
		{
			if (player.GetPlayerID() == playerID)
			{
				return Utils.DistanceXZ(player.transform.position, point) < range;
			}
		}
		return false;
	}

	public static void MessageAllInRange(Vector3 point, float range, MessageHud.MessageType type, string msg, Sprite icon = null)
	{
		foreach (Player player in Player.m_players)
		{
			if (Vector3.Distance(player.transform.position, point) < range)
			{
				player.Message(type, msg, 0, icon);
			}
		}
	}

	public static int GetPlayersInRangeXZ(Vector3 point, float range)
	{
		int num = 0;
		using (List<Player>.Enumerator enumerator = Player.m_players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (Utils.DistanceXZ(enumerator.Current.transform.position, point) < range)
				{
					num++;
				}
			}
		}
		return num;
	}

	public static void GetPlayersInRange(Vector3 point, float range, List<Player> players)
	{
		foreach (Player player in Player.m_players)
		{
			if (Vector3.Distance(player.transform.position, point) < range)
			{
				players.Add(player);
			}
		}
	}

	public static bool IsPlayerInRange(Vector3 point, float range)
	{
		using (List<Player>.Enumerator enumerator = Player.m_players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (Vector3.Distance(enumerator.Current.transform.position, point) < range)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsPlayerInRange(Vector3 point, float range, float minNoise)
	{
		foreach (Player player in Player.m_players)
		{
			if (Vector3.Distance(player.transform.position, point) < range)
			{
				float noiseRange = player.GetNoiseRange();
				if (range <= noiseRange && noiseRange >= minNoise)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static Player GetPlayerNoiseRange(Vector3 point, float noiseRangeScale = 1f)
	{
		foreach (Player player in Player.m_players)
		{
			float num = Vector3.Distance(player.transform.position, point);
			float noiseRange = player.GetNoiseRange();
			if (num < noiseRange * noiseRangeScale)
			{
				return player;
			}
		}
		return null;
	}

	public static List<Player> GetAllPlayers()
	{
		return Player.m_players;
	}

	public static Player GetRandomPlayer()
	{
		if (Player.m_players.Count == 0)
		{
			return null;
		}
		return Player.m_players[UnityEngine.Random.Range(0, Player.m_players.Count)];
	}

	public void GetAvailableRecipes(ref List<Recipe> available)
	{
		available.Clear();
		foreach (Recipe recipe in ObjectDB.instance.m_recipes)
		{
			if (recipe.m_enabled && (recipe.m_item.m_itemData.m_shared.m_dlc.Length <= 0 || DLCMan.instance.IsDLCInstalled(recipe.m_item.m_itemData.m_shared.m_dlc)) && (this.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) || this.m_noPlacementCost) && (this.RequiredCraftingStation(recipe, 1, false) || this.m_noPlacementCost))
			{
				available.Add(recipe);
			}
		}
	}

	private void OnInventoryChanged()
	{
		if (this.m_isLoading)
		{
			return;
		}
		foreach (ItemDrop.ItemData itemData in this.m_inventory.GetAllItems())
		{
			this.AddKnownItem(itemData);
			if (itemData.m_shared.m_name == "$item_hammer")
			{
				this.ShowTutorial("hammer", false);
			}
			else if (itemData.m_shared.m_name == "$item_hoe")
			{
				this.ShowTutorial("hoe", false);
			}
			else if (itemData.m_shared.m_name == "$item_pickaxe_antler")
			{
				this.ShowTutorial("pickaxe", false);
			}
			if (itemData.m_shared.m_name == "$item_trophy_eikthyr")
			{
				this.ShowTutorial("boss_trophy", false);
			}
			if (itemData.m_shared.m_name == "$item_wishbone")
			{
				this.ShowTutorial("wishbone", false);
			}
			else if (itemData.m_shared.m_name == "$item_copperore" || itemData.m_shared.m_name == "$item_tinore")
			{
				this.ShowTutorial("ore", false);
			}
			else if (itemData.m_shared.m_food > 0f)
			{
				this.ShowTutorial("food", false);
			}
		}
		this.UpdateKnownRecipesList();
		this.UpdateAvailablePiecesList();
	}

	public bool InDebugFlyMode()
	{
		return this.m_debugFly;
	}

	public void ShowTutorial(string name, bool force = false)
	{
		if (this.HaveSeenTutorial(name))
		{
			return;
		}
		Tutorial.instance.ShowText(name, force);
	}

	public void SetSeenTutorial(string name)
	{
		if (name.Length == 0)
		{
			return;
		}
		if (this.m_shownTutorials.Contains(name))
		{
			return;
		}
		this.m_shownTutorials.Add(name);
	}

	public bool HaveSeenTutorial(string name)
	{
		return name.Length != 0 && this.m_shownTutorials.Contains(name);
	}

	public static bool IsSeenTutorialsCleared()
	{
		return !Player.m_localPlayer || Player.m_localPlayer.m_shownTutorials.Count == 0;
	}

	public static void ResetSeenTutorials()
	{
		if (Player.m_localPlayer)
		{
			Player.m_localPlayer.m_shownTutorials.Clear();
		}
	}

	public void SetMouseLook(Vector2 mouseLook)
	{
		this.m_lookYaw *= Quaternion.Euler(0f, mouseLook.x, 0f);
		this.m_lookPitch = Mathf.Clamp(this.m_lookPitch - mouseLook.y, -89f, 89f);
		this.UpdateEyeRotation();
		this.m_lookDir = this.m_eye.forward;
	}

	protected override void UpdateEyeRotation()
	{
		this.m_eye.rotation = this.m_lookYaw * Quaternion.Euler(this.m_lookPitch, 0f, 0f);
	}

	public Ragdoll GetRagdoll()
	{
		return this.m_ragdoll;
	}

	public void OnDodgeMortal()
	{
		this.m_dodgeInvincible = false;
	}

	private void UpdateDodge(float dt)
	{
		this.m_queuedDodgeTimer -= dt;
		if (this.m_queuedDodgeTimer > 0f && base.IsOnGround() && !this.IsDead() && !this.InAttack() && !this.IsEncumbered() && !this.InDodge())
		{
			float num = this.m_dodgeStaminaUsage - this.m_dodgeStaminaUsage * this.m_equipmentMovementModifier;
			if (this.HaveStamina(num))
			{
				this.AbortEquipQueue();
				this.m_queuedDodgeTimer = 0f;
				this.m_dodgeInvincible = true;
				base.transform.rotation = Quaternion.LookRotation(this.m_queuedDodgeDir);
				this.m_body.rotation = base.transform.rotation;
				this.m_zanim.SetTrigger("dodge");
				base.AddNoise(5f);
				this.UseStamina(num);
				this.m_dodgeEffects.Create(base.transform.position, Quaternion.identity, base.transform, 1f);
			}
			else
			{
				Hud.instance.StaminaBarNoStaminaFlash();
			}
		}
		AnimatorStateInfo currentAnimatorStateInfo = this.m_animator.GetCurrentAnimatorStateInfo(0);
		AnimatorStateInfo nextAnimatorStateInfo = this.m_animator.GetNextAnimatorStateInfo(0);
		bool flag = this.m_animator.IsInTransition(0);
		bool flag2 = this.m_animator.GetBool("dodge") || (currentAnimatorStateInfo.tagHash == Player.m_animatorTagDodge && !flag) || (flag && nextAnimatorStateInfo.tagHash == Player.m_animatorTagDodge);
		bool value = flag2 && this.m_dodgeInvincible;
		this.m_nview.GetZDO().Set("dodgeinv", value);
		this.m_inDodge = flag2;
	}

	public override bool IsDodgeInvincible()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool("dodgeinv", false);
	}

	public override bool InDodge()
	{
		return this.m_nview.IsValid() && this.m_nview.IsOwner() && this.m_inDodge;
	}

	public override bool IsDead()
	{
		ZDO zdo = this.m_nview.GetZDO();
		return zdo != null && zdo.GetBool("dead", false);
	}

	protected void Dodge(Vector3 dodgeDir)
	{
		this.m_queuedDodgeTimer = 0.5f;
		this.m_queuedDodgeDir = dodgeDir;
	}

	public override bool AlwaysRotateCamera()
	{
		if ((base.GetCurrentWeapon() != null && this.m_currentAttack != null && this.m_lastCombatTimer < 1f && this.m_currentAttack.m_attackType != Attack.AttackType.None && ZInput.IsMouseActive()) || this.IsHoldingAttack() || this.m_blocking)
		{
			return true;
		}
		if (this.InPlaceMode())
		{
			Vector3 from = base.GetLookYaw() * Vector3.forward;
			Vector3 forward = base.transform.forward;
			if (Vector3.Angle(from, forward) > 90f)
			{
				return true;
			}
		}
		return false;
	}

	public override bool TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		if (this.IsTeleporting())
		{
			return false;
		}
		if (this.m_teleportCooldown < 2f)
		{
			return false;
		}
		this.m_teleporting = true;
		this.m_distantTeleport = distantTeleport;
		this.m_teleportTimer = 0f;
		this.m_teleportCooldown = 0f;
		this.m_teleportFromPos = base.transform.position;
		this.m_teleportFromRot = base.transform.rotation;
		this.m_teleportTargetPos = pos;
		this.m_teleportTargetRot = rot;
		return true;
	}

	private void UpdateTeleport(float dt)
	{
		if (!this.m_teleporting)
		{
			this.m_teleportCooldown += dt;
			return;
		}
		this.m_teleportCooldown = 0f;
		this.m_teleportTimer += dt;
		if (this.m_teleportTimer > 2f)
		{
			Vector3 lookDir = this.m_teleportTargetRot * Vector3.forward;
			base.transform.position = this.m_teleportTargetPos;
			base.transform.rotation = this.m_teleportTargetRot;
			this.m_body.velocity = Vector3.zero;
			this.m_maxAirAltitude = base.transform.position.y;
			base.SetLookDir(lookDir);
			if ((this.m_teleportTimer > 8f || !this.m_distantTeleport) && ZNetScene.instance.IsAreaReady(this.m_teleportTargetPos))
			{
				float num = 0f;
				if (ZoneSystem.instance.FindFloor(this.m_teleportTargetPos, out num))
				{
					this.m_teleportTimer = 0f;
					this.m_teleporting = false;
					base.ResetCloth();
					return;
				}
				if (this.m_teleportTimer > 15f || !this.m_distantTeleport)
				{
					if (this.m_distantTeleport)
					{
						Vector3 position = base.transform.position;
						position.y = ZoneSystem.instance.GetSolidHeight(this.m_teleportTargetPos) + 0.5f;
						base.transform.position = position;
					}
					else
					{
						base.transform.rotation = this.m_teleportFromRot;
						base.transform.position = this.m_teleportFromPos;
						this.m_maxAirAltitude = base.transform.position.y;
						this.Message(MessageHud.MessageType.Center, "$msg_portal_blocked", 0, null);
					}
					this.m_teleportTimer = 0f;
					this.m_teleporting = false;
					base.ResetCloth();
				}
			}
		}
	}

	public override bool IsTeleporting()
	{
		return this.m_teleporting;
	}

	public bool ShowTeleportAnimation()
	{
		return this.m_teleporting && this.m_distantTeleport;
	}

	public void SetPlayerModel(int index)
	{
		if (this.m_modelIndex == index)
		{
			return;
		}
		this.m_modelIndex = index;
		this.m_visEquipment.SetModel(index);
	}

	public int GetPlayerModel()
	{
		return this.m_modelIndex;
	}

	public void SetSkinColor(Vector3 color)
	{
		if (color == this.m_skinColor)
		{
			return;
		}
		this.m_skinColor = color;
		this.m_visEquipment.SetSkinColor(this.m_skinColor);
	}

	public void SetHairColor(Vector3 color)
	{
		if (this.m_hairColor == color)
		{
			return;
		}
		this.m_hairColor = color;
		this.m_visEquipment.SetHairColor(this.m_hairColor);
	}

	protected override void SetupVisEquipment(VisEquipment visEq, bool isRagdoll)
	{
		base.SetupVisEquipment(visEq, isRagdoll);
		visEq.SetModel(this.m_modelIndex);
		visEq.SetSkinColor(this.m_skinColor);
		visEq.SetHairColor(this.m_hairColor);
	}

	public override bool CanConsumeItem(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
		{
			return false;
		}
		if (item.m_shared.m_food > 0f && !this.CanEat(item, true))
		{
			return false;
		}
		if (item.m_shared.m_consumeStatusEffect)
		{
			StatusEffect consumeStatusEffect = item.m_shared.m_consumeStatusEffect;
			if (this.m_seman.HaveStatusEffect(item.m_shared.m_consumeStatusEffect.name) || this.m_seman.HaveStatusEffectCategory(consumeStatusEffect.m_category))
			{
				this.Message(MessageHud.MessageType.Center, "$msg_cantconsume", 0, null);
				return false;
			}
		}
		return true;
	}

	public override bool ConsumeItem(Inventory inventory, ItemDrop.ItemData item)
	{
		if (!this.CanConsumeItem(item))
		{
			return false;
		}
		if (item.m_shared.m_consumeStatusEffect)
		{
			StatusEffect consumeStatusEffect = item.m_shared.m_consumeStatusEffect;
			this.m_seman.AddStatusEffect(item.m_shared.m_consumeStatusEffect, true);
		}
		if (item.m_shared.m_food > 0f)
		{
			this.EatFood(item);
		}
		inventory.RemoveOneItem(item);
		return true;
	}

	public void SetIntro(bool intro)
	{
		if (this.m_intro == intro)
		{
			return;
		}
		this.m_intro = intro;
		this.m_zanim.SetBool("intro", intro);
	}

	public override bool InIntro()
	{
		return this.m_intro;
	}

	public override bool InCutscene()
	{
		return this.m_animator.GetCurrentAnimatorStateInfo(0).tagHash == Player.m_animatorTagCutscene || this.InIntro() || this.m_sleeping || base.InCutscene();
	}

	public void SetMaxStamina(float stamina, bool flashBar)
	{
		if (flashBar && Hud.instance != null && stamina > this.m_maxStamina)
		{
			Hud.instance.StaminaBarUppgradeFlash();
		}
		this.m_maxStamina = stamina;
		this.m_stamina = Mathf.Clamp(this.m_stamina, 0f, this.m_maxStamina);
	}

	public void SetMaxHealth(float health, bool flashBar)
	{
		if (flashBar && Hud.instance != null && health > base.GetMaxHealth())
		{
			Hud.instance.FlashHealthBar();
		}
		base.SetMaxHealth(health);
	}

	public override bool IsPVPEnabled()
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_pvp;
		}
		return this.m_nview.GetZDO().GetBool("pvp", false);
	}

	public void SetPVP(bool enabled)
	{
		if (this.m_pvp == enabled)
		{
			return;
		}
		this.m_pvp = enabled;
		this.m_nview.GetZDO().Set("pvp", this.m_pvp);
		if (this.m_pvp)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_pvpon", 0, null);
			return;
		}
		this.Message(MessageHud.MessageType.Center, "$msg_pvpoff", 0, null);
	}

	public bool CanSwitchPVP()
	{
		return this.m_lastCombatTimer > 10f;
	}

	public bool NoCostCheat()
	{
		return this.m_noPlacementCost;
	}

	public void StartEmote(string emote, bool oneshot = true)
	{
		if (!this.CanMove() || this.InAttack() || this.IsHoldingAttack())
		{
			return;
		}
		this.SetCrouch(false);
		int @int = this.m_nview.GetZDO().GetInt("emoteID", 0);
		this.m_nview.GetZDO().Set("emoteID", @int + 1);
		this.m_nview.GetZDO().Set("emote", emote);
		this.m_nview.GetZDO().Set("emote_oneshot", oneshot);
	}

	protected override void StopEmote()
	{
		if (this.m_nview.GetZDO().GetString("emote", "") != "")
		{
			int @int = this.m_nview.GetZDO().GetInt("emoteID", 0);
			this.m_nview.GetZDO().Set("emoteID", @int + 1);
			this.m_nview.GetZDO().Set("emote", "");
		}
	}

	private void UpdateEmote()
	{
		if (this.m_nview.IsOwner() && this.InEmote() && this.m_moveDir != Vector3.zero)
		{
			this.StopEmote();
		}
		int @int = this.m_nview.GetZDO().GetInt("emoteID", 0);
		if (@int != this.m_emoteID)
		{
			this.m_emoteID = @int;
			if (!string.IsNullOrEmpty(this.m_emoteState))
			{
				this.m_animator.SetBool("emote_" + this.m_emoteState, false);
			}
			this.m_emoteState = "";
			this.m_animator.SetTrigger("emote_stop");
			string @string = this.m_nview.GetZDO().GetString("emote", "");
			if (!string.IsNullOrEmpty(@string))
			{
				bool @bool = this.m_nview.GetZDO().GetBool("emote_oneshot", false);
				this.m_animator.ResetTrigger("emote_stop");
				if (@bool)
				{
					this.m_animator.SetTrigger("emote_" + @string);
					return;
				}
				this.m_emoteState = @string;
				this.m_animator.SetBool("emote_" + @string, true);
			}
		}
	}

	public override bool InEmote()
	{
		return !string.IsNullOrEmpty(this.m_emoteState) || this.m_animator.GetCurrentAnimatorStateInfo(0).tagHash == Player.m_animatorTagEmote;
	}

	public override bool IsCrouching()
	{
		return this.m_animator.GetCurrentAnimatorStateInfo(0).tagHash == Player.m_animatorTagCrouch;
	}

	private void UpdateCrouch(float dt)
	{
		if (this.m_crouchToggled)
		{
			if (!this.HaveStamina(0f) || base.IsSwiming() || this.InBed() || this.InPlaceMode() || this.m_run || this.IsBlocking() || base.IsFlying())
			{
				this.SetCrouch(false);
			}
			bool flag = this.InAttack() || this.IsHoldingAttack();
			this.m_zanim.SetBool(Player.crouching, this.m_crouchToggled && !flag);
			return;
		}
		this.m_zanim.SetBool(Player.crouching, false);
	}

	protected override void SetCrouch(bool crouch)
	{
		if (this.m_crouchToggled == crouch)
		{
			return;
		}
		this.m_crouchToggled = crouch;
	}

	public void SetGuardianPower(string name)
	{
		this.m_guardianPower = name;
		this.m_guardianSE = ObjectDB.instance.GetStatusEffect(this.m_guardianPower);
	}

	public string GetGuardianPowerName()
	{
		return this.m_guardianPower;
	}

	public void GetGuardianPowerHUD(out StatusEffect se, out float cooldown)
	{
		se = this.m_guardianSE;
		cooldown = this.m_guardianPowerCooldown;
	}

	public bool StartGuardianPower()
	{
		if (this.m_guardianSE == null)
		{
			return false;
		}
		if ((this.InAttack() && !this.HaveQueuedChain()) || this.InDodge() || !this.CanMove() || base.IsKnockedBack() || base.IsStaggering() || this.InMinorAction())
		{
			return false;
		}
		if (this.m_guardianPowerCooldown > 0f)
		{
			this.Message(MessageHud.MessageType.Center, "$hud_powernotready", 0, null);
			return false;
		}
		this.m_zanim.SetTrigger("gpower");
		return true;
	}

	public bool ActivateGuardianPower()
	{
		if (this.m_guardianPowerCooldown > 0f)
		{
			return false;
		}
		if (this.m_guardianSE == null)
		{
			return false;
		}
		List<Player> list = new List<Player>();
		Player.GetPlayersInRange(base.transform.position, 10f, list);
		foreach (Player player in list)
		{
			player.GetSEMan().AddStatusEffect(this.m_guardianSE.name, true);
		}
		this.m_guardianPowerCooldown = this.m_guardianSE.m_cooldown;
		return false;
	}

	private void UpdateGuardianPower(float dt)
	{
		this.m_guardianPowerCooldown -= dt;
		if (this.m_guardianPowerCooldown < 0f)
		{
			this.m_guardianPowerCooldown = 0f;
		}
	}

	public override void AttachStart(Transform attachPoint, bool hideWeapons, bool isBed, string attachAnimation, Vector3 detachOffset)
	{
		if (this.m_attached)
		{
			return;
		}
		this.m_attached = true;
		this.m_attachPoint = attachPoint;
		this.m_detachOffset = detachOffset;
		this.m_attachAnimation = attachAnimation;
		this.m_zanim.SetBool(attachAnimation, true);
		this.m_nview.GetZDO().Set("inBed", isBed);
		if (hideWeapons)
		{
			base.HideHandItems();
		}
		base.ResetCloth();
	}

	private void UpdateAttach()
	{
		if (this.m_attached)
		{
			if (this.m_attachPoint != null)
			{
				base.transform.position = this.m_attachPoint.position;
				base.transform.rotation = this.m_attachPoint.rotation;
				Rigidbody componentInParent = this.m_attachPoint.GetComponentInParent<Rigidbody>();
				this.m_body.useGravity = false;
				this.m_body.velocity = (componentInParent ? componentInParent.GetPointVelocity(base.transform.position) : Vector3.zero);
				this.m_body.angularVelocity = Vector3.zero;
				this.m_maxAirAltitude = base.transform.position.y;
				return;
			}
			this.AttachStop();
		}
	}

	public override bool IsAttached()
	{
		return this.m_attached;
	}

	public override bool InBed()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool("inBed", false);
	}

	public override void AttachStop()
	{
		if (this.m_sleeping)
		{
			return;
		}
		if (this.m_attached)
		{
			if (this.m_attachPoint != null)
			{
				base.transform.position = this.m_attachPoint.TransformPoint(this.m_detachOffset);
			}
			this.m_body.useGravity = true;
			this.m_attached = false;
			this.m_attachPoint = null;
			this.m_zanim.SetBool(this.m_attachAnimation, false);
			this.m_nview.GetZDO().Set("inBed", false);
			base.ResetCloth();
		}
	}

	public void StartShipControl(ShipControlls shipControl)
	{
		this.m_shipControl = shipControl;
		ZLog.Log("ship controlls set " + shipControl.GetShip().gameObject.name);
	}

	public void StopShipControl()
	{
		if (this.m_shipControl != null)
		{
			if (this.m_shipControl)
			{
				this.m_shipControl.OnUseStop(this);
			}
			ZLog.Log("Stop ship controlls");
			this.m_shipControl = null;
		}
	}

	private void SetShipControl(ref Vector3 moveDir)
	{
		this.m_shipControl.GetShip().ApplyMovementControlls(moveDir);
		moveDir = Vector3.zero;
	}

	public Ship GetControlledShip()
	{
		if (this.m_shipControl)
		{
			return this.m_shipControl.GetShip();
		}
		return null;
	}

	public ShipControlls GetShipControl()
	{
		return this.m_shipControl;
	}

	private void UpdateShipControl(float dt)
	{
		if (!this.m_shipControl)
		{
			return;
		}
		Vector3 forward = this.m_shipControl.GetShip().transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Quaternion to = Quaternion.LookRotation(forward);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, 100f * dt);
		if (Vector3.Distance(this.m_shipControl.transform.position, base.transform.position) > this.m_maxInteractDistance)
		{
			this.StopShipControl();
		}
	}

	public bool IsSleeping()
	{
		return this.m_sleeping;
	}

	public void SetSleeping(bool sleep)
	{
		if (this.m_sleeping == sleep)
		{
			return;
		}
		this.m_sleeping = sleep;
		if (!sleep)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_goodmorning", 0, null);
			this.m_seman.AddStatusEffect("Rested", true);
		}
	}

	public void SetControls(Vector3 movedir, bool attack, bool attackHold, bool secondaryAttack, bool block, bool blockHold, bool jump, bool crouch, bool run, bool autoRun)
	{
		if ((movedir != Vector3.zero || attack || secondaryAttack || block || blockHold || jump || crouch) && this.GetControlledShip() == null)
		{
			this.StopEmote();
			this.AttachStop();
		}
		if (this.m_shipControl)
		{
			this.SetShipControl(ref movedir);
			if (jump)
			{
				this.StopShipControl();
			}
		}
		if (run)
		{
			this.m_walk = false;
		}
		if (!this.m_autoRun)
		{
			Vector3 lookDir = this.m_lookDir;
			lookDir.y = 0f;
			lookDir.Normalize();
			this.m_moveDir = movedir.z * lookDir + movedir.x * Vector3.Cross(Vector3.up, lookDir);
		}
		if (!this.m_autoRun && autoRun && !this.InPlaceMode())
		{
			this.m_autoRun = true;
			this.SetCrouch(false);
			this.m_moveDir = this.m_lookDir;
			this.m_moveDir.y = 0f;
			this.m_moveDir.Normalize();
		}
		else if (this.m_autoRun)
		{
			if (attack || jump || crouch || movedir != Vector3.zero || this.InPlaceMode() || attackHold)
			{
				this.m_autoRun = false;
			}
			else if (autoRun || blockHold)
			{
				this.m_moveDir = this.m_lookDir;
				this.m_moveDir.y = 0f;
				this.m_moveDir.Normalize();
				blockHold = false;
				block = false;
			}
		}
		this.m_attack = attack;
		this.m_attackDraw = attackHold;
		this.m_secondaryAttack = secondaryAttack;
		this.m_blocking = blockHold;
		this.m_run = run;
		if (crouch)
		{
			this.SetCrouch(!this.m_crouchToggled);
		}
		if (jump)
		{
			if (this.m_blocking)
			{
				Vector3 dodgeDir = this.m_moveDir;
				if (dodgeDir.magnitude < 0.1f)
				{
					dodgeDir = -this.m_lookDir;
					dodgeDir.y = 0f;
					dodgeDir.Normalize();
				}
				this.Dodge(dodgeDir);
				return;
			}
			if (this.IsCrouching() || this.m_crouchToggled)
			{
				Vector3 dodgeDir2 = this.m_moveDir;
				if (dodgeDir2.magnitude < 0.1f)
				{
					dodgeDir2 = this.m_lookDir;
					dodgeDir2.y = 0f;
					dodgeDir2.Normalize();
				}
				this.Dodge(dodgeDir2);
				return;
			}
			base.Jump();
		}
	}

	private void UpdateTargeted(float dt)
	{
		this.m_timeSinceTargeted += dt;
		this.m_timeSinceSensed += dt;
	}

	public override void OnTargeted(bool sensed, bool alerted)
	{
		if (sensed)
		{
			if (this.m_timeSinceSensed > 0.5f)
			{
				this.m_timeSinceSensed = 0f;
				this.m_nview.InvokeRPC("OnTargeted", new object[]
				{
					sensed,
					alerted
				});
				return;
			}
		}
		else if (this.m_timeSinceTargeted > 0.5f)
		{
			this.m_timeSinceTargeted = 0f;
			this.m_nview.InvokeRPC("OnTargeted", new object[]
			{
				sensed,
				alerted
			});
		}
	}

	private void RPC_OnTargeted(long sender, bool sensed, bool alerted)
	{
		this.m_timeSinceTargeted = 0f;
		if (sensed)
		{
			this.m_timeSinceSensed = 0f;
		}
		if (alerted)
		{
			MusicMan.instance.ResetCombatTimer();
		}
	}

	protected override void OnDamaged(HitData hit)
	{
		base.OnDamaged(hit);
		Hud.instance.DamageFlash();
	}

	public bool IsTargeted()
	{
		return this.m_timeSinceTargeted < 1f;
	}

	public bool IsSensed()
	{
		return this.m_timeSinceSensed < 1f;
	}

	protected override void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
	{
		if (this.m_chestItem != null)
		{
			mods.Apply(this.m_chestItem.m_shared.m_damageModifiers);
		}
		if (this.m_legItem != null)
		{
			mods.Apply(this.m_legItem.m_shared.m_damageModifiers);
		}
		if (this.m_helmetItem != null)
		{
			mods.Apply(this.m_helmetItem.m_shared.m_damageModifiers);
		}
		if (this.m_shoulderItem != null)
		{
			mods.Apply(this.m_shoulderItem.m_shared.m_damageModifiers);
		}
	}

	public override float GetBodyArmor()
	{
		float num = 0f;
		if (this.m_chestItem != null)
		{
			num += this.m_chestItem.GetArmor();
		}
		if (this.m_legItem != null)
		{
			num += this.m_legItem.GetArmor();
		}
		if (this.m_helmetItem != null)
		{
			num += this.m_helmetItem.GetArmor();
		}
		if (this.m_shoulderItem != null)
		{
			num += this.m_shoulderItem.GetArmor();
		}
		return num;
	}

	protected override void OnSneaking(float dt)
	{
		float t = Mathf.Pow(this.m_skills.GetSkillFactor(Skills.SkillType.Sneak), 0.5f);
		float num = Mathf.Lerp(1f, 0.25f, t);
		this.UseStamina(dt * this.m_sneakStaminaDrain * num);
		if (!this.HaveStamina(0f))
		{
			Hud.instance.StaminaBarNoStaminaFlash();
		}
		this.m_sneakSkillImproveTimer += dt;
		if (this.m_sneakSkillImproveTimer > 1f)
		{
			this.m_sneakSkillImproveTimer = 0f;
			if (BaseAI.InStealthRange(this))
			{
				this.RaiseSkill(Skills.SkillType.Sneak, 1f);
				return;
			}
			this.RaiseSkill(Skills.SkillType.Sneak, 0.1f);
		}
	}

	private void UpdateStealth(float dt)
	{
		this.m_stealthFactorUpdateTimer += dt;
		if (this.m_stealthFactorUpdateTimer > 0.5f)
		{
			this.m_stealthFactorUpdateTimer = 0f;
			this.m_stealthFactorTarget = 0f;
			if (this.IsCrouching())
			{
				this.m_lastStealthPosition = base.transform.position;
				float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Sneak);
				float lightFactor = StealthSystem.instance.GetLightFactor(base.GetCenterPoint());
				this.m_stealthFactorTarget = Mathf.Lerp(0.25f + lightFactor * 0.75f, lightFactor * 0.3f, skillFactor);
				this.m_stealthFactorTarget = Mathf.Clamp01(this.m_stealthFactorTarget);
				this.m_seman.ModifyStealth(this.m_stealthFactorTarget, ref this.m_stealthFactorTarget);
				this.m_stealthFactorTarget = Mathf.Clamp01(this.m_stealthFactorTarget);
			}
			else
			{
				this.m_stealthFactorTarget = 1f;
			}
		}
		this.m_stealthFactor = Mathf.MoveTowards(this.m_stealthFactor, this.m_stealthFactorTarget, dt / 4f);
		this.m_nview.GetZDO().Set("Stealth", this.m_stealthFactor);
	}

	public override float GetStealthFactor()
	{
		if (!this.m_nview.IsValid())
		{
			return 0f;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_stealthFactor;
		}
		return this.m_nview.GetZDO().GetFloat("Stealth", 0f);
	}

	public override bool InAttack()
	{
		if (this.m_animator.IsInTransition(0))
		{
			return this.m_animator.GetNextAnimatorStateInfo(0).tagHash == Humanoid.m_animatorTagAttack || this.m_animator.GetNextAnimatorStateInfo(1).tagHash == Humanoid.m_animatorTagAttack;
		}
		return this.m_animator.GetCurrentAnimatorStateInfo(0).tagHash == Humanoid.m_animatorTagAttack || this.m_animator.GetCurrentAnimatorStateInfo(1).tagHash == Humanoid.m_animatorTagAttack;
	}

	public override float GetEquipmentMovementModifier()
	{
		return this.m_equipmentMovementModifier;
	}

	protected override float GetJogSpeedFactor()
	{
		return 1f + this.m_equipmentMovementModifier;
	}

	protected override float GetRunSpeedFactor()
	{
		float num = 1f;
		float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Run);
		return (num + skillFactor * 0.25f) * (1f + this.m_equipmentMovementModifier * 1.5f);
	}

	public override bool InMinorAction()
	{
		return (this.m_animator.IsInTransition(1) ? this.m_animator.GetNextAnimatorStateInfo(1) : this.m_animator.GetCurrentAnimatorStateInfo(1)).tagHash == Player.m_animatorTagMinorAction;
	}

	public override bool GetRelativePosition(out ZDOID parent, out Vector3 relativePos, out Vector3 relativeVel)
	{
		if (this.m_attached && this.m_attachPoint)
		{
			ZNetView componentInParent = this.m_attachPoint.GetComponentInParent<ZNetView>();
			if (componentInParent && componentInParent.IsValid())
			{
				parent = componentInParent.GetZDO().m_uid;
				relativePos = componentInParent.transform.InverseTransformPoint(base.transform.position);
				relativeVel = Vector3.zero;
				return true;
			}
		}
		return base.GetRelativePosition(out parent, out relativePos, out relativeVel);
	}

	public override Skills GetSkills()
	{
		return this.m_skills;
	}

	public override float GetRandomSkillFactor(Skills.SkillType skill)
	{
		return this.m_skills.GetRandomSkillFactor(skill);
	}

	public override float GetSkillFactor(Skills.SkillType skill)
	{
		return this.m_skills.GetSkillFactor(skill);
	}

	protected override void DoDamageCameraShake(HitData hit)
	{
		if (GameCamera.instance && hit.GetTotalPhysicalDamage() > 0f)
		{
			float num = Mathf.Clamp01(hit.GetTotalPhysicalDamage() / base.GetMaxHealth());
			GameCamera.instance.AddShake(base.transform.position, 50f, this.m_baseCameraShake * num, false);
		}
	}

	protected override bool ToggleEquiped(ItemDrop.ItemData item)
	{
		if (!item.IsEquipable())
		{
			return false;
		}
		if (this.InAttack())
		{
			return true;
		}
		if (item.m_shared.m_equipDuration <= 0f)
		{
			if (base.IsItemEquiped(item))
			{
				base.UnequipItem(item, true);
			}
			else
			{
				base.EquipItem(item, true);
			}
		}
		else if (base.IsItemEquiped(item))
		{
			this.QueueUnequipItem(item);
		}
		else
		{
			this.QueueEquipItem(item);
		}
		return true;
	}

	public void GetActionProgress(out string name, out float progress)
	{
		if (this.m_equipQueue.Count > 0)
		{
			Player.EquipQueueData equipQueueData = this.m_equipQueue[0];
			if (equipQueueData.m_duration > 0.5f)
			{
				if (equipQueueData.m_equip)
				{
					name = "$hud_equipping " + equipQueueData.m_item.m_shared.m_name;
				}
				else
				{
					name = "$hud_unequipping " + equipQueueData.m_item.m_shared.m_name;
				}
				progress = Mathf.Clamp01(equipQueueData.m_time / equipQueueData.m_duration);
				return;
			}
		}
		name = null;
		progress = 0f;
	}

	private void UpdateEquipQueue(float dt)
	{
		if (this.m_equipQueuePause > 0f)
		{
			this.m_equipQueuePause -= dt;
			this.m_zanim.SetBool("equipping", false);
			return;
		}
		this.m_zanim.SetBool("equipping", this.m_equipQueue.Count > 0);
		if (this.m_equipQueue.Count == 0)
		{
			return;
		}
		Player.EquipQueueData equipQueueData = this.m_equipQueue[0];
		if (equipQueueData.m_time == 0f && equipQueueData.m_duration >= 1f)
		{
			this.m_equipStartEffects.Create(base.transform.position, Quaternion.identity, null, 1f);
		}
		equipQueueData.m_time += dt;
		if (equipQueueData.m_time > equipQueueData.m_duration)
		{
			this.m_equipQueue.RemoveAt(0);
			if (equipQueueData.m_equip)
			{
				base.EquipItem(equipQueueData.m_item, true);
			}
			else
			{
				base.UnequipItem(equipQueueData.m_item, true);
			}
			this.m_equipQueuePause = 0.3f;
		}
	}

	private void QueueEquipItem(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return;
		}
		if (this.IsItemQueued(item))
		{
			this.RemoveFromEquipQueue(item);
			return;
		}
		Player.EquipQueueData equipQueueData = new Player.EquipQueueData();
		equipQueueData.m_item = item;
		equipQueueData.m_equip = true;
		equipQueueData.m_duration = item.m_shared.m_equipDuration;
		this.m_equipQueue.Add(equipQueueData);
	}

	private void QueueUnequipItem(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return;
		}
		if (this.IsItemQueued(item))
		{
			this.RemoveFromEquipQueue(item);
			return;
		}
		Player.EquipQueueData equipQueueData = new Player.EquipQueueData();
		equipQueueData.m_item = item;
		equipQueueData.m_equip = false;
		equipQueueData.m_duration = item.m_shared.m_equipDuration;
		this.m_equipQueue.Add(equipQueueData);
	}

	public override void AbortEquipQueue()
	{
		this.m_equipQueue.Clear();
	}

	public override void RemoveFromEquipQueue(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return;
		}
		foreach (Player.EquipQueueData equipQueueData in this.m_equipQueue)
		{
			if (equipQueueData.m_item == item)
			{
				this.m_equipQueue.Remove(equipQueueData);
				break;
			}
		}
	}

	public bool IsItemQueued(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return false;
		}
		using (List<Player.EquipQueueData>.Enumerator enumerator = this.m_equipQueue.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_item == item)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void ResetCharacter()
	{
		this.m_guardianPowerCooldown = 0f;
		Player.ResetSeenTutorials();
		this.m_knownRecipes.Clear();
		this.m_knownStations.Clear();
		this.m_knownMaterial.Clear();
		this.m_uniques.Clear();
		this.m_trophies.Clear();
		this.m_skills.Clear();
		this.m_knownBiome.Clear();
		this.m_knownTexts.Clear();
	}

	private float m_rotatePieceTimer;

	private float m_baseValueUpdatetimer;

	private const int dataVersion = 24;

	private float m_equipQueuePause;

	public static Player m_localPlayer = null;

	private static List<Player> m_players = new List<Player>();

	public static bool m_debugMode = false;

	[Header("Player")]
	public float m_maxPlaceDistance = 5f;

	public float m_maxInteractDistance = 5f;

	public float m_scrollSens = 4f;

	public float m_staminaRegen = 5f;

	public float m_staminaRegenTimeMultiplier = 1f;

	public float m_staminaRegenDelay = 1f;

	public float m_runStaminaDrain = 10f;

	public float m_sneakStaminaDrain = 5f;

	public float m_swimStaminaDrainMinSkill = 5f;

	public float m_swimStaminaDrainMaxSkill = 2f;

	public float m_dodgeStaminaUsage = 10f;

	public float m_weightStaminaFactor = 0.1f;

	public float m_autoPickupRange = 2f;

	public float m_maxCarryWeight = 300f;

	public float m_encumberedStaminaDrain = 10f;

	public float m_hardDeathCooldown = 10f;

	public float m_baseCameraShake = 4f;

	public EffectList m_drownEffects = new EffectList();

	public EffectList m_spawnEffects = new EffectList();

	public EffectList m_removeEffects = new EffectList();

	public EffectList m_dodgeEffects = new EffectList();

	public EffectList m_autopickupEffects = new EffectList();

	public EffectList m_skillLevelupEffects = new EffectList();

	public EffectList m_equipStartEffects = new EffectList();

	public GameObject m_placeMarker;

	public GameObject m_tombstone;

	public GameObject m_valkyrie;

	public Sprite m_textIcon;

	private Skills m_skills;

	private PieceTable m_buildPieces;

	private bool m_noPlacementCost;

	private bool m_hideUnavailable;

	private HashSet<string> m_knownRecipes = new HashSet<string>();

	private Dictionary<string, int> m_knownStations = new Dictionary<string, int>();

	private HashSet<string> m_knownMaterial = new HashSet<string>();

	private HashSet<string> m_shownTutorials = new HashSet<string>();

	private HashSet<string> m_uniques = new HashSet<string>();

	private HashSet<string> m_trophies = new HashSet<string>();

	private HashSet<Heightmap.Biome> m_knownBiome = new HashSet<Heightmap.Biome>();

	private Dictionary<string, string> m_knownTexts = new Dictionary<string, string>();

	private float m_stationDiscoverTimer;

	private bool m_debugFly;

	private bool m_godMode;

	private bool m_ghostMode;

	private float m_lookPitch;

	private const float m_baseHP = 25f;

	private const float m_baseStamina = 75f;

	private const int m_maxFoods = 3;

	private const float m_foodDrainPerSec = 0.1f;

	private float m_foodUpdateTimer;

	private float m_foodRegenTimer;

	private List<Player.Food> m_foods = new List<Player.Food>();

	private float m_stamina = 100f;

	private float m_maxStamina = 100f;

	private float m_staminaRegenTimer;

	private string m_guardianPower = "";

	private float m_guardianPowerCooldown;

	private StatusEffect m_guardianSE;

	private GameObject m_placementMarkerInstance;

	private GameObject m_placementGhost;

	private Player.PlacementStatus m_placementStatus = Player.PlacementStatus.Invalid;

	private int m_placeRotation;

	private int m_placeRayMask;

	private int m_placeGroundRayMask;

	private int m_placeWaterRayMask;

	private int m_removeRayMask;

	private int m_interactMask;

	private int m_autoPickupMask;

	private List<Player.EquipQueueData> m_equipQueue = new List<Player.EquipQueueData>();

	private GameObject m_hovering;

	private Character m_hoveringCreature;

	private float m_lastHoverInteractTime;

	private bool m_pvp;

	private float m_updateCoverTimer;

	private float m_coverPercentage;

	private bool m_underRoof = true;

	private float m_nearFireTimer;

	private bool m_isLoading;

	private float m_queuedAttackTimer;

	private float m_queuedSecondAttackTimer;

	private float m_queuedDodgeTimer;

	private Vector3 m_queuedDodgeDir = Vector3.zero;

	private bool m_inDodge;

	private bool m_dodgeInvincible;

	private CraftingStation m_currentStation;

	private Ragdoll m_ragdoll;

	private Piece m_hoveringPiece;

	private string m_emoteState = "";

	private int m_emoteID;

	private bool m_intro;

	private bool m_firstSpawn = true;

	private bool m_crouchToggled;

	private bool m_autoRun;

	private bool m_safeInHome;

	private ShipControlls m_shipControl;

	private bool m_attached;

	private string m_attachAnimation = "";

	private bool m_sleeping;

	private Transform m_attachPoint;

	private Vector3 m_detachOffset = Vector3.zero;

	private int m_modelIndex;

	private Vector3 m_skinColor = Vector3.one;

	private Vector3 m_hairColor = Vector3.one;

	private bool m_teleporting;

	private bool m_distantTeleport;

	private float m_teleportTimer;

	private float m_teleportCooldown;

	private Vector3 m_teleportFromPos;

	private Quaternion m_teleportFromRot;

	private Vector3 m_teleportTargetPos;

	private Quaternion m_teleportTargetRot;

	private Heightmap.Biome m_currentBiome;

	private float m_biomeTimer;

	private int m_baseValue;

	private int m_comfortLevel;

	private float m_drownDamageTimer;

	private float m_timeSinceTargeted;

	private float m_timeSinceSensed;

	private float m_stealthFactorUpdateTimer;

	private float m_stealthFactor;

	private float m_stealthFactorTarget;

	private Vector3 m_lastStealthPosition = Vector3.zero;

	private float m_wakeupTimer = -1f;

	private float m_timeSinceDeath = 999999f;

	private float m_runSkillImproveTimer;

	private float m_swimSkillImproveTimer;

	private float m_sneakSkillImproveTimer;

	private float m_equipmentMovementModifier;

	private static int crouching = 0;

	protected static int m_attackMask = 0;

	protected static int m_animatorTagDodge = Animator.StringToHash("dodge");

	protected static int m_animatorTagCutscene = Animator.StringToHash("cutscene");

	protected static int m_animatorTagCrouch = Animator.StringToHash("crouch");

	protected static int m_animatorTagMinorAction = Animator.StringToHash("minoraction");

	protected static int m_animatorTagEmote = Animator.StringToHash("emote");

	private List<PieceTable> m_tempOwnedPieceTables = new List<PieceTable>();

	private List<Transform> m_tempSnapPoints1 = new List<Transform>();

	private List<Transform> m_tempSnapPoints2 = new List<Transform>();

	private List<Piece> m_tempPieces = new List<Piece>();

	public enum RequirementMode
	{
		CanBuild,
		IsKnown,
		CanAlmostBuild
	}

	public class Food
	{
		public bool CanEatAgain()
		{
			return this.m_health < this.m_item.m_shared.m_food / 2f;
		}

		public string m_name = "";

		public ItemDrop.ItemData m_item;

		public float m_health;

		public float m_stamina;
	}

	public class EquipQueueData
	{
		public ItemDrop.ItemData m_item;

		public bool m_equip = true;

		public float m_time;

		public float m_duration;
	}

	private enum PlacementStatus
	{
		Valid,
		Invalid,
		BlockedbyPlayer,
		NoBuildZone,
		PrivateZone,
		MoreSpace,
		NoTeleportArea,
		ExtensionMissingStation,
		WrongBiome,
		NeedCultivated,
		NotInDungeon
	}
}
