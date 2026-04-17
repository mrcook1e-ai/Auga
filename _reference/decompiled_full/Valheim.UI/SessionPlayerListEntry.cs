using System;
using Splatform;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UserManagement;

namespace Valheim.UI;

public class SessionPlayerListEntry : MonoBehaviour
{
	[SerializeField]
	private Button _button;

	[SerializeField]
	private Selectable _focusPoint;

	[SerializeField]
	private Image _selection;

	[SerializeField]
	private GameObject _viewPlayerCard;

	[SerializeField]
	private Image _outline;

	[Header("Player")]
	[SerializeField]
	private Image _hostIcon;

	[SerializeField]
	private Image _gamerpic;

	[SerializeField]
	private Sprite otherPlatformPlayerPic;

	[SerializeField]
	private TextMeshProUGUI _gamertagText;

	[SerializeField]
	private TextMeshProUGUI _characterNameText;

	[Header("Block")]
	[SerializeField]
	private Button _blockButton;

	[SerializeField]
	private Image _blockButtonImage;

	[SerializeField]
	private Sprite _blockSprite;

	[SerializeField]
	private Sprite _unblockSprite;

	[Header("Kick")]
	[SerializeField]
	private Button _kickButton;

	[SerializeField]
	private Image _kickButtonImage;

	private PlatformUserID _user;

	private IUserProfile _userProfile;

	private string _gamertag;

	private string _characterName;

	public bool IsSelected => _selection.enabled;

	public Selectable FocusObject => _focusPoint;

	public Selectable BlockButton => _blockButton;

	public Selectable KickButton => _kickButton;

	public PlatformUserID User => _user;

	public bool HasFocusObject => _focusPoint.gameObject.activeSelf;

	public bool HasBlock => _blockButtonImage.gameObject.activeSelf;

	public bool HasKick => _kickButtonImage.gameObject.activeSelf;

	public bool HasActivatedButtons
	{
		get
		{
			if (!_blockButtonImage.gameObject.activeSelf)
			{
				return _kickButtonImage.gameObject.activeSelf;
			}
			return true;
		}
	}

	public bool IsSamePlatform => _user.m_platform == PlatformManager.DistributionPlatform.Platform;

	public bool IsOwnPlayer
	{
		get
		{
			return _outline.gameObject.activeSelf;
		}
		set
		{
			_outline.gameObject.SetActive(value);
		}
	}

	public bool IsHost
	{
		get
		{
			return _hostIcon.gameObject.activeSelf;
		}
		set
		{
			_hostIcon.gameObject.SetActive(value);
		}
	}

	private bool CanBeKicked
	{
		get
		{
			return _kickButtonImage.gameObject.activeSelf;
		}
		set
		{
			_kickButtonImage.gameObject.SetActive(value && !IsHost);
		}
	}

	private bool CanBeBlocked
	{
		get
		{
			return _blockButtonImage.gameObject.activeSelf;
		}
		set
		{
			_blockButtonImage.gameObject.SetActive(value);
		}
	}

