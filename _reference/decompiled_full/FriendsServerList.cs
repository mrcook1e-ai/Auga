using System;
using System.Collections.Generic;
using Splatform;

public class FriendsServerList : IServerList
{
	private static List<ServerJoinData> m_tempServerList = new List<ServerJoinData>();

	private readonly string m_displayName;

	private bool m_needsRefresh = true;

	private DateTime m_lastRefreshedTimeUtc = DateTime.MinValue;

	private HashSet<PlatformUserID> m_hostsBeingRetrieved = new HashSet<PlatformUserID>();

	private string m_filter = string.Empty;

	private bool m_isRefreshingFriendsListFromSplatform;

	private readonly List<ServerJoinDataAndHostUser> m_friendsServers = new List<ServerJoinDataAndHostUser>();

	private readonly Queue<PlatformUserID> m_friendsToSearchViaPlayFab = new Queue<PlatformUserID>();

	private int m_retrievedServerListRevision;

	private int m_eventServerListRevision;

	private readonly List<ServerData> m_steamFriendsServers = new List<ServerData>();

	public string DisplayName => m_displayName;

	public DateTime LastRefreshTimeUtc => m_lastRefreshedTimeUtc;

	public bool CanRefresh => !m_isRefreshingFriendsListFromSplatform;

	public uint TotalServers => (uint)(m_friendsServers.Count + GetSteamFriendsServerList().Count);

	public event ServerListUpdatedHandler ServerListUpdated;

	public FriendsServerList(string displayName)
	{
		m_displayName = displayName;
	}

	private IReadOnlyList<ServerData> GetSteamFriendsServerList()
	{
		ZSteamMatchmaking.instance.SetFriendFilter(enabled: true);
		if (ZSteamMatchmaking.instance.GetServerListRevision(ref m_retrievedServerListRevision))
		{
			m_steamFriendsServers.Clear();
			ZSteamMatchmaking.instance.GetServers(m_steamFriendsServers);
		}
		return m_steamFriendsServers;
	}

	public void Refresh()
	{
		m_needsRefresh = true;
		m_lastRefreshedTimeUtc = DateTime.UtcNow;
	}

	private void InternalRefresh()
	{
		if (m_isRefreshingFriendsListFromSplatform)
		{
			ZLog.LogError("Server list is already refreshing! Skipping.");
			return;
		}
		m_friendsServers.Clear();
		m_friendsToSearchViaPlayFab.Clear();
		m_eventServerListRevision = 0;
		ZSteamMatchmaking.instance.RequestServerlist();
		this.ServerListUpdated?.Invoke();
		IMatchmakingProvider matchmakingProvider = PlatformManager.DistributionPlatform.MatchmakingProvider;
		if (matchmakingProvider == null)
		{
			return;
		}
		if (matchmakingProvider.AvailableFeatureSet.HasFlag(MatchmakingFeatureSet.GetFriendMultiplayerSessions))
		{
			PlatformManager.DistributionPlatform.MatchmakingProvider.GetFriendMultiplayerSessions(OnGetFriendMultiplayerSessionsResult, OnGetFriendMultiplayerSessionsCompleted);
			m_isRefreshingFriendsListFromSplatform = true;
		}
		else if (PlatformManager.DistributionPlatform.RelationsProvider != null)
		{
			PlatformUserID[] friends = PlatformManager.DistributionPlatform.RelationsProvider.GetFriends();
			for (int i = 0; i < friends.Length; i++)
			{
				m_friendsToSearchViaPlayFab.Enqueue(friends[i]);
			}
		}
	}

	private void OnGetFriendMultiplayerSessionsResult(RemoteMultiplayerSessionData[] sessionDatas)
	{
		ZLog.Log($"Found {sessionDatas.Length} servers");
		for (int i = 0; i < sessionDatas.Length; i++)
		{
			RemoteMultiplayerSessionData remoteMultiplayerSessionData = sessionDatas[i];
			ServerJoinData joinData = new ServerJoinData(new ServerJoinDataPlayFabUser(remoteMultiplayerSessionData.m_connectionString));
			m_friendsServers.Add(new ServerJoinDataAndHostUser(joinData, remoteMultiplayerSessionData.m_user));
		}
		this.ServerListUpdated?.Invoke();
	}

	private void OnGetFriendMultiplayerSessionsCompleted(bool succeeded)
	{
		ZLog.Log("Friend server search completed");
		m_isRefreshingFriendsListFromSplatform = false;
	}

	public void SetFilter(string filter, bool isTyping = false)
	{
		m_filter = filter;
		this.ServerListUpdated?.Invoke();
	}

