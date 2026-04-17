using PlayFab;
using PlayFab.ClientModels;
using Splatform;

public static class PlayFabAuthWithGameCenter
{
	public static void Login()
	{
		if (!(PlayFabManager.instance == null))
		{
			PlayFabClientAPI.LoginWithGameCenter(new LoginWithGameCenterRequest
			{
				TitleId = "6E223",
				CreateAccount = true,
				PlayerId = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID.m_userID
			}, OnLoginSuccess, OnLoginFailure);
		}
	}

	private static void OnLoginSuccess(LoginResult result)
	{
		ZLog.Log("PlayFab logged in via Game Center with ID " + result.PlayFabId);
		PlayFabManager.instance.OnLoginSuccess(result);
	}

	private static void OnLoginFailure(PlayFabError error)
	{
		ZLog.LogWarning($"PlayFab failed to login via Game Center with error code {error.Error}");
		PlayFabManager.instance.OnLoginFailure(error);
	}
}
