using Splatform;

public class MatchmakingManager
{
	private static MatchmakingManager s_instance;

	private Invite? m_pendingInvite;

	public static void Initialize()
	{
		if (s_instance != null)
		{
			ZLog.LogError("MatchmakingManager already initialized!");
		}
		else
		{
			s_instance = new MatchmakingManager();
		}
	}

	private MatchmakingManager()
	{
		IMatchmakingProvider matchmakingProvider = PlatformManager.DistributionPlatform.MatchmakingProvider;
		if (matchmakingProvider == null)
		{
			ZLog.Log("Platform doesn't implement matchmaking! Don't initialize matchmaking manager.");
			s_instance = null;
		}
		else
		{
			matchmakingProvider.AcceptMultiplayerSessionInvite += OnAcceptMultiplayerSessionInvite;
		}
	}

	private void OnAcceptMultiplayerSessionInvite(Invite invite)
	{
		if (m_pendingInvite.HasValue)
		{
			ZLog.Log("Existing pending invite was reset");
		}
		m_pendingInvite = null;
		_ = FejdStartup.instance != null;
		if (Game.instance != null && !Game.instance.IsShuttingDown() && UnifiedPopup.IsAvailable() && Menu.instance != null)
		{
			string header;
			string text;
			switch (invite.m_inviteType)
			{
			case InviteType.Invite:
				header = "$menu_acceptedinvite";
				text = "$menu_logoutprompt";
				break;
			case InviteType.JoinSession:
				header = "$menu_joindifferentserver";
				text = "$menu_logoutprompt";
				break;
			default:
				ZLog.LogError("This part of the code should be unreachable - can't join a game via the invite/join system without having been invited or joined!");
				return;
			}
			m_pendingInvite = invite;
			UnifiedPopup.Push(new YesNoPopup(header, text, delegate
			{
				UnifiedPopup.Pop();
				if (Menu.instance != null)
				{
					Menu.instance.OnLogoutYes();
				}
			}, delegate
			{
				UnifiedPopup.Pop();
				m_pendingInvite = null;
			}));
		}
		else
		{
			m_pendingInvite = invite;
		}
	}

	public static bool TryConsumePendingInvite(out Invite invite)
	{
		if (s_instance == null)
		{
			invite = default(Invite);
			return false;
		}
		if (!s_instance.m_pendingInvite.HasValue)
		{
			invite = default(Invite);
			return false;
		}
		invite = s_instance.m_pendingInvite.Value;
		s_instance.m_pendingInvite = null;
		return true;
	}
}
