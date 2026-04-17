using System;
using System.Collections.Generic;
using NetworkingUtils;
using Splatform;
using Steamworks;

public class SteamworksMatchmaking : IMatchmakingBackend, IDisposable
{
	private const double c_timeoutSeconds = 5.0;

	private Dictionary<ServerJoinDataSteamUser, ServerMatchmakingData> m_matchmakingDataCacheSteamUser = new Dictionary<ServerJoinDataSteamUser, ServerMatchmakingData>();

	private Dictionary<IPEndPoint, ServerMatchmakingData> m_matchmakingDataCacheDedicated = new Dictionary<IPEndPoint, ServerMatchmakingData>();

	private int m_retrievedServerListRevision;

	private int m_eventServerListRevision;

	private List<ServerData> m_publicServerList = new List<ServerData>();

	private bool m_isRefreshingPublicServerList;

	private Dictionary<CSteamID, MatchmakingDataRetrievedHandler> m_activeUserHostedSearches = new Dictionary<CSteamID, MatchmakingDataRetrievedHandler>();

	private List<SteamworksUserHostedSearchTimeout> m_activeUserHostedSearchTimeouts = new List<SteamworksUserHostedSearchTimeout>();

	private ServerJoinData m_activeDedicatedSearch = ServerJoinData.None;

	private MatchmakingDataRetrievedHandler m_activeDedicatedSearchCallback;

	public bool IsAvailable => true;

	public uint PublicServerCount => (uint)ZSteamMatchmaking.instance.GetTotalNrOfServers();

	public IReadOnlyList<ServerData> FilteredPublicServerList
	{
		get
		{
			ZSteamMatchmaking.instance.SetFriendFilter(enabled: false);
			if (ZSteamMatchmaking.instance.GetServerListRevision(ref m_retrievedServerListRevision))
			{
				m_publicServerList.Clear();
				ZSteamMatchmaking.instance.GetServers(m_publicServerList);
			}
			return m_publicServerList;
		}
	}

	public bool ServerSideFiltering => false;

	public bool IsRefreshingPublicServerList => m_isRefreshingPublicServerList;

	public event PublicServerListUpdatedHandler FilteredPublicServerListUpdated;

	public void Dispose()
	{
		for (int i = 0; i < m_activeUserHostedSearchTimeouts.Count; i++)
		{
			if (m_activeUserHostedSearches.ContainsKey(m_activeUserHostedSearchTimeouts[i].m_server.m_joinUserID))
			{
				ZSteamMatchmaking.instance.AbortCheckIfOnlineAsync(m_activeUserHostedSearchTimeouts[i].m_server);
			}
		}
		m_activeUserHostedSearchTimeouts.Clear();
	}

	public void RefreshPublicServerList(RefreshPublicServerListFlags flags)
	{
		m_publicServerList.Clear();
		m_retrievedServerListRevision = 0;
		m_eventServerListRevision = 0;
		ZSteamMatchmaking.instance.RequestServerlist();
		this.FilteredPublicServerListUpdated?.Invoke();
	}

	public void SetPublicServerListFilter(string filter)
	{
		ZSteamMatchmaking.instance.SetNameFilter(filter);
	}

	public bool CanRefreshServerNow()
	{
		return IsAvailable;
	}

