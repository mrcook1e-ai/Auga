using System;

public struct ServerNameAtTimePoint(string serverName, DateTime timestampUtc)
{
	public static readonly ServerNameAtTimePoint None;

	public readonly string m_name = serverName;

	public readonly DateTime m_timestampUtc = timestampUtc;
}
