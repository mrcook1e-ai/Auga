using System;

[Serializable]
[Flags]
public enum GraphicsSettingBool : uint
{
	None = 0u,
	Vsync = 2u,
	DistantShadows = 4u,
	Tesselation = 8u,
	Bloom = 0x20u,
	DepthOfField = 0x40u,
	MotionBlur = 0x80u,
	ChromaticAberration = 0x100u,
	SunShafts = 0x200u,
	SoftParticles = 0x400u,
	AntiAliasing = 0x800u,
	AnisotropicTextures = 0x1000u
}
