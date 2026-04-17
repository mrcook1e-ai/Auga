using System;

[Serializable]
[Flags]
public enum GraphicsSettingInt : uint
{
	None = 0u,
	Target3DResolutionVertical = 4u,
	UpscalingAlgorithm = 8u,
	FpsLimit = 0x10u,
	Vegetation = 0x20u,
	LOD = 0x40u,
	Lights = 0x80u,
	ShadowQuality = 0x100u,
	PointLights = 0x200u,
	PointLightShadows = 0x400u,
	SSAO = 0x800u
}
