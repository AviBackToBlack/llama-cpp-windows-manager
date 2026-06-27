# Localization Implementation Plan — LlamaCppWindowsManager

## Overview

Add multi-language UI support via JSON resource files. The app has zero localization infrastructure today — all ~350 user-facing strings are hardcoded C# literals and XAML attributes. This plan covers infrastructure, English extraction, language switching, and a Bulgarian translation pack.

**Target:** net8.0-windows WPF, `PublishSingleFile=true`, UI built programmatically via factory classes (minimal XAML).

---

## Architecture Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Resource format | Flat JSON key-value dictionary | Works with single-file publish as `EmbeddedResource`; easy to edit without Visual Studio; community contributors can send a single JSON file |
| Lookup API | `static class Loc` with `Loc.T(key)` and `Loc.T(key, args)` | Matches existing codebase conventions (`AppPreferenceService`, `LaunchSettingMetadataService` are all static classes) |
| Key naming | `Page.Section.Element` dot-notation | Predictable, self-documenting, easy to grep |
| Page refresh on language change | Re-navigate to current page (re-run factory) | Factories are stateless — they rebuild UI from ViewModel each call. No UI state to migrate. Simpler than binding-based approach |
| XAML strings | Code-behind assignment via `x:Name` | Fits existing pattern (`AppStatusText.Text = ...` already done in code-behind). Avoids custom markup extension complexity |
| Fallback | Missing keys return English value from `Strings.en.json` | Ensures the app never shows blank labels |
| Culture detection order | 1. Saved preference (StateStore) 2. System UI culture (if JSON exists) 3. English | Respects user choice, degrades gracefully |
| Number formatting | All metric/byte displays stay `CultureInfo.InvariantCulture` | Prevents `1,048,576` ↔ `1.048.576` confusion in technical displays |

---

## Files to Create

```
src/LocalLlmConsole.App/
  Localization/
    Loc.cs                          — static lookup + loader
    Strings.en.json                 — English (default/fallback)
    Strings.bg.json                 — Bulgarian (post-extraction)
```

## Files to Modify (in order)

| Phase | File | Change |
|---|---|---|
| 1 | `Models/AppSettings.cs` | Add `string UiCulture = "en"` parameter |
| 1 | `Services/Infrastructure/StateStore.Settings.cs` | Persist/load `uiCulture` key |
| 1 | `MainWindow.xaml` | Add language selector ComboBox in sidebar |
| 1 | `MainWindow.xaml.cs` | Wire language selector, call `Loc.LoadLanguage()` |
| 1 | `MainWindow.Pages.cs` | Add `RefreshCurrentPage()` for language switch |
| 1 | `MainWindow.UiState.cs` | Store current page name for refresh |
| 1 | `LocalLlmConsole.App.csproj` | Add `EmbeddedResource` glob for JSON files |
| 2 | `Ui/Pages/Overview/OverviewPageFactory.cs` | Replace all string literals with `Loc.T()` |
| 2 | `Ui/Common/MetricCardFactory.cs` | Localize "Last known", "ago", "just now", tooltip |
| 2 | `Ui/Common/PageSectionFactory.cs` | Localize section titles (if any) |
| 2 | `MainWindow.xaml` + code-behind | Localize nav buttons, "Tools", status labels, titlebar |
| 2 | `Ui/Pages/Models/LaunchSettingsPanelFactory*.cs` | Replace labels, section titles |
| 2 | `Services/Runtimes/LaunchSettingMetadataService.cs` | Replace tooltip strings with `Loc.T()` |
| 2 | `Ui/Pages/Models/ModelsPageFactory.cs` | Replace all strings |
| 2 | `Ui/Pages/Settings/SettingsPageFactory.cs` | Replace all strings |
| 2 | `Services/App/SettingsPageDefinitionService.cs` | Replace labels, tooltips, action labels |
| 2 | `Services/App/AppPreferenceService.cs` | Replace display labels ("Yes", "No", "Taskbar only", etc.) |
| 2 | `Ui/Pages/Runtimes/RuntimesPageFactory.cs` | Replace all strings |
| 2 | `Ui/Pages/OpenCode/OpenCodePageFactory.cs` | Replace all strings |
| 2 | `Ui/Pages/Lifetime/LifetimePageFactory.cs` | Replace all strings |
| 2 | `Ui/Pages/Logs/LogsPageFactory.cs` | Replace all strings |
| 2 | `Ui/Pages/Environment/WindowsPageFactory.cs` | Replace all strings |
| 2 | `Ui/Pages/Environment/WslPageFactory.cs` | Replace all strings |
| 2 | `Ui/Pages/Updates/UpdatesPageFactory.cs` | Replace all strings |
| 2 | `Ui/Pages/Help/HelpPageFactory.cs` + `HelpContentFactory.cs` | Replace all strings |
| 3 | `Strings.bg.json` | Bulgarian translation |

---

## Phase 1 — Infrastructure (Day 1)

### Step 1.1: Add `UiCulture` to AppSettings

**File:** `Models/AppSettings.cs`

Add `string UiCulture = "en"` as the last constructor parameter with a default:

```csharp
public sealed record AppSettings(
    // ... existing 79 parameters ...
    string CustomParameters = "",
    string UiCulture = "en")   // <-- ADD THIS
```

