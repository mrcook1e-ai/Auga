using System;
using TMPro;
using UnityEngine.UI;

public struct QualitySliderData(GraphicsSettingInt setting, Slider slider, TMP_Text valueText)
{
	public GraphicsSettingInt m_setting = setting;

	public Slider m_slider = slider ?? throw new ArgumentNullException("slider");

	public TMP_Text m_valueText = valueText ?? throw new ArgumentNullException("valueText");
}
