using System;
using System.Collections.Generic;
using Splatform;
using UnityEngine;

public class PresentManager
{
	public const int m_backgroundFPS = 30;

	public const int m_menuFPS = 60;

	public const int m_minimumFPSLimit = 30;

	public const int m_maximumFPSLimit = 360;

	private const float c_FrameRateMarginPercent = 0.002f;

	private int? m_actualFrameRateLimit;

	private int? m_actualVSyncCount;

	private int m_setTargetFrameRate = -1;

	private bool m_setVSyncEnabled;

	private bool m_isXbox;

	private int m_framesForVulkanCrashWorkaround = 1;

	private Resolution m_currentResolution;

	public event Action ResolutionOrRefreshRateChanged;

	public void SetTargetFrameRate(int value)
	{
		m_setTargetFrameRate = ((value < 30 || value > 360) ? (-1) : value);
	}

	public void SetVSyncEnabled(bool value)
	{
		m_setVSyncEnabled = value;
	}

	public bool IsTargetFrameRateEvenlyDivisibleByASupportedFrameRate(uint targetFrameRate)
	{
		foreach (RefreshRate supportedRefreshRate in GetSupportedRefreshRates())
		{
			ZLog.Log($"Checking refresh rate {supportedRefreshRate}");
			if (IsEvenlyDivisible(supportedRefreshRate, targetFrameRate, 0.002f))
			{
				ZLog.Log($"Target frame rate {targetFrameRate} was determined to evenly divisible by refresh rate {supportedRefreshRate} (margin: {0.002f})");
				return true;
			}
		}
		return false;
	}

	public void Update()
	{
		InvokeEventIfResolutionChanged();
		UpdatePresentSettings();
		UpdateTerminalTest();
	}

	public static Resolution GetCurrentPresentResolution()
	{
		Resolution currentResolution = Screen.currentResolution;
		currentResolution.width = Screen.width;
		currentResolution.height = Screen.height;
		return currentResolution;
	}

	private void InvokeEventIfResolutionChanged()
	{
		Resolution currentPresentResolution = GetCurrentPresentResolution();
		if (!m_currentResolution.Equals(currentPresentResolution))
		{
			m_currentResolution = currentPresentResolution;
			this.ResolutionOrRefreshRateChanged?.Invoke();
		}
	}

	private void UpdatePresentSettings()
	{
		if (m_isXbox)
		{
			UpdatePresentSettingsForXbox();
		}
		else
		{
			UpdatePresentSettingsWithVsyncCount();
		}
	}

	private IReadOnlyCollection<RefreshRate> GetSupportedRefreshRates()
	{
		HashSet<RefreshRate> hashSet = new HashSet<RefreshRate>();
		Resolution[] resolutions = Screen.resolutions;
		for (int i = 0; i < resolutions.Length; i++)
		{
			hashSet.Add(resolutions[i].refreshRateRatio);
		}
		return hashSet;
	}

	private int GetCurrentFrameRateTarget()
	{
		int num = ((m_setTargetFrameRate < 0) ? int.MaxValue : m_setTargetFrameRate);
		if (Settings.ReduceBackgroundUsage && !Application.isFocused)
		{
			num = Mathf.Min(num, 30);
		}
		if (!Game.instance || Game.IsPaused())
		{
			num = Mathf.Min(num, 60);
		}
		if (num >= int.MaxValue)
		{
			num = -1;
		}
		return num;
	}

	private void UpdatePresentSettingsForXbox()
	{
		int currentFrameRateTarget = GetCurrentFrameRateTarget();
		if (m_actualFrameRateLimit != currentFrameRateTarget)
		{
			m_actualFrameRateLimit = currentFrameRateTarget;
			RefreshRate preferredRefreshRate = new RefreshRate
			{
				numerator = (uint)m_actualFrameRateLimit.Value,
				denominator = 1u
			};
			Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, Screen.fullScreenMode, preferredRefreshRate);
		}
	}

	private void UpdatePresentSettingsWithVsyncCount()
	{
		if (m_framesForVulkanCrashWorkaround > 0)
		{
			m_framesForVulkanCrashWorkaround--;
			return;
		}
		int currentFrameRateTarget = GetCurrentFrameRateTarget();
		if (m_setVSyncEnabled)
		{
			if (m_actualFrameRateLimit != -1)
			{
				m_actualFrameRateLimit = -1;
				Application.targetFrameRate = -1;
			}
			int num = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
			int num2 = Mathf.Max(1, num / currentFrameRateTarget);
			if (m_actualVSyncCount != num2)
			{
				m_actualVSyncCount = num2;
				QualitySettings.vSyncCount = num2;
			}
		}
		else
		{
			if (m_actualFrameRateLimit != currentFrameRateTarget)
			{
				m_actualFrameRateLimit = currentFrameRateTarget;
				Application.targetFrameRate = currentFrameRateTarget;
			}
			if (m_actualVSyncCount != 0)
			{
				m_actualVSyncCount = 0;
				QualitySettings.vSyncCount = 0;
			}
		}
	}

	private bool IsEvenlyDivisible(int numerator, int denominator)
	{
		return numerator / denominator * denominator == numerator;
	}

	private bool IsEvenlyDivisible(RefreshRate numerator, uint denominator, float percentMargin)
	{
		float num = (float)numerator.value / (float)denominator;
		float num2 = Mathf.Round(num);
		float num3 = num / num2 - 1f;
		if (num3 <= percentMargin)
		{
			return num3 >= 0f - percentMargin;
		}
		return false;
	}

	private void UpdateTerminalTest()
	{
		if (Terminal.m_showTests)
		{
			Terminal.m_testList["fps limit"] = Application.targetFrameRate.ToString();
		}
	}

	internal void Initialize()
	{
		m_currentResolution = GetCurrentPresentResolution();
		EnsureCurrentResolutionIsValid();
		if (PlatformManager.DistributionPlatform != null && PlatformManager.DistributionPlatform.HardwareInfoProvider != null)
		{
			HardwareInfo hardwareInfo = PlatformManager.DistributionPlatform.HardwareInfoProvider.HardwareInfo;
			m_isXbox = hardwareInfo.m_category == HardwareCategory.Console && hardwareInfo.m_brand == "Xbox";
		}
	}

	private void EnsureCurrentResolutionIsValid()
	{
		if (Settings.IsSteamRunningOnSteamDeck())
		{
			ResetToHighestResolutionIfCurrentIsInvalid();
		}
	}

	private void ResetToHighestResolutionIfCurrentIsInvalid()
	{
		Resolution[] resolutions = Screen.resolutions;
		if (resolutions.Length == 0)
		{
			return;
		}
		for (int i = 0; i < resolutions.Length; i++)
		{
			ref Resolution reference = ref resolutions[i];
			if (m_currentResolution.width == reference.width && m_currentResolution.height == reference.height && m_currentResolution.refreshRateRatio.value == reference.refreshRateRatio.value)
			{
				return;
			}
		}
		Resolution resolution = resolutions[0];
		for (int j = 1; j < resolutions.Length; j++)
		{
			ref Resolution reference2 = ref resolutions[j];
			int num = resolution.width * resolution.height;
			int num2 = reference2.width * reference2.height;
			if (num >= num2)
			{
				if (num > num2)
				{
					continue;
				}
				double value = resolution.refreshRateRatio.value;
				double value2 = reference2.refreshRateRatio.value;
				if (value >= value2)
				{
					continue;
				}
			}
			resolution = reference2;
		}
		Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode, resolution.refreshRateRatio);
	}
}
