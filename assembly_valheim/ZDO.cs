using System;
using System.Collections.Generic;
using UnityEngine;

public class ZDO : IEquatable<ZDO>
{
	public void Initialize(ZDOMan man, ZDOID id, Vector3 position)
	{
		this.m_zdoMan = man;
		this.m_uid = id;
		this.m_position = position;
		this.m_sector = ZoneSystem.instance.GetZone(this.m_position);
		this.m_zdoMan.AddToSector(this, this.m_sector);
	}

	public void Initialize(ZDOMan man)
	{
		this.m_zdoMan = man;
	}

	public bool IsValid()
	{
		return this.m_zdoMan != null;
	}

	public void Reset()
	{
		this.m_uid = ZDOID.None;
		this.m_persistent = false;
		this.m_owner = 0L;
		this.m_timeCreated = 0L;
		this.m_ownerRevision = 0U;
		this.m_dataRevision = 0U;
		this.m_pgwVersion = 0;
		this.m_distant = false;
		this.m_tempSortValue = 0f;
		this.m_tempHaveRevision = false;
		this.m_prefab = 0;
		this.m_sector = Vector2i.zero;
		this.m_position = Vector3.zero;
		this.m_rotation = Quaternion.identity;
		this.ReleaseFloats();
		this.ReleaseVec3();
		this.ReleaseQuats();
		this.ReleaseInts();
		this.ReleaseLongs();
		this.ReleaseStrings();
		this.m_zdoMan = null;
	}

	public ZDO Clone()
	{
		ZDO zdo = base.MemberwiseClone() as ZDO;
		zdo.m_floats = null;
		zdo.m_vec3 = null;
		zdo.m_quats = null;
		zdo.m_ints = null;
		zdo.m_longs = null;
		zdo.m_strings = null;
		if (this.m_floats != null && this.m_floats.Count > 0)
		{
			zdo.InitFloats();
			zdo.m_floats.Copy(this.m_floats);
		}
		if (this.m_vec3 != null && this.m_vec3.Count > 0)
		{
			zdo.InitVec3();
			zdo.m_vec3.Copy(this.m_vec3);
		}
		if (this.m_quats != null && this.m_quats.Count > 0)
		{
			zdo.InitQuats();
			zdo.m_quats.Copy(this.m_quats);
		}
		if (this.m_ints != null && this.m_ints.Count > 0)
		{
			zdo.InitInts();
			zdo.m_ints.Copy(this.m_ints);
		}
		if (this.m_longs != null && this.m_longs.Count > 0)
		{
			zdo.InitLongs();
			zdo.m_longs.Copy(this.m_longs);
		}
		if (this.m_strings != null && this.m_strings.Count > 0)
		{
			zdo.InitStrings();
			zdo.m_strings.Copy(this.m_strings);
		}
		return zdo;
	}

	public bool Equals(ZDO other)
	{
		return this == other;
	}

	public void Set(KeyValuePair<int, int> hashPair, ZDOID id)
	{
		this.Set(hashPair.Key, id.userID);
		this.Set(hashPair.Value, (long)((ulong)id.id));
	}

	public static KeyValuePair<int, int> GetHashZDOID(string name)
	{
		return new KeyValuePair<int, int>((name + "_u").GetStableHashCode(), (name + "_i").GetStableHashCode());
	}

	public void Set(string name, ZDOID id)
	{
		this.Set(ZDO.GetHashZDOID(name), id);
	}

	public ZDOID GetZDOID(KeyValuePair<int, int> hashPair)
	{
		long @long = this.GetLong(hashPair.Key, 0L);
		uint num = (uint)this.GetLong(hashPair.Value, 0L);
		if (@long == 0L || num == 0U)
		{
			return ZDOID.None;
		}
		return new ZDOID(@long, num);
	}

	public ZDOID GetZDOID(string name)
	{
		return this.GetZDOID(ZDO.GetHashZDOID(name));
	}

	public void Set(string name, float value)
	{
		int stableHashCode = name.GetStableHashCode();
		this.Set(stableHashCode, value);
	}

