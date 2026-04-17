using System;
using System.Collections.Generic;
using NetworkingUtils;
using Splatform;

public class PlayFabMatchmaking : IMatchmakingBackend, IDisposable
{
	private const float c_RequestDelay = 2f;

	private List<ServerData> m_publicServerList = new List<ServerData>();

	private Dictionary<ServerJoinDataPlayFabUser, ServerMatchmakingData> m_matchmakingDataCachePlayFabUser = new Dictionary<ServerJoinDataPlayFabUser, ServerMatchmakingData>();

	private Dictionary<IPEndPoint, ServerMatchmakingData> m_matchmakingDataCacheDedicated = new Dictionary<IPEndPoint, ServerMatchmakingData>();

	private string m_filterLowerInvariant = string.Empty;

	private List<ServerData> m_filteredPublicServerList = new List<ServerData>();

	private bool m_filteredListOutdated = true;

	private ZPlayFabLobbySearch m_currentLobbySearch;

	private RefreshPublicServerListFlags m_refreshPublicServerListFlags;

	private DateTime m_mostRecentRequestTimeUtc = DateTime.MinValue;

	private ServerJoinData m_currentServer = ServerJoinData.None;

	private PlatformUserID m_currentUser = PlatformUserID.None;

	private MatchmakingDataRetrievedHandler m_callback;

	private readonly HashSet<PlayFabMatchmakingServerData> m_playFabTemporarySearchServerList = new HashSet<PlayFabMatchmakingServerData>();

	private bool m_isSubscribedToLoginEvent;

	public bool IsAvailable => PlayFabManager.IsLoggedIn;

	public uint PublicServerCount => (uint)m_publicServerList.Count;

	public IReadOnlyList<ServerData> FilteredPublicServerList
	{
		get
		{
			if (m_filterLowerInvariant.Length == 0)
			{
				return m_publicServerList;
			}
			if (m_filteredListOutdated)
			{
				m_filteredPublicServerList.Clear();
				ServerListUtils.GetFilteredList(m_publicServerList, m_filterLowerInvariant, m_filteredPublicServerList);
				m_filteredListOutdated = false;
			}
			return m_filteredPublicServerList;
		}
	}

	public bool ServerSideFiltering => true;

	public bool IsRefreshingPublicServerList => m_currentLobbySearch != null;

	public event PublicServerListUpdatedHandler FilteredPublicServerListUpdated;

	public PlayFabMatchmaking()
	{
		if (!PlayFabManager.IsLoggedIn && PlayFabManager.instance != null)
		{
			PlayFabManager.instance.LoginFinished += OnPlayFabLoginFinished;
			m_isSubscribedToLoginEvent = true;
		}
	}

	public void Dispose()
	{
		if (m_isSubscribedToLoginEvent && PlayFabManager.instance != null)
		{
			PlayFabManager.instance.LoginFinished -= OnPlayFabLoginFinished;
			m_isSubscribedToLoginEvent = false;
		}
	}

	private void OnPlayFabLoginFinished(LoginType loginType)
	{
	}

	public void RefreshPublicServerList(RefreshPublicServerListFlags flags)
	{
		CancelPlayFabServerSearch();
		m_refreshPublicServerListFlags = flags;
		if (!m_refreshPublicServerListFlags.HasFlag(RefreshPublicServerListFlags.ServerSideFilterRefresh))
		{
			m_playFabTemporarySearchServerList.Clear();
			SetResultFromTemporaryListToPublicList();
		}
		this.FilteredPublicServerListUpdated?.Invoke();
		m_playFabTemporarySearchServerList.EnsureCapacity(200);
		m_mostRecentRequestTimeUtc = DateTime.UtcNow;
		m_currentLobbySearch = ZPlayFabMatchmaking.ListServers(m_filterLowerInvariant, PlayFabServersFound, PlayFabServerSearchDone);
		ZLog.DevLog("PlayFab server search started!");
	}

	public void SetPublicServerListFilter(string filter)
	{
		if (filter == null)
		{
			filter = string.Empty;
		}
		filter = filter.ToLowerInvariant();
		if (!(m_filterLowerInvariant == filter))
		{
			m_filterLowerInvariant = filter;
			m_filteredListOutdated = true;
			if (m_filterLowerInvariant.Length == 0)
			{
				m_filteredPublicServerList.Clear();
			}
			this.FilteredPublicServerListUpdated?.Invoke();
		}
	}

	public bool CanRefreshServerNow()
	{
		if (!IsAvailable)
		{
			return false;
		}
		if (m_currentServer.IsValid || m_currentUser.IsValid)
		{
			return false;
		}
		if ((DateTime.UtcNow - m_mostRecentRequestTimeUtc).TotalSeconds < 2.0)
		{
			return false;
		}
		return true;
	}

	public bool CanRefreshServerOfTypeNow(ServerJoinDataType type)
	{
		if (type - 2 <= ServerJoinDataType.SteamUser)
		{
			return CanRefreshServerNow();
		}
		return false;
	}

