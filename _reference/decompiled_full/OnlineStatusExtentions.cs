public static class OnlineStatusExtentions
{
	public static bool IsOnline(this OnlineStatus status)
	{
		return status == OnlineStatus.Online;
	}
}
