using System;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public class AudioSettings : MonoBehaviour, ISettingsTab
{
	[Header("Audio")]
	[SerializeField]
	private Slider m_volumeSlider;

	[SerializeField]
	private TMP_Text m_volumeText;

	[SerializeField]
	private Slider m_sfxVolumeSlider;

	[SerializeField]
	private TMP_Text m_sfxVolumeText;

	[SerializeField]
	private Slider m_musicVolumeSlider;

	[SerializeField]
	private TMP_Text m_musicVolumeText;

	[SerializeField]
	private Toggle m_continousMusic;

	[SerializeField]
	private AudioMixer m_masterMixer;

	private float m_oldVolume;

	private float m_oldSfxVolume;

	private float m_oldMusicVolume;

	public event Action<string, int> SharedSettingChanged;

	public void OnTabOpen(Button backButton, Button okButton)
	{
		GuiUtils.SetNavigationDown(m_continousMusic, backButton);
		GuiUtils.SetNavigationUp(backButton, m_continousMusic);
		GuiUtils.SetNavigationUp(okButton, m_continousMusic);
	}

	public void Initialize()
	{
		m_volumeSlider.value = PlatformPrefs.GetFloat("MasterVolume", AudioListener.volume);
		m_sfxVolumeSlider.value = PlatformPrefs.GetFloat("SfxVolume", 1f);
		m_musicVolumeSlider.value = PlatformPrefs.GetFloat("MusicVolume", 1f);
		m_continousMusic.isOn = PlatformPrefs.GetInt("ContinousMusic", 1) == 1;
		m_oldVolume = m_volumeSlider.value;
		m_oldSfxVolume = m_sfxVolumeSlider.value;
		m_oldMusicVolume = m_musicVolumeSlider.value;
	}

	public void OnOkAsync(OkActionCompletedHandler okActionCompletedCallback)
	{
		PlatformPrefs.SetFloat("MasterVolume", m_volumeSlider.value);
		PlatformPrefs.SetFloat("MusicVolume", m_musicVolumeSlider.value);
		PlatformPrefs.SetFloat("SfxVolume", m_sfxVolumeSlider.value);
		PlatformPrefs.SetInt("ContinousMusic", m_continousMusic.isOn ? 1 : 0);
		Settings.ContinousMusic = m_continousMusic.isOn;
		okActionCompletedCallback?.Invoke();
	}

	public void OnBack()
	{
		AudioListener.volume = m_oldVolume;
		MusicMan.m_masterMusicVolume = m_oldMusicVolume;
		AudioMan.SetSFXVolume(m_oldSfxVolume);
	}

	public void OnAudioChanged()
	{
		AudioListener.volume = m_volumeSlider.value;
		m_volumeText.text = Mathf.Round(AudioListener.volume * 100f) + "%";
		MusicMan.m_masterMusicVolume = m_musicVolumeSlider.value;
		m_musicVolumeText.text = Mathf.Round(MusicMan.m_masterMusicVolume * 100f) + "%";
		AudioMan.SetSFXVolume(m_sfxVolumeSlider.value);
		m_sfxVolumeText.text = Mathf.Round(AudioMan.GetSFXVolume() * 100f) + "%";
	}
}
