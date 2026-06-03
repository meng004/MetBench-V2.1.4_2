# Windows VM Prompt: Minimum-MR-SubSet A-Group Import/Export Verification

Use this file only when Windows evidence is requested.

User instruction to run this task:

```text
Read docs/superpowers/vm-prompts/2026-06-02-minimum-mr-subset-a-group-windows-vm-prompt.md and execute the verification prompt.
```

## Scope

This A-group import/export work is intended to be cloud/Linux-safe. It should not require WPF or VM validation unless a later implementation edits `MetBench_Client/`, XAML, Windows app startup, appsettings binding, or UI-visible workflows.

## Preconditions

- Checkout the PR branch that implements `docs/superpowers/plans/2026-06-02-minimum-mr-subset-a-group-import-export-plan.md`.
- Confirm the branch does not contain WPF edits unless the PR explicitly says it does.
- Use Windows PowerShell or the existing VM shell conventions.

## Core Steps

1. Run:

   ```powershell
   git status -sb
   ```

2. Inspect changed files:

   ```powershell
   git diff --name-only origin/main...HEAD
   ```

3. If no files under `MetBench_Client/`, `*.xaml`, Windows startup/config, or UI resources changed, record:

   ```text
   Windows UI evidence not required for this cloud-side import/export PR.
   ```

4. If UI or Windows-specific files did change, stop and request a scoped VM UI plan before continuing.

5. If the PR author requests Windows compile confirmation, run:

   ```powershell
   dotnet build MetBench.sln
   ```

   Record the exact result: errors, warnings, and whether the command completed.

## Acceptance Standard

- VM report states whether Windows evidence was required.
- If no Windows-specific files changed, report that classification with the changed-file list.
- If `dotnet build MetBench.sln` is run, report exact success/failure output; do not summarize a failed build as usable.

## What Not To Do

- Do not execute imported `minimum-mr-subset` SUTs inside the VM unless a separate task asks for it.
- Do not create or edit WPF UI for import/export in this prompt.
- Do not mark imported MRs as runtime-ready from VM evidence alone.
