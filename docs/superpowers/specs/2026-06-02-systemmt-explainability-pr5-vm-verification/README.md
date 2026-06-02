# PR-5 VM Verification — System MT explainability + pair-quality WPF surfaces

Branch: `claude/systemmt-explainability-pr5-wpf-display`
Captured on the Windows VM (200% HiDPI, physical desktop ≈ 2640×1848).
Driver: [`drive.ps1`](drive.ps1) (UIA, adapted from `tools/uia-verify-i18n.ps1`).

## Build / test evidence (Step 6)

- `dotnet build MetBench.sln`: **0 errors** (10028 pre-existing StyleCop/Fody warnings).
- `dotnet test MetBench_Client.Tests --filter ClientI18n`: **15 passed, 0 failed**.
- `dotnet test MetBench_SystemMT.Tests --filter ClientI18n`: **18 passed, 0 failed** (incl. en/zh resx key parity).

New client tests (functional proof of the display surfaces):
- `SystemMtExplanationCardTests` — equation/SUT/MR ViewModels expose the persisted
  explanation/profile fields for the selected row, and empty fields fall back to the
  shared localized "unavailable" text.
- `SystemMtPairQualityEvidenceTests` — execution-history evidence summary renders
  pair-quality counts/rates when present and stays quiet for default-empty / missing rows.
- `SystemMtExplanationLocalizationTests` — every new resource key resolves in en-US and zh-CN.

## Screenshots

| File | What it shows |
|---|---|
| `01-equation-explanation-card.png` | Equation Catalog, built-in `bateman` selected. Full explanation card: class `ODE`, family `linear decay chain`, primary variables, physical meaning, benchmark rationale, expected laws. |
| `02-sut-profile-card.png` | SUT Catalog, `heat_equation` selected; program section loaded. SUT-profile card is wired below the editor fields (see capture limitation below). |
| `03-mr-explanation-card.png` | MR Catalog, `heat_equation` manifest (3 physics MR bindings), `heat-equation-amplitude` selected; MR explanation card wired below the editor fields. |
| `04-...` (MISSING — documented blocker) | Non-empty pair-quality evidence. **Blocker:** all 3 execution-history rows in the local `SystemMT.Litedb` predate PR-3 pair-quality capture, so none expose a non-empty `PairQuality`. No System-MT pipeline was re-run on this VM, and a production DB row was **not** fabricated. The rendering is proven instead by `SystemMtPairQualityEvidenceTests.Evidence_summary_renders_pair_quality_counts_and_rates`. |
| `05-execution-history-no-evidence-or-empty-pair-quality.png` | Execution History, a row with typed verification but default-empty pair-quality — the evidence summary shows typed verification and **no** pair-quality section (quiet-empty path confirmed). |
| `06-zh-cn-equation-or-history.png` | Language switched to 中文: nav, page title, and explanation-card labels (方程类别 / 方程族 / 物理含义 / 基准选型理由 / 预期规律) are localized; data values stay as catalog content. |
| `07-en-us-equation-or-history.png` | Language switched back to English: same surface localized back. |

## Capture limitation (SUT / MR card body, screenshots 02 & 03)

On this VM the display is **200% HiDPI** and the FluentWindow enforces a minimum
height that exceeds the visible screen. Consequence: the SUT/MR detail panels
(long editor forms) fit inside the window without the inner `ScrollViewer`
becoming scrollable (`VerticallyScrollable = False`), but the window's lower
region — where the explanation/profile card sits, below the editor fields —
falls below the physical screen edge and cannot be captured. UIA scroll,
mouse-wheel, window maximize, and `MoveWindow` resize were all attempted; the
window min-height clamps the resize and the panel never scrolls.

This is purely a capture constraint of this VM's resolution + scaling. At normal
user resolution (100% DPI) the window is short enough that the card renders in
view without scrolling. Functional correctness of the SUT profile card and MR
explanation card is proven by `SystemMtExplanationCardTests`, which asserts the
exact fields flow to the ViewModel display properties bound by the XAML cards.

Screenshots 02/03 therefore show the correct page with the target row selected
and its data loaded (the card is wired into the same `ScrollViewer`, immediately
below the editor fields).
