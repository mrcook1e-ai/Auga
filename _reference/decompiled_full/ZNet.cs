using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GUIFramework;
using Splatform;
using Steamworks;
using UnityEngine;
using UnityEngine.Rendering;
using UserManagement;

public class ZNet : MonoBehaviour
{
	public enum ConnectionStatus
	{
		None,
		Connecting,
		Connected,
		ErrorVersion,
		ErrorDisconnected,
		ErrorConnectFailed,
		ErrorPassword,
		ErrorAlreadyConnected,
		ErrorBanned,
		ErrorFull,
		ErrorPlatformExcluded,
		ErrorCrossplayPrivilege,
		ErrorKicked
	}

	public struct CrossNetworkUserInfo : IEquatable<CrossNetworkUserInfo>
	{
		public PlatformUserID m_id;

		public string m_displayName;

		public override bool Equals(object other)
		{
			if (other is CrossNetworkUserInfo)
			{
				return Equals((CrossNetworkUserInfo)other);
			}
			return false;
		}

		public bool Equals(CrossNetworkUserInfo other)
		{
			if (m_id == other.m_id)
			{
				return m_displayName == other.m_displayName;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(m_id, m_displayName);
		}

		public static bool operator ==(CrossNetworkUserInfo lhs, CrossNetworkUserInfo rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(CrossNetworkUserInfo lhs, CrossNetworkUserInfo rhs)
		{
			return !lhs.Equals(rhs);
		}

		public override string ToString()
		{
			return $"{m_displayName} ({m_id})";
		}
	}

	public struct PlayerInfo
	{
		public string m_name;

		public ZDOID m_characterID;

		public CrossNetworkUserInfo m_userInfo;

		public string m_serverAssignedDisplayName;

		public bool m_publicPosition;

		public Vector3 m_position;

		public override string ToString()
		{
			string text = "([";
			text = ((!string.IsNullOrEmpty(m_name)) ? (text + m_name) : (text + "-"));
			text += $", {m_characterID}], [";
			text = ((!string.IsNullOrEmpty(m_userInfo.m_displayName)) ? (text + m_userInfo.m_displayName) : (text + "-"));
			text += $", {m_characterID}], ";
			text = ((!string.IsNullOrEmpty(m_serverAssignedDisplayName)) ? (text + m_serverAssignedDisplayName) : (text + "-"));
			return text + ")";
		}
	}

	public static Action WorldSaveStarted;

	public static Action WorldSaveFinished;

	private float m_banlistTimer;

	private static ZNet m_instance;

	public const int ServerPlayerLimit = 10;

	public int m_hostPort = 2456;

	public RectTransform m_passwordDialog;

	public RectTransform m_connectingDialog;

	public float m_badConnectionPing = 5f;

	public int m_zdoSectorsWidth = 512;

	private ZConnector2 m_serverConnector;

	private ISocket m_hostSocket;

	private readonly List<ZNetPeer> m_peers = new List<ZNetPeer>();

	private readonly List<ZNetPeer> m_peersCopy = new List<ZNetPeer>();

	private Thread m_saveThread;

	private bool m_saveExceededCloudQuota;

	private float m_saveStartTime;

	private float m_saveThreadStartTime;

	private float m_saveDoneTime;

	public static bool m_loadError = false;

	private float m_sendSaveMessage;

	private ZDOMan m_zdoMan;

	private ZRoutedRpc m_routedRpc;

	private ZNat m_nat;

	private double m_netTime = 2040.0;

	private ZDOID m_characterID = ZDOID.None;

	private Vector3 m_referencePosition = Vector3.zero;

	private bool m_publicReferencePosition;

	private float m_periodicSendTimer;

	public Dictionary<string, string> m_serverSyncedPlayerData = new Dictionary<string, string>();

	public static int m_backupCount = 2;

	public static int m_backupShort = 7200;

	public static int m_backupLong = 43200;

	private bool m_haveStoped;

	private static bool m_isServer = true;

	private static World m_world = null;

	private static HttpClient m_httpClient;

	private int m_registerAttempts;

	public static OnlineBackendType m_onlineBackend = OnlineBackendType.Steamworks;

	private static string m_serverPlayFabPlayerId = null;

	private static ulong m_serverSteamID = 0uL;

	private static string m_serverHost = "";

	private static int m_serverHostPort = 0;

	private static bool m_openServer = true;

	private static bool m_publicServer = true;

	private static string m_serverPassword = "";

	private static string m_serverPasswordSalt = "";

	private static string m_ServerName = "";

	private static ConnectionStatus m_connectionStatus = ConnectionStatus.None;

	private static ConnectionStatus m_externalError = ConnectionStatus.None;

	private SyncedList m_adminList;

	private SyncedList m_bannedList;

	private SyncedList m_permittedList;

	private List<PlayerInfo> m_players = new List<PlayerInfo>();

	private List<string> m_adminListForRpc = new List<string>();

	private ZRpc m_tempPasswordRPC;

	private List<CrossNetworkUserInfo> m_playerHistory = new List<CrossNetworkUserInfo>();

	private static readonly Dictionary<ZNetPeer, float> PeersToDisconnectAfterKick = new Dictionary<ZNetPeer, float>();

	private const string PlatformDisplayNameKey = "platformDisplayName";

	private readonly Platform m_steamPlatform = new Platform("Steam");

	public static ZNet instance => m_instance;

	public static bool IsSinglePlayer
	{
		get
		{
			if (m_isServer)
			{
				return !m_openServer;
			}
			return false;
		}
	}

	public ZDOID LocalPlayerCharacterID => m_characterID;

	public List<string> Banned => m_bannedList.GetList();

	public float SaveStartTime => m_saveStartTime;

	public float SaveThreadStartTime => m_saveThreadStartTime;

	public float SaveDoneTime => m_saveDoneTime;

	public bool HaveStopped => m_haveStoped;

	public static World World => m_world;

	private void Awake()
	{
		m_instance = this;
		m_loadError = false;
		m_routedRpc = new ZRoutedRpc(m_isServer);
		m_zdoMan = new ZDOMan(m_zdoSectorsWidth);
		m_passwordDialog.gameObject.SetActive(value: false);
		m_connectingDialog.gameObject.SetActive(value: false);
		WorldGenerator.Deitialize();
		if (!SteamManager.Initialize())
		{
			return;
		}
		string personaName = SteamFriends.GetPersonaName();
		ZLog.Log("Steam initialized, persona:" + personaName);
		ZSteamMatchmaking.Initialize();
		ZPlayFabMatchmaking.Initialize(m_isServer);
		m_backupCount = PlatformPrefs.GetInt("AutoBackups", m_backupCount);
		m_backupShort = PlatformPrefs.GetInt("AutoBackups_short", m_backupShort);
		m_backupLong = PlatformPrefs.GetInt("AutoBackups_long", m_backupLong);
		if (m_isServer)
		{
			FileHelpers.MigrateLocalSyncedListsToCloud();
			if (FileHelpers.LocalStorageSupport == LocalStorageSupport.Supported)
			{
				m_adminList = new SyncedList(new FileHelpers.FileLocation(FileHelpers.FileSource.Local, Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/adminlist.txt"), "List admin players ID  ONE per line");
				m_bannedList = new SyncedList(new FileHelpers.FileLocation(FileHelpers.FileSource.Local, Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/bannedlist.txt"), "List banned players ID  ONE per line");
				m_permittedList = new SyncedList(new FileHelpers.FileLocation(FileHelpers.FileSource.Local, Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/permittedlist.txt"), "List permitted players ID ONE per line");
			}
			else if (FileHelpers.CloudStorageEnabled)
			{
				m_adminList = new SyncedList(new FileHelpers.FileLocation(FileHelpers.FileSource.Cloud, Utils.GetSaveDataPath(FileHelpers.FileSource.Cloud) + "/adminlist.txt"), "List admin players ID  ONE per line");
				m_bannedList = new SyncedList(new FileHelpers.FileLocation(FileHelpers.FileSource.Cloud, Utils.GetSaveDataPath(FileHelpers.FileSource.Cloud) + "/bannedlist.txt"), "List banned players ID  ONE per line");
				m_permittedList = new SyncedList(new FileHelpers.FileLocation(FileHelpers.FileSource.Cloud, Utils.GetSaveDataPath(FileHelpers.FileSource.Cloud) + "/permittedlist.txt"), "List permitted players ID ONE per line");
			}
			else
			{
				ZLog.LogError("Neither Local nor Cloud/Platform storage is enabled on this platform!");
			}
			m_adminListForRpc = m_adminList.GetList();
			if (m_world == null)
			{
				m_publicServer = false;
				m_world = World.GetDevWorld();
			}
			WorldGenerator.Initialize(m_world);
			m_connectionStatus = ConnectionStatus.Connected;
			m_externalError = ConnectionStatus.None;
		}
		m_routedRpc.SetUID(ZDOMan.GetSessionID());
		if (IsServer())
		{
			SendPlayerList();
		}
		if (!IsDedicated())
		{
			m_serverSyncedPlayerData["platformDisplayName"] = PlatformManager.DistributionPlatform.LocalUser.DisplayName;
		}
	}

	private void OnGenerationFinished()
	{
		if (m_openServer)
		{
			OpenServer();
		}
	}

	public void OpenServer()
	{
		if (m_isServer)
		{
			m_openServer = true;
			bool flag = m_serverPassword != "";
			GameVersion currentVersion = Version.CurrentVersion;
			uint networkVersion = 36u;
			string[] modifiers = m_world.m_startingGlobalKeys.ToArray();
			ZSteamMatchmaking.instance.RegisterServer(m_ServerName, flag, currentVersion, modifiers, networkVersion, m_publicServer, m_world.m_seedName, OnSteamServerRegistered);
			if (m_onlineBackend == OnlineBackendType.Steamworks)
			{
				ZSteamSocket zSteamSocket = new ZSteamSocket();
				zSteamSocket.StartHost();
				m_hostSocket = zSteamSocket;
				ZLog.Log("Opened Steam server");
			}
			if (m_onlineBackend == OnlineBackendType.PlayFab)
			{
				ZPlayFabMatchmaking.instance.RegisterServer(m_ServerName, flag, m_publicServer, currentVersion, modifiers, networkVersion, m_world.m_seedName);
				ZPlayFabSocket zPlayFabSocket = new ZPlayFabSocket();
				zPlayFabSocket.StartHost();
				m_hostSocket = zPlayFabSocket;
				ZLog.Log("Opened PlayFab server");
			}
		}
	}

	private void Start()
	{
		ZRpc.SetLongTimeout(enable: false);
		ZLog.Log("ZNET START");
		MuteList.Load(null);
		if (m_isServer)
		{
			ServerLoadWorld();
		}
		else
		{
			ClientConnect();
		}
	}

	private void ServerLoadWorld()
	{
		LoadWorld();
		ZoneSystem.instance.GenerateLocationsIfNeeded();
		ZoneSystem.instance.GenerateLocationsCompleted += OnGenerationFinished;
		if (m_loadError)
		{
			ZLog.LogError("World db couldn't load correctly, saving has been disabled to prevent .old file from being overwritten.");
		}
	}

	private void ClientConnect()
	{
		if (m_onlineBackend == OnlineBackendType.PlayFab)
		{
			ZLog.Log("Connecting to server with PlayFab-backend " + m_serverPlayFabPlayerId);
			Connect(m_serverPlayFabPlayerId);
		}
		if (m_onlineBackend == OnlineBackendType.Steamworks)
		{
			if (m_serverSteamID == 0L)
			{
				ZLog.Log("Connecting to server with Steam-backend " + m_serverHost + ":" + m_serverHostPort);
				SteamNetworkingIPAddr host = default(SteamNetworkingIPAddr);
				host.ParseString(m_serverHost + ":" + m_serverHostPort);
				Connect(host);
				return;
			}
			ZLog.Log("Connecting to server with Steam-backend " + m_serverSteamID);
			Connect(new CSteamID(m_serverSteamID));
		}
		if (m_onlineBackend == OnlineBackendType.CustomSocket)
		{
			ZLog.Log("Connecting to server with socket-backend " + m_serverHost + "  " + m_serverHostPort);
			Connect(m_serverHost, m_serverHostPort);
		}
	}

	private string GetServerIP()
	{
		return GetPublicIP();
	}

	private string LocalIPAddress()
	{
		string text = IPAddress.Loopback.ToString();
		try
		{
			IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
			foreach (IPAddress iPAddress in addressList)
			{
				if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
				{
					text = iPAddress.ToString();
					break;
				}
			}
		}
		catch (Exception ex)
		{
			ZLog.Log($"Failed to get local address, using {text}: {ex.Message}");
		}
		return text;
	}

	public static bool ContainsValidIP(string containsIPAddress, out string ipAddress)
	{
		if (ContainsValidIPv4(containsIPAddress, out var ipAddress2))
		{
			ipAddress = ipAddress2;
			return true;
		}
		ipAddress = "";
		return false;
	}

	private static bool ContainsValidIPv6(string potentialIPv6Address, out string ipAddress)
	{
		if (IPAddress.TryParse(potentialIPv6Address, out var address))
		{
			ipAddress = address.ToString();
			ZLog.Log("Found IPv6 address! Using " + ipAddress + ".");
			return true;
		}
		ipAddress = "";
		return false;
	}

	private static bool ContainsValidIPv4(string containsIPAddress, out string ipAddress)
	{
		MatchCollection matchCollection = new Regex("\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}").Matches(containsIPAddress);
		if (matchCollection.Count > 0)
		{
			ipAddress = matchCollection[0].ToString();
			return true;
		}
		ipAddress = "";
		return false;
	}

	public static string GetPublicIP(int ipGetAttempts = 0)
	{
		if (m_httpClient == null)
		{
			m_httpClient = new HttpClient();
		}
		try
		{
			string[] array = new string[5] { "https://ipv4.icanhazip.com/", "https://api.ipify.org", "https://ipv4.myip.wtf/text", "https://checkip.amazonaws.com/", "https://ipinfo.io/ip/" };
			string[] array2 = new string[3] { "https://ipv6.icanhazip.com/", "https://api6.ipify.org", "https://ipv6.myip.wtf/text" };
			System.Random random = new System.Random();
			string containsIPAddress = DownloadString((ipGetAttempts < 5) ? array[random.Next(array.Length)] : array2[random.Next(array2.Length)]);
			if (ContainsValidIP(containsIPAddress, out var ipAddress))
			{
				return ipAddress;
			}
			throw new Exception("Could not extract valid IP address from externalIP download string.");
		}
		catch (Exception ex)
		{
			ZLog.LogError(ex.Message);
			return "";
		}
		static string DownloadString(string downloadUrl, int timeoutMS = 5000)
		{
			return Task.Run(async () => await DownloadStringAsync(downloadUrl, timeoutMS)).Result;
		}
		static async Task<string> DownloadStringAsync(string downloadUrl, int timeoutMS = 5000)
		{
			_ = 1;
			try
			{
				m_httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMS);
				m_httpClient.GetAsync(downloadUrl);
				HttpResponseMessage obj = await m_httpClient.GetAsync(downloadUrl);
				obj.EnsureSuccessStatusCode();
				return await obj.Content.ReadAsStringAsync();
			}
			catch (Exception ex2)
			{
				Debug.LogError("Exception while waiting for respons from " + downloadUrl + " -> " + ex2.ToString());
				return string.Empty;
			}
		}
	}

	private void OnSteamServerRegistered(bool success)
	{
		if (!success)
		{
			m_registerAttempts++;
			float a = 1f * Mathf.Pow(2f, m_registerAttempts - 1);
			a = Mathf.Min(a, 30f);
			a *= UnityEngine.Random.Range(0.875f, 1.125f);
			RetryRegisterAfterDelay(a);
		}
		IEnumerator DelayThenRegisterCoroutine(float delay)
		{
			ZLog.Log($"Steam register server failed! Retrying in {delay}s, total attempts: {m_registerAttempts}");
			DateTime NextRetryUtc = DateTime.UtcNow + TimeSpan.FromSeconds(delay);
			while (DateTime.UtcNow < NextRetryUtc)
			{
				yield return null;
			}
			bool password = m_serverPassword != "";
			GameVersion currentVersion = Version.CurrentVersion;
			uint networkVersion = 36u;
			string[] modifiers = m_world.m_startingGlobalKeys.ToArray();
			ZSteamMatchmaking.instance.RegisterServer(m_ServerName, password, currentVersion, modifiers, networkVersion, m_publicServer, m_world.m_seedName, OnSteamServerRegistered);
		}
		void RetryRegisterAfterDelay(float delay)
		{
			StartCoroutine(DelayThenRegisterCoroutine(delay));
		}
	}

	public void Shutdown(bool save = true)
	{
		ZLog.Log("ZNet Shutdown");
		if (save)
		{
			Save(sync: true);
		}
		StopAll();
		base.enabled = false;
	}

	public void ShutdownWithoutSave(bool suspending)
	{
		ZLog.Log("ZNet Shutdown without save");
		StopAll(suspending);
		base.enabled = false;
	}

	private void StopAll(bool suspending = false)
	{
		if (m_haveStoped)
		{
			return;
		}
		m_haveStoped = true;
		if (m_saveThread != null && m_saveThread.IsAlive)
		{
			m_saveThread.Join();
			m_saveThread = null;
		}
		if (!suspending)
		{
			m_zdoMan.ShutDown();
		}
		SendDisconnect();
		ZSteamMatchmaking.instance.ReleaseSessionTicket();
		ZSteamMatchmaking.instance.UnregisterServer();
		ZPlayFabMatchmaking.instance?.UnregisterServer();
		if (m_hostSocket != null)
		{
			m_hostSocket.Dispose();
		}
		if (m_serverConnector != null)
		{
			m_serverConnector.Dispose();
		}
		foreach (ZNetPeer peer in m_peers)
		{
			peer.Dispose();
		}
		m_peers.Clear();
	}

	private void OnDestroy()
	{
		ZLog.Log("ZNet OnDestroy");
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	private ZNetPeer Connect(ISocket socket)
	{
		ZNetPeer zNetPeer = new ZNetPeer(socket, server: true);
		OnNewConnection(zNetPeer);
		m_connectionStatus = ConnectionStatus.Connecting;
		m_externalError = ConnectionStatus.None;
		m_connectingDialog.gameObject.SetActive(value: true);
		return zNetPeer;
	}

	public void Connect(string remotePlayerId)
	{
		ZPlayFabSocket socket = null;
		ZNetPeer peer = null;
		socket = new ZPlayFabSocket(remotePlayerId, CheckServerData);
		peer = Connect(socket);
		void CheckServerData(PlayFabMatchmakingServerData serverData)
		{
			if (socket != null)
			{
				if (serverData == null)
				{
					ZLog.LogWarning("Failed to join server '" + serverData.serverName + "' because the found session has incompatible data!");
					m_connectionStatus = ConnectionStatus.ErrorVersion;
					Disconnect(peer);
				}
				else if (!(serverData.platformRestriction == PlatformManager.DistributionPlatform.Platform) && PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.CrossPlatformMultiplayer) != PrivilegeResult.Granted)
				{
					ZLog.LogWarning($"Failed to join server '{serverData.serverName}' due to the local user's privilege settings. The server owner's platform restrictions are '{serverData.platformRestriction}'");
					m_connectionStatus = ConnectionStatus.ErrorCrossplayPrivilege;
					Disconnect(peer);
					if (PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege != null)
					{
						PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.Open(Privilege.CrossPlatformMultiplayer);
					}
				}
			}
		}
	}

	public void Connect(CSteamID hostID)
	{
		Connect(new ZSteamSocket(hostID));
	}

	public void Connect(SteamNetworkingIPAddr host)
	{
		Connect(new ZSteamSocket(host));
	}

	public void Connect(string host, int port)
	{
		m_serverConnector = new ZConnector2(host, port);
		m_connectionStatus = ConnectionStatus.Connecting;
		m_externalError = ConnectionStatus.None;
		m_connectingDialog.gameObject.SetActive(value: true);
	}

	private void UpdateClientConnector(float dt)
	{
		if (m_serverConnector != null && m_serverConnector.UpdateStatus(dt, logErrors: true))
		{
			ZSocket2 zSocket = m_serverConnector.Complete();
			if (zSocket != null)
			{
				ZLog.Log("Connection established to " + m_serverConnector.GetEndPointString());
				ZNetPeer peer = new ZNetPeer(zSocket, server: true);
				OnNewConnection(peer);
			}
			else
			{
				m_connectionStatus = ConnectionStatus.ErrorConnectFailed;
				ZLog.Log("Failed to connect to server");
			}
			m_serverConnector.Dispose();
			m_serverConnector = null;
		}
	}

	private void OnNewConnection(ZNetPeer peer)
	{
		m_peers.Add(peer);
		peer.m_rpc.Register<ZPackage>("PeerInfo", RPC_PeerInfo);
		peer.m_rpc.Register("Disconnect", RPC_Disconnect);
		peer.m_rpc.Register("SavePlayerProfile", RPC_SavePlayerProfile);
		if (m_isServer)
		{
			peer.m_rpc.Register("ServerHandshake", RPC_ServerHandshake);
			return;
		}
		peer.m_rpc.Register("Kicked", RPC_Kicked);
		peer.m_rpc.Register<int>("Error", RPC_Error);
		peer.m_rpc.Register<bool, string>("ClientHandshake", RPC_ClientHandshake);
		peer.m_rpc.Invoke("ServerHandshake");
	}

	public void SaveOtherPlayerProfiles()
	{
		ZLog.Log("Sending message to save player profiles");
		if (!IsServer())
		{
			ZLog.Log("Only server can save the player profiles");
			return;
		}
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.m_rpc != null)
			{
				ZLog.Log("Sent to " + peer.m_socket.GetEndPointString());
				peer.m_rpc.Invoke("SavePlayerProfile");
			}
		}
	}

	private void RPC_SavePlayerProfile(ZRpc rpc)
	{
		Game.instance.SavePlayerProfile(setLogoutPoint: true);
	}

	private void RPC_ServerHandshake(ZRpc rpc)
	{
		ZNetPeer peer = GetPeer(rpc);
		if (peer != null)
		{
			ZLog.Log("Got handshake from client " + peer.m_socket.GetEndPointString());
			ClearPlayerData(peer);
			bool flag = !string.IsNullOrEmpty(m_serverPassword);
			peer.m_rpc.Invoke("ClientHandshake", flag, ServerPasswordSalt());
		}
	}

	public bool InPasswordDialog()
	{
		return m_passwordDialog.gameObject.activeSelf;
	}

	public bool InConnectingScreen()
	{
		return m_connectingDialog.gameObject.activeSelf;
	}

	private void RPC_ClientHandshake(ZRpc rpc, bool needPassword, string serverPasswordSalt)
	{
		m_connectingDialog.gameObject.SetActive(value: false);
		m_serverPasswordSalt = serverPasswordSalt;
		if (needPassword)
		{
			m_passwordDialog.gameObject.SetActive(value: true);
			GuiInputField componentInChildren = m_passwordDialog.GetComponentInChildren<GuiInputField>();
			componentInChildren.text = "";
			componentInChildren.ActivateInputField();
			componentInChildren.OnInputSubmit.AddListener(OnPasswordEntered);
			m_tempPasswordRPC = rpc;
			if (FejdStartup.ServerPassword != null)
			{
				OnPasswordEntered(FejdStartup.ServerPassword);
			}
		}
		else
		{
			SendPeerInfo(rpc);
		}
	}

	private void OnPasswordEntered(string pwd)
	{
		if (m_tempPasswordRPC.IsConnected() && !string.IsNullOrEmpty(pwd))
		{
			m_passwordDialog.GetComponentInChildren<GuiInputField>().OnInputSubmit.RemoveListener(OnPasswordEntered);
			m_passwordDialog.gameObject.SetActive(value: false);
			SendPeerInfo(m_tempPasswordRPC, pwd);
			m_tempPasswordRPC = null;
		}
	}

	private void SendPeerInfo(ZRpc rpc, string password = "")
	{
		ZPackage zPackage = new ZPackage();
		zPackage.Write(GetUID());
		zPackage.Write(Version.CurrentVersion.ToString());
		zPackage.Write(36u);
		zPackage.Write(m_referencePosition);
		zPackage.Write(Game.instance.GetPlayerProfile().GetName());
		if (IsServer())
		{
			zPackage.Write(m_world.m_name);
			zPackage.Write(m_world.m_seed);
			zPackage.Write(m_world.m_seedName);
			zPackage.Write(m_world.m_uid);
			zPackage.Write(m_world.m_worldGenVersion);
			zPackage.Write(m_netTime);
		}
		else
		{
			string data = (string.IsNullOrEmpty(password) ? "" : HashPassword(password, ServerPasswordSalt()));
			zPackage.Write(data);
			rpc.GetSocket().GetHostName();
			SteamNetworkingIdentity serverIdentity = default(SteamNetworkingIdentity);
			serverIdentity.SetSteamID(new CSteamID(m_serverSteamID));
			byte[] array = ZSteamMatchmaking.instance.RequestSessionTicket(ref serverIdentity);
			if (array == null)
			{
				m_connectionStatus = ConnectionStatus.ErrorConnectFailed;
				return;
			}
			zPackage.Write(array);
		}
		rpc.Invoke("PeerInfo", zPackage);
	}

	private void RPC_PeerInfo(ZRpc rpc, ZPackage pkg)
	{
		ZNetPeer peer = GetPeer(rpc);
		if (peer == null)
		{
			return;
		}
		long uid = pkg.ReadLong();
		string versionString = pkg.ReadString();
		uint num = 0u;
		if (GameVersion.TryParseGameVersion(versionString, out var version) && version >= Version.FirstVersionWithNetworkVersion)
		{
			num = pkg.ReadUInt();
		}
		string name = peer.m_socket.GetEndPointString();
		string hostName = peer.m_socket.GetHostName();
		ZLog.Log("Network version check, their:" + num + ", mine:" + 36u);
		if (num != 36)
		{
			if (m_isServer)
			{
				rpc.Invoke("Error", 3);
			}
			else
			{
				m_connectionStatus = ConnectionStatus.ErrorVersion;
			}
			string[] obj = new string[11]
			{
				"Peer ",
				name,
				" has incompatible version, mine:",
				Version.CurrentVersion.ToString(),
				" (network version ",
				36u.ToString(),
				")   remote ",
				null,
				null,
				null,
				null
			};
			GameVersion gameVersion = version;
			obj[7] = gameVersion.ToString();
			obj[8] = " (network version ";
			obj[9] = ((num == uint.MaxValue) ? "unknown" : num.ToString());
			obj[10] = ")";
			ZLog.Log(string.Concat(obj));
			return;
		}
		Vector3 refPos = pkg.ReadVector3();
		string text = pkg.ReadString();
		if (m_isServer)
		{
			if (!IsAllowed(hostName, text))
			{
				rpc.Invoke("Error", 8);
				ZLog.Log("Player " + text + " : " + hostName + " is blacklisted or not in whitelist.");
				return;
			}
			string text2 = pkg.ReadString();
			if (m_onlineBackend == OnlineBackendType.Steamworks)
			{
				ZSteamSocket zSteamSocket = peer.m_socket as ZSteamSocket;
				byte[] ticket = pkg.ReadByteArray();
				if (!ZSteamMatchmaking.instance.VerifySessionTicket(ticket, zSteamSocket.GetPeerID()))
				{
					ZLog.Log("Peer " + name + " has invalid session ticket");
					rpc.Invoke("Error", 8);
					return;
				}
			}
			if (m_onlineBackend == OnlineBackendType.PlayFab)
			{
				PlatformUserID platformUserId = new PlatformUserID(peer.m_socket.GetHostName());
				if (!platformUserId.IsValid)
				{
					ZLog.LogError("Failed to parse peer id! Using blank ID with unknown platform.");
					platformUserId = PlatformUserID.None;
				}
				if (PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.CrossPlatformMultiplayer) != PrivilegeResult.Granted && PlatformManager.DistributionPlatform.Platform != platformUserId.m_platform)
				{
					rpc.Invoke("Error", 10);
					ZLog.Log("Peer diconnected due to server platform privileges disallowing crossplay. Server platform: " + PlatformManager.DistributionPlatform.Platform.ToString() + "   Peer platform: " + platformUserId.m_platform.ToString());
					return;
				}
				PlayFabManager.CheckIfUserAuthenticated((peer.m_socket as ZPlayFabSocket).m_remotePlayerId, platformUserId, delegate(bool isAuthenticated)
				{
					if (!isAuthenticated)
					{
						rpc.Invoke("Error", 5);
						ZLog.Log("Peer " + name + " disconnected because they were not authenticated!");
					}
				});
			}
			if (GetNrOfPlayers() >= 10)
			{
				rpc.Invoke("Error", 9);
				ZLog.Log("Peer " + name + " disconnected due to server is full");
				return;
			}
			if (m_serverPassword != text2)
			{
				rpc.Invoke("Error", 6);
				ZLog.Log("Peer " + name + " has wrong password");
				return;
			}
			if (IsConnected(uid))
			{
				rpc.Invoke("Error", 7);
				ZLog.Log("Already connected to peer with UID:" + uid + "  " + name);
				return;
			}
		}
		else
		{
			m_world = new World();
			m_world.m_name = pkg.ReadString();
			m_world.m_seed = pkg.ReadInt();
			m_world.m_seedName = pkg.ReadString();
			m_world.m_uid = pkg.ReadLong();
			m_world.m_worldGenVersion = pkg.ReadInt();
			WorldGenerator.Initialize(m_world);
			m_netTime = pkg.ReadDouble();
		}
		peer.m_refPos = refPos;
		peer.m_uid = uid;
		peer.m_playerName = text;
		rpc.Register<ZPackage>("ServerSyncedPlayerData", RPC_ServerSyncedPlayerData);
		rpc.Register<ZPackage>("PlayerList", RPC_PlayerList);
		rpc.Register<ZPackage>("AdminList", RPC_AdminList);
		rpc.Register<string>("RemotePrint", RPC_RemotePrint);
		if (m_isServer)
		{
			rpc.Register<ZDOID>("CharacterID", RPC_CharacterID);
			rpc.Register<string>("Kick", RPC_Kick);
			rpc.Register<string>("Ban", RPC_Ban);
			rpc.Register<string>("Unban", RPC_Unban);
			rpc.Register<string>("RPC_RemoteCommand", RPC_RemoteCommand);
			rpc.Register("Save", RPC_Save);
			rpc.Register("PrintBanned", RPC_PrintBanned);
		}
		else
		{
			rpc.Register<double>("NetTime", RPC_NetTime);
		}
		if (m_isServer)
		{
			SendPeerInfo(rpc);
			peer.m_socket.VersionMatch();
			SendPlayerList();
			SendAdminList();
		}
		else
		{
			peer.m_socket.VersionMatch();
			m_connectionStatus = ConnectionStatus.Connected;
		}
		m_zdoMan.AddPeer(peer);
		m_routedRpc.AddPeer(peer);
	}

	private void SendDisconnect()
	{
		ZLog.Log("Sending disconnect msg");
		foreach (ZNetPeer peer in m_peers)
		{
			SendDisconnect(peer);
		}
	}

	private void SendDisconnect(ZNetPeer peer)
	{
		if (peer.m_rpc != null)
		{
			ZLog.Log("Sent to " + peer.m_socket.GetEndPointString());
			peer.m_rpc.Invoke("Disconnect");
		}
	}

	private void RPC_Disconnect(ZRpc rpc)
	{
		ZLog.Log("RPC_Disconnect");
		ZNetPeer peer = GetPeer(rpc);
		if (peer != null)
		{
			if (peer.m_server)
			{
				m_connectionStatus = ConnectionStatus.ErrorDisconnected;
			}
			Disconnect(peer);
		}
	}

	private void RPC_Error(ZRpc rpc, int error)
	{
		ConnectionStatus connectionStatus = (m_connectionStatus = (ConnectionStatus)error);
		ZLog.Log("Got connectoin error msg " + connectionStatus);
	}

	public bool IsConnected(long uid)
	{
		if (uid == GetUID())
		{
			return true;
		}
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.m_uid == uid)
			{
				return true;
			}
		}
		return false;
	}

	private void ClearPlayerData(ZNetPeer peer)
	{
		m_routedRpc.RemovePeer(peer);
		m_zdoMan.RemovePeer(peer);
	}

	public void Disconnect(ZNetPeer peer)
	{
		ClearPlayerData(peer);
		m_peers.Remove(peer);
		peer.Dispose();
		if (m_isServer)
		{
			SendPlayerList();
		}
	}

	private void FixedUpdate()
	{
		UpdateNetTime(Time.fixedDeltaTime);
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		ZSteamSocket.UpdateAllSockets(deltaTime);
		ZPlayFabSocket.UpdateAllSockets(deltaTime);
		if (IsServer())
		{
			UpdateBanList(deltaTime);
		}
		CheckForIncommingServerConnections();
		UpdatePeers(deltaTime);
		SendPeriodicData(deltaTime);
		m_zdoMan.Update(deltaTime);
		UpdateSave();
		if (PeersToDisconnectAfterKick.Count < 1)
		{
			return;
		}
		ZNetPeer[] array = PeersToDisconnectAfterKick.Keys.ToArray();
		foreach (ZNetPeer zNetPeer in array)
		{
			if (!(Time.time < PeersToDisconnectAfterKick[zNetPeer]))
			{
				Disconnect(zNetPeer);
				PeersToDisconnectAfterKick.Remove(zNetPeer);
			}
		}
	}

	private void LateUpdate()
	{
		ZPlayFabSocket.LateUpdateAllSocket();
	}

	private void UpdateNetTime(float dt)
	{
		if (IsServer())
		{
			if (GetNrOfPlayers() > 0)
			{
				m_netTime += dt;
			}
		}
		else
		{
			m_netTime += dt;
		}
	}

	private void UpdateBanList(float dt)
	{
		m_banlistTimer += dt;
		if (!(m_banlistTimer > 5f))
		{
			return;
		}
		m_banlistTimer = 0f;
		CheckWhiteList();
		foreach (string item in m_bannedList.GetList())
		{
			InternalKick(item);
		}
	}

	private void CheckWhiteList()
	{
		if (m_permittedList.Count() == 0)
		{
			return;
		}
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady() && !PeersToDisconnectAfterKick.ContainsKey(peer))
			{
				string hostName = peer.m_socket.GetHostName();
				if (!ListContainsId(m_permittedList, hostName))
				{
					ZLog.Log("Kicking player not in permitted list " + peer.m_playerName + " host: " + hostName);
					InternalKick(peer);
				}
			}
		}
	}

