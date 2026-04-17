using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GraphicsModeConfiguration
{
	[SerializeField]
	[Tooltip("The graphics mode.")]
	private GraphicsMode m_mode;

	[SerializeField]
	[Tooltip("Available presets in this graphics mode.")]
	private List<GraphicsSettingsPreset> m_presets = new List<GraphicsSettingsPreset>();

	[SerializeField]
	[Tooltip("The default preset used for this graphics mode.")]
	private int m_defaultPresetIndex = -1;

	[SerializeField]
	[Tooltip("Whether this configuration has a separate preset in which all graphics settings can be customized.")]
	private bool m_hasCustomPreset = true;

	[SerializeField]
	[Tooltip("Integer quality settings that can be customized without changing the preset in this graphics mode. If this mode configuration doesn't have custom preset enabled, all settings not specified here are hidden.")]
	private GraphicsSettingInt m_customizableQualitySettingsInt;

	[SerializeField]
	[Tooltip("Bool quality settings that can be customized without changing the preset in this graphics mode. If this mode configuration doesn't have custom preset enabled, all settings not specified here are hidden.")]
	private GraphicsSettingBool m_customizableQualitySettingsBool;

	public GraphicsMode Mode => m_mode;

	public IReadOnlyList<GraphicsSettingsPreset> Presets => m_presets;

	public int DefaultPresetIndex => m_defaultPresetIndex;

	public GraphicsSettingsPreset DefaultPreset => m_presets[m_defaultPresetIndex];

	public bool HasCustomPreset => m_hasCustomPreset;

	public bool CanCustomizeGraphicsSetting(GraphicsSettingInt setting)
	{
		return m_customizableQualitySettingsInt.HasFlag(setting);
	}

	public bool CanCustomizeGraphicsSetting(GraphicsSettingBool setting)
	{
		return m_customizableQualitySettingsBool.HasFlag(setting);
	}

	public bool TryFindPresetIndexByID(int id, out int presetIndex)
	{
		for (int i = 0; i < m_presets.Count; i++)
		{
			GraphicsSettingsPreset graphicsSettingsPreset = m_presets[i];
			if ((object)graphicsSettingsPreset.m_type != null && graphicsSettingsPreset.m_type.ID == id)
			{
				presetIndex = i;
				return true;
			}
		}
		presetIndex = -1;
		return false;
	}

	public bool TryFindPresetByID(int id, out GraphicsSettingsPreset preset)
	{
		int presetIndex;
		bool flag = TryFindPresetIndexByID(id, out presetIndex);
		preset = (flag ? m_presets[presetIndex] : null);
		return flag;
	}

	public int GetPresetIndexByID(int id)
	{
		if (id == 100)
		{
			return -1;
		}
		if (TryFindPresetIndexByID(id, out var presetIndex))
		{
			return presetIndex;
		}
		if (HasCustomPreset)
		{
			return -1;
		}
		return DefaultPresetIndex;
	}

	public int GetPresetIDByIndex(int index)
	{
		if (index < 0 || index >= Presets.Count)
		{
			return 100;
		}
		return Presets[index].m_type.ID;
	}

	public GraphicsSettingsPreset GetPresetByID(int id)
	{
		int presetIndexByID = GetPresetIndexByID(id);
		if (presetIndexByID >= 0)
		{
			return Presets[presetIndexByID];
		}
		return null;
	}
}
