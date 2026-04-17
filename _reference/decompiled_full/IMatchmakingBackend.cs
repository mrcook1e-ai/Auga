using System;
using System.Collections.Generic;

public interface IMatchmakingBackend : IDisposable
{
	bool IsAvailable { get; }

	uint PublicServerCount { get; }

	IReadOnlyList<ServerData> FilteredPublicServerList { get; }

	bool ServerSideFiltering { get; }

	bool IsRefreshingPublicServerList { get; }

	event PublicServerListUpdatedHandler FilteredPublicServerListUpdated;

	void RefreshPublicServerList(RefreshPublicServerListFlags flags = RefreshPublicServerListFlags.None);

	void SetPublicServerListFilter(string filter);

	bool CanRefreshServerNow();

	bool CanRefreshServerOfTypeNow(ServerJoinDataType type);

	bool RefreshServer(ServerJoinData server, MatchmakingDataRetrievedHandler callback);

	bool IsPending(ServerJoinData server);

	ServerMatchmakingData GetServerMatchmakingData(ServerJoinData server, DateTime newerThanUtc = default(DateTime));

	void Tick();
}
