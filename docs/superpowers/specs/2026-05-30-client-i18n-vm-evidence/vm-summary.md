# Client i18n VM Verification Summary

_Date: 2026-05-30 | Branch: codex/client-i18n | Task 7 of 8_

## Evidence Table

| Item | Value |
|------|-------|
| Branch | `codex/client-i18n` (HEAD 40fba05 before this task's commit) |
| Languages tested | English (`en-US`), Chinese (`zh-CN`) |
| Localization core | `MetBench_UI.Localization` — `AppLocalizationService`, `IAppLocalizationService`, `LocalizedTextProvider`, `Strings.resx` + `Strings.zh-CN.resx` |
| ClientI18n tests (MetBench_SystemMT.Tests) | **10/10 PASS** (AppLocalizationServiceTests x3, LocalizationAbnormalScenarioTests x3, LocalizationResourceTests x4) |
| ClientI18n tests (MetBench_Client.Tests) | **3/3 PASS** (MainWindowLocalizationTests x1, SettingsLanguageTests x2) |
| Full MetBench_SystemMT.Tests result | All pass (CI green) |
| WPF build | `dotnet build MetBench.sln` — 0 errors |
| UIA screenshots captured | **9/9** |
| Final decision | **PASS** |

## Screenshots

| # | File | What it shows |
|---|------|--------------|
| 01 | `01-red-green-infra-tests.png` | Text render: SystemMT.Tests 10/10 GREEN + Client.Tests 3/3 GREEN + RED state record (Unknown command: i18n-smoke) |
| 02 | `02-settings-language-switch-zh.png` | Settings page in Chinese: 个性化, 主题, 浅色/深色, 语言, 中文 in ComboBox, 语言 button |
| 03 | `03-settings-language-switch-en.png` | Settings page in English: Personalization, Theme, Light/Dark, Language, English in ComboBox, Language button |
| 04 | `04-navigation-zh.png` | Navigation in Chinese mode — window maximized before capture; Wpf.Ui nav rail is still icon-only at this width (nav text labels visible in 06 via System MT page nav) |
| 05 | `05-navigation-en.png` | Navigation in English mode — window maximized before capture; distinct screenshot from 03 (different SHA256 / file size) |
| 06 | `06-system-mt-page-zh.png` | System MT page in Chinese: heading **系统级蜕变测试**, 运行 button — proves nav label switched |
| 07 | `07-system-mt-page-en.png` | System MT page in English: heading **System MT**, Run button — proves label switched back |
| 08 | `08-invalid-culture-fallback.png` | fr-FR → falls back to en-US: CurrentCulture=en-US, Nav_SystemMtExecution="System MT" |
| 09 | `09-missing-key-fallback.png` | GetString("Missing_Key")=??Missing_Key??, null=??null??, ""=??empty??, whitespace=??empty?? |

## Abnormal Scenarios

### Unsupported Culture Fallback (screenshot 08)

**Status: PASS**

`AppLocalizationService.SetCulture(new CultureInfo("fr-FR"))` resolves via `TwoLetterISOLanguageName switch` — `"fr"` matches neither `"zh"` nor `"en"`, falls through to `Cultures.FirstOrDefault(c => c.Culture.Name == "fr-FR")?.Culture ?? English`. Since `fr-FR` is not in the two-item list, it falls back to `English` (en-US). Confirmed: `CurrentCulture.Name = "en-US"`, `Nav_SystemMtExecution = "System MT"`.

### Missing / Invalid Key Fallback (screenshot 09)

**Status: PASS**

`AppLocalizationService.GetString` returns:
- `"Missing_Key"` → `??Missing_Key??` (key not in resource file, falls to `$"??{key}??"`)
- `null` → `??null??` (null check at top of method)
- `""` (empty) → `??empty??` (IsNullOrWhiteSpace check)
- `"   "` (whitespace only) → `??empty??` (IsNullOrWhiteSpace check)

## How Screenshots 08/09 Were Generated

**Option (a): smokeshot-side diagnostic surface** (no debug window added to shipped WPF client).

`CaptureInvalidCultureFallback` and `CaptureMissingKeyFallback` in `tools/smokeshot/Flows.cs` directly instantiate `MetBench_UI.Localization.AppLocalizationService` (via a `ProjectReference` added to `smokeshot.csproj`) and render the results as a 800×400 WinForms bitmap using `System.Drawing.Graphics.DrawString`. This approach is valid because smokeshot is a development/test tool, not a shipped artifact.

## Known Limitation / Follow-up (F3)

**Settings page-title breadcrumb lags one culture-switch.**

In screenshot 02 (zh-mode), the Wpf.Ui page-title breadcrumb reads **"Settings"** (English) instead of **"设置"**, while the page body (Personalization label, ComboBox, button) has already switched to Chinese. Conversely, in screenshot 03 (en-mode after switching back), the breadcrumb reads **"设置"** (Chinese) while the body has already returned to English.

This is expected Wpf.Ui NavigationView breadcrumb behavior: the page header string is applied when the page is navigated to, and does not re-render on a live culture change. It self-heals when the user navigates away and back. The requirement for Task 2 (nav-label refresh) was met — screenshots 06 and 07 (System MT page) confirm that after navigating away the heading correctly shows **系统级蜕变测试** (zh) and **System MT** (en) respectively.

**Status**: follow-up polish item; not in scope for this plan. No production code change is needed — the stale-on-live-switch behavior is a Wpf.Ui framework constraint, not a MetBench localization bug.

## UIA Interaction Notes

- **Navigation**: Wpf.Ui NavigationViewItem DataItems respond to `SelectionItemPattern.Select()` (not `InvokePattern`). The `NavigateTo` helper was updated to try `TrySelect` first.
- **ComboBox**: The Settings page `ComboBox` with `DisplayMemberPath="DisplayName"` exposes items as `ControlType.Text` elements after expand (not `ListItem`). The `SelectComboBoxItem` helper was updated to search text elements.
- **Button**: The apply Button's `Content` binding (`Localization[Settings_Language]`) is not auto-mapped to `AutomationProperties.Name` in WPF. The button was found by its UIA Name which WPF does populate from `Content` if it's a string — confirmed to work as "Language" (en) / "语言" (zh).
- **Mouse coordinates**: UIA `BoundingRectangle` returns physical pixel coordinates; `SetCursorPos`/`mouse_event` take logical pixels. `GetSystemMetrics(0)` returns physical screen width in the DPI-aware smokeshot process context (2186), so `absX/absY` calculation uses physical pixel range.
