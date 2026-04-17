using System;

[Flags]
public enum ServerListEntryDataFlags : byte
{
	None = 0,
	HasMatchmakingData = 1,
	IsAvailable = 2,
	IsOnline = 4,
	IsCrossplay = 8,
	IsRestrictedToOwnPlatform = 0x10,
	IsPasswordProtected = 0x20
}
