# Debt #5 — WPF Interactive Screenshot Verification Plan (VM)

> **For the VM operator (Windows + display):** The code is done — WPF compiles + boots (commit `7766170`),
> cloud tests green, [PR #236](https://github.com/meng004/MetBench-V2.1.4_2/pull/236) is **draft**. The only
> thing left to flip it to ready+merge is **7 interactive GUI screenshots** that a headless agent can't
> capture. Execute task-by-task; each has 前置条件检查 / 操作步骤 / 验收(截图). Companion: the screenshot list
> in [`../specs/2026-05-29-debt5-vm-prompt.md`](../specs/2026-05-29-debt5-vm-prompt.md).

**Goal:** Visually confirm the enum-ized Anomaly status surfaces correctly in the WPF AnomalyList page, then push the screenshots so PR #236 can go ready.

**Branch:** `followup/debts-2026-05-29`. **Save all screenshots to:** `docs/superpowers/specs/2026-05-29-debt5-vm-verification/` (exact filenames below).

**State machine under test:** `new → investigating`; `investigating → {known, confirmed-bug, false-positive, fixed-upstream}`. Everything else is illegal and must throw `InvalidAnomalyStatusTransitionException` (surfaced to the page's `ErrorMessage`, no crash). Status column renders kebab via `AnomalyStatusKebabConverter`.

---

## Task 0: Setup + seed data

**前置条件检查:**
- [ ] `git switch followup/debts-2026-05-29 && git pull --ff-only` → at commit `7766170` or later.
- [ ] `mkdir -p docs/superpowers/specs/2026-05-29-debt5-vm-verification` (screenshot target dir).

**操作步骤:**
- [ ] **Step 1 — build:** `dotnet build MetBench_Client/MetBench_Client.csproj` → 0 errors. **Screenshot this terminal output** as `01-build-success.png`.
- [ ] **Step 2 — seed 2 cross-program anomalies** (gives 2 rows with `Status=investigating`, `Category=cross-program-disagreement`):
  ```
  dotnet run --project tools/SeedCrossProgramAnomalies -- \
    --input docs/experiments/cross-program-anomalies-2026-05-28.json \
    --db <path-to-the-LiteDB-the-WPF-app-reads>
  ```
  (Both `--input` and `--db` are **REQUIRED** — a bare `dotnet run --project tools/SeedCrossProgramAnomalies`
  prints usage and exits 64. The seeder registers the AnomalyStatus BSON serializer itself per
  `AnomalyStatuses.RegisterBsonMapping`. For an isolated run set `METBENCH_DB_PATH` and point `--db` at it.)
- [ ] **Step 3 — launch:** `dotnet run --project MetBench_Client`; navigate to the **Anomaly** list page.

**验收条件:** build 0 errors (captured); app boots to AnomalyList showing ≥2 rows.

---

## Task 1: Status column renders kebab (converter works)

**前置条件检查:** App on AnomalyList page with the 2 seeded rows visible.

**操作步骤:**
- [ ] Look at the **Status** column of the DataGrid.

**验收条件:**
- [ ] Status cells show **kebab text** (`investigating`), NOT `2` (raw int) and NOT `Investigating` (enum ToString).
- [ ] Screenshot the grid → `02-anomalylist-kebab-status.png`.

---

## Task 2: Filter by status = investigating

**前置条件检查:** Same page.

**操作步骤:**
- [ ] In the **Status** filter ComboBox (top filter bar), select `investigating`.

**验收条件:**
- [ ] Grid narrows to only `investigating` rows (the 2 seeded rows remain; any non-investigating rows disappear).
- [ ] Screenshot → `03-filter-by-investigating.png`.
- [ ] Reset the filter to empty afterward.

---

## Task 3: Legal transition — investigating → known

**前置条件检查:** Filter cleared; ≥2 investigating rows present. Use **row A** (first seeded row).

**操作步骤:**
- [ ] Select **row A** in the grid.
- [ ] In the **transition** ComboBox (bottom), select `known`.
- [ ] Click the **Transition** button.

**验收条件:**
- [ ] No error; row A's Status cell now shows `known`; an audit row is written (`anomaly.status-change`, `{"from":"investigating","to":"known"}`).
- [ ] Screenshot showing row A = `known` → `04-transition-legal-investigating-to-known.png`.

---

## Task 4: Illegal transition — rejected, no mutation, no crash

**前置条件检查:** Use **row B** (the second seeded row, still `investigating`). (`investigating → investigating` is a self-loop, not in the transition table → illegal.)

**操作步骤:**
- [ ] Select **row B**.
- [ ] In the transition ComboBox, select `investigating` (self-loop = illegal from `investigating`).
- [ ] Click **Transition**.

**验收条件:**
- [ ] An **error message** appears (the `InvalidAnomalyStatusTransitionException` surfaced via `ErrorMessage`); **the app does NOT crash**; row B's Status is **still `investigating`** (unchanged).
- [ ] Screenshot showing the error + unchanged status → `05-illegal-transition-rejected.png`.
- [ ] (Optional, if a `new`-status row exists from a real MT run: also try `new → confirmed-bug` for a cleaner illegal example.)

---

## Task 5: Cross-program rows intact (migration didn't break old data)

**前置条件检查:** Same page.

**操作步骤:**
- [ ] Locate the cross-program rows (`Category = cross-program-disagreement`; the ScaleModeratorSigmaA / ScaleFuelSigmaT findings).

**验收条件:**
- [ ] Both cross-program rows display normally — Severity, Category, kebab Status all render; row B still `investigating`, row A now `known` (from Task 3).
- [ ] Screenshot → `06-cross-program-rows-intact.png`.

---

## Task 6: LiteDB stores Status as int (not string)

**前置条件检查:** Have a LiteDB viewer (LiteDB Studio) or use the dump approach below. DB is the shared `MetBench_DataBase/*.litedb` (per `DbConfig._conn`), collection `Anomalies`.

**操作步骤:**
- [ ] Open the `Anomalies` collection in LiteDB Studio (or `db.Anomalies.find()` via the LiteDB CLI).

**验收条件:**
- [ ] The `Status` field is stored as an **integer** (e.g. `2` for investigating, `3` for known), NOT a string (`"investigating"`). Confirms `AnomalyStatuses.RegisterBsonMapping` int serialization + the string→int migration ran.
- [ ] Screenshot → `07-litedb-status-int.png`.

---

## Final: commit, push, report

**前置条件检查:** All 7 screenshots present in `docs/superpowers/specs/2026-05-29-debt5-vm-verification/`.

**操作步骤:**
- [ ] `git add docs/superpowers/specs/2026-05-29-debt5-vm-verification/`
- [ ] `git commit -m "test(t5): WPF AnomalyList enum interactive verification screenshots (debt #5 4E)"`
- [ ] `git push origin followup/debts-2026-05-29`

**验收条件:**
- [ ] 7 screenshots pushed.
- [ ] **Report back to cloud**: which screenshots passed, any visual surprise (e.g. status not rendering kebab, error not surfacing). Cloud then flips PR #236 to ready, re-checks base, and merges per §12.
- [ ] If any acceptance FAILS (kebab not shown / illegal transition crashed / Status stored as string): **do not push pass** — report the failure with the screenshot so cloud can fix the code side.
