using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NetworkingUtils;
using Splatform;
using Steamworks;
using UnityEngine;

public class ZSteamMatchmaking
{
	public delegate void AuthSessionTicketResponseHandler();

	public delegate void ServerRegistered(bool success);

	public struct DedicatedPing(NetworkingUtils.IPEndPoint endPoint, ServerPingCompletedHandler completedHandler)
	{
		public NetworkingUtils.IPEndPoint m_endPoint = endPoint;

		public ServerPingCompletedHandler m_completedHandler = completedHandler ?? throw new ArgumentNullException("completedHandler");
	}

	public delegate void ServerPingCompletedHandler(ServerData serverData);

	private static ZSteamMatchmaking m_instance;

	private const int maxServers = 200;

	private List<ServerData> m_matchmakingServers = new List<ServerData>();

	private List<ServerData> m_dedicatedServers = new List<ServerData>();

	private List<ServerData> m_friendServers = new List<ServerData>();

	private int m_serverListRevision;

	private int m_updateTriggerAccumulator;

	private CallResult<LobbyCreated_t> m_lobbyCreated;

	private CallResult<LobbyMatchList_t> m_lobbyMatchList;

	private CallResult<LobbyEnter_t> m_lobbyEntered;

	private Callback<GameServerChangeRequested_t> m_changeServer;

	private Callback<GameLobbyJoinRequested_t> m_joinRequest;

	private Callback<LobbyDataUpdate_t> m_lobbyDataUpdate;

	private Callback<GetAuthSessionTicketResponse_t> m_authSessionTicketResponse;

	private Callback<SteamServerConnectFailure_t> m_steamServerConnectFailure;

	private Callback<SteamServersConnected_t> m_steamServersConnected;

	private Callback<SteamServersDisconnected_t> m_steamServersDisconnected;

	private ServerRegistered serverRegisteredCallback;

	private CSteamID m_myLobby = CSteamID.Nil;

	private CSteamID m_queuedJoinLobby = CSteamID.Nil;

	private ServerJoinData m_joinData = ServerJoinData.None;

	private List<KeyValuePair<CSteamID, string>> m_requestedFriendGames = new List<KeyValuePair<CSteamID, string>>();

	private Dictionary<CSteamID, (CSteamID, ServerPingCompletedHandler)> m_requestedLobbiesUserHosted = new Dictionary<CSteamID, (CSteamID, ServerPingCompletedHandler)>();

	private Queue<DedicatedPing> m_queuedPings = new Queue<DedicatedPing>();

	private DedicatedPing? m_currentPing;

	private ISteamMatchmakingServerListResponse m_steamServerCallbackHandler;

	private ISteamMatchmakingPingResponse m_pingResponseCallbackHandler;

	private HServerQuery m_pingQuery;

	private HServerListRequest m_serverListRequest;

	private bool m_haveListRequest;

	private bool m_refreshingDedicatedServers;

	private bool m_refreshingPublicGames;

	private string m_registerServerName = "";

	private bool m_registerPassword;

	private GameVersion m_registerGameVerson;

	private uint m_registerNetworkVerson;

	private string[] m_registerModifiers = new string[0];

	private string m_nameFilter = "";

	private bool m_friendsFilter = true;

	private HAuthTicket m_authTicket = HAuthTicket.Invalid;

	public static ZSteamMatchmaking instance => m_instance;

	public bool IsRefreshing { get; private set; }

	public event AuthSessionTicketResponseHandler AuthSessionTicketResponse;

	public static void Initialize()
	{
		if (m_instance == null)
		{
			m_instance = new ZSteamMatchmaking();
		}
	}

	private ZSteamMatchmaking()
	{
		m_steamServerCallbackHandler = new ISteamMatchmakingServerListResponse(OnServerResponded, OnServerFailedToRespond, OnRefreshComplete);
		m_pingResponseCallbackHandler = new ISteamMatchmakingPingResponse(OnPingRespond, OnPingFailed);
		m_lobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
		m_lobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
		m_changeServer = Callback<GameServerChangeRequested_t>.Create(OnChangeServerRequest);
		m_joinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
		m_lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
		m_authSessionTicketResponse = Callback<GetAuthSessionTicketResponse_t>.Create(OnAuthSessionTicketResponse);
	}

