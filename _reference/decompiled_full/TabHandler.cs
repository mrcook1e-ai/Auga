using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TabHandler : MonoBehaviour
{
	[Serializable]
	public class Tab
	{
		public Button m_button;

		public RectTransform m_page;

		public bool m_default;

		public UnityEvent m_onClick;
	}

	public bool m_cycling = true;

	public bool m_tabKeyInput = true;

	public bool m_keybaordInput;

	public string m_keyboardNavigateLeft = "TabLeft";

	public string m_keyboardNavigateRight = "TabRight";

	public bool m_gamepadInput;

	public string m_gamepadNavigateLeft = "JoyTabLeft";

	public string m_gamepadNavigateRight = "JoyTabRight";

	private bool m_activeTabEverSet;

	public List<Tab> m_tabs = new List<Tab>();

	public List<GameObject> m_blockingElements = new List<GameObject>();

	[Header("Effects")]
	public EffectList m_setActiveTabEffects = new EffectList();

	private int m_selected;

	private UIGamePad gamePad;

	public event Action<int> ActiveTabChanged;

	private void Start()
	{
		Init(forceSelect: true);
	}

	public void Init(bool forceSelect = false)
	{
		int num = -1;
		for (int i = 0; i < m_tabs.Count; i++)
		{
			Tab tab = m_tabs[i];
			if (!tab.m_button)
			{
				continue;
			}
			tab.m_button.onClick.AddListener(delegate
			{
				OnClick(tab.m_button);
			});
			Transform transform = tab.m_button.gameObject.transform.Find("Selected");
			if ((bool)transform)
			{
				TMP_Text componentInChildren = transform.GetComponentInChildren<TMP_Text>();
				TMP_Text componentInChildren2 = tab.m_button.GetComponentInChildren<TMP_Text>();
				string text = null;
				if (componentInChildren2 != null)
				{
					text = componentInChildren2.text;
				}
				else
				{
					TextMeshProUGUI componentInChildren3 = tab.m_button.GetComponentInChildren<TextMeshProUGUI>();
					if (componentInChildren3 != null)
					{
						text = componentInChildren3.text;
					}
				}
				if (componentInChildren != null)
				{
					componentInChildren.text = text;
				}
				else
				{
					TextMeshProUGUI componentInChildren4 = transform.GetComponentInChildren<TextMeshProUGUI>();
					if (componentInChildren4 != null)
					{
						componentInChildren4.text = text;
					}
				}
			}
			if (tab.m_default)
			{
				num = i;
			}
		}
		if (!m_activeTabEverSet && num >= 0)
		{
			SetActiveTab(num, forceSelect);
		}
		gamePad = GetComponent<UIGamePad>();
	}

	private void Update()
	{
		if (UnifiedPopup.IsVisible())
		{
			return;
		}
		if (m_blockingElements.Count > 0)
		{
			foreach (GameObject blockingElement in m_blockingElements)
			{
				if (blockingElement.activeSelf)
				{
					return;
				}
			}
		}
		int num = 0;
		if (m_gamepadInput && (gamePad == null || !gamePad.IsBlocked()))
		{
			if (!string.IsNullOrEmpty(m_gamepadNavigateLeft) && ZInput.GetButtonDown(m_gamepadNavigateLeft))
			{
				num = -1;
			}
			else if (!string.IsNullOrEmpty(m_gamepadNavigateRight) && ZInput.GetButtonDown(m_gamepadNavigateRight))
			{
				num = 1;
			}
		}
		if (m_keybaordInput)
		{
			if (!string.IsNullOrEmpty(m_keyboardNavigateLeft) && ZInput.GetButtonDown(m_keyboardNavigateLeft))
			{
				num = -1;
			}
			else if (!string.IsNullOrEmpty(m_keyboardNavigateRight) && ZInput.GetButtonDown(m_keyboardNavigateRight))
			{
				num = 1;
			}
		}
		if (m_tabKeyInput && ZInput.GetKeyDown(KeyCode.Tab))
		{
			num = 1;
		}
		if (num == 0)
		{
			return;
		}
		int num2 = m_selected + num;
		if (m_cycling)
		{
			if (num2 < 0)
			{
				num2 = m_tabs.Count - 1;
			}
			else if (num2 > m_tabs.Count - 1)
			{
				num2 = 0;
			}
			if (!m_tabs[num2].m_button)
			{
				for (int i = num2 + num; i <= m_tabs.Count && i != num2; i += num)
				{
					if (i >= m_tabs.Count)
					{
						i = 0;
					}
					if ((bool)m_tabs[i].m_button)
					{
						SetActiveTab(i);
						break;
					}
				}
			}
			else
			{
				SetActiveTab(num2);
			}
		}
		else
		{
			SetActiveTab(Math.Max(0, Math.Min(m_tabs.Count - 1, num2)));
		}
	}

	private void OnClick(Button button)
	{
		SetActiveTab(button);
	}

	private void SetActiveTab(Button button)
	{
		for (int i = 0; i < m_tabs.Count; i++)
		{
			if (!(m_tabs[i].m_button == null) && !(m_tabs[i].m_button != button))
			{
				SetActiveTab(i);
				break;
			}
		}
	}

	public void SetActiveTab(int index, bool forceSelect = false, bool invokeOnClick = true)
	{
		m_activeTabEverSet = true;
		if (!forceSelect && m_selected == index)
		{
			return;
		}
		m_selected = (m_tabs[index].m_button ? index : m_selected);
		for (int i = 0; i < m_tabs.Count; i++)
		{
			Tab tab = m_tabs[i];
			bool flag = i == index;
			if (tab.m_page != null)
			{
				tab.m_page.gameObject.SetActive(flag);
			}
			if ((bool)tab.m_button)
			{
				tab.m_button.interactable = !flag;
				Transform transform = tab.m_button.gameObject.transform.Find("Selected");
				if ((bool)transform)
				{
					transform.gameObject.SetActive(i == m_selected);
				}
				if (flag && invokeOnClick)
				{
					tab.m_onClick?.Invoke();
				}
			}
		}
		if (ZInput.IsGamepadActive())
		{
			m_setActiveTabEffects?.Create((Player.m_localPlayer != null) ? Player.m_localPlayer.transform.position : Vector3.zero, Quaternion.identity);
		}
		this.ActiveTabChanged?.Invoke(index);
	}

	public void SetActiveTabWithoutInvokingOnClick(int index)
	{
		m_selected = (m_tabs[index].m_button ? index : m_selected);
		for (int i = 0; i < m_tabs.Count; i++)
		{
			Tab tab = m_tabs[i];
			bool flag = i == index;
			if (tab.m_page != null)
			{
				tab.m_page.gameObject.SetActive(flag);
			}
			if ((bool)tab.m_button)
			{
				tab.m_button.interactable = !flag;
				Transform transform = tab.m_button.gameObject.transform.Find("Selected");
				if ((bool)transform)
				{
					transform.gameObject.SetActive(i == m_selected);
				}
			}
		}
		this.ActiveTabChanged?.Invoke(index);
	}

	public int GetActiveTab()
	{
		return m_selected;
	}
}
