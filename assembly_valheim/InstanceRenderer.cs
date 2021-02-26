using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class InstanceRenderer : MonoBehaviour
{
	private void Update()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (this.m_instanceCount == 0 || mainCamera == null)
		{
			return;
		}
		if (this.m_frustumCull)
		{
			if (this.m_dirtyBounds)
			{
				this.UpdateBounds();
			}
			if (!Utils.InsideMainCamera(this.m_bounds))
			{
				return;
			}
		}
		if (this.m_useLod)
		{
			float num = this.m_useXZLodDistance ? Utils.DistanceXZ(mainCamera.transform.position, base.transform.position) : Vector3.Distance(mainCamera.transform.position, base.transform.position);
			int num2 = (int)((1f - Utils.LerpStep(this.m_lodMinDistance, this.m_lodMaxDistance, num)) * (float)this.m_instanceCount);
			float maxDelta = Time.deltaTime * (float)this.m_instanceCount;
			this.m_lodCount = Mathf.MoveTowards(this.m_lodCount, (float)num2, maxDelta);
			if (this.m_firstFrame)
			{
				if (num < this.m_lodMinDistance)
				{
					this.m_lodCount = (float)num2;
				}
				this.m_firstFrame = false;
			}
			this.m_lodCount = Mathf.Min(this.m_lodCount, (float)this.m_instanceCount);
			int num3 = (int)this.m_lodCount;
			if (num3 > 0)
			{
				Graphics.DrawMeshInstanced(this.m_mesh, 0, this.m_material, this.m_instances, num3, null, this.m_shadowCasting);
				return;
			}
		}
		else
		{
			Graphics.DrawMeshInstanced(this.m_mesh, 0, this.m_material, this.m_instances, this.m_instanceCount, null, this.m_shadowCasting);
		}
	}

	private void UpdateBounds()
	{
		this.m_dirtyBounds = false;
		Vector3 vector = new Vector3(9999999f, 9999999f, 9999999f);
		Vector3 vector2 = new Vector3(-9999999f, -9999999f, -9999999f);
		float magnitude = this.m_mesh.bounds.extents.magnitude;
		for (int i = 0; i < this.m_instanceCount; i++)
		{
			Matrix4x4 matrix4x = this.m_instances[i];
			Vector3 a = new Vector3(matrix4x[0, 3], matrix4x[1, 3], matrix4x[2, 3]);
			Vector3 lossyScale = matrix4x.lossyScale;
			float num = Mathf.Max(Mathf.Max(lossyScale.x, lossyScale.y), lossyScale.z);
			Vector3 b = new Vector3(num * magnitude, num * magnitude, num * magnitude);
			vector2 = Vector3.Max(vector2, a + b);
			vector = Vector3.Min(vector, a - b);
		}
		this.m_bounds.position = (vector2 + vector) * 0.5f;
		this.m_bounds.radius = Vector3.Distance(vector2, this.m_bounds.position);
	}

	public void AddInstance(Vector3 pos, Quaternion rot, float scale)
	{
		Matrix4x4 m = Matrix4x4.TRS(pos, rot, this.m_scale * scale);
		this.AddInstance(m);
	}

	public void AddInstance(Vector3 pos, Quaternion rot)
	{
		Matrix4x4 m = Matrix4x4.TRS(pos, rot, this.m_scale);
		this.AddInstance(m);
	}

	public void AddInstance(Matrix4x4 m)
	{
		if (this.m_instanceCount >= 1023)
		{
			return;
		}
		this.m_instances[this.m_instanceCount] = m;
		this.m_instanceCount++;
		this.m_dirtyBounds = true;
	}

	public void Clear()
	{
		this.m_instanceCount = 0;
		this.m_dirtyBounds = true;
	}

	public void SetInstance(int index, Vector3 pos, Quaternion rot, float scale)
	{
		Matrix4x4 matrix4x = Matrix4x4.TRS(pos, rot, this.m_scale * scale);
		this.m_instances[index] = matrix4x;
		this.m_dirtyBounds = true;
	}

	private void Resize(int instances)
	{
		this.m_instanceCount = instances;
		this.m_dirtyBounds = true;
	}

	public void SetInstances(List<Transform> transforms, bool faceCamera = false)
	{
		this.Resize(transforms.Count);
		for (int i = 0; i < transforms.Count; i++)
		{
			Transform transform = transforms[i];
			this.m_instances[i] = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
		}
		this.m_dirtyBounds = true;
	}

	public void SetInstancesBillboard(List<Vector4> points)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 forward = -mainCamera.transform.forward;
		this.Resize(points.Count);
		for (int i = 0; i < points.Count; i++)
		{
			Vector4 vector = points[i];
			Vector3 pos = new Vector3(vector.x, vector.y, vector.z);
			float w = vector.w;
			Quaternion q = Quaternion.LookRotation(forward);
			this.m_instances[i] = Matrix4x4.TRS(pos, q, w * this.m_scale);
		}
		this.m_dirtyBounds = true;
	}

	private void OnDrawGizmosSelected()
	{
	}

	public Mesh m_mesh;

	public Material m_material;

	public Vector3 m_scale = Vector3.one;

	public bool m_frustumCull = true;

	private bool m_dirtyBounds = true;

	private BoundingSphere m_bounds;

	public bool m_useLod;

	public bool m_useXZLodDistance = true;

	public float m_lodMinDistance = 5f;

	public float m_lodMaxDistance = 20f;

	public ShadowCastingMode m_shadowCasting;

	private float m_lodCount;

	private Matrix4x4[] m_instances = new Matrix4x4[1024];

	private int m_instanceCount;

	private bool m_firstFrame = true;
}
