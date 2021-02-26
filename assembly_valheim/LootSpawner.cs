using System;
using System.Collections.Generic;
using UnityEngine;

public class LootSpawner : MonoBehaviour
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		base.InvokeRepeating("UpdateSpawner", 10f, 2f);
	}

	private void UpdateSpawner()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_spawnAtDay && EnvMan.instance.IsDay())
		{
			return;
		}
		if (!this.m_spawnAtNight && EnvMan.instance.IsNight())
		{
			return;
		}
		if (this.m_spawnWhenEnemiesCleared)
		{
			bool flag = LootSpawner.IsMonsterInRange(base.transform.position, this.m_enemiesCheckRange);
			if (flag && !this.m_seenEnemies)
			{
				this.m_seenEnemies = true;
			}
			if (flag || !this.m_seenEnemies)
			{
				return;
			}
		}
		long @long = this.m_nview.GetZDO().GetLong("spawn_time", 0L);
		DateTime time = ZNet.instance.GetTime();
		DateTime d = new DateTime(@long);
		TimeSpan timeSpan = time - d;
		if (this.m_respawnTimeMinuts <= 0f && @long != 0L)
		{
			return;
		}
		if (timeSpan.TotalMinutes < (double)this.m_respawnTimeMinuts)
		{
			return;
		}
		if (!Player.IsPlayerInRange(base.transform.position, 20f))
		{
			return;
		}
		List<GameObject> dropList = this.m_items.GetDropList();
		for (int i = 0; i < dropList.Count; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.3f;
			Vector3 position = base.transform.position + new Vector3(vector.x, 0.3f * (float)i, vector.y);
			Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
			UnityEngine.Object.Instantiate<GameObject>(dropList[i], position, rotation);
		}
		this.m_spawnEffect.Create(base.transform.position, Quaternion.identity, null, 1f);
		this.m_nview.GetZDO().Set("spawn_time", ZNet.instance.GetTime().Ticks);
		this.m_seenEnemies = false;
	}

	public static bool IsMonsterInRange(Vector3 point, float range)
	{
		foreach (Character character in Character.GetAllCharacters())
		{
			if (character.IsMonsterFaction() && Vector3.Distance(character.transform.position, point) < range)
			{
				return true;
			}
		}
		return false;
	}

	private void OnDrawGizmos()
	{
	}

	public DropTable m_items = new DropTable();

	public EffectList m_spawnEffect = new EffectList();

	public float m_respawnTimeMinuts = 10f;

	private const float m_triggerDistance = 20f;

	public bool m_spawnAtNight = true;

	public bool m_spawnAtDay = true;

	public bool m_spawnWhenEnemiesCleared;

	public float m_enemiesCheckRange = 30f;

	private ZNetView m_nview;

	private bool m_seenEnemies;
}
