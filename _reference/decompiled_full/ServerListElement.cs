using System.Text;
using Splatform;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal class ServerListElement
{
	public readonly GameObject m_element;

	public readonly Button m_button;

	public readonly RectTransform m_rectTransform;

	private readonly TMP_Text m_serverName;

	private readonly TMP_Text m_modifiers;

	private readonly UITooltip m_tooltip;

	private readonly TMP_Text m_version;

	private readonly TMP_Text m_players;

	private readonly Image m_status;

	private readonly Transform m_crossplay;

	private readonly Transform m_private;

	public readonly RectTransform m_selected;

	private bool m_currentlySelected;

	private ServerListEntryData m_serverListEntry = ServerListEntryData.None;

	public ServerJoinData Server => m_serverListEntry.m_joinData;

	public ServerListElement(GameObject element)
	{
		m_element = element;
		m_button = m_element.GetComponent<Button>();
		m_rectTransform = m_element.transform as RectTransform;
		m_serverName = m_element.GetComponentInChildren<TMP_Text>();
		m_modifiers = m_element.transform.Find("modifiers").GetComponent<TMP_Text>();
		m_tooltip = m_element.GetComponentInChildren<UITooltip>();
		m_version = m_element.transform.Find("version").GetComponent<TMP_Text>();
		m_players = m_element.transform.Find("players").GetComponent<TMP_Text>();
		m_status = m_element.transform.Find("status").GetComponent<Image>();
		m_crossplay = m_element.transform.Find("crossplay");
		m_private = m_element.transform.Find("Private");
		m_selected = m_element.transform.Find("selected") as RectTransform;
		m_currentlySelected = m_selected.gameObject.activeSelf;
		m_tooltip.m_gamepadFocusObject = m_selected.gameObject;
	}

	public void UpdateDisplayData(ref ServerListEntryData serverEntry, bool selected, RectTransform tooltipAnchor, ref ConnectIcons connectIcons)
	{
		UpdateTextAndIcons(ref serverEntry, tooltipAnchor, ref connectIcons);
		UpdateSelectionHighlight(selected);
	}

	private void UpdateTextAndIcons(ref ServerListEntryData serverEntry, RectTransform tooltipAnchor, ref ConnectIcons connectIcons)
	{
		if (m_serverListEntry.Equals(ref serverEntry))
		{
			return;
		}
		m_serverListEntry = serverEntry;
		StringBuilder stringBuilder = new StringBuilder();
		string serverName = m_serverListEntry.m_serverName;
		m_serverName.text = CensorShittyWords.FilterUGC(serverName, UGCType.ServerName, default(PlatformUserID), 0L);
		bool flag = m_serverListEntry.m_modifiers != null && m_serverListEntry.m_modifiers.Length != 0;
		m_modifiers.text = (flag ? Localization.instance.Localize(ServerOptionsGUI.GetWorldModifierSummary(m_serverListEntry.m_modifiers, alwaysShort: true)) : "");
		stringBuilder.Append(flag ? ServerOptionsGUI.GetWorldModifierSummary(m_serverListEntry.m_modifiers, alwaysShort: false, "\n") : "-");
		stringBuilder.Append("\n\n");
		if (m_serverListEntry.m_joinData.m_type.DisplayUnderlyingDataToUser())
		{
			stringBuilder.Append(m_serverListEntry.m_joinData.ToString() + "\n");
		}
		stringBuilder.Append("(" + m_serverListEntry.m_joinData.m_type.ServerTypeDisplayName() + ")");
		m_tooltip.Set("$menu_serveroptions", stringBuilder.ToString(), tooltipAnchor);
		stringBuilder.Clear();
		if (m_serverListEntry.IsUnjoinable)
		{
			m_version.text = "";
			m_players.text = "";
			m_status.sprite = connectIcons.m_failed;
			m_crossplay.gameObject.SetActive(value: false);
			m_private.gameObject.SetActive(value: false);
			return;
		}
		m_version.text = m_serverListEntry.m_gameVersion.ToString();
		if (m_serverListEntry.IsOnline)
		{
			TMP_Text players = m_players;
			uint playerCount = m_serverListEntry.m_playerCount;
			string text = playerCount.ToString();
			playerCount = m_serverListEntry.m_playerLimit;
			players.text = text + " / " + playerCount;
		}
		else
		{
			m_players.text = "";
		}
		if (m_serverListEntry.HasMatchmakingData)
		{
			if (m_serverListEntry.IsOnline)
			{
				m_status.sprite = connectIcons.m_success;
			}
			else if (m_serverListEntry.IsAvailable)
			{
				m_status.sprite = connectIcons.m_failed;
			}
			else
			{
				m_status.sprite = connectIcons.m_unknown;
			}
		}
		else
		{
			m_status.sprite = connectIcons.m_trying;
		}
		m_crossplay.gameObject.SetActive(m_serverListEntry.IsCrossplay);
		m_private.gameObject.SetActive(m_serverListEntry.IsPasswordProtected);
	}

	private void UpdateSelectionHighlight(bool selected)
	{
		if (m_currentlySelected != selected)
		{
			m_currentlySelected = selected;
			m_selected.gameObject.SetActive(m_currentlySelected);
		}
	}
}