	public void GetFilteredList(List<ServerListEntryData> resultOutput)
	{
		resultOutput.Clear();
		IReadOnlyList<ServerData> steamFriendsServerList = GetSteamFriendsServerList();
		for (int i = 0; i < steamFriendsServerList.Count; i++)
		{
			ServerData server = steamFriendsServerList[i];
			string text = RetrieveFriendDisplayName(server.m_matchmakingData.m_hostUser);
			if (text != null)
			{
				AddServerIfNotFiltered(server, text, resultOutput);
			}
		}
		for (int j = 0; j < m_friendsServers.Count; j++)
		{
			ServerJoinDataAndHostUser serverJoinDataAndHostUser = m_friendsServers[j];
			if (serverJoinDataAndHostUser.m_joinData.IsValid)
			{
				string text2 = RetrieveFriendDisplayName(serverJoinDataAndHostUser.m_hostUser);
				ServerMatchmakingData serverMatchmakingData = MultiBackendMatchmaking.GetServerMatchmakingData(serverJoinDataAndHostUser.m_joinData, m_lastRefreshedTimeUtc);
				if (serverMatchmakingData.IsValid && text2 != null)
				{
					AddServerIfNotFiltered(new ServerData(serverJoinDataAndHostUser.m_joinData, serverMatchmakingData), text2, resultOutput);
				}
			}
		}
		resultOutput.Sort((ServerListEntryData a, ServerListEntryData b) => a.m_serverName.CompareTo(b.m_serverName));
	}

	private string RetrieveFriendDisplayName(PlatformUserID hostUser)
	{
		IRelationsProvider relationsProvider = PlatformManager.DistributionPlatform.RelationsProvider;
		if (relationsProvider == null)
		{
			return null;
		}
		if (!relationsProvider.TryGetUserProfile(hostUser, out var profile))
		{
			if (!m_hostsBeingRetrieved.Contains(hostUser))
			{
				m_hostsBeingRetrieved.Add(hostUser);
				relationsProvider.GetUserProfileAsync(hostUser, OnGetUserProfileCompleted, OnGetUserProfileFailed);
			}
			return null;
		}
		return profile.DisplayName;
	}

	private void AddServerIfNotFiltered(ServerData server, string friendDisplayName, List<ServerListEntryData> resultOutput)
	{
		string text = friendDisplayName + " [" + server.m_matchmakingData.m_serverName + "]";
		if (string.IsNullOrEmpty(m_filter) || text.ToLowerInvariant().Contains(m_filter.ToLowerInvariant()))
		{
			resultOutput.Add(new ServerListEntryData(server, text));
		}
	}

	private void OnGetUserProfileCompleted(IUserProfile profile)
	{
		m_hostsBeingRetrieved.Remove(profile.PlatformUserID);
		this.ServerListUpdated?.Invoke();
	}

	private void OnGetUserProfileFailed(PlatformUserID userId, GetUserProfileFailReason reason)
	{
		m_hostsBeingRetrieved.Remove(userId);
	}

	public void OnOpen()
	{
	}

	public void OnClose()
	{
	}

	public void Tick()
	{
		if (m_needsRefresh)
		{
			m_needsRefresh = false;
			InternalRefresh();
		}
		if (ZSteamMatchmaking.instance.GetServerListRevision(ref m_eventServerListRevision))
		{
			this.ServerListUpdated?.Invoke();
		}
		for (int i = 0; i < m_friendsServers.Count; i++)
		{
			if (m_friendsServers[i].m_joinData.IsValid)
			{
				m_tempServerList.Add(m_friendsServers[i].m_joinData);
			}
		}
		ServerListUtils.UpdateServerOnlineStatus(m_tempServerList, m_lastRefreshedTimeUtc, delegate
		{
			this.ServerListUpdated?.Invoke();
		});
		m_tempServerList.Clear();
		PlayFabMatchmaking playFabBackend = MultiBackendMatchmaking.PlayFabBackend;
		if (m_friendsToSearchViaPlayFab.Count > 0 && playFabBackend.IsAvailable && playFabBackend.CanRefreshServerOfTypeNow(ServerJoinDataType.PlayFabUser))
		{
			PlatformUserID hostUser = m_friendsToSearchViaPlayFab.Dequeue();
			playFabBackend.ResolveServerFromHostUser(hostUser, OnFriendServerResolved);
		}
	}

	private void OnFriendServerResolved(ServerData serverData)
	{
		if (serverData.m_joinData.IsValid)
		{
			m_friendsServers.Add(new ServerJoinDataAndHostUser(serverData.m_joinData, serverData.m_matchmakingData.m_hostUser));
			this.ServerListUpdated?.Invoke();
		}
	}
}
