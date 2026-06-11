# WPF MVVM Convergence Chain — Post-Merge Holistic Review (2026-06-11)

> **CLAUDE.md §12.4 R2 chain-end ritual** for the 4-PR WPF MVVM convergence chain.
> Trigger: the plan [`2026-06-06-wpf-mvvm-convergence-plan.md`](../plans/2026-06-06-wpf-mvvm-convergence-plan.md)
> enumerates `PR-1`…`PR-4` (≥ 3-PR phased delivery, `PR-X-N` naming), so the final
> PR's merge gates "Controlled" in `docs/status/current.md` on this review + its cleanup.

## Chain under review

| PR | Commit | Delivered |
|---|---|---|
| #333 (PR-1/2/3 squash) | `eb62fc0` | drop dead `Prism.Wpf` + 2 dead `using Prism`; drop `PropertyChanged.Fody` global weave + delete `FodyWeavers.xml`/`.xsd`; convert 9 legacy XAML `s:Action`/`s:View.ActionTarget` → `Microsoft.Xaml.Behaviors`; (out-of-plan) extract `UiDialog`/`EventAggregator`/`ClientThemeController`, add ~40 `[RelayCommand]` methods |
| #346 (PR-3 guards) | `32f1c27` | add `WpfMvvmConvergenceGuardTests` (7 source-guards) |
| #348 (PR-4 code) | `2977af6` | migrate 6 hand-written `OnPropertyChanged(` sites to `[ObservableProperty]`/`SetProperty`/`[NotifyPropertyChangedFor]` |
| #350 (VM evidence) | `580e718` | VM runtime-verification evidence (docs/screenshots only) |

