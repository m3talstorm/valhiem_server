using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Character))]
public class CharacterDrop : MonoBehaviour
{
	private void Start()
	{
		this.m_character = base.GetComponent<Character>();
		if (this.m_character)
		{
			Character character = this.m_character;
			character.m_onDeath = (Action)Delegate.Combine(character.m_onDeath, new Action(this.OnDeath));
		}
	}

	public void SetDropsEnabled(bool enabled)
	{
		this.m_dropsEnabled = enabled;
	}

	private void OnDeath()
	{
		if (!this.m_dropsEnabled)
		{
			return;
		}
		List<KeyValuePair<GameObject, int>> drops = this.GenerateDropList();
		Vector3 centerPos = this.m_character.GetCenterPoint() + base.transform.TransformVector(this.m_spawnOffset);
		CharacterDrop.DropItems(drops, centerPos, 0.5f);
	}

	public List<KeyValuePair<GameObject, int>> GenerateDropList()
	{
		List<KeyValuePair<GameObject, int>> list = new List<KeyValuePair<GameObject, int>>();
		int num = this.m_character ? Mathf.Max(1, (int)Mathf.Pow(2f, (float)(this.m_character.GetLevel() - 1))) : 1;
		foreach (CharacterDrop.Drop drop in this.m_drops)
		{
			if (!(drop.m_prefab == null))
			{
				float num2 = drop.m_chance;
				if (drop.m_levelMultiplier)
				{
					num2 *= (float)num;
				}
				if (UnityEngine.Random.value <= num2)
				{
					int num3 = UnityEngine.Random.Range(drop.m_amountMin, drop.m_amountMax);
					if (drop.m_levelMultiplier)
					{
						num3 *= num;
					}
					if (drop.m_onePerPlayer)
					{
						num3 = ZNet.instance.GetNrOfPlayers();
					}
					if (num3 > 0)
					{
						list.Add(new KeyValuePair<GameObject, int>(drop.m_prefab, num3));
					}
				}
			}
		}
		return list;
	}

	public static void DropItems(List<KeyValuePair<GameObject, int>> drops, Vector3 centerPos, float dropArea)
	{
		foreach (KeyValuePair<GameObject, int> keyValuePair in drops)
		{
			for (int i = 0; i < keyValuePair.Value; i++)
			{
				Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
				Vector3 b = UnityEngine.Random.insideUnitSphere * dropArea;
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(keyValuePair.Key, centerPos + b, rotation);
				Rigidbody component = gameObject.GetComponent<Rigidbody>();
				if (component)
				{
					Vector3 insideUnitSphere = UnityEngine.Random.insideUnitSphere;
					if (insideUnitSphere.y < 0f)
					{
						insideUnitSphere.y = -insideUnitSphere.y;
					}
					component.AddForce(insideUnitSphere * 5f, ForceMode.VelocityChange);
				}
			}
		}
	}

	public Vector3 m_spawnOffset = Vector3.zero;

	public List<CharacterDrop.Drop> m_drops = new List<CharacterDrop.Drop>();

	private const float m_dropArea = 0.5f;

	private const float m_vel = 5f;

	private bool m_dropsEnabled = true;

	private Character m_character;

	[Serializable]
	public class Drop
	{
		public GameObject m_prefab;

		public int m_amountMin = 1;

		public int m_amountMax = 1;

		public float m_chance = 1f;

		public bool m_onePerPlayer;

		public bool m_levelMultiplier = true;
	}
}
