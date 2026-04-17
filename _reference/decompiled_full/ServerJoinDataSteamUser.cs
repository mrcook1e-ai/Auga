using System;
using Steamworks;

public struct ServerJoinDataSteamUser : IEquatable<ServerJoinDataSteamUser>
{
	public const string c_TypeName = "Steam user";

	public readonly CSteamID m_joinUserID;

	public bool IsValid => m_joinUserID.IsValid();

	public ServerJoinDataSteamUser(ulong joinUserID)
	{
		m_joinUserID = new CSteamID(joinUserID);
	}

	public ServerJoinDataSteamUser(CSteamID joinUserID)
	{
		m_joinUserID = joinUserID;
	}

	public string GetDataName()
	{
		return "Steam user";
	}

	public override bool Equals(object obj)
	{
		if (obj != null && obj is ServerJoinDataSteamUser other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(ServerJoinDataSteamUser other)
	{
		return m_joinUserID.Equals(other.m_joinUserID);
	}

	public override int GetHashCode()
	{
		return -995281327 * -1521134295 + m_joinUserID.GetHashCode();
	}

	public static bool operator ==(ServerJoinDataSteamUser left, ServerJoinDataSteamUser right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ServerJoinDataSteamUser left, ServerJoinDataSteamUser right)
	{
		return !(left == right);
	}

	public override string ToString()
	{
		return m_joinUserID.ToString();
	}
}