Also add the constant default:
```csharp
public const string DefaultUiCulture = "en";
```

In `CreateDefault()`, add `DefaultUiCulture` as the final argument.

### Step 1.2: Persist `uiCulture` in StateStore

**File:** `Services/Infrastructure/StateStore.Settings.cs`

In `GetAppSettingsAsync()`, add after the last setting line:
```csharp
UiCulture = StringValue("uiCulture", defaults.UiCulture)
```

In `SaveAppSettingsAsync()`, add to the rows array:
```csharp
("uiCulture", settings.UiCulture),
```

### Step 1.3: Create `Loc.cs`

**File:** `Localization/Loc.cs`

```csharp
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace LocalLlmConsole.Localization;

public static class Loc
{
    private static readonly Dictionary<string, string> _strings = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _fallback = new(StringComparer.OrdinalIgnoreCase);
    private static string _currentLanguage = "en";

    public static string CurrentLanguage => _currentLanguage;

    /// <summary>Lookup a localized string by key. Returns the key itself if not found.</summary>
    public static string T(string key)
        => _strings.TryGetValue(key, out var value) ? value
            : _fallback.TryGetValue(key, out var fb) ? fb
            : key;

    /// <summary>Lookup with String.Format placeholders. {0}, {1}, etc.</summary>
    public static string T(string key, params object[] args)
    {
        var template = T(key);
        return args.Length > 0 ? string.Format(template, args) : template;
    }

    /// <summary>Load a language by code ("en", "bg", "de"). Falls back to English on any failure.</summary>
    public static void LoadLanguage(string languageCode)
    {
        _currentLanguage = languageCode;
        _strings.Clear();

        // Always load English fallback first
        LoadJson("Strings.en.json", _fallback);

        if (!string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase))
            LoadJson($"Strings.{languageCode}.json", _strings);
    }

    private static void LoadJson(string resourceName, Dictionary<string, string> target)
    {
        target.Clear();
        var assembly = Assembly.GetExecutingAssembly();
        var fullName = $"{assembly.GetName().Name}.Localization.{resourceName}";

        using var stream = assembly.GetManifestResourceStream(fullName);
        if (stream is null) return; // Language file not found — graceful

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (dict is not null)
        {
            foreach (var kvp in dict)
                target[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>Discover available languages by scanning embedded resources.</summary>
    public static IReadOnlyList<string> AvailableLanguages()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var prefix = $"{assembly.GetName().Name}.Localization.Strings.";
        return assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix) && n.EndsWith(".json"))
            .Select(n => n[prefix.Length..^5]) // Extract "en", "bg", etc.
            .OrderBy(c => c == "en" ? 0 : 1).ThenBy(c => c)
            .ToList();
    }

    /// <summary>Human-readable name for a language code (for the ComboBox).</summary>
    public static string LanguageDisplayName(string code) => code switch
    {
        "en" => "English",
        "bg" => "Български",
        "de" => "Deutsch",
        _ => code
    };
}
```

**Design notes:**
- `_fallback` always holds English. `_strings` holds the active translation (empty for English since fallback covers it).
- `T(key)` never throws or returns null — worst case it returns the key itself, making missing strings visible during development.
- `AvailableLanguages()` discovers JSON files at runtime — no hardcoded list.

### Step 1.4: Create English JSON

**File:** `Localization/Strings.en.json`

Start with just the strings needed for Phase 1 validation (nav + overview + a few tooltips). Full extraction happens in Phase 2.

```json
{
  "App.Title": "llama.cpp Windows Manager",
  "App.Version": "v{0}",
  "Nav.Overview": "Overview",
  "Nav.Models": "Models",
  "Nav.Runtimes": "Runtimes",
  "Nav.Settings": "Settings",
  "Nav.OpenCode": "OpenCode",
  "Nav.Lifetime": "Lifetime",
  "Nav.Logs": "Logs",
  "Nav.Tools": "Tools",
  "Nav.Windows": "Windows",
  "Nav.WslLinux": "WSL Linux",
  "Nav.Updates": "Check For Updates",
  "Nav.Help": "Help",
  "Status.CurrentAction": "Current action",
  "Status.Starting": "Starting...",
  "Status.Language": "Language",
  "Overview.LoadedSessions": "Loaded Model Sessions",
  "Overview.LiveRuntimeLog": "Live Runtime Log",
  "Overview.RuntimeMetrics": "All llama.cpp Metrics",
  "Overview.ModelStatus": "Model Status",
  "Overview.Hardware": "Hardware",
  "Overview.Settings": "Settings",
  "Overview.Tokens": "Tokens",
  "Overview.MtpTokens": "MTP tokens",
  "Overview.Slots": "Slots",
  "Overview.Load": "Load",
  "Overview.Unload": "Unload",
  "Overview.LoadTooltip": "Load the selected model with its saved launch settings.",
  "Overview.UnloadTooltip": "Stop the currently loading or loaded model and free runtime resources.",
  "Overview.ModelLabel": "Model",
  "Overview.ModelComboTooltip": "Choose a local model to load with its saved launch profile.",
  "Overview.NoRuntimeLog": "No runtime log is active.",
  "Metrics.LastKnown": "Last known {0} ago",
  "Metrics.JustNow": "just now",
  "Metrics.Tooltip": "Live token rates are using the most recent successful metrics sample."
}

```