	public bool IsSaving()
	{
		return m_saveThread != null;
	}

	public void SaveWorldAndPlayerProfiles()
	{
		if (IsServer())
		{
			RPC_Save(null);
		}
		else
		{
			GetServerRPC()?.Invoke("Save");
		}
	}

	private void RPC_Save(ZRpc rpc)
	{
		if (base.enabled)
		{
			bool exitGamePopupShown;
			if (rpc != null && !ListContainsId(m_adminList, rpc.GetSocket().GetHostName()))
			{
				RemotePrint(rpc, "You are not admin");
			}
			else if (EnoughDiskSpaceAvailable(out exitGamePopupShown))
			{
				RemotePrint(rpc, "Saving..");
				Game.instance.SavePlayerProfile(setLogoutPoint: true);
				Save(sync: false, saveOtherPlayerProfiles: true, !IsDedicated());
			}
		}
	}

	private bool ListContainsId(SyncedList list, string idString)
	{
		if (!PlatformUserID.TryParse(idString, out var platform))
		{
			platform = new PlatformUserID(m_steamPlatform, idString);
		}
		if (platform.m_platform == m_steamPlatform)
		{
			if (!list.Contains(platform.ToString()))
			{
				return list.Contains(platform.m_userID.ToString());
			}
			return true;
		}
		return list.Contains(platform.ToString());
	}