	public byte[] RequestSessionTicket(ref SteamNetworkingIdentity serverIdentity)
	{
		ReleaseSessionTicket();
		byte[] array = new byte[1024];
		uint pcbTicket = 0u;
		SteamNetworkingIdentity pSteamNetworkingIdentity = default(SteamNetworkingIdentity);
		m_authTicket = SteamUser.GetAuthSessionTicket(array, 1024, out pcbTicket, ref pSteamNetworkingIdentity);
		if (m_authTicket == HAuthTicket.Invalid)
		{
			return null;
		}
		byte[] array2 = new byte[pcbTicket];
		Buffer.BlockCopy(array, 0, array2, 0, (int)pcbTicket);
		return array2;
	}

	public void ReleaseSessionTicket()
	{
		if (!(m_authTicket == HAuthTicket.Invalid))
		{
			SteamUser.CancelAuthTicket(m_authTicket);
			m_authTicket = HAuthTicket.Invalid;
			ZLog.Log("Released session ticket");
		}
	}

	public bool VerifySessionTicket(byte[] ticket, CSteamID steamID)
	{
		return SteamUser.BeginAuthSession(ticket, ticket.Length, steamID) == EBeginAuthSessionResult.k_EBeginAuthSessionResultOK;
	}

	private void OnAuthSessionTicketResponse(GetAuthSessionTicketResponse_t data)
	{
		ZLog.Log("Session auth respons callback");
		this.AuthSessionTicketResponse?.Invoke();
	}

	private void OnSteamServersConnected(SteamServersConnected_t data)
	{
		ZLog.Log("Game server connected");
	}

	private void OnSteamServersDisconnected(SteamServersDisconnected_t data)
	{
		ZLog.LogWarning("Game server disconnected");
	}

	private void OnSteamServersConnectFail(SteamServerConnectFailure_t data)
	{
		ZLog.LogWarning("Game server connected failed");
	}

	private void OnChangeServerRequest(GameServerChangeRequested_t data)
	{
		ZLog.Log("ZSteamMatchmaking got change server request to:" + data.m_rgchServer);
		QueueServerJoin(data.m_rgchServer);
	}

	private void OnJoinRequest(GameLobbyJoinRequested_t data)
	{
		CSteamID steamIDFriend = data.m_steamIDFriend;
		string text = steamIDFriend.ToString();
		steamIDFriend = data.m_steamIDLobby;
		ZLog.Log("ZSteamMatchmaking got join request friend:" + text + "  lobby:" + steamIDFriend.ToString());
		QueueLobbyJoin(data.m_steamIDLobby);
	}

	private IPAddress FindIP(string host)
	{
		try
		{
			if (IPAddress.TryParse(host, out var address))
			{
				return address;
			}
			ZLog.Log("Not an ip address " + host + " doing dns lookup");
			IPHostEntry hostEntry = Dns.GetHostEntry(host);
			if (hostEntry.AddressList.Length == 0)
			{
				ZLog.Log("Dns lookup failed");
				return null;
			}
			ZLog.Log("Got dns entries: " + hostEntry.AddressList.Length);
			IPAddress[] addressList = hostEntry.AddressList;
			foreach (IPAddress iPAddress in addressList)
			{
				if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
				{
					return iPAddress;
				}
			}
			return null;
		}
		catch (Exception ex)
		{
			ZLog.Log("Exception while finding ip:" + ex.ToString());
			return null;
		}
	}

	public bool ResolveIPFromAddrString(string addr, ref SteamNetworkingIPAddr destination)
	{
		try
		{
			string[] array = addr.Split(':');
			if (array.Length < 2)
			{
				return false;
			}
			IPAddress iPAddress = FindIP(array[0]);
			if (iPAddress == null)
			{
				ZLog.Log("Invalid address " + array[0]);
				return false;
			}
			uint nIP = (uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(iPAddress.GetAddressBytes(), 0));
			int num = int.Parse(array[1]);
			ZLog.Log("connect to ip:" + iPAddress.ToString() + " port:" + num);
			destination.SetIPv4(nIP, (ushort)num);
			return true;
		}
		catch (Exception ex)
		{
			ZLog.Log("Exception when resolving IP address: " + ex);
			return false;
		}
	}

