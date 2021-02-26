using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeBase : MonoBehaviour, IDestructible
{
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_nview.Register<HitData>("Damage", new Action<long, HitData>(this.RPC_Damage));
		this.m_nview.Register("Grow", new Action<long>(this.RPC_Grow));
		this.m_nview.Register("Shake", new Action<long>(this.RPC_Shake));
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetFloat("health", this.m_health) <= 0f)
		{
			this.m_nview.Destroy();
		}
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Tree;
	}

	public void Damage(HitData hit)
	{
		this.m_nview.InvokeRPC("Damage", new object[]
		{
			hit
		});
	}

	public void Grow()
	{
		this.m_nview.InvokeRPC(ZNetView.Everybody, "Grow", Array.Empty<object>());
	}

	private void RPC_Grow(long uid)
	{
		base.StartCoroutine("GrowAnimation");
	}

	private IEnumerator GrowAnimation()
	{
		GameObject animatedTrunk = UnityEngine.Object.Instantiate<GameObject>(this.m_trunk, this.m_trunk.transform.position, this.m_trunk.transform.rotation, base.transform);
		animatedTrunk.isStatic = false;
		LODGroup component = base.transform.GetComponent<LODGroup>();
		if (component)
		{
			component.fadeMode = LODFadeMode.None;
		}
		this.m_trunk.SetActive(false);
		for (float t = 0f; t < 0.3f; t += Time.deltaTime)
		{
			float d = Mathf.Clamp01(t / 0.3f);
			animatedTrunk.transform.localScale = this.m_trunk.transform.localScale * d;
			yield return null;
		}
		UnityEngine.Object.Destroy(animatedTrunk);
		this.m_trunk.SetActive(true);
		if (this.m_nview.IsOwner())
		{
			this.m_respawnEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f);
		}
		yield break;
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		float num = this.m_nview.GetZDO().GetFloat("health", this.m_health);
		if (num <= 0f)
		{
			this.m_nview.Destroy();
			return;
		}
		HitData.DamageModifier type;
		hit.ApplyResistance(this.m_damageModifiers, out type);
		float totalDamage = hit.GetTotalDamage();
		if (hit.m_toolTier < this.m_minToolTier)
		{
			DamageText.instance.ShowText(DamageText.TextType.TooHard, hit.m_point, 0f, false);
			return;
		}
		DamageText.instance.ShowText(type, hit.m_point, totalDamage, false);
		if (totalDamage <= 0f)
		{
			return;
		}
		num -= totalDamage;
		this.m_nview.GetZDO().Set("health", num);
		this.Shake();
		this.m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform, 1f);
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
		if (closestPlayer)
		{
			closestPlayer.AddNoise(100f);
		}
		if (num <= 0f)
		{
			this.m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform, 1f);
			this.SpawnLog(hit.m_dir);
			List<GameObject> dropList = this.m_dropWhenDestroyed.GetDropList();
			for (int i = 0; i < dropList.Count; i++)
			{
				Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.5f;
				Vector3 position = base.transform.position + Vector3.up * this.m_spawnYOffset + new Vector3(vector.x, this.m_spawnYStep * (float)i, vector.y);
				Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
				UnityEngine.Object.Instantiate<GameObject>(dropList[i], position, rotation);
			}
			base.gameObject.SetActive(false);
			this.m_nview.Destroy();
		}
	}

	private void Shake()
	{
		this.m_nview.InvokeRPC(ZNetView.Everybody, "Shake", Array.Empty<object>());
	}

	private void RPC_Shake(long uid)
	{
		base.StopCoroutine("ShakeAnimation");
		base.StartCoroutine("ShakeAnimation");
	}

	private IEnumerator ShakeAnimation()
	{
		this.m_trunk.gameObject.isStatic = false;
		float t = Time.time;
		while (Time.time - t < 1f)
		{
			float time = Time.time;
			float num = 1f - Mathf.Clamp01((time - t) / 1f);
			float num2 = num * num * num * 1.5f;
			Quaternion localRotation = Quaternion.Euler(Mathf.Sin(time * 40f) * num2, 0f, Mathf.Cos(time * 0.9f * 40f) * num2);
			this.m_trunk.transform.localRotation = localRotation;
			yield return null;
		}
		this.m_trunk.transform.localRotation = Quaternion.identity;
		this.m_trunk.gameObject.isStatic = true;
		yield break;
	}

	private void SpawnLog(Vector3 hitDir)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_logPrefab, this.m_logSpawnPoint.position, this.m_logSpawnPoint.rotation);
		gameObject.GetComponent<ZNetView>().SetLocalScale(base.transform.localScale);
		Rigidbody component = gameObject.GetComponent<Rigidbody>();
		component.mass *= base.transform.localScale.x;
		component.ResetInertiaTensor();
		component.AddForceAtPosition(hitDir * 0.2f, gameObject.transform.position + Vector3.up * 4f * base.transform.localScale.y, ForceMode.VelocityChange);
		if (this.m_stubPrefab)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_stubPrefab, base.transform.position, base.transform.rotation).GetComponent<ZNetView>().SetLocalScale(base.transform.localScale);
		}
	}

	private ZNetView m_nview;

	public float m_health = 1f;

	public HitData.DamageModifiers m_damageModifiers;

	public int m_minToolTier;

	public EffectList m_destroyedEffect = new EffectList();

	public EffectList m_hitEffect = new EffectList();

	public EffectList m_respawnEffect = new EffectList();

	public GameObject m_trunk;

	public GameObject m_stubPrefab;

	public GameObject m_logPrefab;

	public Transform m_logSpawnPoint;

	[Header("Drops")]
	public DropTable m_dropWhenDestroyed = new DropTable();

	public float m_spawnYOffset = 0.5f;

	public float m_spawnYStep = 0.3f;
}
