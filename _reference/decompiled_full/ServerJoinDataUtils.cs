using System;
using NetworkingUtils;

public static class ServerJoinDataUtils
{
	public static void GetAddressAndPortFromString(string address, out string ipAddress, out ushort foundPort)
	{
		if (string.IsNullOrEmpty(address))
		{
			ipAddress = string.Empty;
			foundPort = 0;
			return;
		}
		int num = address.LastIndexOf(":");
		int num2 = ((num >= 0) ? num : address.Length);
		if (num < 0 || !ushort.TryParse(address.AsSpan(num + 1), out foundPort))
		{
			foundPort = 0;
			num2 = address.Length;
		}
		IPv4Address result2;
		IPv6Address result3;
		if (address[0] == '[' && address[num2 - 1] == ']' && IPv6Address.TryParse(address.AsSpan(1, num2 - 2), out var result, allowIPv4: false))
		{
			if (result.AddressRange == IPv6AddressRange.Unspecified)
			{
				ipAddress = string.Empty;
				foundPort = 0;
			}
			else
			{
				ipAddress = result.ToString();
			}
		}
		else if (IPv4Address.TryParse(address.AsSpan(0, num2), out result2))
		{
			ipAddress = result2.ToString();
		}
		else if (IPv6Address.TryParse(address, out result3, allowIPv4: false))
		{
			if (result3.AddressRange == IPv6AddressRange.Unspecified)
			{
				ipAddress = string.Empty;
			}
			else
			{
				ipAddress = result3.ToString();
			}
			foundPort = 0;
		}
		else
		{
			ipAddress = address.Substring(0, num2);
		}
	}
}
