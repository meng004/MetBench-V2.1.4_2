# MR Verification v1.2 PWR Migration Map

## Purpose

This note turns `PWR_MR_Analysis_Report.md` into a concrete migration reference for `PR-10`.
It is not the final catalog itself. Its job is to lock three truths before bulk migration:

1. the report's **43 MR + 4 Property** summary claim is not internally consistent with its own detailed tables;
2. not every entry maps to an already runnable SUT in the current repository;
3. migration assets must therefore separate:
   - **typed executable assets** backed by current programs, and
   - **typed non-runnable catalog assets** that still deserialize and validate but are not executed in CI yet.

## Inventory from the report

### Source inconsistency note

`PWR_MR_Analysis_Report.md` contains two conflicting inventories:

- summary / distribution sections claim `43 MR + 4 Property`;
- detailed classification tables and `(r, R)` total tables explicitly enumerate `44` MR-labeled entries plus `4` properties.

For `PR-10`, the repository now treats the **explicitly enumerated inventory** as the migration source of truth:

- `44` explicit MR entries
- `4` explicit Property entries

This keeps the code and coverage gate aligned with the most concrete source layer instead of silently dropping one entry.

### 44 explicit metamorphic relations

- Diffusion:
  - `Dif-Phy-01` .. `Dif-Phy-13` except `Dif-Phy-08` which is a Property
  - `Dif-Alg-01` .. `Dif-Alg-05`
- Boltzmann transport:
  - `Bol-Phy-01` .. `Bol-Phy-05`
  - `Bol-Alg-01` .. `Bol-Alg-03`
- Burnup / Bateman:
  - `Bur-Phy-01` .. `Bur-Phy-03`
  - `Bur-Alg-01` .. `Bur-Alg-02`
- Resonance:
  - `Res-Alg-01`, `Res-Alg-02`, `Res-Alg-04`
- Kinetics:
  - `Kin-Phy-01`, `Kin-Phy-03`
  - `Kin-Alg-01`
- PWR coupled/application:
  - `Cpl-App-01` .. `Cpl-App-08`

### 4 explicit properties

- `Dif-Phy-08`
- `Bur-Phy-04`
- `Res-Alg-03`
- `Kin-Phy-02`

## Current repository-backed programs

These are the current concrete programs/SUTs in the repository that can already support typed v1.2 assets:

- `diffusion_1d`
- `heat_equation`
- `decay_chain`
- `damped_oscillator`
- `lotka_volterra`
- `subchannel_1d`
- `projectile`
- `openmoc`
- `openmc`

## Mapping policy for PR-10

### A. Executable now

These entries should be migrated into runnable or validate-pass typed assets immediately because the repository already has either a direct program/runtime path or a complete typed predicate path for them.

- diffusion-like scalar monotonic / convergence
  - map to `diffusion_1d`
- heat / Fourier monotonic / convergence
  - map to `heat_equation`
- Bateman conservation / timestep convergence
  - map to `decay_chain`
- subchannel monotonic / linearity
  - map to `subchannel_1d`
- projectile scaling
  - map to `projectile`
- oscillator / LV scaling
  - map to `damped_oscillator`, `lotka_volterra`
- transport cross-method / sigma perturbation
  - map to `openmoc`, `openmc`
- already implemented properties
  - `Dif-Phy-08`, `Bur-Phy-04`, `Res-Alg-03`, `Kin-Phy-02`

### B. Validate-only in PR-10

These entries come from the report and belong to the 43/4 denominator, but the current repository does not yet expose a concrete runnable program or execution harness for them.

- high-fidelity core-level diffusion entries that assume:
  - boron search
  - control-rod insertion
  - ADF toggling
  - core symmetry / reflector boundary switching
- coupled PWR application entries that assume:
  - Gd depletion
  - cycle length / SDM / boron worth
  - whole-core loading pattern comparison

For these, `PR-10` should still create typed YAML assets under migration/golden coverage, but CI should treat them as:

- deserializable
- validatable
- non-runnable until a real program path exists

## Immediate implication for PR-10

`PR-10` should not attempt to fake 44 runnable assets from programs that do not exist.
Instead it should produce:

1. `44` typed MR migration assets
2. `4` typed Property assets
3. executable golden fixtures only for the subset backed by current programs
4. explicit coverage accounting that distinguishes:
   - catalog denominator
   - validate-pass count
   - runnable execution count

This keeps the denominator honest without inventing fake executability.
