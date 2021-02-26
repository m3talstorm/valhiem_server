using System;
using System.Collections.Generic;
using UnityEngine;

public class ZDOPool
{
	public static ZDO Create(ZDOMan man, ZDOID id, Vector3 position)
	{
		ZDO zdo = ZDOPool.Get();
		zdo.Initialize(man, id, position);
		return zdo;
	}

	public static ZDO Create(ZDOMan man)
	{
		ZDO zdo = ZDOPool.Get();
		zdo.Initialize(man);
		return zdo;
	}

	public static void Release(Dictionary<ZDOID, ZDO> objects)
	{
		foreach (ZDO zdo in objects.Values)
		{
			ZDOPool.Release(zdo);
		}
	}

	public static void Release(ZDO zdo)
	{
		zdo.Reset();
		ZDOPool.m_free.Push(zdo);
		ZDOPool.m_active--;
	}

	private static ZDO Get()
	{
		if (ZDOPool.m_free.Count <= 0)
		{
			for (int i = 0; i < ZDOPool.BATCH_SIZE; i++)
			{
				ZDO item = new ZDO();
				ZDOPool.m_free.Push(item);
			}
		}
		ZDOPool.m_active++;
		return ZDOPool.m_free.Pop();
	}

	public static int GetPoolSize()
	{
		return ZDOPool.m_free.Count;
	}

	public static int GetPoolActive()
	{
		return ZDOPool.m_active;
	}

	public static int GetPoolTotal()
	{
		return ZDOPool.m_active + ZDOPool.m_free.Count;
	}

	private static int BATCH_SIZE = 64;

	private static Stack<ZDO> m_free = new Stack<ZDO>();

	private static int m_active = 0;
}
