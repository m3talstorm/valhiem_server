using System;
using UnityEngine;

public class TestSceneSetup : MonoBehaviour
{
	private void Awake()
	{
		WorldGenerator.Initialize(World.GetMenuWorld());
	}

	private void Update()
	{
	}
}
