public static class EmoteExtensions
{
	public static string GetCommandName(this Emotes emote)
	{
		return emote.ToString().ToLowerInvariant();
	}
}
