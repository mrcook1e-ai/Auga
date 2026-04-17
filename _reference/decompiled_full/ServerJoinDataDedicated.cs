using System;
using NetworkingUtils;

public struct ServerJoinDataDedicated : IEquatable<ServerJoinDataDedicated>
{
	public const string c_TypeName = "Dedicated";

	public readonly ushort m_port;

	private IPv6Address? m_address;

	public string m_host { get; private set; }

	public bool IsURL { get; private set; }

	public ServerJoinDataDedicated(string address)
	{
		m_host = null;
		m_port = 0;
		m_address = null;
		IsURL = false;
		ushort foundPort = 0;
		ServerJoinDataUtils.GetAddressAndPortFromString(address, out var ipAddress, out foundPort);
		if (!string.IsNullOrEmpty(ipAddress))
		{
			SetHost(ipAddress);
			if (foundPort != 0)
			{
				m_port = foundPort;
			}
			else
			{
				m_port = 2456;
			}
		}
	}

	public ServerJoinDataDedicated(string host, ushort port)
	{
		m_host = null;
		m_address = null;
		IsURL = false;
		m_port = port;
		SetHost(host);
	}

	public ServerJoinDataDedicated(uint host, ushort port)
	{
		m_address = new IPv4Address(host);
		m_host = m_address.Value.IPv4.ToString();
		m_port = port;
		IsURL = false;
	}

	public ServerJoinDataDedicated(IPEndPoint endPoint)
	{
		m_address = endPoint.m_address;
		m_host = endPoint.m_address.IPv4.ToString();
		m_port = endPoint.m_port;
		IsURL = false;
	}

	public string GetDataName()
	{
		return "Dedicated";
	}

	public override bool Equals(object obj)
	{
		if (obj != null && obj is ServerJoinDataDedicated other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(ServerJoinDataDedicated other)
	{
		if (m_host == other.m_host)
		{
			return m_port == other.m_port;
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = -468063053;
		if (!string.IsNullOrEmpty(m_host))
		{
			num = num * -1521134295 + m_host.GetHashCode();
		}
		else
		{
			ZLog.LogWarning("m_host was null or empty when trying to get hash code!");
		}
		int num2 = num * -1521134295;
		ushort port = m_port;
		return num2 + port.GetHashCode();
	}

	public static bool operator ==(ServerJoinDataDedicated left, ServerJoinDataDedicated right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ServerJoinDataDedicated left, ServerJoinDataDedicated right)
	{
		return !(left == right);
	}

	private void SetHost(string host)
	{
		if (IPv6Address.TryParse(host, out var result))
		{
			m_host = result.ToString();
			m_address = result;
			return;
		}
		string text = host;
		if (!host.StartsWith("http://") && !host.StartsWith("https://"))
		{
			text = "http://" + host;
		}
		if (!host.EndsWith("/"))
		{
			text += "/";
		}
		if (Uri.TryCreate(text, UriKind.Absolute, out var _))
		{
			m_host = host;
			IsURL = true;
		}
		else
		{
			m_host = host;
		}
	}

	public string GetHost()
	{
		return m_host;
	}

	public bool TryGetIPAddress(out IPv6Address address)
	{
		address = (m_address.HasValue ? m_address.Value : default(IPv6Address));
		return m_address.HasValue;
	}

	public bool TryGetIPEndPoint(out IPEndPoint endPoint)
	{
		endPoint = (m_address.HasValue ? new IPEndPoint(m_address.Value, m_port) : default(IPEndPoint));
		return m_address.HasValue;
	}

	public override string ToString()
	{
		string host = GetHost();
		int port = m_port;
		if (m_address.HasValue && m_address.Value.AddressRange != IPv6AddressRange.IPv4Mapped)
		{
			return $"[{host}]:{port}";
		}
		return $"{host}:{port}";
	}
}
