using System;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityStandardAssets.ImageEffects;

public class CameraEffects : MonoBehaviour
{
	public static CameraEffects instance
	{
		get
		{
			return CameraEffects.m_instance;
		}
	}

	private void Awake()
	{
		CameraEffects.m_instance = this;
		this.m_postProcessing = base.GetComponent<PostProcessingBehaviour>();
		this.m_dof = base.GetComponent<DepthOfField>();
		this.ApplySettings();
	}

	private void OnDestroy()
	{
		if (CameraEffects.m_instance == this)
		{
			CameraEffects.m_instance = null;
		}
	}

	public void ApplySettings()
	{
		this.SetDof(PlayerPrefs.GetInt("DOF", 1) == 1);
		this.SetBloom(PlayerPrefs.GetInt("Bloom", 1) == 1);
		this.SetSSAO(PlayerPrefs.GetInt("SSAO", 1) == 1);
		this.SetSunShafts(PlayerPrefs.GetInt("SunShafts", 1) == 1);
		this.SetAntiAliasing(PlayerPrefs.GetInt("AntiAliasing", 1) == 1);
		this.SetCA(PlayerPrefs.GetInt("ChromaticAberration", 1) == 1);
		this.SetMotionBlur(PlayerPrefs.GetInt("MotionBlur", 1) == 1);
	}

	public void SetSunShafts(bool enabled)
	{
		SunShafts component = base.GetComponent<SunShafts>();
		if (component != null)
		{
			component.enabled = enabled;
		}
	}

	private void SetBloom(bool enabled)
	{
		this.m_postProcessing.profile.bloom.enabled = enabled;
	}

	private void SetSSAO(bool enabled)
	{
		this.m_postProcessing.profile.ambientOcclusion.enabled = enabled;
	}

	private void SetMotionBlur(bool enabled)
	{
		this.m_postProcessing.profile.motionBlur.enabled = enabled;
	}

	private void SetAntiAliasing(bool enabled)
	{
		this.m_postProcessing.profile.antialiasing.enabled = enabled;
	}

	private void SetCA(bool enabled)
	{
		this.m_postProcessing.profile.chromaticAberration.enabled = enabled;
	}

	private void SetDof(bool enabled)
	{
		this.m_dof.enabled = (enabled || this.m_forceDof);
	}

	private void LateUpdate()
	{
		this.UpdateDOF();
	}

	private bool ControllingShip()
	{
		return Player.m_localPlayer == null || Player.m_localPlayer.GetControlledShip() != null;
	}

	private void UpdateDOF()
	{
		if (!this.m_dof.enabled || !this.m_dofAutoFocus)
		{
			return;
		}
		float num = this.m_dofMaxDistance;
		RaycastHit raycastHit;
		if (Physics.Raycast(base.transform.position, base.transform.forward, out raycastHit, this.m_dofMaxDistance, this.m_dofRayMask))
		{
			num = raycastHit.distance;
		}
		if (this.ControllingShip() && num < this.m_dofMinDistanceShip)
		{
			num = this.m_dofMinDistanceShip;
		}
		if (num < this.m_dofMinDistance)
		{
			num = this.m_dofMinDistance;
		}
		this.m_dof.focalLength = Mathf.Lerp(this.m_dof.focalLength, num, 0.2f);
	}

	private static CameraEffects m_instance;

	public bool m_forceDof;

	public LayerMask m_dofRayMask;

	public bool m_dofAutoFocus;

	public float m_dofMinDistance = 50f;

	public float m_dofMinDistanceShip = 50f;

	public float m_dofMaxDistance = 3000f;

	private PostProcessingBehaviour m_postProcessing;

	private DepthOfField m_dof;
}