	public bool CanRefreshServerOfTypeNow(ServerJoinDataType type)
	{
		return type switch
		{
			ServerJoinDataType.SteamUser => true, 
			ServerJoinDataType.Dedicated => !m_activeDedicatedSearch.IsValid, 
			_ => false, 
		};
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
		case ServerJoinDataType.SteamUser:
		{
			if (m_activeUserHostedSearches.TryGetValue(server.SteamUser.m_joinUserID, out var value))
			{
				value = (MatchmakingDataRetrievedHandler)Delegate.Combine(value, completedCallback);
				break;
			}
			m_activeUserHostedSearches.Add(server.SteamUser.m_joinUserID, completedCallback);
			m_activeUserHostedSearchTimeouts.Add(new SteamworksUserHostedSearchTimeout(server.SteamUser, DateTime.UtcNow));
			ZSteamMatchmaking.instance.CheckIfOnlineAsync(server.SteamUser, OnServerPingCompleted);
			break;
		}
		case ServerJoinDataType.Dedicated:
		{
			if (!MultiBackendMatchmaking.GetServerIPCached(server.Dedicated, out var address))
			{
				return false;
			}
			m_activeDedicatedSearch = server;
			m_activeDedicatedSearchCallback = completedCallback;
			if (address.HasValue)
			{
				ZSteamMatchmaking.instance.CheckIfOnlineAsync(new IPEndPoint(address.Value, m_activeDedicatedSearch.Dedicated.m_port), OnServerPingCompleted);
			}
			else
			{
				OnServerPingCompleted(new ServerData(m_activeDedicatedSearch, new ServerMatchmakingData(DateTime.UtcNow)));
			}
			break;
		}
		default:
			return false;
		}
		return true;
	}

	public bool IsPending(ServerJoinData server)
	{
		return server.m_type switch
		{
			ServerJoinDataType.SteamUser => m_activeUserHostedSearches.ContainsKey(server.SteamUser.m_joinUserID), 
			ServerJoinDataType.Dedicated => m_activeDedicatedSearch == server, 
			_ => false, 
		};
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
		case ServerJoinDataType.SteamUser:
			if (!m_matchmakingDataCacheSteamUser.TryGetValue(server.SteamUser, out value))
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
		if (ZSteamMatchmaking.instance.GetServerListRevision(ref m_eventServerListRevision))
		{
			this.FilteredPublicServerListUpdated?.Invoke();
		}
		CheckServerSearchTimeouts();
	}

	private void CheckServerSearchTimeouts()
	{
		DateTime dateTime = DateTime.UtcNow.AddSeconds(-5.0);
		int num = 0;
		while (num < m_activeUserHostedSearchTimeouts.Count)
		{
			SteamworksUserHostedSearchTimeout steamworksUserHostedSearchTimeout = m_activeUserHostedSearchTimeouts[num];
			if (!(steamworksUserHostedSearchTimeout.m_refreshStartTimeUtc >= dateTime))
			{
				m_activeUserHostedSearchTimeouts.RemoveAt(num);
				num--;
				if (m_activeUserHostedSearches.ContainsKey(steamworksUserHostedSearchTimeout.m_server.m_joinUserID))
				{
					ZSteamMatchmaking.instance.AbortCheckIfOnlineAsync(steamworksUserHostedSearchTimeout.m_server);
				}
				num++;
				continue;
			}
			break;
		}
	}

	private void OnServerPingCompleted(ServerData serverData)
	{
		switch (serverData.m_joinData.m_type)
		{
		case ServerJoinDataType.SteamUser:
		{
			if (!m_activeUserHostedSearches.TryGetValue(serverData.m_joinData.SteamUser.m_joinUserID, out var value))
			{
				ZLog.LogError($"Got ping completed callback for user {serverData.m_joinData.SteamUser} which wasn't currently an active search!");
				break;
			}
			m_activeUserHostedSearches.Remove(serverData.m_joinData.SteamUser.m_joinUserID);
			RemoveTimeout(serverData.m_joinData.SteamUser);
			m_matchmakingDataCacheSteamUser[serverData.m_joinData.SteamUser] = serverData.m_matchmakingData;
			value?.Invoke(serverData);
			break;
		}
		case ServerJoinDataType.Dedicated:
		{
			if (!m_activeDedicatedSearch.IsValid)
			{
				ZLog.LogError("Current was invalid!");
				break;
			}
			if (m_activeDedicatedSearch != serverData.m_joinData)
			{
				ZLog.LogError("Current did not match!");
				break;
			}
			if (!serverData.m_joinData.Dedicated.TryGetIPEndPoint(out var endPoint))
			{
				ZLog.LogError("Failed to get IP endpoint!");
				break;
			}
			m_matchmakingDataCacheDedicated[endPoint] = serverData.m_matchmakingData;
			MatchmakingDataRetrievedHandler activeDedicatedSearchCallback = m_activeDedicatedSearchCallback;
			m_activeDedicatedSearch = ServerJoinData.None;
			m_activeDedicatedSearchCallback = null;
			activeDedicatedSearchCallback?.Invoke(serverData);
			break;
		}
		default:
		{
			ZLog.LogWarning($"Got back unexpected join data type {serverData.m_joinData.m_type}");
			if (serverData.m_matchmakingData.m_hostUser.m_platform != PlatformManager.DistributionPlatform.Platform)
			{
				ZLog.LogError($"User {serverData.m_matchmakingData.m_hostUser} was from unexpected platform {serverData.m_matchmakingData.m_hostUser.m_platform}");
				break;
			}
			if (!serverData.m_matchmakingData.m_hostUser.TryParseAsUInt64(out var result))
			{
				ZLog.LogError($"User {serverData.m_matchmakingData.m_hostUser} couldn't be parsed as a Steam ID!");
				break;
			}
			CSteamID cSteamID = new CSteamID(result);
			if (!m_activeUserHostedSearches.Remove(cSteamID))
			{
				ZLog.LogError("Failed to remove invalid entry from user hosted servers!");
				break;
			}
			ServerJoinDataSteamUser serverJoinDataSteamUser = new ServerJoinDataSteamUser(cSteamID);
			RemoveTimeout(serverJoinDataSteamUser);
			m_matchmakingDataCacheSteamUser[serverJoinDataSteamUser] = new ServerMatchmakingData(DateTime.UtcNow);
			break;
		}
		}
	}

	private void RemoveTimeout(ServerJoinDataSteamUser joinData)
	{
		for (int i = 0; i < m_activeUserHostedSearchTimeouts.Count; i++)
		{
			if (m_activeUserHostedSearchTimeouts[i].m_server == joinData)
			{
				m_activeUserHostedSearchTimeouts.RemoveAt(i);
				break;
			}
		}
	}
}
