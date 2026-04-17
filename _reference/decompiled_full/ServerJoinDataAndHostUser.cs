using Splatform;

internal struct ServerJoinDataAndHostUser(ServerJoinData joinData, PlatformUserID hostUser)
{
	public ServerJoinData m_joinData = joinData;

	public PlatformUserID m_hostUser = hostUser;
}