	public void Save(bool sync, bool saveOtherPlayerProfiles = false, bool waitForNextFrame = false)
	{
		Game.instance.m_saveTimer = 0f;
		if (m_loadError || ZoneSystem.instance.SkipSaving() || DungeonDB.instance.SkipSaving())
		{
			ZLog.LogWarning("Skipping world save");
		}
		else if (m_isServer && m_world != null)
		{
			if (saveOtherPlayerProfiles)
			{
				SaveOtherPlayerProfiles();
			}
			if (!waitForNextFrame)
			{
				SaveWorld(sync);
			}
			else
			{
				StartCoroutine(DelayedSave(sync));
			}
		}
	}

	private IEnumerator DelayedSave(bool sync)
	{
		yield return null;
		SaveWorld(sync);
	}

	public bool EnoughDiskSpaceAvailable(out bool exitGamePopupShown, bool exitGamePrompt = false, Action<bool> onDecisionMade = null)
	{
		exitGamePopupShown = false;
		string worldSavePath = "";
		World worldIfIsHost = GetWorldIfIsHost();
		FileHelpers.FileSource worldFileSource = FileHelpers.FileSource.Cloud;
		if (worldIfIsHost != null)
		{
			worldSavePath = worldIfIsHost.GetDBPath();
			worldFileSource = worldIfIsHost.m_fileSource;
		}
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		FileHelpers.CheckDiskSpace(worldSavePath, playerProfile.GetPath(), worldFileSource, playerProfile.m_fileSource, out var availableFreeSpace, out var byteLimitWarning, out var byteLimitBlock);
		if (availableFreeSpace <= byteLimitBlock || availableFreeSpace <= byteLimitWarning)
		{
			LowDiskLeftInformer(availableFreeSpace, byteLimitWarning, byteLimitBlock, exitGamePrompt, onDecisionMade);
		}
		if (availableFreeSpace <= byteLimitBlock)
		{
			if (exitGamePrompt)
			{
				exitGamePopupShown = true;
			}
			ZLog.LogWarning("Not enough space left to save. ");
			return false;
		}
		return true;
	}

