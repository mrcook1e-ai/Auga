using System;
using UnityEngine;

[Serializable]
public struct GraphicsSettingStateBool
{
	[SerializeField]
	[SingleEnum(typeof(GraphicsSettingBool))]
	public GraphicsSettingBool m_setting;

	[SerializeField]
	public bool m_value;

	public GraphicsSettingStateBool(GraphicsSettingBool setting)
	{
		m_setting = setting;
		m_value = false;
	}

	public GraphicsSettingStateBool(GraphicsSettingBool setting, bool value)
	{
		m_setting = setting;
		m_value = value;
	}
}
