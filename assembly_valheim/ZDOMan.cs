using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

public class ZDOMan
{
	public static ZDOMan instance
	{
		get
		{
			return ZDOMan.m_instance;
		}
	}

	public ZDOMan(int width)
	{
		ZDOMan.m_instance = this;
		this.m_myid = Utils.GenerateUID();
		ZRoutedRpc.instance.Register<ZPackage>("DestroyZDO", new Action<long, ZPackage>(this.RPC_DestroyZDO));
		ZRoutedRpc.instance.Register<ZDOID>("RequestZDO", new Action<long, ZDOID>(this.RPC_RequestZDO));
		this.m_width = width;
		this.m_halfWidth = this.m_width / 2;
		this.ResetSectorArray();
	}

	private void ResetSectorArray()
	{
		this.m_objectsBySector = new List<ZDO>[this.m_width * this.m_width];
		this.m_objectsByOutsideSector.Clear();
	}

	public void ShutDown()
	{
		if (!ZNet.instance.IsServer())
		{
			int num = this.FlushClientObjects();
			ZLog.Log("Flushed " + num + " objects");
		}
		ZDOPool.Release(this.m_objectsByID);
		this.m_objectsByID.Clear();
		this.m_tempToSync.Clear();
		this.m_tempToSyncDistant.Clear();
		this.m_tempNearObjects.Clear();
		this.m_tempRemoveList.Clear();
		this.m_peers.Clear();
		this.ResetSectorArray();
		GC.Collect();
	}

	public void PrepareSave()
	{
		this.m_saveData = new ZDOMan.SaveData();
		this.m_saveData.m_myid = this.m_myid;
		this.m_saveData.m_nextUid = this.m_nextUid;
		Stopwatch stopwatch = Stopwatch.StartNew();
		this.m_saveData.m_zdos = this.GetSaveClone();
		ZLog.Log("clone " + stopwatch.ElapsedMilliseconds);
		this.m_saveData.m_deadZDOs = new Dictionary<ZDOID, long>(this.m_deadZDOs);
	}

	public void SaveAsync(BinaryWriter writer)
	{
		writer.Write(this.m_saveData.m_myid);
		writer.Write(this.m_saveData.m_nextUid);
		ZPackage zpackage = new ZPackage();
		writer.Write(this.m_saveData.m_zdos.Count);
		foreach (ZDO zdo in this.m_saveData.m_zdos)
		{
			writer.Write(zdo.m_uid.userID);
			writer.Write(zdo.m_uid.id);
			zpackage.Clear();
			zdo.Save(zpackage);
			byte[] array = zpackage.GetArray();
			writer.Write(array.Length);
			writer.Write(array);
		}
		writer.Write(this.m_saveData.m_deadZDOs.Count);
		foreach (KeyValuePair<ZDOID, long> keyValuePair in this.m_saveData.m_deadZDOs)
		{
			writer.Write(keyValuePair.Key.userID);
			writer.Write(keyValuePair.Key.id);
			writer.Write(keyValuePair.Value);
		}
		ZLog.Log("Saved " + this.m_saveData.m_zdos.Count + " zdos");
		this.m_saveData = null;
	}

	public void Load(BinaryReader reader, int version)
	{
		try
		{
			reader.ReadInt64();
			uint num = reader.ReadUInt32();
			int num2 = reader.ReadInt32();
			ZDOPool.Release(this.m_objectsByID);
			this.m_objectsByID.Clear();
			this.ResetSectorArray();
			ZLog.Log(string.Concat(new object[]
			{
				"Loading ",
				num2,
				" zdos , my id ",
				this.m_myid,
				" data version:",
				version
			}));
			ZPackage zpackage = new ZPackage();
			for (int i = 0; i < num2; i++)
			{
				ZDO zdo = ZDOPool.Create(this);
				zdo.m_uid = new ZDOID(reader);
				int count = reader.ReadInt32();
				byte[] data = reader.ReadBytes(count);
				zpackage.Load(data);
				zdo.Load(zpackage, version);
				zdo.SetOwner(0L);
				this.m_objectsByID.Add(zdo.m_uid, zdo);
				this.AddToSector(zdo, zdo.GetSector());
				if (zdo.m_uid.userID == this.m_myid && zdo.m_uid.id >= num)
				{
					num = zdo.m_uid.id + 1U;
				}
			}
			this.m_deadZDOs.Clear();
			int num3 = reader.ReadInt32();
			for (int j = 0; j < num3; j++)
			{
				ZDOID key = new ZDOID(reader.ReadInt64(), reader.ReadUInt32());
				long value = reader.ReadInt64();
				this.m_deadZDOs.Add(key, value);
				if (key.userID == this.m_myid && key.id >= num)
				{
					num = key.id + 1U;
				}
			}
			this.CapDeadZDOList();
			ZLog.Log("Loaded " + this.m_deadZDOs.Count + " dead zdos");
			this.RemoveOldGeneratedZDOS();
			this.m_nextUid = num;
		}
		catch (Exception ex)
		{
			ZLog.LogError("Exception while loading ZDO data:" + ex.ToString());
			ZDOPool.Release(this.m_objectsByID);
			this.m_objectsByID.Clear();
			this.ResetSectorArray();
			this.m_deadZDOs.Clear();
			this.m_myid = Utils.GenerateUID();
			this.m_nextUid = 1U;
		}
	}

