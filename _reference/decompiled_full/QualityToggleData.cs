using System;
using UnityEngine.UI;

public struct QualityToggleData(GraphicsSettingBool setting, Toggle toggle)
{
	public GraphicsSettingBool m_setting = setting;

	public Toggle m_toggle = toggle ?? throw new ArgumentNullException("toggle");
}