	public void QueueServerJoin(string addr)
	{
		SteamNetworkingIPAddr destination = default(SteamNetworkingIPAddr);
		if (ResolveIPFromAddrString(addr, ref destination))
		{
			m_joinData = new ServerJoinData(new ServerJoinDataDedicated(destination.GetIPv4(), destination.m_port));
		}
		else
		{
			ZLog.Log("Couldn't resolve IP address.");
		}
	}

	private void EnqueuePing(NetworkingUtils.IPEndPoint server, ServerPingCompletedHandler completedCallback)
	{
		m_queuedPings.Enqueue(new DedicatedPing(server, completedCallback));
		if (!m_currentPing.HasValue)
		{
			PingNextInQueue();
		}
	}

	private void PingNextInQueue()
	{
		if (m_queuedPings.Count <= 0)
		{
			ZLog.LogError("Queued pings was 0!");
			return;
		}
		if (m_currentPing.HasValue)
		{
			ZLog.LogError("Ping was already active!");
			return;
		}
		m_currentPing = m_queuedPings.Dequeue();
		IPv6Address address = m_currentPing.Value.m_endPoint.m_address;
		if (address.AddressRange != IPv6AddressRange.IPv4Mapped)
		{
			ZLog.LogError($"Address {address} was not an IPv4 address!");
		}
		else
		{
			m_pingQuery = SteamMatchmakingServers.PingServer(address.IPv4.m_value, (ushort)((m_currentPing.Value.m_endPoint.m_port + 1) % 65535), m_pingResponseCallbackHandler);
		}
	}

	private void OnPingRespond(gameserveritem_t serverData)
	{
		string serverName = serverData.GetServerName();
		CSteamID steamID = serverData.m_steamID;
		ZLog.Log("Got join server data " + serverName + "  " + steamID.ToString());
		NetworkingUtils.IPEndPoint iPEndPoint = new NetworkingUtils.IPEndPoint(new IPv4Address(serverData.m_NetAdr.GetIP()), serverData.m_NetAdr.GetConnectionPort());
		DecodeTags(serverData.GetGameTags(), out var gameVersion, out var networkVersion, out var modifiers);
		ServerMatchmakingData matchmakingData = new ServerMatchmakingData(DateTime.UtcNow, serverData.GetServerName(), (uint)serverData.m_nPlayers, (uint)serverData.m_nMaxPlayers, PlatformUserID.None, gameVersion, networkVersion, null, serverData.m_bPassword, new Platform("Steam"), modifiers);
		if (!m_currentPing.HasValue)
		{
			ZLog.LogError("Server " + matchmakingData.m_serverName + " got callback but wasn't requested!");
			return;
		}
		if (m_currentPing.Value.m_endPoint != iPEndPoint)
		{
			ZLog.LogError($"Retrieved address {iPEndPoint} is not equal to the stored address {m_currentPing.Value.m_endPoint}!");
			return;
		}
		ServerJoinData joinData = new ServerJoinData(new ServerJoinDataDedicated(iPEndPoint));
		FinishPing(new ServerData(joinData, matchmakingData));
	}

	private void OnPingFailed()
	{
		ZLog.Log("Failed to get join server data");
		if (!m_currentPing.HasValue)
		{
			ZLog.LogError($"Server {m_currentPing.Value.m_endPoint} got callback but wasn't requested!");
		}
		else
		{
			FinishPing(new ServerData(new ServerJoinData(new ServerJoinDataDedicated(m_currentPing.Value.m_endPoint)), new ServerMatchmakingData(DateTime.UtcNow)));
		}
	}

	private void FinishPing(ServerData serverData)
	{
		InvokeCurrentPingCallback(serverData);
		if (m_queuedPings.Count > 0)
		{
			PingNextInQueue();
		}
	}

	private void InvokeCurrentPingCallback(ServerData serverData)
	{
		ServerPingCompletedHandler completedHandler = m_currentPing.Value.m_completedHandler;
		m_currentPing = null;
		m_pingQuery = default(HServerQuery);
		completedHandler?.Invoke(serverData);
	}

	private bool TryGetLobbyData(CSteamID lobbyID)
	{
		if (!SteamMatchmaking.GetLobbyGameServer(lobbyID, out var _, out var _, out var psteamIDGameServer))
		{
			return false;
		}
		CSteamID cSteamID = psteamIDGameServer;
		ZLog.Log("  hostid: " + cSteamID.ToString());
		m_queuedJoinLobby = CSteamID.Nil;
		m_joinData = GetLobbyServerData(lobbyID).m_joinData;
		return true;
	}

