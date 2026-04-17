using System;
using System.Collections.Generic;
using Splatform;
using UnityEngine;

public class MasterClient
{
	private const int statVersion = 2;

	public Action<List<ServerData>> m_onServerList;

	private string m_msHost = "dvoid.noip.me";

	private int m_msPort = 9983;

	private long m_sessionUID;

	private ZConnector2 m_connector;

	private ZSocket2 m_socket;

	private ZRpc m_rpc;

	private bool m_haveServerlist;

	private List<ServerData> m_servers = new List<ServerData>();

	private ZPackage m_registerPkg;

	private float m_sendStatsTimer;

	private int m_serverListRevision;

	private string m_nameFilter = "";

	private static MasterClient m_instance;

	public static MasterClient instance => m_instance;

	public static void Initialize()
	{
		if (m_instance == null)
		{
			m_instance = new MasterClient();
		}
	}

	public MasterClient()
	{
		m_sessionUID = Utils.GenerateUID();
	}

	public void Dispose()
	{
		if (m_socket != null)
		{
			m_socket.Dispose();
		}
		if (m_connector != null)
		{
			m_connector.Dispose();
		}
		if (m_rpc != null)
		{
			m_rpc.Dispose();
		}
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	public void Update(float dt)
	{
	}

	private void SendStats(float duration)
	{
		ZPackage zPackage = new ZPackage();
		zPackage.Write(2);
		zPackage.Write(m_sessionUID);
		zPackage.Write(Time.time);
		bool flag = Player.m_localPlayer != null;
		zPackage.Write(flag ? duration : 0f);
		bool flag2 = (bool)ZNet.instance && !ZNet.instance.IsServer();
		zPackage.Write(flag2 ? duration : 0f);
		zPackage.Write(Version.CurrentVersion.ToString());
		zPackage.Write(36u);
		bool flag3 = (bool)ZNet.instance && ZNet.instance.IsServer();
		zPackage.Write(flag3);
		if (flag3)
		{
			zPackage.Write(ZNet.instance.GetWorldUID());
			zPackage.Write(duration);
			int num = ZNet.instance.GetPeerConnections();
			if (Player.m_localPlayer != null)
			{
				num++;
			}
			zPackage.Write(num);
			bool data = ZNet.instance.GetZNat() != null && ZNet.instance.GetZNat().GetStatus();
			zPackage.Write(data);
		}
		PlayerProfile playerProfile = ((Game.instance != null) ? Game.instance.GetPlayerProfile() : null);
		if (playerProfile != null)
		{
			zPackage.Write(data: true);
			zPackage.Write(playerProfile.GetPlayerID());
			zPackage.Write(105);
			for (int i = 0; i < 105; i++)
			{
				zPackage.Write(playerProfile.m_playerStats.m_stats[(PlayerStatType)i]);
			}
			zPackage.Write(playerProfile.m_usedCheats);
			zPackage.Write(new DateTimeOffset(playerProfile.m_dateCreated).ToUnixTimeSeconds());
			zPackage.Write(playerProfile.m_knownWorlds.Count);
			foreach (KeyValuePair<string, float> knownWorld in playerProfile.m_knownWorlds)
			{
				zPackage.Write(knownWorld.Key);
				zPackage.Write(knownWorld.Value);
			}
			zPackage.Write(playerProfile.m_knownWorldKeys.Count);
			foreach (KeyValuePair<string, float> knownWorldKey in playerProfile.m_knownWorldKeys)
			{
				zPackage.Write(knownWorldKey.Key);
				zPackage.Write(knownWorldKey.Value);
			}
			zPackage.Write(playerProfile.m_knownCommands.Count);
			foreach (KeyValuePair<string, float> knownCommand in playerProfile.m_knownCommands)
			{
				zPackage.Write(knownCommand.Key);
				zPackage.Write(knownCommand.Value);
			}
			zPackage.Write(playerProfile.m_enemyStats.Count);
			foreach (KeyValuePair<string, float> enemyStat in playerProfile.m_enemyStats)
			{
				zPackage.Write(enemyStat.Key);
				zPackage.Write(enemyStat.Value);
			}
			zPackage.Write(playerProfile.m_itemPickupStats.Count);
			foreach (KeyValuePair<string, float> itemPickupStat in playerProfile.m_itemPickupStats)
			{
				zPackage.Write(itemPickupStat.Key);
				zPackage.Write(itemPickupStat.Value);
			}
			zPackage.Write(playerProfile.m_itemCraftStats.Count);
			foreach (KeyValuePair<string, float> itemCraftStat in playerProfile.m_itemCraftStats)
			{
				zPackage.Write(itemCraftStat.Key);
				zPackage.Write(itemCraftStat.Value);
			}
		}
		else
		{
			zPackage.Write(data: false);
		}
		m_rpc.Invoke("Stats", zPackage);
	}

	public void RegisterServer(string name, string host, int port, bool password, bool upnp, long worldUID, GameVersion gameVersion, uint networkVersion, string[] modifiers)
	{
		m_registerPkg = new ZPackage();
		m_registerPkg.Write(1);
		m_registerPkg.Write(name);
		m_registerPkg.Write(host);
		m_registerPkg.Write(port);
		m_registerPkg.Write(password);
		m_registerPkg.Write(upnp);
		m_registerPkg.Write(worldUID);
		m_registerPkg.Write(gameVersion.ToString());
		m_registerPkg.Write(networkVersion);
		m_registerPkg.Write(StringUtils.EncodeStringListAsString(modifiers));
		if (m_rpc != null)
		{
			m_rpc.Invoke("RegisterServer2", m_registerPkg);
		}
		ZLog.Log("Registering server " + name + "  " + host + ":" + port);
	}

	public void UnregisterServer()
	{
		if (m_registerPkg != null)
		{
			if (m_rpc != null)
			{
				m_rpc.Invoke("UnregisterServer");
			}
			m_registerPkg = null;
		}
	}

	public List<ServerData> GetServers()
	{
		return m_servers;
	}

	public bool GetServers(List<ServerData> servers)
	{
		if (!m_haveServerlist)
		{
			return false;
		}
		servers.Clear();
		servers.AddRange(m_servers);
		return true;
	}

	public void RequestServerlist()
	{
		if (m_rpc != null)
		{
			m_rpc.Invoke("RequestServerlist2");
		}
	}

	private void RPC_ServerList(ZRpc rpc, ZPackage pkg)
	{
		m_haveServerlist = true;
		m_serverListRevision++;
		pkg.ReadInt();
		int num = pkg.ReadInt();
		m_servers.Clear();
		for (int i = 0; i < num; i++)
		{
			string text = pkg.ReadString();
			string text2 = pkg.ReadString();
			int num2 = pkg.ReadInt();
			bool isPasswordProtected = pkg.ReadBool();
			pkg.ReadBool();
			pkg.ReadLong();
			string versionString = pkg.ReadString();
			uint networkVersion = 0u;
			if (GameVersion.TryParseGameVersion(versionString, out var version) && version >= Version.FirstVersionWithNetworkVersion)
			{
				networkVersion = pkg.ReadUInt();
			}
			int playerCount = pkg.ReadInt();
			string[] decodedArray;
			if (version >= Version.FirstVersionWithModifiers)
			{
				StringUtils.TryDecodeStringAsArray(pkg.ReadString(), out decodedArray);
			}
			else
			{
				decodedArray = new string[0];
			}
			ServerJoinData joinData = new ServerJoinData(new ServerJoinDataDedicated(text2 + ":" + num2));
			ServerMatchmakingData matchmakingData = new ServerMatchmakingData(DateTime.UtcNow, text, (uint)playerCount, 10u, PlatformUserID.None, version, networkVersion, null, isPasswordProtected, Platform.Unknown, decodedArray);
			if (m_nameFilter.Length <= 0 || !text.Contains(m_nameFilter))
			{
				m_servers.Add(new ServerData(joinData, matchmakingData));
			}
		}
		if (m_onServerList != null)
		{
			m_onServerList(m_servers);
		}
	}

	public int GetServerListRevision()
	{
		return m_serverListRevision;
	}

	public bool IsConnected()
	{
		return m_rpc != null;
	}

	public void SetNameFilter(string filter)
	{
		m_nameFilter = filter;
		ZLog.Log("filter is " + filter);
	}
}
