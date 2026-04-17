using System;
using UnityEngine;

namespace Valheim.UI;

public static class UIMath
{
	public static Quaternion DirectionToRotation(Vector2 dir)
	{
		return Quaternion.Euler(0f, 0f, 0f - DirectionToAngleDegrees(dir));
	}

	public static float DirectionToAngleDegrees(Vector2 direction, bool positiveRange = true)
	{
		if (!positiveRange)
		{
			return DirectionToAngle(direction) * 57.29578f;
		}
		return Mod(DirectionToAngle(direction) * 57.29578f, 360f);
	}

	public static float DirectionToAngle(Vector2 direction)
	{
		return Mathf.Atan2(direction.normalized.x, direction.normalized.y);
	}

	public static Quaternion AngleToRotation(float angle)
	{
		if (!(angle > 360f))
		{
			return Quaternion.AngleAxis(0f - angle, Vector3.forward);
		}
		return Quaternion.AngleAxis(0f - Mod(angle, 360f), Vector3.forward);
	}

	public static Vector2 AngleToDirection(float angle)
	{
		return new Vector2((float)Mathf.RoundToInt(Mathf.Sin(angle * (MathF.PI / 180f)) * 100f) / 100f, (float)Mathf.RoundToInt(Mathf.Cos(angle * (MathF.PI / 180f)) * 100f) / 100f);
	}

	public static float AngleToPos(float angle, int layer)
	{
		if (!Mathf.Approximately(angle, 360f))
		{
			return 360f * (float)Mathf.Max(layer, 0) + angle;
		}
		return 360f * (float)Mathf.Max(layer, 0);
	}

	public static int AngleToRadialPoint(float angle, float segmentSize)
	{
		int num = (int)Math.Round(Mod(angle, 360f) / segmentSize, MidpointRounding.AwayFromZero);
		if (!Mathf.Approximately(num, 360f / segmentSize))
		{
			return num;
		}
		return 0;
	}

	public static int RadialDirection(Vector2 from, Vector2 to)
	{
		if (from == Vector2.zero || to == Vector2.zero)
		{
			return 0;
		}
		if (!(from.x * to.y - from.y * from.x < 0f))
		{
			return -1;
		}
		return 1;
	}

	public static float Mod(float dividend, float divisor)
	{
		float num = dividend % divisor;
		if (!(num < 0f))
		{
			return num;
		}
		return num + Mathf.Abs(divisor);
	}

	public static float RadialDelta(float p1, float p2)
	{
		float num = Mathf.Abs(Mod(p1, 360f) - Mod(p2, 360f));
		if (!(num > 180f))
		{
			return num;
		}
		return 360f - num;
	}

	public static float ClosestSegment(float value, float segmentSize)
	{
		return Mathf.Round(value / segmentSize) * segmentSize;
	}
}