	private void RemoveOldGeneratedZDOS()
	{
		List<ZDOID> list = new List<ZDOID>();
		foreach (KeyValuePair<ZDOID, ZDO> keyValuePair in this.m_objectsByID)
		{
			int pgwversion = keyValuePair.Value.GetPGWVersion();
			if (pgwversion != 0 && pgwversion != ZoneSystem.instance.m_pgwVersion)
			{
				list.Add(keyValuePair.Key);
				this.RemoveFromSector(keyValuePair.Value, keyValuePair.Value.GetSector());
				ZDOPool.Release(keyValuePair.Value);
			}
		}
		foreach (ZDOID key in list)
		{
			this.m_objectsByID.Remove(key);
		}
		ZLog.Log("Removed " + list.Count + " OLD generated ZDOS");
	}

	private void CapDeadZDOList()
	{
		if (this.m_deadZDOs.Count > 100000)
		{
			List<KeyValuePair<ZDOID, long>> list = this.m_deadZDOs.ToList<KeyValuePair<ZDOID, long>>();
			list.Sort((KeyValuePair<ZDOID, long> a, KeyValuePair<ZDOID, long> b) => a.Value.CompareTo(b.Value));
			int num = list.Count - 100000;
			for (int i = 0; i < num; i++)
			{
				this.m_deadZDOs.Remove(list[i].Key);
			}
		}
	}

	public ZDO CreateNewZDO(Vector3 position)
	{
		long myid = this.m_myid;
		uint nextUid = this.m_nextUid;
		this.m_nextUid = nextUid + 1U;
		ZDOID zdoid = new ZDOID(myid, nextUid);
		while (this.GetZDO(zdoid) != null)
		{
			long myid2 = this.m_myid;
			nextUid = this.m_nextUid;
			this.m_nextUid = nextUid + 1U;
			zdoid = new ZDOID(myid2, nextUid);
		}
		return this.CreateNewZDO(zdoid, position);
	}

	public ZDO CreateNewZDO(ZDOID uid, Vector3 position)
	{
		ZDO zdo = ZDOPool.Create(this, uid, position);
		zdo.m_owner = this.m_myid;
		zdo.m_timeCreated = ZNet.instance.GetTime().Ticks;
		this.m_objectsByID.Add(uid, zdo);
		return zdo;
	}

	public void AddToSector(ZDO zdo, Vector2i sector)
	{
		int num = this.SectorToIndex(sector);
		if (num >= 0 && num < this.m_objectsBySector.Length)
		{
			if (this.m_objectsBySector[num] != null)
			{
				this.m_objectsBySector[num].Add(zdo);
				return;
			}
			List<ZDO> list = new List<ZDO>();
			list.Add(zdo);
			this.m_objectsBySector[num] = list;
			return;
		}
		else
		{
			List<ZDO> list2;
			if (this.m_objectsByOutsideSector.TryGetValue(sector, out list2))
			{
				list2.Add(zdo);
				return;
			}
			list2 = new List<ZDO>();
			list2.Add(zdo);
			this.m_objectsByOutsideSector.Add(sector, list2);
			return;
		}
	}

