using System;
using UnityEngine;

public interface IWaterInteractable
{
	bool IsOwner();

	void SetInWater(float waterLevel);

	Transform GetTransform();
}
