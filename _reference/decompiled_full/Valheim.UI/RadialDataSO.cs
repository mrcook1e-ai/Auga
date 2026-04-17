using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Valheim.UI;

[CreateAssetMenu(fileName = "RadialData", menuName = "Valheim/Radial/Radial Data", order = 0)]
public class RadialDataSO : ScriptableObject
{
	[Header("Settings")]
	[Header("----------------------------")]
	[Header("Layout Settings")]
	[InspectorChangedEvent]
	public float ElementInfoRadius;

	public Action OnElementInfoRadiusChanged;

	[InspectorChangedEvent]
	public float CursorDistance;

	public Action OnCursorDistanceChanged;

	[InspectorChangedEvent]
	public float ElementsDistance;

	public Action OnElementsDistanceChanged;

	[InspectorChangedEvent]
	public float RadialReferenceWidth;

	public Action OnRadialReferenceWidthChanged;

	[InspectorChangedEvent]
	public float RadialBigScale;

	public Action OnRadialBigScaleChanged;

	[InspectorChangedEvent]
	public float RadialSmallScale;

	public Action OnRadialSmallScaleChanged;

	[InspectorChangedEvent]
	public RadialSizeSetting RadialSize;

	public Action OnRadialSizeChanged;

	[Space(10f)]
	[Header("Fade Settings")]
	public SpiralEffectIntensitySetting SpiralEffectInsensity;

	[FormerlySerializedAs("ElementNudgeFactor")]
	public float NormalElementNudgeFactor;

	[FormerlySerializedAs("ElementScaleFactor")]
	public float NormalElementScaleFactor;

	public float SlightElementNudgeFactor;

	public float SlightElementScaleFactor;

	public float ElementFadeDuration;

	public float ReFadeMultiplier;

	public EasingType ElementFadeEasingType;

	public EasingType ElementScaleEasingType;

	[Space(10f)]
	[Header("Layer Settings")]
	[InspectorChangedEvent]
	public int LayerShowCount;

	public Action OnLayerShowCountChanged;

	[InspectorChangedEvent]
	public int LayerFadeCount;

	public Action OnLayerFadeCountChanged;

	public int[] MaxElementsRange = new int[2] { 8, 12 };

	[Space(10f)]
	[Header("Cursor Settings")]
	[Range(0f, 1f)]
	public float CursorSensitivity;

	public float CursorSpeed;

	public EasingType CursorEasingType;

	[FormerlySerializedAs("HoverSelectSelectedSpeed")]
	[Space(10f)]
	[Header("Interaction Settings")]
	public HoverSelectSpeedSetting HoverSelectSelectionSpeed;

	public float HoverSelectSlow;

	public float HoverSelectMedium;

	public float HoverSelectFast;

	[ConditionalHide("EnableHoverSelect", false)]
	public EasingType HoverSelectEasingType;

	public float InteractionDelay;

	public float HoldCloseDelay;

	[Space(10f)]
	[Header("Experimental Features")]
	[InspectorChangedEvent]
	public bool UsePersistantBackBtn;

	public Action OnUsePersistantBackBtnChanged;

	[Space(5f)]
	public bool DefaultToBackButtonOnNewPage;

	[Space(5f)]
	public bool ReSizeOnRefresh;

	[Space(5f)]
	public bool ReFadeAtMidnight;

	[FormerlySerializedAs("EnableSingleUseMode")]
	[Space(5f)]
	public bool EnableReleaseToUseMode;

	[Space(5f)]
	public bool AllowSingleItemHoverMenu;

	[Space(5f)]
	public bool OpenNormalRadialWhenHoverMenuFails;

	[Space(5f)]
	public bool EnableDoubleClick;

	[ConditionalHide("EnableDoubleClick", false)]
	public float DoubleClickTime;

	[ConditionalHide("EnableDoubleClick", false)]
	public float DoubleClickDelay;

	[ConditionalHide("EnableDoubleClick", false)]
	public bool RequireReleaseOnFinalClick;

	[Space(5f)]
	public bool EnableFlick;

	[ConditionalHide("EnableFlick", false)]
	public float FlickTime;

	[Space(5f)]
	[InspectorChangedEvent]
	public bool EnableSelectedOrnament;

	public Action OnEnableSelectedOrnamentChanged;

	[ConditionalHide("EnableSelectedOrnament", false)]
	[InspectorChangedEvent]
	public float OrnamentOffset;

	public Action OnOrnamentOffsetChanged;

	[Space(5f)]
	[InspectorChangedEvent]
	public bool NudgeSelectedElement;

	public Action OnNudgeSelectedElementChanged;

	[ConditionalHide("NudgeSelectedElement", false)]
	[InspectorChangedEvent]
	public float NudgeDistance;

	public Action OnNudgeDistanceChanged;

