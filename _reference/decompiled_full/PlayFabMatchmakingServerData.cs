using System;
using System.Collections.Generic;
using Splatform;

public class PlayFabMatchmakingServerData : IEquatable<PlayFabMatchmakingServerData>
{
	public string serverName;

	public string worldName;

	public GameVersion gameVersion;

	public string[] modifiers;

	public uint networkVersion;

	public string networkId = "";

	public string joinCode;

	public string remotePlayerId;

	public string lobbyId;

	public PlatformUserID platformUserID;

	public string serverIp = "";

	public Platform platformRestriction = Platform.Unknown;

	public bool isDedicatedServer;

	public bool isCommunityServer;

	public bool havePassword;

	public uint numPlayers;

	public uint maxNumPlayers;

	public long tickCreated;

	public override bool Equals(object obj)
	{
		if (obj is PlayFabMatchmakingServerData other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(PlayFabMatchmakingServerData other)
	{
		if (remotePlayerId == other.remotePlayerId && serverIp == other.serverIp)
		{
			return isDedicatedServer == other.isDedicatedServer;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((1416698207 * -1521134295 + EqualityComparer<string>.Default.GetHashCode(remotePlayerId)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(serverIp)) * -1521134295 + isDedicatedServer.GetHashCode();
	}

	public override string ToString()
	{
		return "Server Name : " + serverName + "\nServer IP : " + serverIp + "\n" + $"Game Version : {gameVersion}\n" + $"Network Version : {networkVersion}\n" + "Player ID : " + remotePlayerId + "\n" + $"Players : {numPlayers}\n" + $"Max players : {maxNumPlayers}\n" + "Lobby ID : " + lobbyId + "\nNetwork ID : " + networkId + "\nJoin Code : " + joinCode + "\n" + $"Platform Restriction : {platformRestriction}\n" + $"Dedicated : {isDedicatedServer}\n" + $"Community : {isCommunityServer}\n" + $"TickCreated : {tickCreated}\n" + $"Modifiers : {modifiers}\n";
	}

	public ServerData ToServerData(DateTime timestampUtc)
	{
		ServerJoinData joinData = ((!isDedicatedServer) ? new ServerJoinData(new ServerJoinDataPlayFabUser(remotePlayerId)) : new ServerJoinData(new ServerJoinDataDedicated(serverIp)));
		return new ServerData(joinData, ToServerMatchmakingData(timestampUtc));
	}

	public ServerMatchmakingData ToServerMatchmakingData(DateTime timestampUtc)
	{
		return new ServerMatchmakingData(timestampUtc, serverName, numPlayers, maxNumPlayers, platformUserID, gameVersion, networkVersion, joinCode, isPasswordProtected: true, platformRestriction, modifiers);
	}
}
