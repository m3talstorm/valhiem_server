using System;
using UnityEngine;

public class ZSFX : MonoBehaviour
{
	public void Awake()
	{
		this.m_delay = UnityEngine.Random.Range(this.m_minDelay, this.m_maxDelay);
		this.m_audioSource = base.GetComponent<AudioSource>();
		this.m_baseSpread = this.m_audioSource.spread;
	}

	private void OnDisable()
	{
		if (this.m_playOnAwake && this.m_audioSource.loop)
		{
			this.m_time = 0f;
			this.m_delay = UnityEngine.Random.Range(this.m_minDelay, this.m_maxDelay);
			this.m_audioSource.Stop();
		}
	}

	public void Update()
	{
		if (this.m_audioSource == null)
		{
			return;
		}
		this.m_time += Time.deltaTime;
		if (this.m_delay >= 0f && this.m_time >= this.m_delay)
		{
			this.m_delay = -1f;
			if (this.m_playOnAwake)
			{
				this.Play();
			}
		}
		if (this.m_audioSource.isPlaying)
		{
			if (this.m_distanceReverb && this.m_audioSource.loop)
			{
				this.m_updateReverbTimer += Time.deltaTime;
				if (this.m_updateReverbTimer > 1f)
				{
					this.m_updateReverbTimer = 0f;
					this.UpdateReverb();
				}
			}
			if (this.m_fadeOutOnAwake && this.m_time > this.m_fadeOutDelay)
			{
				this.m_fadeOutOnAwake = false;
				this.FadeOut();
			}
			if (this.m_fadeOutTimer >= 0f)
			{
				this.m_fadeOutTimer += Time.deltaTime;
				if (this.m_fadeOutTimer >= this.m_fadeOutDuration)
				{
					this.m_audioSource.volume = 0f;
					this.Stop();
					return;
				}
				float num = Mathf.Clamp01(this.m_fadeOutTimer / this.m_fadeOutDuration);
				this.m_audioSource.volume = (1f - num) * this.m_vol;
				return;
			}
			else if (this.m_fadeInTimer >= 0f)
			{
				this.m_fadeInTimer += Time.deltaTime;
				float num2 = Mathf.Clamp01(this.m_fadeInTimer / this.m_fadeInDuration);
				this.m_audioSource.volume = num2 * this.m_vol;
				if (this.m_fadeInTimer > this.m_fadeInDuration)
				{
					this.m_fadeInTimer = -1f;
				}
			}
		}
	}

	public void FadeOut()
	{
		if (this.m_fadeOutTimer < 0f)
		{
			this.m_fadeOutTimer = 0f;
		}
	}

	public void Stop()
	{
		if (this.m_audioSource != null)
		{
			this.m_audioSource.Stop();
		}
	}

	public bool IsPlaying()
	{
		return !(this.m_audioSource == null) && this.m_audioSource.isPlaying;
	}

	private void UpdateReverb()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (this.m_distanceReverb && this.m_audioSource.spatialBlend != 0f && mainCamera != null)
		{
			float num = Vector3.Distance(mainCamera.transform.position, base.transform.position);
			float num2 = this.m_useCustomReverbDistance ? this.m_customReverbDistance : 64f;
			float a = Mathf.Clamp01(num / num2);
			float b = Mathf.Clamp01(this.m_audioSource.maxDistance / num2) * Mathf.Clamp01(num / this.m_audioSource.maxDistance);
			float num3 = Mathf.Max(a, b);
			this.m_audioSource.bypassReverbZones = false;
			this.m_audioSource.reverbZoneMix = num3;
			if (this.m_baseSpread < 120f)
			{
				float a2 = Mathf.Max(this.m_baseSpread, 45f);
				this.m_audioSource.spread = Mathf.Lerp(a2, 120f, num3);
				return;
			}
		}
		else
		{
			this.m_audioSource.bypassReverbZones = true;
		}
	}

	public void Play()
	{
		if (this.m_audioSource == null)
		{
			return;
		}
		if (this.m_audioClips.Length == 0)
		{
			return;
		}
		if (!this.m_audioSource.gameObject.activeInHierarchy)
		{
			return;
		}
		int num = UnityEngine.Random.Range(0, this.m_audioClips.Length);
		this.m_audioSource.clip = this.m_audioClips[num];
		this.m_audioSource.pitch = UnityEngine.Random.Range(this.m_minPitch, this.m_maxPitch);
		if (this.m_randomPan)
		{
			this.m_audioSource.panStereo = UnityEngine.Random.Range(this.m_minPan, this.m_maxPan);
		}
		this.m_vol = UnityEngine.Random.Range(this.m_minVol, this.m_maxVol);
		if (this.m_fadeInDuration > 0f)
		{
			this.m_audioSource.volume = 0f;
			this.m_fadeInTimer = 0f;
		}
		else
		{
			this.m_audioSource.volume = this.m_vol;
		}
		this.UpdateReverb();
		this.m_audioSource.Play();
	}

	public bool m_playOnAwake = true;

	[Header("Clips")]
	public AudioClip[] m_audioClips = new AudioClip[0];

	[Header("Random")]
	public float m_maxPitch = 1f;

	public float m_minPitch = 1f;

	public float m_maxVol = 1f;

	public float m_minVol = 1f;

	[Header("Fade")]
	public float m_fadeInDuration;

	public float m_fadeOutDuration;

	public float m_fadeOutDelay;

	public bool m_fadeOutOnAwake;

	[Header("Pan")]
	public bool m_randomPan;

	public float m_minPan = -1f;

	public float m_maxPan = 1f;

	[Header("Delay")]
	public float m_maxDelay;

	public float m_minDelay;

	[Header("Reverb")]
	public bool m_distanceReverb = true;

	public bool m_useCustomReverbDistance;

	public float m_customReverbDistance = 10f;

	private const float m_globalReverbDistance = 64f;

	private const float m_minReverbSpread = 45f;

	private const float m_maxReverbSpread = 120f;

	private float m_delay;

	private float m_time;

	private float m_fadeOutTimer = -1f;

	private float m_fadeInTimer = -1f;

	private float m_vol = 1f;

	private float m_baseSpread;

	private float m_updateReverbTimer;

	private AudioSource m_audioSource;
}
