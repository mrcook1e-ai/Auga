public struct ServerData
{
	public readonly ServerJoinData m_joinData;

	public readonly ServerMatchmakingData m_matchmakingData;

	public static ServerData None => default(ServerData);

	public ServerData(ServerJoinData joinData)
	{
		m_joinData = joinData;
		m_matchmakingData = ServerMatchmakingData.None;
	}

	public ServerData(ServerJoinData joinData, ServerMatchmakingData matchmakingData)
	{
		m_joinData = joinData;
		m_matchmakingData = matchmakingData;
	}
}
