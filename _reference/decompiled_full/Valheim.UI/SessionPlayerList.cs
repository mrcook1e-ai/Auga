using System.Collections.Generic;
using Splatform;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using UserManagement;

namespace Valheim.UI;

public class SessionPlayerList : MonoBehaviour
{
	[SerializeField]
	protected SessionPlayerListEntry _ownPlayer;

	[SerializeField]
	protected ScrollRect _scrollRect;

	[SerializeField]
	protected Button _backButton;

	private List<ZNet.PlayerInfo> _players;

	private readonly List<SessionPlayerListEntry> _remotePlayers = new List<SessionPlayerListEntry>();

	private readonly List<SessionPlayerListEntry> _allPlayers = new List<SessionPlayerListEntry>();

	protected void Awake()
	{
		MuteList.Load(Init);
	}

	private void Init()
	{
		SetEntries();
		foreach (SessionPlayerListEntry allPlayer in _allPlayers)
		{
			allPlayer.OnKicked += OnPlayerWasKicked;
		}
		_ownPlayer.FocusObject.Select();
		UpdateBlockButtons();
	}

	private void UpdateBlockButtons()
	{
		if (this == null)
		{
			return;
		}
		foreach (SessionPlayerListEntry allPlayer in _allPlayers)
		{
			allPlayer.UpdateBlockButton();
		}
	}

	private void OnPlayerWasKicked(SessionPlayerListEntry player)
	{
		player.OnKicked -= OnPlayerWasKicked;
		_allPlayers.Remove(player);
		_remotePlayers.Remove(player);
		Object.Destroy(player.gameObject);
		UpdateNavigation();
	}

	private void SetEntries()
	{
		_allPlayers.Add(_ownPlayer);
		PlatformUserID platformUserID = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
		_players = ZNet.instance.GetPlayerList();
		ZNetPeer serverPeer = ZNet.instance.GetServerPeer();
		if (!ZNet.instance.IsServer() && _players.TryFindPlayerByPlayername(serverPeer.m_playerName, out var playerInfo))
		{
			if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
			{
				PlatformUserID user = new PlatformUserID(PlatformManager.DistributionPlatform.Platform, serverPeer.m_socket.GetEndPointString());
				CreatePlayerEntry(user, serverPeer.m_playerName, isHost: true);
			}
			else
			{
				CreatePlayerEntry(playerInfo.Value.m_userInfo.m_id, playerInfo.Value.m_name, isHost: true);
			}
		}
		for (int i = 0; i < _players.Count; i++)
		{
			ZNet.PlayerInfo playerInfo2 = _players[i];
			if (playerInfo2.m_userInfo.m_id == platformUserID)
			{
				PlatformUserID user2 = new PlatformUserID(PlatformManager.DistributionPlatform.Platform, (ulong)SteamUser.GetSteamID());
				SetOwnPlayer(user2, ZNet.instance.IsServer());
			}
			else if (serverPeer == null || playerInfo2.m_name != serverPeer.m_playerName)
			{
				CreatePlayerEntry(playerInfo2.m_userInfo.m_id, playerInfo2.m_name);
			}
		}
		UpdateNavigation();
	}