**Key naming convention:** `Page.Section.Element` — e.g., `Overview.LoadedSessions`, `Models.LaunchSettings.ContextSize`.

### Step 1.5: Register JSON as EmbeddedResource

**File:** `LocalLlmConsole.App.csproj`

Add inside `<Project>`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Localization\Strings.*.json" />
</ItemGroup>
```

### Step 1.6: Add language selector to the sidebar

**File:** `MainWindow.xaml`

In the sidebar `Grid`, add between the "Tools" section and the status panel. In the `Grid.Row="2"` `StackPanel` (which currently has the status panel + Updates + Help), insert BEFORE the status panel border:

```xml
<!-- Language selector -->
<StackPanel Margin="0,0,0,12">
  <TextBlock x:Name="LanguageLabel"
             Text="Language"
             Foreground="{DynamicResource TextMuted}"
             FontSize="11"
             FontWeight="SemiBold"
             Margin="12,0,0,6"/>
  <ComboBox x:Name="LanguageCombo"
            Style="{StaticResource SidebarCombo}"
            SelectionChanged="LanguageCombo_SelectionChanged"/>
</StackPanel>
```

### Step 1.7: Wire language switching in MainWindow

**File:** `MainWindow.xaml.cs`

In the constructor, after `ApplyStaticButtonToolTips()`:

```csharp
var savedCulture = _settings.UiCulture;
Loc.LoadLanguage(savedCulture);
PopulateLanguageSelector();
ApplyLocalizedXamlStrings();
```

Add these methods to `MainWindow` (in a new file `MainWindow.Localization.cs` or in `MainWindow.xaml.cs`):

```csharp
private void PopulateLanguageSelector()
{
    LanguageCombo.ItemsSource = Loc.AvailableLanguages()
        .Select(code => new { Code = code, Name = Loc.LanguageDisplayName(code) })
        .ToList();
    LanguageCombo.DisplayMemberPath = "Name";
    LanguageCombo.SelectedValuePath = "Code";
    LanguageCombo.SelectedValue = Loc.CurrentLanguage;
}

private async void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (LanguageCombo.SelectedValue is not string lang || lang == Loc.CurrentLanguage)
        return;

    Loc.LoadLanguage(lang);
    _settings = _settings with { UiCulture = lang };
    
    // Persist immediately (fire-and-forget is fine — non-critical)
    if (_stateStore is not null)
        _ = _stateStore.SaveAppSettingsAsync(_settings);

    ApplyLocalizedXamlStrings();
    RefreshCurrentPage();
}

private void ApplyLocalizedXamlStrings()
{
    // XAML elements with x:Name that need localization
    OverviewNavButton.Content = Loc.T("Nav.Overview");
    ModelsNavButton.Content = Loc.T("Nav.Models");
    RuntimesNavButton.Content = Loc.T("Nav.Runtimes");
    SettingsNavButton.Content = Loc.T("Nav.Settings");
    OpenCodeNavButton.Content = Loc.T("Nav.OpenCode");
    LifetimeNavButton.Content = Loc.T("Nav.Lifetime");
    LogsNavButton.Content = Loc.T("Nav.Logs");
    ToolsNavLabel.Text = Loc.T("Nav.Tools");
    WindowsNavButton.Content = Loc.T("Nav.Windows");
    WslLinuxNavButton.Content = Loc.T("Nav.WSL Linux");
    UpdatesNavButton.Content = Loc.T("Nav.Updates");
    HelpNavButton.Content = Loc.T("Nav.Help");
    LanguageLabel.Text = Loc.T("Status.Language");
    Title = $"{Loc.T("App.Title")} {AppVersionLabel}";
}
```

**Important:** Remove the hardcoded `Content="..."` attributes from these buttons in XAML — they'll be set by code-behind. Leave `x:Name` attributes.

### Step 1.8: Add `RefreshCurrentPage()`

**File:** `MainWindow.Pages.cs`

```csharp
private void RefreshCurrentPage()
{
    switch (_viewModel.CurrentPage)
    {
        case "Overview": ShowOverview(); break;
        case "Models": ShowModels(); break;
        case "Runtimes": ShowRuntimes(); break;
        case "Settings": ShowSettings(); break;
        case "OpenCode": ShowOpenCode(); break;
        case "Lifetime": ShowLifetime(); break;
        case "Logs": ShowLogs(); break;
        case "Windows": ShowWindows(); break;
        case "WSL Linux": ShowWslLinux(); break;
        case "Updates": ShowUpdates(); break;
        case "Help": ShowHelp(); break;
    }
}
```

Note: `_viewModel.CurrentPage` is already set by `SetPage()` in `MainWindow.UiState.cs` — no changes needed there.

### Step 1.9: Verify Phase 1

Build and run. Verify:
- [ ] English appears correctly (no regressions)
- [ ] Language ComboBox shows "English" selected
- [ ] Nav buttons show correct text from JSON
- [ ] Status panel shows "Current action" / "Starting..." from JSON
- [ ] Titlebar shows "llama.cpp Windows Manager v1.1.6"
- [ ] Switching language (even though only "en" exists) doesn't crash
- [ ] Overview page loads with localized strings

---

## Phase 2 — String Extraction (Days 2-3)

### General extraction rules

1. **Replace inline strings** like `"Model"` → `Loc.T("Overview.ModelLabel")`
2. **Replace tooltips** like `"Choose a local model..."` → `Loc.T("Overview.ModelComboTooltip")`
3. **Replace section titles** like `LoadedSessionsTitle` constant → inline `Loc.T("Overview.LoadedSessions")` (remove the constant)
4. **Add missing keys to `Strings.en.json`** as you extract them
5. **Format strings with parameters** use `Loc.T("Metrics.LastKnown", age)` pattern
6. **Column headers** in tuple arrays: keep the tuple structure, replace the header string with `Loc.T()` call

### What NOT to translate

These MUST stay as hardcoded English/invariant strings:

| Category | Examples | Reason |
|---|---|---|
| llama.cpp argument values | `"auto"`, `"on"`, `"off"`, `"f16"`, `"q8_0"`, `"none"`, `"atomic-mtp"` | Server command-line arguments — changing them breaks the runtime |
| Runtime metric status strings | `"None"`, `"Stopped"`, `"Loading"`, `"Loaded"`, `"Warm"`, `"Unavailable"`, `"Unknown runtime"`, `"Failed..."` | These are machine-generated values from llama.cpp's `/metrics` endpoint, compared with `string.Equals` in `MetricCardFactory.IsNeutralMetricStatus()` |
| Metric label markers | `"Model status"`, `"KV cache"`, `"Context"`, `"Accepted"`, `"Prompt"`, `"Micro"`, `"Batch"`, `"Cont"`, `"Gen"` | Hardcoded in `SplitMetricLine()` for parsing — not display strings |
| ComboBox option values | `AppPreferenceService` arrays like `["auto", "on", "off"]` | These are the values stored in settings, not display text |
| Model status prefixes | `"Loaded Model:"`, `"Loading Model:"`, `"Warm:"`, `"Stopped:"` | Compared in `TrySplitModelStatusName()` — runtime output parsing |
| Setting keys | `"themeMode"`, `"minimizeBehavior"`, etc. | Database keys — must be stable across versions |

### Extraction order (by file)

#### 2.1 OverviewPageFactory.cs

**Current pattern → new pattern:**

```csharp
// Before
public const string LoadedSessionsTitle = "Loaded Model Sessions";
// After — remove constant, use Loc.T() inline

