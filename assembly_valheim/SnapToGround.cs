using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SnapToGround : MonoBehaviour
{
	private void Awake()
	{
		SnapToGround.m_allSnappers.Add(this);
		this.m_inList = true;
	}

	private void OnDestroy()
	{
		if (this.m_inList)
		{
			SnapToGround.m_allSnappers.Remove(this);
			this.m_inList = false;
		}
	}

	private void Snap()
	{
		if (ZoneSystem.instance == null)
		{
			return;
		}
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		Vector3 position = base.transform.position;
		position.y = groundHeight + this.m_offset;
		base.transform.position = position;
		ZNetView component = base.GetComponent<ZNetView>();
		if (component != null && component.IsOwner())
		{
			component.GetZDO().SetPosition(position);
		}
	}

	public bool HaveUnsnapped()
	{
		return SnapToGround.m_allSnappers.Count > 0;
	}

	public static void SnappAll()
	{
		if (SnapToGround.m_allSnappers.Count == 0)
		{
			return;
		}
		Heightmap.ForceGenerateAll();
		foreach (SnapToGround snapToGround in SnapToGround.m_allSnappers)
		{
			snapToGround.Snap();
			snapToGround.m_inList = false;
		}
		SnapToGround.m_allSnappers.Clear();
	}

	public float m_offset;

	private static List<SnapToGround> m_allSnappers = new List<SnapToGround>();

	private bool m_inList;
}
