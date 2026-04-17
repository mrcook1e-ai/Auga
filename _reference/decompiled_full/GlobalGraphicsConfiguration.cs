using System;
using System.Collections.Generic;
using SoftReferenceableAssets;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "New global graphics configuration", menuName = "Global graphics configuration")]
public class GlobalGraphicsConfiguration : ScriptableObject
{
	[SerializeField]
	[Tooltip("Graphics configurations for platforms")]
	private List<GraphicsConfigurationForPlatform> m_configurationsForPlatforms = new List<GraphicsConfigurationForPlatform>();

	[SerializeField]
	[Tooltip("Graphics settings that should be kept disabled in presets if they are toggled off. Settings that are set in the presets and affect graphics quality but can also be disabled for accessibility reasons should be set here.")]
	[FormerlySerializedAs("m_keepDisabledInPreset")]
	private GraphicsSettingBool m_accessibilitySettings;

	public GraphicsSettingBool AccessibilitySettings => m_accessibilitySettings;

	public SoftReference<GraphicsConfiguration> GetConfigurationForPlatform(string platform)
	{
		for (int i = 0; i < m_configurationsForPlatforms.Count; i++)
		{
			for (int j = 0; j < m_configurationsForPlatforms[i].m_platformHardware.Length; j++)
			{
				if (m_configurationsForPlatforms[i].m_platformHardware[j].Equals(platform, StringComparison.InvariantCultureIgnoreCase))
				{
					return m_configurationsForPlatforms[i].m_configuration;
				}
			}
		}
		ZLog.LogWarning("Couldn't find any graphics configurations for the specified platform!");
		return default(SoftReference<GraphicsConfiguration>);
	}
}
