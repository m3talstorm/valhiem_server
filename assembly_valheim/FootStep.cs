using System;
using System.Collections.Generic;
using UnityEngine;

public class FootStep : MonoBehaviour
{
	private void Start()
	{
		this.m_animator = base.GetComponentInChildren<Animator>();
		this.m_character = base.GetComponent<Character>();
		this.m_nview = base.GetComponent<ZNetView>();
		if (FootStep.m_footstepID == 0)
		{
			FootStep.m_footstepID = Animator.StringToHash("footstep");
			FootStep.m_forwardSpeedID = Animator.StringToHash("forward_speed");
			FootStep.m_sidewaySpeedID = Animator.StringToHash("sideway_speed");
		}
		this.m_footstep = this.m_animator.GetFloat(FootStep.m_footstepID);
		if (this.m_pieceLayer == 0)
		{
			this.m_pieceLayer = LayerMask.NameToLayer("piece");
		}
		Character character = this.m_character;
		character.m_onLand = (Action<Vector3>)Delegate.Combine(character.m_onLand, new Action<Vector3>(this.OnLand));
		if (this.m_nview.IsValid())
		{
			this.m_nview.Register<int, Vector3>("Step", new Action<long, int, Vector3>(this.RPC_Step));
		}
	}

	private void Update()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.UpdateFootstep(Time.deltaTime);
	}

	private void UpdateFootstep(float dt)
	{
		if (this.m_feet.Length == 0)
		{
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		if (Vector3.Distance(base.transform.position, mainCamera.transform.position) > this.m_footstepCullDistance)
		{
			return;
		}
		this.m_footstepTimer += dt;
		float @float = this.m_animator.GetFloat(FootStep.m_footstepID);
		if (Mathf.Sign(@float) != Mathf.Sign(this.m_footstep) && Mathf.Max(Mathf.Abs(this.m_animator.GetFloat(FootStep.m_forwardSpeedID)), Mathf.Abs(this.m_animator.GetFloat(FootStep.m_sidewaySpeedID))) > 0.2f && this.m_footstepTimer > 0.2f)
		{
			this.m_footstepTimer = 0f;
			this.OnFoot();
		}
		this.m_footstep = @float;
	}

	private Transform FindActiveFoot()
	{
		Transform transform = null;
		float num = 9999f;
		Vector3 forward = base.transform.forward;
		foreach (Transform transform2 in this.m_feet)
		{
			Vector3 rhs = transform2.position - base.transform.position;
			float num2 = Vector3.Dot(forward, rhs);
			if (num2 > num || transform == null)
			{
				transform = transform2;
				num = num2;
			}
		}
		return transform;
	}

	private Transform FindFoot(string name)
	{
		foreach (Transform transform in this.m_feet)
		{
			if (transform.gameObject.name == name)
			{
				return transform;
			}
		}
		return null;
	}

	public void OnFoot()
	{
		Transform transform = this.FindActiveFoot();
		if (transform == null)
		{
			return;
		}
		this.OnFoot(transform);
	}

	public void OnFoot(string name)
	{
		Transform transform = this.FindFoot(name);
		if (transform == null)
		{
			ZLog.LogWarning("FAiled to find foot:" + name);
			return;
		}
		this.OnFoot(transform);
	}

	private void OnLand(Vector3 point)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		FootStep.GroundMaterial groundMaterial = this.GetGroundMaterial(this.m_character, point);
		int num = this.FindBestStepEffect(groundMaterial, FootStep.MotionType.Land);
		if (num != -1)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "Step", new object[]
			{
				num,
				point
			});
		}
	}

	private void OnFoot(Transform foot)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		Vector3 vector = (foot != null) ? foot.position : base.transform.position;
		FootStep.MotionType motionType = this.GetMotionType(this.m_character);
		FootStep.GroundMaterial groundMaterial = this.GetGroundMaterial(this.m_character, vector);
		int num = this.FindBestStepEffect(groundMaterial, motionType);
		if (num != -1)
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "Step", new object[]
			{
				num,
				vector
			});
		}
	}

	private static void PurgeOldEffects()
	{
		while (FootStep.m_stepInstances.Count > 30)
		{
			GameObject gameObject = FootStep.m_stepInstances.Dequeue();
			if (gameObject)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
		}
	}

	private void DoEffect(FootStep.StepEffect effect, Vector3 point)
	{
		foreach (GameObject gameObject in effect.m_effectPrefabs)
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, point, base.transform.rotation);
			FootStep.m_stepInstances.Enqueue(gameObject2);
			if (gameObject2.GetComponent<ZNetView>() != null)
			{
				ZLog.LogWarning(string.Concat(new string[]
				{
					"Foot step effect ",
					effect.m_name,
					" prefab ",
					gameObject.name,
					" in ",
					this.m_character.gameObject.name,
					" should not contain a ZNetView component"
				}));
			}
		}
		FootStep.PurgeOldEffects();
	}

	private void RPC_Step(long sender, int effectIndex, Vector3 point)
	{
		FootStep.StepEffect effect = this.m_effects[effectIndex];
		this.DoEffect(effect, point);
	}

	private FootStep.MotionType GetMotionType(Character character)
	{
		if (this.m_character.IsSwiming())
		{
			return FootStep.MotionType.Swiming;
		}
		if (this.m_character.IsWallRunning())
		{
			return FootStep.MotionType.Climbing;
		}
		if (this.m_character.IsRunning())
		{
			return FootStep.MotionType.Run;
		}
		if (this.m_character.IsSneaking())
		{
			return FootStep.MotionType.Sneak;
		}
		return FootStep.MotionType.Walk;
	}

	private FootStep.GroundMaterial GetGroundMaterial(Character character, Vector3 point)
	{
		if (character.InWater())
		{
			return FootStep.GroundMaterial.Water;
		}
		if (!character.IsOnGround())
		{
			return FootStep.GroundMaterial.None;
		}
		float num = Mathf.Acos(Mathf.Clamp01(character.GetLastGroundNormal().y)) * 57.29578f;
		Collider lastGroundCollider = character.GetLastGroundCollider();
		if (lastGroundCollider)
		{
			Heightmap component = lastGroundCollider.GetComponent<Heightmap>();
			if (component != null)
			{
				Heightmap.Biome biome = component.GetBiome(point);
				if (biome == Heightmap.Biome.Mountain || biome == Heightmap.Biome.DeepNorth)
				{
					if (num < 40f && !component.IsCleared(point))
					{
						return FootStep.GroundMaterial.Snow;
					}
				}
				else if (biome == Heightmap.Biome.Swamp)
				{
					if (num < 40f)
					{
						return FootStep.GroundMaterial.Mud;
					}
				}
				else if ((biome == Heightmap.Biome.Meadows || biome == Heightmap.Biome.BlackForest) && num < 25f)
				{
					return FootStep.GroundMaterial.Grass;
				}
				return FootStep.GroundMaterial.GenericGround;
			}
			if (lastGroundCollider.gameObject.layer == this.m_pieceLayer)
			{
				WearNTear componentInParent = lastGroundCollider.GetComponentInParent<WearNTear>();
				if (componentInParent)
				{
					switch (componentInParent.m_materialType)
					{
					case WearNTear.MaterialType.Wood:
						return FootStep.GroundMaterial.Wood;
					case WearNTear.MaterialType.Stone:
						return FootStep.GroundMaterial.Stone;
					case WearNTear.MaterialType.Iron:
						return FootStep.GroundMaterial.Metal;
					case WearNTear.MaterialType.HardWood:
						return FootStep.GroundMaterial.Wood;
					}
				}
			}
		}
		return FootStep.GroundMaterial.Default;
	}

	public void FindJoints()
	{
		ZLog.Log("Finding joints");
		Transform transform = Utils.FindChild(base.transform, "LeftFootFront");
		Transform transform2 = Utils.FindChild(base.transform, "RightFootFront");
		Transform transform3 = Utils.FindChild(base.transform, "LeftFoot");
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "LeftFootBack");
		}
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "l_foot");
		}
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "Foot.l");
		}
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "foot.l");
		}
		Transform transform4 = Utils.FindChild(base.transform, "RightFoot");
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "RightFootBack");
		}
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "r_foot");
		}
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "Foot.r");
		}
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "foot.r");
		}
		List<Transform> list = new List<Transform>();
		if (transform)
		{
			list.Add(transform);
		}
		if (transform2)
		{
			list.Add(transform2);
		}
		if (transform3)
		{
			list.Add(transform3);
		}
		if (transform4)
		{
			list.Add(transform4);
		}
		this.m_feet = list.ToArray();
	}

	private int FindBestStepEffect(FootStep.GroundMaterial material, FootStep.MotionType motion)
	{
		FootStep.StepEffect stepEffect = null;
		int result = -1;
		for (int i = 0; i < this.m_effects.Count; i++)
		{
			FootStep.StepEffect stepEffect2 = this.m_effects[i];
			if (((stepEffect2.m_material & material) != FootStep.GroundMaterial.None || (stepEffect == null && (stepEffect2.m_material & FootStep.GroundMaterial.Default) != FootStep.GroundMaterial.None)) && (stepEffect2.m_motionType & motion) != (FootStep.MotionType)0)
			{
				stepEffect = stepEffect2;
				result = i;
			}
		}
		return result;
	}

	private static Queue<GameObject> m_stepInstances = new Queue<GameObject>();

	private const int m_maxFootstepInstances = 30;

	public float m_footstepCullDistance = 20f;

	public List<FootStep.StepEffect> m_effects = new List<FootStep.StepEffect>();

	public Transform[] m_feet = new Transform[0];

	private static int m_footstepID = 0;

	private static int m_forwardSpeedID = 0;

	private static int m_sidewaySpeedID = 0;

	private float m_footstep;

	private float m_footstepTimer;

	private const float m_minFootstepInterval = 0.2f;

	private int m_pieceLayer;

	private Animator m_animator;

	private Character m_character;

	private ZNetView m_nview;

	public enum MotionType
	{
		Walk = 1,
		Run,
		Sneak = 4,
		Climbing = 8,
		Swiming = 16,
		Land = 32
	}

	public enum GroundMaterial
	{
		None,
		Default,
		Water,
		Stone = 4,
		Wood = 8,
		Snow = 16,
		Mud = 32,
		Grass = 64,
		GenericGround = 128,
		Metal = 256
	}

	[Serializable]
	public class StepEffect
	{
		public string m_name = "";

		[BitMask(typeof(FootStep.MotionType))]
		public FootStep.MotionType m_motionType = FootStep.MotionType.Walk;

		[BitMask(typeof(FootStep.GroundMaterial))]
		public FootStep.GroundMaterial m_material = FootStep.GroundMaterial.Default;

		public GameObject[] m_effectPrefabs = new GameObject[0];
	}
}
