using System;
using System.Collections.Generic;

public interface IServerList
{
	string DisplayName { get; }

	DateTime LastRefreshTimeUtc { get; }

	bool CanRefresh { get; }

	uint TotalServers { get; }

	event ServerListUpdatedHandler ServerListUpdated;

	void Refresh();

	void SetFilter(string filter, bool isTyping = false);

	void GetFilteredList(List<ServerListEntryData> resultOutput);

	void OnOpen();

	void Tick();

	void OnClose();
}