	private void UpdateNavigation()
	{
		Navigation navigation = new Navigation
		{
			mode = Navigation.Mode.Explicit
		};
		int count = _allPlayers.Count;
		for (int i = 0; i < count; i++)
		{
			SessionPlayerListEntry sessionPlayerListEntry = _allPlayers[i];
			SessionPlayerListEntry sessionPlayerListEntry2 = ((i < count - 1) ? _allPlayers[i + 1] : null);
			Navigation navigation2 = sessionPlayerListEntry.BlockButton.navigation;
			navigation2.mode = (sessionPlayerListEntry.HasBlock ? Navigation.Mode.Explicit : Navigation.Mode.None);
			Navigation navigation3 = sessionPlayerListEntry.KickButton.navigation;
			navigation3.mode = (sessionPlayerListEntry.HasKick ? Navigation.Mode.Explicit : Navigation.Mode.None);
			Navigation navigation4 = sessionPlayerListEntry.FocusObject.navigation;
			navigation4.mode = (sessionPlayerListEntry.HasFocusObject ? Navigation.Mode.Explicit : Navigation.Mode.None);
			if (sessionPlayerListEntry2 != null)
			{
				if (!sessionPlayerListEntry.HasActivatedButtons && !sessionPlayerListEntry2.HasActivatedButtons)
				{
					navigation4.selectOnDown = sessionPlayerListEntry2.FocusObject;
				}
				else if (!sessionPlayerListEntry.HasActivatedButtons && sessionPlayerListEntry2.HasActivatedButtons)
				{
					if (sessionPlayerListEntry2.HasBlock)
					{
						navigation4.selectOnDown = sessionPlayerListEntry2.BlockButton;
					}
					else if (sessionPlayerListEntry2.HasKick)
					{
						navigation4.selectOnDown = sessionPlayerListEntry2.KickButton;
					}
				}
				else if (sessionPlayerListEntry.HasActivatedButtons && !sessionPlayerListEntry2.HasActivatedButtons)
				{
					if (sessionPlayerListEntry.HasBlock)
					{
						navigation2.selectOnDown = sessionPlayerListEntry2.FocusObject;
					}
					if (sessionPlayerListEntry.HasKick)
					{
						navigation3.selectOnDown = sessionPlayerListEntry2.FocusObject;
					}
				}
				else
				{
					if (sessionPlayerListEntry.HasBlock)
					{
						if (sessionPlayerListEntry2.HasBlock)
						{
							navigation2.selectOnDown = sessionPlayerListEntry2.BlockButton;
						}
						else if (sessionPlayerListEntry2.HasKick)
						{
							navigation2.selectOnDown = sessionPlayerListEntry2.KickButton;
						}
					}
					if (sessionPlayerListEntry.HasKick)
					{
						if (sessionPlayerListEntry2.HasKick)
						{
							navigation3.selectOnDown = sessionPlayerListEntry2.KickButton;
						}
						else if (sessionPlayerListEntry2.HasBlock)
						{
							navigation3.selectOnDown = sessionPlayerListEntry2.BlockButton;
						}
					}
				}
			}
			else if (sessionPlayerListEntry.HasActivatedButtons)
			{
				if (sessionPlayerListEntry.HasBlock)
				{
					navigation.selectOnUp = sessionPlayerListEntry.BlockButton;
				}
				else if (sessionPlayerListEntry.HasKick)
				{
					navigation.selectOnUp = sessionPlayerListEntry.KickButton;
				}
				if (sessionPlayerListEntry.HasBlock)
				{
					navigation2.selectOnDown = _backButton;
				}
				if (sessionPlayerListEntry.HasKick)
				{
					navigation3.selectOnDown = _backButton;
				}
			}
			else
			{
				navigation4.selectOnDown = _backButton;
				navigation.selectOnUp = sessionPlayerListEntry.FocusObject;
			}
			sessionPlayerListEntry.BlockButton.navigation = navigation2;
			sessionPlayerListEntry.KickButton.navigation = navigation3;
			sessionPlayerListEntry.FocusObject.navigation = navigation4;
		}
		for (int num = count - 1; num >= 0; num--)
		{
			SessionPlayerListEntry sessionPlayerListEntry3 = _allPlayers[num];
			SessionPlayerListEntry sessionPlayerListEntry4 = ((num > 0) ? _allPlayers[num - 1] : null);
			Navigation navigation5 = sessionPlayerListEntry3.BlockButton.navigation;
			Navigation navigation6 = sessionPlayerListEntry3.KickButton.navigation;
			Navigation navigation7 = sessionPlayerListEntry3.FocusObject.navigation;
			if (sessionPlayerListEntry4 != null)
			{
				if (!sessionPlayerListEntry3.HasActivatedButtons && !sessionPlayerListEntry4.HasActivatedButtons)
				{
					navigation7.selectOnUp = sessionPlayerListEntry4.FocusObject;
				}
				else if (!sessionPlayerListEntry3.HasActivatedButtons && sessionPlayerListEntry4.HasActivatedButtons)
				{
					if (sessionPlayerListEntry4.HasBlock)
					{
						navigation7.selectOnUp = sessionPlayerListEntry4.BlockButton;
					}
					else if (sessionPlayerListEntry4.HasKick)
					{
						navigation7.selectOnUp = sessionPlayerListEntry4.KickButton;
					}
				}
				else if (sessionPlayerListEntry3.HasActivatedButtons && !sessionPlayerListEntry4.HasActivatedButtons)
				{
					if (sessionPlayerListEntry3.HasBlock)
					{
						navigation5.selectOnUp = sessionPlayerListEntry4.FocusObject;
					}
					if (sessionPlayerListEntry3.HasKick)
					{
						navigation6.selectOnUp = sessionPlayerListEntry4.FocusObject;
					}
				}
				else
				{
					if (sessionPlayerListEntry3.HasBlock)
					{
						if (sessionPlayerListEntry4.HasBlock)
						{
							navigation5.selectOnUp = sessionPlayerListEntry4.BlockButton;
						}
						else if (sessionPlayerListEntry4.HasKick)
						{
							navigation5.selectOnUp = sessionPlayerListEntry4.KickButton;
						}
					}
					if (sessionPlayerListEntry3.HasKick)
					{
						if (sessionPlayerListEntry4.HasKick)
						{
							navigation6.selectOnUp = sessionPlayerListEntry4.KickButton;
						}
						else if (sessionPlayerListEntry4.HasBlock)
						{
							navigation6.selectOnUp = sessionPlayerListEntry4.BlockButton;
						}
					}
				}
			}
			sessionPlayerListEntry3.BlockButton.navigation = navigation5;
			sessionPlayerListEntry3.KickButton.navigation = navigation6;
			sessionPlayerListEntry3.FocusObject.navigation = navigation7;
		}
		_backButton.navigation = navigation;
	}

