using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public class AccessibilitySettings : MonoBehaviour, ISettingsTab
{
	private float m_oldGuiScale;

	[Header("Accessibility")]
	[SerializeField]
	private Slider m_guiScaleSlider;

	[SerializeField]
	private TMP_Text m_guiScaleText;

	[SerializeField]
	private Toggle m_toggleRun;

	[SerializeField]
	private Toggle m_immersiveCamera;

	[SerializeField]
	private Toggle m_cameraShake;

	[SerializeField]
	private Toggle m_reduceFlashingLights;

	[SerializeField]
	private Toggle m_motionblurToggle;

	[SerializeField]
	private Toggle m_depthOfFieldToggle;

	[SerializeField]
	private Toggle m_closedCaptionsToggle;

	[SerializeField]
	private Toggle m_soundIndicatorsToggle;

	public event Action<string, int> SharedSettingChanged;

	public void OnTabOpen(Button backButton, Button okButton)
	{
		GuiUtils.SetNavigationDown(m_motionblurToggle, backButton);
		GuiUtils.SetNavigationUp(backButton, m_motionblurToggle);
		GuiUtils.SetNavigationUp(okButton, m_depthOfFieldToggle);
		GuiUtils.SetNavigationDown(m_depthOfFieldToggle, okButton);
	}

	public void Initialize()
	{
		m_oldGuiScale = PlatformPrefs.GetFloat("GuiScale", 1f);
		m_guiScaleSlider.value = m_oldGuiScale * 100f;
		m_toggleRun.isOn = PlatformPrefs.GetInt("ToggleRun", ZInput.IsGamepadActive() ? 1 : 0) == 1;
		m_immersiveCamera.isOn = PlatformPrefs.GetInt("ShipCameraTilt", 1) == 1;
		m_cameraShake.isOn = PlatformPrefs.GetInt("CameraShake", 1) == 1;
		m_reduceFlashingLights.isOn = PlatformPrefs.GetInt("ReduceFlashingLights") == 1;
		m_motionblurToggle.isOn = PlatformPrefs.GetInt("MotionBlur", 1) == 1;
		m_depthOfFieldToggle.isOn = PlatformPrefs.GetInt("DOF", 1) == 1;
		m_closedCaptionsToggle.isOn = PlatformPrefs.GetInt("ClosedCaptions") == 1;
		m_soundIndicatorsToggle.isOn = PlatformPrefs.GetInt("DirectionalSoundIndicators") == 1;
		Settings.ReduceFlashingLights = m_reduceFlashingLights.isOn;
		Settings.DirectionalSoundIndicators = m_soundIndicatorsToggle.isOn;
		Settings.ClosedCaptions = m_closedCaptionsToggle.isOn;
	}

	public void OnOkAsync(OkActionCompletedHandler okActionCompletedCallback)
	{
		PlatformPrefs.SetFloat("GuiScale", m_guiScaleSlider.value / 100f);
		PlatformPrefs.SetInt("ToggleRun", m_toggleRun.isOn ? 1 : 0);
		PlatformPrefs.SetInt("ShipCameraTilt", m_immersiveCamera.isOn ? 1 : 0);
		PlatformPrefs.SetInt("CameraShake", m_cameraShake.isOn ? 1 : 0);
		PlatformPrefs.SetInt("ReduceFlashingLights", m_reduceFlashingLights.isOn ? 1 : 0);
		PlatformPrefs.SetInt("ClosedCaptions", m_closedCaptionsToggle.isOn ? 1 : 0);
		PlatformPrefs.SetInt("DirectionalSoundIndicators", m_soundIndicatorsToggle.isOn ? 1 : 0);
		Settings.ReduceFlashingLights = m_reduceFlashingLights.isOn;
		Settings.ClosedCaptions = m_closedCaptionsToggle.isOn;
		Settings.DirectionalSoundIndicators = m_soundIndicatorsToggle.isOn;
		okActionCompletedCallback?.Invoke();
	}

	public void OnBack()
	{
		GuiScaler.SetScale(m_oldGuiScale);
	}

	public void OnSharedSettingChanged(string setting, int value)
	{
		Toggle toggle;
		switch (setting)
		{
		default:
			return;
		case "MotionBlur":
			toggle = m_motionblurToggle;
			break;
		case "DepthOfField":
			toggle = m_depthOfFieldToggle;
			break;
		case "ToggleRun":
			toggle = m_toggleRun;
			break;
		case "ClosedCaptions":
			toggle = m_closedCaptionsToggle;
			break;
		case "DirectionalSoundIndicators":
			toggle = m_soundIndicatorsToggle;
			break;
		}
		bool flag = value == 1;
		if (toggle.isOn != flag)
		{
			toggle.isOn = flag;
		}
	}

	public void OnUIScaleChanged()
	{
		m_guiScaleText.text = m_guiScaleSlider.value + "%";
		GuiScaler.SetScale(m_guiScaleSlider.value / 100f);
	}

	public void OnMotionBlurChanged()
	{
		this.SharedSettingChanged?.Invoke("MotionBlur", m_motionblurToggle.isOn ? 1 : 0);
	}

	public void OnDepthOfFieldChanged()
	{
		this.SharedSettingChanged?.Invoke("DepthOfField", m_depthOfFieldToggle.isOn ? 1 : 0);
	}

	public void OnToggleRunChanged()
	{
		this.SharedSettingChanged?.Invoke("ToggleRun", m_toggleRun.isOn ? 1 : 0);
	}

	public void OnClosedCaptionsChanged()
	{
		this.SharedSettingChanged?.Invoke("ClosedCaptions", m_closedCaptionsToggle.isOn ? 1 : 0);
	}

	public void OnDirectionalSoundIndicatorsChanged()
	{
		this.SharedSettingChanged?.Invoke("DirectionalSoundIndicators", m_soundIndicatorsToggle.isOn ? 1 : 0);
	}
}
