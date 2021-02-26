using System;
using UnityEngine;

public class ItemStyle : MonoBehaviour, IEquipmentVisual
{
	public void Setup(int style)
	{
		base.GetComponent<Renderer>().material.SetFloat("_Style", (float)style);
	}
}