// Before
("Model", "C1", 1.45),
// After
(Loc.T("Overview.Column.Model"), "C1", 1.45),

// Before
new TextBlock { Text = "Model" }
// After
new TextBlock { Text = Loc.T("Overview.ModelLabel") }

// Before
ToolTip = "Choose a local model to load with its saved launch profile."
// After
ToolTip = Loc.T("Overview.ModelComboTooltip")

// Before
var model = MetricCardFactory.AddMetric(runtimeDashboard, "Model status", 0, 0);
// After
var model = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.ModelStatus"), 0, 0);

// Before (Button helper)
"Load" => "Load the selected model with its saved launch settings.",
// After
"Load" => Loc.T("Overview.LoadTooltip"),

// Before
Text = "No runtime log is active."
// After
Text = Loc.T("Overview.NoRuntimeLog")
```

**Keys to add to Strings.en.json:**
```json
{
"Overview.Column.Model": "Model",
"Overview.Column.Size": "Size",
"Overview.Column.State": "State",
"Overview.Column.ApiEndpoints": "API endpoints",
"Overview.Column.Runtime": "Runtime",
"Overview.Column.Backend": "Backend",
"Overview.Column.Metric": "Metric",
"Overview.Column.Labels": "Labels",
"Overview.Column.Value": "Value",
"Overview.Column.Type": "Type",
"Overview.Column.Help": "Help"
}

```

#### 2.2 MetricCardFactory.cs

**Changes:**
```csharp
// Before
target.Text = $"Last known {age} ago";
// After
target.Text = Loc.T("Metrics.LastKnown", age);

// Before
var age = now <= capturedAt ? "just now" : DisplayFormatService.Elapsed(now - capturedAt);
// After
var age = now <= capturedAt ? Loc.T("Metrics.JustNow") : DisplayFormatService.Elapsed(now - capturedAt);

// Before
target.ToolTip = "Live token rates are using the most recent successful metrics sample.";
// After
target.ToolTip = Loc.T("Metrics.Tooltip");
```

**IMPORTANT:** Do NOT translate the strings in `IsNeutralMetricStatus()`, `SplitMetricLine()`, `TrySplitModelStatusName()`, or `IsStatusNameMetricLabel()`. These parse runtime output — they must stay English.

Also: `MetricLabelColumnWidth` uses `label == "Model status"` — this comparison must use the English key constant, not the localized value. Change to compare against a known constant or the English lookup:

```csharp
private static double MetricLabelColumnWidth(string label)
    => string.Equals(label, Loc.T("Overview.ModelStatus"), StringComparison.Ordinal)
        ? 98 : 74;
```

Wait — this is fragile. Better approach: tag the metric card with the lookup key, not the localized label. Change `Tag = label` to `Tag = labelKey` and pass the key separately. But that's a bigger refactor. Simpler fix: store the English key and compare against that:

```csharp
// In AddMetric, change:
valueRows.Tag = label;       // was localized string
// to:
valueRows.Tag = labelKey;    // new parameter — the Loc key

