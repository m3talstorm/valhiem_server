using System;
using System.Collections.Generic;

internal class ZDOComparer : IEqualityComparer<ZDO>
{
	public bool Equals(ZDO a, ZDO b)
	{
		return a == b;
	}

	public int GetHashCode(ZDO a)
	{
		return a.GetHashCode();
	}
}
