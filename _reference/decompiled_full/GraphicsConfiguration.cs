using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "New graphics configuration", menuName = "Graphics configuration")]
public class GraphicsConfiguration : ScriptableObject
{
	[SerializeField]
	[Tooltip("If this is true, only presets whose frame rate is evenly divisible by the display refresh rate can be selected.")]
	private bool m_presetFrameRateTargetMustMatchDisplay = true;

	[SerializeField]
	[Tooltip("The various set of presets available in this platform configuration.")]
	private List<GraphicsModeConfiguration> m_presetSets = new List<GraphicsModeConfiguration>();

	public bool PresetFrameRateTargetMustMatchDisplay => m_presetFrameRateTargetMustMatchDisplay;

	public IReadOnlyList<GraphicsModeConfiguration> PresetSets => m_presetSets;

	public GraphicsModeConfiguration GetGraphicsModeConfiguration(GraphicsMode type)
	{
		for (int i = 0; i < m_presetSets.Count; i++)
		{
			if (type.Equals(m_presetSets[i].Mode))
			{
				return m_presetSets[i];
			}
		}
		return null;
	}
}