// Add an overload or parameter for the English key
public static Grid AddMetric(Grid grid, string labelKey, int row, int column)
{
    return AddMetric(grid, Loc.T(labelKey), labelKey, row, column, false, out _, out _);
}
```

This means updating all callers to pass the key. Worth doing correctly in Phase 2.

#### 2.3 LaunchSettingMetadataService.cs

Replace the `Tooltip()` switch statement with Loc lookups:

```csharp
public static string Tooltip(string label) => label switch
{
    "Context size"      => Loc.T("Tooltip.ContextSize"),
    "Parallel slots"    => Loc.T("Tooltip.ParallelSlots"),
    "Batch size"        => Loc.T("Tooltip.BatchSize"),
    "Micro batch"       => Loc.T("Tooltip.MicroBatch"),
    "Threads"           => Loc.T("Tooltip.Threads"),
    "GPU layers"        => Loc.T("Tooltip.GpuLayers"),
    // ... all 45 cases ...
    _                   => Loc.T("Tooltip.Default")
};
```

For `ContextSizeTooltip()`, which appends a suggestion dynamically:

```csharp
public static string ContextSizeTooltip(string text)
{
    var tooltip = Tooltip("Context size");
    if (!LaunchSettingParser.TryNormalizeContextSize(text, out var value) || value <= 0)
        return tooltip;
    
    var normalized = value.ToString(CultureInfo.InvariantCulture);
    var compactText = (text ?? "")
        .Replace(",", "", StringComparison.Ordinal)
        .Replace("_", "", StringComparison.Ordinal);
    return string.Equals(compactText, normalized, StringComparison.OrdinalIgnoreCase)
        ? tooltip
        : Loc.T("Tooltip.ContextSizeSuggestion", value.ToString("N0", CultureInfo.InvariantCulture));
}
```

Keys:
```json
{
"Tooltip.ContextSize": "How much text and conversation history the model can keep in memory. 0 uses the model's GGUF default. Shorthand like 196 or 196k becomes 200,704.",
"Tooltip.ContextSizeSuggestion": "{0}{1}{1}Suggestion: {2} tokens.",
"Tooltip.Default": "Setting used when starting or restarting this model."
}

```

Wait — the `ContextSizeSuggestion` format: the original is `$"{tooltip}{Environment.NewLine}{Environment.NewLine}Suggestion: {value:N0} tokens."`. Use:

`"Tooltip.ContextSizeSuggestion": "{0}\n\nSuggestion: {1} tokens."`

And call: `Loc.T("Tooltip.ContextSizeSuggestion", tooltip, value.ToString("N0", CultureInfo.InvariantCulture))`

#### 2.4 LaunchSettingsPanelFactory*.cs

These files contain 40+ setting labels. Extract systematically:

```csharp
// Before
builder.AddLaunchSetting(basicGrid, "Context size", contextSizeBox);
// After
builder.AddLaunchSetting(basicGrid, Loc.T("LaunchSettings.ContextSize"), contextSizeBox);

// Before
AddLaunchSection(panel, builder, "Basic Launch", basicGrid);
// After
AddLaunchSection(panel, builder, Loc.T("LaunchSettings.Section.BasicLaunch"), basicGrid);
```

**Section title keys:**
```json
{
"LaunchSettings.Section.BasicLaunch": "Basic Launch",
"LaunchSettings.Section.PerformanceMemory": "Performance & Memory",
"LaunchSettings.Section.SpeculativeMtp": "Speculative / MTP",
"LaunchSettings.Section.ChatCapabilities": "Chat & Model Capabilities",
"LaunchSettings.Section.Generation": "Generation",
"LaunchSettings.Section.Advanced": "Advanced"
}