	public void Set(int hash, float value)
	{
		this.InitFloats();
		float num;
		if (this.m_floats.TryGetValue(hash, out num) && num == value)
		{
			return;
		}
		this.m_floats[hash] = value;
		this.IncreseDataRevision();
	}

	public void Set(string name, Vector3 value)
	{
		int stableHashCode = name.GetStableHashCode();
		this.Set(stableHashCode, value);
	}

	public void Set(int hash, Vector3 value)
	{
		this.InitVec3();
		Vector3 lhs;
		if (this.m_vec3.TryGetValue(hash, out lhs) && lhs == value)
		{
			return;
		}
		this.m_vec3[hash] = value;
		this.IncreseDataRevision();
	}

	public void Set(string name, Quaternion value)
	{
		int stableHashCode = name.GetStableHashCode();
		this.Set(stableHashCode, value);
	}

	public void Set(int hash, Quaternion value)
	{
		this.InitQuats();
		Quaternion lhs;
		if (this.m_quats.TryGetValue(hash, out lhs) && lhs == value)
		{
			return;
		}
		this.m_quats[hash] = value;
		this.IncreseDataRevision();
	}

	public void Set(string name, int value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	public void Set(int hash, int value)
	{
		this.InitInts();
		int num;
		if (this.m_ints.TryGetValue(hash, out num) && num == value)
		{
			return;
		}
		this.m_ints[hash] = value;
		this.IncreseDataRevision();
	}

	public void Set(string name, bool value)
	{
		this.Set(name, value ? 1 : 0);
	}

	public void Set(int hash, bool value)
	{
		this.Set(hash, value ? 1 : 0);
	}

	public void Set(string name, long value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	public void Set(int hash, long value)
	{
		this.InitLongs();
		long num;
		if (this.m_longs.TryGetValue(hash, out num) && num == value)
		{
			return;
		}
		this.m_longs[hash] = value;
		this.IncreseDataRevision();
	}

	public void Set(string name, byte[] bytes)
	{
		string value = Convert.ToBase64String(bytes);
		this.Set(name, value);
	}

	public byte[] GetByteArray(string name)
	{
		string @string = this.GetString(name, "");
		if (@string.Length > 0)
		{
			return Convert.FromBase64String(@string);
		}
		return null;
	}

	public void Set(string name, string value)
	{
		this.InitStrings();
		int stableHashCode = name.GetStableHashCode();
		string a;
		if (this.m_strings.TryGetValue(stableHashCode, out a) && a == value)
		{
			return;
		}
		this.m_strings[stableHashCode] = value;
		this.IncreseDataRevision();
	}

	public void SetPosition(Vector3 pos)
	{
		this.InternalSetPosition(pos);
	}

	public void InternalSetPosition(Vector3 pos)
	{
		if (this.m_position == pos)
		{
			return;
		}
		this.m_position = pos;
		this.SetSector(ZoneSystem.instance.GetZone(this.m_position));
		if (this.IsOwner())
		{
			this.IncreseDataRevision();
		}
	}

	private void SetSector(Vector2i sector)
	{
		if (this.m_sector == sector)
		{
			return;
		}
		this.m_zdoMan.RemoveFromSector(this, this.m_sector);
		this.m_sector = sector;
		this.m_zdoMan.AddToSector(this, this.m_sector);
		this.m_zdoMan.ZDOSectorInvalidated(this);
	}

	public Vector2i GetSector()
	{
		return this.m_sector;
	}

	public void SetRotation(Quaternion rot)
	{
		if (this.m_rotation == rot)
		{
			return;
		}
		this.m_rotation = rot;
		this.IncreseDataRevision();
	}

	public void SetType(ZDO.ObjectType type)
	{
		if (this.m_type == type)
		{
			return;
		}
		this.m_type = type;
		this.IncreseDataRevision();
	}

	public void SetDistant(bool distant)
	{
		if (this.m_distant == distant)
		{
			return;
		}
		this.m_distant = distant;
		this.IncreseDataRevision();
	}

	public void SetPrefab(int prefab)
	{
		if (this.m_prefab == prefab)
		{
			return;
		}
		this.m_prefab = prefab;
		this.IncreseDataRevision();
	}

	public int GetPrefab()
	{
		return this.m_prefab;
	}

	public Vector3 GetPosition()
	{
		return this.m_position;
	}

	public Quaternion GetRotation()
	{
		return this.m_rotation;
	}

	private void IncreseDataRevision()
	{
		this.m_dataRevision += 1U;
		if (!ZNet.instance.IsServer())
		{
			ZDOMan.instance.ClientChanged(this.m_uid);
		}
	}

	private void IncreseOwnerRevision()
	{
		this.m_ownerRevision += 1U;
		if (!ZNet.instance.IsServer())
		{
			ZDOMan.instance.ClientChanged(this.m_uid);
		}
	}

	public float GetFloat(string name, float defaultValue = 0f)
	{
		return this.GetFloat(name.GetStableHashCode(), defaultValue);
	}

	public float GetFloat(int hash, float defaultValue = 0f)
	{
		if (this.m_floats == null)
		{
			return defaultValue;
		}
		float result;
		if (this.m_floats.TryGetValue(hash, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public Vector3 GetVec3(string name, Vector3 defaultValue)
	{
		return this.GetVec3(name.GetStableHashCode(), defaultValue);
	}

	public Vector3 GetVec3(int hash, Vector3 defaultValue)
	{
		if (this.m_vec3 == null)
		{
			return defaultValue;
		}
		Vector3 result;
		if (this.m_vec3.TryGetValue(hash, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public Quaternion GetQuaternion(string name, Quaternion defaultValue)
	{
		return this.GetQuaternion(name.GetStableHashCode(), defaultValue);
	}

	public Quaternion GetQuaternion(int hash, Quaternion defaultValue)
	{
		if (this.m_quats == null)
		{
			return defaultValue;
		}
		Quaternion result;
		if (this.m_quats.TryGetValue(hash, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public int GetInt(string name, int defaultValue = 0)
	{
		return this.GetInt(name.GetStableHashCode(), defaultValue);
	}

	public int GetInt(int hash, int defaultValue = 0)
	{
		if (this.m_ints == null)
		{
			return defaultValue;
		}
		int result;
		if (this.m_ints.TryGetValue(hash, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public bool GetBool(string name, bool defaultValue = false)
	{
		return this.GetBool(name.GetStableHashCode(), defaultValue);
	}

	public bool GetBool(int hash, bool defaultValue = false)
	{
		if (this.m_ints == null)
		{
			return defaultValue;
		}
		int num;
		if (this.m_ints.TryGetValue(hash, out num))
		{
			return num != 0;
		}
		return defaultValue;
	}

	public long GetLong(string name, long defaultValue = 0L)
	{
		return this.GetLong(name.GetStableHashCode(), defaultValue);
	}

	public long GetLong(int hash, long defaultValue = 0L)
	{
		if (this.m_longs == null)
		{
			return defaultValue;
		}
		long result;
		if (this.m_longs.TryGetValue(hash, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public string GetString(string name, string defaultValue = "")
	{
		if (this.m_strings == null)
		{
			return defaultValue;
		}
		string result;
		if (this.m_strings.TryGetValue(name.GetStableHashCode(), out result))
		{
			return result;
		}
		return defaultValue;
	}

	public void Serialize(ZPackage pkg)
	{
		pkg.Write(this.m_persistent);
		pkg.Write(this.m_distant);
		pkg.Write(this.m_timeCreated);
		pkg.Write(this.m_pgwVersion);
		pkg.Write((sbyte)this.m_type);
		pkg.Write(this.m_prefab);
		pkg.Write(this.m_rotation);
		int num = 0;
		if (this.m_floats != null && this.m_floats.Count > 0)
		{
			num |= 1;
		}
		if (this.m_vec3 != null && this.m_vec3.Count > 0)
		{
			num |= 2;
		}
		if (this.m_quats != null && this.m_quats.Count > 0)
		{
			num |= 4;
		}
		if (this.m_ints != null && this.m_ints.Count > 0)
		{
			num |= 8;
		}
		if (this.m_strings != null && this.m_strings.Count > 0)
		{
			num |= 16;
		}
		if (this.m_longs != null && this.m_longs.Count > 0)
		{
			num |= 64;
		}
		pkg.Write(num);
		if (this.m_floats != null && this.m_floats.Count > 0)
		{
			pkg.Write((byte)this.m_floats.Count);
			foreach (KeyValuePair<int, float> keyValuePair in this.m_floats)
			{
				pkg.Write(keyValuePair.Key);
				pkg.Write(keyValuePair.Value);
			}
		}
		if (this.m_vec3 != null && this.m_vec3.Count > 0)
		{
			pkg.Write((byte)this.m_vec3.Count);
			foreach (KeyValuePair<int, Vector3> keyValuePair2 in this.m_vec3)
			{
				pkg.Write(keyValuePair2.Key);
				pkg.Write(keyValuePair2.Value);
			}
		}
		if (this.m_quats != null && this.m_quats.Count > 0)
		{
			pkg.Write((byte)this.m_quats.Count);
			foreach (KeyValuePair<int, Quaternion> keyValuePair3 in this.m_quats)
			{
				pkg.Write(keyValuePair3.Key);
				pkg.Write(keyValuePair3.Value);
			}
		}
		if (this.m_ints != null && this.m_ints.Count > 0)
		{
			pkg.Write((byte)this.m_ints.Count);
			foreach (KeyValuePair<int, int> keyValuePair4 in this.m_ints)
			{
				pkg.Write(keyValuePair4.Key);
				pkg.Write(keyValuePair4.Value);
			}
		}
		if (this.m_longs != null && this.m_longs.Count > 0)
		{
			pkg.Write((byte)this.m_longs.Count);
			foreach (KeyValuePair<int, long> keyValuePair5 in this.m_longs)
			{
				pkg.Write(keyValuePair5.Key);
				pkg.Write(keyValuePair5.Value);
			}
		}
		if (this.m_strings != null && this.m_strings.Count > 0)
		{
			pkg.Write((byte)this.m_strings.Count);
			foreach (KeyValuePair<int, string> keyValuePair6 in this.m_strings)
			{
				pkg.Write(keyValuePair6.Key);
				pkg.Write(keyValuePair6.Value);
			}
		}
	}

	public void Deserialize(ZPackage pkg)
	{
		this.m_persistent = pkg.ReadBool();
		this.m_distant = pkg.ReadBool();
		this.m_timeCreated = pkg.ReadLong();
		this.m_pgwVersion = pkg.ReadInt();
		this.m_type = (ZDO.ObjectType)pkg.ReadSByte();
		this.m_prefab = pkg.ReadInt();
		this.m_rotation = pkg.ReadQuaternion();
		int num = pkg.ReadInt();
		if ((num & 1) != 0)
		{
			this.InitFloats();
			int num2 = (int)pkg.ReadByte();
			for (int i = 0; i < num2; i++)
			{
				int key = pkg.ReadInt();
				this.m_floats[key] = pkg.ReadSingle();
			}
		}
		else
		{
			this.ReleaseFloats();
		}
		if ((num & 2) != 0)
		{
			this.InitVec3();
			int num3 = (int)pkg.ReadByte();
			for (int j = 0; j < num3; j++)
			{
				int key2 = pkg.ReadInt();
				this.m_vec3[key2] = pkg.ReadVector3();
			}
		}
		else
		{
			this.ReleaseVec3();
		}
		if ((num & 4) != 0)
		{
			this.InitQuats();
			int num4 = (int)pkg.ReadByte();
			for (int k = 0; k < num4; k++)
			{
				int key3 = pkg.ReadInt();
				this.m_quats[key3] = pkg.ReadQuaternion();
			}
		}
		else
		{
			this.ReleaseQuats();
		}
		if ((num & 8) != 0)
		{
			this.InitInts();
			int num5 = (int)pkg.ReadByte();
			for (int l = 0; l < num5; l++)
			{
				int key4 = pkg.ReadInt();
				this.m_ints[key4] = pkg.ReadInt();
			}
		}
		else
		{
			this.ReleaseInts();
		}
		if ((num & 64) != 0)
		{
			this.InitLongs();
			int num6 = (int)pkg.ReadByte();
			for (int m = 0; m < num6; m++)
			{
				int key5 = pkg.ReadInt();
				this.m_longs[key5] = pkg.ReadLong();
			}
		}
		else
		{
			this.ReleaseLongs();
		}
		if ((num & 16) != 0)
		{
			this.InitStrings();
			int num7 = (int)pkg.ReadByte();
			for (int n = 0; n < num7; n++)
			{
				int key6 = pkg.ReadInt();
				this.m_strings[key6] = pkg.ReadString();
			}
			return;
		}
		this.ReleaseStrings();
	}

	public void Save(ZPackage pkg)
	{
		pkg.Write(this.m_ownerRevision);
		pkg.Write(this.m_dataRevision);
		pkg.Write(this.m_persistent);
		pkg.Write(this.m_owner);
		pkg.Write(this.m_timeCreated);
		pkg.Write(this.m_pgwVersion);
		pkg.Write((sbyte)this.m_type);
		pkg.Write(this.m_distant);
		pkg.Write(this.m_prefab);
		pkg.Write(this.m_sector);
		pkg.Write(this.m_position);
		pkg.Write(this.m_rotation);
		if (this.m_floats != null)
		{
			pkg.Write((char)this.m_floats.Count);
			using (Dictionary<int, float>.Enumerator enumerator = this.m_floats.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<int, float> keyValuePair = enumerator.Current;
					pkg.Write(keyValuePair.Key);
					pkg.Write(keyValuePair.Value);
				}
				goto IL_FB;
			}
		}
		pkg.Write('\0');
		IL_FB:
		if (this.m_vec3 != null)
		{
			pkg.Write((char)this.m_vec3.Count);
			using (Dictionary<int, Vector3>.Enumerator enumerator2 = this.m_vec3.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					KeyValuePair<int, Vector3> keyValuePair2 = enumerator2.Current;
					pkg.Write(keyValuePair2.Key);
					pkg.Write(keyValuePair2.Value);
				}
				goto IL_165;
			}
		}
		pkg.Write('\0');
		IL_165:
		if (this.m_quats != null)
		{
			pkg.Write((char)this.m_quats.Count);
			using (Dictionary<int, Quaternion>.Enumerator enumerator3 = this.m_quats.GetEnumerator())
			{
				while (enumerator3.MoveNext())
				{
					KeyValuePair<int, Quaternion> keyValuePair3 = enumerator3.Current;
					pkg.Write(keyValuePair3.Key);
					pkg.Write(keyValuePair3.Value);
				}
				goto IL_1D1;
			}
		}
		pkg.Write('\0');
		IL_1D1:
		if (this.m_ints != null)
		{
			pkg.Write((char)this.m_ints.Count);
			using (Dictionary<int, int>.Enumerator enumerator4 = this.m_ints.GetEnumerator())
			{
				while (enumerator4.MoveNext())
				{
					KeyValuePair<int, int> keyValuePair4 = enumerator4.Current;
					pkg.Write(keyValuePair4.Key);
					pkg.Write(keyValuePair4.Value);
				}
				goto IL_23D;
			}
		}
		pkg.Write('\0');
		IL_23D:
		if (this.m_longs != null)
		{
			pkg.Write((char)this.m_longs.Count);
			using (Dictionary<int, long>.Enumerator enumerator5 = this.m_longs.GetEnumerator())
			{
				while (enumerator5.MoveNext())
				{
					KeyValuePair<int, long> keyValuePair5 = enumerator5.Current;
					pkg.Write(keyValuePair5.Key);
					pkg.Write(keyValuePair5.Value);
				}
				goto IL_2A9;
			}
		}
		pkg.Write('\0');
		IL_2A9:
		if (this.m_strings != null)
		{
			pkg.Write((char)this.m_strings.Count);
			using (Dictionary<int, string>.Enumerator enumerator6 = this.m_strings.GetEnumerator())
			{
				while (enumerator6.MoveNext())
				{
					KeyValuePair<int, string> keyValuePair6 = enumerator6.Current;
					pkg.Write(keyValuePair6.Key);
					pkg.Write(keyValuePair6.Value);
				}
				return;
			}
		}
		pkg.Write('\0');
	}

	public void Load(ZPackage pkg, int version)
	{
		this.m_ownerRevision = pkg.ReadUInt();
		this.m_dataRevision = pkg.ReadUInt();
		this.m_persistent = pkg.ReadBool();
		this.m_owner = pkg.ReadLong();
		this.m_timeCreated = pkg.ReadLong();
		this.m_pgwVersion = pkg.ReadInt();
		if (version >= 16 && version < 24)
		{
			pkg.ReadInt();
		}
		if (version >= 23)
		{
			this.m_type = (ZDO.ObjectType)pkg.ReadSByte();
		}
		if (version >= 22)
		{
			this.m_distant = pkg.ReadBool();
		}
		if (version < 13)
		{
			pkg.ReadChar();
			pkg.ReadChar();
		}
		if (version >= 17)
		{
			this.m_prefab = pkg.ReadInt();
		}
		this.m_sector = pkg.ReadVector2i();
		this.m_position = pkg.ReadVector3();
		this.m_rotation = pkg.ReadQuaternion();
		int num = (int)pkg.ReadChar();
		if (num > 0)
		{
			this.InitFloats();
			for (int i = 0; i < num; i++)
			{
				int key = pkg.ReadInt();
				this.m_floats[key] = pkg.ReadSingle();
			}
		}
		else
		{
			this.ReleaseFloats();
		}
		int num2 = (int)pkg.ReadChar();
		if (num2 > 0)
		{
			this.InitVec3();
			for (int j = 0; j < num2; j++)
			{
				int key2 = pkg.ReadInt();
				this.m_vec3[key2] = pkg.ReadVector3();
			}
		}
		else
		{
			this.ReleaseVec3();
		}
		int num3 = (int)pkg.ReadChar();
		if (num3 > 0)
		{
			this.InitQuats();
			for (int k = 0; k < num3; k++)
			{
				int key3 = pkg.ReadInt();
				this.m_quats[key3] = pkg.ReadQuaternion();
			}
		}
		else
		{
			this.ReleaseQuats();
		}
		int num4 = (int)pkg.ReadChar();
		if (num4 > 0)
		{
			this.InitInts();
			for (int l = 0; l < num4; l++)
			{
				int key4 = pkg.ReadInt();
				this.m_ints[key4] = pkg.ReadInt();
			}
		}
		else
		{
			this.ReleaseInts();
		}
		int num5 = (int)pkg.ReadChar();
		if (num5 > 0)
		{
			this.InitLongs();
			for (int m = 0; m < num5; m++)
			{
				int key5 = pkg.ReadInt();
				this.m_longs[key5] = pkg.ReadLong();
			}
		}
		else
		{
			this.ReleaseLongs();
		}
		int num6 = (int)pkg.ReadChar();
		if (num6 > 0)
		{
			this.InitStrings();
			for (int n = 0; n < num6; n++)
			{
				int key6 = pkg.ReadInt();
				this.m_strings[key6] = pkg.ReadString();
			}
		}
		else
		{
			this.ReleaseStrings();
		}
		if (version < 17)
		{
			this.m_prefab = this.GetInt("prefab", 0);
		}
	}

	public bool IsOwner()
	{
		return this.m_owner == this.m_zdoMan.GetMyID();
	}

	public bool HasOwner()
	{
		return this.m_owner != 0L;
	}

	public void Print()
	{
		ZLog.Log("UID:" + this.m_uid);
		ZLog.Log("Persistent:" + this.m_persistent.ToString());
		ZLog.Log("Owner:" + this.m_owner);
		ZLog.Log("Revision:" + this.m_ownerRevision);
		foreach (KeyValuePair<int, float> keyValuePair in this.m_floats)
		{
			ZLog.Log(string.Concat(new object[]
			{
				"F:",
				keyValuePair.Key,
				" = ",
				keyValuePair.Value
			}));
		}
	}

	public void SetOwner(long uid)
	{
		if (this.m_owner == uid)
		{
			return;
		}
		this.m_owner = uid;
		this.IncreseOwnerRevision();
	}

	public void SetPGWVersion(int version)
	{
		this.m_pgwVersion = version;
	}

	public int GetPGWVersion()
	{
		return this.m_pgwVersion;
	}

	private void InitFloats()
	{
		if (this.m_floats == null)
		{
			this.m_floats = Pool<Dictionary<int, float>>.Create();
			this.m_floats.Clear();
		}
	}

	private void InitVec3()
	{
		if (this.m_vec3 == null)
		{
			this.m_vec3 = Pool<Dictionary<int, Vector3>>.Create();
			this.m_vec3.Clear();
		}
	}

	private void InitQuats()
	{
		if (this.m_quats == null)
		{
			this.m_quats = Pool<Dictionary<int, Quaternion>>.Create();
			this.m_quats.Clear();
		}
	}

	private void InitInts()
	{
		if (this.m_ints == null)
		{
			this.m_ints = Pool<Dictionary<int, int>>.Create();
			this.m_ints.Clear();
		}
	}

	private void InitLongs()
	{
		if (this.m_longs == null)
		{
			this.m_longs = Pool<Dictionary<int, long>>.Create();
			this.m_longs.Clear();
		}
	}

	private void InitStrings()
	{
		if (this.m_strings == null)
		{
			this.m_strings = Pool<Dictionary<int, string>>.Create();
			this.m_strings.Clear();
		}
	}

	private void ReleaseFloats()
	{
		if (this.m_floats != null)
		{
			Pool<Dictionary<int, float>>.Release(this.m_floats);
			this.m_floats = null;
		}
	}

	private void ReleaseVec3()
	{
		if (this.m_vec3 != null)
		{
			Pool<Dictionary<int, Vector3>>.Release(this.m_vec3);
			this.m_vec3 = null;
		}
	}

	private void ReleaseQuats()
	{
		if (this.m_quats != null)
		{
			Pool<Dictionary<int, Quaternion>>.Release(this.m_quats);
			this.m_quats = null;
		}
	}

	private void ReleaseInts()
	{
		if (this.m_ints != null)
		{
			Pool<Dictionary<int, int>>.Release(this.m_ints);
			this.m_ints = null;
		}
	}

	private void ReleaseLongs()
	{
		if (this.m_longs != null)
		{
			Pool<Dictionary<int, long>>.Release(this.m_longs);
			this.m_longs = null;
		}
	}

	private void ReleaseStrings()
	{
		if (this.m_strings != null)
		{
			Pool<Dictionary<int, string>>.Release(this.m_strings);
			this.m_strings = null;
		}
	}

	public ZDOID m_uid;

	public bool m_persistent;

	public bool m_distant;

	public long m_owner;

	public long m_timeCreated;

	public uint m_ownerRevision;

	public uint m_dataRevision;

	public int m_pgwVersion;

	public ZDO.ObjectType m_type;

	public float m_tempSortValue;

	public bool m_tempHaveRevision;

	public int m_tempRemoveEarmark = -1;

	public int m_tempCreateEarmark = -1;

	private int m_prefab;

	private Vector2i m_sector = Vector2i.zero;

	private Vector3 m_position = Vector3.zero;

	private Quaternion m_rotation = Quaternion.identity;

	private Dictionary<int, float> m_floats;

	private Dictionary<int, Vector3> m_vec3;

	private Dictionary<int, Quaternion> m_quats;

	private Dictionary<int, int> m_ints;

	private Dictionary<int, long> m_longs;

	private Dictionary<int, string> m_strings;

	private ZDOMan m_zdoMan;

	public enum ObjectType
	{
		Default,
		Prioritized,
		Solid
	}
}
