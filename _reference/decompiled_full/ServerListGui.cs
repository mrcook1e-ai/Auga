using System;
using System.Collections.Generic;
using GUIFramework;
using NetworkingUtils;
using Splatform;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ServerListGui : MonoBehaviour
{
	private static ServerListGui s_instance;

	private const int c_MaxRecentServers = 11;

	private const string c_FavoriteListFileName = "favorite";

	private const string c_RecentListFileName = "recent";

	private const float c_TabAreaWidth = 515f;

	private const float c_TabSpacing = 5f;

	private List<IServerList> m_serverLists = new List<IServerList>();

	private LocalServerList m_favoriteServersList;

	private LocalServerList m_recentServersList;

	private List<ServerListEntryData> m_filteredList = new List<ServerListEntryData>();

	private int m_currentServerList = int.MaxValue;

	private bool m_isAwaitingServerAdd;

	private bool m_buttonsOutdated = true;

	private bool m_initialized;

	private bool m_filteredListOutdated;

	private bool m_updateServerListGui;

	private bool m_centerSelection;

	[SerializeField]
	private Button m_favoriteButton;

	[SerializeField]
	private Button m_removeButton;

	[SerializeField]
	private Button m_upButton;

	[SerializeField]
	private Button m_downButton;

	[SerializeField]
	private GameObject m_serverListTab;

	[SerializeField]
	private ConnectIcons m_connectIcons;

	[SerializeField]
	private FejdStartup m_startup;

	private UIGamePad m_uiGamePad;

	[Header("Join")]
	public float m_serverListElementStep = 32f;

	public RectTransform m_serverListRoot;

	public GameObject m_serverListElement;

	public ScrollRectEnsureVisible m_serverListEnsureVisible;

	public Button m_serverRefreshButton;

	public TextMeshProUGUI m_serverCount;

	public GuiInputField m_filterInputField;

	public RectTransform m_tooltipAnchor;

	public Button m_addServerButton;

	public GameObject m_addServerPanel;

	public Button m_addServerConfirmButton;

	public Button m_addServerCancelButton;

	public GuiInputField m_addServerTextInput;

	public TabHandler m_serverListTabHandler;

	public Button m_joinGameButton;

	private float m_serverListBaseSize;

	private List<GameObject> m_serverListTabs = new List<GameObject>();

	private List<ServerListElement> m_serverListElements = new List<ServerListElement>();

	private Dictionary<ServerJoinData, ServerListElement> m_tempJoinDataToElementMap = new Dictionary<ServerJoinData, ServerListElement>(200);

	private Stack<ServerListElement> m_serverListElementPool = new Stack<ServerListElement>();

	private List<ServerListEntryData> CurrentServerListFiltered
	{
		get
		{
			if (m_filteredListOutdated)
			{
				FilterList();
			}
			return m_filteredList;
		}
	}

	public static string GetServerListFolder(FileHelpers.FileSource fileSource)
	{
		if (fileSource != FileHelpers.FileSource.Local)
		{
			return "/serverlist/";
		}
		return "/serverlist_local/";
	}

	public static string GetServerListFolderPath(FileHelpers.FileSource fileSource)
	{
		return Utils.GetSaveDataPath(fileSource) + GetServerListFolder(fileSource);
	}

	public static FileHelpers.FileLocation[] GetServerListLocations(string serverListFileName)
	{
		List<FileHelpers.FileLocation> list = new List<FileHelpers.FileLocation>();
		if (FileHelpers.LocalStorageSupported)
		{
			list.Add(new FileHelpers.FileLocation(FileHelpers.FileSource.Local, GetServerListFolderPath(FileHelpers.FileSource.Local) + serverListFileName));
		}
		if (FileHelpers.CloudStorageEnabled)
		{
			list.Add(new FileHelpers.FileLocation(FileHelpers.FileSource.Cloud, GetServerListFolderPath(FileHelpers.FileSource.Cloud) + serverListFileName));
		}
		return list.ToArray();
	}

	private void Awake()
	{
		Initialize();
	}

	private void OnEnable()
	{
		if (s_instance != null && s_instance != this)
		{
			ZLog.LogError("More than one instance of ServerList!");
			return;
		}
		s_instance = this;
		for (int i = 0; i < m_serverLists.Count; i++)
		{
			m_serverLists[i].ServerListUpdated += OnCurrentServerListUpdated;
		}
		RecreateTabs();
		Update();
	}

	private void OnApplicationQuit()
	{
		SaveLocalServerLists();
	}

	private void OnDisable()
	{
		for (int i = 0; i < m_serverLists.Count; i++)
		{
			m_serverLists[i].ServerListUpdated -= OnCurrentServerListUpdated;
		}
		SaveLocalServerLists();
	}

	private void OnDestroy()
	{
		if (s_instance != this)
		{
			ZLog.LogError("ServerList instance was not this!");
			return;
		}
		m_favoriteServersList.Dispose();
		m_recentServersList.Dispose();
		s_instance = null;
	}

	private void Update()
	{
		m_serverLists[m_currentServerList].Tick();
		UpdateInput();
		if (m_updateServerListGui)
		{
			UpdateServerListGuiInternal(m_centerSelection);
			m_updateServerListGui = false;
			m_centerSelection = false;
		}
		UpdateButtons();
	}

	private void SaveLocalServerLists(bool force = false)
	{
		SaveStatusCode saveStatusCode = m_favoriteServersList.Save(force);
		ZLog.Log($"Saved favorite servers list with result {saveStatusCode}");
		saveStatusCode = m_recentServersList.Save(force);
		ZLog.Log($"Saved recent servers list with result {saveStatusCode}");
	}

	private void UpdateInput()
	{
		if (!m_uiGamePad.IsBlocked())
		{
			UpdateGamepad();
			UpdateKeyboard();
		}
	}

	private void UpdateAddServerButtons()
	{
		if (m_addServerPanel.activeInHierarchy)
		{
			m_addServerConfirmButton.interactable = m_addServerTextInput.text.Length > 0 && !m_isAwaitingServerAdd;
			m_addServerCancelButton.interactable = !m_isAwaitingServerAdd;
		}
	}

	private void OnCurrentServerListUpdated()
	{
		m_filteredListOutdated = true;
		UpdateServerListGui(centerSelection: false);
		UpdateServerCount();
		bool flag = false;
		for (int i = 0; i < CurrentServerListFiltered.Count; i++)
		{
			if (CurrentServerListFiltered[i].m_joinData == m_startup.GetServerToJoin())
			{
				flag = true;
				break;
			}
		}
		if (m_startup.HasServerToJoin() && !flag)
		{
			ZLog.Log("Serverlist does not contain selected server, clearing");
			if (CurrentServerListFiltered.Count > 0)
			{
				SetSelectedServer(0, centerSelection: true);
			}
			else
			{
				ClearSelectedServer();
			}
		}
	}

	private void Initialize()
	{
		if (m_initialized)
		{
			ZLog.LogError("Already initialized!");
			return;
		}
		m_initialized = true;
		m_favoriteButton.onClick.AddListener(delegate
		{
			OnFavoriteServerButton();
		});
		m_removeButton.onClick.AddListener(delegate
		{
			OnRemoveServerButton();
		});
		m_upButton.onClick.AddListener(delegate
		{
			OnMoveServerUpButton();
		});
		m_downButton.onClick.AddListener(delegate
		{
			OnMoveServerDownButton();
		});
		m_filterInputField.onValueChanged.AddListener(delegate
		{
			OnServerFilterChanged(isTyping: true);
		});
		m_addServerButton.gameObject.SetActive(value: true);
		if (PlatformPrefs.HasKey("LastIPJoined"))
		{
			PlatformPrefs.DeleteKey("LastIPJoined");
		}
		m_serverListBaseSize = m_serverListRoot.rect.height;
		m_serverLists = new List<IServerList>();
		m_favoriteServersList = new LocalServerList("$menu_favorite", GetServerListLocations("favorite"));
		m_serverLists.Add(m_favoriteServersList);
		m_recentServersList = new LocalServerList("$menu_recent", GetServerListLocations("recent"));
		m_serverLists.Add(m_recentServersList);
		m_serverLists.Add(new FriendsServerList("$menu_friends"));
		m_serverLists.Add(new CommunityServerList("$menu_community"));
		m_uiGamePad = GetComponent<UIGamePad>();
		if (m_uiGamePad == null)
		{
			ZLog.LogError("UI Gamepad component was null!");
		}
	}

	private void RecreateTabs()
	{
		for (int i = 0; i < m_serverListTabs.Count; i++)
		{
			UnityEngine.Object.Destroy(m_serverListTabs[i]);
			m_serverListTabs[i] = null;
		}
		m_serverListTabs.Clear();
		m_serverListTabHandler.m_tabs.Clear();
		float x = (515f - 5f * (float)(m_serverLists.Count - 1)) / (float)m_serverLists.Count;
		for (int j = 0; j < m_serverLists.Count; j++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(m_serverListTab, base.transform);
			m_serverListTabs.Add(gameObject);
			gameObject.transform.SetSiblingIndex(j);
			gameObject.SetActive(value: true);
			SetHint(gameObject, j);
			TMP_Text componentInChildren = gameObject.GetComponentInChildren<TMP_Text>();
			if ((object)componentInChildren == null)
			{
				ZLog.LogError("Couldn't find server list tab text component!");
			}
			else
			{
				componentInChildren.text = Localization.instance.Localize(m_serverLists[j].DisplayName);
			}
			RectTransform component = gameObject.GetComponent<RectTransform>();
			if ((object)component == null)
			{
				ZLog.LogError("Couldn't find server list tab rect transform!");
			}
			else
			{
				Vector2 sizeDelta = component.sizeDelta;
				sizeDelta.x = x;
				component.sizeDelta = sizeDelta;
				Vector3 localPosition = component.localPosition;
				localPosition.x += (float)j * (component.rect.width + 5f);
				component.localPosition = localPosition;
			}
			UnityEvent unityEvent = new UnityEvent();
			unityEvent.AddListener(OnTab);
			TabHandler.Tab item = new TabHandler.Tab
			{
				m_button = gameObject.GetComponent<Button>(),
				m_page = null,
				m_default = (j == 0),
				m_onClick = unityEvent
			};
			m_serverListTabHandler.m_tabs.Add(item);
		}
		if (PlatformPrefs.HasKey("publicfilter"))
		{
			PlatformPrefs.DeleteKey("publicfilter");
		}
		int index = PlatformPrefs.GetInt("serverListTab");
		m_serverListTabHandler.SetActiveTab(index, forceSelect: true);
		m_serverListTabHandler.Init(forceSelect: true);
	}

	private void SetHint(GameObject tabObject, int index)
	{
		UIGamePad component = tabObject.GetComponent<UIGamePad>();
		component.m_hint = null;
		if (m_serverLists.Count <= 1)
		{
			return;
		}
		bool flag = index == 0;
		bool flag2 = index == m_serverLists.Count - 1;
		if (flag || flag2)
		{
			Transform transform = tabObject.transform.Find(flag ? "gamepad_hint_left" : "gamepad_hint_right");
			if ((object)transform == null)
			{
				ZLog.LogError("Couldn't find server list tab hint object!");
			}
			else
			{
				component.m_hint = transform.gameObject;
			}
		}
	}

	public void FilterList()
	{
		m_serverLists[m_currentServerList].GetFilteredList(m_filteredList);
		m_filteredListOutdated = false;
	}

	private void UpdateButtons()
	{
		UpdateServerRefreshInteractability();
		UpdateAddServerButtons();
		if (m_buttonsOutdated)
		{
			m_buttonsOutdated = false;
			int selectedServer = GetSelectedServer();
			bool flag = selectedServer >= 0;
			bool flag2 = flag && m_favoriteServersList.Contains(CurrentServerListFiltered[selectedServer].m_joinData);
			if (m_serverLists[m_currentServerList] == m_favoriteServersList)
			{
				m_upButton.interactable = flag && selectedServer != 0;
				m_downButton.interactable = flag && selectedServer != CurrentServerListFiltered.Count - 1;
				m_removeButton.interactable = flag;
				m_favoriteButton.interactable = flag && (m_removeButton == null || !m_removeButton.gameObject.activeSelf);
			}
			else if (m_serverLists[m_currentServerList] == m_recentServersList)
			{
				m_favoriteButton.interactable = flag && !flag2;
				m_removeButton.interactable = flag;
			}
			else
			{
				m_favoriteButton.interactable = flag && !flag2;
			}
			m_joinGameButton.interactable = flag;
		}
	}

	private void UpdateServerRefreshInteractability()
	{
		bool flag = true;
		flag &= (DateTime.UtcNow - m_serverLists[m_currentServerList].LastRefreshTimeUtc).TotalSeconds > 1.0;
		flag &= m_serverLists[m_currentServerList].CanRefresh;
		m_serverRefreshButton.interactable = flag;
	}

	private void SetServerFilter(string filter)
	{
		m_filterInputField.text = filter;
		OnServerFilterChanged();
	}

	private void OnTab()
	{
		int activeTab = m_serverListTabHandler.GetActiveTab();
		if (m_currentServerList != activeTab)
		{
			m_filteredListOutdated = true;
			if (m_currentServerList >= 0 && m_currentServerList < m_serverLists.Count)
			{
				m_serverLists[m_currentServerList].OnClose();
			}
			m_currentServerList = activeTab;
			m_serverLists[m_currentServerList].OnOpen();
			SetServerFilter("");
			PlatformPrefs.SetInt("serverListTab", m_serverListTabHandler.GetActiveTab());
			UpdateServerListGui(centerSelection: true);
			UpdateServerCount();
			UpdateLocalServerListSelection();
			m_serverLists[m_currentServerList].Tick();
			ResetListManipulationButtons();
			if (m_serverLists[m_currentServerList] == m_favoriteServersList)
			{
				m_removeButton.gameObject.SetActive(value: true);
			}
			else
			{
				m_favoriteButton.gameObject.SetActive(value: true);
			}
		}
	}

	public void OnFavoriteServerButton()
	{
		if ((m_removeButton == null || !m_removeButton.gameObject.activeSelf) && m_serverLists[m_currentServerList] == m_favoriteServersList)
		{
			OnRemoveServerButton();
			return;
		}
		int selectedServer = GetSelectedServer();
		ServerListEntryData serverListEntryData = CurrentServerListFiltered[selectedServer];
		MultiBackendMatchmaking.SetServerName(serverListEntryData.m_joinData, new ServerNameAtTimePoint(serverListEntryData.m_serverName, serverListEntryData.m_timeStampUtc));
		m_favoriteServersList.Add(serverListEntryData.m_joinData);
		SetButtonsOutdated();
	}

	public void OnRemoveServerButton()
	{
		int selectedServer = GetSelectedServer();
		UnifiedPopup.Push(new YesNoPopup("$menu_removeserver", CensorShittyWords.FilterUGC(CurrentServerListFiltered[selectedServer].m_serverName, UGCType.ServerName, default(PlatformUserID), 0L), delegate
		{
			OnRemoveServerConfirm();
		}, delegate
		{
			UnifiedPopup.Pop();
		}));
	}

	public void OnMoveServerUpButton()
	{
		int selectedServer = GetSelectedServer();
		m_favoriteServersList.Swap(selectedServer, selectedServer - 1);
		m_filteredListOutdated = true;
		UpdateServerListGui(centerSelection: true);
	}

	public void OnMoveServerDownButton()
	{
		int selectedServer = GetSelectedServer();
		m_favoriteServersList.Swap(selectedServer, selectedServer + 1);
		m_filteredListOutdated = true;
		UpdateServerListGui(centerSelection: true);
	}

	private void OnRemoveServerConfirm()
	{
		if (m_serverLists[m_currentServerList] != m_favoriteServersList)
		{
			ZLog.LogError("Can't remove server from invalid list!");
			return;
		}
		int selectedServer = GetSelectedServer();
		ServerJoinData joinData = CurrentServerListFiltered[selectedServer].m_joinData;
		if (!m_favoriteServersList.TryGetIndexOf(joinData, out var index))
		{
			ZLog.LogError("Selected server was not in the favorites list!");
			return;
		}
		m_favoriteServersList.Remove(m_favoriteServersList[index]);
		m_filteredListOutdated = true;
		if (CurrentServerListFiltered.Count <= 0 && m_filterInputField.text != "")
		{
			m_filterInputField.text = "";
			OnServerFilterChanged();
			m_startup.SetServerToJoin(ServerJoinData.None);
		}
		else
		{
			UpdateLocalServerListSelection();
			SetSelectedServer(selectedServer, centerSelection: true);
		}
		UnifiedPopup.Pop();
	}

	private void ResetListManipulationButtons()
	{
		m_favoriteButton.gameObject.SetActive(value: false);
		m_removeButton.gameObject.SetActive(value: false);
		m_favoriteButton.interactable = false;
		m_upButton.interactable = false;
		m_downButton.interactable = false;
		m_removeButton.interactable = false;
	}

	private void SetButtonsOutdated()
	{
		m_buttonsOutdated = true;
	}

	private void UpdateServerListGui(bool centerSelection)
	{
		m_updateServerListGui = true;
		m_centerSelection |= centerSelection;
	}

	private void UpdateServerListGuiInternal(bool centerSelection)
	{
		for (int i = 0; i < m_serverListElements.Count; i++)
		{
			if (m_tempJoinDataToElementMap.TryGetValue(m_serverListElements[i].Server, out var _))
			{
				ZLog.LogWarning("Join data " + m_serverListElements[i].Server.ToString() + " already has a server list element, even though duplicates are not allowed! Discarding this element.\nWhile this warning itself is fine, it might be an indication of a bug that may cause navigation issues in the server list.");
				UnityEngine.Object.Destroy(m_serverListElements[i].m_element);
			}
			else
			{
				m_tempJoinDataToElementMap.Add(m_serverListElements[i].Server, m_serverListElements[i]);
			}
		}
		m_serverListElements.Clear();
		float num = 0f;
		for (int j = 0; j < CurrentServerListFiltered.Count; j++)
		{
			ServerListEntryData serverEntry = CurrentServerListFiltered[j];
			ServerListElement serverListElement;
			if (m_tempJoinDataToElementMap.ContainsKey(serverEntry.m_joinData))
			{
				serverListElement = m_tempJoinDataToElementMap[serverEntry.m_joinData];
				m_serverListElements.Add(serverListElement);
				m_tempJoinDataToElementMap.Remove(serverEntry.m_joinData);
			}
			else if (m_serverListElementPool.Count > 0)
			{
				serverListElement = m_serverListElementPool.Pop();
				serverListElement.m_element.SetActive(value: true);
				m_serverListElements.Add(serverListElement);
				serverListElement.m_button.onClick.AddListener(delegate
				{
					OnSelectedServer(serverEntry.m_joinData);
				});
			}
			else
			{
				GameObject obj = UnityEngine.Object.Instantiate(m_serverListElement, m_serverListRoot);
				obj.SetActive(value: true);
				serverListElement = new ServerListElement(obj);
				serverListElement.m_button.onClick.AddListener(delegate
				{
					OnSelectedServer(serverEntry.m_joinData);
				});
				m_serverListElements.Add(serverListElement);
			}
			serverListElement.m_rectTransform.anchoredPosition = new Vector2(0f, 0f - num);
			num += serverListElement.m_rectTransform.sizeDelta.y;
			bool flag = m_startup.HasServerToJoin() && m_startup.GetServerToJoin().Equals(serverEntry.m_joinData);
			if (centerSelection && flag)
			{
				m_serverListEnsureVisible.CenterOnItem(serverListElement.m_selected);
			}
			serverListElement.UpdateDisplayData(ref serverEntry, flag, m_tooltipAnchor, ref m_connectIcons);
		}
		foreach (ServerListElement value2 in m_tempJoinDataToElementMap.Values)
		{
			value2.m_element.SetActive(value: false);
			m_serverListElementPool.Push(value2);
			value2.m_button.onClick.RemoveAllListeners();
		}
		m_tempJoinDataToElementMap.Clear();
		m_serverListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(num, m_serverListBaseSize));
		SetButtonsOutdated();
	}

	private void UpdateServerCount()
	{
		uint totalServers = m_serverLists[m_currentServerList].TotalServers;
		uint num = 0u;
		for (int i = 0; i < CurrentServerListFiltered.Count; i++)
		{
			if (CurrentServerListFiltered[i].IsOnline)
			{
				num++;
			}
		}
		m_serverCount.text = $"{num} / {totalServers}";
	}

	private void OnSelectedServer(ServerJoinData selected)
	{
		m_startup.SetServerToJoin(selected);
		UpdateServerListGui(centerSelection: false);
	}

	private void SetSelectedServer(int index, bool centerSelection)
	{
		if (CurrentServerListFiltered.Count == 0)
		{
			if (m_startup.HasServerToJoin())
			{
				ZLog.Log("Serverlist is empty, clearing selection");
			}
			ClearSelectedServer();
		}
		else
		{
			index = Mathf.Clamp(index, 0, CurrentServerListFiltered.Count - 1);
			m_startup.SetServerToJoin(CurrentServerListFiltered[index].m_joinData);
			UpdateServerListGui(centerSelection);
		}
	}

	private int GetSelectedServer()
	{
		if (!m_startup.HasServerToJoin())
		{
			return -1;
		}
		for (int i = 0; i < CurrentServerListFiltered.Count; i++)
		{
			if (m_startup.GetServerToJoin() == CurrentServerListFiltered[i].m_joinData)
			{
				return i;
			}
		}
		return -1;
	}

	private void ClearSelectedServer()
	{
		m_startup.SetServerToJoin(ServerJoinData.None);
		SetButtonsOutdated();
	}

	private int FindSelectedServer(GameObject button)
	{
		for (int i = 0; i < m_serverListElements.Count; i++)
		{
			if (m_serverListElements[i].m_element == button)
			{
				return i;
			}
		}
		return -1;
	}

	private void UpdateLocalServerListSelection()
	{
		if (GetSelectedServer() < 0)
		{
			ClearSelectedServer();
			UpdateServerListGui(centerSelection: true);
		}
	}

	public void OnRefreshButton()
	{
		RequestServerList();
	}

	public static void Refresh()
	{
		if (!(s_instance == null))
		{
			s_instance.RequestServerList();
		}
	}

	public static void UpdateServerListGuiStatic()
	{
		if (!(s_instance == null))
		{
			s_instance.UpdateServerListGui(centerSelection: false);
		}
	}

	public void RequestServerList()
	{
		ZLog.DevLog("Request serverlist");
		if (!m_serverRefreshButton.interactable)
		{
			ZLog.DevLog("Server queue already running");
			return;
		}
		m_serverLists[m_currentServerList].Refresh();
		UpdateServerRefreshInteractability();
	}

	public void OnServerFilterChanged(bool isTyping = false)
	{
		m_serverLists[m_currentServerList].SetFilter(m_filterInputField.text, isTyping);
		m_filteredListOutdated = true;
		UpdateServerListGui(centerSelection: true);
		UpdateServerCount();
	}

	private void UpdateGamepad()
	{
		if (ZInput.IsGamepadActive())
		{
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				SetSelectedServer(GetSelectedServer() + 1, centerSelection: true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				SetSelectedServer(GetSelectedServer() - 1, centerSelection: true);
			}
		}
	}

	private void UpdateKeyboard()
	{
		if (ZInput.GetKeyDown(KeyCode.R) && !m_filterInputField.isFocused)
		{
			RequestServerList();
		}
		UpdateKeyboardSelection();
		UpdateKeyboardMoveServer();
	}

	private void UpdateKeyboardSelection()
	{
		if (ZInput.GetKeyDown(KeyCode.UpArrow))
		{
			SetSelectedServer(GetSelectedServer() - 1, centerSelection: true);
		}
		if (ZInput.GetKeyDown(KeyCode.DownArrow))
		{
			SetSelectedServer(GetSelectedServer() + 1, centerSelection: true);
		}
	}

	private void UpdateKeyboardMoveServer()
	{
		if (m_filterInputField.isFocused || m_serverLists[m_currentServerList] != m_favoriteServersList)
		{
			return;
		}
		int num = 0;
		num += (ZInput.GetKeyDown(KeyCode.W) ? (-1) : 0);
		num += (ZInput.GetKeyDown(KeyCode.S) ? 1 : 0);
		if (num != 0)
		{
			int selectedServer = GetSelectedServer();
			if (num > 0 && selectedServer + num < m_favoriteServersList.Count)
			{
				OnMoveServerDownButton();
			}
			else if (num < 0 && selectedServer + num >= 0)
			{
				OnMoveServerUpButton();
			}
		}
	}

	public static void AddToRecentServersList(ServerJoinData data)
	{
		if (!data.IsValid)
		{
			ZLog.LogError($"Couldn't add server to server list, server data {data} was invalid!");
			return;
		}
		if (s_instance != null)
		{
			s_instance.AddToRecentServersListCached(data);
			return;
		}
		LocalServerList localServerList = new LocalServerList(null, GetServerListLocations("recent"));
		localServerList.Remove(data);
		localServerList.AddToBeginning(data);
		while (localServerList.Count > 11)
		{
			localServerList.Remove(localServerList[localServerList.Count - 1]);
		}
		SaveStatusCode saveStatusCode = localServerList.Save();
		switch (saveStatusCode)
		{
		case SaveStatusCode.CloudQuotaExceeded:
			ZLog.LogWarning("Couln't add server " + data.ToString() + " to server list, cloud quota exceeded.");
			break;
		default:
			ZLog.LogError($"Couln't add server {data.ToString()} to server list: {saveStatusCode}");
			break;
		case SaveStatusCode.Succeess:
			ZLog.Log("Added server " + data.ToString() + " to server list");
			break;
		}
		localServerList.Dispose();
	}

	private void AddToRecentServersListCached(ServerJoinData data)
	{
		m_recentServersList.Remove(data);
		m_recentServersList.AddToBeginning(data);
		while (m_recentServersList.Count > 11)
		{
			m_recentServersList.Remove(m_recentServersList[m_recentServersList.Count - 1]);
		}
		ZLog.Log("Added server with name " + MultiBackendMatchmaking.GetServerName(data) + " to server list");
	}

	public void OnAddServerOpen()
	{
		if (!m_filterInputField.isFocused)
		{
			m_addServerPanel.SetActive(value: true);
		}
	}

	public void OnAddServerClose()
	{
		m_addServerPanel.SetActive(value: false);
	}

	public void OnAddServer()
	{
		m_addServerPanel.SetActive(value: true);
		string text = m_addServerTextInput.text;
		string[] array = text.Split(':');
		if (array.Length == 0)
		{
			return;
		}
		if (array.Length == 1)
		{
			string text2 = array[0];
			if (ZPlayFabMatchmaking.IsJoinCode(text2))
			{
				if (PlayFabManager.IsLoggedIn)
				{
					OnManualAddToFavoritesStart();
					MultiBackendMatchmaking.PlayFabBackend.ResolveJoinCode(text2, OnPlayFabJoinCodeSuccess, OnJoinCodeFailed);
				}
				else
				{
					OnJoinCodeFailed(ZPLayFabMatchmakingFailReason.NotLoggedIn);
				}
				return;
			}
		}
		ServerJoinDataUtils.GetAddressAndPortFromString(text, out var ipAddress, out var _);
		if (!string.IsNullOrEmpty(ipAddress))
		{
			ServerJoinDataDedicated newServerListEntryDedicated = new ServerJoinDataDedicated(text);
			OnManualAddToFavoritesStart();
			MultiBackendMatchmaking.GetServerIPAsync(newServerListEntryDedicated, delegate(bool success, IPv6Address? address)
			{
				if (success && address.HasValue)
				{
					OnManualAddToFavoritesSuccess(new ServerJoinData(newServerListEntryDedicated));
				}
				else
				{
					if (newServerListEntryDedicated.IsURL)
					{
						UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfaileddnslookup", delegate
						{
							UnifiedPopup.Pop();
						}));
					}
					else
					{
						UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfailedincorrectformatting", delegate
						{
							UnifiedPopup.Pop();
						}));
					}
					m_isAwaitingServerAdd = false;
				}
			});
		}
		else
		{
			UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfailedincorrectformatting", delegate
			{
				UnifiedPopup.Pop();
			}));
		}
	}

	private void OnManualAddToFavoritesStart()
	{
		m_isAwaitingServerAdd = true;
	}

	private void OnManualAddToFavoritesSuccess(ServerJoinData newFavoriteServer)
	{
		if (!m_favoriteServersList.Contains(newFavoriteServer))
		{
			m_favoriteServersList.Add(newFavoriteServer);
		}
		m_filteredListOutdated = true;
		m_serverListTabHandler.SetActiveTab(0);
		m_startup.SetServerToJoin(newFavoriteServer);
		UpdateServerListGui(centerSelection: true);
		OnAddServerClose();
		m_addServerTextInput.text = "";
		m_isAwaitingServerAdd = false;
	}

	private void OnPlayFabJoinCodeSuccess(ServerData serverData)
	{
		if (!serverData.m_joinData.IsValid || serverData.m_matchmakingData.m_networkVersion != 36)
		{
			UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$error_incompatibleversion", delegate
			{
				UnifiedPopup.Pop();
			}));
			m_isAwaitingServerAdd = false;
		}
		else if (!serverData.m_matchmakingData.IsCrossplay && !serverData.m_matchmakingData.IsRestrictedToOwnPlatform)
		{
			UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$error_platformexcluded", delegate
			{
				UnifiedPopup.Pop();
			}));
			m_isAwaitingServerAdd = false;
		}
		else if (serverData.m_matchmakingData.IsCrossplay && PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.CrossPlatformMultiplayer) != PrivilegeResult.Granted)
		{
			if (PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege != null)
			{
				PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.Open(Privilege.CrossPlatformMultiplayer);
			}
			else
			{
				UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$xbox_error_crossplayprivilege", delegate
				{
					UnifiedPopup.Pop();
				}));
			}
			m_isAwaitingServerAdd = false;
		}
		else
		{
			ZPlayFabMatchmaking.JoinCode = serverData.m_matchmakingData.m_joinCode;
			OnManualAddToFavoritesSuccess(serverData.m_joinData);
		}
	}

	private void OnJoinCodeFailed(ZPLayFabMatchmakingFailReason failReason)
	{
		ZLog.Log("Failed to resolve join code for the following reason: " + failReason);
		m_isAwaitingServerAdd = false;
		UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfailedresolvejoincode", delegate
		{
			UnifiedPopup.Pop();
		}));
	}
}