	public void ZDOSectorInvalidated(ZDO zdo)
	{
		ZDOID uid = zdo.m_uid;
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			zdopeer.ZDOSectorInvalidated(uid);
		}
	}

	public void RemoveFromSector(ZDO zdo, Vector2i sector)
	{
		int num = this.SectorToIndex(sector);
		List<ZDO> list;
		if (num >= 0 && num < this.m_objectsBySector.Length)
		{
			if (this.m_objectsBySector[num] != null)
			{
				this.m_objectsBySector[num].Remove(zdo);
				return;
			}
		}
		else if (this.m_objectsByOutsideSector.TryGetValue(sector, out list))
		{
			list.Remove(zdo);
		}
	}

	public ZDO GetZDO(ZDOID id)
	{
		if (id == ZDOID.None)
		{
			return null;
		}
		ZDO result;
		if (this.m_objectsByID.TryGetValue(id, out result))
		{
			return result;
		}
		return null;
	}

	public void AddPeer(ZNetPeer netPeer)
	{
		ZDOMan.ZDOPeer zdopeer = new ZDOMan.ZDOPeer();
		zdopeer.m_peer = netPeer;
		this.m_peers.Add(zdopeer);
		zdopeer.m_peer.m_rpc.Register<ZPackage>("ZDOData", new Action<ZRpc, ZPackage>(this.RPC_ZDOData));
	}

	public void RemovePeer(ZNetPeer netPeer)
	{
		ZDOMan.ZDOPeer zdopeer = this.FindPeer(netPeer);
		if (zdopeer != null)
		{
			this.m_peers.Remove(zdopeer);
			if (ZNet.instance.IsServer())
			{
				this.RemoveOrphanNonPersistentZDOS();
			}
		}
	}

	private ZDOMan.ZDOPeer FindPeer(ZNetPeer netPeer)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			if (zdopeer.m_peer == netPeer)
			{
				return zdopeer;
			}
		}
		return null;
	}

	private ZDOMan.ZDOPeer FindPeer(ZRpc rpc)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			if (zdopeer.m_peer.m_rpc == rpc)
			{
				return zdopeer;
			}
		}
		return null;
	}

	public void Update(float dt)
	{
		if (ZNet.instance.IsServer())
		{
			this.ReleaseZDOS(dt);
		}
		this.SendZDOToPeers(dt);
		this.SendDestroyed();
		this.UpdateStats(dt);
	}

	private void UpdateStats(float dt)
	{
		this.m_statTimer += dt;
		if (this.m_statTimer >= 1f)
		{
			this.m_statTimer = 0f;
			this.m_zdosSentLastSec = this.m_zdosSent;
			this.m_zdosRecvLastSec = this.m_zdosRecv;
			this.m_zdosRecv = 0;
			this.m_zdosSent = 0;
		}
	}

	private void SendZDOToPeers(float dt)
	{
		int num = 0;
		this.m_sendTimer += dt;
		if (this.m_sendTimer > 0.05f)
		{
			this.m_sendTimer = 0f;
			foreach (ZDOMan.ZDOPeer peer in this.m_peers)
			{
				num += this.SendZDOs(peer, false);
			}
		}
		this.m_zdosSent += num;
	}

	private int FlushClientObjects()
	{
		int num = 0;
		foreach (ZDOMan.ZDOPeer peer in this.m_peers)
		{
			num += this.SendZDOs(peer, true);
		}
		return num;
	}

	private void ReleaseZDOS(float dt)
	{
		this.m_releaseZDOTimer += dt;
		if (this.m_releaseZDOTimer > 2f)
		{
			this.m_releaseZDOTimer = 0f;
			this.ReleaseNearbyZDOS(ZNet.instance.GetReferencePosition(), this.m_myid);
			foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
			{
				this.ReleaseNearbyZDOS(zdopeer.m_peer.m_refPos, zdopeer.m_peer.m_uid);
			}
		}
	}

	private bool IsInPeerActiveArea(Vector2i sector, long uid)
	{
		if (uid == this.m_myid)
		{
			return ZNetScene.instance.InActiveArea(sector, ZNet.instance.GetReferencePosition());
		}
		ZNetPeer peer = ZNet.instance.GetPeer(uid);
		return peer != null && ZNetScene.instance.InActiveArea(sector, peer.GetRefPos());
	}

	private void ReleaseNearbyZDOS(Vector3 refPosition, long uid)
	{
		Vector2i zone = ZoneSystem.instance.GetZone(refPosition);
		this.m_tempNearObjects.Clear();
		this.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, 0, this.m_tempNearObjects, null);
		foreach (ZDO zdo in this.m_tempNearObjects)
		{
			if (zdo.m_persistent)
			{
				if (zdo.m_owner == uid)
				{
					if (!ZNetScene.instance.InActiveArea(zdo.GetSector(), zone))
					{
						zdo.SetOwner(0L);
					}
				}
				else if ((zdo.m_owner == 0L || !this.IsInPeerActiveArea(zdo.GetSector(), zdo.m_owner)) && ZNetScene.instance.InActiveArea(zdo.GetSector(), zone))
				{
					zdo.SetOwner(uid);
				}
			}
		}
	}

	public void DestroyZDO(ZDO zdo)
	{
		if (!zdo.IsOwner())
		{
			return;
		}
		this.m_destroySendList.Add(zdo.m_uid);
	}

	private void SendDestroyed()
	{
		if (this.m_destroySendList.Count == 0)
		{
			return;
		}
		ZPackage zpackage = new ZPackage();
		zpackage.Write(this.m_destroySendList.Count);
		foreach (ZDOID id in this.m_destroySendList)
		{
			zpackage.Write(id);
		}
		this.m_destroySendList.Clear();
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "DestroyZDO", new object[]
		{
			zpackage
		});
	}

	private void RPC_DestroyZDO(long sender, ZPackage pkg)
	{
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			ZDOID uid = pkg.ReadZDOID();
			this.HandleDestroyedZDO(uid);
		}
	}

	private void HandleDestroyedZDO(ZDOID uid)
	{
		if (uid.userID == this.m_myid && uid.id >= this.m_nextUid)
		{
			this.m_nextUid = uid.id + 1U;
		}
		ZDO zdo = this.GetZDO(uid);
		if (zdo == null)
		{
			return;
		}
		if (this.m_onZDODestroyed != null)
		{
			this.m_onZDODestroyed(zdo);
		}
		this.RemoveFromSector(zdo, zdo.GetSector());
		this.m_objectsByID.Remove(zdo.m_uid);
		ZDOPool.Release(zdo);
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			zdopeer.m_zdos.Remove(uid);
		}
		if (ZNet.instance.IsServer())
		{
			long ticks = ZNet.instance.GetTime().Ticks;
			this.m_deadZDOs[uid] = ticks;
		}
	}

	private int SendZDOs(ZDOMan.ZDOPeer peer, bool flush)
	{
		if (!flush && peer.m_peer.m_socket.IsSending())
		{
			return 0;
		}
		float time = Time.time;
		this.m_tempToSync.Clear();
		this.CreateSyncList(peer, this.m_tempToSync);
		if (this.m_tempToSync.Count <= 0)
		{
			return 0;
		}
		int num = this.m_dataPerSec / 20;
		ZPackage zpackage = new ZPackage();
		ZPackage zpackage2 = new ZPackage();
		int num2 = 0;
		for (int i = 0; i < this.m_tempToSync.Count; i++)
		{
			ZDO zdo = this.m_tempToSync[i];
			peer.m_forceSend.Remove(zdo.m_uid);
			if (!ZNet.instance.IsServer())
			{
				this.m_clientChangeQueue.Remove(zdo.m_uid);
			}
			if (peer.ShouldSend(zdo))
			{
				zpackage.Write(zdo.m_uid);
				zpackage.Write(zdo.m_ownerRevision);
				zpackage.Write(zdo.m_dataRevision);
				zpackage.Write(zdo.m_owner);
				zpackage.Write(zdo.GetPosition());
				zpackage2.Clear();
				zdo.Serialize(zpackage2);
				zpackage.Write(zpackage2);
				peer.m_zdos[zdo.m_uid] = new ZDOMan.ZDOPeer.PeerZDOInfo(zdo.m_dataRevision, zdo.m_ownerRevision, time);
				num2++;
				if (!flush && zpackage.Size() > num)
				{
					break;
				}
			}
		}
		if (num2 > 0)
		{
			zpackage.Write(ZDOID.None);
			peer.m_peer.m_rpc.Invoke("ZDOData", new object[]
			{
				zpackage
			});
		}
		return num2;
	}

	private void RPC_ZDOData(ZRpc rpc, ZPackage pkg)
	{
		ZDOMan.ZDOPeer zdopeer = this.FindPeer(rpc);
		if (zdopeer == null)
		{
			ZLog.Log("ZDO data from unkown host, ignoring");
			return;
		}
		float time = Time.time;
		int num = 0;
		ZPackage pkg2 = new ZPackage();
		for (;;)
		{
			ZDOID zdoid = pkg.ReadZDOID();
			if (zdoid.IsNone())
			{
				break;
			}
			num++;
			uint num2 = pkg.ReadUInt();
			uint num3 = pkg.ReadUInt();
			long owner = pkg.ReadLong();
			Vector3 vector = pkg.ReadVector3();
			pkg.ReadPackage(ref pkg2);
			ZDO zdo = this.GetZDO(zdoid);
			bool flag = false;
			if (zdo != null)
			{
				if (num3 <= zdo.m_dataRevision)
				{
					if (num2 > zdo.m_ownerRevision)
					{
						zdo.m_owner = owner;
						zdo.m_ownerRevision = num2;
						zdopeer.m_zdos[zdoid] = new ZDOMan.ZDOPeer.PeerZDOInfo(num3, num2, time);
						continue;
					}
					continue;
				}
			}
			else
			{
				zdo = this.CreateNewZDO(zdoid, vector);
				flag = true;
			}
			zdo.m_ownerRevision = num2;
			zdo.m_dataRevision = num3;
			zdo.m_owner = owner;
			zdo.InternalSetPosition(vector);
			zdopeer.m_zdos[zdoid] = new ZDOMan.ZDOPeer.PeerZDOInfo(zdo.m_dataRevision, zdo.m_ownerRevision, time);
			zdo.Deserialize(pkg2);
			if (ZNet.instance.IsServer() && flag && this.m_deadZDOs.ContainsKey(zdoid))
			{
				zdo.SetOwner(this.m_myid);
				this.DestroyZDO(zdo);
			}
		}
		this.m_zdosRecv += num;
	}

	public void FindSectorObjects(Vector2i sector, int area, int distantArea, List<ZDO> sectorObjects, List<ZDO> distantSectorObjects = null)
	{
		this.FindObjects(sector, sectorObjects);
		for (int i = 1; i <= area; i++)
		{
			for (int j = sector.x - i; j <= sector.x + i; j++)
			{
				this.FindObjects(new Vector2i(j, sector.y - i), sectorObjects);
				this.FindObjects(new Vector2i(j, sector.y + i), sectorObjects);
			}
			for (int k = sector.y - i + 1; k <= sector.y + i - 1; k++)
			{
				this.FindObjects(new Vector2i(sector.x - i, k), sectorObjects);
				this.FindObjects(new Vector2i(sector.x + i, k), sectorObjects);
			}
		}
		List<ZDO> objects = (distantSectorObjects != null) ? distantSectorObjects : sectorObjects;
		for (int l = area + 1; l <= area + distantArea; l++)
		{
			for (int m = sector.x - l; m <= sector.x + l; m++)
			{
				this.FindDistantObjects(new Vector2i(m, sector.y - l), objects);
				this.FindDistantObjects(new Vector2i(m, sector.y + l), objects);
			}
			for (int n = sector.y - l + 1; n <= sector.y + l - 1; n++)
			{
				this.FindDistantObjects(new Vector2i(sector.x - l, n), objects);
				this.FindDistantObjects(new Vector2i(sector.x + l, n), objects);
			}
		}
	}

	public void FindSectorObjects(Vector2i sector, int area, List<ZDO> sectorObjects)
	{
		for (int i = sector.y - area; i <= sector.y + area; i++)
		{
			for (int j = sector.x - area; j <= sector.x + area; j++)
			{
				Vector2i sector2 = new Vector2i(j, i);
				this.FindObjects(sector2, sectorObjects);
			}
		}
	}

	private void CreateSyncList(ZDOMan.ZDOPeer peer, List<ZDO> toSync)
	{
		if (ZNet.instance.IsServer())
		{
			Vector3 refPos = peer.m_peer.GetRefPos();
			Vector2i zone = ZoneSystem.instance.GetZone(refPos);
			this.m_tempToSyncDistant.Clear();
			this.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, ZoneSystem.instance.m_activeDistantArea, toSync, this.m_tempToSyncDistant);
			this.ServerSortSendZDOS(toSync, refPos, peer);
			toSync.AddRange(this.m_tempToSyncDistant);
			this.AddForceSendZdos(peer, toSync);
			return;
		}
		this.m_tempRemoveList.Clear();
		foreach (ZDOID zdoid in this.m_clientChangeQueue)
		{
			ZDO zdo = this.GetZDO(zdoid);
			if (zdo != null)
			{
				toSync.Add(zdo);
			}
			else
			{
				this.m_tempRemoveList.Add(zdoid);
			}
		}
		foreach (ZDOID item in this.m_tempRemoveList)
		{
			this.m_clientChangeQueue.Remove(item);
		}
		this.ClientSortSendZDOS(toSync, peer);
		this.AddForceSendZdos(peer, toSync);
	}

	private void AddForceSendZdos(ZDOMan.ZDOPeer peer, List<ZDO> syncList)
	{
		if (peer.m_forceSend.Count > 0)
		{
			this.m_tempRemoveList.Clear();
			foreach (ZDOID zdoid in peer.m_forceSend)
			{
				ZDO zdo = this.GetZDO(zdoid);
				if (zdo != null)
				{
					syncList.Insert(0, zdo);
				}
				else
				{
					this.m_tempRemoveList.Add(zdoid);
				}
			}
			foreach (ZDOID item in this.m_tempRemoveList)
			{
				peer.m_forceSend.Remove(item);
			}
		}
	}

	private static int ServerSendCompare(ZDO x, ZDO y)
	{
		bool flag = x.m_owner != ZDOMan.compareReceiver;
		bool flag2 = y.m_owner != ZDOMan.compareReceiver;
		if (flag && flag2)
		{
			if (x.m_type == y.m_type)
			{
				return x.m_tempSortValue.CompareTo(y.m_tempSortValue);
			}
			if (x.m_type == ZDO.ObjectType.Prioritized)
			{
				return -1;
			}
			if (y.m_type == ZDO.ObjectType.Prioritized)
			{
				return 1;
			}
			return x.m_tempSortValue.CompareTo(y.m_tempSortValue);
		}
		else
		{
			if (flag != flag2)
			{
				if (flag && x.m_type == ZDO.ObjectType.Prioritized)
				{
					return -1;
				}
				if (flag2 && y.m_type == ZDO.ObjectType.Prioritized)
				{
					return 1;
				}
			}
			if (x.m_type == y.m_type)
			{
				return x.m_tempSortValue.CompareTo(y.m_tempSortValue);
			}
			if (x.m_type == ZDO.ObjectType.Solid)
			{
				return -1;
			}
			if (y.m_type == ZDO.ObjectType.Solid)
			{
				return 1;
			}
			if (x.m_type == ZDO.ObjectType.Prioritized)
			{
				return -1;
			}
			if (y.m_type == ZDO.ObjectType.Prioritized)
			{
				return 1;
			}
			return x.m_tempSortValue.CompareTo(y.m_tempSortValue);
		}
	}

	private void ServerSortSendZDOS(List<ZDO> objects, Vector3 refPos, ZDOMan.ZDOPeer peer)
	{
		float time = Time.time;
		for (int i = 0; i < objects.Count; i++)
		{
			ZDO zdo = objects[i];
			Vector3 position = zdo.GetPosition();
			zdo.m_tempSortValue = Vector3.Distance(position, refPos);
			float num = 100f;
			ZDOMan.ZDOPeer.PeerZDOInfo peerZDOInfo;
			if (peer.m_zdos.TryGetValue(zdo.m_uid, out peerZDOInfo))
			{
				num = Mathf.Clamp(time - peerZDOInfo.m_syncTime, 0f, 100f);
				zdo.m_tempHaveRevision = true;
			}
			else
			{
				zdo.m_tempHaveRevision = false;
			}
			zdo.m_tempSortValue -= num * 1.5f;
		}
		ZDOMan.compareReceiver = peer.m_peer.m_uid;
		objects.Sort(new Comparison<ZDO>(ZDOMan.ServerSendCompare));
	}

	private static int ClientSendCompare(ZDO x, ZDO y)
	{
		if (x.m_type == y.m_type)
		{
			return x.m_tempSortValue.CompareTo(y.m_tempSortValue);
		}
		if (x.m_type == ZDO.ObjectType.Prioritized)
		{
			return -1;
		}
		if (y.m_type == ZDO.ObjectType.Prioritized)
		{
			return 1;
		}
		return x.m_tempSortValue.CompareTo(y.m_tempSortValue);
	}

	private void ClientSortSendZDOS(List<ZDO> objects, ZDOMan.ZDOPeer peer)
	{
		float time = Time.time;
		for (int i = 0; i < objects.Count; i++)
		{
			ZDO zdo = objects[i];
			zdo.m_tempSortValue = 0f;
			float num = 100f;
			ZDOMan.ZDOPeer.PeerZDOInfo peerZDOInfo;
			if (peer.m_zdos.TryGetValue(zdo.m_uid, out peerZDOInfo))
			{
				num = Mathf.Clamp(time - peerZDOInfo.m_syncTime, 0f, 100f);
			}
			zdo.m_tempSortValue -= num * 1.5f;
		}
		objects.Sort(new Comparison<ZDO>(ZDOMan.ClientSendCompare));
	}

	private void PrintZdoList(List<ZDO> zdos)
	{
		ZLog.Log("Sync list " + zdos.Count);
		foreach (ZDO zdo in zdos)
		{
			string text = "";
			int prefab = zdo.GetPrefab();
			if (prefab != 0)
			{
				GameObject prefab2 = ZNetScene.instance.GetPrefab(prefab);
				if (prefab2)
				{
					text = prefab2.name;
				}
			}
			ZLog.Log(string.Concat(new object[]
			{
				"  ",
				zdo.m_uid.ToString(),
				"  ",
				zdo.m_ownerRevision,
				" prefab:",
				text
			}));
		}
	}

	private void AddDistantObjects(ZDOMan.ZDOPeer peer, int maxItems, List<ZDO> toSync)
	{
		if (peer.m_sendIndex >= this.m_objectsByID.Count)
		{
			peer.m_sendIndex = 0;
		}
		IEnumerable<KeyValuePair<ZDOID, ZDO>> enumerable = this.m_objectsByID.Skip(peer.m_sendIndex).Take(maxItems);
		peer.m_sendIndex += maxItems;
		foreach (KeyValuePair<ZDOID, ZDO> keyValuePair in enumerable)
		{
			toSync.Add(keyValuePair.Value);
		}
	}

	public long GetMyID()
	{
		return this.m_myid;
	}

	private int SectorToIndex(Vector2i s)
	{
		return (s.y + this.m_halfWidth) * this.m_width + (s.x + this.m_halfWidth);
	}

	private void FindObjects(Vector2i sector, List<ZDO> objects)
	{
		int num = this.SectorToIndex(sector);
		List<ZDO> collection;
		if (num >= 0 && num < this.m_objectsBySector.Length)
		{
			if (this.m_objectsBySector[num] != null)
			{
				objects.AddRange(this.m_objectsBySector[num]);
				return;
			}
		}
		else if (this.m_objectsByOutsideSector.TryGetValue(sector, out collection))
		{
			objects.AddRange(collection);
		}
	}

	private void FindDistantObjects(Vector2i sector, List<ZDO> objects)
	{
		int num = this.SectorToIndex(sector);
		List<ZDO> list2;
		if (num >= 0 && num < this.m_objectsBySector.Length)
		{
			List<ZDO> list = this.m_objectsBySector[num];
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					ZDO zdo = list[i];
					if (zdo.m_distant)
					{
						objects.Add(zdo);
					}
				}
				return;
			}
		}
		else if (this.m_objectsByOutsideSector.TryGetValue(sector, out list2))
		{
			for (int j = 0; j < list2.Count; j++)
			{
				ZDO zdo2 = list2[j];
				if (zdo2.m_distant)
				{
					objects.Add(zdo2);
				}
			}
		}
	}

	private void RemoveOrphanNonPersistentZDOS()
	{
		foreach (KeyValuePair<ZDOID, ZDO> keyValuePair in this.m_objectsByID)
		{
			ZDO value = keyValuePair.Value;
			if (!value.m_persistent && (value.m_owner == 0L || !this.IsPeerConnected(value.m_owner)))
			{
				ZLog.Log(string.Concat(new object[]
				{
					"Destroying abandoned non persistent zdo ",
					value.m_uid,
					" owner ",
					value.m_owner
				}));
				value.SetOwner(this.m_myid);
				this.DestroyZDO(value);
			}
		}
	}

	private bool IsPeerConnected(long uid)
	{
		if (this.m_myid == uid)
		{
			return true;
		}
		using (List<ZDOMan.ZDOPeer>.Enumerator enumerator = this.m_peers.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_peer.m_uid == uid)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void GetAllZDOsWithPrefab(string prefab, List<ZDO> zdos)
	{
		int stableHashCode = prefab.GetStableHashCode();
		foreach (ZDO zdo in this.m_objectsByID.Values)
		{
			if (zdo.GetPrefab() == stableHashCode)
			{
				zdos.Add(zdo);
			}
		}
	}

	private static bool InvalidZDO(ZDO zdo)
	{
		return !zdo.IsValid();
	}

	public bool GetAllZDOsWithPrefabIterative(string prefab, List<ZDO> zdos, ref int index)
	{
		int stableHashCode = prefab.GetStableHashCode();
		if (index >= this.m_objectsBySector.Length)
		{
			foreach (List<ZDO> list in this.m_objectsByOutsideSector.Values)
			{
				foreach (ZDO zdo in list)
				{
					if (zdo.GetPrefab() == stableHashCode)
					{
						zdos.Add(zdo);
					}
				}
			}
			zdos.RemoveAll(new Predicate<ZDO>(ZDOMan.InvalidZDO));
			return true;
		}
		int num = 0;
		while (index < this.m_objectsBySector.Length)
		{
			List<ZDO> list2 = this.m_objectsBySector[index];
			if (list2 != null)
			{
				foreach (ZDO zdo2 in list2)
				{
					if (zdo2.GetPrefab() == stableHashCode)
					{
						zdos.Add(zdo2);
					}
				}
				num++;
				if (num > 400)
				{
					break;
				}
			}
			index++;
		}
		return false;
	}

	private List<ZDO> GetSaveClone()
	{
		List<ZDO> list = new List<ZDO>();
		for (int i = 0; i < this.m_objectsBySector.Length; i++)
		{
			if (this.m_objectsBySector[i] != null)
			{
				foreach (ZDO zdo in this.m_objectsBySector[i])
				{
					if (zdo.m_persistent)
					{
						list.Add(zdo.Clone());
					}
				}
			}
		}
		foreach (List<ZDO> list2 in this.m_objectsByOutsideSector.Values)
		{
			foreach (ZDO zdo2 in list2)
			{
				if (zdo2.m_persistent)
				{
					list.Add(zdo2.Clone());
				}
			}
		}
		return list;
	}

	public int NrOfObjects()
	{
		return this.m_objectsByID.Count;
	}

	public int GetSentZDOs()
	{
		return this.m_zdosSentLastSec;
	}

	public int GetRecvZDOs()
	{
		return this.m_zdosRecvLastSec;
	}

	public void GetAverageStats(out float sentZdos, out float recvZdos)
	{
		sentZdos = (float)this.m_zdosSentLastSec / 20f;
		recvZdos = (float)this.m_zdosRecvLastSec / 20f;
	}

	public int GetClientChangeQueue()
	{
		return this.m_clientChangeQueue.Count;
	}

	public void RequestZDO(ZDOID id)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC("RequestZDO", new object[]
		{
			id
		});
	}

	private void RPC_RequestZDO(long sender, ZDOID id)
	{
		ZDOMan.ZDOPeer peer = this.GetPeer(sender);
		if (peer != null)
		{
			peer.ForceSendZDO(id);
		}
	}

	private ZDOMan.ZDOPeer GetPeer(long uid)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			if (zdopeer.m_peer.m_uid == uid)
			{
				return zdopeer;
			}
		}
		return null;
	}

	public void ForceSendZDO(ZDOID id)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			zdopeer.ForceSendZDO(id);
		}
	}

	public void ForceSendZDO(long peerID, ZDOID id)
	{
		if (ZNet.instance.IsServer())
		{
			ZDOMan.ZDOPeer peer = this.GetPeer(peerID);
			if (peer != null)
			{
				peer.ForceSendZDO(id);
				return;
			}
		}
		else
		{
			foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
			{
				zdopeer.ForceSendZDO(id);
			}
		}
	}

	public void ClientChanged(ZDOID id)
	{
		this.m_clientChangeQueue.Add(id);
	}

	private static long compareReceiver;

	public Action<ZDO> m_onZDODestroyed;

	private Dictionary<ZDOID, ZDO> m_objectsByID = new Dictionary<ZDOID, ZDO>();

	private List<ZDO>[] m_objectsBySector;

	private Dictionary<Vector2i, List<ZDO>> m_objectsByOutsideSector = new Dictionary<Vector2i, List<ZDO>>();

	private List<ZDOMan.ZDOPeer> m_peers = new List<ZDOMan.ZDOPeer>();

	private const int m_maxDeadZDOs = 100000;

	private Dictionary<ZDOID, long> m_deadZDOs = new Dictionary<ZDOID, long>();

	private List<ZDOID> m_destroySendList = new List<ZDOID>();

	private HashSet<ZDOID> m_clientChangeQueue = new HashSet<ZDOID>();

	private long m_myid;

	private uint m_nextUid = 1U;

	private int m_width;

	private int m_halfWidth;

	private int m_dataPerSec = 61440;

	private float m_sendTimer;

	private const float m_sendFPS = 20f;

	private float m_releaseZDOTimer;

	private static ZDOMan m_instance;

	private int m_zdosSent;

	private int m_zdosRecv;

	private int m_zdosSentLastSec;

	private int m_zdosRecvLastSec;

	private float m_statTimer;

	private List<ZDO> m_tempToSync = new List<ZDO>();

	private List<ZDO> m_tempToSyncDistant = new List<ZDO>();

	private List<ZDO> m_tempNearObjects = new List<ZDO>();

	private List<ZDOID> m_tempRemoveList = new List<ZDOID>();

	private ZDOMan.SaveData m_saveData;

	private class ZDOPeer
	{
		public void ZDOSectorInvalidated(ZDOID uid)
		{
			if (this.m_zdos.ContainsKey(uid))
			{
				this.ForceSendZDO(uid);
			}
		}

		public void ForceSendZDO(ZDOID id)
		{
			this.m_forceSend.Add(id);
		}

		public bool ShouldSend(ZDO zdo)
		{
			ZDOMan.ZDOPeer.PeerZDOInfo peerZDOInfo;
			return !this.m_zdos.TryGetValue(zdo.m_uid, out peerZDOInfo) || (ulong)zdo.m_ownerRevision > (ulong)peerZDOInfo.m_ownerRevision || zdo.m_dataRevision > peerZDOInfo.m_dataRevision;
		}

		public ZNetPeer m_peer;

		public Dictionary<ZDOID, ZDOMan.ZDOPeer.PeerZDOInfo> m_zdos = new Dictionary<ZDOID, ZDOMan.ZDOPeer.PeerZDOInfo>();

		public HashSet<ZDOID> m_forceSend = new HashSet<ZDOID>();

		public int m_sendIndex;

		public struct PeerZDOInfo
		{
			public PeerZDOInfo(uint dataRevision, uint ownerRevision, float syncTime)
			{
				this.m_dataRevision = dataRevision;
				this.m_ownerRevision = (long)((ulong)ownerRevision);
				this.m_syncTime = syncTime;
			}

			public uint m_dataRevision;

			public long m_ownerRevision;

			public float m_syncTime;
		}
	}

	private class SaveData
	{
		public long m_myid;

		public uint m_nextUid = 1U;

		public List<ZDO> m_zdos;

		public Dictionary<ZDOID, long> m_deadZDOs;
	}
}
