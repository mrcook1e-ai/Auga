using System;
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
        // Переданные из Prefix в Postfix данные о волосах/бороде
        private static ItemDrop _originalNoHair;
        private static ItemDrop _originalNoBeard;

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

            // Сохраняем логотип до замены меню
            var originalLogo = __instance.transform.Find("Menu/Logo");
            if (originalLogo != null)
                originalLogo.SetParent(__instance.transform, true);

            // Заменяем все префабы — должно произойти ДО ванильного Awake(),
            // чтобы ванильный код нашёл нужные объекты по именам.
            var mainMenu = __instance.Replace("Menu", Auga.Assets.MainMenuPrefab);
            if (mainMenu == null) { Auga.LogError("Failed to replace Menu"); return; }
            if (originalLogo != null)
                originalLogo.SetParent(mainMenu, true);

            __instance.Replace("ConnectionFailed", Auga.Assets.MainMenuPrefab);
            __instance.Replace("Credits", Auga.Assets.MainMenuPrefab);
            __instance.Replace("BLACK", Auga.Assets.MainMenuPrefab);
            __instance.Replace("Loading", Auga.Assets.MainMenuPrefab);
            __instance.Replace("CharacterSelection/SelectCharacter", Auga.Assets.MainMenuPrefab);

            // Сохраняем hair/beard из ванильного NewCharacterPanel ДО его замены
            var oldCustomizaton = __instance.m_newCharacterPanel != null
                ? __instance.m_newCharacterPanel.GetComponent<PlayerCustomizaton>()
                : null;
            _originalNoHair = oldCustomizaton?.m_noHair;
            _originalNoBeard = oldCustomizaton?.m_noBeard;

            __instance.Replace("CharacterSelection/NewCharacterPanel", Auga.Assets.MainMenuPrefab);
            __instance.Replace("StartGame", Auga.Assets.MainMenuPrefab);

            // КРИТИЧНО: Replace() уничтожает оригинальные GO, превращая инспекторные
            // ссылки в "dead" объекты. Ванильный Awake() обращается к некоторым из
            // них (m_crossplayServerToggle, m_serverOptions, m_menuList и др.) ДО
            // нашего Postfix. В Unity 6 доступ к .gameObject на destroyed-компоненте
            // бросает NPE. Создаём stubs прямо здесь, чтобы ванильный код не упал.
            FixDeadFields(__instance);
        }

        public static void Postfix(FejdStartup __instance)
        {
            // Все назначения полей выполняются ЗДЕСЬ, ПОСЛЕ ванильного Awake(),
            // который перезаписывал наши значения нулями (т.к. ванильные пути
            // не совпадают со структурой Auga-префабов).

            // ---- Menu ----
            var mainMenu = __instance.transform.Find("Menu");
            if (mainMenu != null)
            {
                __instance.m_mainMenu = mainMenu.gameObject;
                __instance.m_menuList = FO(mainMenu, "MenuList");
                __instance.m_menuSelectedButton = FC<Button>(mainMenu, "MenuList/StartGame");
                // m_versionLabel: Menu/Version использует legacy Text (не TMP_Text) — поле остаётся null
                __instance.m_betaText = FO(mainMenu, "DummyObjects/Dummy");
                __instance.m_ndaPanel = FO(mainMenu, "DummyObjects/Dummy");
                SetButtonListener(mainMenu, "MenuList/StartGame", __instance.OnStartGame);
                SetButtonListener(mainMenu, "MenuList/Settings", __instance.OnButtonSettings);
                SetButtonListener(mainMenu, "MenuList/Credits", __instance.OnCredits);
                SetButtonListener(mainMenu, "MenuList/Exit", __instance.OnAbort);

                // Кнопки MenuList — вложенные prefab-экземпляры AugaMenuButton.
                // Переопределения из MainMenu.prefab не применяются без пересборки asset bundle,
                // поэтому устанавливаем тексты программно через GetComponentInChildren.
                SetMenuButtonText(mainMenu, "MenuList/StartGame", "$menu_start");
                SetMenuButtonText(mainMenu, "MenuList/Settings", "$menu_settings");
                SetMenuButtonText(mainMenu, "MenuList/Credits", "$menu_credits");
                SetMenuButtonText(mainMenu, "MenuList/Exit", "$menu_exit");
            }

            // ---- ConnectionFailed ----
            // В Auga ConnectionFailed находится под CharacterSelection, а не в корне
            var connectionFailed = __instance.transform.Find("CharacterSelection/ConnectionFailed")
                                ?? __instance.transform.Find("ConnectionFailed");
            if (connectionFailed != null)
            {
                __instance.m_connectionFailedPanel = connectionFailed.gameObject;
                // Text компонент — legacy Text, не TMP_Text; поле m_connectionFailedError остаётся null (косметика)
                SetButtonListener(connectionFailed, "ButtonYes", __instance.OnConnectionFailedOk);
            }

            // ---- Credits ----
            var credits = __instance.transform.Find("Credits");
            if (credits != null)
            {
                __instance.m_creditsPanel = credits.gameObject;
                __instance.m_creditsList = credits.Find("ContactInfo") as RectTransform;
                SetButtonListener(credits, "Back-panel/ButtonSettings", __instance.OnCreditsBack);
            }

            // ---- Loading ----
            var loading = __instance.transform.Find("Loading");
            if (loading != null)
            {
                __instance.m_loading = loading.gameObject;
                __instance.m_loading.SetActive(false);
            }

            // ---- CharacterSelectScreen ----
            // m_characterSelectScreen — это весь CharacterSelection-узел. Используется
            // в HideAll(), ShowCharacterSelection(), UpdateCamera() и т.д. Ванильный код
            // не находит его сам (путь не совпадает), поэтому назначаем явно.
            var characterSelectionNode = __instance.transform.Find("CharacterSelection");
            if (characterSelectionNode != null)
                __instance.m_characterSelectScreen = characterSelectionNode.gameObject;

            // ---- SelectCharacter ----
            // Реальная структура Auga SelectCharacter:
            //   SelectCharacter
            //   ├── Panel
            //   │   ├── Inset / RemoveButton, NewButton, NewButtonBig
            //   │   ├── Back
            //   │   ├── Start
            //   │   ├── ManageSaves
            //   │   ├── DummyObjects
            //   │   └── SourceInfo / Text
            //   └── RemoveCharacterDialog / Text, ButtonYes, ButtonNo
            var charSelect = __instance.transform.Find("CharacterSelection/SelectCharacter");
            if (charSelect != null)
            {
                __instance.m_selectCharacterPanel = charSelect.gameObject;
                __instance.m_removeCharacterDialog = FO(charSelect, "RemoveCharacterDialog");
                __instance.m_removeCharacterName = FC<TMP_Text>(charSelect, "RemoveCharacterDialog/Text");
                __instance.m_csRemoveButton = FC<Button>(charSelect, "Panel/Inset/RemoveButton");
                __instance.m_csStartButton = FC<Button>(charSelect, "Panel/Start");
                __instance.m_csNewButton = FC<Button>(charSelect, "Panel/Inset/NewButton");
                __instance.m_csNewBigButton = FC<Button>(charSelect, "Panel/Inset/NewButtonBig");
                __instance.m_csLeftButton = FC<Button>(charSelect, "Panel/DummyObjects/Dummy");
                __instance.m_csRightButton = FC<Button>(charSelect, "Panel/DummyObjects/Dummy");
                __instance.m_csName = FC<TMP_Text>(charSelect, "Panel/DummyObjects/Dummy");
                __instance.m_csFileSource = FC<TMP_Text>(charSelect, "Panel/SourceInfo/Text");
                __instance.m_csSourceInfo = FC<TMP_Text>(charSelect, "Panel/SourceInfo/Text");
                SetButtonListener(charSelect, "Panel/Inset/RemoveButton", __instance.OnCharacterRemove);
                SetButtonListener(charSelect, "Panel/Inset/NewButton", __instance.OnCharacterNew);
                SetButtonListener(charSelect, "Panel/Inset/NewButtonBig", __instance.OnCharacterNew);
                SetButtonListener(charSelect, "Panel/Back", __instance.OnSelelectCharacterBack);
                SetButtonListener(charSelect, "Panel/Start", __instance.OnCharacterStart);
                SetButtonListener(charSelect, "Panel/ManageSaves", () => __instance.OnManageSaves(1));
                SetButtonListener(charSelect, "RemoveCharacterDialog/ButtonYes", __instance.OnButtonRemoveCharacterYes);
                SetButtonListener(charSelect, "RemoveCharacterDialog/ButtonNo", __instance.OnButtonRemoveCharacterNo);

                // Тексты кнопок SelectCharacter — вложенные AugaMenuButton с дефолтом "LABEL"
                SetMenuButtonText(charSelect, "Panel/Inset/RemoveButton", "$menu_remove");
                SetMenuButtonText(charSelect, "Panel/Inset/NewButton", "$menu_new");
                SetMenuButtonText(charSelect, "Panel/Inset/NewButtonBig", "$menu_new");
                SetMenuButtonText(charSelect, "Panel/Back", "$menu_back");
                SetMenuButtonText(charSelect, "Panel/Start", "$menu_start");
                SetMenuButtonText(charSelect, "Panel/ManageSaves", "$menu_managesaves");
            }

            // ---- NewCharacterPanel ----
            var newCharacter = __instance.transform.Find("CharacterSelection/NewCharacterPanel");
            if (newCharacter != null)
            {
                var newPlayerCustomization = newCharacter.GetComponent<PlayerCustomizaton>();
                if (newPlayerCustomization != null)
                {
                    newPlayerCustomization.m_noHair = _originalNoHair;
                    newPlayerCustomization.m_noBeard = _originalNoBeard;
                }
                __instance.m_newCharacterPanel = newCharacter.gameObject;
                __instance.m_csNewCharacterDone = FC<Button>(newCharacter, "Content/Done")
                    ?? FC<Button>(newCharacter, "Done");
                __instance.m_newCharacterError = FO(newCharacter, "Content/NameExistsWarning");
                __instance.m_csNewCharacterName = FC<GUIFramework.GuiInputField>(newCharacter, "Content/CharacterName")
                    ?? FC<GUIFramework.GuiInputField>(newCharacter, "CharacterName");
                SetButtonListener(newCharacter, "Content/Done", () => __instance.OnNewCharacterDone(true));
                SetButtonListener(newCharacter, "Content/Cancel", __instance.OnNewCharacterCancel);

                // Тексты кнопок и лейблов NewCharacterPanel
                SetMenuButtonText(newCharacter, "Content/Done", "$menu_done");
                SetMenuButtonText(newCharacter, "Content/Cancel", "$menu_cancel");

                // Лейблы GradientSlider — используют дочерний "Label" (legacy Text) или "TMP Label"
                SetLabelText(newCharacter, "Content/SkinColor", "$menu_skintone");
                SetLabelText(newCharacter, "Content/HairColor", "$menu_hairtone");
                SetLabelText(newCharacter, "Content/HairTone", "$menu_hairtone");

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
            var startGame = __instance.transform.Find("StartGame");
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

            // ---- Animator ----
            if (__instance.m_menuAnimator != null)
            {
                __instance.m_menuAnimator.runtimeAnimatorController =
                    Auga.Assets.MainMenuPrefab.GetComponent<Animator>()?.runtimeAnimatorController;
            }

            Localization.instance.Localize(__instance.transform);

            // ---- Stub-компоненты для DEAD-полей ----
            // Поля m_patchLogScroll, m_serverOptionsButton, m_crossplayServerToggle и др. —
            // это новые поля Valheim которых нет в Auga. После Replace() старые Unity-объекты
            // уничтожены, но C#-ссылки остались ("dead"). В Unity 6 обращение .gameObject на
            // destroyed-компонент бросает NPE. Создаём живые stub-компоненты на скрытом GO.
            FixDeadFields(__instance);

            // ---- PhotoBooth ----
            // Запускаем ПОСЛЕ того как FejdStartup.instance и m_mainCamera инициализированы
            // ванильным Awake() — иначе GetCamera() бросает NPE.
            try
            {
                UnityEngine.Object.Instantiate(
                    Auga.Assets.MainMenuPrefab.GetComponentInChildren<AugaCharacterSelectPhotoBooth>(true),
                    __instance.transform);
            }
            catch (Exception e)
            {
                Auga.LogWarning($"PhotoBooth instantiate failed: {e.Message}");
            }

            // OnSelectWorldTab вызывается здесь, когда все поля уже инициализированы.
            try
            {
                __instance.OnSelectWorldTab();
            }
            catch (Exception e)
            {
                Auga.LogWarning($"OnSelectWorldTab failed: {e.Message}");
            }
        }

        // Универсальный авто-фиксер мёртвых и null-полей.
        //
        // Проблема: Auga.Replace() уничтожает оригинальные Unity-объекты, но у FejdStartup
        // есть поля, которые на них ссылались. В Unity 6 доступ к .gameObject на destroyed-
        // компоненте бросает NPE. При обновлении Valheim появляются новые такие поля.
        //
        // Решение: рефлексией перебираем все поля FejdStartup. Если поле:
        //   - имеет тип Component (или его наследник)  → stub GO + AddComponent нужного типа
        //   - имеет тип GameObject                     → пустой stub GO
        //   - содержит dead ИЛИ null ссылку            → обрабатываем; живые пропускаем
        //
        // Вызывается ДВАЖДЫ:
        //   1. В конце Prefix — ДО ванильного Awake, чтобы m_crossplayServerToggle,
        //      m_serverOptions, m_menuList, m_characterSelectScreen и др. были живыми
        //      когда ванильный код к ним обращается.
        //   2. В конце Postfix — после наших назначений, чтобы добить оставшиеся null/dead.
        //
        // Каждое поле получает свой GO → решает "один Selectable на объект" для Button/Toggle.
        private static void FixDeadFields(FejdStartup instance)
        {
            var bindFlags = System.Reflection.BindingFlags.Instance
                          | System.Reflection.BindingFlags.NonPublic
                          | System.Reflection.BindingFlags.Public;

            foreach (var field in typeof(FejdStartup).GetFields(bindFlags))
            {
                var fieldType = field.FieldType;
                bool isComponent = typeof(Component).IsAssignableFrom(fieldType);
                bool isGameObject = fieldType == typeof(GameObject);

                if (!isComponent && !isGameObject) continue;

                var val = field.GetValue(instance) as UnityEngine.Object;

                // Живой объект → пропускаем
                if (val != null && (bool)val) continue;

                // null ИЛИ dead → нужен stub
                var stub = new GameObject($"_AugaStub_{field.Name}");
                stub.SetActive(false);
                stub.transform.SetParent(instance.transform, false);

                if (isGameObject)
                {
                    field.SetValue(instance, stub);
                    Auga.LogWarning($"FixDeadFields: dummy GO for {field.Name}");
                }
                else
                {
                    try
                    {
                        var comp = stub.AddComponent(fieldType);
                        field.SetValue(instance, comp);
                        Auga.LogWarning($"FixDeadFields: stubbed {field.Name} ({fieldType.Name})");
                    }
                    catch (Exception ex)
                    {
                        // AddComponent не поддерживает абстрактные типы или специальные
                        // требования — уничтожаем stub, поле остаётся dead/null.
                        // Код использующий это поле защищён SafeHide/null-check.
                        UnityEngine.Object.Destroy(stub);
                        Auga.LogWarning($"FixDeadFields: cannot stub {field.Name} ({fieldType.Name}): {ex.Message}");
                    }
                }
            }
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

        // Находит TMP_Text в кнопке (вложенный prefab) и устанавливает локализационный ключ.
        // Вызывается после Localize() поэтому текст сразу переводится движком.
        private static void SetMenuButtonText(Transform root, string path, string locKey)
        {
            var t = root.Find(path);
            if (t == null) { Auga.LogWarning($"SetMenuButtonText: path not found: {path}"); return; }
            var tmp = t.GetComponentInChildren<TMP_Text>(true);
            if (tmp == null) { Auga.LogWarning($"SetMenuButtonText: no TMP_Text in: {path}"); return; }
            tmp.text = locKey;
        }

        // Устанавливает текст на лейбле (legacy Text или TMP_Text) — для GradientSlider и подобных
        private static void SetLabelText(Transform root, string path, string locKey)
        {
            var t = root.Find(path);
            if (t == null) return;
            // Пробуем TMP_Text сначала (приоритетнее), потом legacy Text
            var tmp = t.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) { tmp.text = locKey; return; }
            var legacyText = t.GetComponentInChildren<Text>(true);
            if (legacyText != null) legacyText.text = locKey;
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

    // HideAll() итерирует по списку полей и вызывает SetActive(false) на каждом.
    // Первый же null/dead объект вызывает NPE → все последующие SetActive не выполняются
    // → часть панелей остаётся видимой. Заменяем метод полностью безопасной версией.
    [HarmonyPatch(typeof(FejdStartup), "HideAll")]
    public static class FejdStartup_HideAll_Patch
    {
        public static bool Prefix(FejdStartup __instance)
        {
            // Безопасный HideAll: Unity operator bool возвращает false для null/dead объектов.
            // Это точная копия полей из FejdStartup.HideAll() (decompiled 0.221.12).
            SafeHide(__instance.m_worldVersionPanel);
            SafeHide(__instance.m_playerVersionPanel);
            SafeHide(__instance.m_newGameVersionPanel);
            SafeHide(__instance.m_loading);
            SafeHide(__instance.m_pleaseWait);
            SafeHide(__instance.m_characterSelectScreen);
            SafeHide(__instance.m_creditsPanel);
            SafeHide(__instance.m_startGamePanel);
            SafeHide(__instance.m_createWorldPanel);
            // m_serverOptions — Component, не GameObject; безопасный доступ через оператор bool
            if (__instance.m_serverOptions)
                __instance.m_serverOptions.gameObject.SetActive(false);
            SafeHide(__instance.m_mainMenu);
            SafeHide(__instance.m_ndaPanel);
            SafeHide(__instance.m_betaText);
            return false; // ванильный HideAll пропускаем
        }

        private static void SafeHide(GameObject go)
        {
            // Unity operator bool возвращает false для C#-null И для destroyed GO
            if (go) go.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "SetupGui")]
    public static class FejdStartup_SetupGui_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    [HarmonyPatch(typeof(FejdStartup), "Update")]
    public static class FejdStartup_Update_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    [HarmonyPatch(typeof(FejdStartup), "ShowCharacterSelection")]
    public static class FejdStartup_ShowCharacterSelection_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    [HarmonyPatch(typeof(FejdStartup), "ShowStartGame")]
    public static class FejdStartup_ShowStartGame_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    [HarmonyPatch(typeof(FejdStartup), "UpdateWorldList")]
    public static class FejdStartup_UpdateWorldList_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.UpdateCharacterList))]
    public static class FejdStartup_UpdateCharacterList_Patch
    {
        // Постфикс не вызывается если ванильный метод бросил исключение,
        // поэтому используем Finalizer — он вызывается всегда.
        public static Exception Finalizer(FejdStartup __instance, Exception __exception)
        {
            var characterSelect = __instance.GetComponentInChildren<AugaCharacterSelect>(true);
            if (characterSelect != null)
                characterSelect.UpdateCharacterList();
            // Подавляем NPE от null-полей (m_csName, m_csLeftButton и т.д.) которые
            // Auga заменяет своим AugaCharacterSelect-компонентом.
            return __exception is NullReferenceException ? null : __exception;
        }
    }

    // OnCharacterNew / OnCharacterRemove обращаются к полям которые null/dead в Auga
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnCharacterNew))]
    public static class FejdStartup_OnCharacterNew_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnCharacterRemove))]
    public static class FejdStartup_OnCharacterRemove_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    // CharacterPortraitsController.Update() спамит NPE каждый кадр —
    // использует PostProcessingBehaviour/DepthOfField которых нет в Unity 6,
    // а также m_playerInstance может быть null. Отключаем целиком.
    [HarmonyPatch(typeof(AugaUnity.CharacterPortraitsController), "Update")]
    public static class CharacterPortraitsController_Update_Patch
    {
        public static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(AugaUnity.CharacterPortraitsController), "Awake")]
    public static class CharacterPortraitsController_Awake_Patch
    {
        public static bool Prefix() => false;
    }

    // AugaCharacterSelectPhotoBooth.Awake вызывает CharacterPortraitsController.GetCamera()
    // который использует DepthOfField и PostProcessingBehaviour — в Unity 6 они отсутствуют.
    // Результат: _camera = null, PhotoBoothCoroutine падает при TakePhoto.
    // Отключаем Awake и Start полностью (фотографии персонажей недоступны в Unity 6 без PP-стека).
    [HarmonyPatch(typeof(AugaUnity.AugaCharacterSelectPhotoBooth), "Awake")]
    public static class AugaCharacterSelectPhotoBooth_Awake_Patch
    {
        public static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(AugaUnity.AugaCharacterSelectPhotoBooth), "Start")]
    public static class AugaCharacterSelectPhotoBooth_Start_Patch
    {
        public static bool Prefix() => false;
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
            // Всегда разрешаем vanilla ClearCharacterPreview выполниться полностью.
            // Ранее при TakingPhotos=true мы возвращали false и вручную уничтожали
            // только m_playerInstance, пропуская полную очистку terrain/Heightmap.
            // Это приводило к накоплению Heightmap-объектов в статическом списке,
            // из-за чего ClutterSystem бросал NullReferenceException каждый кадр.
            return true;
        }
    }
}
