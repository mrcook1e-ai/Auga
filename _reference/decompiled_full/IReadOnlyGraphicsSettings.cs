using System.Collections.Generic;

public interface IReadOnlyGraphicsSettings
{
	IReadOnlyList<GraphicsSettingStateInt> GraphicsSettingsStatesInt { get; }

	IReadOnlyList<GraphicsSettingStateBool> GraphicsSettingsStatesBool { get; }
}
