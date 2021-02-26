using System;
using System.Collections.Generic;

internal static class ShuffleClass
{
	public static void Shuffle<T>(this IList<T> list)
	{
		int i = list.Count;
		while (i > 1)
		{
			i--;
			int index = ShuffleClass.rng.Next(i + 1);
			T value = list[index];
			list[index] = list[i];
			list[i] = value;
		}
	}

	private static Random rng = new Random();
}
