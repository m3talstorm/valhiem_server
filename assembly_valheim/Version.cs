using System;
using UnityEngine;

internal class Version
{
	public static string GetVersionString()
	{
		return global::Version.CombineVersion(global::Version.m_major, global::Version.m_minor, global::Version.m_patch);
	}

	public static bool IsVersionNewer(int major, int minor, int patch)
	{
		if (major > global::Version.m_major)
		{
			return true;
		}
		if (major == global::Version.m_major && minor > global::Version.m_minor)
		{
			return true;
		}
		if (major != global::Version.m_major || minor != global::Version.m_minor)
		{
			return false;
		}
		if (global::Version.m_patch >= 0)
		{
			return patch > global::Version.m_patch;
		}
		return patch >= 0 || patch < global::Version.m_patch;
	}

	public static string CombineVersion(int major, int minor, int patch)
	{
		if (patch == 0)
		{
			return major.ToString() + "." + minor.ToString();
		}
		if (patch < 0)
		{
			return string.Concat(new string[]
			{
				major.ToString(),
				".",
				minor.ToString(),
				".rc",
				Mathf.Abs(patch).ToString()
			});
		}
		return string.Concat(new string[]
		{
			major.ToString(),
			".",
			minor.ToString(),
			".",
			patch.ToString()
		});
	}

	public static bool IsWorldVersionCompatible(int version)
	{
		if (version == global::Version.m_worldVersion)
		{
			return true;
		}
		foreach (int num in global::Version.m_compatibleWorldVersions)
		{
			if (version == num)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsPlayerVersionCompatible(int version)
	{
		if (version == global::Version.m_playerVersion)
		{
			return true;
		}
		foreach (int num in global::Version.m_compatiblePlayerVersions)
		{
			if (version == num)
			{
				return true;
			}
		}
		return false;
	}

	public static int m_major = 0;

	public static int m_minor = 143;

	public static int m_patch = 5;

	public static int m_playerVersion = 32;

	public static int[] m_compatiblePlayerVersions = new int[]
	{
		31,
		30,
		29,
		28,
		27
	};

	public static int m_worldVersion = 26;

	public static int[] m_compatibleWorldVersions = new int[]
	{
		25,
		24,
		23,
		22,
		21,
		20,
		19,
		18,
		17,
		16,
		15,
		14,
		13,
		11,
		10,
		9
	};

	public static int m_worldGenVersion = 1;
}
