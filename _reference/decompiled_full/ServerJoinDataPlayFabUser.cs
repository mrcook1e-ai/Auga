using System;

public struct ServerJoinDataPlayFabUser(string remotePlayerId) : IEquatable<ServerJoinDataPlayFabUser>
{
	public const string c_TypeName = "PlayFab user";

	public readonly string m_remotePlayerId = remotePlayerId;

	public bool IsValid => m_remotePlayerId != null;

	public string GetDataName()
	{
		return "PlayFab user";
	}

	public override bool Equals(object obj)
	{
		if (obj != null && obj is ServerJoinDataPlayFabUser other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(ServerJoinDataPlayFabUser other)
	{
		return ToString().Equals(other.ToString());
	}

	public override int GetHashCode()
	{
		return 1688301347 * -1521134295 + ToString().GetHashCode();
	}

	public static bool operator ==(ServerJoinDataPlayFabUser left, ServerJoinDataPlayFabUser right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ServerJoinDataPlayFabUser left, ServerJoinDataPlayFabUser right)
	{
		return !(left == right);
	}

	public override string ToString()
	{
		return m_remotePlayerId;
	}
}
