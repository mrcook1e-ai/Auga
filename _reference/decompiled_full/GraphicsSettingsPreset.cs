using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New graphics settings preset", menuName = "Graphics settings preset")]
public class GraphicsSettingsPreset : ScriptableObject
{
	[SerializeField]
	public GraphicsSettingsPresetType m_type;

	[SerializeField]
	private GraphicsSettings2 m_graphicsSettings = new GraphicsSettings2();

	public IReadOnlyGraphicsSettings GraphicsSettings => m_graphicsSettings;

	public void SetQualitySetting(GraphicsSettingInt setting, int value)
	{
		m_graphicsSettings.SetQualitySetting(setting, value);
	}

	public void SetQualitySetting(GraphicsSettingBool setting, bool value)
	{
		m_graphicsSettings.SetQualitySetting(setting, value);
	}

	public bool TryGetQualitySetting(GraphicsSettingInt setting, out int value)
	{
		return m_graphicsSettings.TryGetQualitySetting(setting, out value);
	}

	public bool TryGetQualitySetting(GraphicsSettingInt setting, out uint value)
	{
		int value2;
		bool result = m_graphicsSettings.TryGetQualitySetting(setting, out value2);
		if (value2 < 0)
		{
			throw new InvalidOperationException("Tried to retrieve a signed int and store it in an unsigned int!");
		}
		value = (uint)value2;
		return result;
	}

	public bool TryGetQualitySetting(GraphicsSettingBool setting, out bool value)
	{
		return m_graphicsSettings.TryGetQualitySetting(setting, out value);
	}

	internal void OnValidate()
	{
		try
		{
			m_graphicsSettings.OnValidate();
		}
		catch (Exception arg)
		{
			ZLog.LogError($"Preset {base.name}: {arg}");
		}
	}
}