	[ConditionalHide("NudgeSelectedElement", false)]
	public float NudgeDuration;

	[ConditionalHide("NudgeSelectedElement", false)]
	public EasingType NudgeEasingType;

	[Space(5f)]
	public bool EnableToggleAnimation;

	[ConditionalHide("EnableToggleAnimation", false)]
	public float ToggleAnimDuration;

	[ConditionalHide("EnableToggleAnimation", false)]
	public float ToggleAnimDistance;

	[ConditionalHide("EnableToggleAnimation", false)]
	public EasingType TogglePosEasingType;

	[ConditionalHide("EnableToggleAnimation", false)]
	public EasingType ToggleAlphaEasingType;

	[Space(20f)]
	[Header("References")]
	[Header("----------------------------")]
	[Header("Element Prefabs")]
	public EmptyElement EmptyElement;

	public BackElement BackElement;

	public EmoteElement EmoteElement;

	public GroupElement GroupElement;

	public ThrowElement ThrowElement;

	public ItemElement ItemElement;

	public HammerItemElement HammerItemElement;

	[Space(10f)]
	[Header("Mappings References")]
	public ItemGroupMappings ItemGroupMappings;

	public EmoteMappings EmoteMappings;

	[Space(10f)]
	[Header("Radial Group Configs")]
	public OpenRadialConfig OpenConfig;

	public ValheimRadialConfig MainGroupConfig;

	public HotbarGroupConfig HotbarGroupConfig;

	public ItemGroupConfig ItemGroupConfig;

	public ThrowGroupConfig ThrowGroupConfig;

	public EmoteGroupConfig EmoteGroupConfig;

	public float ElementNudgeFactor
	{
		get
		{
			SpiralEffectIntensitySetting spiralEffectInsensity = SpiralEffectInsensity;
			switch (spiralEffectInsensity)
			{
			case SpiralEffectIntensitySetting.Off:
				return 0f;
			case SpiralEffectIntensitySetting.Slight:
				return SlightElementNudgeFactor;
			case SpiralEffectIntensitySetting.Normal:
				return NormalElementNudgeFactor;
			default:
			{
				global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException(spiralEffectInsensity);
				float result = default(float);
				return result;
			}
			}
		}
	}

	public float ElementScaleFactor
	{
		get
		{
			SpiralEffectIntensitySetting spiralEffectInsensity = SpiralEffectInsensity;
			switch (spiralEffectInsensity)
			{
			case SpiralEffectIntensitySetting.Off:
				return 1f;
			case SpiralEffectIntensitySetting.Slight:
				return SlightElementScaleFactor;
			case SpiralEffectIntensitySetting.Normal:
				return NormalElementScaleFactor;
			default:
			{
				global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException(spiralEffectInsensity);
				float result = default(float);
				return result;
			}
			}
		}
	}

	public float HoverSelectSpeed => HoverSelectSelectionSpeed switch
	{
		HoverSelectSpeedSetting.Slow => HoverSelectSlow, 
		HoverSelectSpeedSetting.Medium => HoverSelectMedium, 
		HoverSelectSpeedSetting.Fast => HoverSelectFast, 
		HoverSelectSpeedSetting.Off => -1f, 
		_ => -1f, 
	};

	private void OnInspectorChanged(string propertyName)
	{
		switch (propertyName)
		{
		case "ElementInfoRadius":
			OnElementInfoRadiusChanged?.Invoke();
			break;
		case "CursorDistance":
			OnCursorDistanceChanged?.Invoke();
			break;
		case "ElementsDistance":
			OnElementsDistanceChanged?.Invoke();
			break;
		case "LayerShowCount":
			OnLayerShowCountChanged?.Invoke();
			break;
		case "LayerFadeCount":
			OnLayerFadeCountChanged?.Invoke();
			break;
		case "UsePersistantBackBtn":
			OnUsePersistantBackBtnChanged?.Invoke();
			break;
		case "EnableSelectedOrnament":
			OnEnableSelectedOrnamentChanged?.Invoke();
			break;
		case "OrnamentOffset":
			OnOrnamentOffsetChanged?.Invoke();
			break;
		case "NudgeSelectedElement":
			OnNudgeSelectedElementChanged?.Invoke();
			break;
		case "NudgeDistance":
			OnNudgeDistanceChanged?.Invoke();
			break;
		case "RadialReferenceWidth":
			OnRadialReferenceWidthChanged?.Invoke();
			break;
		case "RadialBigScale":
			OnRadialBigScaleChanged?.Invoke();
			break;
		case "RadialSmallScale":
			OnRadialSmallScaleChanged?.Invoke();
			break;
		case "RadialSize":
			OnRadialSizeChanged?.Invoke();
			break;
		default:
			Debug.Log("No variable with name " + propertyName + " found.");
			break;
		}
	}
}
