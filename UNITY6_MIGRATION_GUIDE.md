# Auga — Руководство по миграции на Unity 6 / Valheim 0.221+

> **Ветка:** `fix/unity-6000-compat`  
> **Дата анализа:** апрель 2026  
> **Статус:** WIP — большинство проблем решены, ряд задач остаётся

---

## Содержание

1. [Что изменилось — краткий список](#1-что-изменилось--краткий-список)
2. [Система сборки (Build System)](#2-система-сборки-build-system)
3. [Переименования и удалённые сборки](#3-переименования-и-удалённые-сборки)
4. [Изменения Valheim API](#4-изменения-valheim-api)
5. [Изменения Unity API](#5-изменения-unity-api)
6. [Паттерны обхода через Reflection](#6-паттерны-обхода-через-reflection)
7. [FejdStartup / Главное меню](#7-fejdstartup--главное-меню)
8. [Settings UI](#8-settings-ui)
9. [Unity Editor: Asset Bundles и prefab-конфликты](#9-unity-editor-asset-bundles-и-prefab-конфликты)
10. [Оставшиеся задачи](#10-оставшиеся-задачи)
11. [Шпаргалка: быстрый поиск по симптому](#11-шпаргалка-быстрый-поиск-по-симптому)

---

## 1. Что изменилось — краткий список

| Категория | Было (Valheim ≤ 0.217 / Unity 2022) | Стало (Valheim 0.221+ / Unity 6) |
|-----------|--------------------------------------|-----------------------------------|
| Формат проекта | Legacy `.csproj` с захардкоженными путями | SDK-style + `Valheim.props` |
| Pre-publicized DLLs | `assembly_valheim_publicized.dll` и др. | Оригинальные DLL + `BepInEx.AssemblyPublicizer.MSBuild` |
| `ui_lib.dll` / `Fishlabs` namespace | `using Fishlabs;` + `GuiInputField` | `gui_framework.dll` + `GUIFramework.GuiInputField` |
| `TMP_InputField` vs `GuiInputField` | `GuiInputField` (Fishlabs) | `TMP_InputField` (TextMeshPro) |
| `InventoryGui.m_selectedRecipe` | `KeyValuePair<Recipe, ItemDrop.ItemData>` (`.Key` / `.Value`) | Структура с именованными полями `.Recipe` / `.ItemData` |
| `FileHelpers.m_cloudEnabled` | Поле-переменная | `FileHelpers.CloudStorageEnabled` (свойство) |
| `DamageText.AddInworldText` | `(TextType type, float dmg, bool mySelf)` | `(TextType type, string text, bool mySelf)` |
| `Texture2D.EncodeToPNG()` | Метод расширения на Texture2D | `ImageConversion.EncodeToPNG(tex)` (статический) |
| `Texture2D.LoadImage()` | Метод расширения на Texture2D | `ImageConversion.LoadImage(tex, bytes)` (статический) |
| `GuiBar.SetBar(float)` | Публичный метод | `internal` — только через Reflection |
| `Settings` | Простой MonoBehaviour | Использует `ISettingsTab` + `TabHandler` |
| `ZInput.GetAxis(...)` для колёсика мыши | `ZInput.GetAxis("Mouse ScrollWheel")` | `Input.GetAxis("Mouse ScrollWheel")` |
| `Game.m_firstSpawn` | Поле существовало | Удалено |
| `Piece.PieceCategory.Building` | Именованный enum | Числовое приведение `(Piece.PieceCategory)2` |
| `Hud.m_pieceBarPosX` | Поле существовало | Удалено |
| Keybinding через ZInput | `ZInput.instance.m_buttons[key].m_key` → `KeyCode` | Только `Localization.GetBoundKeyString(key)` → строка |
| APIManager | Внешняя DLL | Embedded resource + `AppDomain.AssemblyResolve` |

---

## 2. Система сборки (Build System)

### 2.1 Переход на SDK-style проекты

**Было (legacy format):**
```xml
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    ...
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="assembly_valheim_publicized">
      <HintPath>M:\Code\VapokModBase\References\Valheim\0.217.30\assembly_valheim_publicized.dll</HintPath>
    </Reference>
  </ItemGroup>
```

**Стало (SDK-style):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="..\Valheim.props" />
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    ...
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BepInEx.AssemblyPublicizer.MSBuild" Version="0.4.2" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include="assembly_valheim">
      <HintPath>$(ValheimManagedDir)\assembly_valheim.dll</HintPath>
      <Publicize>true</Publicize>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
```

**Ключевые моменты:**
- `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` — обязателен, если в проекте уже есть `Properties/AssemblyInfo.cs`
- `<Nullable>disable</Nullable>` — отключает nullable-предупреждения (мод под .NET Framework 4.7.2)
- Больше не нужны предварительно публикованные `_publicized.dll` — `BepInEx.AssemblyPublicizer.MSBuild` делает это автоматически при сборке

### 2.2 Централизованный Valheim.props

Файл `Valheim.props` в корне репозитория определяет все пути. Авто-поиск через Steam реестр, ручное переопределение через `/p:ValheimDir=...`:

```xml
<ValheimDir Condition="Exists('F:\SteamLibrary\steamapps\common\Valheim')">F:\SteamLibrary\...</ValheimDir>
<!-- BepInEx DLL -->
<BepInExCoreDir>$(ValheimDir)\BepInEx\core</BepInExCoreDir>
<!-- Все Managed DLL игры -->
<ValheimManagedDir>$(ValheimDir)\valheim_Data\Managed</ValheimManagedDir>
<!-- Куда копируется мод -->
<ValheimPluginsDir>$(ValheimDir)\BepInEx\plugins</ValheimPluginsDir>
```

Сборка без параметров: `dotnet build`  
Сборка с явным путём: `dotnet build /p:ValheimDir="D:\Games\Valheim"`

---

## 3. Переименования и удалённые сборки

### 3.1 `ui_lib.dll` → `gui_framework.dll`

Сборка с пространством имён `Fishlabs` переименована. **Все** `using Fishlabs;` нужно заменить.

| Старое | Новое | Файл |
|--------|-------|------|
| `using Fishlabs;` | `using GUIFramework;` или убрать | везде |
| `Fishlabs.GuiInputField` | `GUIFramework.GuiInputField` (Minimap) | `Minimap_Setup.cs` |
| `GuiInputField` (Fishlabs) как тип поля | `TMP_InputField` | `GuiInputFieldSubmit.cs`, `AugaCraftingControls.cs`, `AugaCraftingPanel.cs`, `API.Common.cs` |

> **Важно:** В Minimap `m_nameInput` остаётся `GUIFramework.GuiInputField` — это ванильное поле Valheim.  
> В Auga-компонентах `inputAmount` / `InputAmount` переведён на `TMP_InputField`.

```csharp
// Было:
public GuiInputField inputAmount;  // Fishlabs
// Стало:
public TMP_InputField inputAmount;  // UnityEngine.UI / TMPro
```

### 3.2 Удаление JetBrains.Annotations

Атрибуты `[UsedImplicitly]`, `[CanBeNull]` убраны из всех файлов AugaUnityLib — они не нужны в рантайме и добавляли лишнюю зависимость.

```csharp
// Было:
[UsedImplicitly]
public void OnEnable() { ... }

[CanBeNull] public Image UpgradedIcon;

// Стало:
public void OnEnable() { ... }
public Image UpgradedIcon;
```

---

## 4. Изменения Valheim API

### 4.1 `InventoryGui.m_selectedRecipe` — самое массовое изменение

**Было:** `KeyValuePair<Recipe, ItemDrop.ItemData>` — поля `.Key` и `.Value`  
**Стало:** Именованная структура с полями `.Recipe` и `.ItemData`

Затронутые файлы:
- `AugaUnityLib/AugaCraftingPanel.cs`
- `AugaUnityLib/CraftingRequirementsPanel.cs`
- `Auga/PlayerInventory_Setup.cs`

```csharp
// Было:
inventoryGui.m_selectedRecipe.Key    // Recipe
inventoryGui.m_selectedRecipe.Value  // ItemDrop.ItemData

// Стало:
inventoryGui.m_selectedRecipe.Recipe    // Recipe
inventoryGui.m_selectedRecipe.ItemData  // ItemDrop.ItemData
```

**Поиск в кодовой базе:** `grep -r "m_selectedRecipe\.(Key\|Value)"` — все найденные места нужно заменить.

### 4.2 `FileHelpers.m_cloudEnabled` → `FileHelpers.CloudStorageEnabled`

```csharp
// Было:
var showSourceInfoPanel = !FileHelpers.m_cloudEnabled;

// Стало:
var showSourceInfoPanel = !FileHelpers.CloudStorageEnabled;
```

### 4.3 `DamageText.AddInworldText` — сигнатура метода

```csharp
// Было (патч на метод с float dmg):
public static void AddInworldText_Postfix(DamageText __instance, DamageText.TextType type, float dmg, bool mySelf)

// Стало (параметр dmg удалён, добавлен string text):
public static void AddInworldText_Postfix(DamageText __instance, DamageText.TextType type, string text, bool mySelf)
{
    // При необходимости парсим число из строки:
    float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var dmg);
    ...
}
```

### 4.4 Keybinding — удалён доступ к KeyCode через ZInput

```csharp
// Было (прямой доступ к KeyCode):
var keycode = ZInput.instance.m_buttons[keyName].m_key;
switch (keycode) {
    case KeyCode.Mouse0: showMouse = 0; break;
    ...
}

// Стало (только через локализованную строку):
var localizedKeyString = Localization.instance.GetBoundKeyString(keyName);
if (localizedKeyString == "Mouse0" || localizedKeyString == "LMB") showMouse = 0;
else if (localizedKeyString == "Mouse1" || localizedKeyString == "RMB") showMouse = 1;
// и т.д.
```

### 4.5 `ZInput.GetAxis` для мыши → `Input.GetAxis`

```csharp
// Было:
_lastPosition += ZInput.GetAxis("Mouse ScrollWheel");

// Стало:
_lastPosition += Input.GetAxis("Mouse ScrollWheel");
```

### 4.6 `Game.m_firstSpawn` удалено

```csharp
// Было:
if (Game.instance.m_firstSpawn) { AugaMessageLog.instance.AddArrivalLog(...); }

// Стало — всегда логируем при спавне:
AugaMessageLog.instance.AddArrivalLog(Player.m_localPlayer);
```

### 4.7 `Hud.m_pieceBarPosX` удалено

```csharp
// Было:
__instance.m_pieceBarPosX = __instance.m_pieceBarTargetPosX;

// Стало — строчку убираем, сброс позиции игра делает сама:
// m_pieceBarPosX removed in current Valheim; skipping position reset
```

### 4.8 `Piece.PieceCategory.Building` — изменение enum

```csharp
// Было:
piece.m_category == Piece.PieceCategory.Building

// Стало (имя пропало из деком., используем числовой каст):
piece.m_category == (Piece.PieceCategory)2
```

> Проверь в декомпиляции актуальной версии — если `Building` снова есть, используй имя.

### 4.9 Версионная проверка при старте плагина удалена

```csharp
// Было:
if ((Version.CurrentVersion.m_minor == 217 && Version.CurrentVersion.m_patch >= 27) || ...)
{
    // ok
} else {
    Destroy(this); return;  // убивал плагин на неподходящей версии
}

// Стало:
// Version gate removed — PTB check is no longer needed for current Valheim.
Debug.LogWarning($"===...===");
```

### 4.10 `Settings` — полный рефакторинг (ISettingsTab)

Подробно описано в разделе 8.

---

## 5. Изменения Unity API

### 5.1 `Texture2D.EncodeToPNG()` / `LoadImage()` — перенесены в `ImageConversion`

В Unity 6 эти методы расширений `Texture2D` были выделены в отдельный статический класс `UnityEngine.ImageConversion`. Прямая ссылка на `UnityEngine.ImageConversionModule.dll` из .NET Framework проекта вызывает ошибки CS1705/CS7069 из-за `ReadOnlySpan<byte>` overloads.

**Решение — Reflection wrapper:**

```csharp
internal static class ImageConversionReflection
{
    private static MethodInfo _encodeToPng;
    private static MethodInfo _loadImage;
    private static bool _resolved;

    private static void EnsureResolved()
    {
        if (_resolved) return;
        _resolved = true;
        var t = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
        _encodeToPng = t?.GetMethod("EncodeToPNG",
            BindingFlags.Public | BindingFlags.Static, null,
            new[] { typeof(Texture2D) }, null);
        _loadImage = t?.GetMethod("LoadImage",
            BindingFlags.Public | BindingFlags.Static, null,
            new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) }, null);
        // Fallback: 2-arg overload
        if (_loadImage == null)
            _loadImage = t?.GetMethod("LoadImage",
                BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(Texture2D), typeof(byte[]) }, null);
    }

    public static byte[] EncodeToPNG(Texture2D texture)
    {
        EnsureResolved();
        return (byte[])_encodeToPng?.Invoke(null, new object[] { texture });
    }

    public static void LoadImage(Texture2D texture, byte[] data)
    {
        EnsureResolved();
        var p = _loadImage?.GetParameters().Length ?? 0;
        if (p == 3) _loadImage?.Invoke(null, new object[] { texture, data, false });
        else        _loadImage?.Invoke(null, new object[] { texture, data });
    }
}
```

```csharp
// Было:
var bytes = profilePic.EncodeToPNG();
_texture.LoadImage(bytes);

// Стало:
var bytes = ImageConversionReflection.EncodeToPNG(profilePic);
ImageConversionReflection.LoadImage(_texture, bytes);
```

### 5.2 `GuiBar.SetBar(float)` стал `internal`

```csharp
// Было (в Editor-режиме для мгновенного превью):
FastBar.SetBar(fastValue / MaxValue);

// Стало — через Reflection:
private static MethodInfo _setBarMethod;
private static void SetBarViaReflection(GuiBar bar, float value)
{
    if (_setBarMethod == null)
        _setBarMethod = typeof(GuiBar).GetMethod("SetBar",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(float) }, null);
    _setBarMethod?.Invoke(bar, new object[] { value });
}
```

### 5.3 `Text` → `TMP_Text` в ряде мест

```csharp
// Было:
var text = levelDisplayX.GetComponentInChildren<Text>();

// Стало:
var text = levelDisplayX.GetComponentInChildren<TMP_Text>();
if (text != null) text.text = $"x {level - 1}";
```

### 5.4 `WorldGenerator` guard в `AugaCharacterSelect`

В Unity 6 / новом Valheim `WorldGenerator` инициализируется позже. Попытка создать `Heightmap` в главном меню без `WorldGenerator` роняла игру.

```csharp
public void Start()
{
    // Новая проверка:
    if (WorldGenerator.instance == null)
    {
        Debug.LogWarning("[Auga] PhotoBooth: WorldGenerator not available, skipping character photos.");
        TakingPhotos = false;
        return;
    }
    StartCoroutine(PhotoBoothCoroutine());
}
```

---

## 6. Паттерны обхода через Reflection

Когда Valheim делает метод/поле `internal`, есть несколько вариантов:

### Вариант A: Reflection wrapper (применён в проекте)
Подходит для редко вызываемых методов (Editor-превью, разовая инициализация).  
Пример: `SetBarViaReflection`, `ImageConversionReflection`.

### Вариант B: AssemblyPublicizer (применён в проекте)
`BepInEx.AssemblyPublicizer.MSBuild` — NuGet пакет, который при сборке автоматически делает все `internal`/`private` члены `public` в копии DLL.  
Указывается в `.csproj`:
```xml
<PackageReference Include="BepInEx.AssemblyPublicizer.MSBuild" Version="0.4.2" />
...
<Reference Include="assembly_valheim">
  <HintPath>$(ValheimManagedDir)\assembly_valheim.dll</HintPath>
  <Publicize>true</Publicize>
</Reference>
```

### Вариант C: Harmony Transpiler
Для патча логики без прямого вызова приватного метода.

---

## 7. FejdStartup / Главное меню

Это самая сложная часть миграции. Суть проблемы: в Unity 6 уничтожение GameObject через `Replace()` оставляет "мёртвые" ссылки в `FejdStartup.__instance`, а ванильный `Awake()` обращается к ним **до** нашего Postfix.

### Стратегия: Prefix + Postfix разделение

```
FejdStartup.Awake()
  │
  ├─ [Prefix] — Replace() все префабы ДО ванильного кода
  │              Сохранить hair/beard refs до замены NewCharacterPanel
  │              FixDeadFields() — создать stubs для полей,
  │              к которым ванильный код обращается до Postfix
  │
  ├─ [Vanilla Awake()] — ищет объекты по ванильным путям, не находит,
  │                       пишет null, но не падает (потому что stubs)
  │
  └─ [Postfix] — ВСЕ назначения полей __instance.m_* выполняются здесь,
                 ПОСЛЕ ванильного Awake()
```

### Null-safe хелперы

```csharp
// Null-safe Find + GetComponent с логгированием
private static T FC<T>(Transform root, string path) where T : Component
{
    if (root == null) { Auga.LogWarning($"FC<{typeof(T).Name}>: root is null (path={path})"); return null; }
    var t = root.Find(path);
    if (t == null) { Auga.LogWarning($"FC<{typeof(T).Name}>: path not found: {path}"); return null; }
    var c = t.GetComponent<T>();
    if (c == null) Auga.LogWarning($"FC<{typeof(T).Name}>: no component on: {path}");
    return c;
}

private static GameObject FO(Transform root, string path)
{
    if (root == null) { Auga.LogWarning($"FO: root is null (path={path})"); return null; }
    var t = root.Find(path);
    if (t == null) { Auga.LogWarning($"FO: path not found: {path}"); return null; }
    return t.gameObject;
}
```

### Новые поля FejdStartup (добавлены в Valheim 0.221)

```csharp
__instance.m_csFileSource = FC<TMP_Text>(charSelect, "Panel/SourceInfo/Text");
__instance.m_csSourceInfo = FC<TMP_Text>(charSelect, "Panel/SourceInfo/Text");
__instance.m_characterSelectScreen = characterSelectionNode.gameObject;
```

### Изменение типов полей

| Поле | Было | Стало |
|------|------|-------|
| `m_removeCharacterName` | `Text` (Unity legacy) | `TMP_Text` |
| `m_csName` | `Text` (Unity legacy) | `TMP_Text` |
| `m_csFileSource` | не существовало | `TMP_Text` |
| `m_csSourceInfo` | не существовало | `TMP_Text` |

### Пути в иерархии Auga-префабов

```
FejdStartup (root)
├── Menu/
│   ├── Logo
│   ├── MenuList/
│   │   ├── StartGame   (AugaMenuButton)
│   │   ├── Settings    (AugaMenuButton)
│   │   ├── Credits     (AugaMenuButton)
│   │   └── Exit        (AugaMenuButton)
│   ├── Version
│   └── DummyObjects/Dummy
├── ConnectionFailed/          ← или CharacterSelection/ConnectionFailed
├── Credits/
├── BLACK/
├── Loading/
├── CharacterSelection/
│   ├── SelectCharacter/
│   │   ├── Panel/
│   │   │   ├── Inset/ RemoveButton, NewButton, NewButtonBig
│   │   │   ├── Back, Start, ManageSaves
│   │   │   ├── DummyObjects/Dummy
│   │   │   └── SourceInfo/Text
│   │   └── RemoveCharacterDialog/ Text, ButtonYes, ButtonNo
│   ├── NewCharacterPanel/     (PlayerCustomizaton компонент)
│   └── ConnectionFailed/      (возможно)
└── StartGame/
```

---

## 8. Settings UI

### Что изменилось в Valheim 0.221

`Settings.cs` полностью переработан:
- Добавлен интерфейс `ISettingsTab` (в namespace `Valheim.SettingsGui`)
- `private List<ISettingsTab> SettingsTabs` — список реализаций по одной на вкладку
- `private void InitializeTabs()` — создаёт и наполняет SettingsTabs
- `private int m_tabsToSave` — счётчик для отслеживания когда закрыть Settings
- `OnOk()` итерирует по `SettingsTabs`, вызывает `tab.SaveTab()` для каждого, ждёт callback → `ApplyAndClose()`

**Проблема:** Auga-префаб Settings никогда не имел `ISettingsTab`-компонентов → `SettingsTabs` остаётся `null` или пустым → NPE в `OnOk`, `OnBack`, `CloseSettings`.

### Решение: Prefix + Finalizer патчи

```csharp
// 1. Инициализируем SettingsTabs как пустой список (не null!)
[HarmonyPatch(typeof(Settings), "InitializeTabs")]
public static class Settings_InitializeTabs_Patch
{
    public static bool Prefix(Settings __instance)
    {
        s_settingsTabsField.SetValue(__instance, new List<ISettingsTab>());
        return false; // пропускаем ванильный метод
    }
}

// 2. Защита Awake от NPE
[HarmonyPatch(typeof(Settings), nameof(Settings.Awake))]
public static class Settings_Awake_Patch
{
    public static Exception Finalizer(Exception __exception)
        => __exception is NullReferenceException ? null : __exception;
}

// 3. Полная замена OnOk (ванильная версия ждёт SaveTab callbacks)
[HarmonyPatch(typeof(Settings), nameof(Settings.OnOk))]
public static class Settings_OnOk_Patch
{
    public static bool Prefix(Settings __instance)
    {
        ZInput.instance?.Save();
        if (GameCamera.instance) GameCamera.instance.ApplySettings();
        if (MusicMan.instance) MusicMan.instance.ApplySettings();
        if (KeyHints.instance) KeyHints.instance.ApplySettings();
        PlatformPrefs.Save();
        s_closeSettings?.Invoke(__instance, null);
        return false; // пропускаем ванильный OnOk
    }
}
```

### Настройки: buffer-on-change паттерн (`AugaSettings_Controller.cs`)

Вместо прямой записи в `PlatformPrefs` при изменении контрола, изменения буферизируются в `Dictionary<string, Action>` и применяются только при нажатии OK:

```csharp
// Wire() — при открытии Settings: читаем PlatformPrefs → устанавливаем контролы
// Pend() — onChange → _pending[key] = () => { PlatformPrefs.SetFloat(key, value); }
// Commit() — в Prefix OnOk: применяем _pending → PlatformPrefs → GraphicsSettingsManager.ApplyStartupSettings()
```

**Ключевые исправления PlatformPrefs-ключей:**
| Старый ключ | Новый ключ |
|-------------|------------|
| `LoD` | `LodBias` |
| `TargetFrameRate` | `FPSLimit` |
| `SSAO` | `SSAO`, `SSAO_2` (два поля) |

---

## 9. Unity Editor: Asset Bundles и prefab-конфликты

### Проблема Missing Scripts

После пересборки `Unity.Auga.dll` Unity теряет связь между MonoBehaviour-компонентами в prefab'ах и их скриптами. Это происходит из-за смены GUID типов при пересборке.

**Решение:** `AugaUnity/Assets/Editor/FixMissingScripts.cs` — Editor утилита:  
`Menu: Auga → Fix Missing Scripts`

Она:
1. Force-reimport `Assets/ExternalLibraries/Unity.Auga.dll`
2. Обходит все prefab'ы в `Assets/Prefabs/`
3. Удаляет компоненты с missing scripts
4. Сохраняет prefab'ы

### Загрузка Unity.Auga.dll в Editor

В Unity 6 `.dll.meta` файлы требуют явного указания платформы. Изменение в `Unity.Auga.dll.meta`:

```yaml
# Было (Unity 2022):
platformData: []

# Стало (Unity 6):
platformData:
  - first:
      Any:
    second:
      enabled: 1
```

### Asset Bundle: обновление augaassets

После изменений в prefab'ах нужно пересобрать asset bundle через:  
`Menu: Auga → Build Asset Bundles`  
Или через AugaLauncher: `Menu: Auga → Show Launcher Window → Build Asset Bundles`

---

## 10. Оставшиеся задачи

На основе TODO-комментариев и анализа кода:

### 10.1 Hud_Setup.cs — потеряны источники стилей текста

```csharp
// TODO: augaText/augaSelectedText source GameObjects were lost; text styling skipped
```
Нужно найти откуда брать стили для текста билд-меню или захардкодить их.

### 10.2 Minimap — `GuiInputField` неоднозначность

```csharp
minimap.m_nameInput = newMap.Find("NameField").GetComponent<GUIFramework.GuiInputField>();
```
Нужно проверить, что `NameField` в Auga-префабе Minimap действительно имеет `GUIFramework.GuiInputField`.

### 10.3 Settings — dropdown и slider wire-up

`AugaSettings_Controller.cs` содержит полный `Wire()`, но нужно проверить реальную структуру Auga Settings prefab — все пути к слайдерам и дропдаунам должны совпадать.

### 10.4 Кнопки меню показывают "LABEL"

```csharp
// Кнопки MenuList — вложенные prefab-экземпляры AugaMenuButton.
// Переопределения из MainMenu.prefab не применяются без пересборки asset bundle
SetMenuButtonText(mainMenu, "MenuList/StartGame", "$menu_start");
```
Решение: пересобрать asset bundle с обновлёнными текстами, **или** оставить программный override.

### 10.5 ConnectionFailed путь неоднозначен

```csharp
var connectionFailed = __instance.transform.Find("CharacterSelection/ConnectionFailed")
                    ?? __instance.transform.Find("ConnectionFailed");
```
Нужно определить точный путь в Auga-префабе.

### 10.6 `m_connectionFailedError` и `m_versionLabel` — null

В Auga-префабе используется TMP_Text, но поля FejdStartup объявлены как `Text` (legacy). Это косметика — поля остаются null, что не вызывает краш, но может давать пустые тексты.

---

## 11. Шпаргалка: быстрый поиск по симптому

| Симптом / Ошибка | Причина | Решение |
|-----------------|---------|---------|
| `NullReferenceException` в `Settings.Awake()` | SettingsTabs = null | Патч `InitializeTabs` → пустой список |
| `NullReferenceException` в `Settings.OnOk()` | Ждёт `SaveTab` callbacks | Полная замена `OnOk` через Prefix |
| `MissingMethodException: GuiInputField` | DLL переименована | `gui_framework.dll`, убрать `using Fishlabs;` |
| `m_selectedRecipe.Key` / `.Value` не существует | API изменился | `.Recipe` / `.ItemData` |
| `FileHelpers.m_cloudEnabled` не найдено | Переименовано | `FileHelpers.CloudStorageEnabled` |
| Crash при открытии меню персонажа | WorldGenerator == null | Guard check перед PhotoBoothCoroutine |
| Crash при Replace() в FejdStartup | Мёртвые ссылки в Unity 6 | `FixDeadFields()` + Prefix+Postfix split |
| Missing Scripts в prefab | GUID рассинхронизация после пересборки DLL | `Auga → Fix Missing Scripts` |
| `CS1705` / `CS7069` при компиляции | Конфликт ImageConversion.dll | `ImageConversionReflection` wrapper |
| Кнопки меню показывают "LABEL" | Текст не записан в prefab | `SetMenuButtonText()` в Postfix |
| Keybinding показывает "W" для всех кнопок | Имя кнопки не передавалось | `AutomaticKeyName = GO.name` |
| `GuiBar.SetBar` недоступен | Стал internal | `SetBarViaReflection()` |
| `EncodeToPNG` / `LoadImage` не найден на Texture2D | Перенесён в ImageConversion | `ImageConversionReflection` |
| `Game.m_firstSpawn` не существует | Поле удалено | Убрать проверку, всегда логировать |
| `m_pieceBarPosX` не существует | Поле удалено | Убрать присваивание |
| `Piece.PieceCategory.Building` не компилируется | Enum изменился | `(Piece.PieceCategory)2` |

---

## Рабочий процесс для нового файла / компонента

При портировании любого нового файла в Unity 6:

1. **Убрать `using Fishlabs;`** → заменить на `using GUIFramework;` или убрать
2. **Убрать `using JetBrains.Annotations;`** и атрибуты `[UsedImplicitly]`, `[CanBeNull]`
3. **Заменить `GuiInputField`** на `TMP_InputField` (если это Auga-компонент) или `GUIFramework.GuiInputField` (если это ванильное поле)
4. **Найти `m_selectedRecipe.Key` / `.Value`** → заменить на `.Recipe` / `.ItemData`
5. **Найти `EncodeToPNG()` / `LoadImage()`** прямо на Texture2D → `ImageConversionReflection`
6. **Найти `m_cloudEnabled`** → `CloudStorageEnabled`
7. **Проверить все Unity legacy `Text` поля** → возможно нужен `TMP_Text`
8. **Если есть HarmonyPatch на метод** — проверить сигнатуру через декомпилятор (dnSpy / ILSpy)

---

*Документ составлен на основе анализа ветки `fix/unity-6000-compat`. При возникновении новых проблем — добавить в таблицу в разделе 11.*
