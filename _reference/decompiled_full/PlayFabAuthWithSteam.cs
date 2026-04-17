using System.Text;
using PlayFab;
using PlayFab.ClientModels;
using Steamworks;

public static class PlayFabAuthWithSteam
{
	private static string m_steamTicket;

	public static void Login()
	{
		SteamNetworkingIdentity serverIdentity = default(SteamNetworkingIdentity);
		byte[] array = ZSteamMatchmaking.instance.RequestSessionTicket(ref serverIdentity);
		if (array == null)
		{
			PlayFabManager.instance.OnLoginFailure(null);
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.AppendFormat("{0:x2}", array[i]);
		}
		m_steamTicket = stringBuilder.ToString();
		ZSteamMatchmaking.instance.AuthSessionTicketResponse += OnAuthSessionTicketResponse;
	}

	private static void OnAuthSessionTicketResponse()
	{
		ZSteamMatchmaking.instance.AuthSessionTicketResponse -= OnAuthSessionTicketResponse;
		LoginWithSteamRequest request = new LoginWithSteamRequest
		{
			CreateAccount = true,
			SteamTicket = m_steamTicket
		};
		m_steamTicket = null;
		PlayFabClientAPI.LoginWithSteam(request, OnSteamLoginSuccess, OnSteamLoginFailed);
	}

	private static void OnSteamLoginSuccess(LoginResult result)
	{
		ZLog.Log("Logged in PlayFab user via Steam auth session ticket");
		PlayFabManager.instance.OnLoginSuccess(result);
		ZSteamMatchmaking.instance.ReleaseSessionTicket();
	}

	private static void OnSteamLoginFailed(PlayFabError error)
	{
		ZLog.LogError("Failed to logged in PlayFab user via Steam auth session ticket: " + error.GenerateErrorReport());
		PlayFabManager.instance.OnLoginFailure(error);
		ZSteamMatchmaking.instance.ReleaseSessionTicket();
	}
}
