# V12 Coverage Dashboard

## PR-10 current snapshot

- Explicit migration inventory: `44` MR + `4` Property
- Migration validation gate: `44/44` MR valid, `4/4` Property valid
- Golden fixture buckets present:
  - `pass`
  - `fail`
  - `missing`
  - `invalid`

## Source note

The upstream PWR report summary claims `43 MR + 4 Property`, but its detailed classification and `(r, R)` tables enumerate `44` MR entries plus `4` properties.
PR-10 therefore gates against the **explicitly enumerated inventory**, while retaining the inconsistency note in:

- `docs/superpowers/specs/2026-05-25-v12-pwr-migration-map.md`

## CI gate intent

The repository test suite now protects these minimum facts:

1. all migration bundle documents deserialize under v1.2 typed schema;
2. all migrated documents pass `Validate()`;
3. invalid migration spec count remains `0`;
4. golden fixture buckets remain structurally complete.
