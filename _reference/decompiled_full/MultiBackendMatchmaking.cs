using System;
using System.Collections.Generic;
using NetworkingUtils;
using UnityEngine;

public class MultiBackendMatchmaking : MonoBehaviour
{
	private static MultiBackendMatchmaking s_instance;

	private uint m_referenceCounter;

	private List<IMatchmakingBackend> m_backends;

	public readonly Dictionary<ServerJoinData, ServerNameAtTimePoint> m_serverNames = new Dictionary<ServerJoinData, ServerNameAtTimePoint>();

	public readonly DnsResolver m_dnsResolver = new DnsResolver();

	public static MultiBackendMatchmaking Instance => s_instance;

	public IReadOnlyList<IMatchmakingBackend> Backends => m_backends;

	public static PlayFabMatchmaking PlayFabBackend
	{
		get
		{
			if ((object)s_instance == null)
			{
				ZLog.LogError("s_instance was null!");
				return null;
			}
			return s_instance.m_backends[0] as PlayFabMatchmaking;
		}
	}

	public static void Hold()
	{
		if ((object)s_instance == null)
		{
			GameObject obj = new GameObject("MultiBackendMatchmaking", typeof(MultiBackendMatchmaking));
			s_instance = obj.GetComponent<MultiBackendMatchmaking>();
			UnityEngine.Object.DontDestroyOnLoad(obj);
		}
		s_instance.m_referenceCounter++;
	}

	public static void Release()
	{
		if (!(s_instance == null))
		{
			s_instance.m_referenceCounter--;
			if (s_instance.m_referenceCounter == 0)
			{
				UnityEngine.Object.Destroy(s_instance.gameObject);
				s_instance = null;
			}
		}
	}

	private MultiBackendMatchmaking()
	{
		m_backends = new List<IMatchmakingBackend>
		{
			new PlayFabMatchmaking(),
			new SteamworksMatchmaking()
		};
	}

	private void OnDestroy()
	{
		for (int i = 0; i < m_backends.Count; i++)
		{
			m_backends[i].Dispose();
		}
		m_backends.Clear();
	}

	private void Update()
	{
		for (int i = 0; i < m_backends.Count; i++)
		{
			m_backends[i].Tick();
		}
	}

	public static ServerMatchmakingData GetServerMatchmakingData(ServerJoinData server, DateTime newerThanUtc = default(DateTime))
	{
		if ((object)s_instance == null)
		{
			ZLog.LogError(string.Format("{0} was null! Couldn't get server matchmaking data for server {1}, returning {2} as a fallback.", "s_instance", server, "None"));
			return ServerMatchmakingData.None;
		}
		if (!server.IsValid)
		{
			throw new ArgumentException("Server has to be valid!");
		}
		int num = 0;
		int num2 = 0;
		ServerMatchmakingData result = ServerMatchmakingData.None;
		for (int i = 0; i < s_instance.m_backends.Count; i++)
		{
			if (!s_instance.m_backends[i].IsAvailable)
			{
				continue;
			}
			num++;
			ServerMatchmakingData serverMatchmakingData = s_instance.m_backends[i].GetServerMatchmakingData(server, newerThanUtc);
			if (serverMatchmakingData.IsValid)
			{
				num2++;
				if (!result.IsValid || result.m_onlineStatus < serverMatchmakingData.m_onlineStatus || (serverMatchmakingData.m_onlineStatus.IsOnline() && serverMatchmakingData.m_timestampUtc <= result.m_timestampUtc))
				{
					result = serverMatchmakingData;
				}
				if (result.m_onlineStatus.IsOnline())
				{
					break;
				}
			}
		}
		if (!result.m_onlineStatus.IsOnline() && num2 < num)
		{
			return ServerMatchmakingData.None;
		}
		return result;
	}

	public static string GetServerName(ServerJoinData server)
	{
		if (!TryGetServerName(server, out var serverName))
		{
			return server.ToString();
		}
		return serverName;
	}

