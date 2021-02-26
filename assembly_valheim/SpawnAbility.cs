using System;
using System.Collections;
using UnityEngine;

public class SpawnAbility : MonoBehaviour, IProjectile
{
	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item)
	{
		this.m_owner = owner;
		base.StartCoroutine("Spawn");
	}

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	private IEnumerator Spawn()
	{
		int toSpawn = UnityEngine.Random.Range(this.m_minToSpawn, this.m_maxToSpawn);
		int num;
		for (int i = 0; i < toSpawn; i = num)
		{
			Vector3 vector;
			if (this.FindTarget(out vector))
			{
				Vector3 vector2 = this.m_spawnAtTarget ? vector : base.transform.position;
				Vector2 vector3 = UnityEngine.Random.insideUnitCircle * this.m_spawnRadius;
				Vector3 vector4 = vector2 + new Vector3(vector3.x, 0f, vector3.y);
				if (this.m_snapToTerrain)
				{
					float solidHeight = ZoneSystem.instance.GetSolidHeight(vector4);
					vector4.y = solidHeight;
				}
				vector4.y += this.m_spawnGroundOffset;
				if (Mathf.Abs(vector4.y - vector2.y) <= 100f)
				{
					GameObject gameObject = this.m_spawnPrefab[UnityEngine.Random.Range(0, this.m_spawnPrefab.Length)];
					if (this.m_maxSpawned <= 0 || SpawnSystem.GetNrOfInstances(gameObject) < this.m_maxSpawned)
					{
						GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, vector4, Quaternion.Euler(0f, UnityEngine.Random.value * 3.1415927f * 2f, 0f));
						Projectile component = gameObject2.GetComponent<Projectile>();
						if (component)
						{
							this.SetupProjectile(component, vector);
						}
						BaseAI component2 = gameObject2.GetComponent<BaseAI>();
						if (component2 != null && this.m_alertSpawnedCreature)
						{
							component2.Alert();
						}
						this.m_spawnEffects.Create(vector4, Quaternion.identity, null, 1f);
						if (this.m_spawnDelay > 0f)
						{
							yield return new WaitForSeconds(this.m_spawnDelay);
						}
					}
				}
			}
			num = i + 1;
		}
		UnityEngine.Object.Destroy(base.gameObject);
		yield break;
	}

	private void SetupProjectile(Projectile projectile, Vector3 targetPoint)
	{
		Vector3 vector = (targetPoint - projectile.transform.position).normalized;
		Vector3 axis = Vector3.Cross(vector, Vector3.up);
		Quaternion rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(-this.m_projectileAccuracy, this.m_projectileAccuracy), Vector3.up);
		vector = Quaternion.AngleAxis(UnityEngine.Random.Range(-this.m_projectileAccuracy, this.m_projectileAccuracy), axis) * vector;
		vector = rotation * vector;
		projectile.Setup(this.m_owner, vector * this.m_projectileVelocity, -1f, null, null);
	}

	private bool FindTarget(out Vector3 point)
	{
		point = Vector3.zero;
		switch (this.m_targetType)
		{
		case SpawnAbility.TargetType.ClosestEnemy:
		{
			if (this.m_owner == null)
			{
				return false;
			}
			Character character = BaseAI.FindClosestEnemy(this.m_owner, base.transform.position, this.m_maxTargetRange);
			if (character != null)
			{
				point = character.transform.position;
				return true;
			}
			return false;
		}
		case SpawnAbility.TargetType.RandomEnemy:
		{
			if (this.m_owner == null)
			{
				return false;
			}
			Character character2 = BaseAI.FindRandomEnemy(this.m_owner, base.transform.position, this.m_maxTargetRange);
			if (character2 != null)
			{
				point = character2.transform.position;
				return true;
			}
			return false;
		}
		case SpawnAbility.TargetType.Caster:
			if (this.m_owner == null)
			{
				return false;
			}
			point = this.m_owner.transform.position;
			return true;
		case SpawnAbility.TargetType.Position:
			point = base.transform.position;
			return true;
		default:
			return false;
		}
	}

	[Header("Spawn")]
	public GameObject[] m_spawnPrefab;

	public bool m_alertSpawnedCreature = true;

	public bool m_spawnAtTarget = true;

	public int m_minToSpawn = 1;

	public int m_maxToSpawn = 1;

	public int m_maxSpawned = 3;

	public float m_spawnRadius = 3f;

	public bool m_snapToTerrain = true;

	public float m_spawnGroundOffset;

	public float m_spawnDelay;

	public SpawnAbility.TargetType m_targetType;

	public float m_maxTargetRange = 40f;

	public EffectList m_spawnEffects = new EffectList();

	[Header("Projectile")]
	public float m_projectileVelocity = 10f;

	public float m_projectileAccuracy = 10f;

	private Character m_owner;

	public enum TargetType
	{
		ClosestEnemy,
		RandomEnemy,
		Caster,
		Position
	}
}
