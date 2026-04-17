using System;
using System.Collections.Generic;
using System.Linq;
using GUIFramework;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Valheim.UI;

namespace Valheim.SettingsGui;

public class RadialSettings : MonoBehaviour, ISettingsTab
{
	[Header("Radial")]
	[SerializeField]
	private Button m_back;

	[SerializeField]
	private GuiDropdown m_radialSize;

	[SerializeField]
	private GuiDropdown m_hoverSelect;

	[SerializeField]
	private Toggle m_persistentBackBtn;

	[SerializeField]
	private Toggle m_animateRadial;

	[SerializeField]
	private GuiDropdown m_spiralEffect;

	[SerializeField]
	private Toggle m_doubleTap;

	[SerializeField]
	private Toggle m_flick;

	[FormerlySerializedAs("m_singleUse")]
	[SerializeField]
	private Toggle m_releaseToUse;

	private Dictionary<HoverSelectSpeedSetting, string> m_hoverSpeedOptionStrings = new Dictionary<HoverSelectSpeedSetting, string>
	{
		{
			HoverSelectSpeedSetting.Off,
			"$radial_speed_off"
		},
		{
			HoverSelectSpeedSetting.Slow,
			"$radial_speed_slow"
		},
		{
			HoverSelectSpeedSetting.Medium,
			"$radial_speed_medium"
		},
		{
			HoverSelectSpeedSetting.Fast,
			"$radial_speed_fast"
		}
	};

	private Dictionary<SpiralEffectIntensitySetting, string> m_sprialOptionStrings = new Dictionary<SpiralEffectIntensitySetting, string>
	{
		{
			SpiralEffectIntensitySetting.Off,
			"$settings_spiral_off"
		},
		{
			SpiralEffectIntensitySetting.Slight,
			"$settings_spiral_slight"
		},
		{
			SpiralEffectIntensitySetting.Normal,
			"$settings_spiral_normal"
		}
	};

	private Dictionary<RadialSizeSetting, string> m_radialSizeOptionStrings = new Dictionary<RadialSizeSetting, string>
	{
		{
			RadialSizeSetting.Big,
			"$settings_radial_big"
		},
		{
			RadialSizeSetting.Small,
			"$settings_radial_small"
		},
		{
			RadialSizeSetting.SmallEdge,
			"$settings_radial_small_edge"
		}
	};

	public event Action<string, int> SharedSettingChanged;

	public void OnTabOpen(Button backButton, Button okButton)
	{
		GuiUtils.SetNavigationUp(backButton, m_releaseToUse);
		GuiUtils.SetNavigationDown(m_releaseToUse, backButton);
		GuiUtils.SetNavigationUp(okButton, m_releaseToUse);
	}

	public void Initialize()
	{
		List<string> list = m_hoverSpeedOptionStrings.Values.ToList();
		int value = list.IndexOf(m_hoverSpeedOptionStrings[(HoverSelectSpeedSetting)PlatformPrefs.GetInt("RadialHoverSpd")]);
		foreach (string item in list.ToList())
		{
			list[list.IndexOf(item)] = Localization.instance.Localize(item);
		}
		m_hoverSelect.ClearOptions();
		m_hoverSelect.AddOptions(list);
		m_hoverSelect.value = value;
		list = m_radialSizeOptionStrings.Values.ToList();
		value = list.IndexOf(m_radialSizeOptionStrings[(RadialSizeSetting)PlatformPrefs.GetInt("RadialSize")]);
		foreach (string item2 in list.ToList())
		{
			list[list.IndexOf(item2)] = Localization.instance.Localize(item2);
		}
		m_radialSize.ClearOptions();
		m_radialSize.AddOptions(list);
		m_radialSize.value = value;
		list = m_sprialOptionStrings.Values.ToList();
		value = list.IndexOf(m_sprialOptionStrings[(SpiralEffectIntensitySetting)PlatformPrefs.GetInt("RadialSpiral", 2)]);
		foreach (string item3 in list.ToList())
		{
			list[list.IndexOf(item3)] = Localization.instance.Localize(item3);
		}
		m_spiralEffect.ClearOptions();
		m_spiralEffect.AddOptions(list);
		m_spiralEffect.value = value;
		m_animateRadial.isOn = PlatformPrefs.GetInt("RadialAnimateRadial", 1) != 0;
		m_doubleTap.isOn = PlatformPrefs.GetInt("RadialDoubleTap") != 0;
		m_flick.isOn = PlatformPrefs.GetInt("RadialFlick") != 0;
		m_flick.onValueChanged.RemoveListener(OnFlickUpdated);
		m_flick.onValueChanged.AddListener(OnFlickUpdated);
		m_doubleTap.onValueChanged.RemoveListener(OnDoubleTapUpdated);
		m_doubleTap.onValueChanged.AddListener(OnDoubleTapUpdated);
		OnDoubleTapUpdated(m_doubleTap.isOn);
	}

	public void OnOkAsync(OkActionCompletedHandler okActionCompletedCallback)
	{
		PlatformPrefs.SetInt("RadialAnimateRadial", m_animateRadial.isOn ? 1 : 0);
		PlatformPrefs.SetInt("RadialDoubleTap", m_doubleTap.isOn ? 1 : 0);
		PlatformPrefs.SetInt("RadialFlick", m_flick.isOn ? 1 : 0);
		PlatformPrefs.SetInt("RadialHoverSpd", m_hoverSelect.value);
		PlatformPrefs.SetInt("RadialSpiral", m_spiralEffect.value);
		PlatformPrefs.SetInt("RadialSize", m_radialSize.value);
		RadialData.SO.EnableToggleAnimation = m_animateRadial.isOn;
		RadialData.SO.NudgeSelectedElement = m_animateRadial.isOn;
		RadialData.SO.EnableDoubleClick = m_doubleTap.isOn;
		RadialData.SO.EnableFlick = m_flick.isOn;
		RadialData.SO.SpiralEffectInsensity = (SpiralEffectIntensitySetting)m_spiralEffect.value;
		RadialData.SO.HoverSelectSelectionSpeed = (HoverSelectSpeedSetting)m_hoverSelect.value;
		RadialData.SO.RadialSize = (RadialSizeSetting)m_radialSize.value;
		okActionCompletedCallback?.Invoke();
	}

	private void OnFlickUpdated(bool value)
	{
		if (value && m_doubleTap.isOn)
		{
			m_doubleTap.isOn = false;
		}
		if (value && m_doubleTap.isOn)
		{
			m_doubleTap.isOn = false;
		}
	}

	private void OnDoubleTapUpdated(bool value)
	{
		if (value && m_flick.isOn)
		{
			m_flick.isOn = false;
		}
	}
}
