using UnityEngine;

namespace Valheim.UI;

internal static class RadialUtilityExtensions
{
	internal static bool TryGetComponentInParent<T>(this GameObject go, out T result)
	{
		result = go.GetComponentInParent<T>();
		return result != null;
	}
}
