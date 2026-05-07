# MetBench System-level MT BDD Design

## Purpose

MetBench currently supports method/unit-level metamorphic testing (MT), where the
target is a function or method and the observable result is usually a return
value. The new work extends MetBench to system/acceptance-level MT, where the
target is a complete executable program invoked through a CLI, the input is a
file or case directory, and the observable result is an output file or artifact.

The first implementation target is a minimal system-level MT closed loop:

```text
Gherkin feature
-> Reqnroll step definitions
-> C# system-level MT runner
-> CLI execution of the program under test
-> output file parsing through a Python adapter
-> C# MR assertion
-> pass/fail result
```

The design preserves the existing method-level MT path. System-level MT is added
as a parallel execution model rather than replacing or rewriting the current
unit-level model.

## Scope

### In Scope

- Represent system-level MR scenarios with Gherkin feature files.
- Use Reqnroll as the BDD execution framework.
- Keep WPF as the user-facing interface and C# as the business orchestration
  layer.
- Support CLI invocation of external programs under test.
- Support the first closed loop with file input and file output.
- Use Python adapters for program-specific input/output file conversion and
  parsing.
- Return deterministic pass/fail results for at least one system-level MR.

### Out of Scope for Stage 1

- Automatic source-to-follow-up input generation.
- Randoop integration.
- OpenMOC-specific production adapter implementation.
- OpenMC/OpenMOC cross-program MR reuse.
- Report generation and visualization for system-level MT.
- WPF screens for full task authoring and result browsing.

These items are assigned to later stages and must not block the Stage 1 closed
loop.

## Stage Roadmap

### Stage 1: System-level MT Representation and BDD Execution

Goal: establish the system-level MT closed loop using Gherkin, Reqnroll, C# BLL,
CLI execution, file output parsing, and MR assertion.

Acceptance criteria:

- At least one `.feature` file describes a system-level MR scenario.
- Reqnroll executes the feature and calls C# step definitions.
- C# starts an external example program through CLI.
- Source and follow-up output files are read.
- At least one MR assertion returns pass/fail.
- Existing method-level MT behavior remains unaffected.

### Stage 2: Input Data Generation

Goal: generate follow-up input files from source input files using MR
transformation configuration.

Acceptance criteria:

- A source input file plus an MR transformation config produces a follow-up
  input file.
- The system records source input, follow-up input, transformation parameters,
  and logs.
- Stage 1 execution can consume generated follow-up inputs.
- At least one numeric transformation is supported.
- Generation failures return explicit errors.

### Stage 3: OpenMOC Single-program Application

Goal: apply the Stage 1 and Stage 2 mechanisms to OpenMOC as the first real
scientific computing program.

Acceptance criteria:

- MetBench/Reqnroll starts OpenMOC for a source case.
- The system prepares or generates a follow-up case and starts the second run.
- OpenMOC output files are parsed for MR-relevant values.
- At least one OpenMOC MR executes end to end and returns pass/fail.
- OpenMOC-specific logic remains isolated in a Python adapter.

### Stage 4: Platform Enhancements and Reporting

Goal: add WPF task management, result persistence, reports, batch execution, and
multi-program extension.

Acceptance criteria:

- Users can launch system-level MT tasks from WPF.
- Each run result is persisted and can be reviewed later.
- At least one report format is generated.
- Multiple BDD scenarios can execute in batch.
- A second program adapter design or prototype exists.

## Architecture

System-level MT is organized into six units with clear responsibility
boundaries.

```text
Feature Layer
  Gherkin `.feature` files describe system-level MR scenarios.

BDD Binding Layer
  Reqnroll step definitions translate Given/When/Then steps into C# runner calls.

Business Orchestration Layer
  C# BLL models system-level tasks, cases, commands, outputs, and MR assertions.

CLI Execution Layer
  A C# runner starts external programs with configured working directories,
  arguments, timeout, stdout, stderr, and exit-code handling.

Adapter Layer
  Python adapters parse program-specific output files and return normalized JSON.
  In later stages they will also transform input files.

Assertion Layer
  C# compares normalized source and follow-up outputs against the selected MR.
```

The BDD execution layer must not call Python directly. Reqnroll steps call C#
services. C# services may call Python adapters as subprocesses through a narrow
adapter interface.

## Feature Format

Stage 1 uses MR scenario-level Gherkin. The feature describes the MT workflow and
references named source/follow-up cases, not low-level scientific variables.

Example:

```gherkin
Feature: System-level metamorphic testing through CLI

  Scenario: Output value should increase after configured input increase
    Given a system MT case named "source" with input file "source/input.txt"
    And a system MT case named "follow-up" with input file "followup/input.txt"
    When I run both cases with program profile "example-cli"
    Then the parsed output value "result" of "follow-up" should be greater than "source"
```

This format avoids turning Gherkin into a programming language. The CLI command,
working directory, output path, adapter script, and parser details belong to
configuration and C# task objects.

## Core Data Model

Stage 1 introduces system-level models in `MetBench_BLL` without changing the
existing `FunctionProgram` model.

Required concepts:

- `SystemProgram`: a `TargetProgram` for CLI-invoked programs.
- `SystemMtCase`: one concrete run, including case name, input path, working
  directory, output path, and environment variables.
- `SystemMtTask`: source case, follow-up case, program profile, selected MR, and
  timeout.
- `CliRunResult`: exit code, stdout, stderr, elapsed time, and resolved output
  path.