	public bool RefreshServer(ServerJoinData server, MatchmakingDataRetrievedHandler completedCallback)
	{
		if (!server.IsValid)
		{
			throw new ArgumentException("server was invalid!");
		}
		if (!CanRefreshServerOfTypeNow(server.m_type))
		{
			return false;
		}
		switch (server.m_type)
		{
		case ServerJoinDataType.PlayFabUser:
			m_currentServer = server;
			m_callback = completedCallback;
			ZPlayFabMatchmaking.CheckHostOnlineStatus(m_currentServer.PlayFabUser.m_remotePlayerId, PlayFabPingSuccess, PlayFabPingFailed);
			break;
		case ServerJoinDataType.Dedicated:
		{
			if (!MultiBackendMatchmaking.GetServerIPCached(server.Dedicated, out var address))
			{
				return false;
			}
			m_currentServer = server;
			m_callback = completedCallback;
			if (address.HasValue)
			{
				ZPlayFabMatchmaking.FindHostByIp(new IPEndPoint(address.Value, m_currentServer.Dedicated.m_port), PlayFabPingSuccess, PlayFabPingFailed);
			}
			else
			{
				PlayFabPingFailed(ZPLayFabMatchmakingFailReason.EndPointNotOnInternet);
			}
			break;
		}
		default:
			return false;
		}
		return true;
	}

	public bool ResolveServerFromHostUser(PlatformUserID hostUser, MatchmakingDataRetrievedHandler completedCallback)
	{
		if (!hostUser.IsValid)
		{
			throw new ArgumentException("hostUser was invalid!");
		}
		if (!CanRefreshServerOfTypeNow(ServerJoinDataType.PlayFabUser))
		{
			return false;
		}
		m_currentUser = hostUser;
		m_callback = completedCallback;
		ZPlayFabMatchmaking.FindServerByHostUser(hostUser, PlayFabPingSuccess, PlayFabPingFailed);
		return true;
	}

	public bool IsPending(ServerJoinData server)
	{
		return m_currentServer == server;
	}

	public bool IsPending(PlatformUserID server)
	{
		return m_currentUser == server;
	}

	public ServerMatchmakingData GetServerMatchmakingData(ServerJoinData server, DateTime newerThanUtc = default(DateTime))
	{
		if (!server.IsValid)
		{
			throw new ArgumentException("server was invalid");
		}
		ServerMatchmakingData value;
		switch (server.m_type)
		{
		case ServerJoinDataType.PlayFabUser:
			if (!m_matchmakingDataCachePlayFabUser.TryGetValue(server.PlayFabUser, out value))
			{
				return ServerMatchmakingData.None;
			}
			break;
		case ServerJoinDataType.Dedicated:
		{
			if (!MultiBackendMatchmaking.GetServerIPCached(server.Dedicated, out var address))
			{
				return ServerMatchmakingData.None;
			}
			if (!address.HasValue)
			{
				return new ServerMatchmakingData(newerThanUtc.AddTicks(1L));
			}
			if (!address.Value.IsPublic())
			{
				return new ServerMatchmakingData(newerThanUtc.AddTicks(1L));
			}
			if (!m_matchmakingDataCacheDedicated.TryGetValue(new IPEndPoint(address.Value, server.Dedicated.m_port), out value))
			{
				return ServerMatchmakingData.None;
			}
			break;
		}
		default:
			return new ServerMatchmakingData(newerThanUtc.AddTicks(1L));
		}
		if (value.m_timestampUtc <= newerThanUtc)
		{
			return ServerMatchmakingData.None;
		}
		return value;
	}

	public void Tick()
	{
	}

	private void PlayFabPingSuccess(PlayFabMatchmakingServerData serverData)
	{
		if (!m_currentServer.IsValid && !m_currentUser.IsValid)
		{
			ZLog.LogError("Current was invalid!");
		}
		else
		{
			SetResult(serverData.ToServerData(DateTime.UtcNow));
		}
	}

	private void PlayFabPingFailed(ZPLayFabMatchmakingFailReason failReason)
	{
		if (m_currentServer.IsValid)
		{
			ServerData result = new ServerData(m_currentServer, new ServerMatchmakingData(DateTime.UtcNow));
			SetResult(result);
		}
		else if (m_currentUser.IsValid)
		{
			SetResult(ServerData.None);
		}
		else
		{
			ZLog.LogError("Current was invalid!");
		}
	}

	private void SetResult(ServerData result)
	{
		if (result.m_joinData.IsValid)
		{
			SetMatchmakingDataCacheEntry(result.m_joinData, result.m_matchmakingData);
		}
		MatchmakingDataRetrievedHandler callback = m_callback;
		m_currentServer = ServerJoinData.None;
		m_currentUser = PlatformUserID.None;
		m_callback = null;
		callback?.Invoke(result);
	}