	public void QueueLobbyJoin(CSteamID lobbyID)
	{
		if (!TryGetLobbyData(lobbyID))
		{
			CSteamID cSteamID = lobbyID;
			ZLog.Log("Failed to get lobby data for lobby " + cSteamID.ToString() + ", requesting lobby data");
			m_queuedJoinLobby = lobbyID;
			SteamMatchmaking.RequestLobbyData(lobbyID);
		}
		if (!(FejdStartup.instance == null))
		{
			return;
		}
		if (UnifiedPopup.IsAvailable() && Menu.instance != null)
		{
			UnifiedPopup.Push(new YesNoPopup("$menu_joindifferentserver", "$menu_logoutprompt", delegate
			{
				UnifiedPopup.Pop();
				if (Menu.instance != null)
				{
					Menu.instance.OnLogoutYes();
				}
			}, delegate
			{
				UnifiedPopup.Pop();
				m_queuedJoinLobby = CSteamID.Nil;
				m_joinData = ServerJoinData.None;
			}));
		}
		else
		{
			Debug.LogWarning("Couldn't handle invite appropriately! Ignoring.");
			m_queuedJoinLobby = CSteamID.Nil;
			m_joinData = ServerJoinData.None;
		}
	}

	private void OnLobbyDataUpdate(LobbyDataUpdate_t data)
	{
		CSteamID cSteamID = new CSteamID(data.m_ulSteamIDLobby);
		if (cSteamID == m_queuedJoinLobby)
		{
			if (TryGetLobbyData(cSteamID))
			{
				ZLog.Log("Got lobby data, for queued lobby");
			}
			return;
		}
		ZLog.Log("Got requested lobby data");
		ServerData lobbyServerData = GetLobbyServerData(cSteamID);
		foreach (KeyValuePair<CSteamID, string> requestedFriendGame in m_requestedFriendGames)
		{
			if (requestedFriendGame.Key == cSteamID && lobbyServerData.m_joinData.IsValid)
			{
				m_friendServers.Add(lobbyServerData);
				m_serverListRevision++;
			}
		}
		if (m_requestedLobbiesUserHosted.TryGetValue(cSteamID, out var value))
		{
			m_requestedLobbiesUserHosted.Remove(cSteamID);
			value.Item2?.Invoke(lobbyServerData);
		}
	}

	public void RegisterServer(string name, bool password, GameVersion gameVersion, string[] modifiers, uint networkVersion, bool publicServer, string worldName, ServerRegistered serverRegisteredCallback)
	{
		UnregisterServer();
		this.serverRegisteredCallback = serverRegisteredCallback;
		SteamAPICall_t hAPICall = SteamMatchmaking.CreateLobby((!publicServer) ? ELobbyType.k_ELobbyTypeFriendsOnly : ELobbyType.k_ELobbyTypePublic, 10);
		m_lobbyCreated.Set(hAPICall);
		m_registerServerName = name;
		m_registerPassword = password;
		m_registerGameVerson = gameVersion;
		m_registerNetworkVerson = networkVersion;
		m_registerModifiers = modifiers;
		ZLog.Log("Registering lobby");
	}

