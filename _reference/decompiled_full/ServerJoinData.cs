using System;
using Splatform;

public struct ServerJoinData : IEquatable<ServerJoinData>
{
	public PlatformUserID m_owner;

	public readonly ServerJoinDataType m_type;

	private readonly ServerJoinDataSteamUser m_steamUser;

	private readonly ServerJoinDataPlayFabUser m_playFabUser;

	private readonly ServerJoinDataDedicated m_dedicated;

	public static ServerJoinData None => default(ServerJoinData);

	public ServerJoinDataSteamUser SteamUser
	{
		get
		{
			if (m_type != ServerJoinDataType.SteamUser)
			{
				throw new InvalidOperationException($"Can't get {ServerJoinDataType.SteamUser} server data when type is {m_type}.");
			}
			return m_steamUser;
		}
	}

	public ServerJoinDataPlayFabUser PlayFabUser
	{
		get
		{
			if (m_type != ServerJoinDataType.PlayFabUser)
			{
				throw new InvalidOperationException($"Can't get {ServerJoinDataType.PlayFabUser} server data when type is {m_type}.");
			}
			return m_playFabUser;
		}
	}

	public ServerJoinDataDedicated Dedicated
	{
		get
		{
			if (m_type != ServerJoinDataType.Dedicated)
			{
				throw new InvalidOperationException($"Can't get {ServerJoinDataType.Dedicated} server data when type is {m_type}.");
			}
			return m_dedicated;
		}
	}

	public bool IsValid
	{
		get
		{
			if (m_type == ServerJoinDataType.None)
			{
				return false;
			}
			return m_type switch
			{
				ServerJoinDataType.SteamUser => m_steamUser.IsValid, 
				ServerJoinDataType.PlayFabUser => m_playFabUser.IsValid, 
				ServerJoinDataType.Dedicated => true, 
				_ => throw new NotImplementedException($"No valid check for server join data type \"{m_type}\""), 
			};
		}
	}

	public ServerJoinData(ServerJoinDataSteamUser steam)
	{
		m_playFabUser = default(ServerJoinDataPlayFabUser);
		m_dedicated = default(ServerJoinDataDedicated);
		m_type = ServerJoinDataType.SteamUser;
		m_steamUser = steam;
		m_owner = new PlatformUserID(new Platform("Steam"), m_steamUser.m_joinUserID.m_SteamID);
	}

	public ServerJoinData(ServerJoinDataPlayFabUser playfab, PlatformUserID owner)
	{
		m_steamUser = default(ServerJoinDataSteamUser);
		m_dedicated = default(ServerJoinDataDedicated);
		m_type = ServerJoinDataType.PlayFabUser;
		m_playFabUser = playfab;
		m_owner = owner;
	}

	public ServerJoinData(ServerJoinDataPlayFabUser playfab)
	{
		m_steamUser = default(ServerJoinDataSteamUser);
		m_dedicated = default(ServerJoinDataDedicated);
		m_type = ServerJoinDataType.PlayFabUser;
		m_playFabUser = playfab;
		m_owner = default(PlatformUserID);
	}

	public ServerJoinData(ServerJoinDataDedicated dedicated)
	{
		m_steamUser = default(ServerJoinDataSteamUser);
		m_playFabUser = default(ServerJoinDataPlayFabUser);
		m_type = ServerJoinDataType.Dedicated;
		m_dedicated = dedicated;
		m_owner = default(PlatformUserID);
	}

	public string GetDataName()
	{
		return m_type switch
		{
			ServerJoinDataType.SteamUser => m_steamUser.GetDataName(), 
			ServerJoinDataType.PlayFabUser => m_playFabUser.GetDataName(), 
			ServerJoinDataType.Dedicated => m_dedicated.GetDataName(), 
			_ => throw new NotImplementedException($"No data name for server join data type \"{m_type}\""), 
		};
	}

	public override bool Equals(object obj)
	{
		if (obj != null && obj is ServerJoinData other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(ServerJoinData other)
	{
		if (m_type != other.m_type)
		{
			return false;
		}
		return m_type switch
		{
			ServerJoinDataType.None => true, 
			ServerJoinDataType.SteamUser => m_steamUser == other.m_steamUser, 
			ServerJoinDataType.PlayFabUser => m_playFabUser == other.m_playFabUser, 
			ServerJoinDataType.Dedicated => m_dedicated == other.m_dedicated, 
			_ => throw new NotImplementedException($"No equality check for server join data type \"{m_type}\""), 
		};
	}

	public override int GetHashCode()
	{
		int num = m_type.GetHashCode();
		switch (m_type)
		{
		case ServerJoinDataType.SteamUser:
			num = HashCode.Combine(num, m_steamUser.GetHashCode());
			break;
		case ServerJoinDataType.PlayFabUser:
			num = HashCode.Combine(num, m_playFabUser.GetHashCode());
			break;
		case ServerJoinDataType.Dedicated:
			num = HashCode.Combine(num, m_dedicated.GetHashCode());
			break;
		}
		return num;
	}

	public static bool operator ==(ServerJoinData lhs, ServerJoinData rhs)
	{
		return lhs.Equals(rhs);
	}

	public static bool operator !=(ServerJoinData lhs, ServerJoinData rhs)
	{
		return !(lhs == rhs);
	}

	public override string ToString()
	{
		return m_type switch
		{
			ServerJoinDataType.SteamUser => m_steamUser.ToString(), 
			ServerJoinDataType.PlayFabUser => m_playFabUser.ToString(), 
			ServerJoinDataType.Dedicated => m_dedicated.ToString(), 
			_ => "", 
		};
	}
}
