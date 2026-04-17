using System;
using System.Collections.Generic;

public class CommunityServerList : IServerList
{
	private const float c_ServerSideFilterCooldownSeconds = 0.5f;

	private const int c_MaxServers = 200;

	private readonly string m_displayName;

	private string m_filter = string.Empty;

	private string m_lastSearchedFilter = string.Empty;

	private DateTime m_filterLastChangedUtc = DateTime.MinValue;

	private DateTime m_lastRefreshedTimeUtc = DateTime.MinValue;

	private bool m_needsRefresh = true;

	private readonly Dictionary<ServerJoinData, int> m_tempServerJoinDataToIndexInFilteredList = new Dictionary<ServerJoinData, int>(200);

	public string DisplayName => m_displayName;

	public DateTime LastRefreshTimeUtc
	{
		get
		{
			if (m_needsRefresh)
			{
				return DateTime.UtcNow;
			}
			return m_lastRefreshedTimeUtc;
		}
	}

	public bool CanRefresh => true;

	public uint TotalServers
	{
		get
		{
			uint num = 0u;
			IReadOnlyList<IMatchmakingBackend> backends = MultiBackendMatchmaking.Instance.Backends;
			for (int i = 0; i < backends.Count; i++)
			{
				num += backends[i].PublicServerCount;
			}
			return num;
		}
	}

	public event ServerListUpdatedHandler ServerListUpdated;

	public CommunityServerList(string displayName)
	{
		m_displayName = displayName;
	}

	public void Refresh()
	{
		m_needsRefresh = true;
	}

	private void RefreshInternal()
	{
		m_needsRefresh = false;
		m_lastSearchedFilter = m_filter;
		m_lastRefreshedTimeUtc = DateTime.UtcNow;
		IReadOnlyList<IMatchmakingBackend> backends = MultiBackendMatchmaking.Instance.Backends;
		for (int i = 0; i < backends.Count; i++)
		{
			backends[i].RefreshPublicServerList();
		}
	}

	private void RefreshServerSideFilter()
	{
		m_lastSearchedFilter = m_filter;
		m_lastRefreshedTimeUtc = DateTime.UtcNow;
		IReadOnlyList<IMatchmakingBackend> backends = MultiBackendMatchmaking.Instance.Backends;
		for (int i = 0; i < backends.Count; i++)
		{
			IMatchmakingBackend matchmakingBackend = backends[i];
			if (matchmakingBackend.ServerSideFiltering)
			{
				matchmakingBackend.RefreshPublicServerList(RefreshPublicServerListFlags.ServerSideFilterRefresh);
			}
		}
	}

	public void SetFilter(string filter, bool isTyping = false)
	{
		if (filter == null)
		{
			filter = string.Empty;
		}
		if (!(filter == m_filter))
		{
			m_filter = filter;
			m_filterLastChangedUtc = (isTyping ? DateTime.UtcNow : DateTime.MaxValue);
			IReadOnlyList<IMatchmakingBackend> backends = MultiBackendMatchmaking.Instance.Backends;
			for (int i = 0; i < backends.Count; i++)
			{
				backends[i].SetPublicServerListFilter(filter);
			}
		}
	}

	public void GetFilteredList(List<ServerListEntryData> resultOutput)
	{
		resultOutput.Clear();
		List<IMatchmakingBackend> list = new List<IMatchmakingBackend>(MultiBackendMatchmaking.Instance.Backends);
		int num = 0;
		int num2 = 0;
		while (resultOutput.Count < 200)
		{
			IReadOnlyList<ServerData> filteredPublicServerList = list[num2].FilteredPublicServerList;
			if (num >= filteredPublicServerList.Count)
			{
				if (list.Count <= 1)
				{
					break;
				}
				list.RemoveAt(num2);
				num2--;
			}
			else
			{
				ServerData serverData = filteredPublicServerList[num];
				if (m_tempServerJoinDataToIndexInFilteredList.TryGetValue(serverData.m_joinData, out var value))
				{
					if (resultOutput[value].m_timeStampUtc < serverData.m_matchmakingData.m_timestampUtc)
					{
						resultOutput[value] = new ServerListEntryData(serverData);
					}
				}
				else
				{
					m_tempServerJoinDataToIndexInFilteredList.Add(serverData.m_joinData, resultOutput.Count);
					resultOutput.Add(new ServerListEntryData(serverData));
				}
			}
			num2++;
			if (num2 >= list.Count)
			{
				num2 = 0;
				num++;
			}
		}
		m_tempServerJoinDataToIndexInFilteredList.Clear();
		resultOutput.Sort((ServerListEntryData a, ServerListEntryData b) => a.m_serverName.CompareTo(b.m_serverName));
	}

	public void OnOpen()
	{
		IReadOnlyList<IMatchmakingBackend> backends = MultiBackendMatchmaking.Instance.Backends;
		for (int i = 0; i < backends.Count; i++)
		{
			backends[i].FilteredPublicServerListUpdated += OnBackendServerListUpdated;
		}
		if (m_needsRefresh)
		{
			Refresh();
		}
	}

	public void OnClose()
	{
		IReadOnlyList<IMatchmakingBackend> backends = MultiBackendMatchmaking.Instance.Backends;
		for (int i = 0; i < backends.Count; i++)
		{
			backends[i].FilteredPublicServerListUpdated -= OnBackendServerListUpdated;
		}
	}

	public void Tick()
	{
		RefreshIfNeeded();
		RefreshByFilterIfNeeded();
		void RefreshByFilterIfNeeded()
		{
			if (!(m_filter == m_lastSearchedFilter) && !((DateTime.UtcNow - m_filterLastChangedUtc).TotalSeconds < 0.5))
			{
				RefreshServerSideFilter();
			}
		}
		void RefreshIfNeeded()
		{
			if (m_needsRefresh)
			{
				RefreshInternal();
			}
		}
	}

	private void OnBackendServerListUpdated()
	{
		this.ServerListUpdated?.Invoke();
	}
}
