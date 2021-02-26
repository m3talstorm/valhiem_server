using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ZNetScene : MonoBehaviour
{
	public static ZNetScene instance
	{
		get
		{
			return ZNetScene.m_instance;
		}
	}

	private void Awake()
	{
		ZNetScene.m_instance = this;
		foreach (GameObject gameObject in this.m_prefabs)
		{
			this.m_namedPrefabs.Add(gameObject.name.GetStableHashCode(), gameObject);
		}
		foreach (GameObject gameObject2 in this.m_nonNetViewPrefabs)
		{
			this.m_namedPrefabs.Add(gameObject2.name.GetStableHashCode(), gameObject2);
		}
		ZDOMan instance = ZDOMan.instance;
		instance.m_onZDODestroyed = (Action<ZDO>)Delegate.Combine(instance.m_onZDODestroyed, new Action<ZDO>(this.OnZDODestroyed));
		this.m_netSceneRoot = new GameObject("_NetSceneRoot");
		ZRoutedRpc.instance.Register<Vector3, Quaternion, int>("SpawnObject", new Action<long, Vector3, Quaternion, int>(this.RPC_SpawnObject));
	}

	private void OnDestroy()
	{
		ZLog.Log("Net scene destroyed");
		if (ZNetScene.m_instance == this)
		{
			ZNetScene.m_instance = null;
		}
	}

	public void Shutdown()
	{
		foreach (KeyValuePair<ZDO, ZNetView> keyValuePair in this.m_instances)
		{
			if (keyValuePair.Value)
			{
				keyValuePair.Value.ResetZDO();
				UnityEngine.Object.Destroy(keyValuePair.Value.gameObject);
			}
		}
		this.m_instances.Clear();
		base.enabled = false;
	}

	public void AddInstance(ZDO zdo, ZNetView nview)
	{
		this.m_instances[zdo] = nview;
		if (nview.transform.parent == null)
		{
			nview.transform.SetParent(this.m_netSceneRoot.transform);
		}
	}

	private bool IsPrefabZDOValid(ZDO zdo)
	{
		int prefab = zdo.GetPrefab();
		return prefab != 0 && !(this.GetPrefab(prefab) == null);
	}

	private GameObject CreateObject(ZDO zdo)
	{
		int prefab = zdo.GetPrefab();
		if (prefab == 0)
		{
			return null;
		}
		GameObject prefab2 = this.GetPrefab(prefab);
		if (prefab2 == null)
		{
			return null;
		}
		Vector3 position = zdo.GetPosition();
		Quaternion rotation = zdo.GetRotation();
		ZNetView.m_useInitZDO = true;
		ZNetView.m_initZDO = zdo;
		GameObject result = UnityEngine.Object.Instantiate<GameObject>(prefab2, position, rotation);
		if (ZNetView.m_initZDO != null)
		{
			ZLog.LogWarning(string.Concat(new object[]
			{
				"ZDO ",
				zdo.m_uid,
				" not used when creating object ",
				prefab2.name
			}));
			ZNetView.m_initZDO = null;
		}
		ZNetView.m_useInitZDO = false;
		return result;
	}

	public void Destroy(GameObject go)
	{
		ZNetView component = go.GetComponent<ZNetView>();
		if (component && component.GetZDO() != null)
		{
			ZDO zdo = component.GetZDO();
			component.ResetZDO();
			this.m_instances.Remove(zdo);
			if (zdo.IsOwner())
			{
				ZDOMan.instance.DestroyZDO(zdo);
			}
		}
		UnityEngine.Object.Destroy(go);
	}

	public GameObject GetPrefab(int hash)
	{
		GameObject result;
		if (this.m_namedPrefabs.TryGetValue(hash, out result))
		{
			return result;
		}
		return null;
	}

	public GameObject GetPrefab(string name)
	{
		int stableHashCode = name.GetStableHashCode();
		return this.GetPrefab(stableHashCode);
	}

	public int GetPrefabHash(GameObject go)
	{
		return go.name.GetStableHashCode();
	}

	public bool IsAreaReady(Vector3 point)
	{
		Vector2i zone = ZoneSystem.instance.GetZone(point);
		if (!ZoneSystem.instance.IsZoneLoaded(zone))
		{
			return false;
		}
		this.m_tempCurrentObjects.Clear();
		ZDOMan.instance.FindSectorObjects(zone, 1, 0, this.m_tempCurrentObjects, null);
		foreach (ZDO zdo in this.m_tempCurrentObjects)
		{
			if (this.IsPrefabZDOValid(zdo) && !this.FindInstance(zdo))
			{
				return false;
			}
		}
		return true;
	}

	private bool InLoadingScreen()
	{
		return Player.m_localPlayer == null || Player.m_localPlayer.IsTeleporting();
	}

	private void CreateObjects(List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
	{
		int maxCreatedPerFrame = 10;
		if (this.InLoadingScreen())
		{
			maxCreatedPerFrame = 100;
		}
		int frameCount = Time.frameCount;
		foreach (ZDO zdo in this.m_instances.Keys)
		{
			zdo.m_tempCreateEarmark = frameCount;
		}
		int num = 0;
		this.CreateObjectsSorted(currentNearObjects, maxCreatedPerFrame, ref num);
		this.CreateDistantObjects(currentDistantObjects, maxCreatedPerFrame, ref num);
	}

	private void CreateObjectsSorted(List<ZDO> currentNearObjects, int maxCreatedPerFrame, ref int created)
	{
		this.m_tempCurrentObjects2.Clear();
		int frameCount = Time.frameCount;
		foreach (ZDO zdo in currentNearObjects)
		{
			if (zdo.m_tempCreateEarmark != frameCount && (zdo.m_distant || ZoneSystem.instance.IsZoneLoaded(zdo.GetSector())))
			{
				this.m_tempCurrentObjects2.Add(zdo);
			}
		}
		foreach (ZDO zdo2 in from item in this.m_tempCurrentObjects2
		orderby item.m_type descending
		select item)
		{
			if (this.CreateObject(zdo2) != null)
			{
				created++;
				if (created > maxCreatedPerFrame)
				{
					break;
				}
			}
			else if (ZNet.instance.IsServer())
			{
				zdo2.SetOwner(ZDOMan.instance.GetMyID());
				ZLog.Log("Destroyed invalid predab ZDO:" + zdo2.m_uid);
				ZDOMan.instance.DestroyZDO(zdo2);
			}
		}
	}

	private void CreateDistantObjects(List<ZDO> objects, int maxCreatedPerFrame, ref int created)
	{
		if (created > maxCreatedPerFrame)
		{
			return;
		}
		int frameCount = Time.frameCount;
		foreach (ZDO zdo in objects)
		{
			if (zdo.m_tempCreateEarmark != frameCount)
			{
				if (this.CreateObject(zdo) != null)
				{
					created++;
					if (created > maxCreatedPerFrame)
					{
						break;
					}
				}
				else if (ZNet.instance.IsServer())
				{
					zdo.SetOwner(ZDOMan.instance.GetMyID());
					ZLog.Log(string.Concat(new object[]
					{
						"Destroyed invalid predab ZDO:",
						zdo.m_uid,
						"  prefab hash:",
						zdo.GetPrefab()
					}));
					ZDOMan.instance.DestroyZDO(zdo);
				}
			}
		}
	}

	private void OnZDODestroyed(ZDO zdo)
	{
		ZNetView znetView;
		if (this.m_instances.TryGetValue(zdo, out znetView))
		{
			znetView.ResetZDO();
			UnityEngine.Object.Destroy(znetView.gameObject);
			this.m_instances.Remove(zdo);
		}
	}

	private void RemoveObjects(List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
	{
		int frameCount = Time.frameCount;
		foreach (ZDO zdo in currentNearObjects)
		{
			zdo.m_tempRemoveEarmark = frameCount;
		}
		foreach (ZDO zdo2 in currentDistantObjects)
		{
			zdo2.m_tempRemoveEarmark = frameCount;
		}
		this.m_tempRemoved.Clear();
		foreach (ZNetView znetView in this.m_instances.Values)
		{
			if (znetView.GetZDO().m_tempRemoveEarmark != frameCount)
			{
				this.m_tempRemoved.Add(znetView);
			}
		}
		for (int i = 0; i < this.m_tempRemoved.Count; i++)
		{
			ZNetView znetView2 = this.m_tempRemoved[i];
			ZDO zdo3 = znetView2.GetZDO();
			znetView2.ResetZDO();
			UnityEngine.Object.Destroy(znetView2.gameObject);
			if (!zdo3.m_persistent && zdo3.IsOwner())
			{
				ZDOMan.instance.DestroyZDO(zdo3);
			}
			this.m_instances.Remove(zdo3);
		}
	}

	public ZNetView FindInstance(ZDO zdo)
	{
		ZNetView result;
		if (this.m_instances.TryGetValue(zdo, out result))
		{
			return result;
		}
		return null;
	}

	public bool HaveInstance(ZDO zdo)
	{
		return this.m_instances.ContainsKey(zdo);
	}

	public GameObject FindInstance(ZDOID id)
	{
		ZDO zdo = ZDOMan.instance.GetZDO(id);
		if (zdo != null)
		{
			ZNetView znetView = this.FindInstance(zdo);
			if (znetView)
			{
				return znetView.gameObject;
			}
		}
		return null;
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		this.m_createDestroyTimer += deltaTime;
		if (this.m_createDestroyTimer >= 0.033333335f)
		{
			this.m_createDestroyTimer = 0f;
			this.CreateDestroyObjects();
		}
	}

	private void CreateDestroyObjects()
	{
		Vector2i zone = ZoneSystem.instance.GetZone(ZNet.instance.GetReferencePosition());
		this.m_tempCurrentObjects.Clear();
		this.m_tempCurrentDistantObjects.Clear();
		ZDOMan.instance.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, ZoneSystem.instance.m_activeDistantArea, this.m_tempCurrentObjects, this.m_tempCurrentDistantObjects);
		this.CreateObjects(this.m_tempCurrentObjects, this.m_tempCurrentDistantObjects);
		this.RemoveObjects(this.m_tempCurrentObjects, this.m_tempCurrentDistantObjects);
	}

	public bool InActiveArea(Vector2i zone, Vector3 refPoint)
	{
		Vector2i zone2 = ZoneSystem.instance.GetZone(refPoint);
		return this.InActiveArea(zone, zone2);
	}

	public bool InActiveArea(Vector2i zone, Vector2i refCenterZone)
	{
		int num = ZoneSystem.instance.m_activeArea - 1;
		return zone.x >= refCenterZone.x - num && zone.x <= refCenterZone.x + num && zone.y <= refCenterZone.y + num && zone.y >= refCenterZone.y - num;
	}

	public bool OutsideActiveArea(Vector3 point)
	{
		return this.OutsideActiveArea(point, ZNet.instance.GetReferencePosition());
	}

	public bool OutsideActiveArea(Vector3 point, Vector3 refPoint)
	{
		Vector2i zone = ZoneSystem.instance.GetZone(refPoint);
		Vector2i zone2 = ZoneSystem.instance.GetZone(point);
		return zone2.x <= zone.x - ZoneSystem.instance.m_activeArea || zone2.x >= zone.x + ZoneSystem.instance.m_activeArea || zone2.y >= zone.y + ZoneSystem.instance.m_activeArea || zone2.y <= zone.y - ZoneSystem.instance.m_activeArea;
	}

	public bool HaveInstanceInSector(Vector2i sector)
	{
		foreach (KeyValuePair<ZDO, ZNetView> keyValuePair in this.m_instances)
		{
			if (keyValuePair.Value && !keyValuePair.Value.m_distant && ZoneSystem.instance.GetZone(keyValuePair.Value.transform.position) == sector)
			{
				return true;
			}
		}
		return false;
	}

	public int NrOfInstances()
	{
		return this.m_instances.Count;
	}

	public void SpawnObject(Vector3 pos, Quaternion rot, GameObject prefab)
	{
		int prefabHash = this.GetPrefabHash(prefab);
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SpawnObject", new object[]
		{
			pos,
			rot,
			prefabHash
		});
	}

	private void RPC_SpawnObject(long spawner, Vector3 pos, Quaternion rot, int prefabHash)
	{
		GameObject prefab = this.GetPrefab(prefabHash);
		if (prefab == null)
		{
			ZLog.Log("Missing prefab " + prefabHash);
			return;
		}
		UnityEngine.Object.Instantiate<GameObject>(prefab, pos, rot);
	}

	private static ZNetScene m_instance;

	private const int m_maxCreatedPerFrame = 10;

	private const int m_maxDestroyedPerFrame = 20;

	private const float m_createDestroyFps = 30f;

	public List<GameObject> m_prefabs = new List<GameObject>();

	public List<GameObject> m_nonNetViewPrefabs = new List<GameObject>();

	private Dictionary<int, GameObject> m_namedPrefabs = new Dictionary<int, GameObject>();

	private Dictionary<ZDO, ZNetView> m_instances = new Dictionary<ZDO, ZNetView>(new ZDOComparer());

	private GameObject m_netSceneRoot;

	private List<ZDO> m_tempCurrentObjects = new List<ZDO>();

	private List<ZDO> m_tempCurrentObjects2 = new List<ZDO>();

	private List<ZDO> m_tempCurrentDistantObjects = new List<ZDO>();

	private List<ZNetView> m_tempRemoved = new List<ZNetView>();

	private HashSet<ZDO> m_tempActiveZDOs = new HashSet<ZDO>(new ZDOComparer());

	private float m_createDestroyTimer;
}
