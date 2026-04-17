using System;
using System.Collections.Generic;
using GUIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public class GraphicsSettings : MonoBehaviour, ISettingsTab
{
	[SerializeField]
	private UIGroupHandler m_groupHandler;

	[SerializeField]
	private VerticalLayoutGroup m_verticalLayoutGroup;

	[SerializeField]
	private GridLayoutGroup m_toggleGridLayoutGroup;

	[SerializeField]
	private TMP_Text m_devBuildSettingsText;

	[SerializeField]
	private GameObject m_devBuildSettingFramePrefab;

	[SerializeField]
	private TMP_Text m_devGraphicsModeValuesText;

	[SerializeField]
	private TMP_Text m_devPlayerPrefsValuesText;

	[Header("Present settings")]
	[SerializeField]
	private GameObject m_resolutionRoot;

	[SerializeField]
	private GuiDropdown m_resolutionDropdown;

	[SerializeField]
	private GameObject m_upscalingOptionsRoot;

	[SerializeField]
	private GuiDropdown m_renderScaleDropdown;

	[SerializeField]
	private GuiDropdown m_upscalingAlgorithmDropdown;

	[SerializeField]
	private Toggle m_fullscreenToggle;

	[SerializeField]
	private GuiButton m_testResolutionButton;

	[SerializeField]
	private GameObject m_resolutionDialog;

	[SerializeField]
	private GameObject m_resolutionListElement;

	[SerializeField]
	private RectTransform m_resolutionListRoot;

	[SerializeField]
	private Scrollbar m_resolutionListScroll;

	[SerializeField]
	private GameObject m_resolutionSwitchDialog;

	[SerializeField]
	private GuiButton m_resolutionOk;

	[SerializeField]
	private Slider m_fpsLimitSlider;

	[SerializeField]
	private TMP_Text m_fpsLimitText;

	[SerializeField]
	private Toggle m_vsyncToggle;

	[SerializeField]
	private int m_minResWidth = 1280;

	[SerializeField]
	private int m_minResHeight = 720;

	[Header("Graphics presets")]
	[SerializeField]
	private GameObject m_graphicPresetsRoot;

	[SerializeField]
	private TMP_Text m_graphicsMode;

	[SerializeField]
	private Button m_graphicPresetLeft;

	[SerializeField]
	private Button m_graphicPresetRight;

	[SerializeField]
	private TMP_Text m_graphicsModeDescr;

	[Header("Quality settings")]
	[SerializeField]
	private GameObject m_qualitySliderPrefab;

	[SerializeField]
	private GameObject m_qualityTogglePrefab;

	private List<Resolution> m_resolutions = new List<Resolution>();

	private List<Resolution> m_resolutionOptions = new List<Resolution>();

	private ScrollRectEnsureVisible m_dropdownScrollRectEnsureVisible;

	private bool m_resolutionOptionModified;

	private bool m_oldFullscreen;

	private Resolution m_oldResolution;

	private OkActionCompletedHandler m_okActionCompletedCallback;

	private GraphicsSettingsState m_currentSettingsRaw;

	private int m_currentPresetID = 100;

	private bool m_currentPresetModified;

	private List<QualitySliderData> m_qualitySliders = new List<QualitySliderData>(Enum.GetValues(typeof(GraphicsSettingInt)).Length);

	private List<QualityToggleData> m_qualityToggles = new List<QualityToggleData>(Enum.GetValues(typeof(GraphicsSettingBool)).Length);

	private List<Slider> m_dynamicQualitySliders = new List<Slider>(Enum.GetValues(typeof(GraphicsSettingInt)).Length);

	private List<Toggle> m_dynamicQualityToggles = new List<Toggle>(Enum.GetValues(typeof(GraphicsSettingBool)).Length);

	private List<QualityDropdownData> m_qualityDropdowns = new List<QualityDropdownData>(3);

	private List<GameObject> m_devBuildSettingFrames = new List<GameObject>();

	public event Action<string, int> SharedSettingChanged;

	public void Initialize()
	{
		InitializeUI();
		GraphicsModeConfiguration currentGraphicsModeConfiguration = GraphicsSettingsManager.Instance.GetCurrentGraphicsModeConfiguration();
		GraphicsSettingsState state = m_currentSettingsRaw;
		GraphicsSettingsPreset currentPreset = GetCurrentPreset(currentGraphicsModeConfiguration, excludeUnsupported: true);
		if ((object)currentPreset != null)
		{
			GraphicsSettingsManager.Instance.SetGraphicsSettingsFromPreset(currentGraphicsModeConfiguration, ref state, currentPreset);
		}
		SetSettingsFromState(state);
		SubscribeEvents();
	}

	private void UpdateUI()
	{
		GraphicsModeConfiguration currentGraphicsModeConfiguration = GraphicsSettingsManager.Instance.GetCurrentGraphicsModeConfiguration();
		UpdateAvailableResolutions();
		UpdateSettingAvailability(currentGraphicsModeConfiguration);
		m_currentSettingsRaw = GraphicsSettingsManager.Instance.CurrentSettingsRaw;
		m_currentPresetID = GraphicsSettingsManager.Instance.CurrentPresetID;
	}

	public void Terminate()
	{
		UnsubscribeEvents();
	}

	public void OnTabOpen(Button backButton, Button okButton)
	{
		UpdateNavigation(backButton, okButton);
	}

	public void OnOkAsync(OkActionCompletedHandler okActionCompletedCallback)
	{
		if (okActionCompletedCallback == null)
		{
			throw new ArgumentNullException("okActionCompletedCallback");
		}
		GraphicsModeConfiguration currentGraphicsModeConfiguration = GraphicsSettingsManager.Instance.GetCurrentGraphicsModeConfiguration();
		GraphicsSettingsState settings = m_currentSettingsRaw;
		GraphicsSettingsPreset graphicsSettingsPreset = (m_currentPresetModified ? null : GetCurrentPreset(currentGraphicsModeConfiguration, excludeUnsupported: true));
		if ((object)graphicsSettingsPreset != null)
		{
			GraphicsSettingsManager.Instance.SaveAndApplyGraphicsSettingsWithPreset(ref settings, graphicsSettingsPreset.m_type);
		}
		else
		{
			GraphicsSettingsManager.Instance.SaveAndApplyGraphicsSettingsCustom(ref settings);
		}
		if (GraphicsSettingsManager.CanChangePresentSettings() && ResolutionSettingsChanged())
		{
			m_okActionCompletedCallback = okActionCompletedCallback;
			OnTestResolution();
		}
		else
		{
			okActionCompletedCallback();
		}
	}

	public void OnBack()
	{
	}

	public void OnSharedSettingChanged(string setting, int value)
	{
		GraphicsSettingBool graphicsSettingBool;
		if (!(setting == "MotionBlur"))
		{
			if (!(setting == "DepthOfField"))
			{
				return;
			}
			graphicsSettingBool = GraphicsSettingBool.DepthOfField;
		}
		else
		{
			graphicsSettingBool = GraphicsSettingBool.MotionBlur;
		}
		bool isOn = value == 1;
		int i;
		for (i = 0; i < m_qualityToggles.Count && m_qualityToggles[i].m_setting != graphicsSettingBool; i++)
		{
		}
		m_qualityToggles[i].m_toggle.isOn = isOn;
	}

	private void InitializeUI()
	{
		InitializePresentSettings();
		InitializeUpscalingAlgorithmsDropdown();
		CreateDynamicGraphicsQualitySettings();
		UpdateUI();
	}

	private void InitializePresentSettings()
	{
		InitializePresentSettingsState();
		m_resolutionDialog.SetActive(value: false);
		m_qualitySliders.Add(new QualitySliderData(GraphicsSettingInt.FpsLimit, m_fpsLimitSlider, m_fpsLimitText));
		m_qualityToggles.Add(new QualityToggleData(GraphicsSettingBool.Vsync, m_vsyncToggle));
	}

	private void InitializePresentSettingsState()
	{
		m_fpsLimitSlider.minValue = 30f;
		m_fpsLimitSlider.maxValue = 361f;
	}

	private void SaveRevertableResolutionSetting()
	{
		m_oldFullscreen = Screen.fullScreen;
		m_oldResolution = PresentManager.GetCurrentPresentResolution();
		Debug.Log(m_oldResolution);
	}

	private void InitializeUpscalingAlgorithmsDropdown()
	{
		List<string> list = new List<string>();
		for (int i = 0; Enum.IsDefined(typeof(UpscalingAlgorithm), i); i++)
		{
			list.Add(((UpscalingAlgorithm)i).ToDisplayName());
		}
		m_upscalingAlgorithmDropdown.ClearOptions();
		m_upscalingAlgorithmDropdown.AddOptions(list);
		m_qualityDropdowns.Add(new QualityDropdownData(GraphicsSettingInt.UpscalingAlgorithm, m_upscalingAlgorithmDropdown, null));
	}

	private void CreateDynamicGraphicsQualitySettings()
	{
		GraphicsSettingsManager.Instance.GetCurrentGraphicsModeConfiguration();
		int num = m_qualitySliderPrefab.transform.GetSiblingIndex() + 1;
		for (int i = 0; i < 32; i++)
		{
			GraphicsSettingInt graphicsSettingInt = (GraphicsSettingInt)(1 << i);
			if (Enum.IsDefined(typeof(GraphicsSettingInt), graphicsSettingInt) && graphicsSettingInt.IsShownToUser() && !graphicsSettingInt.IsPresentSetting() && graphicsSettingInt != GraphicsSettingInt.Target3DResolutionVertical && graphicsSettingInt != GraphicsSettingInt.UpscalingAlgorithm)
			{
				GameObject obj = UnityEngine.Object.Instantiate(m_qualitySliderPrefab, m_qualitySliderPrefab.transform.parent);
				obj.transform.SetSiblingIndex(num++);
				Slider component = obj.GetComponent<Slider>();
				TMP_Text component2 = obj.transform.Find("Label").GetComponent<TMP_Text>();
				TMP_Text component3 = obj.transform.Find("Value").GetComponent<TMP_Text>();
				RangeIntInclusive range = graphicsSettingInt.GetRange();
				component.minValue = range.m_minValue;
				component.maxValue = range.m_maxValue;
				component2.text = graphicsSettingInt.ToDisplayName();
				obj.SetActive(value: true);
				QualitySliderData item = new QualitySliderData(graphicsSettingInt, component, component3);
				m_qualitySliders.Add(item);
				m_dynamicQualitySliders.Add(component);
			}
		}
		num = m_qualityTogglePrefab.transform.GetSiblingIndex();
		for (int j = 0; j < 32; j++)
		{
			GraphicsSettingBool graphicsSettingBool = (GraphicsSettingBool)(1 << j);
			if (Enum.IsDefined(typeof(GraphicsSettingBool), graphicsSettingBool) && graphicsSettingBool.IsShownToUser() && !graphicsSettingBool.IsPresentSetting())
			{
				GameObject obj2 = UnityEngine.Object.Instantiate(m_qualityTogglePrefab, m_qualityTogglePrefab.transform.parent);
				obj2.transform.SetSiblingIndex(num++);
				GuiToggle componentInChildren = obj2.GetComponentInChildren<GuiToggle>();
				componentInChildren.transform.Find("Label").GetComponent<TMP_Text>().text = graphicsSettingBool.ToDisplayName();
				obj2.SetActive(value: true);
				QualityToggleData item2 = new QualityToggleData(graphicsSettingBool, componentInChildren);
				m_qualityToggles.Add(item2);
				m_dynamicQualityToggles.Add(componentInChildren);
			}
		}
	}

	private void SubscribeEvents()
	{
		GraphicsSettingsManager.GraphicsSettingsChanged += UpdateUI;
		m_resolutionDropdown.onValueChanged.AddListener(delegate
		{
			OnResolutionOptionModified();
		});
		m_fullscreenToggle.onValueChanged.AddListener(delegate
		{
			OnResolutionOptionModified();
		});
		m_resolutionDropdown.OnExpandedStateChange += OnDropdownExpanded;
		for (int num = 0; num < m_qualityDropdowns.Count; num++)
		{
			QualityDropdownData ui = m_qualityDropdowns[num];
			ui.m_dropdown.OnExpandedStateChange += OnDropdownExpanded;
			ui.m_dropdown.onValueChanged.AddListener(delegate
			{
				OnDropdownValueUpdated(ui.m_setting);
			});
		}
		for (int num2 = 0; num2 < m_qualitySliders.Count; num2++)
		{
			QualitySliderData ui2 = m_qualitySliders[num2];
			ui2.m_slider.onValueChanged.AddListener(delegate
			{
				OnSliderValueUpdated(ui2.m_setting);
			});
		}
		for (int num3 = 0; num3 < m_qualityToggles.Count; num3++)
		{
			QualityToggleData ui3 = m_qualityToggles[num3];
			ui3.m_toggle.onValueChanged.AddListener(delegate
			{
				OnToggleValueUpdated(ui3.m_setting);
			});
		}
	}

	private void UnsubscribeEvents()
	{
		GraphicsSettingsManager.GraphicsSettingsChanged -= UpdateUI;
		m_resolutionDropdown.onValueChanged.RemoveAllListeners();
		m_fullscreenToggle.onValueChanged.RemoveAllListeners();
		m_resolutionDropdown.OnExpandedStateChange -= OnDropdownExpanded;
		for (int i = 0; i < m_qualityDropdowns.Count; i++)
		{
			_ = m_qualityDropdowns[i];
			m_renderScaleDropdown.OnExpandedStateChange -= OnDropdownExpanded;
			m_renderScaleDropdown.onValueChanged.RemoveAllListeners();
		}
		for (int j = 0; j < m_qualitySliders.Count; j++)
		{
			m_qualitySliders[j].m_slider.onValueChanged.RemoveAllListeners();
		}
		for (int k = 0; k < m_qualityToggles.Count; k++)
		{
			m_qualityToggles[k].m_toggle.onValueChanged.RemoveAllListeners();
		}
	}

	private void Update()
	{
		CenterScrollRectOnCurrenlySelected();
	}

	public void OnResSwitchOK()
	{
		m_resolutionSwitchDialog.SetActive(value: false);
		m_resolutionOptionModified = false;
		UpdateTestResolutionButton();
		Settings.instance.BlockNavigation(block: false);
		InvokeCallbackIfSet();
	}

	public void OnResSwitchCancel()
	{
		RevertMode();
		m_resolutionSwitchDialog.SetActive(value: false);
	}

	public void OnTestResolution()
	{
		SaveRevertableResolutionSetting();
		ApplyResolution(m_resolutionOptions[m_resolutionDropdown.value]);
		m_resolutionSwitchDialog.SetActive(value: true);
		if (m_resolutionSwitchDialog.transform.parent == base.transform)
		{
			m_resolutionSwitchDialog.transform.parent = m_resolutionSwitchDialog.transform.parent.parent;
		}
		m_resolutionSwitchDialog.GetComponent<ResolutionSwitchDialogTimedRemoval>().ResCountdownTimer = 5f;
		EventSystem.current.SetSelectedGameObject(m_resolutionOk.gameObject);
		Settings.instance.BlockNavigation(block: true);
	}

	public void OnGraphicPresetRight()
	{
		ChangePreset(1);
	}

	public void OnGraphicPresetLeft()
	{
		ChangePreset(-1);
	}

	private void OnDropdownValueUpdated(GraphicsSettingInt setting)
	{
		int i;
		for (i = 0; i < m_qualityDropdowns.Count && m_qualityDropdowns[i].m_setting != setting; i++)
		{
		}
		QualityDropdownData qualityDropdownData = m_qualityDropdowns[i];
		int value = qualityDropdownData.GetValue(qualityDropdownData.m_dropdown.value);
		ModifySetting(setting, value);
	}

	private void OnSliderValueUpdated(GraphicsSettingInt setting)
	{
		int i;
		for (i = 0; i < m_qualitySliders.Count && m_qualitySliders[i].m_setting != setting; i++)
		{
		}
		QualitySliderData qualitySliderData = m_qualitySliders[i];
		int value = Mathf.RoundToInt(qualitySliderData.m_slider.value);
		qualitySliderData.m_valueText.text = GetDisplayValue(setting, value);
		ModifySetting(setting, value);
	}

	private void OnToggleValueUpdated(GraphicsSettingBool setting)
	{
		int i;
		for (i = 0; i < m_qualityToggles.Count && m_qualityToggles[i].m_setting != setting; i++)
		{
		}
		bool isOn = m_qualityToggles[i].m_toggle.isOn;
		ModifySetting(setting, isOn);
		switch (setting)
		{
		case GraphicsSettingBool.DepthOfField:
			this.SharedSettingChanged?.Invoke("DepthOfField", isOn ? 1 : 0);
			break;
		case GraphicsSettingBool.MotionBlur:
			this.SharedSettingChanged?.Invoke("MotionBlur", isOn ? 1 : 0);
			break;
		}
	}

	private void OnDropdownExpanded(bool expanded)
	{
		Settings.instance.BlockNavigation(expanded);
		if (m_dropdownScrollRectEnsureVisible == null && expanded)
		{
			FindDropdownScrollRect();
		}
	}

	private void UpdateAvailableResolutions()
	{
		PopulateResolutions();
		PopulateRenderScales();
	}

	private void PopulateResolutions()
	{
		UpdateValidResolutions();
		Resolution? resolution = null;
		if (m_resolutionDropdown.value < m_resolutionOptions.Count)
		{
			resolution = m_resolutionOptions[m_resolutionDropdown.value];
		}
		m_resolutionDropdown.ClearOptions();
		m_resolutionOptions.Clear();
		List<string> list = new List<string>();
		int num = -1;
		foreach (Resolution resolution2 in m_resolutions)
		{
			string text = $"{resolution2.width}x{resolution2.height}";
			if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
			{
				list.Add($"{text} {Mathf.Round((float)resolution2.refreshRateRatio.value)}hz");
			}
			else
			{
				if (list.Contains(text))
				{
					continue;
				}
				list.Add(text);
			}
			if (resolution.HasValue)
			{
				if (resolution.Value.Equals(resolution2))
				{
					num = m_resolutionOptions.Count;
				}
			}
			else if (Screen.width == resolution2.width && Screen.height == resolution2.height)
			{
				num = m_resolutionOptions.Count;
			}
			m_resolutionOptions.Add(resolution2);
		}
		m_resolutionDropdown.AddOptions(list);
		if (num >= 0)
		{
			m_resolutionDropdown.SetValueWithoutNotify(num);
		}
	}

	private void PopulateRenderScales()
	{
		int target3DResolutionVertical = GraphicsSettingsManager.Instance.GetCurrentSettingsWithCurrentPresetApplied(includeBackground: false).m_target3DResolutionVertical;
		List<int> list = new List<int>
		{
			int.MaxValue,
			2160,
			1800,
			1600,
			1440,
			1200,
			1080,
			900,
			800,
			720,
			600,
			480,
			360,
			240,
			192,
			160
		};
		if (UpscaledFrameBuffer.AutomaticRenderScaleSupported())
		{
			list.Add(0);
		}
		int height = Screen.height;
		int num = (height - 1) / 240;
		for (int i = 2; i <= num; i++)
		{
			int num2 = height / i;
			if (num2 * i == height && !list.Contains(num2))
			{
				list.Add(num2);
			}
		}
		if (!list.Contains(target3DResolutionVertical))
		{
			list.Add(target3DResolutionVertical);
		}
		list.Sort(delegate(int a, int b)
		{
			if (a == 0)
			{
				a = 2147483646;
			}
			if (b == 0)
			{
				b = 2147483646;
			}
			return -a.CompareTo(b);
		});
		int value = list.IndexOf(target3DResolutionVertical);
		List<string> list2 = new List<string>(list.Count);
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			int num4 = list[num3];
			switch (num4)
			{
			case int.MaxValue:
				list2.Add(Localization.instance.Localize("$settings_native"));
				continue;
			case 0:
				list2.Add(Localization.instance.Localize("$settings_automatic"));
				continue;
			}
			int num5 = height / num4;
			if (num5 * num4 == height && num5 > 1)
			{
				list2.Add($"{num4}p (1/{num5})");
			}
			else
			{
				list2.Add($"{num4}p");
			}
		}
		m_renderScaleDropdown.ClearOptions();
		m_renderScaleDropdown.AddOptions(list2);
		m_renderScaleDropdown.value = value;
		m_qualityDropdowns.Add(new QualityDropdownData(GraphicsSettingInt.Target3DResolutionVertical, m_renderScaleDropdown, null, list));
	}

	private void UpdateSettingAvailability(GraphicsModeConfiguration config)
	{
		bool isChangeable = GraphicsSettingsManager.CanChangePresentSettings();
		bool flag = SetChangeable(isChangeable, m_resolutionDropdown);
		bool flag2 = SetChangeable(isChangeable, m_fullscreenToggle);
		m_resolutionRoot.SetActive(flag || flag2);
		bool flag3 = SetChangeable(config.HasCustomPreset || config.CanCustomizeGraphicsSetting(GraphicsSettingInt.Target3DResolutionVertical), m_renderScaleDropdown);
		bool flag4 = SetChangeable(config.HasCustomPreset || config.CanCustomizeGraphicsSetting(GraphicsSettingInt.UpscalingAlgorithm), m_upscalingAlgorithmDropdown);
		m_upscalingOptionsRoot.SetActive(flag3 || flag4);
		int num = config.Presets.Count;
		if (config.HasCustomPreset)
		{
			num++;
		}
		SetChangeable(num > 1, m_graphicPresetsRoot, m_graphicPresetLeft, m_graphicPresetRight);
		for (int i = 0; i < m_qualitySliders.Count; i++)
		{
			QualitySliderData qualitySliderData = m_qualitySliders[i];
			SetChangeable(config.HasCustomPreset || config.CanCustomizeGraphicsSetting(qualitySliderData.m_setting), qualitySliderData.m_slider);
		}
		for (int j = 0; j < m_qualityToggles.Count; j++)
		{
			QualityToggleData qualityToggleData = m_qualityToggles[j];
			SetChangeable(config.HasCustomPreset || config.CanCustomizeGraphicsSetting(qualityToggleData.m_setting), qualityToggleData.m_toggle);
		}
	}

	private bool SetChangeable(bool isChangeable, GameObject root, params Selectable[] uis)
	{
		bool flag = isChangeable;
		root?.SetActive(flag);
		if (flag && !isChangeable)
		{
			for (int i = 0; i < uis.Length; i++)
			{
				AddFrame(uis[i].gameObject);
			}
			return true;
		}
		return flag;
	}

	private bool SetChangeable(bool isChangeable, Selectable ui)
	{
		return SetChangeable(isChangeable, ui.gameObject, ui);
	}

	private void UpdateNavigation(Button backButton, Button okButton)
	{
		List<Selectable> list = new List<Selectable>(m_dynamicQualitySliders.Count);
		for (int i = 0; i < m_dynamicQualitySliders.Count; i++)
		{
			if (m_dynamicQualitySliders[i].isActiveAndEnabled)
			{
				list.Add(m_dynamicQualitySliders[i]);
			}
		}
		List<Selectable> list2 = new List<Selectable>(m_dynamicQualityToggles.Count);
		for (int j = 0; j < m_dynamicQualityToggles.Count; j++)
		{
			if (m_dynamicQualityToggles[j].isActiveAndEnabled)
			{
				list2.Add(m_dynamicQualityToggles[j]);
			}
		}
		Selectable selectable = m_graphicPresetLeft;
		Selectable target = ((list.Count > 0) ? list[0] : ((list2.Count <= 0) ? backButton : list2[0]));
		GuiUtils.SetNavigationDown(m_graphicPresetLeft, target);
		GuiUtils.SetNavigationDown(m_graphicPresetRight, target);
		target = ((list2.Count <= 0) ? backButton : list2[0]);
		for (int k = 0; k < list.Count; k++)
		{
			Selectable selectable2 = list[k];
			Navigation navigation = selectable2.navigation;
			navigation.selectOnUp = ((k > 0) ? list[k - 1] : selectable);
			navigation.selectOnDown = ((k < list.Count - 1) ? list[k + 1] : target);
			selectable2.navigation = navigation;
		}
		if (list.Count > 0)
		{
			selectable = list[list.Count - 1];
		}
		target = backButton;
		int constraintCount = m_toggleGridLayoutGroup.constraintCount;
		for (int l = 0; l < list2.Count; l++)
		{
			int num = l / constraintCount;
			int num2 = l - constraintCount * num;
			Selectable selectable3 = list2[l];
			Navigation navigation2 = selectable3.navigation;
			navigation2.selectOnLeft = ((num2 > 0) ? list2[l - 1] : null);
			navigation2.selectOnRight = ((num2 < constraintCount - 1 && l + 1 < list2.Count) ? list2[l + 1] : null);
			navigation2.selectOnUp = ((l - constraintCount >= 0) ? list2[l - constraintCount] : selectable);
			navigation2.selectOnDown = ((l + constraintCount < list2.Count) ? list2[l + constraintCount] : target);
			selectable3.navigation = navigation2;
		}
		if (list2.Count > 0)
		{
			selectable = list2[list2.Count - 1];
		}
		GuiUtils.SetNavigationUp(backButton, selectable);
		GuiUtils.SetNavigationUp(okButton, selectable);
	}

	private void CenterScrollRectOnCurrenlySelected()
	{
		GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
		if ((bool)m_dropdownScrollRectEnsureVisible && (bool)currentSelectedGameObject && ZInput.GamepadActive)
		{
			m_dropdownScrollRectEnsureVisible.CenterOnItem(currentSelectedGameObject.GetComponent<RectTransform>());
		}
	}

	private void UpdateDisplayValues()
	{
		for (int i = 0; i < m_qualitySliders.Count; i++)
		{
			m_qualitySliders[i].m_valueText.text = GetDisplayValue(m_qualitySliders[i].m_setting, Mathf.RoundToInt(m_qualitySliders[i].m_slider.value));
		}
		UpdateModeStepperInfo();
		UpdateTestResolutionButton();
	}

	private void OnResolutionOptionModified()
	{
		m_resolutionOptionModified = true;
		UpdateTestResolutionButton();
	}

	private void UpdateTestResolutionButton()
	{
		m_testResolutionButton.interactable = ResolutionSettingsChanged();
		m_testResolutionButton.gameObject.SetActive(m_testResolutionButton.interactable);
	}

	private void UpdateModeStepperInfo()
	{
		GraphicsModeConfiguration currentGraphicsModeConfiguration = GraphicsSettingsManager.Instance.GetCurrentGraphicsModeConfiguration();
		GraphicsSettingsPreset currentPreset = GetCurrentPreset(currentGraphicsModeConfiguration, excludeUnsupported: false);
		if ((object)currentPreset == null)
		{
			m_graphicsMode.alpha = 1f;
			m_graphicsMode.text = Localization.instance.Localize("$settings_quality_mode_custom");
			m_graphicsModeDescr.text = "";
		}
		else if (GraphicsSettingsManager.Instance.IsPresetSupported(currentPreset))
		{
			m_graphicsMode.alpha = 1f;
			m_graphicsMode.text = Localization.instance.Localize(currentPreset.m_type.NameTextId) + (m_currentPresetModified ? "*" : "");
			m_graphicsModeDescr.text = Localization.instance.Localize(m_currentPresetModified ? "$settings_quality_mode_customized" : currentPreset.m_type.DescriptionTextId);
		}
		else
		{
			m_graphicsMode.alpha = 0.25f;
			m_graphicsMode.text = Localization.instance.Localize(currentPreset.m_type.NameTextId) + "*";
			m_graphicsModeDescr.text = Localization.instance.Localize("$settings_quality_mode_not_supported");
		}
	}

	private void FindDropdownScrollRect()
	{
		ScrollRectEnsureVisible[] componentsInChildren = GetComponentsInChildren<ScrollRectEnsureVisible>(includeInactive: false);
		if (componentsInChildren.Length == 0)
		{
			ZLog.LogError("Missing ScrollRectEnsureVisible component on Resolution dropdown list!");
		}
		else if (componentsInChildren.Length == 1)
		{
			m_dropdownScrollRectEnsureVisible = componentsInChildren[0];
		}
		else
		{
			ZLog.LogError("More than one enabled component with ScrollRectEnsureVisible active within graphics tab at a time - not supported!");
		}
	}

	private void RemoveAllFrames()
	{
		foreach (GameObject devBuildSettingFrame in m_devBuildSettingFrames)
		{
			UnityEngine.Object.Destroy(devBuildSettingFrame);
		}
		m_devBuildSettingFrames.Clear();
	}

	private void AddFrame(GameObject target)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(m_devBuildSettingFramePrefab, target.transform);
		gameObject.name = target.name + " [Dev Frame]";
		gameObject.SetActive(value: true);
		m_devBuildSettingFrames.Add(gameObject);
		Vector3[] array = new Vector3[4];
		RectTransform component = target.GetComponent<RectTransform>();
		component.GetWorldCorners(array);
		Vector2 vector = new Vector2(array[3].x - array[0].x, array[1].y - array[0].y);
		float num = component.rect.width / vector.x;
		gameObject.transform.position = new Vector2(array[0].x + vector.x / 2f, array[2].y - vector.y / 2f);
		gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2((vector.x + 10f) * num, (vector.y + 10f) * num);
	}

	private GraphicsSettingsPreset GetCurrentPreset(GraphicsModeConfiguration config, bool excludeUnsupported)
	{
		GraphicsSettingsPreset presetByID = config.GetPresetByID(m_currentPresetID);
		if (config.HasCustomPreset)
		{
			return presetByID;
		}
		if ((object)presetByID == null)
		{
			return config.DefaultPreset;
		}
		if (excludeUnsupported && (object)presetByID != null && !GraphicsSettingsManager.Instance.IsPresetSupported(presetByID))
		{
			presetByID = config.GetPresetByID(GraphicsSettingsManager.Instance.CurrentPresetID);
		}
		return presetByID;
	}

	private GraphicsSettingsState GetStateFromSettingsMenu()
	{
		GraphicsSettingsState result = new GraphicsSettingsState
		{
			m_presentSettings = new PresentSettingsState
			{
				m_fpsLimit = Mathf.RoundToInt(m_fpsLimitSlider.value),
				m_vsync = m_vsyncToggle.isOn
			}
		};
		for (int i = 0; i < m_qualityDropdowns.Count; i++)
		{
			GraphicsSettingInt setting = m_qualityDropdowns[i].m_setting;
			if (!setting.IsPresentSetting())
			{
				int value = m_qualityDropdowns[i].GetValue(Mathf.RoundToInt(m_qualityDropdowns[i].m_dropdown.value));
				result.SetValue(setting, value);
			}
		}
		for (int j = 0; j < m_qualitySliders.Count; j++)
		{
			GraphicsSettingInt setting2 = m_qualitySliders[j].m_setting;
			if (!setting2.IsPresentSetting())
			{
				result.SetValue(setting2, Mathf.RoundToInt(m_qualitySliders[j].m_slider.value));
			}
		}
		for (int k = 0; k < m_qualityToggles.Count; k++)
		{
			GraphicsSettingBool setting3 = m_qualityToggles[k].m_setting;
			if (!setting3.IsPresentSetting())
			{
				result.SetValue(setting3, m_qualityToggles[k].m_toggle.isOn);
			}
		}
		return result;
	}

	private void ModifySetting(GraphicsSettingInt setting, int value)
	{
		GraphicsModeConfiguration currentGraphicsModeConfiguration = GraphicsSettingsManager.Instance.GetCurrentGraphicsModeConfiguration();
		if (currentGraphicsModeConfiguration.HasCustomPreset && m_currentPresetID != 100)
		{
			GraphicsSettingsPreset currentPreset = GetCurrentPreset(currentGraphicsModeConfiguration, excludeUnsupported: true);
			if (!currentGraphicsModeConfiguration.CanCustomizeGraphicsSetting(setting) && currentPreset.TryGetQualitySetting(setting, out int _))
			{
				m_currentPresetModified = true;
				m_currentSettingsRaw = GetStateFromSettingsMenu();
			}
		}
		m_currentSettingsRaw.SetValue(setting, value);
		UpdateModeStepperInfo();
	}

	private void ModifySetting(GraphicsSettingBool setting, bool value)
	{
		GraphicsModeConfiguration currentGraphicsModeConfiguration = GraphicsSettingsManager.Instance.GetCurrentGraphicsModeConfiguration();
		if (currentGraphicsModeConfiguration.HasCustomPreset && m_currentPresetID != 100)
		{
			GraphicsSettingsPreset currentPreset = GetCurrentPreset(currentGraphicsModeConfiguration, excludeUnsupported: true);
			if (!currentGraphicsModeConfiguration.CanCustomizeGraphicsSetting(setting) && currentPreset.TryGetQualitySetting(setting, out var value2) && (!GraphicsSettingsManager.Instance.IsAccessibilitySetting(setting) || (!value2 && value)))
			{
				m_currentPresetModified = true;
				m_currentSettingsRaw = GetStateFromSettingsMenu();
			}
		}
		m_currentSettingsRaw.SetValue(setting, value);
		UpdateModeStepperInfo();
	}

	private void InvokeCallbackIfSet()
	{
		if (m_okActionCompletedCallback != null)
		{
			OkActionCompletedHandler okActionCompletedCallback = m_okActionCompletedCallback;
			m_okActionCompletedCallback = null;
			okActionCompletedCallback();
		}
	}

	private bool ResolutionSettingsChanged()
	{
		if (!m_resolutionOptionModified)
		{
			return false;
		}
		Resolution resolution = m_resolutionOptions[m_resolutionDropdown.value];
		bool fullScreen = Screen.fullScreen;
		Resolution currentPresentResolution = PresentManager.GetCurrentPresentResolution();
		if (currentPresentResolution.width == resolution.width && currentPresentResolution.height == resolution.height && currentPresentResolution.refreshRateRatio.value == resolution.refreshRateRatio.value)
		{
			return fullScreen != m_fullscreenToggle.isOn;
		}
		return true;
	}

	private void SetSettingsFromState(GraphicsSettingsState state)
	{
		m_fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
		for (int i = 0; i < m_qualityDropdowns.Count; i++)
		{
			QualityDropdownData qualityDropdownData = m_qualityDropdowns[i];
			int value = state.GetValue(qualityDropdownData.m_setting);
			for (int j = 0; j < qualityDropdownData.OptionCount; j++)
			{
				if (qualityDropdownData.GetValue(j) == value)
				{
					qualityDropdownData.m_dropdown.SetValueWithoutNotify(j);
					break;
				}
			}
		}
		for (int k = 0; k < m_qualitySliders.Count; k++)
		{
			QualitySliderData qualitySliderData = m_qualitySliders[k];
			int num = state.GetValue(m_qualitySliders[k].m_setting);
			if (qualitySliderData.m_setting == GraphicsSettingInt.FpsLimit && num < 30)
			{
				num = 361;
			}
			m_qualitySliders[k].m_slider.SetValueWithoutNotify(num);
		}
		for (int l = 0; l < m_qualityToggles.Count; l++)
		{
			m_qualityToggles[l].m_toggle.SetIsOnWithoutNotify(state.GetValue(m_qualityToggles[l].m_setting));
		}
		UpdateDisplayValues();
	}

	public void ChangePreset(int relativeIndex)
	{
		GraphicsModeConfiguration currentGraphicsModeConfiguration = GraphicsSettingsManager.Instance.GetCurrentGraphicsModeConfiguration();
		int num = (currentGraphicsModeConfiguration.HasCustomPreset ? 1 : 0);
		int presetIndexByID = currentGraphicsModeConfiguration.GetPresetIndexByID(m_currentPresetID);
		presetIndexByID = Utils.Mod(presetIndexByID + relativeIndex + num, currentGraphicsModeConfiguration.Presets.Count + num) - num;
		m_currentPresetID = currentGraphicsModeConfiguration.GetPresetIDByIndex(presetIndexByID);
		m_currentPresetModified = false;
		GraphicsSettingsState state = m_currentSettingsRaw;
		GraphicsSettingsPreset currentPreset = GetCurrentPreset(currentGraphicsModeConfiguration, excludeUnsupported: true);
		if ((object)currentPreset != null)
		{
			GraphicsSettingsManager.Instance.SetGraphicsSettingsFromPreset(currentGraphicsModeConfiguration, ref state, currentPreset);
		}
		SetSettingsFromState(state);
	}

	private void UpdateValidResolutions()
	{
		Resolution[] resolutions = Screen.resolutions;
		m_resolutions.Clear();
		Resolution[] array = resolutions;
		for (int i = 0; i < array.Length; i++)
		{
			Resolution item = array[i];
			if ((item.width >= m_minResWidth && item.height >= m_minResHeight) || item.width == m_oldResolution.width || item.height == m_oldResolution.height)
			{
				m_resolutions.Add(item);
			}
		}
		if (m_resolutions.Count == 0)
		{
			m_resolutions.Add(m_oldResolution);
		}
		m_resolutions.Sort(delegate(Resolution a, Resolution b)
		{
			if (a.width != b.width)
			{
				return -a.width.CompareTo(b.width);
			}
			if (a.height != b.height)
			{
				return -a.height.CompareTo(b.height);
			}
			double value = a.refreshRateRatio.value;
			double value2 = b.refreshRateRatio.value;
			return (value != value2) ? (-value.CompareTo(value2)) : 0;
		});
	}

	private void ApplyResolution(Resolution resolution)
	{
		if (Screen.width != resolution.width || Screen.height != resolution.height || m_fullscreenToggle.isOn != Screen.fullScreen)
		{
			Screen.SetResolution(resolution.width, resolution.height, m_fullscreenToggle.isOn ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, resolution.refreshRateRatio);
		}
	}

	public void RevertMode()
	{
		m_fullscreenToggle.isOn = m_oldFullscreen;
		ApplyResolution(m_oldResolution);
		UpdateTestResolutionButton();
		Settings.instance.BlockNavigation(block: false);
		InvokeCallbackIfSet();
	}

	private static string GetQualityText(int level)
	{
		return level switch
		{
			1 => Localization.instance.Localize("[$settings_medium]"), 
			2 => Localization.instance.Localize("[$settings_high]"), 
			3 => Localization.instance.Localize("[$settings_veryhigh]"), 
			_ => Localization.instance.Localize("[$settings_low]"), 
		};
	}

	private static string GetDisplayValue(GraphicsSettingInt setting, int value)
	{
		switch (setting)
		{
		case GraphicsSettingInt.FpsLimit:
			if (value > 360)
			{
				return Localization.instance.Localize("$settings_unlimited");
			}
			break;
		case GraphicsSettingInt.Vegetation:
			return GetQualityText(Math.Max(0, value - 1));
		case GraphicsSettingInt.LOD:
		case GraphicsSettingInt.Lights:
		case GraphicsSettingInt.ShadowQuality:
			return GetQualityText(value);
		case GraphicsSettingInt.PointLights:
		{
			int pointLightLimit = GraphicsSettingsManager.GetPointLightLimit(value);
			return GetQualityText(value) + " (" + ((pointLightLimit < 0) ? Localization.instance.Localize("$settings_infinite") : pointLightLimit.ToString()) + ")";
		}
		case GraphicsSettingInt.PointLightShadows:
		{
			int pointLightShadowLimit = GraphicsSettingsManager.GetPointLightShadowLimit(value);
			return GetQualityText(value) + " (" + ((pointLightShadowLimit < 0) ? Localization.instance.Localize("$settings_infinite") : pointLightShadowLimit.ToString()) + ")";
		}
		case GraphicsSettingInt.SSAO:
			return value switch
			{
				0 => Localization.instance.Localize("[$hud_off]"), 
				1 => GetQualityText(0), 
				_ => GetQualityText(2), 
			};
		}
		return value.ToString();
	}
}
