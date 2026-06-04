# Minimum-MR-SubSet PUT Import/Export Staging

This directory contains the cloud-side import/export staging model for `minimum-mr-subset`.

Scope:

- Included staged SUTs: A group P5, P4, P9; B group P8, P3.
- Excluded SUTs: P10, P1, P2, P6, P7.
- The exported package is a JSON staging artifact only.
- The exporter does not write `SUT/`, live System-MT manifests, LiteDB files, or execution evidence repositories.
- A group has separate live runtime promotion evidence; B group remains import-only until its runtime promotion stage lands.

Evidence captured in the fixtures:

- External repository: `https://github.com/meng004/Minimum-MR-SubSet.git`
- Source commit: `b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f`
- P5 source path: `experiments/puts/p5_pke.py`
- P4 source path: `experiments/puts/p4_pendulum.py`
- P9 source path: `experiments/puts/p9_openmc.py`
- P3 source path: `experiments/puts/p3_lorenz.py`
- P8 source path: `experiments/puts/p8_schrodinger.py`
- Shared smoke source path: `tests/puts/test_smoke.py`

Evidence limits:

- Local external P3/P8 smoke was not claimed by this staging model; the observed external sources import NumPy, and P3 also imports SciPy.
- No P3/P8 detection matrix was observed in the external source tree; P3/P8 detection records are staged as `Inconclusive`.

Runtime readiness rule:

- Imported assets default to `ImportedOnly`.
- A relation can be marked `RuntimeCandidate` only when both transform and assertion bindings are explicit runtime-supported bindings.
- P9 additionally requires explicit `sigma_k` and noise-aware/statistical assertion semantics because the imported asset is a deterministic OpenMC surrogate, not a live OpenMC runtime binding.
