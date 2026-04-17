public static class UpscalingAlgorithmExtentions
{
	public static string ToDisplayName(this UpscalingAlgorithm _enum)
	{
		return _enum switch
		{
			UpscalingAlgorithm.Bilinear => Localization.instance.Localize("$settings_bilinear"), 
			UpscalingAlgorithm.NearestNeighbor => Localization.instance.Localize("$settings_nearestneighbor"), 
			_ => EnumUtils.GetDisplayName(_enum).ToString(), 
		};
	}
}