```

**Setting label keys (partial list — extract all ~40):**
```json
{
"LaunchSettings.ContextSize": "Context size",
"LaunchSettings.Threads": "Threads",
"LaunchSettings.GpuLayers": "GPU layers",
"LaunchSettings.FlashAttention": "Flash attention",
"LaunchSettings.KCache": "K cache",
"LaunchSettings.VCache": "V cache",
"LaunchSettings.KvOffload": "KV offload",
"LaunchSettings.UnifiedKv": "Unified KV",
"LaunchSettings.PromptCache": "Prompt cache",
"LaunchSettings.PromptCacheMb": "Prompt cache MB",
"LaunchSettings.Checkpoints": "Checkpoints",
"LaunchSettings.CheckpointCount": "Checkpoint count",
"LaunchSettings.CheckpointSpacing": "Checkpoint spacing",
"LaunchSettings.ContinuousBatch": "Continuous batch",
"LaunchSettings.MemoryMap": "Memory map",
"LaunchSettings.MemoryLock": "Memory lock",
"LaunchSettings.Metrics": "Metrics",
"LaunchSettings.CustomParams": "Custom params",
"LaunchSettings.SpecType": "Spec type",
"LaunchSettings.DraftModel": "Draft model",
"LaunchSettings.MtpHead": "MTP head",
"LaunchSettings.DraftKCcache": "Draft K cache",
"LaunchSettings.DraftVCcache": "Draft V cache",
"LaunchSettings.DraftMax": "Draft max",
"LaunchSettings.DraftMin": "Draft min",
"LaunchSettings.DraftGpu": "Draft GPU",
"LaunchSettings.SplitProb": "Split prob",
"LaunchSettings.MinProb": "Min prob",
"LaunchSettings.Reasoning": "Reasoning",
"LaunchSettings.ReasonFormat": "Reason format",
"LaunchSettings.ReasonBudget": "Reason budget",
"LaunchSettings.JinjaChat": "Jinja chat",
"LaunchSettings.Vision": "Vision",
"LaunchSettings.VisionHead": "Vision head",
"LaunchSettings.ImageMin": "Image min",
"LaunchSettings.ImageMax": "Image max",
"LaunchSettings.Temperature": "Temperature",
"LaunchSettings.TopK": "Top K",
"LaunchSettings.TopP": "Top P",
"LaunchSettings.MinP": "Min P",
"LaunchSettings.MaxTokens": "Max tokens",
"LaunchSettings.Seed": "Seed",
"LaunchSettings.RepeatWindow": "Repeat window",
"LaunchSettings.RepeatPen": "Repeat pen",
"LaunchSettings.Presence": "Presence",
"LaunchSettings.Frequency": "Frequency",
"LaunchSettings.RopeScaling": "RoPE scaling",
"LaunchSettings.RopeScale": "RoPE scale",
"LaunchSettings.RopeBase": "RoPE base",
"LaunchSettings.RopeFreq": "RoPE freq"
}

```

#### 2.5 SettingsPageDefinitionService.cs

Each `new SettingRowDefinition(...)` has labels, tooltips, and action text. Extract:

```csharp
// Before
new("Storage", "Cache", "cache", $"{DisplayFormatService.BytesOrZero(CacheMaintenanceService.Size(settings.CacheRoot))} in {settings.CacheRoot}", "readonly", Action: "Clear",
    ToolTip: "Disposable app cache used for downloads..."),
// After
new(Loc.T("Settings.Section.Storage"), Loc.T("Settings.Cache"), "cache",
    Loc.T("Settings.CacheValue", DisplayFormatService.BytesOrZero(CacheMaintenanceService.Size(settings.CacheRoot)), settings.CacheRoot),
    "readonly", Action: Loc.T("Settings.Action.Clear"),
    ToolTip: Loc.T("Settings.Tooltip.Cache")),
```

Keys:
```json
{
"Settings.Section.Storage": "Storage",
"Settings.Section.Window": "Window",
"Settings.Section.OpenCode": "OpenCode",
"Settings.Section.Model": "Model",
"Settings.Section.Runtime": "Runtime",
"Settings.Section.Network": "Network",
"Settings.Section.Logs": "Logs",
"Settings.Cache": "Cache",
"Settings.CacheValue": "{0} in {1}",
"Settings.Action.Clear": "Clear",
"Settings.Action.Generate": "Generate",
"Settings.Tooltip.Cache": "Disposable app cache used for downloads, runtime packages, source archives, and temporary build files. Clear only removes safe cache contents while no cache job is running.",
"Settings.MinimizeBehavior": "Minimize behavior",
"Settings.Tooltip.MinimizeBehavior": "Controls where the app goes when minimized: taskbar only, tray only, or both tray and taskbar.",
"Settings.StartWithWindows": "Start with Windows",
"Settings.Tooltip.StartWithWindows": "Registers or removes this app from the current user's Windows startup apps.",
"Settings.AutoSyncOpenCode": "Auto-sync entries",
"Settings.Tooltip.AutoSyncOpenCode": "Automatically rewrites OpenCode local model entries after Settings saves, saved model launch settings, or saved variants change. Turn off to leave existing OpenCode config untouched unless you add or update entries from the OpenCode page. OpenCode provider config stores the synced API key in plain text.",
"Settings.LimitOutput": "Limit output",
"Settings.Tooltip.LimitOutput": "Whole number from 1 to {0}. Written to OpenCode local model entries as limit.output when OpenCode settings are saved or synced.",
"Settings.AutoUnloadIdle": "Auto unload idle min",
"Settings.Tooltip.AutoUnloadIdle": "Whole number of idle minutes before quiet loaded models are stopped automatically. Use 0 to disable idle auto-unload.",
"Settings.DeleteSourceAfterBuild": "Delete source after build",
"Settings.Tooltip.DeleteSourceAfterBuild": "Deletes downloaded source trees after successful source builds. Official prebuilt runtimes are not affected.",
"Settings.LanExposure": "LAN exposure",
"Settings.Tooltip.LanExposure": "Controls which serving endpoints accept trusted LAN clients: gateway only, direct model ports only, both, or local-only loopback.",
"Settings.AutoLoadGateway": "Auto-load gateway",
"Settings.Tooltip.AutoLoadGateway": "Enables one shared OpenAI-compatible /v1 endpoint for clients like OpenCode. Requests still load each model on its own direct runtime port, then the gateway proxies to that port.",
"Settings.GatewayPort": "Gateway port",
"Settings.Tooltip.GatewayPort": "Whole number from 1 to 65535 for the shared auto-load gateway. Keep this separate from direct model ports.",
"Settings.GatewayPolicy": "Gateway policy",
"Settings.Tooltip.GatewayPolicy": "Prefer keeping loaded models leaves existing sessions running. Single active model unloads other models before loading the requested model to free VRAM.",
"Settings.ApiKeyAuth": "API key auth",
"Settings.Tooltip.ApiKeyAuth": "When enabled, local OpenAI-compatible endpoints require a Bearer API key for authentication. Disable to allow unauthenticated access (not recommended when exposing on LAN). Your current key is preserved and restored when re-enabled.",
"Settings.ApiKey": "API key",
"Settings.Tooltip.ApiKey": "Bearer key required by local OpenAI-compatible endpoints. Must be at least 32 non-whitespace characters; leaving it blank generates a new key on save. OpenCode sync copies this key into OpenCode provider config in plain text.",
"Settings.MaxLogFileMb": "Max log file MB",
"Settings.Tooltip.MaxLogFileMb": "Whole number from 1 to 4096. Limits each app, runtime, or job log file before rotation or trimming."
}

