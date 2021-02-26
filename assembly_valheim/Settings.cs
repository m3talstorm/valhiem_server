using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
	public static Settings instance
	{
		get
		{
			return Settings.m_instance;
		}
	}

	private void Awake()
	{
		Settings.m_instance = this;
		this.m_bindDialog.SetActive(false);
		this.m_resDialog.SetActive(false);
		this.m_resSwitchDialog.SetActive(false);
		this.m_resListBaseSize = this.m_resListRoot.rect.height;
		this.LoadSettings();
		this.SetupKeys();
	}

	private void OnDestroy()
	{
		Settings.m_instance = null;
	}

	private void Update()
	{
		if (this.m_bindDialog.activeSelf)
		{
			this.UpdateBinding();
			return;
		}
		this.UpdateResSwitch(Time.deltaTime);
		AudioListener.volume = this.m_volumeSlider.value;
		MusicMan.m_masterMusicVolume = this.m_musicVolumeSlider.value;
		AudioMan.SetSFXVolume(this.m_sfxVolumeSlider.value);
		this.SetQualityText(this.m_shadowQualityText, (int)this.m_shadowQuality.value);
		this.SetQualityText(this.m_lodText, (int)this.m_lod.value);
		this.SetQualityText(this.m_lightsText, (int)this.m_lights.value);
		this.SetQualityText(this.m_vegetationText, (int)this.m_vegetation.value);
		this.m_resButtonText.text = string.Concat(new object[]
		{
			this.m_selectedRes.width,
			"x",
			this.m_selectedRes.height,
			"  ",
			this.m_selectedRes.refreshRate,
			"hz"
		});
		this.m_guiScaleText.text = this.m_guiScaleSlider.value.ToString() + "%";
		GuiScaler.SetScale(this.m_guiScaleSlider.value / 100f);
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			this.OnBack();
		}
	}

	private void SetQualityText(Text text, int level)
	{
		switch (level)
		{
		case 0:
			text.text = Localization.instance.Localize("[$settings_low]");
			return;
		case 1:
			text.text = Localization.instance.Localize("[$settings_medium]");
			return;
		case 2:
			text.text = Localization.instance.Localize("[$settings_high]");
			return;
		case 3:
			text.text = Localization.instance.Localize("[$settings_veryhigh]");
			return;
		default:
			return;
		}
	}

	public void OnBack()
	{
		this.RevertMode();
		this.LoadSettings();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void OnOk()
	{
		this.SaveSettings();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void SaveSettings()
	{
		PlayerPrefs.SetFloat("MasterVolume", this.m_volumeSlider.value);
		PlayerPrefs.SetFloat("MouseSensitivity", this.m_sensitivitySlider.value);
		PlayerPrefs.SetFloat("MusicVolume", this.m_musicVolumeSlider.value);
		PlayerPrefs.SetFloat("SfxVolume", this.m_sfxVolumeSlider.value);
		PlayerPrefs.SetInt("ContinousMusic", this.m_continousMusic.isOn ? 1 : 0);
		PlayerPrefs.SetInt("InvertMouse", this.m_invertMouse.isOn ? 1 : 0);
		PlayerPrefs.SetFloat("GuiScale", this.m_guiScaleSlider.value / 100f);
		PlayerPrefs.SetInt("CameraShake", this.m_cameraShake.isOn ? 1 : 0);
		PlayerPrefs.SetInt("ShipCameraTilt", this.m_shipCameraTilt.isOn ? 1 : 0);
		PlayerPrefs.SetInt("QuickPieceSelect", this.m_quickPieceSelect.isOn ? 1 : 0);
		PlayerPrefs.SetInt("KeyHints", this.m_showKeyHints.isOn ? 1 : 0);
		PlayerPrefs.SetInt("DOF", this.m_dofToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("VSync", this.m_vsyncToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("Bloom", this.m_bloomToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("SSAO", this.m_ssaoToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("SunShafts", this.m_sunshaftsToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("AntiAliasing", this.m_aaToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("ChromaticAberration", this.m_caToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("MotionBlur", this.m_motionblurToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("SoftPart", this.m_softPartToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("Tesselation", this.m_tesselationToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("ShadowQuality", (int)this.m_shadowQuality.value);
		PlayerPrefs.SetInt("LodBias", (int)this.m_lod.value);
		PlayerPrefs.SetInt("Lights", (int)this.m_lights.value);
		PlayerPrefs.SetInt("ClutterQuality", (int)this.m_vegetation.value);
		ZInput.SetGamepadEnabled(this.m_gamepadEnabled.isOn);
		ZInput.instance.Save();
		if (GameCamera.instance)
		{
			GameCamera.instance.ApplySettings();
		}
		if (CameraEffects.instance)
		{
			CameraEffects.instance.ApplySettings();
		}
		if (ClutterSystem.instance)
		{
			ClutterSystem.instance.ApplySettings();
		}
		if (MusicMan.instance)
		{
			MusicMan.instance.ApplySettings();
		}
		if (GameCamera.instance)
		{
			GameCamera.instance.ApplySettings();
		}
		if (KeyHints.instance)
		{
			KeyHints.instance.ApplySettings();
		}
		Settings.ApplyQualitySettings();
		this.ApplyMode();
		PlayerController.m_mouseSens = this.m_sensitivitySlider.value;
		PlayerController.m_invertMouse = this.m_invertMouse.isOn;
		Localization.instance.SetLanguage(this.m_languageKey);
		GuiScaler.LoadGuiScale();
		PlayerPrefs.Save();
	}

	public static void ApplyStartupSettings()
	{
		QualitySettings.vSyncCount = ((PlayerPrefs.GetInt("VSync", 0) == 1) ? 1 : 0);
		Settings.ApplyQualitySettings();
	}

	private static void ApplyQualitySettings()
	{
		QualitySettings.softParticles = (PlayerPrefs.GetInt("SoftPart", 1) == 1);
		if (PlayerPrefs.GetInt("Tesselation", 1) == 1)
		{
			Shader.EnableKeyword("TESSELATION_ON");
		}
		else
		{
			Shader.DisableKeyword("TESSELATION_ON");
		}
		switch (PlayerPrefs.GetInt("LodBias", 2))
		{
		case 0:
			QualitySettings.lodBias = 1f;
			break;
		case 1:
			QualitySettings.lodBias = 1.5f;
			break;
		case 2:
			QualitySettings.lodBias = 2f;
			break;
		case 3:
			QualitySettings.lodBias = 5f;
			break;
		}
		switch (PlayerPrefs.GetInt("Lights", 2))
		{
		case 0:
			QualitySettings.pixelLightCount = 2;
			break;
		case 1:
			QualitySettings.pixelLightCount = 4;
			break;
		case 2:
			QualitySettings.pixelLightCount = 8;
			break;
		}
		Settings.ApplyShadowQuality();
	}

	private static void ApplyShadowQuality()
	{
		switch (PlayerPrefs.GetInt("ShadowQuality", 2))
		{
		case 0:
			QualitySettings.shadowCascades = 2;
			QualitySettings.shadowDistance = 80f;
			QualitySettings.shadowResolution = ShadowResolution.Low;
			return;
		case 1:
			QualitySettings.shadowCascades = 3;
			QualitySettings.shadowDistance = 120f;
			QualitySettings.shadowResolution = ShadowResolution.Medium;
			return;
		case 2:
			QualitySettings.shadowCascades = 4;
			QualitySettings.shadowDistance = 150f;
			QualitySettings.shadowResolution = ShadowResolution.High;
			return;
		default:
			return;
		}
	}

	private void LoadSettings()
	{
		ZInput.instance.Load();
		AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", AudioListener.volume);
		MusicMan.m_masterMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
		AudioMan.SetSFXVolume(PlayerPrefs.GetFloat("SfxVolume", 1f));
		this.m_continousMusic.isOn = (PlayerPrefs.GetInt("ContinousMusic", 0) == 1);
		PlayerController.m_mouseSens = PlayerPrefs.GetFloat("MouseSensitivity", PlayerController.m_mouseSens);
		PlayerController.m_invertMouse = (PlayerPrefs.GetInt("InvertMouse", 0) == 1);
		float @float = PlayerPrefs.GetFloat("GuiScale", 1f);
		this.m_volumeSlider.value = AudioListener.volume;
		this.m_sensitivitySlider.value = PlayerController.m_mouseSens;
		this.m_sfxVolumeSlider.value = AudioMan.GetSFXVolume();
		this.m_musicVolumeSlider.value = MusicMan.m_masterMusicVolume;
		this.m_guiScaleSlider.value = @float * 100f;
		this.m_invertMouse.isOn = PlayerController.m_invertMouse;
		this.m_gamepadEnabled.isOn = ZInput.IsGamepadEnabled();
		this.m_languageKey = Localization.instance.GetSelectedLanguage();
		this.m_language.text = Localization.instance.Localize("$language_" + this.m_languageKey.ToLower());
		this.m_cameraShake.isOn = (PlayerPrefs.GetInt("CameraShake", 1) == 1);
		this.m_shipCameraTilt.isOn = (PlayerPrefs.GetInt("ShipCameraTilt", 1) == 1);
		this.m_quickPieceSelect.isOn = (PlayerPrefs.GetInt("QuickPieceSelect", 0) == 1);
		this.m_showKeyHints.isOn = (PlayerPrefs.GetInt("KeyHints", 1) == 1);
		this.m_dofToggle.isOn = (PlayerPrefs.GetInt("DOF", 1) == 1);
		this.m_vsyncToggle.isOn = (PlayerPrefs.GetInt("VSync", 0) == 1);
		this.m_bloomToggle.isOn = (PlayerPrefs.GetInt("Bloom", 1) == 1);
		this.m_ssaoToggle.isOn = (PlayerPrefs.GetInt("SSAO", 1) == 1);
		this.m_sunshaftsToggle.isOn = (PlayerPrefs.GetInt("SunShafts", 1) == 1);
		this.m_aaToggle.isOn = (PlayerPrefs.GetInt("AntiAliasing", 1) == 1);
		this.m_caToggle.isOn = (PlayerPrefs.GetInt("ChromaticAberration", 1) == 1);
		this.m_motionblurToggle.isOn = (PlayerPrefs.GetInt("MotionBlur", 1) == 1);
		this.m_softPartToggle.isOn = (PlayerPrefs.GetInt("SoftPart", 1) == 1);
		this.m_tesselationToggle.isOn = (PlayerPrefs.GetInt("Tesselation", 1) == 1);
		this.m_shadowQuality.value = (float)PlayerPrefs.GetInt("ShadowQuality", 2);
		this.m_lod.value = (float)PlayerPrefs.GetInt("LodBias", 2);
		this.m_lights.value = (float)PlayerPrefs.GetInt("Lights", 2);
		this.m_vegetation.value = (float)PlayerPrefs.GetInt("ClutterQuality", 2);
		this.m_fullscreenToggle.isOn = Screen.fullScreen;
		this.m_oldFullscreen = this.m_fullscreenToggle.isOn;
		this.m_oldRes = Screen.currentResolution;
		this.m_oldRes.width = Screen.width;
		this.m_oldRes.height = Screen.height;
		this.m_selectedRes = this.m_oldRes;
		ZLog.Log(string.Concat(new object[]
		{
			"Current res ",
			Screen.currentResolution.width,
			"x",
			Screen.currentResolution.height,
			"     ",
			Screen.width,
			"x",
			Screen.height
		}));
	}

	private void SetupKeys()
	{
		foreach (Settings.KeySetting keySetting in this.m_keys)
		{
			keySetting.m_keyTransform.GetComponentInChildren<Button>().onClick.AddListener(new UnityAction(this.OnKeySet));
		}
		this.UpdateBindings();
	}

	private void UpdateBindings()
	{
		foreach (Settings.KeySetting keySetting in this.m_keys)
		{
			keySetting.m_keyTransform.GetComponentInChildren<Button>().GetComponentInChildren<Text>().text = Localization.instance.GetBoundKeyString(keySetting.m_keyName);
		}
	}

	private void OnKeySet()
	{
		foreach (Settings.KeySetting keySetting in this.m_keys)
		{
			if (keySetting.m_keyTransform.GetComponentInChildren<Button>().gameObject == EventSystem.current.currentSelectedGameObject)
			{
				this.OpenBindDialog(keySetting.m_keyName);
				return;
			}
		}
		ZLog.Log("NOT FOUND");
	}

	private void OpenBindDialog(string keyName)
	{
		ZLog.Log("BInding key " + keyName);
		ZInput.instance.StartBindKey(keyName);
		this.m_bindDialog.SetActive(true);
	}

	private void UpdateBinding()
	{
		if (this.m_bindDialog.activeSelf && ZInput.instance.EndBindKey())
		{
			this.m_bindDialog.SetActive(false);
			this.UpdateBindings();
		}
	}

	public void ResetBindings()
	{
		ZInput.instance.Reset();
		this.UpdateBindings();
	}

	public void OnLanguageLeft()
	{
		this.m_languageKey = Localization.instance.GetPrevLanguage(this.m_languageKey);
		this.m_language.text = Localization.instance.Localize("$language_" + this.m_languageKey.ToLower());
	}

	public void OnLanguageRight()
	{
		this.m_languageKey = Localization.instance.GetNextLanguage(this.m_languageKey);
		this.m_language.text = Localization.instance.Localize("$language_" + this.m_languageKey.ToLower());
	}

	public void OnShowResList()
	{
		this.m_resDialog.SetActive(true);
		this.FillResList();
	}

	private void UpdateValidResolutions()
	{
		Resolution[] array = Screen.resolutions;
		if (array.Length == 0)
		{
			array = new Resolution[]
			{
				this.m_oldRes
			};
		}
		this.m_resolutions.Clear();
		foreach (Resolution item in array)
		{
			if ((item.width >= this.m_minResWidth && item.height >= this.m_minResHeight) || item.width == this.m_oldRes.width || item.height == this.m_oldRes.height)
			{
				this.m_resolutions.Add(item);
			}
		}
		if (this.m_resolutions.Count == 0)
		{
			Resolution item2 = default(Resolution);
			item2.width = 1280;
			item2.height = 720;
			item2.refreshRate = 60;
			this.m_resolutions.Add(item2);
		}
	}

	private void FillResList()
	{
		foreach (GameObject obj in this.m_resObjects)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_resObjects.Clear();
		this.UpdateValidResolutions();
		float num = 0f;
		foreach (Resolution resolution in this.m_resolutions)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_resListElement, this.m_resListRoot.transform);
			gameObject.SetActive(true);
			gameObject.GetComponentInChildren<Button>().onClick.AddListener(new UnityAction(this.OnResClick));
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, num * -this.m_resListSpace);
			gameObject.GetComponentInChildren<Text>().text = string.Concat(new object[]
			{
				resolution.width,
				"x",
				resolution.height,
				"  ",
				resolution.refreshRate,
				"hz"
			});
			this.m_resObjects.Add(gameObject);
			num += 1f;
		}
		float size = Mathf.Max(this.m_resListBaseSize, num * this.m_resListSpace);
		this.m_resListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		this.m_resListScroll.value = 1f;
	}

	public void OnResCancel()
	{
		this.m_resDialog.SetActive(false);
	}

	private void OnResClick()
	{
		this.m_resDialog.SetActive(false);
		GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
		for (int i = 0; i < this.m_resObjects.Count; i++)
		{
			if (currentSelectedGameObject == this.m_resObjects[i])
			{
				this.m_selectedRes = this.m_resolutions[i];
				return;
			}
		}
	}

	public void OnApplyMode()
	{
		this.ApplyMode();
		this.ShowResSwitchCountdown();
	}

	private void ApplyMode()
	{
		if (Screen.width == this.m_selectedRes.width && Screen.height == this.m_selectedRes.height && this.m_fullscreenToggle.isOn == Screen.fullScreen)
		{
			return;
		}
		Screen.SetResolution(this.m_selectedRes.width, this.m_selectedRes.height, this.m_fullscreenToggle.isOn);
		this.m_modeApplied = true;
	}

	private void RevertMode()
	{
		if (!this.m_modeApplied)
		{
			return;
		}
		this.m_modeApplied = false;
		this.m_selectedRes = this.m_oldRes;
		this.m_fullscreenToggle.isOn = this.m_oldFullscreen;
		Screen.SetResolution(this.m_oldRes.width, this.m_oldRes.height, this.m_oldFullscreen);
	}

	private void ShowResSwitchCountdown()
	{
		this.m_resSwitchDialog.SetActive(true);
		this.m_resCountdownTimer = 5f;
	}

	public void OnResSwitchOK()
	{
		this.m_resSwitchDialog.SetActive(false);
	}

	private void UpdateResSwitch(float dt)
	{
		if (this.m_resSwitchDialog.activeSelf)
		{
			this.m_resCountdownTimer -= dt;
			this.m_resSwitchCountdown.text = Mathf.CeilToInt(this.m_resCountdownTimer).ToString();
			if (this.m_resCountdownTimer <= 0f)
			{
				this.RevertMode();
				this.m_resSwitchDialog.SetActive(false);
			}
		}
	}

	public void OnResetTutorial()
	{
		Player.ResetSeenTutorials();
	}

	private static Settings m_instance;

	[Header("Inout")]
	public Slider m_sensitivitySlider;

	public Toggle m_invertMouse;

	public Toggle m_gamepadEnabled;

	public GameObject m_bindDialog;

	public List<Settings.KeySetting> m_keys = new List<Settings.KeySetting>();

	[Header("Misc")]
	public Toggle m_cameraShake;

	public Toggle m_shipCameraTilt;

	public Toggle m_quickPieceSelect;

	public Toggle m_showKeyHints;

	public Slider m_guiScaleSlider;

	public Text m_guiScaleText;

	public Text m_language;

	public Button m_resetTutorial;

	[Header("Audio")]
	public Slider m_volumeSlider;

	public Slider m_sfxVolumeSlider;

	public Slider m_musicVolumeSlider;

	public Toggle m_continousMusic;

	public AudioMixer m_masterMixer;

	[Header("Graphics")]
	public Toggle m_dofToggle;

	public Toggle m_vsyncToggle;

	public Toggle m_bloomToggle;

	public Toggle m_ssaoToggle;

	public Toggle m_sunshaftsToggle;

	public Toggle m_aaToggle;

	public Toggle m_caToggle;

	public Toggle m_motionblurToggle;

	public Toggle m_tesselationToggle;

	public Toggle m_softPartToggle;

	public Toggle m_fullscreenToggle;

	public Slider m_shadowQuality;

	public Text m_shadowQualityText;

	public Slider m_lod;

	public Text m_lodText;

	public Slider m_lights;

	public Text m_lightsText;

	public Slider m_vegetation;

	public Text m_vegetationText;

	public Text m_resButtonText;

	public GameObject m_resDialog;

	public GameObject m_resListElement;

	public RectTransform m_resListRoot;

	public Scrollbar m_resListScroll;

	public float m_resListSpace = 20f;

	public GameObject m_resSwitchDialog;

	public Text m_resSwitchCountdown;

	public int m_minResWidth = 1280;

	public int m_minResHeight = 720;

	private string m_languageKey = "";

	private bool m_oldFullscreen;

	private Resolution m_oldRes;

	private Resolution m_selectedRes;

	private List<GameObject> m_resObjects = new List<GameObject>();

	private List<Resolution> m_resolutions = new List<Resolution>();

	private float m_resListBaseSize;

	private bool m_modeApplied;

	private float m_resCountdownTimer = 1f;

	[Serializable]
	public class KeySetting
	{
		public string m_keyName = "";

		public RectTransform m_keyTransform;
	}
}
