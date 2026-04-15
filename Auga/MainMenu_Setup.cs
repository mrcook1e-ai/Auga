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
        public static void Prefix(FejdStartup __instance)
        {
            ZInput.Initialize();

            __instance.m_settingsPrefab = Auga.Assets.SettingsPrefab;

            var originalLogo = __instance.transform.Find("Menu/Logo");
            if (originalLogo != null)
                originalLogo.SetParent(__instance.transform, true);

            var mainMenu = __instance.Replace("Menu", Auga.Assets.MainMenuPrefab);
            if (originalLogo != null)
                originalLogo.SetParent(mainMenu, true);

            __instance.m_mainMenu = mainMenu.gameObject;
            __instance.m_menuList = mainMenu.Find("MenuList").gameObject;
            __instance.m_menuSelectedButton = mainMenu.Find("MenuList/StartGame").GetComponent<Button>();
            __instance.m_versionLabel = mainMenu.Find("Version").GetComponent<TMP_Text>();
            __instance.m_betaText = mainMenu.Find("DummyObjects/Dummy").gameObject;
            __instance.m_ndaPanel = mainMenu.Find("DummyObjects/Dummy").gameObject;
            SetButtonListener(__instance.m_mainMenu.transform, "MenuList/StartGame", __instance.OnStartGame);
            SetButtonListener(__instance.m_mainMenu.transform, "MenuList/Settings", __instance.OnButtonSettings);
            SetButtonListener(__instance.m_mainMenu.transform, "MenuList/Credits", __instance.OnCredits);
            SetButtonListener(__instance.m_mainMenu.transform, "MenuList/Exit", __instance.OnAbort);

            var connectionFailedDialog = __instance.Replace("ConnectionFailed", Auga.Assets.MainMenuPrefab);
            __instance.m_connectionFailedPanel = connectionFailedDialog.gameObject;
            __instance.m_connectionFailedError = connectionFailedDialog.Find("Text").GetComponent<TMP_Text>();
            SetButtonListener(connectionFailedDialog, "ButtonYes", __instance.OnConnectionFailedOk);

            var credits = __instance.Replace("Credits", Auga.Assets.MainMenuPrefab);
            __instance.m_creditsPanel = credits.gameObject;
            __instance.m_creditsList = (RectTransform)credits.Find("ContactInfo");
            SetButtonListener(__instance.m_creditsPanel.transform, "Back-panel/ButtonSettings", __instance.OnCreditsBack);

            __instance.Replace("BLACK", Auga.Assets.MainMenuPrefab);

            __instance.m_loading = __instance.Replace("Loading", Auga.Assets.MainMenuPrefab).gameObject;
            __instance.m_loading.SetActive(false);

            var charSelect = __instance.Replace("CharacterSelection/SelectCharacter", Auga.Assets.MainMenuPrefab);
            __instance.m_selectCharacterPanel = charSelect.gameObject;
            __instance.m_removeCharacterDialog = charSelect.Find("RemoveCharacterDialog").gameObject;
            __instance.m_removeCharacterName = charSelect.Find("RemoveCharacterDialog/Text").GetComponent<TMP_Text>();
            __instance.m_csRemoveButton = charSelect.Find("Panel/Inset/RemoveButton").GetComponent<Button>();
            __instance.m_csStartButton = charSelect.Find("Panel/Start").GetComponent<Button>();
            __instance.m_csNewButton = charSelect.Find("Panel/Inset/NewButton").GetComponent<Button>();
            __instance.m_csNewBigButton = charSelect.Find("Panel/Inset/NewButtonBig").GetComponent<Button>();
            __instance.m_csLeftButton = charSelect.Find("Panel/DummyObjects/Dummy").GetComponent<Button>();
            __instance.m_csRightButton = charSelect.Find("Panel/DummyObjects/Dummy").GetComponent<Button>();
            __instance.m_csName = charSelect.Find("Panel/DummyObjects/Dummy").GetComponent<TMP_Text>();
            SetButtonListener(charSelect, "Panel/Inset/RemoveButton", __instance.OnCharacterRemove);
            SetButtonListener(charSelect, "Panel/Inset/NewButton", __instance.OnCharacterNew);
            SetButtonListener(charSelect, "Panel/Inset/NewButtonBig", __instance.OnCharacterNew);
            SetButtonListener(charSelect, "Panel/Back", __instance.OnSelelectCharacterBack);
            SetButtonListener(charSelect, "Panel/Start", __instance.OnCharacterStart);
            SetButtonListener(charSelect, "Panel/ManageSaves", () => __instance.OnManageSaves(1));
            SetButtonListener(charSelect, "RemoveCharacterDialog/ButtonYes", __instance.OnButtonRemoveCharacterYes);
            SetButtonListener(charSelect, "RemoveCharacterDialog/ButtonNo", __instance.OnButtonRemoveCharacterNo);

            var oldPlayerCustomizaton = __instance.m_newCharacterPanel.GetComponent<PlayerCustomizaton>();
            var originalNoHair = oldPlayerCustomizaton.m_noHair;
            var originalNoBeard = oldPlayerCustomizaton.m_noBeard;

            var newCharacter = __instance.Replace("CharacterSelection/NewCharacterPanel", Auga.Assets.MainMenuPrefab);
            var newPlayerCustomization = newCharacter.GetComponent<PlayerCustomizaton>();
            newPlayerCustomization.m_noHair = originalNoHair;
            newPlayerCustomization.m_noBeard = originalNoBeard;
            __instance.m_newCharacterPanel = newCharacter.gameObject;
            __instance.m_csNewCharacterDone = newCharacter.Find("Panel/Done").GetComponent<Button>();
            __instance.m_newCharacterError = newCharacter.Find("Panel/Content/NameExistsWarning").gameObject;
            __instance.m_csNewCharacterName = newCharacter.Find("Panel/Content/CharacterName").GetComponent<GUIFramework.GuiInputField>();
            SetButtonListener(newCharacter, "Panel/Done", () => __instance.OnNewCharacterDone(true));
            SetButtonListener(newCharacter, "Panel/Cancel", __instance.OnNewCharacterCancel);

            {
                var toggle = newCharacter.Find("Panel/Content/ToggleGroup/Toggle_Female").GetComponent<Toggle>();
                toggle.onValueChanged = new Toggle.ToggleEvent();
                toggle.onValueChanged.AddListener((on) => { if (on) newPlayerCustomization.SetPlayerModel(1); });
                toggle.onValueChanged.AddListener((on) => toggle.transform.GetChild(1).gameObject.SetActive(on));
            }
            {
                var toggle = newCharacter.Find("Panel/Content/ToggleGroup/Toggle_Male").GetComponent<Toggle>();
                toggle.onValueChanged = new Toggle.ToggleEvent();
                toggle.onValueChanged.AddListener((on) => { if (on) newPlayerCustomization.SetPlayerModel(0); });
                toggle.onValueChanged.AddListener((on) => toggle.transform.GetChild(1).gameObject.SetActive(on));
            }

            var startGame = __instance.Replace("StartGame", Auga.Assets.MainMenuPrefab);
            __instance.m_startGamePanel = startGame.gameObject;
            __instance.m_createWorldPanel = startGame.Find("NewWorldDialog").gameObject;
            __instance.m_serverListPanel = startGame.Find("Panel/JoinPanel").gameObject;
            __instance.m_publicServerToggle = startGame.Find("Panel/WorldPanel/CheckboxRow/StartPublicGameToggle").GetComponent<Toggle>();
            __instance.m_openServerToggle = startGame.Find("Panel/WorldPanel/CheckboxRow/StartServerToggle").GetComponent<Toggle>();
            __instance.m_serverPassword = startGame.Find("Panel/WorldPanel/ServerPassword").GetComponent<GUIFramework.GuiInputField>();
            __instance.m_passwordError = startGame.Find("Panel/WorldPanel/ServerPassword/Tooltip/ErrorText").GetComponent<TMP_Text>();
            __instance.m_worldListRoot = startGame.Find("Panel/WorldPanel/ScrollRect/ItemList").GetComponent<RectTransform>();
            __instance.m_worldListElement = Auga.Assets.WorldListElement;
            __instance.m_worldListEnsureVisible = startGame.Find("Panel/WorldPanel/ScrollRect").GetComponent<ScrollRectEnsureVisible>();
            __instance.m_worldListElementStep = 30;
            __instance.m_newWorldName = startGame.Find("NewWorldDialog/WorldName").GetComponent<GUIFramework.GuiInputField>();
            __instance.m_newWorldSeed = startGame.Find("NewWorldDialog/WorldSeed").GetComponent<GUIFramework.GuiInputField>();
            __instance.m_newWorldDone = startGame.Find("NewWorldDialog/Done").GetComponent<Button>();
            __instance.m_worldStart = startGame.Find("Panel/WorldPanel/Start").GetComponent<Button>();
            __instance.m_worldRemove = startGame.Find("Panel/WorldPanel/RemoveButton").GetComponent<Button>();
            __instance.m_removeWorldDialog = startGame.Find("RemoveWorldDialog").gameObject;
            __instance.m_removeWorldName = startGame.Find("RemoveWorldDialog/Text").GetComponent<TMP_Text>();
            __instance.m_worldListPanel = startGame.Find("Panel/WorldPanel").gameObject;

            // Server browser (m_serverListRoot, m_filterInputField, m_joinIPPanel etc.) was
            // completely redesigned in current Valheim — handled now by ServerOptionsGUI.
            // TODO: wire up Auga server browser elements if needed.

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

            __instance.m_menuAnimator.runtimeAnimatorController = Auga.Assets.MainMenuPrefab.GetComponent<Animator>().runtimeAnimatorController;

            // Cloud/file source display fields
            __instance.m_csFileSource = charSelect.Find("Panel/DummyObjects/Dummy").GetComponent<TMP_Text>();
            __instance.m_csSourceInfo = charSelect.Find("Panel/DummyObjects/Dummy").GetComponent<TMP_Text>();

            __instance.OnSelectWorldTab();

            Object.Instantiate(Auga.Assets.MainMenuPrefab.GetComponentInChildren<AugaCharacterSelectPhotoBooth>(true), __instance.transform);

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
