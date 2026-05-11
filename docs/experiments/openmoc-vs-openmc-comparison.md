# OpenMOC vs OpenMC — cross-program MR comparison

Test case: 2D pin-cell, 2-group MGXS, reflective boundaries (`SUT/openmoc/sample/pincell.json`).

MR factor: **1.5**

## k_eff results

| Case | OpenMOC | OpenMC | OpenMC − OpenMOC | (OpenMC − OpenMOC) / OpenMOC |
|------|---------|--------|-------------------|------------------------------|
| Source | 1.13306 | 1.12450 ± 0.00179 | -0.00856 | -0.7553% |
| ScaleNuSigmaF (factor=1.5) | 1.69990 | 1.68996 ± 0.00252 | -0.00994 | -0.5849% |
| ScaleFuelSigmaA (factor=1.5) | 0.80690 | 0.80272 ± 0.00132 | -0.00419 | -0.5189% |

## MR direction verification

| Solver | ScaleNuSigmaF (k_followup > k_source) | ScaleFuelSigmaA (k_followup < k_source) |
|--------|---------------------------------------|-----------------------------------------|
| openmoc | ✅ PASS (1.69990 > 1.13306) | ✅ PASS (0.80690 < 1.13306) |
| openmc | ✅ PASS (1.68996 > 1.12450) | ✅ PASS (0.80272 < 1.12450) |

## Cross-solver consistency

Both solvers should respond to the MR in the same direction. Compare
the k_eff ratios `k_followup / k_source` per solver:

| MR | OpenMOC ratio | OpenMC ratio | Δratio |
|----|---------------|--------------|--------|
| ScaleNuSigmaF | 1.50028 | 1.50286 | +0.00258 |
| ScaleFuelSigmaA | 0.71215 | 0.71384 | +0.00170 |

## Raw outputs

```json
{
  "openmoc": {
    "source": {
      "k_eff": 1.1330583459569659,
      "iterations": 553,
      "converged": true,
      "metadata": {
        "runner": "openmoc"
      }
    },
    "nu_sigma_f": {
      "k_eff": 1.6999039264109281,
      "iterations": 581,
      "converged": true,
      "metadata": {
        "runner": "openmoc"
      }
    },
    "sigma_a": {
      "k_eff": 0.8069041098845144,
      "iterations": 464,
      "converged": true,
      "metadata": {
        "runner": "openmoc"
      }
    }
  },
  "openmc": {
    "source": {
      "k_eff": 1.1245000140252943,
      "k_eff_std": 0.00178685228117027,
      "batches": 60,
      "particles": 5000,
      "converged": true,
      "metadata": {
        "runner": "openmc",
        "energy_mode": "multi-group"
      }
    },
    "nu_sigma_f": {
      "k_eff": 1.6899608591871278,
      "k_eff_std": 0.0025165741998431227,
      "batches": 60,
      "particles": 5000,
      "converged": true,
      "metadata": {
        "runner": "openmc",
        "energy_mode": "multi-group"
      }
    },
    "sigma_a": {
      "k_eff": 0.8027171860617888,
      "k_eff_std": 0.0013193223082996342,
      "batches": 60,
      "particles": 5000,
      "converged": true,
      "metadata": {
        "runner": "openmc",
        "energy_mode": "multi-group"
      }
    }
  }
}
```