using System;
using System.ComponentModel;
using System.Threading;
using Splatform;
using UnityEngine;

public static class PlatformInitializer
{
	private static bool s_platformInitialized = false;

	private static bool s_startedStorageInitialization = false;

	private static bool s_allowStorageInitialization = true;

	private static bool s_inputDeviceRequired = false;

	public static bool PlatformInitialized => s_platformInitialized;

	public static bool StartedSaveDataInitialization => s_startedStorageInitialization;

	public static bool PreferencesInitialized
	{
		get
		{
			if (PlatformManager.DistributionPlatform.PreferencesProvider != null)
			{
				return PlatformManager.DistributionPlatform.PreferencesProvider.IsInitialized;
			}
			return true;
		}
	}

	public static bool SaveDataInitialized
	{
		get
		{
			if (PlatformManager.DistributionPlatform.SaveDataProvider == null || !PlatformManager.DistributionPlatform.SaveDataProvider.IsEnabled || PlatformManager.DistributionPlatform.SaveDataProvider.IsInitialized)
			{
				return PreferencesInitialized;
			}
			return false;
		}
	}

	public static bool AllowSaveDataInitialization
	{
		get
		{
			return s_allowStorageInitialization;
		}
		set
		{
			s_allowStorageInitialization = value;
			if (s_allowStorageInitialization && !s_startedStorageInitialization)
			{
				InitializeSaveDataStorage();
			}
		}
	}

	public static bool InputDeviceRequired
	{
		get
		{
			return s_inputDeviceRequired;
		}
		set
		{
			s_inputDeviceRequired = value;
			if (PlatformManager.DistributionPlatform != null && PlatformManager.DistributionPlatform.InputDeviceManager != null)
			{
				PlatformManager.DistributionPlatform.InputDeviceManager.SetInputDeviceRequiredForLocalUser(requireGamepad: false, ZInput.CheckKeyboardMouseConnected);
			}
		}
	}

	public static bool WaitingForInputDevice
	{
		get
		{
			if (PlatformManager.DistributionPlatform == null)
			{
				return false;
			}
			if (PlatformManager.DistributionPlatform.InputDeviceManager == null)
			{
				return false;
			}
			return !PlatformManager.DistributionPlatform.InputDeviceManager.HasInputDeviceAssociation(PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	private static void EarlyInitialize()
	{
		if (!Application.isEditor)
		{
			GameObject.Find("");
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void InitializePlatform()
	{
		SetMainThreadName();
		ParseArguments();
		PlatformConfiguration platformConfiguration = default(PlatformConfiguration);
		SteamManager.Initialize();
		platformConfiguration.SetBool("managesteamruntime", value: false);
		platformConfiguration.SetUIntArray("acceptedappids", new uint[2] { 1223920u, 892970u });
		Splatform.Logger.SetLogHandler(OnSplatformLog);
		PlatformManager.InitializeAsync(platformConfiguration, OnInitializeCompleted);
	}

	private static void SetMainThreadName()
	{
		if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
		{
			Thread.CurrentThread.Name = "MainValheimThread";
		}
	}

	private static void ParseArguments()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			_ = commandLineArgs[i];
		}
	}

	private static void OnInitializeCompleted(bool succeeded)
	{
		if (!succeeded)
		{
			ZLog.LogError("Failed to initialize platform!");
			Application.Quit();
			return;
		}
		SuspendManager.Initialize();
		s_platformInitialized = true;
		ZLog.Log("Initialized platform!");
		PlatformManager.DistributionPlatform.LocalUser.SignedIn += OnLoginCompleted;
	}

	private static void OnLoginCompleted()
	{
		PlatformManager.DistributionPlatform.LocalUser.SignedIn -= OnLoginCompleted;
		MatchmakingManager.Initialize();
		if (s_allowStorageInitialization)
		{
			InitializeSaveDataStorage();
		}
	}

	private static void InitializeSaveDataStorage()
	{
		s_startedStorageInitialization = true;
		if (PlatformManager.DistributionPlatform.SaveDataProvider == null)
		{
			return;
		}
		PlatformManager.DistributionPlatform.SaveDataProvider.InitializeAsync(delegate(bool succeeded)
		{
			if (succeeded)
			{
				if (FileHelpers.LocalStorageSupported)
				{
					string[] files = FileHelpers.GetFiles(FileHelpers.FileSource.Local, Utils.GetSaveDataPath(FileHelpers.FileSource.Local));
					string text = "All files in local storage save data:";
					for (int i = 0; i < files.Length; i++)
					{
						text += $"\n{files[i]} ({FileHelpers.GetFileSize(files[i], FileHelpers.FileSource.Local)})";
					}
					ZLog.Log(text);
				}
				else
				{
					ZLog.Log("Local storage is not supported");
				}
				if (FileHelpers.CloudStorageSupported && FileHelpers.CloudStorageEnabled)
				{
					string[] files = FileHelpers.GetFiles(FileHelpers.FileSource.Cloud, Utils.GetSaveDataPath(FileHelpers.FileSource.Cloud));
					string text = "All files in platform save data:";
					for (int j = 0; j < files.Length; j++)
					{
						text += $"\n{files[j]} ({FileHelpers.GetFileSize(files[j], FileHelpers.FileSource.Cloud)})";
					}
					ZLog.Log(text);
				}
				else
				{
					ZLog.Log("Cloud storage is not supported or enabled");
				}
				InitializeAndLoadPrefs();
			}
		});
	}

	private static void InitializeAndLoadPrefs()
	{
		ZLog.Log("Initializing preferences provider...");
		IPreferencesProvider preferences = PlatformManager.DistributionPlatform.PreferencesProvider;
		if (preferences == null)
		{
			ZLog.Log("No preference provider available for this platform!");
			return;
		}
		byte[] data = null;
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.DoWork += delegate
		{
			if (!FileHelpers.FileExistsCloud("Preferences"))
			{
				ZLog.Log("Preferences Provider save file with path Preferences does not exist.");
				return;
			}
			FileReader fileReader = null;
			try
			{
				fileReader = new FileReader("Preferences", FileHelpers.FileSource.Cloud);
				int count = fileReader.m_binary.ReadInt32();
				data = fileReader.m_binary.ReadBytes(count);
			}
			catch (Exception ex)
			{
				ZLog.LogError("Exception while loading preferences: " + ex.Message);
			}
			finally
			{
				fileReader?.Dispose();
			}
		};
		backgroundWorker.RunWorkerCompleted += delegate
		{
			ZLog.Log("Finished loading preference data!");
			preferences.InitializeAsync(data, delegate(bool succeeded)
			{
				if (!succeeded)
				{
					ZLog.LogError("Failed to initialize preferences provider");
				}
				else
				{
					ZLog.Log("Preferences initialized successfully!");
				}
			});
		};
		backgroundWorker.RunWorkerAsync();
	}

	private static void OnSplatformLog(LogType logType, object message)
	{
		switch (logType)
		{
		case LogType.Error:
			ZLog.LogError(message);
			break;
		case LogType.Warning:
			ZLog.LogWarning(message);
			break;
		case LogType.Log:
			ZLog.Log(message);
			break;
		default:
			ZLog.LogError($"Log type {logType} not implemented! Log message:\n{message}");
			break;
		}
	}
}
