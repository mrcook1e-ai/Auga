using System;
using UnityEngine;

namespace Valheim.UI;

internal static class EasingFunctions
{
	private const float c1 = 1.70158f;

	private const float c2 = 2.5949094f;

	private const float c3 = 2.70158f;

	internal static Func<float, float> GetFunc(EasingType type)
	{
		return type switch
		{
			EasingType.Linear => null, 
			EasingType.SineIn => SineIn, 
			EasingType.SineOut => SineOut, 
			EasingType.SineInOut => SineInOut, 
			EasingType.QuadIn => QuadIn, 
			EasingType.QuadOut => QuadOut, 
			EasingType.QuadInOut => QuadInOut, 
			EasingType.CubicIn => CubicIn, 
			EasingType.CubicOut => CubicOut, 
			EasingType.CubicInOut => CubicInOut, 
			EasingType.QuartIn => QuartIn, 
			EasingType.QuartOut => QuartOut, 
			EasingType.QuartInOut => QuartInOut, 
			EasingType.QuintIn => QuintIn, 
			EasingType.QuintOut => QuintOut, 
			EasingType.QuintInOut => QuintInOut, 
			EasingType.ExpoIn => ExpoIn, 
			EasingType.ExpoOut => ExpoOut, 
			EasingType.ExpoInOut => ExpoInOut, 
			EasingType.CircIn => CircIn, 
			EasingType.CircOut => CircOut, 
			EasingType.CircInOut => CircInOut, 
			EasingType.BackIn => BackIn, 
			EasingType.BackOut => BackOut, 
			EasingType.BackInOut => BackInOut, 
			_ => null, 
		};
	}

	private static float SineIn(float t)
	{
		return 1f - Mathf.Cos(t * MathF.PI / 2f);
	}

	private static float SineOut(float t)
	{
		return Mathf.Cos(t * MathF.PI / 2f);
	}

	private static float SineInOut(float t)
	{
		return (0f - (Mathf.Cos(MathF.PI * t) - 1f)) / 2f;
	}

	private static float QuadIn(float t)
	{
		return t * t;
	}

	private static float QuadOut(float t)
	{
		return 1f - (1f - t) * (1f - t);
	}

	private static float QuadInOut(float t)
	{
		if (!(t < 0.5f))
		{
			return 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
		}
		return 2f * t * t;
	}

	private static float CubicIn(float t)
	{
		return t * t * t;
	}

	private static float CubicOut(float t)
	{
		return 1f - Mathf.Pow(1f - t, 3f);
	}

	private static float CubicInOut(float t)
	{
		if (!(t < 0.5f))
		{
			return 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
		}
		return 4f * t * t * t;
	}

	private static float QuartIn(float t)
	{
		return t * t * t * t;
	}

	private static float QuartOut(float t)
	{
		return 1f - Mathf.Pow(1f - t, 4f);
	}

	private static float QuartInOut(float t)
	{
		if (!(t < 0.5f))
		{
			return 1f - Mathf.Pow(-2f * t + 2f, 4f) / 2f;
		}
		return 8f * t * t * t * t;
	}

	private static float QuintIn(float t)
	{
		return t * t * t * t * t;
	}

	private static float QuintOut(float t)
	{
		return 1f - Mathf.Pow(1f - t, 5f);
	}

	private static float QuintInOut(float t)
	{
		if (!(t < 0.5f))
		{
			return 1f - Mathf.Pow(-2f * t + 2f, 5f) / 2f;
		}
		return 16f * t * t * t * t * t;
	}

	private static float ExpoIn(float t)
	{
		if (!Mathf.Approximately(t, 0f))
		{
			return Mathf.Pow(2f, 10f * t - 10f);
		}
		return 0f;
	}

	private static float ExpoOut(float t)
	{
		if (!Mathf.Approximately(t, 1f))
		{
			return Mathf.Pow(2f, -10f * t);
		}
		return 1f;
	}

	private static float ExpoInOut(float t)
	{
		if (!Mathf.Approximately(t, 0f))
		{
			if (!Mathf.Approximately(t, 1f))
			{
				if (!(t < 0.5f))
				{
					return (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
				}
				return Mathf.Pow(2f, 20f * t - 10f) / 2f;
			}
			return 1f;
		}
		return 0f;
	}

	private static float CircIn(float t)
	{
		return 1f - Mathf.Sqrt(1f - Mathf.Pow(t, 2f));
	}

	private static float CircOut(float t)
	{
		return Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f));
	}

	private static float CircInOut(float t)
	{
		if (!(t < 0.5f))
		{
			return (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) / 2f;
		}
		return (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) / 2f;
	}

	private static float BackIn(float t)
	{
		return 2.70158f * t * t * t - 1.70158f * t * t;
	}

	private static float BackOut(float t)
	{
		return 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 2.70158f * Mathf.Pow(t - 1f, 2f);
	}

	private static float BackInOut(float t)
	{
		if (!(t < 0.5f))
		{
			return (Mathf.Pow(2f * t - 2f, 2f) * (3.5949094f * (t * 2f - 2f) + 2.5949094f) + 2f) / 2f;
		}
		return Mathf.Pow(2f * t, 2f) * (7.189819f * t - 2.5949094f) / 2f;
	}
}
