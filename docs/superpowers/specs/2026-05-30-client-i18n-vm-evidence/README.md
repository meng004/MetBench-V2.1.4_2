# Client i18n VM Evidence

UIA smoke evidence for Task 7 of the client i18n implementation plan
(`docs/superpowers/plans/2026-05-30-client-i18n-plan.md`).

## Evidence Files

| File | Description |
|------|-------------|
| `vm-status.jsonl` | One JSON object per step: setup, RED, GREEN, per-screenshot status, final verdict |
| `vm-summary.md` | Summary table + abnormal-scenario notes + UIA interaction notes |
| `01-red-green-infra-tests.png` | Text render showing 10+3 ClientI18n tests GREEN; includes RED state record |
| `02-settings-language-switch-zh.png` | WPF Settings page after switching to Chinese (个性化, 语言, 中文) |
| `03-settings-language-switch-en.png` | WPF Settings page after switching back to English (Personalization, Language, English) |
| `04-navigation-zh.png` | WPF window in Chinese mode after SW_MAXIMIZE — distinct screenshot from 02 (different SHA256); nav rail icon-only at this width |
| `05-navigation-en.png` | WPF window in English mode after SW_MAXIMIZE — distinct screenshot from 03 (different SHA256) |
| `06-system-mt-page-zh.png` | System MT page in Chinese — heading 系统级蜕变测试, button 运行 |
| `07-system-mt-page-en.png` | System MT page in English — heading System MT, button Run |
| `08-invalid-culture-fallback.png` | Diagnostic: fr-FR falls back to en-US (option a: smokeshot-side) |
| `09-missing-key-fallback.png` | Diagnostic: ??Missing_Key??, ??null??, ??empty?? fallbacks (option a: smokeshot-side) |

## Tool

Screenshots 02-07 captured via `tools/smokeshot/smokeshot.csproj` (`i18n-smoke` command),
driving the live WPF app (`MetBench_Client.exe`) via UI Automation.

Screenshots 01, 08, 09 are programmatically rendered text images (800x400 or 900x500 PNG)
using `System.Drawing` within the smokeshot tool.

## Final Status: PASS

All 9/9 screenshots captured. i18n-smoke exit 0. 13 infra tests pass.
