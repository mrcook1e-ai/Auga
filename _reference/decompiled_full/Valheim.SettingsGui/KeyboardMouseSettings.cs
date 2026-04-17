using System;
using System.Collections;
using System.Collections.Generic;
using GUIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public class KeyboardMouseSettings : MonoBehaviour, ISettingsTab
{
	[SerializeField]
	private UIGroupHandler m_groupHandler;

	[Header("Controls")]
	[SerializeField]
	private Slider m_mouseSensitivitySlider;

	[SerializeField]
	private TMP_Text m_mouseSensitivityText;

	[SerializeField]
	private Toggle m_invertMouse;

	[SerializeField]
	private Toggle m_quickPieceSelect;

	[SerializeField]
	private GameObject m_bindDialog;

	[SerializeField]
	private List<KeySetting> m_keys = new List<KeySetting>();

	[SerializeField]
	private Button m_consoleKeyButton;

	[SerializeField]
	private Button m_bottomLeftKeyButton;

	[SerializeField]
	private Button m_bottomRightKeyButton;

	[SerializeField]
	private int m_keyRows = 13;

	[SerializeField]
	private int m_keyCols = 2;

	private GameObject m_selectedGameObject;

	private ScrollRectEnsureVisible m_scrollRectVisibilityManager;

	private float m_blockInputDelay;

	private KeySetting m_selectedKey;

	private static int m_mouseSensModifier = 1;

	public event Action<string, int> SharedSettingChanged;

	public void OnTabOpen(Button backButton, Button okButton)
	{
		Button button = ((m_consoleKeyButton.transform.parent.localScale.x > 0f) ? m_consoleKeyButton.GetComponentInChildren<Button>() : m_bottomLeftKeyButton.GetComponentInChildren<Button>());
		GuiUtils.SetNavigationDown(button, backButton);
		GuiUtils.SetNavigationUp(backButton, button);
		button = m_bottomRightKeyButton.GetComponentInChildren<Button>();
		GuiUtils.SetNavigationDown(button, okButton);
		GuiUtils.SetNavigationUp(okButton, button);
	}

	public void Initialize()
	{
		PlayerController.m_mouseSens = PlatformPrefs.GetFloat("MouseSensitivity", PlayerController.m_mouseSens);
		m_mouseSensitivitySlider.value = PlatformPrefs.GetFloat("MouseSensitivity", PlayerController.m_mouseSens) / (float)m_mouseSensModifier;
		PlayerController.m_invertMouse = PlatformPrefs.GetInt("InvertMouse") == 1;
		m_invertMouse.isOn = PlayerController.m_invertMouse;
		m_quickPieceSelect.isOn = PlatformPrefs.GetInt("QuickPieceSelect") == 1;
		OnMouseSensitivityChanged();
		m_bindDialog.SetActive(value: false);
		SetupKeys();
		m_scrollRectVisibilityManager = GetComponentInChildren<ScrollRectEnsureVisible>();
		m_selectedGameObject = EventSystem.current.currentSelectedGameObject;
		if (m_consoleKeyButton.transform.parent.localScale.x > 0f)
		{
			SetConsoleEnabled(enabled: true);
		}
	}

	public void OnBack()
	{
		ZInput.instance.Load();
	}

	public void OnOkAsync(OkActionCompletedHandler okActionCompletedCallback)
	{
		PlatformPrefs.SetFloat("MouseSensitivity", m_mouseSensitivitySlider.value * (float)m_mouseSensModifier);
		PlatformPrefs.SetInt("InvertMouse", m_invertMouse.isOn ? 1 : 0);
		PlatformPrefs.SetInt("QuickPieceSelect", m_quickPieceSelect.isOn ? 1 : 0);
		PlayerController.m_mouseSens = m_mouseSensitivitySlider.value * (float)m_mouseSensModifier;
		PlayerController.m_invertMouse = m_invertMouse.isOn;
		okActionCompletedCallback?.Invoke();
	}

	private void Update()
	{
		if (!m_bindDialog.activeSelf)
		{
			if (ZInput.IsGamepadActive() && !(EventSystem.current.currentSelectedGameObject == m_selectedGameObject))
			{
				m_selectedGameObject = EventSystem.current.currentSelectedGameObject;
				m_scrollRectVisibilityManager?.CenterOnItem(m_selectedGameObject.transform as RectTransform);
			}
			return;
		}
		m_blockInputDelay -= Time.unscaledDeltaTime;
		if (!(m_blockInputDelay >= 0f))
		{
			if (InvalidKeyBind())
			{
				m_bindDialog.SetActive(value: false);
				InvalidKeybindPopup();
			}
			else if (!ZInput.s_IsRebindActive && m_bindDialog.activeSelf)
			{
				m_bindDialog.SetActive(value: false);
				UpdateBindings();
				StartCoroutine(DelayedKeyEnable());
			}
		}
	}

	private bool InvalidKeyBind()
	{
		KeyCode[] blockedButtons = m_selectedKey.m_blockedButtons;
		for (int i = 0; i < blockedButtons.Length; i++)
		{
			if (ZInput.GetKeyDown(blockedButtons[i]))
			{
				return true;
			}
		}
		return false;
	}

	private void InvalidKeybindPopup()
	{
		string text = "$invalid_keybind_text";
		UnifiedPopup.Push(new WarningPopup("$invalid_keybind_header", text, delegate
		{
			UnifiedPopup.Pop();
			StartCoroutine(DelayedKeyEnable());
		}));
	}

	private IEnumerator DelayedKeyEnable()
	{
		if (!(base.gameObject == null))
		{
			yield return null;
			EnableKeys(enable: true);
			m_groupHandler.m_defaultElement = m_mouseSensitivitySlider.gameObject;
			Settings.instance.BlockNavigation(block: false);
		}
	}

	private void OnDestroy()
	{
		foreach (KeySetting key in m_keys)
		{
			key.m_keyTransform.GetComponentInChildren<GuiButton>().onClick.RemoveAllListeners();
		}
		m_keys.Clear();
	}

	public void OnMouseSensitivityChanged()
	{
		m_mouseSensitivityText.text = Mathf.Round(m_mouseSensitivitySlider.value * 100f) + "%";
	}

	public void SetConsoleEnabled(bool enabled)
	{
		int num = (enabled ? 1 : 0);
		m_consoleKeyButton.transform.parent.transform.localScale = new Vector3(num, num, 1f);
		if (enabled)
		{
			GuiUtils.SetNavigationUp(m_consoleKeyButton, m_bottomLeftKeyButton);
			GuiUtils.SetNavigationLeft(m_consoleKeyButton, null);
			GuiUtils.SetNavigationDown(m_bottomLeftKeyButton, m_consoleKeyButton);
		}
	}

	private void SetupKeys()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (KeySetting key in m_keys)
		{
			GuiButton componentInChildren = key.m_keyTransform.GetComponentInChildren<GuiButton>();
			componentInChildren.onClick.AddListener(delegate
			{
				OpenBindDialog(key);
			});
			if (num < m_keyRows - 1)
			{
				num3 = num2 * m_keyRows + num + 1;
				if (num3 < m_keys.Count)
				{
					GuiButton componentInChildren2 = m_keys[num3].m_keyTransform.GetComponentInChildren<GuiButton>();
					GuiUtils.SetNavigationDown(componentInChildren, componentInChildren2);
				}
			}
			if (num > 0)
			{
				num3 = num2 * m_keyRows + num - 1;
				GuiButton componentInChildren2 = m_keys[num3].m_keyTransform.GetComponentInChildren<GuiButton>();
				GuiUtils.SetNavigationUp(componentInChildren, componentInChildren2);
			}
			if (num2 > 0)
			{
				num3 = (num2 - 1) * m_keyRows + num;
				GuiButton componentInChildren2 = m_keys[num3].m_keyTransform.GetComponentInChildren<GuiButton>();
				GuiUtils.SetNavigationLeft(componentInChildren, componentInChildren2);
			}
			if (num2 < m_keyCols - 1)
			{
				num3 = (num2 + 1) * m_keyRows + num;
				if (num3 < m_keys.Count)
				{
					GuiButton componentInChildren2 = m_keys[num3].m_keyTransform.GetComponentInChildren<GuiButton>();
					GuiUtils.SetNavigationRight(componentInChildren, componentInChildren2);
				}
			}
			num++;
			if (num % m_keyRows == 0)
			{
				num = 0;
				num2++;
			}
		}
		UpdateBindings();
	}

	private void EnableKeys(bool enable)
	{
		foreach (KeySetting key in m_keys)
		{
			key.m_keyTransform.GetComponentInChildren<GuiButton>().interactable = enable;
		}
	}

	private void OpenBindDialog(KeySetting key)
	{
		ZLog.Log("Binding key " + key.m_keyName);
		m_selectedKey = key;
		Settings.instance.BlockNavigation(block: true);
		m_bindDialog.SetActive(value: true);
		m_blockInputDelay = 0.2f;
		m_groupHandler.m_defaultElement = EventSystem.current.currentSelectedGameObject;
		EventSystem.current.SetSelectedGameObject(m_bindDialog.gameObject);
		EnableKeys(enable: false);
		ZInput.instance.StartBindKey(key.m_keyName);
	}

	private void UpdateBindings()
	{
		foreach (KeySetting key in m_keys)
		{
			key.m_keyTransform.GetComponentInChildren<Button>().GetComponentInChildren<TMP_Text>().text = Localization.instance.GetBoundKeyString(key.m_keyName, emptyStringOnMissing: true);
		}
	}

	public void ResetBindings()
	{
		ZInput.instance.ResetToDefault();
		UpdateBindings();
	}

	public static void SetPlatformSpecificFirstTimeSettings()
	{
		if (!PlatformPrefs.HasKey("MouseSensitivity"))
		{
			PlatformPrefs.SetFloat("MouseSensitivity", m_mouseSensModifier);
		}
	}
}
