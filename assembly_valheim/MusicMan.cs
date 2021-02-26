using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicMan : MonoBehaviour
{
	public static MusicMan instance
	{
		get
		{
			return MusicMan.m_instance;
		}
	}

	private void Awake()
	{
		if (MusicMan.m_instance)
		{
			return;
		}
		MusicMan.m_instance = this;
		GameObject gameObject = new GameObject("music");
		gameObject.transform.SetParent(base.transform);
		this.m_musicSource = gameObject.AddComponent<AudioSource>();
		this.m_musicSource.loop = true;
		this.m_musicSource.spatialBlend = 0f;
		this.m_musicSource.outputAudioMixerGroup = this.m_musicMixer;
		this.m_musicSource.bypassReverbZones = true;
		this.m_randomAmbientInterval = UnityEngine.Random.Range(this.m_randomMusicIntervalMin, this.m_randomMusicIntervalMax);
		MusicMan.m_masterMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
		this.ApplySettings();
		foreach (MusicMan.NamedMusic namedMusic in this.m_music)
		{
			foreach (AudioClip audioClip in namedMusic.m_clips)
			{
				if (audioClip == null || !audioClip)
				{
					namedMusic.m_enabled = false;
					ZLog.LogWarning("Missing audio clip in music " + namedMusic.m_name);
					break;
				}
			}
		}
	}

	public void ApplySettings()
	{
		bool flag = PlayerPrefs.GetInt("ContinousMusic", 0) == 1;
		foreach (MusicMan.NamedMusic namedMusic in this.m_music)
		{
			if (namedMusic.m_ambientMusic)
			{
				namedMusic.m_loop = flag;
				if (!flag && this.GetCurrentMusic() == namedMusic.m_name && this.m_musicSource.loop)
				{
					this.StopMusic();
				}
			}
		}
	}

	private void OnDestroy()
	{
		if (MusicMan.m_instance == this)
		{
			MusicMan.m_instance = null;
		}
	}

	private void Update()
	{
		if (MusicMan.m_instance != this)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		this.UpdateCurrentMusic(deltaTime);
		this.UpdateCombatMusic(deltaTime);
		this.UpdateMusic(deltaTime);
	}

	private void UpdateCurrentMusic(float dt)
	{
		string currentMusic = this.GetCurrentMusic();
		if (Game.instance != null)
		{
			if (Player.m_localPlayer == null)
			{
				this.StartMusic("respawn");
				return;
			}
			if (currentMusic == "respawn")
			{
				this.StopMusic();
			}
		}
		if (Player.m_localPlayer && Player.m_localPlayer.InIntro())
		{
			this.StartMusic("intro");
			return;
		}
		if (currentMusic == "intro")
		{
			this.StopMusic();
		}
		if (this.HandleEventMusic(currentMusic))
		{
			return;
		}
		if (this.HandleSailingMusic(dt, currentMusic))
		{
			return;
		}
		if (this.HandleTriggerMusic(currentMusic))
		{
			return;
		}
		this.HandleEnvironmentMusic(dt, currentMusic);
	}

	private bool HandleEnvironmentMusic(float dt, string currentMusic)
	{
		if (!EnvMan.instance)
		{
			return false;
		}
		MusicMan.NamedMusic environmentMusic = this.GetEnvironmentMusic();
		if (environmentMusic == null || (!environmentMusic.m_loop && environmentMusic.m_name != this.GetCurrentMusic()))
		{
			this.StopMusic();
			return true;
		}
		if (!environmentMusic.m_loop)
		{
			if (Time.time - this.m_lastAmbientMusicTime < this.m_randomAmbientInterval)
			{
				return false;
			}
			this.m_randomAmbientInterval = UnityEngine.Random.Range(this.m_randomMusicIntervalMin, this.m_randomMusicIntervalMax);
			this.m_lastAmbientMusicTime = Time.time;
		}
		this.StartMusic(environmentMusic);
		return true;
	}

	private MusicMan.NamedMusic GetEnvironmentMusic()
	{
		string name;
		if (Player.m_localPlayer && Player.m_localPlayer.IsSafeInHome())
		{
			name = "home";
		}
		else
		{
			name = EnvMan.instance.GetAmbientMusic();
		}
		return this.FindMusic(name);
	}

	private bool HandleTriggerMusic(string currentMusic)
	{
		if (this.m_triggerMusic != null)
		{
			this.StartMusic(this.m_triggerMusic);
			this.m_triggeredMusic = this.m_triggerMusic;
			this.m_triggerMusic = null;
			return true;
		}
		if (this.m_triggeredMusic != null)
		{
			if (currentMusic == this.m_triggeredMusic)
			{
				return true;
			}
			this.m_triggeredMusic = null;
		}
		return false;
	}

	private bool HandleEventMusic(string currentMusic)
	{
		if (RandEventSystem.instance)
		{
			string musicOverride = RandEventSystem.instance.GetMusicOverride();
			if (musicOverride != null)
			{
				this.StartMusic(musicOverride);
				this.m_randomEventMusic = musicOverride;
				return true;
			}
			if (currentMusic == this.m_randomEventMusic)
			{
				this.m_randomEventMusic = null;
				this.StopMusic();
			}
		}
		return false;
	}

	private bool HandleCombatMusic(string currentMusic)
	{
		if (this.InCombat())
		{
			this.StartMusic("combat");
			return true;
		}
		if (currentMusic == "combat")
		{
			this.StopMusic();
		}
		return false;
	}

	private bool HandleSailingMusic(float dt, string currentMusic)
	{
		if (this.IsSailing())
		{
			this.m_notSailDuration = 0f;
			this.m_sailDuration += dt;
			if (this.m_sailDuration > this.m_sailMusicMinSailTime)
			{
				this.StartMusic("sailing");
				return true;
			}
		}
		else
		{
			this.m_sailDuration = 0f;
			this.m_notSailDuration += dt;
			if (this.m_notSailDuration > this.m_sailMusicMinSailTime / 2f && currentMusic == "sailing")
			{
				this.StopMusic();
			}
		}
		return false;
	}

	private bool IsSailing()
	{
		if (!Player.m_localPlayer)
		{
			return false;
		}
		Ship localShip = Ship.GetLocalShip();
		return localShip && localShip.GetSpeed() > this.m_sailMusicShipSpeedThreshold;
	}

	private void UpdateMusic(float dt)
	{
		if (this.m_queuedMusic != null || this.m_stopMusic)
		{
			if (this.m_musicSource.isPlaying && this.m_currentMusicVol > 0f)
			{
				float num = (this.m_queuedMusic != null) ? Mathf.Min(this.m_queuedMusic.m_fadeInTime, this.m_musicFadeTime) : this.m_musicFadeTime;
				this.m_currentMusicVol = Mathf.MoveTowards(this.m_currentMusicVol, 0f, dt / num);
				this.m_musicSource.volume = Utils.SmoothStep(0f, 1f, this.m_currentMusicVol) * this.m_musicVolume * MusicMan.m_masterMusicVolume;
				return;
			}
			if (this.m_musicSource.isPlaying && this.m_currentMusic != null && this.m_currentMusic.m_loop && this.m_currentMusic.m_resume)
			{
				this.m_currentMusic.m_lastPlayedTime = Time.time;
				this.m_currentMusic.m_savedPlaybackPos = this.m_musicSource.timeSamples;
				ZLog.Log(string.Concat(new object[]
				{
					"Stoped music ",
					this.m_currentMusic.m_name,
					" at ",
					this.m_currentMusic.m_savedPlaybackPos
				}));
			}
			this.m_musicSource.Stop();
			this.m_stopMusic = false;
			this.m_currentMusic = null;
			if (this.m_queuedMusic != null)
			{
				this.m_musicSource.clip = this.m_queuedMusic.m_clips[UnityEngine.Random.Range(0, this.m_queuedMusic.m_clips.Length)];
				this.m_musicSource.loop = this.m_queuedMusic.m_loop;
				this.m_musicSource.volume = 0f;
				this.m_musicSource.timeSamples = 0;
				this.m_musicSource.Play();
				if (this.m_queuedMusic.m_loop && this.m_queuedMusic.m_resume && Time.time - this.m_queuedMusic.m_lastPlayedTime < this.m_musicSource.clip.length * 2f)
				{
					this.m_musicSource.timeSamples = this.m_queuedMusic.m_savedPlaybackPos;
					ZLog.Log(string.Concat(new object[]
					{
						"Resumed music ",
						this.m_queuedMusic.m_name,
						" at ",
						this.m_queuedMusic.m_savedPlaybackPos
					}));
				}
				this.m_currentMusicVol = 0f;
				this.m_musicVolume = this.m_queuedMusic.m_volume;
				this.m_musicFadeTime = this.m_queuedMusic.m_fadeInTime;
				this.m_alwaysFadeout = this.m_queuedMusic.m_alwaysFadeout;
				this.m_currentMusic = this.m_queuedMusic;
				this.m_queuedMusic = null;
				return;
			}
		}
		else if (this.m_musicSource.isPlaying)
		{
			float num2 = this.m_musicSource.clip.length - this.m_musicSource.time;
			if (this.m_alwaysFadeout && !this.m_musicSource.loop && num2 < this.m_musicFadeTime)
			{
				this.m_currentMusicVol = Mathf.MoveTowards(this.m_currentMusicVol, 0f, dt / this.m_musicFadeTime);
				this.m_musicSource.volume = Utils.SmoothStep(0f, 1f, this.m_currentMusicVol) * this.m_musicVolume * MusicMan.m_masterMusicVolume;
				return;
			}
			this.m_currentMusicVol = Mathf.MoveTowards(this.m_currentMusicVol, 1f, dt / this.m_musicFadeTime);
			this.m_musicSource.volume = Utils.SmoothStep(0f, 1f, this.m_currentMusicVol) * this.m_musicVolume * MusicMan.m_masterMusicVolume;
			return;
		}
		else if (this.m_currentMusic != null && !this.m_musicSource.isPlaying)
		{
			this.m_currentMusic = null;
		}
	}

	private void UpdateCombatMusic(float dt)
	{
		if (this.m_combatTimer > 0f)
		{
			this.m_combatTimer -= Time.deltaTime;
		}
	}

	public void ResetCombatTimer()
	{
		this.m_combatTimer = this.m_combatMusicTimeout;
	}

	private bool InCombat()
	{
		return this.m_combatTimer > 0f;
	}

	public void TriggerMusic(string name)
	{
		this.m_triggerMusic = name;
	}

	private void StartMusic(string name)
	{
		if (this.GetCurrentMusic() == name)
		{
			return;
		}
		MusicMan.NamedMusic music = this.FindMusic(name);
		this.StartMusic(music);
	}

	private void StartMusic(MusicMan.NamedMusic music)
	{
		if (music != null && this.GetCurrentMusic() == music.m_name)
		{
			return;
		}
		if (music != null)
		{
			this.m_queuedMusic = music;
			this.m_stopMusic = false;
			return;
		}
		this.StopMusic();
	}

	private MusicMan.NamedMusic FindMusic(string name)
	{
		if (name == null || name.Length == 0)
		{
			return null;
		}
		foreach (MusicMan.NamedMusic namedMusic in this.m_music)
		{
			if (namedMusic.m_name == name && namedMusic.m_enabled && namedMusic.m_clips.Length != 0 && namedMusic.m_clips[0])
			{
				return namedMusic;
			}
		}
		return null;
	}

	public bool IsPlaying()
	{
		return this.m_musicSource.isPlaying;
	}

	private string GetCurrentMusic()
	{
		if (this.m_stopMusic)
		{
			return "";
		}
		if (this.m_queuedMusic != null)
		{
			return this.m_queuedMusic.m_name;
		}
		if (this.m_currentMusic != null)
		{
			return this.m_currentMusic.m_name;
		}
		return "";
	}

	private void StopMusic()
	{
		this.m_queuedMusic = null;
		this.m_stopMusic = true;
	}

	public void Reset()
	{
		this.StopMusic();
		this.m_combatTimer = 0f;
		this.m_randomEventMusic = null;
		this.m_triggerMusic = null;
	}

	private string m_triggeredMusic = "";

	private static MusicMan m_instance;

	public static float m_masterMusicVolume = 1f;

	public AudioMixerGroup m_musicMixer;

	public List<MusicMan.NamedMusic> m_music = new List<MusicMan.NamedMusic>();

	[Header("Combat")]
	public float m_combatMusicTimeout = 4f;

	[Header("Sailing")]
	public float m_sailMusicShipSpeedThreshold = 3f;

	public float m_sailMusicMinSailTime = 20f;

	[Header("Ambient music")]
	public float m_randomMusicIntervalMin = 300f;

	public float m_randomMusicIntervalMax = 500f;

	private MusicMan.NamedMusic m_queuedMusic;

	private MusicMan.NamedMusic m_currentMusic;

	private float m_musicVolume = 1f;

	private float m_musicFadeTime = 3f;

	private bool m_alwaysFadeout;

	private bool m_stopMusic;

	private string m_randomEventMusic;

	private float m_lastAmbientMusicTime;

	private float m_randomAmbientInterval;

	private string m_triggerMusic;

	private float m_combatTimer;

	private AudioSource m_musicSource;

	private float m_currentMusicVol;

	private float m_sailDuration;

	private float m_notSailDuration;

	[Serializable]
	public class NamedMusic
	{
		public string m_name = "";

		public AudioClip[] m_clips;

		public float m_volume = 1f;

		public float m_fadeInTime = 3f;

		public bool m_alwaysFadeout;

		public bool m_loop;

		public bool m_resume;

		public bool m_enabled = true;

		public bool m_ambientMusic;

		[NonSerialized]
		public int m_savedPlaybackPos;

		[NonSerialized]
		public float m_lastPlayedTime;
	}
}