```

#### 2.6 AppPreferenceService.cs

The display labels must move to Loc:

```csharp
// Before
public static readonly string[] MinimizeBehaviorOptionLabels = ["Taskbar only", "Tray only", "Tray + taskbar"];
// After
public static readonly string[] MinimizeBehaviorOptionLabels =
    [Loc.T("Pref.TaskbarOnly"), Loc.T("Pref.TrayOnly"), Loc.T("Pref.TrayAndTaskbar")];
```

BUT — this is a static readonly field initialized at type load time, before `Loc.LoadLanguage()` has run. The labels would be frozen to English.

**Fix:** Change from static readonly arrays to static properties that evaluate lazily:

```csharp
public static IEnumerable<string> MinimizeBehaviorOptions()
    => [Loc.T("Pref.TaskbarOnly"), Loc.T("Pref.TrayOnly"), Loc.T("Pref.TrayAndTaskbar")];
```

This is already the pattern used for return values (`MinimizeBehaviorOptions()` returns `MinimizeBehaviorOptionLabels`). Just inline the Loc calls.

JSON keys:
```json
{
"Pref.TaskbarOnly": "Taskbar only",
"Pref.TrayOnly": "Tray only",
"Pref.TrayAndTaskbar": "Tray + taskbar",
"Pref.LocalOnly": "Local only",
"Pref.GatewayLanOnly": "Gateway LAN only",
"Pref.DirectModelsLanOnly": "Direct models LAN only",
"Pref.GatewayAndDirectLan": "Gateway + direct LAN",
"Pref.PreferKeepingLoaded": "Prefer keeping loaded models",
"Pref.SingleActiveModel": "Single active model",
"Pref.Latest": "Latest",
"Pref.Compatibility": "Compatibility",
"Pref.Yes": "Yes",
"Pref.No": "No"
}

```

#### 2.7 ModelsPageFactory.cs, RuntimesPageFactory.cs, etc.

Follow the same pattern for each remaining factory. Key extraction priorities:

- **Column headers** — every `GridFor()` call
- **Section titles** — every `GridSection()`, `FramedSection()` call
- **Button labels** — every `Button()` call or inline button Content
- **Tooltips** — every `ToolTip =` assignment
- **Search box watermarks** — if any
- **Status/empty-state messages** — "No models found", "Scanning...", etc.

#### 2.8 HelpContentFactory.cs / HelpPageFactory.cs

The help system has dynamic content. Extract only the static labels and section headers. If help content is markdown or generated, leave it English-only for v1.

### Phase 2 verification checklist

After each file is extracted:
- [ ] Build succeeds (no compile errors from missing Loc.T calls)
- [ ] Run the app, navigate to the page, verify English text matches pre-refactor
- [ ] Check that no runtime metric comparisons broke (status strings still work)
- [ ] Verify tooltips still appear correctly
- [ ] Test with a dummy `Strings.xx.json` that has some Bulgarian strings to confirm fallback works

---

## Phase 3 — Bulgarian Translation (Day 4)

### Step 3.1: Generate the Bulgarian JSON

Copy `Strings.en.json` → `Strings.bg.json`. Translate every value. Key translation guidelines:

1. **Technical terms stay English** — "GPU layers", "KV cache", "RoPE scaling", "MTP head", etc. These are llama.cpp concepts without standard Bulgarian equivalents. Translating them creates confusion.
2. **UI chrome translates** — nav labels, button text, status messages, section titles, tooltips.
3. **Format placeholders stay intact** — `{0}`, `{1}` must appear exactly as in English.
4. **Bulgarian uses Cyrillic** — ensure the font renders correctly (Cascadia Mono is the metric font, but UI text uses Segoe UI which supports Cyrillic natively).

Example:
```json
{
  "Nav.Overview": "Общ преглед",
  "Nav.Models": "Модели",
  "Nav.Settings": "Настройки",
  "Overview.Load": "Зареди",
  "Overview.Unload": "Освободи",
  "Status.Starting": "Стартиране...",
  "Metrics.LastKnown": "Последно преди {0}",
  "Metrics.JustNow": "току-що"
}

