# 2026-06-16 Quality Follow-up Plan

## Scope

This plan executes the immediate follow-up from the 2026-06-16 project-T
assessment without changing System MT semantics or widening T6 research scope.

In scope:

- Add a mechanical guard that .NET test projects can emit coverage artifacts.
- Replace legacy `NotImplementedException` placeholders on WPF window/navigation
  and v1 DAL repository surfaces with deterministic behavior.
- Add an async markdown report-generation path and route async artifact export
  through it.
- Record that T6 F-T6-02 remains a separate research/implementation plan, not a
  drive-by fix.

Out of scope:

- Implementing semantic mutation operators, equivalent mutant detection, or
  minimum MR subset search.
- Changing System MT runtime semantics, typed catalog predicates, or launcher
  boundaries.
- Windows visual validation beyond source-level WPF contract guards.

## Acceptance Criteria

- `MetBench_SystemMT.Tests` and `MetBench_Client.Tests` include `coverlet.collector`
  so `dotnet test --collect:"XPlat Code Coverage"` is available.
- Source guards fail if `MetBench_Client/Views/Windows` reintroduces
  `throw new NotImplementedException`.
- `ApplicationRepository.Get(Application)` and
  `MetamorphicRelationRepository.Get(MetamorphicRelation)` return filtered
  collections instead of throwing.
- `ExecutionArtifactExporter.ExportAsync` awaits markdown generation when a
  markdown report is requested.
- Verification records the current local limitation if `dotnet` is unavailable.

## T6 Follow-up Boundary

F-T6-02 is still not complete. The next implementation plan must define:

- semantic/syntactic mutation operator taxonomy,
- equivalent mutant detection criteria,
- objective minimum-MR-subset search metric,
- persisted evidence shape and acceptance tests.

This work should be planned as a dedicated T6 PR, because it affects research
validity, mutation result interpretation, and report projections.
