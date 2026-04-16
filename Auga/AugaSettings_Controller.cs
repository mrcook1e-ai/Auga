using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    // ============================================================
    // AugaSettings_Controller.cs
    //
    // Реализует логику настроек для Auga-префаба Settings.
    //
    // Проблема: Auga-префаб Settings содержал скрипт AugaSettingsManager
    // (отсутствует в собранном DLL — "Missing Script" компонент). Этот
    // скрипт соединял UI-контролы (слайдеры, тогглы, дропдауны) с
    // PlatformPrefs. В Valheim 0.221 Settings полностью переработан
    // на ISettingsTab, поэтому связка была полностью сломана.
    //
    // Решение: Postfix на Settings.Awake находит контролы по имени GO
    // в каждой вкладке и программно:
    //   1. Читает текущие значения из PlatformPrefs → устанавливает контролы
    //   2. Подписывается на onChange → применяет изменения в реальном времени
    //   3. При OK (через Settings_OnOk_Patch) → PlatformPrefs.Save()
    //
    // GO-имена контролов взяты из AugaSettings.prefab:
    //   Audio:    MasterVolume, EffectVolume, MusicVolume, ContinuousMusic
    //   Controls: MouseSensitivity, GamepadSensitivity, InvertMouse, ToggleRun
    //   Graphics: DepthOfField, VSYNC, Bloom, SSAO, SunShafts, AntiAliasing,
    //             ChromaticAbberation, MotionBlur, Tessellation, DistantShadows,
    //             SoftParticles, Fullscreen, ShadowQuality, LOD, Lights,
    //             Vegitation, PointLights, PointLightsShadows
    //   Misc:     GuiScale, ShowKeyHints, ShowTutorials, Autobackups, Language
    //
    // PlatformPrefs-ключи верифицированы по decompile Valheim 0.221:
    //   - Toggles (DOF, Bloom и т.д.): GetBool/SetBool (= GetInt/SetInt под капотом)
    //   - Quality levels (ShadowQuality, LoD, Lights и т.д.): GetInt/SetInt
    // ============================================================

    [HarmonyPatch(typeof(Settings), nameof(Settings.Awake))]
    public static class Settings_Awake_Init_Patch
    {
        public static void Postfix(Settings __instance)
        {
            try
            {
                AugaSettingsWirer.Wire(__instance.gameObject);
            }
            catch (Exception ex)
            {
                Auga.LogWarning($"[AugaSettings] Wire() exception: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    public static class AugaSettingsWirer
    {
        public static void Wire(GameObject settingsRoot)
        {
            var tabHandler = settingsRoot.GetComponentInChildren<TabHandler>(true);
            if (tabHandler == null)
            {
                Auga.LogWarning("[AugaSettings] TabHandler not found — settings will be unresponsive");
                return;
            }

            int wiredTabs = 0;
            foreach (var tab in tabHandler.m_tabs)
            {
                if (tab.m_page == null) continue;
                var page = tab.m_page;

                switch (page.gameObject.name)
                {
                    case "Audio":    WireAudio(page);    wiredTabs++; break;
                    case "Controls": WireControls(page); wiredTabs++; break;
                    case "Graphics": WireGraphics(page); wiredTabs++; break;
                    case "Misc":     WireMisc(page);     wiredTabs++; break;
                }
            }

            Auga.Log($"[AugaSettings] Wired {wiredTabs}/{tabHandler.m_tabs.Count} tabs");
        }

        // ===================== Audio =====================
        private static void WireAudio(Transform page)
        {
            // Слайдеры громкости: 0-1
            WireSlider(page, "MasterVolume",
                read:  () => PlatformPrefs.GetFloat("MasterVolume", AudioListener.volume),
                apply: v => { AudioListener.volume = v; PlatformPrefs.SetFloat("MasterVolume", v); },
                valueSuffix: "%");

            WireSlider(page, "EffectVolume",
                read:  () => PlatformPrefs.GetFloat("SfxVolume", 1f),
                apply: v => { AudioMan.SetSFXVolume(v); PlatformPrefs.SetFloat("SfxVolume", v); },
                valueSuffix: "%");

            WireSlider(page, "MusicVolume",
                read:  () => PlatformPrefs.GetFloat("MusicVolume", 1f),
                apply: v => { MusicMan.m_masterMusicVolume = v; PlatformPrefs.SetFloat("MusicVolume", v); },
                valueSuffix: "%");

            WireToggle(page, "ContinuousMusic",
                read:  () => PlatformPrefs.GetBool("ContinousMusic", true),  // ключ с опечаткой в Valheim
                apply: v => { Settings.ContinousMusic = v; PlatformPrefs.SetBool("ContinousMusic", v); });
        }

        // ===================== Controls =====================
        private static void WireControls(Transform page)
        {
            WireSlider(page, "MouseSensitivity",
                read:  () => PlatformPrefs.GetFloat("MouseSensitivity", PlayerController.m_mouseSens),
                apply: v => { PlayerController.m_mouseSens = v; PlatformPrefs.SetFloat("MouseSensitivity", v); },
                valueSuffix: "%");

            // GamepadSensitivity — поле в Valheim gamepad settings (нет прямого PlatformPrefs ключа в KBM)
            WireSlider(page, "GamepadSensitivity",
                read:  () => PlatformPrefs.GetFloat("GamepadSensitivity", 1f),
                apply: v => PlatformPrefs.SetFloat("GamepadSensitivity", v),
                valueSuffix: "%");

            WireToggle(page, "InvertMouse",
                read:  () => PlatformPrefs.GetBool("InvertMouse"),
                apply: v => { PlayerController.m_invertMouse = v; PlatformPrefs.SetBool("InvertMouse", v); });

            WireToggle(page, "ToggleRun",
                read:  () => PlatformPrefs.GetBool("ToggleRun", ZInput.IsGamepadActive()),
                apply: v => { ZInput.ToggleRun = v; PlatformPrefs.SetBool("ToggleRun", v); });

            // AugaBindingDisplay контролы обновляются автоматически через AugaBindingDisplay.Update()
            // (GetBoundKeyString) — дополнительной логики не требуется
        }

        // ===================== Graphics =====================
        private static void WireGraphics(Transform page)
        {
            // Toggles: используем GetBool/SetBool — соответствует Valheim 0.221
            // Значения по умолчанию взяты из GraphicsSettings.s_defaultGraphicsSettings
            WireToggle(page, "DepthOfField",       () => PlatformPrefs.GetBool("DOF", true),              v => PlatformPrefs.SetBool("DOF", v));
            WireToggle(page, "VSYNC",              () => PlatformPrefs.GetBool("VSync"),                  v => { QualitySettings.vSyncCount = v ? 1 : 0; PlatformPrefs.SetBool("VSync", v); });
            WireToggle(page, "Bloom",              () => PlatformPrefs.GetBool("Bloom", true),            v => PlatformPrefs.SetBool("Bloom", v));
            WireToggle(page, "SSAO",               () => PlatformPrefs.GetBool("SSAO", true),             v => PlatformPrefs.SetBool("SSAO", v));
            WireToggle(page, "SunShafts",          () => PlatformPrefs.GetBool("SunShafts", true),        v => PlatformPrefs.SetBool("SunShafts", v));
            WireToggle(page, "AntiAliasing",       () => PlatformPrefs.GetBool("AntiAliasing", true),     v => PlatformPrefs.SetBool("AntiAliasing", v));
            WireToggle(page, "ChromaticAbberation",() => PlatformPrefs.GetBool("ChromaticAberration"),    v => PlatformPrefs.SetBool("ChromaticAberration", v));  // typo в GO-имени намеренно
            WireToggle(page, "MotionBlur",         () => PlatformPrefs.GetBool("MotionBlur"),             v => PlatformPrefs.SetBool("MotionBlur", v));
            WireToggle(page, "Tessellation",       () => PlatformPrefs.GetBool("Tesselation", true),      v => PlatformPrefs.SetBool("Tesselation", v));  // ключ с одной s
            WireToggle(page, "DistantShadows",     () => PlatformPrefs.GetBool("DistantShadows", true),   v => PlatformPrefs.SetBool("DistantShadows", v));
            WireToggle(page, "SoftParticles",      () => PlatformPrefs.GetBool("SoftPart", true),         v => PlatformPrefs.SetBool("SoftPart", v));   // ключ "SoftPart", не "SoftParticles"
            WireToggle(page, "Fullscreen",         () => Screen.fullScreen,                               v => Screen.fullScreen = v);

            // Dropdowns: качество (0=Low, 1=Med, 2=High, 3=VeryHigh для Shadow; 0-2 для остальных)
            WireDropdown(page, "ShadowQuality",      () => PlatformPrefs.GetInt("ShadowQuality", 2),     v => PlatformPrefs.SetInt("ShadowQuality", v));
            WireDropdown(page, "LOD",                () => PlatformPrefs.GetInt("LoD", 2),               v => PlatformPrefs.SetInt("LoD", v));
            WireDropdown(page, "Lights",             () => PlatformPrefs.GetInt("Lights", 2),            v => PlatformPrefs.SetInt("Lights", v));
            WireDropdown(page, "Vegitation",         () => PlatformPrefs.GetInt("ClutterQuality", 2),    v => PlatformPrefs.SetInt("ClutterQuality", v));
            WireDropdown(page, "PointLights",        () => PlatformPrefs.GetInt("PointLights", 2),       v => PlatformPrefs.SetInt("PointLights", v));
            WireDropdown(page, "PointLightsShadows", () => PlatformPrefs.GetInt("PointLightShadows", 0), v => PlatformPrefs.SetInt("PointLightShadows", v));
        }

        // ===================== Misc =====================
        private static void WireMisc(Transform page)
        {
            WireSlider(page, "GuiScale",
                read:  () => PlatformPrefs.GetFloat("GuiScale", 1f),
                apply: v => PlatformPrefs.SetFloat("GuiScale", v));

            WireToggle(page, "ShowKeyHints",
                read:  () => PlatformPrefs.GetBool("KeyHints", true),
                apply: v => PlatformPrefs.SetBool("KeyHints", v));

            WireToggle(page, "ShowTutorials",
                read:  () => PlatformPrefs.GetBool("TutorialsEnabled", true),
                apply: v => { Raven.m_tutorialsEnabled = v; PlatformPrefs.SetBool("TutorialsEnabled", v); });

            // Autobackups: в Auga-префабе Toggle (в ванили — Slider).
            // Toggle=ON: 4 бэкапа (дефолт Valheim); Toggle=OFF: 0 бэкапов
            WireToggle(page, "Autobackups",
                read:  () => PlatformPrefs.GetInt("AutoBackups", 4) > 0,
                apply: v => PlatformPrefs.SetInt("AutoBackups", v ? 4 : 0));

            // Language dropdown — заполняем список языков локализации
            WireLanguageDropdown(page);
        }

        // ===================== Helpers =====================

        private static void WireSlider(Transform page, string goName,
            Func<float> read, Action<float> apply, string valueSuffix = null)
        {
            var go = FindDeepChild(page, goName);
            if (go == null)
            {
                Auga.LogWarning($"[AugaSettings] Slider GO '{goName}' not found in '{page.name}'");
                return;
            }

            var slider = go.GetComponentInChildren<Slider>(true);
            if (slider == null)
            {
                Auga.LogWarning($"[AugaSettings] No Slider component in '{goName}'");
                return;
            }

            try { slider.SetValueWithoutNotify(read()); }
            catch (Exception ex) { Auga.LogWarning($"[AugaSettings] Slider '{goName}' read error: {ex.Message}"); }

            // Текстовый дисплей значения (если есть — обновляем при изменении)
            var valueText = go.GetComponentsInChildren<TMPro.TMP_Text>(true)
                .FirstOrDefault(t => t.gameObject.name.IndexOf("Value", StringComparison.OrdinalIgnoreCase) >= 0
                                  || t.gameObject.name.IndexOf("Amount", StringComparison.OrdinalIgnoreCase) >= 0);

            void UpdateValueText(float v)
            {
                if (valueText != null && valueSuffix == "%")
                    valueText.text = Mathf.Round(v * 100f) + "%";
            }

            UpdateValueText(slider.value);
            slider.onValueChanged.AddListener(v => { apply(v); UpdateValueText(v); });
        }

        private static void WireToggle(Transform page, string goName,
            Func<bool> read, Action<bool> apply)
        {
            var go = FindDeepChild(page, goName);
            if (go == null)
            {
                Auga.LogWarning($"[AugaSettings] Toggle GO '{goName}' not found in '{page.name}'");
                return;
            }

            var toggle = go.GetComponentInChildren<Toggle>(true);
            if (toggle == null)
            {
                Auga.LogWarning($"[AugaSettings] No Toggle component in '{goName}'");
                return;
            }

            try { toggle.SetIsOnWithoutNotify(read()); }
            catch (Exception ex) { Auga.LogWarning($"[AugaSettings] Toggle '{goName}' read error: {ex.Message}"); }

            toggle.onValueChanged.AddListener(v => {
                try { apply(v); }
                catch (Exception ex) { Auga.LogWarning($"[AugaSettings] Toggle '{goName}' apply error: {ex.Message}"); }
            });
        }

        private static void WireDropdown(Transform page, string goName,
            Func<int> read, Action<int> apply)
        {
            var go = FindDeepChild(page, goName);
            if (go == null)
            {
                Auga.LogWarning($"[AugaSettings] Dropdown GO '{goName}' not found in '{page.name}'");
                return;
            }

            var dd = go.GetComponentInChildren<Dropdown>(true);
            if (dd == null)
            {
                Auga.LogWarning($"[AugaSettings] No Dropdown component in '{goName}'");
                return;
            }

            try
            {
                int idx = Mathf.Clamp(read(), 0, Mathf.Max(0, dd.options.Count - 1));
                dd.SetValueWithoutNotify(idx);
            }
            catch (Exception ex) { Auga.LogWarning($"[AugaSettings] Dropdown '{goName}' read error: {ex.Message}"); }

            dd.onValueChanged.AddListener(v => {
                try { apply(v); }
                catch (Exception ex) { Auga.LogWarning($"[AugaSettings] Dropdown '{goName}' apply error: {ex.Message}"); }
            });
        }

        private static void WireLanguageDropdown(Transform page)
        {
            var go = FindDeepChild(page, "Language");
            if (go == null) return;

            var dd = go.GetComponentInChildren<Dropdown>(true);
            if (dd == null) return;

            try
            {
                var languages = Localization.instance.GetLanguages();
                if (languages == null || languages.Count == 0) return;

                dd.ClearOptions();
                dd.AddOptions(languages
                    .Select(l => Localization.instance.Localize("$language_" + l.ToLower()))
                    .ToList());

                var currentLang = Localization.instance.GetSelectedLanguage();
                int langIdx = languages.IndexOf(currentLang);
                dd.SetValueWithoutNotify(Mathf.Max(0, langIdx));

                dd.onValueChanged.AddListener(idx =>
                {
                    if (idx >= 0 && idx < languages.Count)
                        Localization.instance.SetLanguage(languages[idx]);
                });
            }
            catch (Exception ex)
            {
                Auga.LogWarning($"[AugaSettings] Language dropdown error: {ex.Message}");
            }
        }

        // Рекурсивный поиск GO по имени начиная с root (включая root сам по себе)
        private static GameObject FindDeepChild(Transform root, string name)
        {
            if (root.gameObject.name == name) return root.gameObject;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeepChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