	private bool CanBeMuted
	{
		get
		{
			return false;
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public string Gamertag
	{
		get
		{
			return _gamertag;
		}
		set
		{
			string text = "";
			if (value != null)
			{
				text += value;
			}
			_gamertag = text;
			if (IsHost)
			{
				text += " (Host)";
			}
			_gamertagText.text = text;
		}
	}

	public string CharacterName
	{
		get
		{
			return _characterName;
		}
		set
		{
			string text = value;
			if (!IsOwnPlayer)
			{
				text = CensorShittyWords.FilterUGC(text, UGCType.CharacterName, _user, 0L);
			}
			_characterName = text;
			_characterNameText.text = text;
		}
	}

	public event Action<SessionPlayerListEntry> OnKicked;

	private void Awake()
	{
		_selection.enabled = false;
		_viewPlayerCard.SetActive(value: false);
		if (_button != null)
		{
			_button.enabled = true;
		}
	}

	private void Update()
	{
		if (EventSystem.current != null && (EventSystem.current.currentSelectedGameObject == _focusPoint.gameObject || EventSystem.current.currentSelectedGameObject == _blockButton.gameObject || EventSystem.current.currentSelectedGameObject == _kickButton.gameObject || EventSystem.current.currentSelectedGameObject == _button.gameObject))
		{
			SelectEntry();
		}
		else
		{
			Deselect();
		}
		UpdateFocusPoint();
	}

	public void SelectEntry()
	{
		_selection.enabled = true;
		_viewPlayerCard.SetActive(IsSamePlatform);
	}

	public void Deselect()
	{
		_selection.enabled = false;
		_viewPlayerCard.SetActive(value: false);
	}

	public void OnBlock()
	{
		if (RelationsManager.IsBlocked(_user))
		{
			OnViewCard();
			return;
		}
		if (MuteList.Contains(_user))
		{
			MuteList.Unblock(_user);
		}
		else
		{
			MuteList.Block(_user);
		}
		UpdateBlockButton();
	}

	private void UpdateButtons()
	{
		UpdateBlockButton();
		UpdateFocusPoint();
	}

	private void UpdateFocusPoint()
	{
		_focusPoint.gameObject.SetActive(!HasActivatedButtons);
	}

	public void UpdateBlockButton()
	{
		_blockButtonImage.sprite = ((MuteList.Contains(_user) || RelationsManager.IsBlocked(_user)) ? _unblockSprite : _blockSprite);
	}

	public void OnKick()
	{
		if (ZNet.instance != null)
		{
			UnifiedPopup.Push(new YesNoPopup("$menu_kick_player_title", Localization.instance.Localize("$menu_kick_player", CharacterName), delegate
			{
				ZNet.instance.Kick(CharacterName);
				this.OnKicked?.Invoke(this);
				UnifiedPopup.Pop();
			}, delegate
			{
				UnifiedPopup.Pop();
			}));
		}
	}

	public void SetValues(string characterName, PlatformUserID user, bool isHost, bool canBeBlocked, bool canBeKicked)
	{
		_user = user;
		IsHost = isHost;
		CharacterName = characterName;
		Gamertag = "";
		_gamerpic.sprite = otherPlatformPlayerPic;
		if (IsSamePlatform && PlatformManager.DistributionPlatform.RelationsProvider != null)
		{
			PlatformManager.DistributionPlatform.RelationsProvider.GetUserProfileAsync(user, GetUserProfileCompleted, GetUserProfileFailed);
		}
		else
		{
			UpdateProfile();
		}
		CanBeKicked = !isHost && canBeKicked;
		CanBeBlocked = canBeBlocked;
		UpdateButtons();
	}

	private void GetUserProfileCompleted(IUserProfile profile)
	{
		if (!(this == null))
		{
			_ = DateTime.UtcNow;
			_userProfile = profile;
			_userProfile.RequestProfilePictureAsync(GetProfilePictureResolution());
			UpdateProfile();
			_userProfile.ProfileDataUpdated += UpdateProfile;
			UpdateProfilePicture();
			_userProfile.ProfilePictureUpdated += UpdateProfilePicture;
		}
	}

	private static uint GetProfilePictureResolution()
	{
		if (PlatformManager.DistributionPlatform.HardwareInfoProvider == null)
		{
			return 128u;
		}
		HardwareInfo hardwareInfo = PlatformManager.DistributionPlatform.HardwareInfoProvider.HardwareInfo;
		if (hardwareInfo.m_category == HardwareCategory.Unknown)
		{
			return 128u;
		}
		if (hardwareInfo.m_category < HardwareCategory.Console)
		{
			return 50u;
		}
		if (hardwareInfo.m_category == HardwareCategory.Console && hardwareInfo.m_generation <= 8)
		{
			return 50u;
		}
		return 128u;
	}

	private void GetUserProfileFailed(PlatformUserID userId, GetUserProfileFailReason failReason)
	{
		switch (failReason)
		{
		case GetUserProfileFailReason.DifferentPlatformsNotAvailable:
		case GetUserProfileFailReason.SamePlatformNotAvailable:
			return;
		}
		Debug.LogError($"Failed to get user profile for user {userId}: {failReason}");
	}

	private void UpdateProfile()
	{
		string displayName;
		if (IsSamePlatform)
		{
			Gamertag = _userProfile.DisplayName;
		}
		else if (ZNet.TryGetServerAssignedDisplayName(_user, out displayName))
		{
			Gamertag = displayName;
		}
		UpdateButtons();
	}

	private void UpdateProfilePicture()
	{
		if (IsSamePlatform && _userProfile.ProfilePicture != null)
		{
			_gamerpic.SetSpriteFromTexture(_userProfile.ProfilePicture);
		}
		else
		{
			_gamerpic.sprite = otherPlatformPlayerPic;
		}
	}

	public void OnViewCard()
	{
		if (PlatformManager.DistributionPlatform.UIProvider.ShowUserProfile != null && IsSamePlatform)
		{
			PlatformManager.DistributionPlatform.UIProvider.ShowUserProfile.Open(_user);
		}
	}

	public void RemoveCallbacks()
	{
		if (_userProfile != null)
		{
			_userProfile.ProfileDataUpdated -= UpdateProfile;
			_userProfile.ProfilePictureUpdated -= UpdateProfilePicture;
		}
	}
}