- `ParsedOutput`: normalized key-value output parsed from a program artifact.
- `SystemMtResult`: source run, follow-up run, parsed outputs, assertion result,
  and failure reason.

All file paths used by the runner should be resolved to absolute paths before
execution. Relative paths in feature files are interpreted relative to the
feature project test data directory.

## Python Adapter Contract

Python adapters are external processes with a stable JSON contract. Stage 1 only
requires output parsing.

Invocation shape:

```text
python adapter.py parse-output --output-file <path-to-output-file>
```

Adapter stdout must be a JSON object:

```json
{
  "values": {
    "result": 42.0
  },
  "metadata": {
    "adapter": "example",
    "outputFile": "absolute/or/resolved/path"
  }
}
```

Adapter stderr is treated as diagnostic text. A non-zero exit code means adapter
failure. Invalid JSON means adapter failure. Missing requested keys mean MR
assertion failure with an explicit reason.

Python adapters must not:

- decide whether the MR passed;
- control source/follow-up execution order;
- write MetBench database records;
- call Reqnroll or inspect feature files.

## CLI Execution Rules

The C# CLI runner owns process execution.

Rules:

- Use `ProcessStartInfo` with `UseShellExecute = false`.
- Set `WorkingDirectory` explicitly.
- Redirect stdout and stderr.
- Enforce a configurable timeout.
- Treat non-zero exit code as run failure unless the program profile explicitly
  marks the code as acceptable.
- Store stdout, stderr, exit code, elapsed time, and output file path in the run
  result.
- Do not parse program-specific output in the CLI runner; parsing belongs to the
  adapter.

## MR Assertion Rules

Stage 1 supports a minimal assertion set sufficient for the first closed loop.

Required assertion:

- `GreaterThan`: follow-up parsed numeric value is greater than source parsed
  numeric value.

The assertion layer should expose an interface so later stages can add equality
with tolerance, monotonic decrease, approximate conservation, range inclusion,
and domain-specific assertions without changing Reqnroll steps.

The assertion output must include:

- MR assertion name;
- source value;
- follow-up value;
- pass/fail;
- human-readable failure reason.

## Error Handling

Stage 1 must distinguish these failure classes:

- Feature binding failure: Gherkin step does not map to a Reqnroll step.
- Configuration failure: program profile, case path, output path, or adapter path
  is missing.
- CLI execution failure: process cannot start, times out, or exits with an
  unacceptable code.
- Output artifact failure: expected output file is missing or unreadable.
- Adapter failure: Python adapter exits non-zero or returns invalid JSON.
- Assertion failure: parsed values are valid but the MR condition is false.

Each failure must produce a message that identifies the failed layer and the
case name when applicable.

## Test Strategy

Stage 1 should be test-driven at two levels.

Unit tests:

- Validate `SystemProgram`, `SystemMtCase`, and `SystemMtTask` path and argument
  handling.
- Verify CLI runner behavior for success, non-zero exit, timeout, and missing
  output.
- Verify adapter JSON parsing and invalid JSON handling.
- Verify `GreaterThan` assertion pass/fail behavior.

BDD integration test:

- Include one Reqnroll feature using a tiny local example CLI program or script.
- Run source and follow-up cases.
- Parse output through an example Python adapter.
- Assert follow-up `result` is greater than source `result`.

Verification commands for Stage 1 implementation should include:

```bash
rtk dotnet test MetBench.sln
```

If the existing WPF project prevents solution-level test execution on non-Windows
machines, the implementation plan should define a separate test project command
for the system-level MT test project.

## Persistence and UI

Stage 1 does not require a full WPF authoring UI or database migration. The C#
models should be designed so later stages can persist task definitions and
results in LiteDB.

Allowed Stage 1 UI work:

- Add a minimal developer-facing command or test entry point.
- Register new services in dependency injection if needed.

Deferred UI work:

- WPF pages for creating system-level MT tasks.
- Result history screens.
- Report generation screens.

## Compatibility

Existing method-level MT must continue to use `FunctionProgram`, `AutoRunMR_Await`,
and the existing Python method-level execution scripts. The Stage 1 system-level
runner must not change their public behavior.

Any shared abstraction added to `TargetProgram` must be backward compatible with
`FunctionProgram`.

## Design Decisions

1. Reqnroll is selected for BDD execution because it keeps the test workflow in
   the .NET ecosystem and integrates naturally with C# step definitions.
2. Python is selected only for program-specific input/output file adaptation
   because scientific computing file formats are easier to handle in Python and
   future OpenMOC/OpenMC adapters will benefit from that ecosystem.
3. MR scenario-level Gherkin is selected instead of variable-level Gherkin to
   keep feature files readable and avoid embedding transformation logic in
   natural-language steps.
4. Stage 1 uses file input and file output because this is closer to OpenMOC and
   OpenMC than method parameters or stdout-only programs.
5. System-level MT is added in parallel with method-level MT to avoid destabilizing
   existing functionality.

## Open Questions Resolved for Stage 1

- BDD framework: Reqnroll.
- UI technology: WPF.
- Business logic language: C#.
- Adapter language: Python.
- Program invocation style: CLI.
- Stage 1 input/output style: file input and file output.
- First implementation target: example CLI closed loop, not OpenMOC production
  integration.

## Approval Gate

This design is ready for implementation planning when the user confirms that:

- Stage 1 should remain focused on the closed loop only;
- OpenMOC-specific work stays in Stage 3;
- Python remains an adapter layer rather than the workflow controller;
- existing method-level MT must remain compatible.
