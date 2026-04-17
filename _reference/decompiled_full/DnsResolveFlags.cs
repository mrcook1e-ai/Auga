using System;

[Flags]
public enum DnsResolveFlags
{
	None = 0,
	CacheOnly = 1,
	DontCheckCache = 2
}
