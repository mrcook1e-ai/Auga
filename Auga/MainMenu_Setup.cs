using System.Collections;
using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.SnapTo))]
    public static class SnapTo_Patch
    {
        public static void Postfix(TextsDialog __instance, RectTransform listRoot, ScrollRect scrollRect)
        {
            var augaTextComponent = __instance.GetComponent<AugaTextsDialogFilter>();
            if (augaTextComponent == null)
                return;

            var newVector = new Vector2(0, listRoot.anchoredPosition.y);
            listRoot.anchoredPosition = newVector;
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    public static class FejdStartup_Awake_Patch
    {
        // Null-safe Find + GetComponent. Logs warning if path or component missing.
        private static T FC<T>(Transform root, string path) where T : Component
        {
            if (root == null) { Auga.LogWarning($"FC<{typeof(T).Name}>: root is null (path={path})"); return null; }
            var t = root.Find(path);
            if (t == null) { Auga.LogWarning($"FC<{typeof(T).Name}>: path not found: {path}"); return null; }
            var c = t.GetComponent<T>();
            if (c == null) Auga.LogWarning($"FC<{typeof(T).Name}>: no component on: {path}");
            return c;
        }

        // Null-safe Find -> GameObject.
        private static GameObject FO(Transform root, string path)
        {
            if (root == null) { Auga.LogWarning($"FO: root is null (path={path})"); return null; }
            var t = root.Find(path);
            if (t == null) { Auga.LogWarning($"FO: path not found: {path}"); return null; }
            return t.gameObject;
        }

        public static void Prefix(FejdStartup __instance)
        {
            ZInput.Initialize();

            __instance.m_settingsPrefab = Auga.Assets.SettingsPrefab;

            var originalLogo = __instance.transform.Find("Menu/Logo");
            if (originalLogo != null)
                originalLogo.SetParent(__instance.transform, true);

            var mainMenu = __instance.Replace("Menu", Auga.Assets.MainMenuPrefab);
            if (mainMenu == null) { Auga.LogError("Failed to replace Menu"); return; }
            if (originalLogo != null)
                originalLogo.SetParent(mainMenu, true);

            __instance.m_mainMenu = mainMenu.gameObject;
            __instance.m_menuList = FO(mainMenu, "MenuList");
            __instance.m_menuSelectedButton = FC<Button>(mainMenu, "MenuList/StartGame");
            __instance.m_versionLabel = FC<TMP_Text>(mainMenu, "Version");
            __instance.m_betaText = FO(mainMenu, "DummyObjects/Dummy");
            __instance.m_ndaPanel = FO(mainMenu, "DummyObjects/Dummy");
            SetButtonListener(mainMenu, "MenuList/StartGame", __instance.OnStartGame);
            SetButtonListener(mainMenu, "MenuList/Settings", __instance.OnButtonSettings);
            SetButtonListener(mainMenu, "MenuList/Credits", __instance.OnCredits);
            SetButtonListener(mainMenu, "MenuList/Exit", __instance.OnAbort);

            var connectionFailedDialog = __instance.Replace("ConnectionFailed", Auga.Assets.MainMenuPrefab);
            if (connectionFailedDialog != null)
            {
                __instance.m_connectionFailedPanel = connectionFailedDialog.gameObject;
                __instance.m_connectionFailedError = FC<TMP_Text>(connectionFailedDialog, "Text");
                SetButtonListener(connectionFailedDialog, "ButtonYes", __instance.OnConnectionFailedOk);
            }

            var credits = __instance.Replace("Credits", Auga.Assets.MainMenuPrefab);
            if (credits != null)
            {
                __instance.m_creditsPanel = credits.gameObject;
                __instance.m_creditsList = credits.Find("ContactInfo") as RectTransform;
                SetButtonListener(credits, "Back-panel/ButtonSettings", __instance.OnCreditsBack);
            }

            __instance.Replace("BLACK", Auga.Assets.MainMenuPrefab);

            var loading = __instance.Replace("Loading", Auga.Assets.MainMenuPrefab);
            if (loading != null)
            {
                __instance.m_loading = loading.gameObject;
                __instance.m_loading.SetActive(false);
            }

            // ---- SelectCharacter ----
            var charSelect = __instance.Replace("CharacterSelection/SelectCharacter", Auga.Assets.MainMenuPrefab);
            if (charSelect != null)
            {
                __instance.m_selectCharacterPanel = charSelect.gameObject;
                // Auga SelectCharacter paths (from AugaUnity prefab structure)
                __instance.m_removeCharacterDialog = FO(charSelect, "RemoveCharacterDialog");
                __instance.m_removeCharacterName = FC<TMP_Text>(charSelect, "RemoveCharacterDialog/Text");
                __instance.m_csRemoveButton = FC<Button>(charSelect, "Inset/RemoveButton");
                __instance.m_csStartButton = FC<Button>(charSelect, "Start");
                __instance.m_csNewButton = FC<Button>(charSelect, "Inset/NewButton");
                __instance.m_csNewBigButton = FC<Button>(charSelect, "Inset/NewButtonBig");
                __instance.m_csLeftButton = FC<Button>(charSelect, "DummyObjects/Dummy");
                __instance.m_csRightButton = FC<Button>(charSelect, "DummyObjects/Dummy");
                __instance.m_csName = FC<TMP_Text>(charSelect, "DummyObjects/Dummy");
                __instance.m_csFileSource = FC<TMP_Text>(charSelect, "SourceInfo");
                __instance.m_csSourceInfo = FC<TMP_Text>(charSelect, "SourceInfo");
                SetButtonListener(charSelect, "Inset/RemoveButton", __instance.OnCharacterRemove);
                SetButtonListener(charSelect, "Inset/NewButton", __instance.OnCharacterNew);
                SetButtonListener(charSelect, "Inset/NewButtonBig", __instance.OnCharacterNew);
                SetButtonListener(charSelect, "Back", __instance.OnSelelectCharacterBack);
                SetButtonListener(charSelect, "Start", __instance.OnCharacterStart);
                SetButtonListener(charSelect, "ManageSaves", () => __instance.OnManageSaves(1));
                SetButtonListener(charSelect, "RemoveCharacterDialog/ButtonYes", __instance.OnButtonRemoveCharacterYes);
                SetButtonListener(charSelect, "RemoveCharacterDialog/ButtonNo", __instance.OnButtonRemoveCharacterNo);
            }

            // ---- NewCharacterPanel ----
            var oldPlayerCustomizaton = __instance.m_newCharacterPanel != null
                ? __instance.m_newCharacterPanel.GetComponent<PlayerCustomizaton>()
                : null;
            var originalNoHair = oldPlayerCustomizaton != null ? oldPlayerCustomizaton.m_noHair : null;
            var originalNoBeard = oldPlayerCustomizaton != null ? oldPlayerCustomizaton.m_noBeard : null;

            var newCharacter = __instance.Replace("CharacterSelection/NewCharacterPanel", Auga.Assets.MainMenuPrefab);
            if (newCharacter != null)
            {
                var newPlayerCustomization = newCharacter.GetComponent<PlayerCustomizaton>();
                if (newPlayerCustomization != null)
                {
                    newPlayerCustomization.m_noHair = originalNoHair;
                    newPlayerCustomization.m_noBeard = originalNoBeard;
                }
                __instance.m_newCharacterPanel = newCharacter.gameObject;
                // Auga NewCharacterPanel paths (Content + ToggleGroup structure)
                __instance.m_csNewCharacterDone = FC<Button>(newCharacter, "Content/Done")
                    ?? FC<Button>(newCharacter, "Done");
                __instance.m_newCharacterError = FO(newCharacter, "Content/NameExistsWarning");
                __instance.m_csNewCharacterName = FC<GUIFramework.GuiInputField>(newCharacter, "Content/CharacterName")
                    ?? FC<GUIFramework.GuiInputField>(newCharacter, "CharacterName");
                SetButtonListener(newCharacter, "Content/Done", () => __instance.OnNewCharacterDone(true));
                SetButtonListener(newCharacter, "Content/Cancel", __instance.OnNewCharacterCancel);

                var toggleFemale = FC<Toggle>(newCharacter, "ToggleGroup/Toggle_Female");
                if (toggleFemale != null)
                {
                    toggleFemale.onValueChanged = new Toggle.ToggleEvent();
                    toggleFemale.onValueChanged.AddListener((on) => { if (on) newPlayerCustomization?.SetPlayerModel(1); });
                    toggleFemale.onValueChanged.AddListener((on) => toggleFemale.transform.GetChild(1).gameObject.SetActive(on));
                }
                var toggleMale = FC<Toggle>(newCharacter, "ToggleGroup/Toggle_Male");
                if (toggleMale != null)
                {
                    toggleMale.onValueChanged = new Toggle.ToggleEvent();
                    toggleMale.onValueChanged.AddListener((on) => { if (on) newPlayerCustomization?.SetPlayerModel(0); });
                    toggleMale.onValueChanged.AddListener((on) => toggleMale.transform.GetChild(1).gameObject.SetActive(on));
                }
            }

            // ---- StartGame ----
            var startGame = __instance.Replace("StartGame", Auga.Assets.MainMenuPrefab);
            if (startGame != null)
            {
                __instance.m_startGamePanel = startGame.gameObject;
                __instance.m_createWorldPanel = FO(startGame, "NewWorldDialog");
                __instance.m_serverListPanel = FO(startGame, "Panel/JoinPanel");
                __instance.m_publicServerToggle = FC<Toggle>(startGame, "Panel/WorldPanel/CheckboxRow/StartPublicGameToggle");
                __instance.m_openServerToggle = FC<Toggle>(startGame, "Panel/WorldPanel/CheckboxRow/StartServerToggle");
                __instance.m_serverPassword = FC<GUIFramework.GuiInputField>(startGame, "Panel/WorldPanel/ServerPassword");
                __instance.m_passwordError = FC<TMP_Text>(startGame, "Panel/WorldPanel/ServerPassword/Tooltip/ErrorText");
                __instance.m_worldListRoot = FC<RectTransform>(startGame, "Panel/WorldPanel/ScrollRect/ItemList");
                __instance.m_worldListElement = Auga.Assets.WorldListElement;
                __instance.m_worldListEnsureVisible = FC<ScrollRectEnsureVisible>(startGame, "Panel/WorldPanel/ScrollRect");
                __instance.m_worldListElementStep = 30;
                __instance.m_newWorldName = FC<GUIFramework.GuiInputField>(startGame, "NewWorldDialog/WorldName");
                __instance.m_newWorldSeed = FC<GUIFramework.GuiInputField>(startGame, "NewWorldDialog/WorldSeed");
                __instance.m_newWorldDone = FC<Button>(startGame, "NewWorldDialog/Done");
                __instance.m_worldStart = FC<Button>(startGame, "Panel/WorldPanel/Start");
                __instance.m_worldRemove = FC<Button>(startGame, "Panel/WorldPanel/RemoveButton");
                __instance.m_removeWorldDialog = FO(startGame, "RemoveWorldDialog");
                __instance.m_removeWorldName = FC<TMP_Text>(startGame, "RemoveWorldDialog/Text");
                __instance.m_worldListPanel = FO(startGame, "Panel/WorldPanel");

                SetButtonListener(startGame, "Panel/WorldPanel/RemoveButton", __instance.OnWorldRemove);
                SetButtonListener(startGame, "Panel/WorldPanel/NewButton", __instance.OnWorldNew);
                SetButtonListener(startGame, "Panel/WorldPanel/Back", __instance.OnStartGameBack);
                SetButtonListener(startGame, "Panel/WorldPanel/Start", __instance.OnWorldStart);
                SetButtonListener(startGame, "RemoveWorldDialog/ButtonYes", __instance.OnButtonRemoveWorldYes);
                SetButtonListener(startGame, "RemoveWorldDialog/ButtonNo", __instance.OnButtonRemoveWorldNo);
                SetButtonListener(startGame, "NewWorldDialog/Cancel", __instance.OnNewWorldBack);
                SetButtonListener(startGame, "NewWorldDialog/Done", () => __instance.OnNewWorldDone(true));

                var tabHandler = startGame.GetComponentInChildren<TabHandler>(true);
                if (tabHandler != null && tabHandler.m_tabs.Count >= 2)
                {
                    tabHandler.m_tabs[0].m_onClick = new Button.ButtonClickedEvent();
                    tabHandler.m_tabs[0].m_onClick.AddListener(__instance.OnSelectWorldTab);
                    tabHandler.m_tabs[1].m_onClick = new Button.ButtonClickedEvent();
                    tabHandler.m_tabs[1].m_onClick.AddListener(__instance.OnServerListTab);
                }
            }

            __instance.m_menuAnimator.runtimeAnimatorController =
                Auga.Assets.MainMenuPrefab.GetComponent<Animator>()?.runtimeAnimatorController;

            __instance.OnSelectWorldTab();

            Object.Instantiate(
                Auga.Assets.MainMenuPrefab.GetComponentInChildren<AugaCharacterSelectPhotoBooth>(true),
                __instance.transform);

            Localization.instance.Localize(__instance.transform);
        }

        private static void SetButtonListener(Transform root, string childName, UnityAction listener)
        {
            var t = root.Find(childName);
            if (t == null) { Auga.LogWarning($"SetButtonListener: path not found: {childName}"); return; }
            var button = t.GetComponent<Button>();
            if (button == null) { Auga.LogWarning($"SetButtonListener: no Button on: {childName}"); return; }
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(listener);
        }

        private static void SetToggleListener(Transform root, string childName, UnityAction<bool> listener)
        {
            var t = root.Find(childName);
            if (t == null) return;
            var toggle = t.GetComponent<Toggle>();
            if (toggle == null) return;
            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.onValueChanged.AddListener(listener);
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.UpdateCharacterList))]
    public static class FejdStartup_UpdateCharacterList_Patch
    {
        public static void Postfix(FejdStartup __instance)
        {
            var characterSelect = __instance.GetComponentInChildren<AugaCharacterSelect>(true);
            if (characterSelect != null)
                characterSelect.UpdateCharacterList();
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnNewCharacterDone))]
    public static class FejdStartup_OnNewCharacterDone_Patch
    {
        public static void Postfix(FejdStartup __instance)
        {
            var photoBooth = __instance.GetComponentInChildren<AugaCharacterSelectPhotoBooth>(true);
            if (photoBooth != null)
                photoBooth.StartCoroutine(NewCharPhotoCoroutine(__instance, photoBooth));
        }

        private static IEnumerator NewCharPhotoCoroutine(FejdStartup instance, AugaCharacterSelectPhotoBooth photoBooth)
        {
            yield return photoBooth.TakePhoto(instance.m_profileIndex);
            instance.UpdateCharacterList();
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.ClearCharacterPreview))]
    public static class FejdStartup_ClearCharacterPreview_Patch
    {
        public static bool Prefix(FejdStartup __instance)
        {
            if (AugaCharacterSelectPhotoBooth.TakingPhotos)
            {
                Object.Destroy(__instance.m_playerInstance);
                __instance.m_playerInstance = null;
                return false;
            }
            return true;
        }
    }
}