Base before chain: `c01de21` (#332 plan registration). Cumulative code diff reviewed:
`git diff c01de21..580e718 -- MetBench_Client/ MetBench_SystemMT.Tests/SystemMT/Architecture/WpfMvvmConvergenceGuardTests.cs`.

## Method

Independent fresh-context review agent (no prior session state) over the cumulative
diff, checking ObservableProperty name-mapping, SetProperty change-detection semantics,
`[NotifyPropertyChangedFor]` wiring, XAML `s:Action`→Behaviors fidelity, guard
completeness (R1/R4), and cross-PR consistency. Each finding below was independently
re-verified against source at `c01de21` and `580e718`.

## Verdict: FINDINGS (3) — 1 P1, 2 P2. No P0.

### F1 (P1, verified) — `MouseDoubleClick="{s:Action show}"` dropped from MRRecommendationPage DataGrid without flagging

- **Where**: `MetBench_Client/Views/Pages/MRRecommendationPage.xaml`. Base `c01de21` lines 38–39 had `s:View.ActionTarget="{Binding ViewModel}"` + `MouseDoubleClick="{s:Action show}"` on the results DataGrid; HEAD `580e718` dropped both with **no** `i:Interaction.Triggers` replacement (the page declares no `xmlns:i`).
- **Why not P0/P1-functional**: the Stylet target method `show` **never existed** on `MRRecommendationViewModel` — at base the VM had only `showMessageAsync` (verified `c01de21:MetBench_Client/ViewModels/MRRecommendationViewModel.cs:154`, no `show()` / `ShowCommand`). The binding was already dead/dangling (Stylet would no-op + log). **No working feature was lost.**
- **The real defect**: it was dropped **silently**, inconsistent with the sibling conversions on `MRManagementPage.xaml:52` and `ApplicationManagementPage.xaml:40` (`MouseLeftButtonUp="{s:Action show}"` → `EventTrigger`+`InvokeCommandAction Command="{Binding ViewModel.ShowSelectedCommand}"`), and undocumented (CLAUDE.md §6 显式报错).
- **Disposition**: **Accepted as dead-code removal, documented here.** Not restored — restoring a binding to a non-existent method would add a never-functional path (CLAUDE.md §0.5 禁止自发添加). If double-click-to-open on the recommendation grid is ever desired, it is a *new feature*: add a real `[RelayCommand]` to `MRRecommendationViewModel`, declare `xmlns:i`, and add the `EventTrigger`/`InvokeCommandAction` matching MRManagementPage.

### F2 (P2, verified) — hand-rolled `EventAggregator.Publish` is synchronous (latent off-thread risk)

- **Where**: `MetBench_Client/Services/EventAggregator.cs` (new in #333) — `Publish<TMessage>` invokes `handler.Handle(message)` synchronously on the publishing thread, replacing Stylet's `IEventAggregator` whose default `Publish` marshalled `Handle` onto the dispatcher.
- **Why P2 not P1**: handlers (e.g. `MRManagementViewModel.Handle(...)` → `reload_ItemsSource()`) reassign an `ObservableCollection` bound to a `DataGrid`; if ever `Publish`-ed off-thread this throws. **All current call sites are inside UI command handlers already on the dispatcher**, so it works today. This is a latent regression vs Stylet's threading guarantee, not a present break.
- **Disposition**: **Accepted with documented contract.** The implicit contract is "Publish from the UI thread." No code change in this cleanup (would touch WPF source + require recompile; risk > value for a latent P2). Future hardening option: document the UI-thread contract on `IEventAggregator.Publish` or marshal `Handle` via `Dispatcher`.

### F3 (P2, verified) — `No_ViewModel_calls_OnPropertyChanged_manually` guard never scanned `Models/`

- **Where**: `MetBench_SystemMT.Tests/SystemMT/Architecture/WpfMvvmConvergenceGuardTests.cs` — `ClientViewModelFiles()` scans only `MetBench_Client/ViewModels/`. But PR-4 also migrated `ApplicationEx.IsChecked` and `DomainEx.IsChecked` under `MetBench_Client/Models/`. A re-introduced manual `OnPropertyChanged(` in `Models/` would pass the guard silently (R1/R4 guard-completeness gap). Repo is currently clean (verified: zero `OnPropertyChanged(` anywhere in `MetBench_Client`).
- **Disposition**: **Fixed in this cleanup (CLAUDE.md §12.5 — finding → guard line).** Added 8th guard `No_Model_calls_OnPropertyChanged_manually` scanning `MetBench_Client/Models/`. Guard set now 7 → **8**; focus suite `WpfMvvmConvergence` = **8/8 green** on VM.

## Verified clean (coverage the reviewer confirmed)

- **ObservableProperty name-mapping**: `ApplicationEx`/`DomainEx` are `partial : ObservableObject`; `[ObservableProperty] private bool _isChecked` → generated `IsChecked` matches XAML/external refs.
- **`[NotifyPropertyChangedFor]`**: `SystemMtResultViewModel` attribute on `_isHistoricalView` raises derived `IsBinaryView`; the now-redundant manual raise was removed.
- **SetProperty side-effects**: `MTReportGeneratorViewModel.SelectedValue` **already** short-circuited on equality at base, so `SetProperty` is identical semantics (no skipped side-effect). The two `SelectedText` custom getters (class-name → empty-string fallback) are preserved.
- **Other 8 XAML event conversions** (ApplicationManagementPage:40, MRManagementPage:52, MTExecutionPage:185, MRDisplayPage:203, ApplicationProgramsWindow OK/Cancel) are faithful; all 5 files using `i:Interaction` declare `xmlns:i`.
- **No dangling references**: zero `using Prism`/`using Stylet`/`xmlns:s=`/`s:Action`/`PropertyChanged.Fody`/`RaisePropertyChanged` in `MetBench_Client`; csproj references only `CommunityToolkit.Mvvm` + `Microsoft.Xaml.Behaviors.Wpf`; `FodyWeavers.xml`/`.xsd` deleted.

## Chain closure

With F3 fixed (guard line) and F1/F2 dispositioned (accepted + documented), the chain
meets R2 closure. This cleanup PR also moves the three projection docs to Controlled:
- plan header → "4 PR 全部完成 + 8 guard 全绿 + VM 运行时证据齐 + chain-end review done";
- active-plan-index row → Active → Expired;
- `docs/status/current.md` §6 WPF MVVM row → Controlled (+ VM evidence + this review pointer).