	private void SetOwnPlayer(PlatformUserID user, bool isHost)
	{
		UserInfo localUser = UserInfo.GetLocalUser();
		_ownPlayer.IsOwnPlayer = true;
		_ownPlayer.SetValues(localUser.Name, user, isHost, canBeBlocked: false, canBeKicked: false);
	}

	private void CreatePlayerEntry(PlatformUserID user, string name, bool isHost = false)
	{
		SessionPlayerListEntry sessionPlayerListEntry = Object.Instantiate(_ownPlayer, _scrollRect.content);
		sessionPlayerListEntry.IsOwnPlayer = false;
		sessionPlayerListEntry.SetValues(name, user, isHost, canBeBlocked: true, !isHost && ZNet.instance.LocalPlayerIsAdminOrHost());
		if (!isHost)
		{
			_remotePlayers.Add(sessionPlayerListEntry);
		}
		_allPlayers.Add(sessionPlayerListEntry);
	}

	public void OnBack()
	{
		foreach (SessionPlayerListEntry allPlayer in _allPlayers)
		{
			allPlayer.RemoveCallbacks();
		}
		MuteList.Persist();
		Object.Destroy(base.gameObject);
	}

	private void Update()
	{
		UpdateScrollPosition();
		if (ZInput.GetKeyDown(KeyCode.Escape))
		{
			OnBack();
		}
	}

	private void UpdateScrollPosition()
	{
		if (!_scrollRect.verticalScrollbar.gameObject.activeSelf)
		{
			return;
		}
		foreach (SessionPlayerListEntry allPlayer in _allPlayers)
		{
			if (allPlayer.IsSelected && !_scrollRect.IsVisible(allPlayer.transform as RectTransform))
			{
				_scrollRect.SnapToChild(allPlayer.transform as RectTransform);
				break;
			}
		}
	}
}
