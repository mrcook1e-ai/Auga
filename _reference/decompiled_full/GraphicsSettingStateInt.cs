using System;
using UnityEngine;

[Serializable]
public struct GraphicsSettingStateInt
{
	[SerializeField]
	[SingleEnum(typeof(GraphicsSettingInt))]
	public GraphicsSettingInt m_setting;

	[SerializeField]
	public int m_value;

	public GraphicsSettingStateInt(GraphicsSettingInt setting)
	{
		m_setting = setting;
		m_value = 0;
	}

	public GraphicsSettingStateInt(GraphicsSettingInt setting, int value)
	{
		m_setting = setting;
		m_value = value;
	}
}
