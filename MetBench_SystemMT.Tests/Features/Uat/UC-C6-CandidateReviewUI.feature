Feature: UAT UC-C6 — Candidate Review UI (manual only)

  Rubric: docs/uat/acceptance-rubric.md §C6
  Pass criterion: 列表非空 + Promote 入正式 MR 表
  Note: UI-only case. Cloud agent cannot drive WPF; manual screenshot verification
  required per docs/uat/test-procedures.md.

  Scenario: UC-C6 is documented for manual UI verification
    Then UAT case "UC-C6" is documented for manual UI verification in "docs/uat/test-procedures.md"
