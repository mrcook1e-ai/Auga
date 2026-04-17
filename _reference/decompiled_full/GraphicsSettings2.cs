using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GraphicsSettings2 : IReadOnlyGraphicsSettings
{
	[SerializeField]
	private List<GraphicsSettingStateInt> m_graphicsSettingsInt = new List<GraphicsSettingStateInt>();

	[SerializeField]
	private List<GraphicsSettingStateBool> m_graphicsSettingsBool = new List<GraphicsSettingStateBool>();

	public IReadOnlyList<GraphicsSettingStateInt> GraphicsSettingsStatesInt => m_graphicsSettingsInt;

	public IReadOnlyList<GraphicsSettingStateBool> GraphicsSettingsStatesBool => m_graphicsSettingsBool;

	public void SetQualitySetting(GraphicsSettingInt setting, int value)
	{
		GraphicsSettingStateInt graphicsSettingStateInt = new GraphicsSettingStateInt(setting, value);
		for (int i = 0; i < m_graphicsSettingsInt.Count; i++)
		{
			if (m_graphicsSettingsInt[i].m_setting == setting)
			{
				m_graphicsSettingsInt[i] = graphicsSettingStateInt;
				return;
			}
		}
		m_graphicsSettingsInt.Add(graphicsSettingStateInt);
	}

	public void SetQualitySetting(GraphicsSettingBool setting, bool value)
	{
		GraphicsSettingStateBool graphicsSettingStateBool = new GraphicsSettingStateBool(setting, value);
		for (int i = 0; i < m_graphicsSettingsBool.Count; i++)
		{
			if (m_graphicsSettingsBool[i].m_setting == setting)
			{
				m_graphicsSettingsBool[i] = graphicsSettingStateBool;
				return;
			}
		}
		m_graphicsSettingsBool.Add(graphicsSettingStateBool);
	}

	public bool TryGetQualitySetting(GraphicsSettingInt setting, out int value)
	{
		for (int i = 0; i < m_graphicsSettingsInt.Count; i++)
		{
			if (m_graphicsSettingsInt[i].m_setting == setting)
			{
				value = m_graphicsSettingsInt[i].m_value;
				return true;
			}
		}
		value = 0;
		return false;
	}

	public bool TryGetQualitySetting(GraphicsSettingBool setting, out bool value)
	{
		for (int i = 0; i < m_graphicsSettingsBool.Count; i++)
		{
			if (m_graphicsSettingsBool[i].m_setting == setting)
			{
				value = m_graphicsSettingsBool[i].m_value;
				return true;
			}
		}
		value = false;
		return false;
	}

	internal void OnValidate()
	{
		for (int i = 0; i < m_graphicsSettingsInt.Count; i++)
		{
			GraphicsSettingStateInt value = m_graphicsSettingsInt[i];
			if (value.m_setting == GraphicsSettingInt.None)
			{
				continue;
			}
			bool flag = false;
			for (int j = 0; j < 32; j++)
			{
				if (1 << j == (int)value.m_setting)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				value.m_setting = GraphicsSettingInt.None;
				m_graphicsSettingsInt[i] = value;
				continue;
			}
			RangeIntInclusive range = value.m_setting.GetRange();
			if (value.m_value < range.m_minValue || value.m_value > range.m_maxValue)
			{
				value.m_value = Mathf.Clamp(value.m_value, range.m_minValue, range.m_maxValue);
				m_graphicsSettingsInt[i] = value;
			}
		}
		for (int k = 0; k < m_graphicsSettingsBool.Count; k++)
		{
			GraphicsSettingStateBool value2 = m_graphicsSettingsBool[k];
			if (value2.m_setting == GraphicsSettingBool.None)
			{
				continue;
			}
			bool flag2 = false;
			for (int l = 0; l < 32; l++)
			{
				if (1 << l == (int)value2.m_setting)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				value2.m_setting = GraphicsSettingBool.None;
				m_graphicsSettingsBool[k] = value2;
			}
		}
	}
}
