using System;
using SoftReferenceableAssets;
using UnityEngine;

[Serializable]
public struct GraphicsConfigurationForPlatform
{
	[SerializeField]
	[Tooltip("The hardware utilizing this graphics configuration")]
	public string[] m_platformHardware;

	[SerializeField]
	[Tooltip("The graphics configuration utilized by this hardware")]
	public SoftReference<GraphicsConfiguration> m_configuration;
}
