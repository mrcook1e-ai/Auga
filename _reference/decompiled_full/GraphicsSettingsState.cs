using System;

public struct GraphicsSettingsState
{
	public PresentSettingsState m_presentSettings;

	public int m_target3DResolutionVertical;

	public UpscalingAlgorithm m_upscalingAlgorithm;

	public ClutterSystem.Quality m_vegetation;

	public int m_lod;

	public int m_lights;

	public int m_shadowQuality;

	public int m_pointLights;

	public int m_pointLightShadows;

	public int m_ssao;

	public bool m_distantShadows;

	public bool m_tesselation;

	public bool m_bloom;

	public bool m_depthOfField;

	public bool m_motionBlur;

	public bool m_chromaticAberration;

	public bool m_sunShafts;

	public bool m_softParticles;

	public bool m_antiAliasing;

	public bool m_anisotropicTextures;

	public static bool TryCreateFromPreset(GraphicsSettingsPreset preset, out GraphicsSettingsState state)
	{
		state = default(GraphicsSettingsState);
		int value;
		int num = (int)(1u & (preset.TryGetQualitySetting(GraphicsSettingInt.FpsLimit, out state.m_presentSettings.m_fpsLimit) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingBool.Vsync, out state.m_presentSettings.m_vsync) ? 1u : 0u)) & (preset.TryGetQualitySetting(GraphicsSettingInt.Vegetation, out value) ? 1 : 0);
		state.m_vegetation = (ClutterSystem.Quality)value;
		int value2;
		int num2 = (int)((uint)num & (preset.TryGetQualitySetting(GraphicsSettingInt.LOD, out state.m_lod) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingInt.Lights, out state.m_lights) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingInt.ShadowQuality, out state.m_shadowQuality) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingInt.PointLights, out state.m_pointLights) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingInt.PointLightShadows, out state.m_pointLightShadows) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingInt.SSAO, out state.m_ssao) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingInt.Target3DResolutionVertical, out state.m_target3DResolutionVertical) ? 1u : 0u)) & (preset.TryGetQualitySetting(GraphicsSettingInt.UpscalingAlgorithm, out value2) ? 1 : 0);
		state.m_upscalingAlgorithm = (UpscalingAlgorithm)value2;
		int num3 = (int)((uint)num2 & (preset.TryGetQualitySetting(GraphicsSettingBool.DistantShadows, out state.m_distantShadows) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingBool.Tesselation, out state.m_tesselation) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingBool.Bloom, out state.m_bloom) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingBool.DepthOfField, out state.m_depthOfField) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingBool.MotionBlur, out state.m_motionBlur) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingBool.ChromaticAberration, out state.m_chromaticAberration) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingBool.SunShafts, out state.m_sunShafts) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingBool.SoftParticles, out state.m_softParticles) ? 1u : 0u) & (preset.TryGetQualitySetting(GraphicsSettingBool.AntiAliasing, out state.m_antiAliasing) ? 1u : 0u)) & (preset.TryGetQualitySetting(GraphicsSettingBool.AnisotropicTextures, out state.m_anisotropicTextures) ? 1 : 0);
		if (num3 == 0)
		{
			ZLog.LogError($"Preset {preset} is missing settings!");
		}
		return (byte)num3 != 0;
	}

	public int GetValue(GraphicsSettingInt setting)
	{
		return setting switch
		{
			GraphicsSettingInt.FpsLimit => m_presentSettings.m_fpsLimit, 
			GraphicsSettingInt.Vegetation => (int)m_vegetation, 
			GraphicsSettingInt.Lights => m_lights, 
			GraphicsSettingInt.LOD => m_lod, 
			GraphicsSettingInt.ShadowQuality => m_shadowQuality, 
			GraphicsSettingInt.PointLights => m_pointLights, 
			GraphicsSettingInt.PointLightShadows => m_pointLightShadows, 
			GraphicsSettingInt.SSAO => m_ssao, 
			GraphicsSettingInt.Target3DResolutionVertical => m_target3DResolutionVertical, 
			GraphicsSettingInt.UpscalingAlgorithm => (int)m_upscalingAlgorithm, 
			_ => throw new ArgumentException($"Can't get setting state for {setting}"), 
		};
	}

	public bool GetValue(GraphicsSettingBool setting)
	{
		return setting switch
		{
			GraphicsSettingBool.Vsync => m_presentSettings.m_vsync, 
			GraphicsSettingBool.Tesselation => m_tesselation, 
			GraphicsSettingBool.DistantShadows => m_distantShadows, 
			GraphicsSettingBool.SoftParticles => m_softParticles, 
			GraphicsSettingBool.AntiAliasing => m_antiAliasing, 
			GraphicsSettingBool.Bloom => m_bloom, 
			GraphicsSettingBool.DepthOfField => m_depthOfField, 
			GraphicsSettingBool.MotionBlur => m_motionBlur, 
			GraphicsSettingBool.ChromaticAberration => m_chromaticAberration, 
			GraphicsSettingBool.SunShafts => m_sunShafts, 
			GraphicsSettingBool.AnisotropicTextures => m_anisotropicTextures, 
			_ => throw new ArgumentException($"Can't get setting state for {setting}"), 
		};
	}

	public void SetValue(GraphicsSettingInt setting, int value)
	{
		switch (setting)
		{
		case GraphicsSettingInt.FpsLimit:
			m_presentSettings.m_fpsLimit = value;
			break;
		case GraphicsSettingInt.Vegetation:
			m_vegetation = (ClutterSystem.Quality)value;
			break;
		case GraphicsSettingInt.Lights:
			m_lights = value;
			break;
		case GraphicsSettingInt.LOD:
			m_lod = value;
			break;
		case GraphicsSettingInt.ShadowQuality:
			m_shadowQuality = value;
			break;
		case GraphicsSettingInt.PointLights:
			m_pointLights = value;
			break;
		case GraphicsSettingInt.PointLightShadows:
			m_pointLightShadows = value;
			break;
		case GraphicsSettingInt.SSAO:
			m_ssao = value;
			break;
		case GraphicsSettingInt.Target3DResolutionVertical:
			m_target3DResolutionVertical = value;
			break;
		case GraphicsSettingInt.UpscalingAlgorithm:
			m_upscalingAlgorithm = (UpscalingAlgorithm)value;
			break;
		default:
			throw new ArgumentException($"Can't get setting state for {setting}");
		}
	}

	public void SetValue(GraphicsSettingBool setting, bool value)
	{
		switch (setting)
		{
		case GraphicsSettingBool.Vsync:
			m_presentSettings.m_vsync = value;
			break;
		case GraphicsSettingBool.Tesselation:
			m_tesselation = value;
			break;
		case GraphicsSettingBool.DistantShadows:
			m_distantShadows = value;
			break;
		case GraphicsSettingBool.SoftParticles:
			m_softParticles = value;
			break;
		case GraphicsSettingBool.AntiAliasing:
			m_antiAliasing = value;
			break;
		case GraphicsSettingBool.Bloom:
			m_bloom = value;
			break;
		case GraphicsSettingBool.DepthOfField:
			m_depthOfField = value;
			break;
		case GraphicsSettingBool.MotionBlur:
			m_motionBlur = value;
			break;
		case GraphicsSettingBool.ChromaticAberration:
			m_chromaticAberration = value;
			break;
		case GraphicsSettingBool.SunShafts:
			m_sunShafts = value;
			break;
		case GraphicsSettingBool.AnisotropicTextures:
			m_anisotropicTextures = value;
			break;
		default:
			throw new ArgumentException($"Can't get setting state for {setting}");
		}
	}
}
