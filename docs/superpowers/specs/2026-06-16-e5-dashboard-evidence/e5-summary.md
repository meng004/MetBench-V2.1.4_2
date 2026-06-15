# E5 — Dashboard main page: >=4 summary cards (implemented)

> Date: 2026-06-16 · UAT rubric §E5 · follows the SP1-SP5 chain + follow-up #8.

## What changed

The Dashboard landing page was a stub (a single "Click me!" demo button + counter).
It now renders **6 summary cards** with real counts pulled from
`CoverageService.Compute()` — the same read-only 4-dimension coverage service the
Coverage Dashboard uses (DI-registered `AddScoped<CoverageService>`):

| Card | Source field | Captured value (this host's MR.Litedb) |
|---|---|---|
| Registered SUTs | `SutMr.TotalSuts` | 22 |
| Metamorphic Relations | `SutMr.TotalMRs` | 38 |
| MetaPattern Coverage | `MetaPattern.CoveredMetaPatterns / TotalMetaPatterns` | 3 / 8 (38%) |
| SUT×MR Bindings | `SutMr.BoundCells / PossibleCells` | 38 / 836 (5%) |
| Known Bugs Reproduced | `Bug.ReproducedBugs / TotalKnownBugs` | 0 / 0 (0%) |
| Mutation Kill Rate | `Mutation.DetectedMutants / TotalMutants` | 4 / 4 (100%) |

Files: `MetBench_Client/ViewModels/DashboardViewModel.cs` (rewrite: inject
CoverageService, INavigationAware -> Refresh on nav, 6 `DashboardMetric` items),
`MetBench_Client/Views/Pages/DashboardPage.xaml` (header + ScrollViewer/WrapPanel
of cards + error surface), `Strings.resx` + `Strings.zh-CN.resx` (6 card-title keys).

## Verification

- **Build**: `dotnet build MetBench_Client.csproj` (Debug + Release) -> 0 errors.
- **Real UI run** (FlaUI/UIA, `tools/uia-acceptance`): navigate to Dashboard ->
  dump tree + screenshot. The UIA tree (`e5-tree.txt`) shows all 6 cards with the
  real values above; `e5-02-dashboard-wrap.png` is the rendered page.
- Cards wrap responsively (WrapPanel + 250px fixed-width cards inside a vertical
  ScrollViewer) so none clip at narrow window widths (an earlier UniformGrid
  Columns=3 layout clipped columns 2-3 under the NavigationView content presenter;
  replaced).

## Review notes (accepted, consistent with the sibling CoverageDashboard)

An independent review found 0 high/medium correctness bugs. Three low items, all
matching the sanctioned `CoverageDashboardViewModel` pattern: card captions are
English-in-code while titles are localized via resx (same title-localized /
sublabel-English split as the sibling); snapshot labels do not re-resolve on a
live culture switch until re-navigation; `Compute()` runs synchronously in
`OnNavigatedTo` (per CLAUDE.md §7, accepted, mirrors the sibling). No change made —
diverging would make this page inconsistent with the existing dashboard.
