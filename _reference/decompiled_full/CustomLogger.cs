using System;
using System.Diagnostics;
using UnityEngine;

public class CustomLogger : MonoBehaviour
{
	private static string s_link = Application.persistentDataPath + "/Player.log";

	private static string s_target = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Library/Logs/IronGate/Valheim/Player.log";

	public static void SetupSymbolicLink()
	{
		if (Application.platform != RuntimePlatform.OSXPlayer && Application.platform != RuntimePlatform.OSXEditor)
		{
			UnityEngine.Debug.LogError("Only use SetupSymbolicLink on MacOS in its current incarnation!");
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo("/bin/ln", "-sf \"" + s_target + "\" \"" + s_link + "\"")
			{
				UseShellExecute = false,
				CreateNoWindow = true
			});
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogError("Error when trying to create symbolic link to log file in Application.persistentDataPath! " + ex.Message);
		}
	}
}
