using UnityEngine;

namespace Valheim.UI;

public static class RadialConfigHelper
{
	public static void SetXYControls(this RadialBase radial)
	{
		radial.GetControllerDirection = () => ZInput.GetValue<Vector2>("RadialStick");
		radial.GetMouseDirection = () => GetMouseDirection(radial);
	}

	private static Vector2 GetMouseDirection(RadialBase radial)
	{
		Vector2 vector = ZInput.mousePosition;
		Vector2 infoPosition = radial.InfoPosition;
		return (vector - infoPosition).normalized;
	}

	public static void SetItemInteractionControls(this RadialBase radial)
	{
		radial.GetConfirm = () => (ZInput.GetButtonLastPressedTimer("JoyRadialInteract") < 0.33f && ZInput.GetButtonUp("JoyRadialInteract")) || ZInput.GetMouseButtonUp(0);
		radial.GetReleaseToUse = delegate
		{
			if (!RadialData.SO.EnableReleaseToUseMode)
			{
				return false;
			}
			return (ZInput.GetButtonLastPressedTimer("JoyRadial") > RadialData.SO.HoldCloseDelay && ZInput.GetButtonUp("JoyRadial")) || (ZInput.GetButtonLastPressedTimer("OpenRadial") > RadialData.SO.HoldCloseDelay && ZInput.GetButtonUp("OpenRadial")) || (ZInput.GetButtonLastPressedTimer("OpenEmote") > RadialData.SO.HoldCloseDelay && ZInput.GetButtonUp("OpenEmote"));
		};
		radial.GetBack = () => ZInput.GetButtonUp("JoyRadialBack") || (ZInput.GetButtonUp("JoyRadialClose") && !radial.IsTopLevel) || (!ZInput.GetKey(KeyCode.LeftShift) && ZInput.GetMouseButtonUp(1));
		radial.GetThrow = () => (ZInput.GetButtonLastPressedTimer("JoyRadialSecondaryInteract") < 0.33f && ZInput.GetButtonUp("JoyRadialSecondaryInteract")) || (ZInput.GetKey(KeyCode.LeftShift) && ZInput.GetButtonLastPressedTimer("RadialSecondaryInteract") < 0.33f && ZInput.GetButtonUp("RadialSecondaryInteract"));
		radial.GetOpenThrowMenu = () => (ZInput.GetButtonPressedTimer("JoyRadialSecondaryInteract") > 0.33f && ZInput.GetButton("JoyRadialSecondaryInteract")) || (ZInput.GetKey(KeyCode.LeftShift) && ZInput.GetButtonPressedTimer("RadialSecondaryInteract") > 0.33f && ZInput.GetButton("RadialSecondaryInteract"));
		radial.GetClose = () => ((ZInput.GetButtonUp("JoyRadialClose") && radial.IsTopLevel) || ZInput.GetButtonDown("JoyRadial") || ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetKeyDown(KeyCode.BackQuote) || ZInput.GetButtonDown("JoyMenu") || ZInput.GetButtonDown("JoyMap") || ZInput.GetButtonDown("Map") || ZInput.GetButtonDown("JoyChat") || ZInput.GetButtonDown("Chat") || ZInput.GetButtonDown("Console") || ZInput.GetButtonDown("OpenEmote")) ? true : false;
		radial.GetFlick = () => ZInput.GetRadialTap();
		radial.GetDoubleTap = () => ZInput.GetRadialMultiTap();
		ZInput.UpdateRadialMultiTap(RadialData.SO.DoubleClickTime, RadialData.SO.DoubleClickDelay, 2, RadialData.SO.RequireReleaseOnFinalClick);
		ZInput.UpdateRadialTapTime(RadialData.SO.FlickTime);
	}
}