	private void OnLobbyCreated(LobbyCreated_t data, bool ioError)
	{
		ZLog.Log("Lobby was created " + data.m_eResult.ToString() + "  " + data.m_ulSteamIDLobby + "  error:" + ioError);
		if (ioError)
		{
			serverRegisteredCallback?.Invoke(success: false);
			return;
		}
		if (data.m_eResult == EResult.k_EResultNoConnection)
		{
			ZLog.LogWarning("Failed to connect to Steam to register the server!");
			serverRegisteredCallback?.Invoke(success: false);
			return;
		}
		m_myLobby = new CSteamID(data.m_ulSteamIDLobby);
		if (!SteamMatchmaking.SetLobbyData(m_myLobby, "name", m_registerServerName))
		{
			Debug.LogError("Couldn't set name in lobby");
		}
		if (!SteamMatchmaking.SetLobbyData(m_myLobby, "password", m_registerPassword ? "1" : "0"))
		{
			Debug.LogError("Couldn't set password in lobby");
		}
		if (!SteamMatchmaking.SetLobbyData(m_myLobby, "version", m_registerGameVerson.ToString()))
		{
			Debug.LogError("Couldn't set game version in lobby");
		}
		if (!SteamMatchmaking.SetLobbyData(m_myLobby, "networkversion", m_registerNetworkVerson.ToString()))
		{
			Debug.LogError("Couldn't set network version in lobby");
		}
		if (!SteamMatchmaking.SetLobbyData(m_myLobby, "modifiers", StringUtils.EncodeStringListAsString(m_registerModifiers)))
		{
			Debug.LogError("Couldn't set modifiers in lobby");
		}
		string pchValue;
		string pchValue2;
		string pchValue3;
		switch (ZNet.m_onlineBackend)
		{
		case OnlineBackendType.CustomSocket:
			pchValue = "Dedicated";
			pchValue2 = ZNet.GetServerString(includeBackend: false);
			pchValue3 = "1";
			break;
		case OnlineBackendType.Steamworks:
			pchValue = "Steam user";
			pchValue2 = "";
			pchValue3 = "0";
			break;
		case OnlineBackendType.PlayFab:
			pchValue = "PlayFab user";
			pchValue2 = PlayFabManager.instance.Entity.Id;
			pchValue3 = "1";
			break;
		default:
			Debug.LogError("Can't create lobby for server with unknown or unsupported backend");
			pchValue = "";
			pchValue2 = "";
			pchValue3 = "";
			break;
		}
		if (PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.CrossPlatformMultiplayer) != PrivilegeResult.Granted)
		{
			pchValue3 = "0";
		}
		if (!SteamMatchmaking.SetLobbyData(m_myLobby, "serverType", pchValue))
		{
			Debug.LogError("Couldn't set backend in lobby");
		}
		if (!SteamMatchmaking.SetLobbyData(m_myLobby, "hostID", pchValue2))
		{
			Debug.LogError("Couldn't set host in lobby");
		}
		if (!SteamMatchmaking.SetLobbyData(m_myLobby, "isCrossplay", pchValue3))
		{
			Debug.LogError("Couldn't set crossplay in lobby");
		}
		SteamMatchmaking.SetLobbyGameServer(m_myLobby, 0u, 0, SteamUser.GetSteamID());
		serverRegisteredCallback?.Invoke(success: true);
	}

	private void OnLobbyEnter(LobbyEnter_t data, bool ioError)
	{
		ZLog.LogWarning("Entering lobby " + data.m_ulSteamIDLobby);
	}

	public void UnregisterServer()
	{
		if (m_myLobby != CSteamID.Nil)
		{
			SteamMatchmaking.SetLobbyJoinable(m_myLobby, bLobbyJoinable: false);
			SteamMatchmaking.LeaveLobby(m_myLobby);
			m_myLobby = CSteamID.Nil;
		}
	}

	public void RequestServerlist()
	{
		IsRefreshing = true;
		RequestFriendGames();
		RequestPublicLobbies();
		RequestDedicatedServers();
	}

	public void StopServerListing()
	{
		if (m_haveListRequest)
		{
			SteamMatchmakingServers.ReleaseRequest(m_serverListRequest);
			m_haveListRequest = false;
			IsRefreshing = false;
		}
	}

	private void RequestFriendGames()
	{
		m_friendServers.Clear();
		m_requestedFriendGames.Clear();
		int num = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
		if (num == -1)
		{
			ZLog.Log("GetFriendCount returned -1, the current user is not logged in.");
			num = 0;
		}
		for (int i = 0; i < num; i++)
		{
			CSteamID friendByIndex = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
			string friendPersonaName = SteamFriends.GetFriendPersonaName(friendByIndex);
			if (SteamFriends.GetFriendGamePlayed(friendByIndex, out var pFriendGameInfo) && pFriendGameInfo.m_gameID == (CGameID)SteamManager.APP_ID && pFriendGameInfo.m_steamIDLobby != CSteamID.Nil)
			{
				ZLog.Log("Friend is in our game");
				m_requestedFriendGames.Add(new KeyValuePair<CSteamID, string>(pFriendGameInfo.m_steamIDLobby, friendPersonaName));
				SteamMatchmaking.RequestLobbyData(pFriendGameInfo.m_steamIDLobby);
			}
		}
		m_serverListRevision++;
	}

	private void RequestPublicLobbies()
	{
		SteamAPICall_t hAPICall = SteamMatchmaking.RequestLobbyList();
		m_lobbyMatchList.Set(hAPICall);
		m_refreshingPublicGames = true;
	}

	private void RequestDedicatedServers()
	{
		if (m_haveListRequest)
		{
			SteamMatchmakingServers.ReleaseRequest(m_serverListRequest);
			m_haveListRequest = false;
		}
		m_dedicatedServers.Clear();
		m_serverListRequest = SteamMatchmakingServers.RequestInternetServerList(SteamUtils.GetAppID(), new MatchMakingKeyValuePair_t[0], 0u, m_steamServerCallbackHandler);
		m_haveListRequest = true;
	}

	private void OnLobbyMatchList(LobbyMatchList_t data, bool ioError)
	{
		m_refreshingPublicGames = false;
		m_matchmakingServers.Clear();
		for (int i = 0; i < data.m_nLobbiesMatching; i++)
		{
			CSteamID lobbyByIndex = SteamMatchmaking.GetLobbyByIndex(i);
			ServerData lobbyServerData = GetLobbyServerData(lobbyByIndex);
			if (lobbyServerData.m_joinData.IsValid)
			{
				m_matchmakingServers.Add(lobbyServerData);
			}
		}
		m_serverListRevision++;
	}

	private ServerData GetLobbyServerData(CSteamID lobbyID)
	{
		string lobbyData = SteamMatchmaking.GetLobbyData(lobbyID, "name");
		bool isPasswordProtected = SteamMatchmaking.GetLobbyData(lobbyID, "password") == "1";
		GameVersion gameVersion = GameVersion.ParseGameVersion(SteamMatchmaking.GetLobbyData(lobbyID, "version"));
		StringUtils.TryDecodeStringAsArray(SteamMatchmaking.GetLobbyData(lobbyID, "modifiers"), out var decodedArray);
		uint networkVersion = (uint.TryParse(SteamMatchmaking.GetLobbyData(lobbyID, "networkversion"), out networkVersion) ? networkVersion : 0u);
		int numLobbyMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
		int lobbyMemberLimit = SteamMatchmaking.GetLobbyMemberLimit(lobbyID);
		if (!SteamMatchmaking.GetLobbyGameServer(lobbyID, out var _, out var _, out var psteamIDGameServer))
		{
			ZLog.Log("Failed to get lobby gameserver");
			return ServerData.None;
		}
		string lobbyData2 = SteamMatchmaking.GetLobbyData(lobbyID, "hostID");
		string lobbyData3 = SteamMatchmaking.GetLobbyData(lobbyID, "serverType");
		string lobbyData4 = SteamMatchmaking.GetLobbyData(lobbyID, "isCrossplay");
		if (lobbyData3 == null || lobbyData3.Length != 0)
		{
			switch (lobbyData3)
			{
			case "Steam user":
				break;
			case "PlayFab user":
				goto IL_00ff;
			case "Dedicated":
				goto IL_010f;
			default:
				return ServerData.None;
			}
		}
		ServerJoinData joinData = new ServerJoinData(new ServerJoinDataSteamUser(psteamIDGameServer));
		goto IL_0125;
		IL_0125:
		if (!joinData.IsValid)
		{
			return ServerData.None;
		}
		ServerMatchmakingData matchmakingData = new ServerMatchmakingData(DateTime.UtcNow, lobbyData, (uint)numLobbyMembers, (uint)lobbyMemberLimit, new PlatformUserID(new Platform("Steam"), psteamIDGameServer.ToString()), gameVersion, networkVersion, null, isPasswordProtected, (lobbyData4 == "1") ? Platform.Unknown : PlatformManager.DistributionPlatform.Platform, decodedArray);
		return new ServerData(joinData, matchmakingData);
		IL_010f:
		joinData = new ServerJoinData(new ServerJoinDataDedicated(lobbyData2));
		goto IL_0125;
		IL_00ff:
		joinData = new ServerJoinData(new ServerJoinDataPlayFabUser(lobbyData2));
		goto IL_0125;
	}

	public string KnownBackendsString()
	{
		List<string> list = new List<string>();
		list.Add("Steam user");
		list.Add("PlayFab user");
		list.Add("Dedicated");
		return "Known backends: " + string.Join(", ", list.Select((string s) => "\"" + s + "\""));
	}

	public void GetServers(List<ServerData> allServers)
	{
		if (m_friendsFilter)
		{
			FilterServers(m_friendServers, allServers);
			return;
		}
		FilterServers(m_matchmakingServers, allServers);
		FilterServers(m_dedicatedServers, allServers);
	}

	private void FilterServers(List<ServerData> input, List<ServerData> allServers)
	{
		string text = m_nameFilter.ToLowerInvariant();
		foreach (ServerData item in input)
		{
			if (text.Length == 0 || item.m_matchmakingData.m_serverName.ToLowerInvariant().Contains(text))
			{
				allServers.Add(item);
			}
			if (allServers.Count >= 200)
			{
				break;
			}
		}
	}

	public void CheckIfOnlineAsync(NetworkingUtils.IPEndPoint server, ServerPingCompletedHandler completedCallback)
	{
		if (server.m_address.AddressRange == IPv6AddressRange.IPv4Mapped)
		{
			EnqueuePing(server, completedCallback);
		}
		else
		{
			completedCallback?.Invoke(new ServerData(new ServerJoinData(new ServerJoinDataDedicated(server)), ServerMatchmakingData.None));
		}
	}

	public void CheckIfOnlineAsync(ServerJoinDataSteamUser server, ServerPingCompletedHandler completedCallback)
	{
		int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
		bool flag = false;
		for (int i = 0; i < friendCount; i++)
		{
			if (SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate) == server.m_joinUserID)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			completedCallback?.Invoke(new ServerData(new ServerJoinData(server), new ServerMatchmakingData(DateTime.UtcNow, couldCheck: false)));
			return;
		}
		if (!SteamFriends.GetFriendGamePlayed(server.m_joinUserID, out var pFriendGameInfo) || pFriendGameInfo.m_gameID != (CGameID)SteamManager.APP_ID || pFriendGameInfo.m_steamIDLobby == CSteamID.Nil)
		{
			completedCallback?.Invoke(new ServerData(new ServerJoinData(server), new ServerMatchmakingData(DateTime.UtcNow)));
			return;
		}
		if (m_requestedLobbiesUserHosted.TryGetValue(pFriendGameInfo.m_steamIDLobby, out var value))
		{
			ref ServerPingCompletedHandler item = ref value.Item2;
			item = (ServerPingCompletedHandler)Delegate.Combine(item, completedCallback);
		}
		else
		{
			m_requestedLobbiesUserHosted.Add(pFriendGameInfo.m_steamIDLobby, (server.m_joinUserID, completedCallback));
		}
		SteamMatchmaking.RequestLobbyData(pFriendGameInfo.m_steamIDLobby);
	}

	public void AbortCheckIfOnlineAsync(ServerJoinDataSteamUser server)
	{
		CSteamID cSteamID = CSteamID.Nil;
		ServerPingCompletedHandler serverPingCompletedHandler = null;
		foreach (KeyValuePair<CSteamID, (CSteamID, ServerPingCompletedHandler)> item in m_requestedLobbiesUserHosted)
		{
			if (item.Value.Item1 == server.m_joinUserID)
			{
				cSteamID = item.Key;
				serverPingCompletedHandler = item.Value.Item2;
				break;
			}
		}
		if (!(cSteamID == CSteamID.Nil))
		{
			m_requestedLobbiesUserHosted.Remove(cSteamID);
			serverPingCompletedHandler(new ServerData(new ServerJoinData(server), new ServerMatchmakingData(DateTime.UtcNow)));
		}
	}

	public bool CheckIfOnline(ServerJoinData dataToMatchAgainst, ref ServerData serverData)
	{
		for (int i = 0; i < m_friendServers.Count; i++)
		{
			if (m_friendServers[i].m_joinData.Equals(dataToMatchAgainst))
			{
				serverData = m_friendServers[i];
				return true;
			}
		}
		for (int j = 0; j < m_matchmakingServers.Count; j++)
		{
			if (m_matchmakingServers[j].m_joinData.Equals(dataToMatchAgainst))
			{
				serverData = m_matchmakingServers[j];
				return true;
			}
		}
		for (int k = 0; k < m_dedicatedServers.Count; k++)
		{
			if (m_dedicatedServers[k].m_joinData.Equals(dataToMatchAgainst))
			{
				serverData = m_dedicatedServers[k];
				return true;
			}
		}
		if (!IsRefreshing)
		{
			serverData = new ServerData(dataToMatchAgainst, ServerMatchmakingData.None);
			return true;
		}
		return false;
	}

	public bool GetJoinHost(out ServerJoinData joinData)
	{
		if (!m_joinData.IsValid)
		{
			joinData = default(ServerJoinData);
			return false;
		}
		joinData = m_joinData;
		m_joinData = ServerJoinData.None;
		return true;
	}

	private void OnServerResponded(HServerListRequest request, int iServer)
	{
		gameserveritem_t serverDetails = SteamMatchmakingServers.GetServerDetails(request, iServer);
		string serverName = serverDetails.GetServerName();
		SteamNetworkingIPAddr steamNetworkingIPAddr = default(SteamNetworkingIPAddr);
		steamNetworkingIPAddr.SetIPv4(serverDetails.m_NetAdr.GetIP(), serverDetails.m_NetAdr.GetConnectionPort());
		ServerJoinData joinData = new ServerJoinData(new ServerJoinDataDedicated(steamNetworkingIPAddr.GetIPv4(), steamNetworkingIPAddr.m_port));
		DecodeTags(serverDetails.GetGameTags(), out var gameVersion, out var networkVersion, out var modifiers);
		ServerMatchmakingData matchmakingData = new ServerMatchmakingData(DateTime.UtcNow, serverName, (uint)serverDetails.m_nPlayers, (uint)serverDetails.m_nMaxPlayers, PlatformUserID.None, gameVersion, networkVersion, null, serverDetails.m_bPassword, PlatformManager.DistributionPlatform.Platform, modifiers);
		m_dedicatedServers.Add(new ServerData(joinData, matchmakingData));
		m_updateTriggerAccumulator++;
		if (m_updateTriggerAccumulator > 100)
		{
			m_updateTriggerAccumulator = 0;
			m_serverListRevision++;
		}
	}

	private static void DecodeTags(string tagsString, out GameVersion gameVersion, out uint networkVersion, out string[] modifiers)
	{
		string value;
		if (!StringUtils.TryDecodeStringAsIDictionary<Dictionary<string, string>>(tagsString, out var decodedDictionary))
		{
			value = tagsString;
			networkVersion = 0u;
			modifiers = new string[0];
		}
		else
		{
			if ((!decodedDictionary.TryGetValue("g", out value) && !decodedDictionary.TryGetValue("gameversion", out value)) || (!decodedDictionary.TryGetValue("n", out var value2) && !decodedDictionary.TryGetValue("networkversion", out value2)) || !uint.TryParse(value2, out networkVersion))
			{
				value = tagsString;
				networkVersion = 0u;
			}
			if (networkVersion != 36 || !decodedDictionary.TryGetValue("m", out var value3) || !StringUtils.TryDecodeStringAsIDictionary<Dictionary<string, string>>(value3, out var decodedDictionary2) || !ServerOptionsGUI.TryConvertCompactKVPToModifierKeys(decodedDictionary2, out modifiers))
			{
				modifiers = new string[0];
			}
		}
		gameVersion = GameVersion.ParseGameVersion(value);
	}

	private void OnServerFailedToRespond(HServerListRequest request, int iServer)
	{
	}

	private void OnRefreshComplete(HServerListRequest request, EMatchMakingServerResponse response)
	{
		ZLog.Log("Refresh complete " + m_dedicatedServers.Count + "  " + response);
		IsRefreshing = false;
		m_serverListRevision++;
	}

	public void SetNameFilter(string filter)
	{
		if (!(m_nameFilter == filter))
		{
			m_nameFilter = filter;
			m_serverListRevision++;
		}
	}

	public void SetFriendFilter(bool enabled)
	{
		if (m_friendsFilter != enabled)
		{
			m_friendsFilter = enabled;
			m_serverListRevision++;
		}
	}

	public int GetServerListRevision()
	{
		return m_serverListRevision;
	}

	public bool GetServerListRevision(ref int revision)
	{
		bool result = m_serverListRevision != revision;
		revision = m_serverListRevision;
		return result;
	}

	public int GetTotalNrOfServers()
	{
		return m_matchmakingServers.Count + m_dedicatedServers.Count + m_friendServers.Count;
	}
}
