public static class ServerJoinDataTypeExtentions
{
	public static string ServerTypeDisplayName(this ServerJoinDataType joinDataType)
	{
		return joinDataType switch
		{
			ServerJoinDataType.SteamUser => "Steam", 
			ServerJoinDataType.PlayFabUser => "PlayFab", 
			_ => joinDataType.ToString(), 
		};
	}

	public static bool DisplayUnderlyingDataToUser(this ServerJoinDataType joinDataType)
	{
		switch (joinDataType)
		{
		case ServerJoinDataType.SteamUser:
		case ServerJoinDataType.Dedicated:
			return true;
		default:
			return false;
		}
	}
}
