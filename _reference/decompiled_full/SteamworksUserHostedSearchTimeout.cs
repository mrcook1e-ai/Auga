using System;

public struct SteamworksUserHostedSearchTimeout(ServerJoinDataSteamUser server, DateTime refreshStartTimeUtc)
{
	public ServerJoinDataSteamUser m_server = server;

	public DateTime m_refreshStartTimeUtc = refreshStartTimeUtc;
}
