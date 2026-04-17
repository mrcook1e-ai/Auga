using System;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public interface ISettingsTab
{
	event Action<string, int> SharedSettingChanged;

	void Initialize();

	void Terminate()
	{
	}

	void OnTabOpen(Button backButton, Button okButton);

	void OnOkAsync(OkActionCompletedHandler okActionCompletedCallback);

	void OnBack()
	{
	}

	void OnSharedSettingChanged(string setting, int value)
	{
	}
}