	public static bool TryGetServerName(ServerJoinData server, out string serverName)
	{
		ServerNameAtTimePoint serverNameAtTimePoint;
		ServerNameSource source;
		bool result = TryGetServerName(server, out serverNameAtTimePoint, out source);
		serverName = serverNameAtTimePoint.m_name;
		return result;
	}

	public static bool TryGetServerName(ServerJoinData server, out ServerNameAtTimePoint serverNameAtTimePoint, out ServerNameSource source)
	{
		if (!server.IsValid)
		{
			throw new ArgumentException("Server has to be valid!");
		}
		source = ServerNameSource.ManuallySet;
		serverNameAtTimePoint = default(ServerNameAtTimePoint);
		if ((object)s_instance == null)
		{
			ZLog.LogError(string.Format("{0} was null! Couldn't get server name for server {1}.", "s_instance", server));
			return false;
		}
		if (s_instance.m_serverNames.TryGetValue(server, out serverNameAtTimePoint))
		{
			source = ServerNameSource.ManuallySet;
		}
		else
		{
			serverNameAtTimePoint = ServerNameAtTimePoint.None;
		}
		for (int i = 0; i < s_instance.m_backends.Count; i++)
		{
			ServerMatchmakingData serverMatchmakingData = s_instance.m_backends[i].GetServerMatchmakingData(server);
			if (serverMatchmakingData.m_onlineStatus.IsOnline() && !(serverMatchmakingData.m_timestampUtc <= serverNameAtTimePoint.m_timestampUtc))
			{
				serverNameAtTimePoint = new ServerNameAtTimePoint(serverMatchmakingData.m_serverName, serverMatchmakingData.m_timestampUtc);
				source = ServerNameSource.Matchmaking;
			}
		}
		return serverNameAtTimePoint.m_name != null;
	}

	public static void SetServerName(ServerJoinData server, ServerNameAtTimePoint nameAndFileTimestamp)
	{
		ServerNameAtTimePoint value;
		if ((object)s_instance == null)
		{
			ZLog.LogError(string.Format("{0} was null! Couldn't set server name for server {1}, doing nothing as a fallback.", "s_instance", server));
		}
		else if (!s_instance.m_serverNames.TryGetValue(server, out value) || !(value.m_timestampUtc > nameAndFileTimestamp.m_timestampUtc))
		{
			s_instance.m_serverNames[server] = nameAndFileTimestamp;
		}
	}

	public static void GetServerIPAsync(ServerJoinDataDedicated server, ResolveDomainCompletedHandler completedHandler)
	{
		if ((object)s_instance == null)
		{
			ZLog.LogError("s_instance was null! If the IP address is not already part of the join data, the operation will fail.");
		}
		if (server.TryGetIPAddress(out var address))
		{
			completedHandler?.Invoke(succeeded: true, address);
		}
		else if ((object)s_instance == null)
		{
			completedHandler?.Invoke(succeeded: false, null);
		}
		else
		{
			s_instance.m_dnsResolver.ResolveDomainNameAsync(server.m_host, completedHandler);
		}
	}

	public static bool GetServerIPCached(ServerJoinDataDedicated server, out IPv6Address? address)
	{
		if ((object)s_instance == null)
		{
			ZLog.LogError("s_instance was null! If the IP address is not already part of the join data, the operation will fail.");
		}
		if (server.TryGetIPAddress(out var address2))
		{
			address = address2;
			return true;
		}
		if ((object)s_instance == null)
		{
			address = null;
			return false;
		}
		return s_instance.m_dnsResolver.ResolveDomainName(server.m_host, out address, DnsResolveFlags.CacheOnly);
	}

	public static bool ServerIPAddressIsKnown(ServerJoinDataDedicated server)
	{
		IPv6Address? address;
		return GetServerIPCached(server, out address);
	}
}
