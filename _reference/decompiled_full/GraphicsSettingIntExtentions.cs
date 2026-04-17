using System;

public static class GraphicsSettingIntExtentions
{
	public static bool IsShownToUser(this GraphicsSettingInt setting)
	{
		return true;
	}

	public static bool IsPresentSetting(this GraphicsSettingInt setting)
	{
		if (setting == GraphicsSettingInt.FpsLimit)
		{
			return true;
		}
		return false;
	}

	public static string ToDisplayName(this GraphicsSettingInt obj)
	{
		return obj switch
		{
			GraphicsSettingInt.Target3DResolutionVertical => Localization.instance.Localize("$settings_3dresolutionlimit"), 
			GraphicsSettingInt.UpscalingAlgorithm => Localization.instance.Localize("$settings_upscalingalgorithm"), 
			GraphicsSettingInt.FpsLimit => Localization.instance.Localize("$settings_fpslimit"), 
			GraphicsSettingInt.Vegetation => Localization.instance.Localize("$settings_vegetation"), 
			GraphicsSettingInt.LOD => Localization.instance.Localize("$settings_lod"), 
			GraphicsSettingInt.Lights => Localization.instance.Localize("$settings_lights"), 
			GraphicsSettingInt.ShadowQuality => Localization.instance.Localize("$settings_shadowquality"), 
			GraphicsSettingInt.PointLights => Localization.instance.Localize("$settings_pointlights"), 
			GraphicsSettingInt.PointLightShadows => Localization.instance.Localize("$settings_pointlightshadows"), 
			GraphicsSettingInt.SSAO => Localization.instance.Localize("$settings_ssao"), 
			_ => EnumUtils.GetDisplayName(obj).ToString(), 
		};
	}

	public static RangeIntInclusive GetRange(this GraphicsSettingInt obj)
	{
		switch (obj)
		{
		case GraphicsSettingInt.Target3DResolutionVertical:
			return RangeIntInclusive.Positive;
		case GraphicsSettingInt.UpscalingAlgorithm:
		{
			EnumUtils.GetRange<UpscalingAlgorithm>(out var isContiguous, out var minValueInclusive, out var maxValueInclusive);
			if (!isContiguous)
			{
				ZLog.LogError($"Range of {typeof(UpscalingAlgorithm)} was not contiguous!");
			}
			return new RangeIntInclusive(minValueInclusive, maxValueInclusive);
		}
		case GraphicsSettingInt.FpsLimit:
			return new RangeIntInclusive(30, 361);
		case GraphicsSettingInt.Vegetation:
			return new RangeIntInclusive(1, 3);
		case GraphicsSettingInt.Lights:
		case GraphicsSettingInt.ShadowQuality:
			return new RangeIntInclusive(0, 2);
		case GraphicsSettingInt.LOD:
		case GraphicsSettingInt.PointLights:
		case GraphicsSettingInt.PointLightShadows:
			return new RangeIntInclusive(0, 3);
		case GraphicsSettingInt.SSAO:
			return new RangeIntInclusive(0, 2);
		default:
			throw new NotImplementedException($"{obj} {(int)obj}");
		}
	}
}