	private void LowDiskLeftInformer(ulong availableFreeSpace, ulong byteLimitWarning, ulong byteLimitBlock, bool exitGamePrompt, Action<bool> onDecisionMade)
	{
		if (availableFreeSpace <= byteLimitBlock)
		{
			if (IsDedicated())
			{
				MessageHud.instance.MessageAll(MessageHud.MessageType.Center, "$msg_worldsaveblockedonserver");
			}
			else if (exitGamePrompt)
			{
				string text = "$menu_lowdisk_block_exitanyway_prompt";
				UnifiedPopup.Push(new YesNoPopup("$menu_lowdisk_block_exitanyway_header", text, delegate
				{
					onDecisionMade?.Invoke(obj: true);
					UnifiedPopup.Pop();
				}, delegate
				{
					onDecisionMade?.Invoke(obj: false);
					UnifiedPopup.Pop();
				}));
			}
			else
			{
				SavingBlockedPopup();
			}
		}
		else if (IsDedicated())
		{
			MessageHud.instance.MessageAll(MessageHud.MessageType.Center, "$msg_worldsavewarningonserver");
		}
		else
		{
			SaveLowDiskWarningPopup();
		}
		ZLog.LogWarning($"Running low on disk space... Available space: {availableFreeSpace} bytes.");
	}

