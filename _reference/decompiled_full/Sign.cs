using Splatform;
using TMPro;
using UnityEngine;
using UserManagement;

public class Sign : MonoBehaviour, Hoverable, Interactable, TextReceiver
{
	public TextMeshProUGUI m_textWidget;

	public string m_name = "Sign";

	public string m_defaultText = "Sign";

	public string m_writtenBy = "Written by";

	public int m_characterLimit = 50;

	private ZNetView m_nview;

	private bool m_isViewable = true;

	private string m_authorDisplayName = "";

	private PlatformUserID? m_author;

	private string m_currentText;

	private uint m_lastRevision = uint.MaxValue;

	private void Awake()
	{
		m_currentText = m_defaultText;
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			UpdateText();
			InvokeRepeating("UpdateText", 2f, 2f);
		}
	}

	public string GetHoverText()
	{
		string text = (m_isViewable ? ("\"" + GetText().RemoveRichTextTags() + "\"") : ((!m_author.HasValue || !MuteList.Contains(m_author.Value)) ? ("[" + Localization.instance.Localize("$text_hidden_notification_ugc_settings") + "]") : ("[" + Localization.instance.Localize("$text_hidden_notification_muted") + "]")));
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return text;
		}
		string text2 = "";
		return text + text2 + "\n" + Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return false;
		}
		PrivilegeResult privilegeResult = PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.ViewUserGeneratedContent);
		if (!privilegeResult.IsGranted())
		{
			if (PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege != null)
			{
				PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.Open(Privilege.ViewUserGeneratedContent);
				if (!PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.IsOpen)
				{
					ZLog.LogError(string.Format("{0} can't resolve the {1} privilege on this platform, which was denied with result {2}. Modifying sign text was blocked without meaningful feedback to the user!", "ResolvePrivilegeUI", Privilege.ViewUserGeneratedContent, privilegeResult));
				}
			}
			else
			{
				ZLog.LogError(string.Format("{0} is not available on this platform to resolve the {1} privilege, which was denied with result {2}. Modifying sign text was blocked without meaningful feedback to the user!", "ResolvePrivilegeUI", Privilege.ViewUserGeneratedContent, privilegeResult));
			}
			return false;
		}
		TextInput.instance.RequestText(this, "$piece_sign_input", m_characterLimit);
		return true;
	}

	private void UpdateText()
	{
		uint dataRevision = m_nview.GetZDO().DataRevision;
		if (m_lastRevision == dataRevision)
		{
			UpdateViewPermission();
			return;
		}
		m_lastRevision = dataRevision;
		string currentText = m_nview.GetZDO().GetString(ZDOVars.s_text, m_defaultText);
		m_currentText = currentText;
		m_authorDisplayName = m_nview.GetZDO().GetString(ZDOVars.s_authorDisplayName);
		string resolvedAuthor = m_nview.GetZDO().GetString(ZDOVars.s_author);
		if (m_nview.IsOwner() && RelationsManager.UpdateAuthorIfHost(resolvedAuthor, ref resolvedAuthor))
		{
			m_nview.GetZDO().Set(ZDOVars.s_author, resolvedAuthor);
		}
		if (string.IsNullOrEmpty(resolvedAuthor))
		{
			m_author = PlatformUserID.None;
		}
		else if (resolvedAuthor == "host")
		{
			m_author = null;
		}
		else
		{
			m_author = new PlatformUserID(resolvedAuthor);
		}
		UpdateViewPermission();
	}

	private void UpdateViewPermission()
	{
		if (!m_author.HasValue)
		{
			OnCheckPermissionCompleted(RelationsManagerPermissionResult.Denied);
		}
		if (m_author.Value.IsValid)
		{
			RelationsManager.CheckPermissionAsync(m_author.Value, Permission.ViewUserGeneratedContent, isSender: false, OnCheckPermissionCompleted);
		}
		else
		{
			OnCheckPermissionCompleted(RelationsManagerPermissionResult.Granted);
		}
	}

	private void OnCheckPermissionCompleted(RelationsManagerPermissionResult result)
	{
		if (result.IsGranted())
		{
			m_isViewable = true;
			if (result == RelationsManagerPermissionResult.GrantedRequiresFiltering)
			{
				CensorShittyWords.Filter(m_currentText, out var output);
				m_textWidget.text = output;
			}
			else
			{
				m_textWidget.text = m_currentText;
			}
		}
		else
		{
			m_isViewable = false;
			m_textWidget.text = "ᚬᛏᛁᛚᛚᚴᛅᚾᚴᛚᛁᚴ";
		}
	}

	public string GetText()
	{
		return m_textWidget.text;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void SetText(string text)
	{
		if (PrivateArea.CheckAccess(base.transform.position))
		{
			m_nview.ClaimOwnership();
			m_nview.GetZDO().Set(ZDOVars.s_text, text);
			PlatformUserID platformUserID = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
			m_nview.GetZDO().Set(ZDOVars.s_author, PlatformManager.DistributionPlatform.LocalUser.IsSignedIn ? platformUserID.ToString() : "host");
			if (ZNet.TryGetServerAssignedDisplayName(platformUserID, out var displayName))
			{
				m_nview.GetZDO().Set(ZDOVars.s_authorDisplayName, displayName);
			}
			UpdateText();
		}
	}
}
