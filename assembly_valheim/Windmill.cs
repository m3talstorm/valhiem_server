using System;
using UnityEngine;

public class Windmill : MonoBehaviour
{
	private void Start()
	{
		this.m_smelter = base.GetComponent<Smelter>();
		base.InvokeRepeating("CheckCover", 0.1f, 5f);
	}

	private void Update()
	{
		Quaternion to = Quaternion.LookRotation(-EnvMan.instance.GetWindDir());
		float powerOutput = this.GetPowerOutput();
		this.m_bom.rotation = Quaternion.RotateTowards(this.m_bom.rotation, to, this.m_bomRotationSpeed * powerOutput * Time.deltaTime);
		float num = powerOutput * this.m_propellerRotationSpeed;
		this.m_propAngle += num * Time.deltaTime;
		this.m_propeller.localRotation = Quaternion.Euler(0f, 0f, this.m_propAngle);
		if (this.m_smelter == null || this.m_smelter.IsActive())
		{
			this.m_grindStoneAngle += powerOutput * this.m_grindstoneRotationSpeed * Time.deltaTime;
		}
		this.m_grindstone.localRotation = Quaternion.Euler(0f, this.m_grindStoneAngle, 0f);
		this.m_propellerAOE.SetActive(Mathf.Abs(num) > this.m_minAOEPropellerSpeed);
		this.UpdateAudio(Time.deltaTime);
	}

	public float GetPowerOutput()
	{
		float num = Utils.LerpStep(this.m_minWindSpeed, 1f, EnvMan.instance.GetWindIntensity());
		return (1f - this.m_cover) * num;
	}

	private void CheckCover()
	{
		bool flag;
		Cover.GetCoverForPoint(this.m_propeller.transform.position, out this.m_cover, out flag);
	}

	private void UpdateAudio(float dt)
	{
		float powerOutput = this.GetPowerOutput();
		float target = Mathf.Lerp(this.m_minPitch, this.m_maxPitch, Mathf.Clamp01(powerOutput / this.m_maxPitchVel));
		float target2 = this.m_maxVol * Mathf.Clamp01(powerOutput / this.m_maxVolVel);
		foreach (AudioSource audioSource in this.m_sfxLoops)
		{
			audioSource.volume = Mathf.MoveTowards(audioSource.volume, target2, this.m_audioChangeSpeed * dt);
			audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, target, this.m_audioChangeSpeed * dt);
		}
	}

	public Transform m_propeller;

	public Transform m_grindstone;

	public Transform m_bom;

	public AudioSource[] m_sfxLoops;

	public GameObject m_propellerAOE;

	public float m_minAOEPropellerSpeed = 5f;

	public float m_bomRotationSpeed = 10f;

	public float m_propellerRotationSpeed = 10f;

	public float m_grindstoneRotationSpeed = 10f;

	public float m_minWindSpeed = 0.1f;

	public float m_minPitch = 1f;

	public float m_maxPitch = 1.5f;

	public float m_maxPitchVel = 10f;

	public float m_maxVol = 1f;

	public float m_maxVolVel = 10f;

	public float m_audioChangeSpeed = 2f;

	private float m_cover;

	private float m_propAngle;

	private float m_grindStoneAngle;

	private Smelter m_smelter;
}