	private void SavingBlockedPopup()
	{
		string text = "$menu_lowdisk_message_block";
		UnifiedPopup.Push(new WarningPopup("$menu_lowdisk_header_block", text, delegate
		{
			UnifiedPopup.Pop();
		}));
	}

	private void SaveLowDiskWarningPopup()
	{
		string text = "$menu_lowdisk_message_warn";
		UnifiedPopup.Push(new WarningPopup("$menu_lowdisk_header_warn", text, delegate
		{
			UnifiedPopup.Pop();
		}));
	}

	public bool LocalPlayerIsAdminOrHost()
	{
		if (IsServer())
		{
			return true;
		}
		return PlayerIsAdmin(UserInfo.GetLocalUser().UserId);
	}

	public bool PlayerIsAdmin(PlatformUserID networkUserId)
	{
		List<string> adminList = GetAdminList();
		if (networkUserId.IsValid && adminList != null && adminList.Contains(networkUserId.ToString()))
		{
			return true;
		}
		return false;
	}

	public static World GetWorldIfIsHost()
	{
		if (m_isServer)
		{
			return m_world;
		}
		return null;
	}

	private void SendPeriodicData(float dt)
	{
		m_periodicSendTimer += dt;
		if (!(m_periodicSendTimer >= 2f))
		{
			return;
		}
		m_periodicSendTimer = 0f;
		if (IsServer())
		{
			SendNetTime();
			SendPlayerList();
			return;
		}
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady())
			{
				SendServerSyncPlayerData(peer);
			}
		}
	}

	private void SendServerSyncPlayerData(ZNetPeer peer)
	{
		ZPackage zPackage = new ZPackage();
		zPackage.Write(m_referencePosition);
		zPackage.Write(m_publicReferencePosition);
		zPackage.Write(m_serverSyncedPlayerData.Count);
		foreach (KeyValuePair<string, string> serverSyncedPlayerDatum in m_serverSyncedPlayerData)
		{
			zPackage.Write(serverSyncedPlayerDatum.Key);
			zPackage.Write(serverSyncedPlayerDatum.Value);
		}
		peer.m_rpc.Invoke("ServerSyncedPlayerData", zPackage);
	}

	private void SendNetTime()
	{
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady())
			{
				peer.m_rpc.Invoke("NetTime", m_netTime);
			}
		}
	}

	private void RPC_NetTime(ZRpc rpc, double time)
	{
		m_netTime = time;
	}

	private void RPC_ServerSyncedPlayerData(ZRpc rpc, ZPackage data)
	{
		RandEventSystem.SetRandomEventsNeedsRefresh();
		ZNetPeer peer = GetPeer(rpc);
		if (peer != null)
		{
			peer.m_refPos = data.ReadVector3();
			peer.m_publicRefPos = data.ReadBool();
			peer.m_serverSyncedPlayerData.Clear();
			int num = data.ReadInt();
			for (int i = 0; i < num; i++)
			{
				peer.m_serverSyncedPlayerData.Add(data.ReadString(), data.ReadString());
			}
		}
	}

	private void UpdatePeers(float dt)
	{
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.m_rpc.IsConnected())
			{
				continue;
			}
			if (peer.m_server)
			{
				if (m_externalError != ConnectionStatus.None)
				{
					m_connectionStatus = m_externalError;
				}
				else if (m_connectionStatus == ConnectionStatus.Connecting)
				{
					m_connectionStatus = ConnectionStatus.ErrorConnectFailed;
				}
				else
				{
					m_connectionStatus = ConnectionStatus.ErrorDisconnected;
				}
			}
			Disconnect(peer);
			break;
		}
		m_peersCopy.Clear();
		m_peersCopy.AddRange(m_peers);
		foreach (ZNetPeer item in m_peersCopy)
		{
			if (item.m_rpc.Update(dt) == ZRpc.ErrorCode.IncompatibleVersion)
			{
				m_connectionStatus = ConnectionStatus.ErrorVersion;
			}
		}
	}

	private void CheckForIncommingServerConnections()
	{
		if (m_hostSocket == null)
		{
			return;
		}
		ISocket socket = m_hostSocket.Accept();
		if (socket != null)
		{
			if (!socket.IsConnected())
			{
				socket.Dispose();
				return;
			}
			ZNetPeer peer = new ZNetPeer(socket, server: false);
			OnNewConnection(peer);
		}
	}

	public ZNetPeer GetPeerByPlayerName(string name)
	{
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady() && peer.m_playerName == name)
			{
				return peer;
			}
		}
		return null;
	}

	public ZNetPeer GetPeerByHostName(string endpoint)
	{
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady() && peer.m_socket.GetHostName() == endpoint)
			{
				return peer;
			}
		}
		return null;
	}

	public ZNetPeer GetPeer(long uid)
	{
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.m_uid == uid)
			{
				return peer;
			}
		}
		return null;
	}

	private ZNetPeer GetPeer(ZRpc rpc)
	{
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.m_rpc == rpc)
			{
				return peer;
			}
		}
		return null;
	}

	public List<ZNetPeer> GetConnectedPeers()
	{
		return new List<ZNetPeer>(m_peers);
	}

	private void SaveWorld(bool sync)
	{
		WorldSaveStarted?.Invoke();
		if (m_saveThread != null && m_saveThread.IsAlive)
		{
			m_saveThread.Join();
			m_saveThread = null;
		}
		m_saveStartTime = Time.realtimeSinceStartup;
		m_zdoMan.PrepareSave();
		ZoneSystem.instance.PrepareSave();
		RandEventSystem.instance.PrepareSave();
		m_backupCount = PlatformPrefs.GetInt("AutoBackups", m_backupCount);
		m_saveThreadStartTime = Time.realtimeSinceStartup;
		m_saveThread = new Thread(SaveWorldThread);
		m_saveThread.Start();
		if (sync)
		{
			m_saveThread.Join();
			m_saveThread = null;
			m_sendSaveMessage = 0.5f;
		}
	}

	private void UpdateSave()
	{
		if (m_sendSaveMessage > 0f)
		{
			m_sendSaveMessage -= Time.fixedDeltaTime;
			if (m_sendSaveMessage < 0f)
			{
				PrintWorldSaveMessage();
				m_sendSaveMessage = 0f;
			}
		}
		if (m_saveThread != null && !m_saveThread.IsAlive)
		{
			m_saveThread = null;
			m_sendSaveMessage = 0.5f;
		}
	}

	private void PrintWorldSaveMessage()
	{
		float num = m_saveThreadStartTime - m_saveStartTime;
		float num2 = Time.realtimeSinceStartup - m_saveThreadStartTime;
		m_saveDoneTime = Time.realtimeSinceStartup;
		if (m_saveExceededCloudQuota)
		{
			m_saveExceededCloudQuota = false;
			MessageHud.instance.MessageAll(MessageHud.MessageType.TopLeft, "$msg_worldsavedcloudstoragefull ( " + num.ToString("0.00") + "+" + num2.ToString("0.00") + "s )");
		}
		else
		{
			MessageHud.instance.MessageAll(MessageHud.MessageType.TopLeft, "$msg_worldsaved ( " + num.ToString("0.00") + "+" + num2.ToString("0.00") + "s )");
		}
		WorldSaveFinished?.Invoke();
	}

	private void SaveWorldThread()
	{
		DateTime now = DateTime.Now;
		try
		{
			ulong num = 52428800uL;
			num += FileHelpers.GetFileSize(m_world.GetMetaPath(), m_world.m_fileSource);
			if (FileHelpers.Exists(m_world.GetDBPath(), m_world.m_fileSource))
			{
				num += FileHelpers.GetFileSize(m_world.GetDBPath(), m_world.m_fileSource);
			}
			bool flag = SaveSystem.CheckMove(m_world.m_fileName, SaveDataType.World, ref m_world.m_fileSource, now, num);
			bool flag2 = m_world.m_createBackupBeforeSaving && !flag;
			if (FileHelpers.CloudStorageEnabled && m_world.m_fileSource == FileHelpers.FileSource.Cloud)
			{
				num *= (ulong)(flag2 ? 3 : 2);
				if (FileHelpers.OperationExceedsCloudCapacity(num))
				{
					if (!FileHelpers.LocalStorageSupported)
					{
						throw new Exception("The world save operation may exceed the cloud save quota and was therefore not performed!");
					}
					string metaPath = m_world.GetMetaPath();
					string dBPath = m_world.GetDBPath();
					m_world.m_fileSource = FileHelpers.FileSource.Local;
					string metaPath2 = m_world.GetMetaPath();
					string dBPath2 = m_world.GetDBPath();
					FileHelpers.FileCopyOutFromCloud(metaPath, metaPath2, deleteOnCloud: true);
					if (FileHelpers.FileExistsCloud(dBPath))
					{
						FileHelpers.FileCopyOutFromCloud(dBPath, dBPath2, deleteOnCloud: true);
					}
					SaveSystem.InvalidateCache();
					ZLog.LogWarning("The world save operation may exceed the cloud save quota and it has therefore been moved to local storage!");
					m_saveExceededCloudQuota = true;
				}
			}
			if (flag2)
			{
				if (SaveSystem.TryGetSaveByName(m_world.m_fileName, SaveDataType.World, out var save) && !save.IsDeleted)
				{
					if (SaveSystem.CreateBackup(save.PrimaryFile, DateTime.Now, m_world.m_fileSource))
					{
						ZLog.Log("Migrating world save from an old save format, created backup!");
					}
					else
					{
						ZLog.LogError("Failed to create backup of world save " + m_world.m_fileName + "!");
					}
				}
				else
				{
					ZLog.LogError("Failed to get world save " + m_world.m_fileName + " from save system, so a backup couldn't be created!");
				}
			}
			m_world.m_createBackupBeforeSaving = false;
			DateTime now2 = DateTime.Now;
			bool flag3 = m_world.m_fileSource != FileHelpers.FileSource.Cloud;
			string dBPath3 = m_world.GetDBPath();
			string text = (flag3 ? (dBPath3 + ".new") : dBPath3);
			string oldFile = dBPath3 + ".old";
			ZLog.Log("World save writing starting");
			FileWriter fileWriter = new FileWriter(text, FileHelpers.FileHelperType.Binary, m_world.m_fileSource);
			ZLog.Log("World save writing started");
			BinaryWriter binary = fileWriter.m_binary;
			binary.Write(37);
			binary.Write(m_netTime);
			m_zdoMan.SaveAsync(binary);
			ZoneSystem.instance.SaveASync(binary);
			RandEventSystem.instance.SaveAsync(binary);
			ZLog.Log("World save writing finishing");
			fileWriter.Finish();
			SaveSystem.InvalidateCache();
			ZLog.Log("World save writing finished");
			m_world.m_needsDB = true;
			m_world.SaveWorldMetaData(now, considerBackup: false, out var _, out var metaWriter);
			if (m_world.m_fileSource == FileHelpers.FileSource.Cloud && (metaWriter.Status == FileWriter.WriterStatus.CloseFailed || fileWriter.Status == FileWriter.WriterStatus.CloseFailed))
			{
				string text2 = GetBackupPath(m_world.GetMetaPath(FileHelpers.FileSource.Local), now);
				string text3 = GetBackupPath(m_world.GetDBPath(FileHelpers.FileSource.Local), now);
				metaWriter.DumpCloudWriteToLocalFile(text2);
				fileWriter.DumpCloudWriteToLocalFile(text3);
				SaveSystem.InvalidateCache();
				string text4 = "";
				if (metaWriter.Status == FileWriter.WriterStatus.CloseFailed)
				{
					text4 = text4 + "Cloud save to location \"" + m_world.GetMetaPath() + "\" failed!\n";
				}
				if (fileWriter.Status == FileWriter.WriterStatus.CloseFailed)
				{
					text4 = text4 + "Cloud save to location \"" + dBPath3 + "\" failed!\n ";
				}
				text4 = text4 + "Saved world as local backup \"" + text2 + "\" and \"" + text3 + "\". Use the \"Manage saves\" menu to restore this backup.";
				ZLog.LogError(text4);
			}
			else
			{
				if (flag3)
				{
					FileHelpers.ReplaceOldFile(dBPath3, text, oldFile, m_world.m_fileSource);
					SaveSystem.InvalidateCache();
				}
				ZLog.Log("World saved ( " + (DateTime.Now - now2).TotalMilliseconds + "ms )");
				now2 = DateTime.Now;
				if (ConsiderAutoBackup(m_world.m_fileName, SaveDataType.World, now))
				{
					ZLog.Log("World auto backup saved ( " + (DateTime.Now - now2).ToString() + "ms )");
				}
			}
		}
		catch (Exception ex)
		{
			ZLog.LogError("Error saving world! " + ex.Message);
			Terminal.m_threadSafeMessages.Enqueue("Error saving world! See log or console.");
			Terminal.m_threadSafeConsoleLog.Enqueue("Error saving world! " + ex.Message);
		}
		static string GetBackupPath(string filePath, DateTime dateTime)
		{
			FileHelpers.SplitFilePath(filePath, out var directory, out var fileName, out var fileExtension);
			return directory + fileName + "_backup_cloud-" + dateTime.ToString("yyyyMMdd-HHmmss") + fileExtension;
		}
	}

	public static bool ConsiderAutoBackup(string saveName, SaveDataType dataType, DateTime now)
	{
		int num = 1200;
		int num2 = ((m_backupCount != 1) ? m_backupCount : 0);
		string value;
		int result;
		string value2;
		int result2;
		string value3;
		int result3;
		if (num2 > 0)
		{
			return SaveSystem.ConsiderBackup(saveName, dataType, now, num2, (Terminal.m_testList.TryGetValue("autoshort", out value) && int.TryParse(value, out result)) ? result : m_backupShort, (Terminal.m_testList.TryGetValue("autolong", out value2) && int.TryParse(value2, out result2)) ? result2 : m_backupLong, (Terminal.m_testList.TryGetValue("autowait", out value3) && int.TryParse(value3, out result3)) ? result3 : num, ZoneSystem.instance ? ZoneSystem.instance.TimeSinceStart() : 0f);
		}
		return false;
	}

	private void LoadWorld()
	{
		ZLog.Log("Load world: " + m_world.m_name + " (" + m_world.m_fileName + ")");
		string dBPath = m_world.GetDBPath();
		FileReader fileReader;
		try
		{
			fileReader = new FileReader(dBPath, m_world.m_fileSource);
		}
		catch
		{
			ZLog.Log("  missing " + dBPath);
			WorldSetup();
			return;
		}
		BinaryReader binary = fileReader.m_binary;
		try
		{
			if (!CheckDataVersion(binary, out var version))
			{
				ZLog.Log("  incompatible data version " + version);
				m_loadError = true;
				binary.Close();
				fileReader.Dispose();
				WorldSetup();
				return;
			}
			if (version >= 4)
			{
				m_netTime = binary.ReadDouble();
			}
			m_zdoMan.Load(binary, version);
			if (version >= 12)
			{
				ZoneSystem.instance.Load(binary, version);
			}
			if (version >= 15)
			{
				RandEventSystem.instance.Load(binary, version);
			}
			fileReader.Dispose();
			WorldSetup();
		}
		catch (Exception ex)
		{
			ZLog.LogError("Exception while loading world " + dBPath + ":" + ex.ToString());
			m_loadError = true;
		}
		Game.instance.CollectResources();
	}

	private bool CheckDataVersion(BinaryReader reader, out int version)
	{
		version = reader.ReadInt32();
		if (!Version.IsWorldVersionCompatible(version))
		{
			return false;
		}
		return true;
	}

	private void WorldSetup()
	{
		ZoneSystem.instance.SetStartingGlobalKeys();
		m_world.m_startingKeysChanged = false;
	}

	public int GetHostPort()
	{
		if (m_hostSocket != null)
		{
			return m_hostSocket.GetHostPort();
		}
		return 0;
	}

	public static long GetUID()
	{
		return ZDOMan.GetSessionID();
	}

	public long GetWorldUID()
	{
		return m_world.m_uid;
	}

	public string GetWorldName()
	{
		if (m_world != null)
		{
			return m_world.m_name;
		}
		return null;
	}

	public void SetCharacterID(ZDOID id)
	{
		m_characterID = id;
		if (!m_isServer)
		{
			m_peers[0].m_rpc.Invoke("CharacterID", id);
		}
	}

	private void RPC_CharacterID(ZRpc rpc, ZDOID characterID)
	{
		ZNetPeer peer = GetPeer(rpc);
		if (peer != null)
		{
			peer.m_characterID = characterID;
			string playerName = peer.m_playerName;
			ZDOID zDOID = characterID;
			ZLog.Log("Got character ZDOID from " + playerName + " : " + zDOID.ToString());
		}
	}

	public void SetPublicReferencePosition(bool pub)
	{
		m_publicReferencePosition = pub;
	}

	public bool IsReferencePositionPublic()
	{
		return m_publicReferencePosition;
	}

	public void SetReferencePosition(Vector3 pos)
	{
		m_referencePosition = pos;
	}

	public Vector3 GetReferencePosition()
	{
		return m_referencePosition;
	}

	public List<ZDO> GetAllCharacterZDOS()
	{
		List<ZDO> list = new List<ZDO>();
		ZDO zDO = m_zdoMan.GetZDO(m_characterID);
		if (zDO != null)
		{
			list.Add(zDO);
		}
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady() && !peer.m_characterID.IsNone())
			{
				ZDO zDO2 = m_zdoMan.GetZDO(peer.m_characterID);
				if (zDO2 != null)
				{
					list.Add(zDO2);
				}
			}
		}
		return list;
	}

	public int GetPeerConnections()
	{
		int num = 0;
		for (int i = 0; i < m_peers.Count; i++)
		{
			if (m_peers[i].IsReady())
			{
				num++;
			}
		}
		return num;
	}

	public ZNat GetZNat()
	{
		return m_nat;
	}

	public static void SetServer(bool server, bool openServer, bool publicServer, string serverName, string password, World world)
	{
		m_isServer = server;
		m_openServer = openServer;
		m_publicServer = publicServer;
		m_serverPassword = (string.IsNullOrEmpty(password) ? "" : HashPassword(password, ServerPasswordSalt()));
		m_ServerName = serverName;
		m_world = world;
	}

	private static string HashPassword(string password, string salt)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(password + salt);
		byte[] bytes2 = new MD5CryptoServiceProvider().ComputeHash(bytes);
		return Encoding.ASCII.GetString(bytes2);
	}

	public static void ResetServerHost()
	{
		m_serverPlayFabPlayerId = null;
		m_serverSteamID = 0uL;
		m_serverHost = "";
		m_serverHostPort = 0;
	}

	public static bool HasServerHost()
	{
		if (!(m_serverHost != "") && m_serverPlayFabPlayerId == null)
		{
			return m_serverSteamID != 0;
		}
		return true;
	}

	public static void SetServerHost(string remotePlayerId)
	{
		ResetServerHost();
		m_serverPlayFabPlayerId = remotePlayerId;
		m_onlineBackend = OnlineBackendType.PlayFab;
	}

	public static void SetServerHost(ulong serverID)
	{
		ResetServerHost();
		m_serverSteamID = serverID;
		m_onlineBackend = OnlineBackendType.Steamworks;
	}

	public static void SetServerHost(string host, int port, OnlineBackendType backend)
	{
		ResetServerHost();
		m_serverHost = host;
		m_serverHostPort = port;
		m_onlineBackend = backend;
	}

	public static string GetServerString(bool includeBackend = true)
	{
		if (m_onlineBackend == OnlineBackendType.PlayFab)
		{
			return (includeBackend ? "playfab/" : "") + m_serverPlayFabPlayerId;
		}
		if (m_onlineBackend == OnlineBackendType.Steamworks)
		{
			return (includeBackend ? "steam/" : "") + m_serverSteamID + "/" + m_serverHost + ":" + m_serverHostPort;
		}
		return (includeBackend ? "socket/" : "") + m_serverHost + ":" + m_serverHostPort;
	}

	public bool IsServer()
	{
		return m_isServer;
	}

	public static bool IsOpenServer()
	{
		return m_openServer;
	}

	public bool IsDedicated()
	{
		return false;
	}

	public bool IsCurrentServerDedicated()
	{
		List<ZNetPeer> peers = GetPeers();
		bool result = false;
		for (int i = 0; i < peers.Count; i++)
		{
			if (peers[i].m_characterID.IsNone())
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public static bool IsPasswordDialogShowing()
	{
		if (m_instance == null)
		{
			return false;
		}
		return m_instance.m_passwordDialog.gameObject.activeInHierarchy;
	}

	public static bool TryGetServerAssignedDisplayName(PlatformUserID userId, out string displayName)
	{
		if (instance == null)
		{
			displayName = null;
			return false;
		}
		for (int i = 0; i < instance.m_players.Count; i++)
		{
			if (instance.m_players[i].m_userInfo.m_id == userId && instance.m_players[i].m_serverAssignedDisplayName != null)
			{
				displayName = instance.m_players[i].m_serverAssignedDisplayName;
				return true;
			}
		}
		displayName = null;
		return false;
	}

	private string GetUniqueDisplayName(CrossNetworkUserInfo userInfo)
	{
		bool flag = false;
		int num = 1;
		int num2 = 0;
		for (int i = 0; i < m_playerHistory.Count; i++)
		{
			if (m_playerHistory[i].m_displayName != userInfo.m_displayName)
			{
				continue;
			}
			num2++;
			if (!flag)
			{
				if (m_playerHistory[i].m_id == userInfo.m_id)
				{
					flag = true;
				}
				else
				{
					num++;
				}
			}
		}
		if (!flag)
		{
			ZLog.LogError($"Couldn't find matching ID to user {userInfo} in player history!");
		}
		if (num2 > 1)
		{
			return $"{userInfo.m_displayName}#{num}";
		}
		return userInfo.m_displayName;
	}

	private void UpdatePlayerList()
	{
		m_players.Clear();
		if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
		{
			PlayerInfo item = new PlayerInfo
			{
				m_name = Game.instance.GetPlayerProfile().GetName(),
				m_userInfo = 
				{
					m_id = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID,
					m_displayName = PlatformManager.DistributionPlatform.LocalUser.DisplayName
				},
				m_characterID = m_characterID,
				m_publicPosition = m_publicReferencePosition
			};
			if (item.m_publicPosition)
			{
				item.m_position = m_referencePosition;
			}
			m_players.Add(item);
		}
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady())
			{
				PlayerInfo item2 = new PlayerInfo
				{
					m_name = peer.m_playerName,
					m_characterID = peer.m_characterID
				};
				if (m_onlineBackend == OnlineBackendType.Steamworks)
				{
					item2.m_userInfo.m_id = new PlatformUserID(m_steamPlatform, peer.m_socket.GetHostName());
				}
				else
				{
					item2.m_userInfo.m_id = new PlatformUserID(peer.m_socket.GetHostName());
				}
				item2.m_userInfo.m_displayName = (peer.m_serverSyncedPlayerData.ContainsKey("platformDisplayName") ? peer.m_serverSyncedPlayerData["platformDisplayName"] : "");
				item2.m_publicPosition = peer.m_publicRefPos;
				if (item2.m_publicPosition)
				{
					item2.m_position = peer.m_refPos;
				}
				m_players.Add(item2);
			}
		}
		UpdatePlayerHistory();
		for (int i = 0; i < m_players.Count; i++)
		{
			PlayerInfo value = m_players[i];
			value.m_serverAssignedDisplayName = GetUniqueDisplayName(value.m_userInfo);
			m_players[i] = value;
		}
	}

	private void SendPlayerList()
	{
		UpdatePlayerList();
		if (m_peers.Count <= 0)
		{
			return;
		}
		ZPackage zPackage = new ZPackage();
		zPackage.Write(m_players.Count);
		foreach (PlayerInfo player in m_players)
		{
			zPackage.Write(player.m_name);
			zPackage.Write(player.m_characterID);
			zPackage.Write(player.m_userInfo.m_id.ToString());
			zPackage.Write(player.m_userInfo.m_displayName);
			zPackage.Write(player.m_serverAssignedDisplayName);
			zPackage.Write(player.m_publicPosition);
			if (player.m_publicPosition)
			{
				zPackage.Write(player.m_position);
			}
		}
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady())
			{
				peer.m_rpc.Invoke("PlayerList", zPackage);
			}
		}
		UpdatePlayerHistory();
	}

	private void SendAdminList()
	{
		if (m_peers.Count <= 0)
		{
			return;
		}
		ZPackage zPackage = new ZPackage();
		zPackage.Write(m_adminList.Count());
		foreach (string item in m_adminList.GetList())
		{
			zPackage.Write(item);
		}
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady())
			{
				peer.m_rpc.Invoke("AdminList", zPackage);
			}
		}
	}

	private void RPC_AdminList(ZRpc rpc, ZPackage pkg)
	{
		m_adminListForRpc.Clear();
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			string item = pkg.ReadString();
			m_adminListForRpc.Add(item);
		}
	}

	private void RPC_PlayerList(ZRpc rpc, ZPackage pkg)
	{
		m_players.Clear();
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			PlayerInfo item = new PlayerInfo
			{
				m_name = pkg.ReadString(),
				m_characterID = pkg.ReadZDOID(),
				m_userInfo = 
				{
					m_id = new PlatformUserID(pkg.ReadString()),
					m_displayName = pkg.ReadString()
				},
				m_serverAssignedDisplayName = pkg.ReadString(),
				m_publicPosition = pkg.ReadBool()
			};
			if (item.m_publicPosition)
			{
				item.m_position = pkg.ReadVector3();
			}
			m_players.Add(item);
		}
		UpdatePlayerHistory();
	}

	private void UpdatePlayerHistory()
	{
		List<PlatformUserID> list = new List<PlatformUserID>();
		PlatformUserID platformUserID = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
		foreach (PlayerInfo player in m_players)
		{
			int i;
			for (i = 0; i < m_playerHistory.Count && !(m_playerHistory[i].m_id == player.m_userInfo.m_id); i++)
			{
			}
			if (i < m_playerHistory.Count)
			{
				m_playerHistory[i] = player.m_userInfo;
				continue;
			}
			m_playerHistory.Add(player.m_userInfo);
			if (!(player.m_userInfo.m_id == platformUserID))
			{
				list.Add(player.m_userInfo.m_id);
			}
		}
		IMatchmakingProvider matchmakingProvider = PlatformManager.DistributionPlatform.MatchmakingProvider;
		if (matchmakingProvider != null && list.Count > 0)
		{
			matchmakingProvider.AddRecentPlayers(list.ToArray());
		}
	}

	public List<PlayerInfo> GetPlayerList()
	{
		return m_players;
	}

	public static bool TryGetPlayerByPlatformUserID(PlatformUserID platformUserID, out PlayerInfo playerInfo)
	{
		if (instance == null)
		{
			playerInfo = default(PlayerInfo);
			return false;
		}
		for (int i = 0; i < instance.m_players.Count; i++)
		{
			if (instance.m_players[i].m_userInfo.m_id == platformUserID)
			{
				playerInfo = instance.m_players[i];
				return true;
			}
		}
		playerInfo = default(PlayerInfo);
		return false;
	}

	public List<string> GetAdminList()
	{
		return m_adminListForRpc;
	}

	public void GetOtherPublicPlayers(List<PlayerInfo> playerList)
	{
		foreach (PlayerInfo player in m_players)
		{
			if (player.m_publicPosition)
			{
				ZDOID characterID = player.m_characterID;
				if (!characterID.IsNone() && !(player.m_characterID == m_characterID))
				{
					playerList.Add(player);
				}
			}
		}
	}

	public int GetNrOfPlayers()
	{
		return m_players.Count;
	}

	public void GetNetStats(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		localQuality = 0f;
		remoteQuality = 0f;
		ping = 0;
		outByteSec = 0f;
		inByteSec = 0f;
		if (IsServer())
		{
			int num = 0;
			foreach (ZNetPeer peer in m_peers)
			{
				if (peer.IsReady())
				{
					num++;
					peer.m_socket.GetConnectionQuality(out var localQuality2, out var remoteQuality2, out var ping2, out var outByteSec2, out var inByteSec2);
					localQuality += localQuality2;
					remoteQuality += remoteQuality2;
					ping += ping2;
					outByteSec += outByteSec2;
					inByteSec += inByteSec2;
				}
			}
			if (num > 0)
			{
				localQuality /= num;
				remoteQuality /= num;
				ping /= num;
			}
		}
		else
		{
			if (m_connectionStatus != ConnectionStatus.Connected)
			{
				return;
			}
			foreach (ZNetPeer peer2 in m_peers)
			{
				if (peer2.IsReady())
				{
					peer2.m_socket.GetConnectionQuality(out localQuality, out remoteQuality, out ping, out outByteSec, out inByteSec);
					break;
				}
			}
		}
	}

	public void SetNetTime(double time)
	{
		m_netTime = time;
	}

	public DateTime GetTime()
	{
		long ticks = (long)(m_netTime * 1000.0 * 10000.0);
		return new DateTime(ticks);
	}

	public float GetWrappedDayTimeSeconds()
	{
		return (float)(m_netTime % 86400.0);
	}

	public double GetTimeSeconds()
	{
		return m_netTime;
	}

	public static ConnectionStatus GetConnectionStatus()
	{
		if (m_instance != null && m_instance.IsServer())
		{
			return ConnectionStatus.Connected;
		}
		if (m_externalError != ConnectionStatus.None)
		{
			m_connectionStatus = m_externalError;
		}
		return m_connectionStatus;
	}

	public bool HasBadConnection()
	{
		return GetServerPing() > m_badConnectionPing;
	}

	public float GetServerPing()
	{
		if (IsServer())
		{
			return 0f;
		}
		if (m_connectionStatus == ConnectionStatus.Connecting || m_connectionStatus == ConnectionStatus.None)
		{
			return 0f;
		}
		if (m_connectionStatus == ConnectionStatus.Connected)
		{
			foreach (ZNetPeer peer in m_peers)
			{
				if (peer.IsReady())
				{
					return peer.m_rpc.GetTimeSinceLastPing();
				}
			}
		}
		return 0f;
	}

	public ZNetPeer GetServerPeer()
	{
		if (IsServer())
		{
			return null;
		}
		if (m_connectionStatus == ConnectionStatus.Connecting || m_connectionStatus == ConnectionStatus.None)
		{
			return null;
		}
		if (m_connectionStatus == ConnectionStatus.Connected)
		{
			foreach (ZNetPeer peer in m_peers)
			{
				if (peer.IsReady())
				{
					return peer;
				}
			}
		}
		return null;
	}

	public ZRpc GetServerRPC()
	{
		return GetServerPeer()?.m_rpc;
	}

	public List<ZNetPeer> GetPeers()
	{
		return m_peers;
	}

	public void RemotePrint(ZRpc rpc, string text)
	{
		if (rpc == null)
		{
			if ((bool)Console.instance)
			{
				Console.instance.Print(text);
			}
		}
		else
		{
			rpc.Invoke("RemotePrint", text);
		}
	}

	private void RPC_RemotePrint(ZRpc rpc, string text)
	{
		if ((bool)Console.instance)
		{
			Console.instance.Print(text);
		}
	}

	public void Kick(string user)
	{
		if (IsServer())
		{
			InternalKick(user);
			return;
		}
		GetServerRPC()?.Invoke("Kick", user);
	}

	private void RPC_Kick(ZRpc rpc, string user)
	{
		if (!ListContainsId(m_adminList, rpc.GetSocket().GetHostName()))
		{
			RemotePrint(rpc, "You are not admin");
			return;
		}
		RemotePrint(rpc, "Kicking user " + user);
		InternalKick(user);
	}

	private void RPC_Kicked(ZRpc rpc)
	{
		ZNetPeer peer = GetPeer(rpc);
		if (peer != null && peer.m_server)
		{
			m_connectionStatus = ConnectionStatus.ErrorKicked;
			Disconnect(peer);
		}
	}

	private void InternalKick(string user)
	{
		if (user == "")
		{
			return;
		}
		ZNetPeer zNetPeer = null;
		if (!PlatformUserID.TryParse(user, out var platform))
		{
			platform = new PlatformUserID(m_steamPlatform, user);
		}
		if (m_onlineBackend == OnlineBackendType.Steamworks)
		{
			if (platform.m_platform == m_steamPlatform)
			{
				zNetPeer = GetPeerByHostName(platform.m_userID);
			}
		}
		else
		{
			zNetPeer = GetPeerByHostName(platform.ToString());
		}
		if (zNetPeer == null)
		{
			zNetPeer = GetPeerByPlayerName(user);
		}
		if (zNetPeer != null)
		{
			InternalKick(zNetPeer);
		}
	}

	private void InternalKick(ZNetPeer peer)
	{
		if (IsServer() && peer != null && !PeersToDisconnectAfterKick.ContainsKey(peer))
		{
			ZLog.Log("Kicking " + peer.m_playerName);
			peer.m_rpc.Invoke("Kicked");
			PeersToDisconnectAfterKick[peer] = Time.time + 1f;
		}
	}

	private bool IsAllowed(string hostName, string playerName)
	{
		if (ListContainsId(m_bannedList, hostName) || m_bannedList.Contains(playerName))
		{
			return false;
		}
		if (m_permittedList.Count() > 0 && !ListContainsId(m_permittedList, hostName))
		{
			return false;
		}
		return true;
	}

	public void Ban(string user)
	{
		if (IsServer())
		{
			InternalBan(null, user);
			return;
		}
		GetServerRPC()?.Invoke("Ban", user);
	}

	private void RPC_Ban(ZRpc rpc, string user)
	{
		if (!ListContainsId(m_adminList, rpc.GetSocket().GetHostName()))
		{
			RemotePrint(rpc, "You are not admin");
		}
		else
		{
			InternalBan(rpc, user);
		}
	}

	private void InternalBan(ZRpc rpc, string user)
	{
		if (IsServer() && !(user == ""))
		{
			ZNetPeer peerByPlayerName = GetPeerByPlayerName(user);
			if (peerByPlayerName != null)
			{
				user = peerByPlayerName.m_socket.GetHostName();
			}
			RemotePrint(rpc, "Banning user " + user);
			m_bannedList.Add(user);
		}
	}

	public void Unban(string user)
	{
		if (IsServer())
		{
			InternalUnban(null, user);
			return;
		}
		GetServerRPC()?.Invoke("Unban", user);
	}

	private void RPC_Unban(ZRpc rpc, string user)
	{
		if (!ListContainsId(m_adminList, rpc.GetSocket().GetHostName()))
		{
			RemotePrint(rpc, "You are not admin");
		}
		else
		{
			InternalUnban(rpc, user);
		}
	}

	private void InternalUnban(ZRpc rpc, string user)
	{
		if (IsServer() && !(user == ""))
		{
			RemotePrint(rpc, "Unbanning user " + user);
			m_bannedList.Remove(user);
		}
	}

	public bool IsAdmin(string hostName)
	{
		return ListContainsId(m_adminList, hostName);
	}

	public void PrintBanned()
	{
		if (IsServer())
		{
			InternalPrintBanned(null);
		}
		else
		{
			GetServerRPC()?.Invoke("PrintBanned");
		}
	}

	private void RPC_PrintBanned(ZRpc rpc)
	{
		if (!ListContainsId(m_adminList, rpc.GetSocket().GetHostName()))
		{
			RemotePrint(rpc, "You are not admin");
		}
		else
		{
			InternalPrintBanned(rpc);
		}
	}

	private void InternalPrintBanned(ZRpc rpc)
	{
		RemotePrint(rpc, "Banned users");
		List<string> list = m_bannedList.GetList();
		if (list.Count == 0)
		{
			RemotePrint(rpc, "-");
		}
		else
		{
			for (int i = 0; i < list.Count; i++)
			{
				RemotePrint(rpc, i + ": " + list[i]);
			}
		}
		RemotePrint(rpc, "");
		RemotePrint(rpc, "Permitted users");
		List<string> list2 = m_permittedList.GetList();
		if (list2.Count == 0)
		{
			RemotePrint(rpc, "All");
			return;
		}
		for (int j = 0; j < list2.Count; j++)
		{
			RemotePrint(rpc, j + ": " + list2[j]);
		}
	}

	public void RemoteCommand(string command)
	{
		if (IsServer())
		{
			InternalCommand(null, command);
			return;
		}
		GetServerRPC()?.Invoke("RPC_RemoteCommand", command);
	}

	private void RPC_RemoteCommand(ZRpc rpc, string command)
	{
		if (!ListContainsId(m_adminList, rpc.GetSocket().GetHostName()))
		{
			RemotePrint(rpc, "You are not admin");
		}
		else
		{
			InternalCommand(rpc, command);
		}
	}

	private void InternalCommand(ZRpc rpc, string command)
	{
		ZLog.Log("Remote admin '" + rpc.GetSocket().GetHostName() + "' executed command '" + command + "' remotely.");
		Console.instance.TryRunCommand(command);
	}

	private static string ServerPasswordSalt()
	{
		if (m_serverPasswordSalt.Length == 0)
		{
			byte[] array = new byte[16];
			RandomNumberGenerator.Create().GetBytes(array);
			m_serverPasswordSalt = Encoding.ASCII.GetString(array);
		}
		return m_serverPasswordSalt;
	}

	public static void SetExternalError(ConnectionStatus error)
	{
		m_externalError = error;
	}
}
