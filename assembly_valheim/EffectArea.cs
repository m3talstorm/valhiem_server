using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectArea : MonoBehaviour
{
	private void Awake()
	{
		if (EffectArea.m_characterMask == 0)
		{
			EffectArea.m_characterMask = LayerMask.GetMask(new string[]
			{
				"character_trigger"
			});
		}
		this.m_collider = base.GetComponent<Collider>();
		EffectArea.m_allAreas.Add(this);
	}

	private void OnDestroy()
	{
		EffectArea.m_allAreas.Remove(this);
	}

	private void OnTriggerStay(Collider collider)
	{
		if (ZNet.instance == null)
		{
			return;
		}
		Character component = collider.GetComponent<Character>();
		if (component && component.IsOwner())
		{
			if (!string.IsNullOrEmpty(this.m_statusEffect))
			{
				component.GetSEMan().AddStatusEffect(this.m_statusEffect, true);
			}
			if ((this.m_type & EffectArea.Type.Heat) != (EffectArea.Type)0)
			{
				component.OnNearFire(base.transform.position);
			}
		}
	}

	public float GetRadius()
	{
		SphereCollider sphereCollider = this.m_collider as SphereCollider;
		if (sphereCollider != null)
		{
			return sphereCollider.radius;
		}
		return this.m_collider.bounds.size.magnitude;
	}

	public static EffectArea IsPointInsideArea(Vector3 p, EffectArea.Type type, float radius = 0f)
	{
		int num = Physics.OverlapSphereNonAlloc(p, radius, EffectArea.m_tempColliders, EffectArea.m_characterMask);
		for (int i = 0; i < num; i++)
		{
			EffectArea component = EffectArea.m_tempColliders[i].GetComponent<EffectArea>();
			if (component && (component.m_type & type) != (EffectArea.Type)0)
			{
				return component;
			}
		}
		return null;
	}

	public static int GetBaseValue(Vector3 p, float radius)
	{
		int num = 0;
		int num2 = Physics.OverlapSphereNonAlloc(p, radius, EffectArea.m_tempColliders, EffectArea.m_characterMask);
		for (int i = 0; i < num2; i++)
		{
			EffectArea component = EffectArea.m_tempColliders[i].GetComponent<EffectArea>();
			if (component && (component.m_type & EffectArea.Type.PlayerBase) != (EffectArea.Type)0)
			{
				num++;
			}
		}
		return num;
	}

	public static List<EffectArea> GetAllAreas()
	{
		return EffectArea.m_allAreas;
	}

	[BitMask(typeof(EffectArea.Type))]
	public EffectArea.Type m_type = EffectArea.Type.None;

	public string m_statusEffect = "";

	private Collider m_collider;

	private static int m_characterMask = 0;

	private static List<EffectArea> m_allAreas = new List<EffectArea>();

	private static Collider[] m_tempColliders = new Collider[128];

	public enum Type
	{
		Heat = 1,
		Fire,
		PlayerBase = 4,
		Burning = 8,
		Teleport = 16,
		NoMonsters = 32,
		None = 999
	}
}
