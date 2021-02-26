using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelEffects : MonoBehaviour
{
	private void Start()
	{
		this.m_character = base.GetComponentInParent<Character>();
		Character character = this.m_character;
		character.m_onLevelSet = (Action<int>)Delegate.Combine(character.m_onLevelSet, new Action<int>(this.OnLevelSet));
		this.SetupLevelVisualization(this.m_character.GetLevel());
	}

	private void OnLevelSet(int level)
	{
		this.SetupLevelVisualization(level);
	}

	private void SetupLevelVisualization(int level)
	{
		if (level <= 1)
		{
			return;
		}
		if (this.m_levelSetups.Count >= level - 1)
		{
			LevelEffects.LevelSetup levelSetup = this.m_levelSetups[level - 2];
			base.transform.localScale = new Vector3(levelSetup.m_scale, levelSetup.m_scale, levelSetup.m_scale);
			if (this.m_mainRender)
			{
				string key = this.m_character.m_name + level.ToString();
				Material material;
				if (LevelEffects.m_materials.TryGetValue(key, out material))
				{
					Material[] sharedMaterials = this.m_mainRender.sharedMaterials;
					sharedMaterials[0] = material;
					this.m_mainRender.sharedMaterials = sharedMaterials;
				}
				else
				{
					Material[] sharedMaterials2 = this.m_mainRender.sharedMaterials;
					sharedMaterials2[0] = new Material(sharedMaterials2[0]);
					sharedMaterials2[0].SetFloat("_Hue", levelSetup.m_hue);
					sharedMaterials2[0].SetFloat("_Saturation", levelSetup.m_saturation);
					sharedMaterials2[0].SetFloat("_Value", levelSetup.m_value);
					this.m_mainRender.sharedMaterials = sharedMaterials2;
					LevelEffects.m_materials[key] = sharedMaterials2[0];
				}
			}
			if (this.m_baseEnableObject)
			{
				this.m_baseEnableObject.SetActive(false);
			}
			if (levelSetup.m_enableObject)
			{
				levelSetup.m_enableObject.SetActive(true);
			}
		}
	}

	public void GetColorChanges(out float hue, out float saturation, out float value)
	{
		int level = this.m_character.GetLevel();
		if (level > 1 && this.m_levelSetups.Count >= level - 1)
		{
			LevelEffects.LevelSetup levelSetup = this.m_levelSetups[level - 2];
			hue = levelSetup.m_hue;
			saturation = levelSetup.m_saturation;
			value = levelSetup.m_value;
			return;
		}
		hue = 0f;
		saturation = 0f;
		value = 0f;
	}

	public Renderer m_mainRender;

	public GameObject m_baseEnableObject;

	public List<LevelEffects.LevelSetup> m_levelSetups = new List<LevelEffects.LevelSetup>();

	private static Dictionary<string, Material> m_materials = new Dictionary<string, Material>();

	private Character m_character;

	[Serializable]
	public class LevelSetup
	{
		public float m_scale = 1f;

		public float m_hue;

		public float m_saturation;

		public float m_value;

		public GameObject m_enableObject;
	}
}
