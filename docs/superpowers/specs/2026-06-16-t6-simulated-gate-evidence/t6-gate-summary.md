# T6 mutation: SIMULATED-runner UI gate (assessment P1)

> Date: 2026-06-16 · addresses the assessment's **#1 top risk**: the in-app
> WPF mutation campaign uses a deterministic simulator, so any kill-rate /
> detection-rate it shows is fabricated and could be mistaken for a real
> measurement.

## Change

`MutationCampaignViewModel.StubCellRunner` is the only cell-runner wired in the
WPF app; its XML doc + per-row `Notes` already say "SIMULATED (T6 Prototype)",
but the headline stats (detection-rate, detected/missed counts) carried no
visible warning. Added an **always-visible warning banner** at the top of the
campaign action area (`MutationCampaignPage.xaml`, Row 2, directly above the
`Start campaign` button and above the summary card + results grid):

> ⚠ SIMULATED — T6 prototype: this campaign uses a deterministic stub
> cell-runner (StubCellRunner). The detection-rate / outcomes shown are
> placeholders, NOT real measurements. Real mutation runs execute offline via
> tools/mutation_study.py.

Two localized resx keys added (en + zh-CN): `Mutation_SimulatedBadge`,
`Mutation_SimulatedTooltip`. No ViewModel/logic change — the banner is static
localized text, so it shows before a run (warning at decision time) and stays
above the numbers once a run produces them.

This is the lower-risk variant of the recommendation (gate the UI). The deeper
variant — a launcher-backed real cell-runner needing a per-run SUT-root override
on `LauncherOptions` — remains deferred (documented in the StubCellRunner XML
doc and the maturity plan).

## Verification

- `dotnet build MetBench_Client.csproj` (Release) → 0 errors.
- FlaUI/UIA (`tools/uia-acceptance`): navigate to the Mutation page → the step
  `assertname:⚠ SIMULATED` **passed** (banner present in the UI tree), and the
  page-tree dump (`t6-tree.txt`) shows both banner TextBlocks
  (`⚠ SIMULATED` + the full prototype warning) immediately above the
  `Start campaign` button. Screenshot: `t6-03-banner.png` (the banner sits below
  the selection panels at the default window size; it is always rendered, as the
  assert + dump confirm).

## Incidental finding (recorded, not fixed here)

While driving a real campaign run to populate the summary card, the in-app
campaign failed with `Cannot insert duplicate key in unique index
'MutResult_Composite'` for an already-present `(MutantId, MRBindingId)` pair —
i.e. the in-app path cannot re-run a campaign over mutant×binding pairs that
already have a persisted result. This is a separate pre-existing T6-immaturity
quirk (independent of this banner) and reinforces why the simulated-vs-real gate
matters; left for the deferred launcher-backed T6 work.