```

### Step 3.2: Language display name

Update `Loc.LanguageDisplayName()` to include Bulgarian:
```csharp
"bg" => "Български",
```

### Step 3.3: Initial culture detection

In `MainWindow` constructor, before `Loc.LoadLanguage()`:

```csharp
var savedCulture = _settings.UiCulture;
if (string.IsNullOrWhiteSpace(savedCulture) || savedCulture == "en")
{
    // Check if system culture has a matching JSON
    var sysCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    var available = Loc.AvailableLanguages();
    if (available.Contains(sysCulture))
        savedCulture = sysCulture;
    else
        savedCulture = "en";
}
Loc.LoadLanguage(savedCulture);
```

### Phase 3 verification
- [ ] Switch to Bulgarian — all pages display Bulgarian text
- [ ] Switch back to English — all pages revert correctly
- [ ] Missing Bulgarian keys fall back to English (delete a key from bg.json temporarily to test)
- [ ] Number formatting unchanged (comma thousands separator, decimal point)
- [ ] App restart preserves language selection
- [ ] Tooltips display in Bulgarian
- [ ] Status bar messages display in Bulgarian

---

## Phase 4 — Polish & Testing (Day 5)

### Testing matrix

| Test | Steps |
|---|---|
| Fresh install, no settings | App starts in English. Language selector shows English. |
| Switch to Bulgarian | All visible strings change immediately. Current page refreshes. |
| Navigate all pages in Bulgarian | Every label, header, tooltip, button reads in Bulgarian. No English leakage. |
| Switch back to English mid-session | All strings revert. |
| Restart app | Language preference persists (check StateStore). |
| Missing key in bg.json | Falls back to English string (not blank, not key name). |
| ComboBox dropdowns | Options display in correct language (Yes/No, minimize behavior, etc.). |
| Runtime metrics | Status values ("Loaded", "Stopped") unchanged — these are machine values. |
| Launch a model | Launch settings labels in correct language. Tooltips in correct language. ComboBox option values (auto/on/off) remain English. |
| OpenCode page | All labels, buttons, section titles localized. |
| Help page | Static labels localized; generated content may remain English (acceptable for v1). |
| Single-file publish | `dotnet publish` produces working exe with embedded JSON. |
| Window title | Shows "llama.cpp Windows Manager v1.1.6" in all languages (product name not translated). |

### Debug mode (optional enhancement)

Add a conditional in `Loc.T()`:

```csharp
#if DEBUG
public static bool ShowMissingKeys { get; set; } = false;
#endif

public static string T(string key)
{
    if (_strings.TryGetValue(key, out var value))
        return value;
    if (_fallback.TryGetValue(key, out var fb))
    {
#if DEBUG
        if (ShowMissingKeys && !string.Equals(_currentLanguage, "en", StringComparison.OrdinalIgnoreCase))
            return $"«{key}»"; // Visual indicator that key is missing from translation
#endif
        return fb;
    }
#if DEBUG
    return $"???{key}???";
#else
    return key;
#endif
}
```

### Future languages

To add German (as an example):
1. Copy `Strings.en.json` → `Strings.de.json`
2. Translate all values
3. Add `"de" => "Deutsch"` to `Loc.LanguageDisplayName()`
4. That's it — `AvailableLanguages()` discovers it automatically at startup

---

## Key Naming Convention Reference

```
Nav.<PageName>                    — sidebar navigation buttons
Status.<Element>                  — status bar, titlebar
Overview.<Element>                — overview page
Overview.Column.<Name>            — overview datagrid columns
Metrics.<Element>                 — metric card labels
Models.<Element>                  — models page
LaunchSettings.<Label>            — launch setting field labels
LaunchSettings.Section.<Title>    — launch settings section headers
Tooltip.<Label>                   — launch setting tooltips (from LaunchSettingMetadataService)
Settings.<Label>                  — settings page labels
Settings.Tooltip.<Label>          — settings page tooltips
Settings.Section.<Name>           — settings page section headers
Settings.Action.<Verb>            — settings page button labels
Pref.<Label>                      — preference option display text
Runtimes.<Element>                — runtimes page
OpenCode.<Element>                — opencode page
Lifetime.<Element>                — lifetime page
Logs.<Element>                    — logs page
Windows.<Element>                 — windows environment page
Wsl.<Element>                     — WSL environment page
Updates.<Element>                 — updates page
Help.<Element>                    — help page
```

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| `AppPreferenceService` static arrays evaluated before Loc loads | Labels frozen to English | Convert to lazy properties (already partially done with `*Options()` methods) |
| `MetricCardFactory` label comparisons break | Runtime dashboard shows wrong formatting | Tag metric cards with English key, not localized label. Compare against key. |
| ComboBox SelectedValue comparison breaks on translated values | Settings dropdowns stop working | ComboBox `SelectedValuePath` already uses internal value strings ("taskbarOnly", etc.) — only `DisplayMemberPath` text changes |
| Single-file publish can't find JSON | App crashes on startup | `EmbeddedResource` is always available via `Assembly.GetManifestResourceStream()`. Test publish early. |
| Unicode/Cyrillic rendering issues | Bulgarian text shows as boxes | Segoe UI (WPF default) has full Cyrillic support. Metric font (Cascadia Mono) only used for numbers. |
| Help content too large/dynamic for JSON | Help page partially untranslated | Acceptable for v1. Help is markdown-driven; can add per-language markdown files later. |
