using System;
using System.Collections.Generic;
using System.Linq;
using GUIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public class GamepadSettings : MonoBehaviour, ISettingsTab
{
	[SerializeField]
	private UIGroupHandler m_groupHandler;

	[Header("Gamepad")]
	[SerializeField]
	private Toggle m_gamepadEnabled;

	[SerializeField]
	private Slider m_gamepadSensitivitySlider;

	[SerializeField]
	private TMP_Text m_cameraSensitivityText;

	[SerializeField]
	private Button m_leftLayoutButton;

	[SerializeField]
	private Button m_rightLayoutButton;

	[SerializeField]
	private GamepadMapController m_gamepadMapController;

	[SerializeField]
	private TMP_Text m_layoutText;

	[SerializeField]
	private Toggle m_swapTriggers;

	[SerializeField]
	private GuiDropdown m_glyphs;

	[SerializeField]
	private Toggle m_invertCameraY;

	[SerializeField]
	private Toggle m_invertCameraX;

	[SerializeField]
	private GameObject m_emptyToggleShift;

	private const string GlyphsXbox = "Xbox";

	private const string GlyphsPlaystation = "Playstation";

	private List<string> m_glyphOptions = new List<string> { "Xbox", "Playstation" };

	private GamepadGlyphs m_initialGlyph;

	private InputLayout m_initialLayout;

	private InputLayout m_currentLayout;

	private bool m_initialAlternativeGlyphs;

	private bool m_initialSwapTriggers;

	public event Action<string, int> SharedSettingChanged;

	public void OnTabOpen(Button backButton, Button okButton)
	{
		GuiUtils.SetNavigationDown(m_gamepadSensitivitySlider, backButton);
		GuiUtils.SetNavigationUp(backButton, m_gamepadSensitivitySlider);
		GuiUtils.SetNavigationUp(okButton, m_gamepadSensitivitySlider);
	}

	public void Initialize()
	{
		PlayerController.m_gamepadSens = PlatformPrefs.GetFloat("GamepadSensitivity", PlayerController.m_gamepadSens);
		PlayerController.m_invertCameraY = PlatformPrefs.GetInt("InvertCameraY", PlatformPrefs.GetInt("InvertMouse")) == 1;
		PlayerController.m_invertCameraX = PlatformPrefs.GetInt("InvertCameraX") == 1;
		m_initialLayout = ZInput.InputLayout;
		m_currentLayout = m_initialLayout;
		if (PlatformPrefs.GetInt("AltGlyphs", 99) != 99)
		{
			m_initialGlyph = ((PlatformPrefs.GetInt("AltGlyphs") == 1) ? GamepadGlyphs.Playstation : GamepadGlyphs.Auto);
			PlatformPrefs.DeleteKey("AltGlyphs");
		}
		else
		{
			string[] names = Enum.GetNames(typeof(GamepadGlyphs));
			m_initialGlyph = (GamepadGlyphs)Array.IndexOf(names, PlatformPrefs.GetString("gamepad_glyphs", "Auto"));
		}
		ZInput.CurrentGlyph = m_initialGlyph;
		m_initialSwapTriggers = ZInput.SwapTriggers;
		m_gamepadEnabled.isOn = ZInput.IsGamepadEnabled();
		m_gamepadSensitivitySlider.value = PlayerController.m_gamepadSens;
		m_invertCameraY.isOn = PlayerController.m_invertCameraY;
		m_invertCameraX.isOn = PlayerController.m_invertCameraX;
		m_swapTriggers.isOn = m_initialSwapTriggers;
		m_glyphs.ClearOptions();
		m_glyphOptions = Enum.GetNames(typeof(GamepadGlyphs)).ToList();
		m_glyphs.AddOptions(m_glyphOptions);
		m_glyphs.value = m_glyphOptions.IndexOf(m_initialGlyph.ToString());
		m_glyphs.onValueChanged.RemoveListener(OnGamepadGlyphChanged);
		m_glyphs.onValueChanged.AddListener(OnGamepadGlyphChanged);
		m_gamepadMapController.Show(m_initialLayout);
		OnLayoutChanged();
		OnZInputLayoutChanged();
	}

	public void OnBack()
	{
		m_glyphs.value = Enum.GetNames(typeof(GamepadGlyphs)).ToList().IndexOf(m_initialGlyph.ToString());
		m_currentLayout = m_initialLayout;
		m_swapTriggers.isOn = m_initialSwapTriggers;
		OnLayoutChanged();
	}

	public void OnOkAsync(OkActionCompletedHandler okActionCompletedCallback)
	{
		PlatformPrefs.SetFloat("GamepadSensitivity", m_gamepadSensitivitySlider.value);
		PlatformPrefs.SetInt("InvertCameraY", m_invertCameraY.isOn ? 1 : 0);
		PlatformPrefs.SetInt("InvertCameraX", m_invertCameraX.isOn ? 1 : 0);
		PlatformPrefs.SetInt("SwapTriggers", m_swapTriggers.isOn ? 1 : 0);
		PlatformPrefs.SetString("gamepad_glyphs", m_glyphs.options[m_glyphs.value].text);
		PlayerController.m_gamepadSens = m_gamepadSensitivitySlider.value;
		PlayerController.m_invertCameraY = m_invertCameraY.isOn;
		PlayerController.m_invertCameraX = m_invertCameraX.isOn;
		ZInput.SwapTriggers = m_swapTriggers.isOn;
		ZInput.SetGamepadEnabled(m_gamepadEnabled.isOn);
		okActionCompletedCallback?.Invoke();
	}

	public void OnGamepadSensitivityChanged()
	{
		m_cameraSensitivityText.text = Mathf.Round(m_gamepadSensitivitySlider.value * 100f) + "%";
	}

	public void OnLayoutLeft()
	{
		m_currentLayout = GamepadMapController.PrevLayout(m_gamepadMapController.VisibleLayout);
		OnLayoutChanged();
	}

	public void OnLayoutRight()
	{
		m_currentLayout = GamepadMapController.NextLayout(m_gamepadMapController.VisibleLayout);
		OnLayoutChanged();
	}

	public void OnLayoutChanged()
	{
		if (ZInput.instance != null)
		{
			ZInput.SwapTriggers = m_swapTriggers.isOn;
			ZInput.instance.ChangeLayout(m_currentLayout);
		}
	}

	public void OnGamepadGlyphChanged(int newValue)
	{
		ZInput.CurrentGlyph = (GamepadGlyphs)Enum.Parse(typeof(GamepadGlyphs), m_glyphs.options[m_glyphs.value].text);
		OnLayoutChanged();
	}

	private void OnZInputLayoutChanged()
	{
		m_gamepadMapController.Show(m_currentLayout, GamepadMapController.GetType(ZInput.CurrentGlyph, Settings.IsSteamRunningOnSteamDeck()));
		m_layoutText.text = Localization.instance.Localize(GamepadMapController.GetLayoutStringId(m_currentLayout));
	}

	private void OnEnable()
	{
		ZInput.OnInputLayoutChanged += OnZInputLayoutChanged;
	}

	private void OnDisable()
	{
		ZInput.OnInputLayoutChanged -= OnZInputLayoutChanged;
	}
}