	private void PlayFabServersFound(PlayFabMatchmakingServerData[] serverDatas)
	{
		foreach (PlayFabMatchmakingServerData playFabMatchmakingServerData in serverDatas)
		{
			if (m_playFabTemporarySearchServerList.TryGetValue(playFabMatchmakingServerData, out var actualValue))
			{
				if (playFabMatchmakingServerData.tickCreated > actualValue.tickCreated)
				{
					m_playFabTemporarySearchServerList.Remove(playFabMatchmakingServerData);
					m_playFabTemporarySearchServerList.Add(playFabMatchmakingServerData);
				}
			}
			else
			{
				m_playFabTemporarySearchServerList.Add(playFabMatchmakingServerData);
			}
		}
		SetResultFromTemporaryListToPublicList();
	}

	private void PlayFabServerSearchDone(ZPLayFabMatchmakingFailReason failedReason)
	{
		ZLog.DevLog("PlayFab server search done!");
		if (m_playFabTemporarySearchServerList.Count <= 0)
		{
			SetResultFromTemporaryListToPublicList();
		}
		m_currentLobbySearch = null;
	}

	private void CancelPlayFabServerSearch()
	{
		if (m_currentLobbySearch != null)
		{
			m_currentLobbySearch.Cancel();
			m_currentLobbySearch = null;
		}
	}

	private void SetResultFromTemporaryListToPublicList()
	{
		m_publicServerList.Clear();
		foreach (PlayFabMatchmakingServerData playFabTemporarySearchServer in m_playFabTemporarySearchServerList)
		{
			ServerJoinData serverJoinData;
			if (playFabTemporarySearchServer.isDedicatedServer && !string.IsNullOrEmpty(playFabTemporarySearchServer.serverIp))
			{
				ServerJoinDataDedicated dedicated = new ServerJoinDataDedicated(playFabTemporarySearchServer.serverIp);
				if (dedicated.TryGetIPAddress(out var _))
				{
					serverJoinData = new ServerJoinData(dedicated);
				}
				else
				{
					ZLog.Log("Dedicated server with invalid IP address - fallback to PlayFab ID");
					serverJoinData = new ServerJoinData(new ServerJoinDataPlayFabUser(playFabTemporarySearchServer.remotePlayerId), playFabTemporarySearchServer.platformUserID);
				}
			}
			else
			{
				serverJoinData = new ServerJoinData(new ServerJoinDataPlayFabUser(playFabTemporarySearchServer.remotePlayerId), playFabTemporarySearchServer.platformUserID);
			}
			if (!playFabTemporarySearchServer.gameVersion.IsValid())
			{
				ZLog.LogWarning($"Failed to parse version string! Skipping server entry {serverJoinData}.");
				continue;
			}
			_ = Platform.Unknown;
			if (playFabTemporarySearchServer.gameVersion >= Version.FirstVersionWithPlatformRestriction)
			{
				_ = playFabTemporarySearchServer.platformRestriction;
			}
			ServerMatchmakingData matchmakingData = playFabTemporarySearchServer.ToServerMatchmakingData(DateTime.UtcNow);
			m_publicServerList.Add(new ServerData(serverJoinData, matchmakingData));
		}
		m_filteredListOutdated = true;
		this.FilteredPublicServerListUpdated?.Invoke();
	}

	public void ResolveJoinCode(string joinCode, MatchmakingDataRetrievedHandler successCallback, ZPlayFabMatchmakingFailedCallback failedCallback)
	{
		ZPlayFabMatchmaking.ResolveJoinCode(joinCode, OnResolveJoinCodeSuccess, failedCallback);
		void OnResolveJoinCodeSuccess(PlayFabMatchmakingServerData serverData)
		{
			ServerData serverData2 = serverData.ToServerData(DateTime.UtcNow);
			SetMatchmakingDataCacheEntry(serverData2.m_joinData, serverData2.m_matchmakingData);
			successCallback(serverData2);
		}
	}

	private void SetMatchmakingDataCacheEntry(ServerJoinData server, ServerMatchmakingData serverMatchmakingData)
	{
		switch (server.m_type)
		{
		case ServerJoinDataType.PlayFabUser:
			m_matchmakingDataCachePlayFabUser[server.PlayFabUser] = serverMatchmakingData;
			break;
		case ServerJoinDataType.Dedicated:
		{
			if (!server.Dedicated.TryGetIPEndPoint(out var endPoint))
			{
				if (!MultiBackendMatchmaking.Instance.m_dnsResolver.ResolveDomainName(server.Dedicated.m_host, out var address, DnsResolveFlags.CacheOnly))
				{
					throw new InvalidOperationException($"Can't set matchmaking data cache entry for server {server} - the address must either be part of the server data or must already be in the DNS resolve cache!");
				}
				if (!address.HasValue)
				{
					break;
				}
				endPoint = new IPEndPoint(address.Value, server.Dedicated.m_port);
			}
			m_matchmakingDataCacheDedicated[endPoint] = serverMatchmakingData;
			break;
		}
		default:
			throw new ArgumentException($"Can't set matchmaking data cache entry for server {server} of type {server.m_type}.");
		}
	}
}
