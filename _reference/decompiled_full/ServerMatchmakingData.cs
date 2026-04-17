using System;
using System.Text;
using Splatform;

public struct ServerMatchmakingData
{
	public readonly DateTime m_timestampUtc;

	public readonly string m_serverName;

	public readonly uint m_playerCount;

	public readonly uint m_playerLimit;

	public readonly PlatformUserID m_hostUser;

	public readonly GameVersion m_gameVersion;

	public readonly uint m_networkVersion;

	public readonly string m_joinCode;

	public readonly bool m_isPasswordProtected;

	public readonly Platform m_platformRestriction;

	public readonly string[] m_modifiers;

	public readonly OnlineStatus m_onlineStatus;

	public static ServerMatchmakingData None => default(ServerMatchmakingData);

	public bool IsCrossplay => !m_platformRestriction.IsValid;

	public bool IsRestrictedToOwnPlatform => m_platformRestriction == PlatformManager.DistributionPlatform.Platform;

	public bool IsUnjoinable
	{
		get
		{
			if (!m_onlineStatus.IsOnline())
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

	public bool IsValid => m_timestampUtc != default(DateTime);

	public ServerMatchmakingData(DateTime timestampUtc, bool couldCheck = true)
	{
		m_timestampUtc = timestampUtc;
		m_serverName = null;
		m_playerCount = 0u;
		m_playerLimit = 0u;
		m_hostUser = default(PlatformUserID);
		m_gameVersion = default(GameVersion);
		m_networkVersion = 0u;
		m_joinCode = null;
		m_isPasswordProtected = false;
		m_platformRestriction = default(Platform);
		m_modifiers = null;
		m_onlineStatus = ((!couldCheck) ? OnlineStatus.NotAvailable : OnlineStatus.Offline);
	}

	public ServerMatchmakingData(DateTime timestampUtc, string serverName)
	{
		m_timestampUtc = timestampUtc;
		m_serverName = serverName;
		m_playerCount = 0u;
		m_playerLimit = 0u;
		m_hostUser = default(PlatformUserID);
		m_gameVersion = default(GameVersion);
		m_networkVersion = 0u;
		m_joinCode = null;
		m_isPasswordProtected = false;
		m_platformRestriction = default(Platform);
		m_modifiers = null;
		m_onlineStatus = OnlineStatus.Offline;
	}

	public ServerMatchmakingData(DateTime timestampUtc, string serverName, uint playerCount, uint playerLimit, PlatformUserID serverOwner, GameVersion gameVersion, uint networkVersion, string joinCode, bool isPasswordProtected, Platform platformRestriction, string[] modifiers)
	{
		m_timestampUtc = timestampUtc;
		m_serverName = serverName ?? throw new ArgumentNullException("serverName");
		m_playerCount = playerCount;
		m_playerLimit = playerLimit;
		m_hostUser = serverOwner;
		m_gameVersion = gameVersion;
		m_networkVersion = networkVersion;
		m_joinCode = joinCode;
		m_isPasswordProtected = isPasswordProtected;
		m_platformRestriction = platformRestriction;
		m_modifiers = modifiers;
		m_onlineStatus = OnlineStatus.Online;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(string.Format("{0}: {1}", "m_timestampUtc", m_timestampUtc));
		stringBuilder.AppendLine("m_serverName: " + m_serverName);
		stringBuilder.AppendLine(string.Format("{0}: {1}", "m_playerCount", m_playerCount));
		stringBuilder.AppendLine(string.Format("{0}: {1}", "m_playerLimit", m_playerLimit));
		stringBuilder.AppendLine(string.Format("{0}: {1}", "m_hostUser", m_hostUser));
		stringBuilder.AppendLine(string.Format("{0}: {1}", "m_gameVersion", m_gameVersion));
		stringBuilder.AppendLine(string.Format("{0}: {1}", "m_networkVersion", m_networkVersion));
		stringBuilder.AppendLine("m_joinCode: " + m_joinCode);
		stringBuilder.AppendLine(string.Format("{0}: {1}", "m_isPasswordProtected", m_isPasswordProtected));
		stringBuilder.AppendLine(string.Format("{0}: {1}", "m_platformRestriction", m_platformRestriction));
		stringBuilder.AppendLine("m_modifiers:");
		for (int i = 0; i < m_modifiers.Length; i++)
		{
			stringBuilder.AppendLine("\t" + m_modifiers[i]);
		}
		stringBuilder.AppendLine(string.Format("{0}: {1}", "m_onlineStatus", m_onlineStatus));
		return stringBuilder.ToString();
	}
}
