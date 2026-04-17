namespace Valheim.UI;

public static class RadialData
{
	public static RadialDataSO SO;

	public static void Init(RadialDataSO dataSO)
	{
		SO = dataSO;
		SO.EnableToggleAnimation = PlatformPrefs.GetInt("RadialAnimateRadial", 1) != 0;
		SO.EnableDoubleClick = PlatformPrefs.GetInt("RadialDoubleTap") != 0;
		SO.EnableFlick = PlatformPrefs.GetInt("RadialFlick") != 0;
		SO.HoverSelectSelectionSpeed = (HoverSelectSpeedSetting)PlatformPrefs.GetInt("RadialHoverSpd");
		SO.RadialSize = (RadialSizeSetting)PlatformPrefs.GetInt("RadialSize");
		SO.SpiralEffectInsensity = (SpiralEffectIntensitySetting)PlatformPrefs.GetInt("RadialSpiral", 2);
	}
}
