# Minimum-MR-SubSet PUT Import/Export Staging

This directory contains the cloud-side first version of the A-group import/export staging model for `minimum-mr-subset`.

Scope:

- Included SUTs: P5, P4, P9.
- Excluded SUTs: P8, P3, P10, P1, P2, P6, P7.
- The exported package is a JSON staging artifact only.
- The exporter does not write `SUT/`, live System-MT manifests, LiteDB files, or execution evidence repositories.

Evidence captured in the fixtures:

- External repository: `https://github.com/meng004/Minimum-MR-SubSet.git`
- Source commit: `b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f`
- P5 source path: `experiments/puts/p5_pke.py`
- P4 source path: `experiments/puts/p4_pendulum.py`
- P9 source path: `experiments/puts/p9_openmc.py`
- Shared smoke source path: `tests/puts/test_smoke.py`

Runtime readiness rule:

- Imported assets default to `ImportedOnly`.
- A relation can be marked `RuntimeCandidate` only when both transform and assertion bindings are explicit runtime-supported bindings.
- P9 additionally requires explicit `sigma_k` and noise-aware/statistical assertion semantics because the imported asset is a deterministic OpenMC surrogate, not a live OpenMC runtime binding.
