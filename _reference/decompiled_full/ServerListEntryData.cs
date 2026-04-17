using System;
using Splatform;

public struct ServerListEntryData : IEquatable<ServerListEntryData>
{
	public readonly ServerJoinData m_joinData;

	public readonly ServerListEntryDataFlags m_flags;

	public readonly string m_serverName;

	public readonly uint m_playerCount;

	public readonly uint m_playerLimit;

	public readonly GameVersion m_gameVersion;

	public readonly string[] m_modifiers;

	public readonly DateTime m_timeStampUtc;

	public bool HasMatchmakingData => m_flags.HasFlag(ServerListEntryDataFlags.HasMatchmakingData);

	public bool IsAvailable => m_flags.HasFlag(ServerListEntryDataFlags.IsAvailable);

	public bool IsOnline => m_flags.HasFlag(ServerListEntryDataFlags.IsOnline);

	public bool IsCrossplay => m_flags.HasFlag(ServerListEntryDataFlags.IsCrossplay);

	public bool IsRestrictedToOwnPlatform => m_flags.HasFlag(ServerListEntryDataFlags.IsRestrictedToOwnPlatform);

	public bool IsPasswordProtected => m_flags.HasFlag(ServerListEntryDataFlags.IsPasswordProtected);

	public bool IsUnjoinable
	{
		get
		{
			if (!IsOnline)
			{
				return false;
			}
			if (IsRestrictedToOwnPlatform)
			{
				return false;
			}
			if (!IsCrossplay)
			{
				return true;
			}
			return PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.CrossPlatformMultiplayer) != PrivilegeResult.Granted;
		}
	}

	public static ServerListEntryData None => default(ServerListEntryData);

	public ServerListEntryData(ServerData serverData, string serverName = null)
	{
		this = default(ServerListEntryData);
		m_joinData = serverData.m_joinData;
		if (serverName == null)
		{
			m_serverName = serverData.m_matchmakingData.m_serverName;
		}
		else
		{
			m_serverName = serverName;
		}
		m_flags = ServerListEntryDataFlags.None;
		if (!serverData.m_matchmakingData.IsValid)
		{
			return;
		}
		m_flags |= ServerListEntryDataFlags.HasMatchmakingData;
		m_timeStampUtc = serverData.m_matchmakingData.m_timestampUtc;
		if (serverData.m_matchmakingData.m_onlineStatus == OnlineStatus.NotAvailable)
		{
			return;
		}
		m_flags |= ServerListEntryDataFlags.IsAvailable;
		if (serverData.m_matchmakingData.m_onlineStatus.IsOnline())
		{
			m_flags |= ServerListEntryDataFlags.IsOnline;
			if (serverData.m_matchmakingData.IsCrossplay)
			{
				m_flags |= ServerListEntryDataFlags.IsCrossplay;
			}
			else if (serverData.m_matchmakingData.IsRestrictedToOwnPlatform)
			{
				m_flags |= ServerListEntryDataFlags.IsRestrictedToOwnPlatform;
			}
			if (serverData.m_matchmakingData.m_isPasswordProtected)
			{
				m_flags |= ServerListEntryDataFlags.IsPasswordProtected;
			}
			m_playerCount = serverData.m_matchmakingData.m_playerCount;
			if (serverData.m_matchmakingData.m_networkVersion < 35)
			{
				m_playerLimit = 10u;
			}
			else
			{
				m_playerLimit = serverData.m_matchmakingData.m_playerLimit;
			}
			m_gameVersion = serverData.m_matchmakingData.m_gameVersion;
			m_modifiers = serverData.m_matchmakingData.m_modifiers;
		}
	}

	public bool Equals(ref ServerListEntryData other)
	{
		if (m_joinData.Equals(other.m_joinData) && m_flags.Equals(other.m_flags) && !(m_serverName != other.m_serverName))
		{
			uint playerCount = m_playerCount;
			if (playerCount.Equals(other.m_playerCount))
			{
				playerCount = m_playerLimit;
				if (playerCount.Equals(other.m_playerLimit) && m_gameVersion.Equals(other.m_gameVersion) && (m_modifiers == null).Equals(other.m_modifiers == null))
				{
					if (m_modifiers == null)
					{
						return true;
					}
					if (m_modifiers.Length != other.m_modifiers.Length)
					{
						return false;
					}
					for (int i = 0; i < m_modifiers.Length; i++)
					{
						if (!m_modifiers[i].Equals(other.m_modifiers[i]))
						{
							return false;
						}
					}
					return true;
				}
			}
		}
		return false;
	}

	public bool Equals(ServerListEntryData other)
	{
		return Equals(ref other);
	}
}
