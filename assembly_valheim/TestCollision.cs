using System;
using UnityEngine;

public class TestCollision : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
	}

	public void OnCollisionEnter(Collision info)
	{
		ZLog.Log("Hit by " + info.rigidbody.gameObject.name);
		ZLog.Log(string.Concat(new object[]
		{
			"rel vel ",
			info.relativeVelocity,
			" ",
			info.relativeVelocity
		}));
		ZLog.Log(string.Concat(new object[]
		{
			"Vel ",
			info.rigidbody.velocity,
			"  ",
			info.rigidbody.angularVelocity
		}));
	}
}
