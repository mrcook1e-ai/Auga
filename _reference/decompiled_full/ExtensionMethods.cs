using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class ExtensionMethods
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Swap<T>(this List<T> list, int indexA, int indexB)
	{
		T val = list[indexB];
		T val2 = list[indexA];
		T val3 = (list[indexA] = val);
		val3 = (list[indexB] = val2);
	}
}
