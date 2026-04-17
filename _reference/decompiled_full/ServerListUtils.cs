using System;
using System.Collections.Generic;

public static class ServerListUtils
{
	public static uint[] FairSplit(uint[] entryCounts, uint maxEntries)
	{
		uint num = 0u;
		uint num2 = 0u;
		for (int i = 0; i < entryCounts.Length; i++)
		{
			num += entryCounts[i];
			if (entryCounts[i] != 0)
			{
				num2++;
			}
		}
		if (num <= maxEntries)
		{
			return entryCounts;
		}
		uint[] array = new uint[entryCounts.Length];
		while (num2 != 0)
		{
			uint num3 = maxEntries / num2;
			if (num3 != 0)
			{
				for (int j = 0; j < entryCounts.Length; j++)
				{
					if (entryCounts[j] != 0)
					{
						if (entryCounts[j] > num3)
						{
							array[j] += num3;
							maxEntries -= num3;
							entryCounts[j] -= num3;
						}
						else
						{
							array[j] += entryCounts[j];
							maxEntries -= entryCounts[j];
							entryCounts[j] = 0u;
							num2--;
						}
					}
				}
				continue;
			}
			uint num4 = 0u;
			for (int k = 0; k < maxEntries; k++)
			{
				if (entryCounts[num4] != 0)
				{
					array[num4]++;
				}
				else
				{
					k--;
				}
				num4++;
			}
			maxEntries = 0u;
			break;
		}
		return array;
	}

	public static void GetFilteredList(IReadOnlyList<ServerJoinData> unfilteredList, string filter, List<ServerJoinData> resultOutput)
	{
		if (filter.Length <= 0)
		{
			resultOutput.AddRange(unfilteredList);
			return;
		}
		string value = filter.ToLowerInvariant();
		for (int i = 0; i < unfilteredList.Count; i++)
		{
			if (MultiBackendMatchmaking.TryGetServerName(unfilteredList[i], out var serverName) && serverName.ToLowerInvariant().Contains(value))
			{
				resultOutput.Add(unfilteredList[i]);
			}
		}
	}

	public static void GetFilteredList(IReadOnlyList<ServerData> unfilteredList, string filterLowerInvariant, List<ServerData> resultOutput)
	{
		for (int i = 0; i < unfilteredList.Count; i++)
		{
			if (unfilteredList[i].m_matchmakingData.m_serverName.ToLowerInvariant().Contains(filterLowerInvariant))
			{
				resultOutput.Add(unfilteredList[i]);
			}
		}
	}

	public static void UpdateServerOnlineStatus(IReadOnlyList<ServerJoinData> servers, DateTime newerThanUtc, MatchmakingDataRetrievedHandler matchmakingDataRetrievedHandler)
	{
		if (servers.Count <= 0)
		{
			return;
		}
		IReadOnlyList<IMatchmakingBackend> backends = MultiBackendMatchmaking.Instance.Backends;
		for (int i = 0; i < backends.Count; i++)
		{
			IMatchmakingBackend matchmakingBackend = backends[i];
			if (!matchmakingBackend.IsAvailable || !matchmakingBackend.CanRefreshServerNow())
			{
				continue;
			}
			for (int j = 0; j < servers.Count; j++)
			{
				ServerJoinData server = servers[j];
				if (!server.IsValid || !matchmakingBackend.CanRefreshServerOfTypeNow(server.m_type) || (server.m_type == ServerJoinDataType.Dedicated && !MultiBackendMatchmaking.ServerIPAddressIsKnown(server.Dedicated)) || matchmakingBackend.IsPending(server))
				{
					continue;
				}
				ServerMatchmakingData serverMatchmakingData = matchmakingBackend.GetServerMatchmakingData(server, newerThanUtc);
				if (serverMatchmakingData.IsValid && serverMatchmakingData.m_timestampUtc > newerThanUtc)
				{
					continue;
				}
				bool flag = false;
				for (int k = 0; k < backends.Count; k++)
				{
					if (i == k)
					{
						continue;
					}
					IMatchmakingBackend matchmakingBackend2 = backends[k];
					if (matchmakingBackend2.IsAvailable)
					{
						if (matchmakingBackend2.IsPending(server))
						{
							flag = true;
							break;
						}
						ServerMatchmakingData serverMatchmakingData2 = matchmakingBackend2.GetServerMatchmakingData(server, newerThanUtc);
						if (serverMatchmakingData2.m_onlineStatus.IsOnline() && serverMatchmakingData2.m_timestampUtc >= newerThanUtc)
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag && matchmakingBackend.RefreshServer(server, matchmakingDataRetrievedHandler) && !matchmakingBackend.CanRefreshServerNow())
				{
					break;
				}
			}
		}
	}
}
