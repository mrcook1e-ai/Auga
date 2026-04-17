using System;
using PlayFab.Party;
using Splatform;
using UnityEngine.SceneManagement;

public class SuspendManager
{
	private static SuspendManager s_instance;

	public static void Initialize()
	{
		if (s_instance != null)
		{
			ZLog.LogError("SuspendManager already initialized!");
		}
		else
		{
			s_instance = new SuspendManager();
		}
	}

	private SuspendManager()
	{
		IPLMProvider pLMProvider = PlatformManager.DistributionPlatform.PLMProvider;
		if (pLMProvider == null)
		{
			ZLog.Log("Platform doesn't implement Process Lifetime Management! Don't initialize suspend manager.");
			s_instance = null;
			return;
		}
		pLMProvider.EnteringSuspend += OnEnteringSuspend;
		pLMProvider.LeavingSuspend += OnLeavingSuspend;
		pLMProvider.ResumedFromSuspend += OnResumedFromSuspend;
		pLMProvider.IsRunningInBackgroundChanged += OnIsRunningInBackgroundChanged;
	}

	private void OnEnteringSuspend(DateTime deadlineUtc)
	{
		if (Game.instance != null && !ZNet.IsSinglePlayer)
		{
			ZNetScene.instance.Shutdown();
			ZNet.instance.ShutdownWithoutSave(suspending: true);
		}
		PlayFabMultiplayerManager.Get().Suspend();
	}

	private void OnLeavingSuspend()
	{
		if (!(Game.instance == null))
		{
			bool num = ZNet.instance != null && ZNet.instance.IsServer();
			bool flag = ZNet.IsOpenServer();
			if (num == flag)
			{
				SceneManager.sceneLoaded += OnMainMenuAfterResume;
				Game.instance.Logout();
			}
		}
	}

	private void OnResumedFromSuspend()
	{
		if (PlatformManager.DistributionPlatform.PLMProvider.SupportedSuspendEvents.HasFlag(SuspendEvents.EnteringSuspend))
		{
			PlayFabMultiplayerManager.Get().Resume();
		}
	}

	private void OnMainMenuAfterResume(Scene scene, LoadSceneMode mode)
	{
		SceneManager.sceneLoaded -= OnMainMenuAfterResume;
		if (UnifiedPopup.IsAvailable() && !UnifiedPopup.IsVisible())
		{
			string text = "$online_kickedfromsession_suspendresume_xbox_text";
			UnifiedPopup.Push(new WarningPopup("$online_kickedfromsession_header", text, delegate
			{
				UnifiedPopup.Pop();
			}));
		}
	}

	private void OnIsRunningInBackgroundChanged(bool isRunningInBackground)
	{
		if (Minimap.instance != null)
		{
			Minimap.instance.PauseUpdateTemporarily();
		}
	}
}
