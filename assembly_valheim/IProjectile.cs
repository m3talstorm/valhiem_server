using System;
using UnityEngine;

public interface IProjectile
{
	void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item);

	string GetTooltipString(int itemQuality);
}
