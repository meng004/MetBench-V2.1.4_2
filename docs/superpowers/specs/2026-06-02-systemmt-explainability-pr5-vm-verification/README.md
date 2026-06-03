# PR-5 VM Verification - System MT Explainability + Pair Quality WPF Surfaces

Branch: `claude/systemmt-explainability-pr5-strict-acceptance`
Base evidence: PR #265 is merged; GitHub MCP confirmed merge commit
`a58a72c6c7cb84cc4af10d44724887a8fa73bfe2`. Local `git fetch origin`
could not refresh during this strict pass because the VM DNS could not resolve
`github.com`; local `origin/main` already pointed at `a58a72c`.

Captured on the Windows VM at 200% HiDPI. No `rtk` executable was available on
this VM, so commands used native PowerShell.

Driver: [`drive.ps1`](drive.ps1) (UIA, adapted from
`tools/uia-verify-i18n.ps1`). The driver starts the WPF client, launches a real
pure-stdlib System MT run, visits the catalog/history pages, captures the
screenshots, and toggles language where UIA can find the culture menu.

## Build / Test Evidence

- `dotnet build MetBench.sln`: 0 errors, 12 warnings.
- `dotnet test MetBench_Client.Tests --no-build --filter ClientI18n`: 16 passed, 0 failed.
- `dotnet test MetBench_SystemMT.Tests --no-build --filter ClientI18n`: 18 passed, 0 failed.
- `dotnet test MetBench_Client.Tests --no-build --filter "FullyQualifiedName~SystemMtExplanationCardTests|FullyQualifiedName~SystemMtPairQualityEvidenceTests|FullyQualifiedName~SystemMtExplanationLocalizationTests"`: 12 passed, 0 failed.

New/updated client proof points:

- `SystemMtExplanationCardTests`: equation/SUT/MR ViewModels expose persisted
  explanation/profile fields for the selected row.
- `SystemMtPairQualityEvidenceTests`: evidence summary renders pair-quality
  counts/rates when present, stays quiet for default-empty/missing rows, and
  clears stale evidence when a history filter removes the selected row.
- `SystemMtExplanationLocalizationTests`: all new resource keys resolve in
  en-US and zh-CN.

## Real Execution Evidence

The non-empty pair-quality screenshot was produced by a real WPF-launched
System MT execution, not by inserting fake LiteDB rows. The UIA driver clicked
the System MT run button for the default pure-stdlib scenario and the history
page captured real `advection-amplitude-linearity` rows. Screenshot 04 shows
execution `101a6cc7-fb9c-45db-8cc9-878ad23d585d` at
`2026-06-02T07:38:02Z` with non-empty PairQuality.

## Screenshot Matrix

| File | What it shows |
|---|---|
| `01-equation-explanation-card.png` | Equation Catalog, built-in `bateman` selected. The card shows equation class, family, primary variables, physical meaning, benchmark rationale, and expected laws. |
| `02-sut-profile-card.png` | SUT Catalog, `heat_equation` selected. The SUT profile card is visible and shows program type, solver method, runtime key, input/output contract, adapter, and dependency risk. |
| `03-mr-explanation-card.png` | MR Catalog, `heat_equation` manifest and `heat-equation-amplitude` selected. The MR explanation card shows meta-pattern rationale, transformation semantics, observables, predicate, tolerance, applicability, and failure meaning. |
| `04-execution-history-non-empty-pair-quality.png` | Execution History with a real non-empty PairQuality block: planned/executed/valid/passed pairs = 1, failed/skipped/invalid spec pairs = 0, pass-rate valid/all = 100.0%. |
| `05-execution-history-no-evidence-or-empty-pair-quality.png` | Execution History filtered to no matching rows. The evidence panel is quiet and does not show a stale or default-empty PairQuality block. Default-empty evidence is also pinned by `SystemMtPairQualityEvidenceTests`. |
| `06-zh-cn-equation-or-history.png` | zh-CN equation catalog surface with localized navigation/title/card labels. Data values stay as catalog content. |
| `07-en-us-equation-or-history.png` | en-US equation catalog surface after switching back to English. |

## Fixes Verified By This Pass

- The SUT and MR explanation/profile cards are placed before the long editor
  forms so they are capturable at 200% DPI.
- Execution history shows PairQuality before the longer typed-verification
  details.
- PairQuality count labels explicitly name planned/executed/valid/passed/failed
  /skipped/invalid spec pairs.
- Execution-history filtering clears stale selected evidence, so quiet paths do
  not keep showing the previous row's PairQuality.

## Remaining Blockers

None for PR-5 strict VM acceptance.
