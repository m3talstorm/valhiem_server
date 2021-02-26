using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public class AudioMan : MonoBehaviour
{
	public static AudioMan instance
	{
		get
		{
			return AudioMan.m_instance;
		}
	}

	private void Awake()
	{
		if (AudioMan.m_instance != null)
		{
			ZLog.Log("Audioman already exist, destroying self");
			UnityEngine.Object.DestroyImmediate(base.gameObject);
			return;
		}
		AudioMan.m_instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		GameObject gameObject = new GameObject("ocean_ambient_loop");
		gameObject.transform.SetParent(base.transform);
		this.m_oceanAmbientSource = gameObject.AddComponent<AudioSource>();
		this.m_oceanAmbientSource.loop = true;
		this.m_oceanAmbientSource.spatialBlend = 0.75f;
		this.m_oceanAmbientSource.outputAudioMixerGroup = this.m_ambientMixer;
		this.m_oceanAmbientSource.maxDistance = 128f;
		this.m_oceanAmbientSource.minDistance = 40f;
		this.m_oceanAmbientSource.spread = 90f;
		this.m_oceanAmbientSource.rolloffMode = AudioRolloffMode.Linear;
		this.m_oceanAmbientSource.clip = this.m_oceanAudio;
		this.m_oceanAmbientSource.bypassReverbZones = true;
		this.m_oceanAmbientSource.dopplerLevel = 0f;
		this.m_oceanAmbientSource.volume = 0f;
		this.m_oceanAmbientSource.Play();
		GameObject gameObject2 = new GameObject("ambient_loop");
		gameObject2.transform.SetParent(base.transform);
		this.m_ambientLoopSource = gameObject2.AddComponent<AudioSource>();
		this.m_ambientLoopSource.loop = true;
		this.m_ambientLoopSource.spatialBlend = 0f;
		this.m_ambientLoopSource.outputAudioMixerGroup = this.m_ambientMixer;
		this.m_ambientLoopSource.bypassReverbZones = true;
		this.m_ambientLoopSource.volume = 0f;
		GameObject gameObject3 = new GameObject("wind_loop");
		gameObject3.transform.SetParent(base.transform);
		this.m_windLoopSource = gameObject3.AddComponent<AudioSource>();
		this.m_windLoopSource.loop = true;
		this.m_windLoopSource.spatialBlend = 0f;
		this.m_windLoopSource.outputAudioMixerGroup = this.m_ambientMixer;
		this.m_windLoopSource.bypassReverbZones = true;
		this.m_windLoopSource.clip = this.m_windAudio;
		this.m_windLoopSource.volume = 0f;
		this.m_windLoopSource.Play();
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			AudioListener.volume = 0f;
			return;
		}
		AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", AudioListener.volume);
		AudioMan.SetSFXVolume(PlayerPrefs.GetFloat("SfxVolume", AudioMan.GetSFXVolume()));
	}

	private void OnDestroy()
	{
		if (AudioMan.m_instance == this)
		{
			AudioMan.m_instance = null;
		}
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		this.UpdateAmbientLoop(deltaTime);
		this.UpdateRandomAmbient(deltaTime);
		this.UpdateSnapshots(deltaTime);
	}

	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		this.UpdateOceanAmbiance(fixedDeltaTime);
		this.UpdateWindAmbience(fixedDeltaTime);
	}

	public static float GetSFXVolume()
	{
		if (AudioMan.m_instance == null)
		{
			return 1f;
		}
		float num;
		AudioMan.m_instance.m_masterMixer.GetFloat("SfxVol", out num);
		return Mathf.Pow(10f, num / 20f);
	}

	public static void SetSFXVolume(float vol)
	{
		if (AudioMan.m_instance == null)
		{
			return;
		}
		float value = Mathf.Log(Mathf.Clamp(vol, 0.001f, 1f)) * 10f;
		AudioMan.m_instance.m_masterMixer.SetFloat("SfxVol", value);
	}

	private void UpdateRandomAmbient(float dt)
	{
		if (this.InMenu())
		{
			return;
		}
		this.m_randomAmbientTimer += dt;
		if (this.m_randomAmbientTimer > this.m_randomAmbientInterval)
		{
			this.m_randomAmbientTimer = 0f;
			if (UnityEngine.Random.value <= this.m_randomAmbientChance)
			{
				AudioClip audioClip = this.SelectRandomAmbientClip();
				if (audioClip)
				{
					Vector3 randomAmbiencePoint = this.GetRandomAmbiencePoint();
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_randomAmbientPrefab, randomAmbiencePoint, Quaternion.identity, base.transform);
					gameObject.GetComponent<AudioSource>().pitch = UnityEngine.Random.Range(this.m_randomMinPitch, this.m_randomMaxPitch);
					ZSFX component = gameObject.GetComponent<ZSFX>();
					component.m_audioClips = new AudioClip[]
					{
						audioClip
					};
					component.Play();
					component.FadeOut();
				}
			}
		}
	}

	private Vector3 GetRandomAmbiencePoint()
	{
		Vector3 a = Vector3.zero;
		Camera mainCamera = Utils.GetMainCamera();
		if (Player.m_localPlayer)
		{
			a = Player.m_localPlayer.transform.position;
		}
		else if (mainCamera)
		{
			a = mainCamera.transform.position;
		}
		float f = UnityEngine.Random.value * 3.1415927f * 2f;
		float num = UnityEngine.Random.Range(this.m_randomMinDistance, this.m_randomMaxDistance);
		return a + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
	}

	private AudioClip SelectRandomAmbientClip()
	{
		if (EnvMan.instance == null)
		{
			return null;
		}
		EnvSetup currentEnvironment = EnvMan.instance.GetCurrentEnvironment();
		AudioMan.BiomeAmbients biomeAmbients;
		if (currentEnvironment != null && !string.IsNullOrEmpty(currentEnvironment.m_ambientList))
		{
			biomeAmbients = this.GetAmbients(currentEnvironment.m_ambientList);
		}
		else
		{
			biomeAmbients = this.GetBiomeAmbients(EnvMan.instance.GetCurrentBiome());
		}
		if (biomeAmbients == null)
		{
			return null;
		}
		List<AudioClip> list = new List<AudioClip>(biomeAmbients.m_randomAmbientClips);
		List<AudioClip> collection = EnvMan.instance.IsDaylight() ? biomeAmbients.m_randomAmbientClipsDay : biomeAmbients.m_randomAmbientClipsNight;
		list.AddRange(collection);
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private void UpdateAmbientLoop(float dt)
	{
		if (EnvMan.instance == null)
		{
			this.m_ambientLoopSource.Stop();
			return;
		}
		if (this.m_queuedAmbientLoop || this.m_stopAmbientLoop)
		{
			if (this.m_ambientLoopSource.isPlaying && this.m_ambientLoopSource.volume > 0f)
			{
				this.m_ambientLoopSource.volume = Mathf.MoveTowards(this.m_ambientLoopSource.volume, 0f, dt / this.m_ambientFadeTime);
				return;
			}
			this.m_ambientLoopSource.Stop();
			this.m_stopAmbientLoop = false;
			if (this.m_queuedAmbientLoop)
			{
				this.m_ambientLoopSource.clip = this.m_queuedAmbientLoop;
				this.m_ambientLoopSource.volume = 0f;
				this.m_ambientLoopSource.Play();
				this.m_ambientVol = this.m_queuedAmbientVol;
				this.m_queuedAmbientLoop = null;
				return;
			}
		}
		else if (this.m_ambientLoopSource.isPlaying)
		{
			this.m_ambientLoopSource.volume = Mathf.MoveTowards(this.m_ambientLoopSource.volume, this.m_ambientVol, dt / this.m_ambientFadeTime);
		}
	}

	public void SetIndoor(bool indoor)
	{
		this.m_indoor = indoor;
	}

	private bool InMenu()
	{
		return FejdStartup.instance != null || Menu.IsVisible() || (Game.instance && Game.instance.WaitingForRespawn()) || TextViewer.IsShowingIntro();
	}

	private void UpdateSnapshots(float dt)
	{
		if (this.InMenu())
		{
			this.SetSnapshot(AudioMan.Snapshot.Menu);
			return;
		}
		if (this.m_indoor)
		{
			this.SetSnapshot(AudioMan.Snapshot.Indoor);
			return;
		}
		this.SetSnapshot(AudioMan.Snapshot.Default);
	}

	private void SetSnapshot(AudioMan.Snapshot snapshot)
	{
		if (this.m_currentSnapshot == snapshot)
		{
			return;
		}
		this.m_currentSnapshot = snapshot;
		switch (snapshot)
		{
		case AudioMan.Snapshot.Default:
			this.m_masterMixer.FindSnapshot("Default").TransitionTo(this.m_snapshotTransitionTime);
			return;
		case AudioMan.Snapshot.Menu:
			this.m_masterMixer.FindSnapshot("Menu").TransitionTo(this.m_snapshotTransitionTime);
			return;
		case AudioMan.Snapshot.Indoor:
			this.m_masterMixer.FindSnapshot("Indoor").TransitionTo(this.m_snapshotTransitionTime);
			return;
		default:
			return;
		}
	}

	public void StopAmbientLoop()
	{
		this.m_queuedAmbientLoop = null;
		this.m_stopAmbientLoop = true;
	}

	public void QueueAmbientLoop(AudioClip clip, float vol)
	{
		if (this.m_queuedAmbientLoop == clip && this.m_queuedAmbientVol == vol)
		{
			return;
		}
		if (this.m_queuedAmbientLoop == null && this.m_ambientLoopSource.clip == clip && this.m_ambientVol == vol)
		{
			return;
		}
		this.m_queuedAmbientLoop = clip;
		this.m_queuedAmbientVol = vol;
		this.m_stopAmbientLoop = false;
	}

	private void UpdateWindAmbience(float dt)
	{
		if (ZoneSystem.instance == null)
		{
			this.m_windLoopSource.volume = 0f;
			return;
		}
		float num = EnvMan.instance.GetWindIntensity();
		num = Mathf.Pow(num, this.m_windIntensityPower);
		num += num * Mathf.Sin(Time.time) * Mathf.Sin(Time.time * 1.54323f) * Mathf.Sin(Time.time * 2.31237f) * this.m_windVariation;
		this.m_windLoopSource.volume = Mathf.Lerp(this.m_windMinVol, this.m_windMaxVol, num);
		this.m_windLoopSource.pitch = Mathf.Lerp(this.m_windMinPitch, this.m_windMaxPitch, num);
	}

	private void UpdateOceanAmbiance(float dt)
	{
		if (ZoneSystem.instance == null)
		{
			this.m_oceanAmbientSource.volume = 0f;
			return;
		}
		this.m_oceanUpdateTimer += dt;
		if (this.m_oceanUpdateTimer > 2f)
		{
			this.m_oceanUpdateTimer = 0f;
			this.m_haveOcean = this.FindAverageOceanPoint(out this.m_avgOceanPoint);
		}
		if (this.m_haveOcean)
		{
			float windIntensity = EnvMan.instance.GetWindIntensity();
			float target = Mathf.Lerp(this.m_oceanVolumeMin, this.m_oceanVolumeMax, windIntensity);
			this.m_oceanAmbientSource.volume = Mathf.MoveTowards(this.m_oceanAmbientSource.volume, target, this.m_oceanFadeSpeed * dt);
			this.m_oceanAmbientSource.transform.position = Vector3.Lerp(this.m_oceanAmbientSource.transform.position, this.m_avgOceanPoint, this.m_oceanMoveSpeed);
			return;
		}
		this.m_oceanAmbientSource.volume = Mathf.MoveTowards(this.m_oceanAmbientSource.volume, 0f, this.m_oceanFadeSpeed * dt);
	}

	private bool FindAverageOceanPoint(out Vector3 point)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			point = Vector3.zero;
			return false;
		}
		Vector3 vector = Vector3.zero;
		int num = 0;
		Vector3 position = mainCamera.transform.position;
		Vector2i zone = ZoneSystem.instance.GetZone(position);
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				Vector2i id = zone;
				id.x += j;
				id.y += i;
				Vector3 zonePos = ZoneSystem.instance.GetZonePos(id);
				if (this.IsOceanZone(zonePos))
				{
					num++;
					vector += zonePos;
				}
			}
		}
		if (num > 0)
		{
			vector /= (float)num;
			point = vector;
			point.y = ZoneSystem.instance.m_waterLevel;
			return true;
		}
		point = Vector3.zero;
		return false;
	}

	private bool IsOceanZone(Vector3 centerPos)
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(centerPos);
		return ZoneSystem.instance.m_waterLevel - groundHeight > this.m_oceanDepthTreshold;
	}

	private AudioMan.BiomeAmbients GetAmbients(string name)
	{
		foreach (AudioMan.BiomeAmbients biomeAmbients in this.m_randomAmbients)
		{
			if (biomeAmbients.m_name == name)
			{
				return biomeAmbients;
			}
		}
		return null;
	}

	private AudioMan.BiomeAmbients GetBiomeAmbients(Heightmap.Biome biome)
	{
		foreach (AudioMan.BiomeAmbients biomeAmbients in this.m_randomAmbients)
		{
			if ((biomeAmbients.m_biome & biome) != Heightmap.Biome.None)
			{
				return biomeAmbients;
			}
		}
		return null;
	}

	private static AudioMan m_instance;

	[Header("Mixers")]
	public AudioMixerGroup m_ambientMixer;

	public AudioMixer m_masterMixer;

	public float m_snapshotTransitionTime = 2f;

	[Header("Wind")]
	public AudioClip m_windAudio;

	public float m_windMinVol;

	public float m_windMaxVol = 1f;

	public float m_windMinPitch = 0.5f;

	public float m_windMaxPitch = 1.5f;

	public float m_windVariation = 0.2f;

	public float m_windIntensityPower = 1.5f;

	[Header("Ocean")]
	public AudioClip m_oceanAudio;

	public float m_oceanVolumeMax = 1f;

	public float m_oceanVolumeMin = 1f;

	public float m_oceanFadeSpeed = 0.1f;

	public float m_oceanMoveSpeed = 0.1f;

	public float m_oceanDepthTreshold = 10f;

	[Header("Random ambients")]
	public float m_ambientFadeTime = 2f;

	public float m_randomAmbientInterval = 5f;

	public float m_randomAmbientChance = 0.5f;

	public float m_randomMinPitch = 0.9f;

	public float m_randomMaxPitch = 1.1f;

	public float m_randomMinVol = 0.2f;

	public float m_randomMaxVol = 0.4f;

	public float m_randomPan = 0.2f;

	public float m_randomFadeIn = 0.2f;

	public float m_randomFadeOut = 2f;

	public float m_randomMinDistance = 5f;

	public float m_randomMaxDistance = 20f;

	public List<AudioMan.BiomeAmbients> m_randomAmbients = new List<AudioMan.BiomeAmbients>();

	public GameObject m_randomAmbientPrefab;

	private AudioSource m_oceanAmbientSource;

	private AudioSource m_ambientLoopSource;

	private AudioSource m_windLoopSource;

	private AudioClip m_queuedAmbientLoop;

	private float m_queuedAmbientVol;

	private float m_ambientVol;

	private float m_randomAmbientTimer;

	private bool m_stopAmbientLoop;

	private bool m_indoor;

	private float m_oceanUpdateTimer;

	private bool m_haveOcean;

	private Vector3 m_avgOceanPoint = Vector3.zero;

	private AudioMan.Snapshot m_currentSnapshot;

	[Serializable]
	public class BiomeAmbients
	{
		public string m_name = "";

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		public List<AudioClip> m_randomAmbientClips = new List<AudioClip>();

		public List<AudioClip> m_randomAmbientClipsDay = new List<AudioClip>();

		public List<AudioClip> m_randomAmbientClipsNight = new List<AudioClip>();
	}

	private enum Snapshot
	{
		Default,
		Menu,
		Indoor
	}
}
