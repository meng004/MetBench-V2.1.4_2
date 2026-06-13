# SP1 全运行时真实跑通证据汇总

日期：2026-06-13
机器：Windows 11 + Docker Desktop 29.5.3
镜像：`metbench-runtime:latest`（`docker/Dockerfile.runtime` = `metbench-sut:latest` + .NET 8 SDK）
digest `sha256:bc4a4a3d1fd1a5ea1a4b0a11400c7fc8fc1fa45ca04d375f62db6ced94f07ca7`
依据：spec/plan `docs/superpowers/{specs,plans}/2026-06-13-sp1-all-real-runtime-acceptance-*`

## 1. 容器内预检（三运行时可导入）

```
/opt/openmoc-venv/bin/python -c 'import openmoc'        → OPENMOC_OK
/opt/openmc-venv/bin/python  -c 'import openmc'         → OPENMC_OK
/opt/openmc-venv/bin/python  -c 'import scipy.integrate'→ SCIPY_OK
```
（scipy 取自 openmc venv 的 system-site-packages，故 `METBENCH_SCIPY_PYTHON=/opt/openmc-venv/bin/python`。）

## 2. 整套测试结果（容器内）

命令见 `docs/uat/sp1-all-real-runtime-runbook.md` §3。trx：`sp1-all-real.trx`（本目录）。

```
Passed: 1895   Failed: 0   Skipped: 6   Total: 1901   Duration: 2m46s
```

**全套 0 failed。**

## 3. 运行时类测试逐项 Passed（SP1 判据，从 trx 摘出）

3 个新增异步作业路径测试（本 PR）：
- ✅ `Async_job_runs_scipy_mr_end_to_end`
- ✅ `Async_job_runs_openmoc_mr_end_to_end`
- ✅ `Async_job_runs_openmc_mr_end_to_end`

scipy 端到端：
- ✅ `RunAsync_scipy_ivp_lv_prey_growth_monotone_passes_end_to_end`
- ✅ `RunAsync_scipy_ivp_lv_step_convergence_passes_end_to_end`
- ✅ `RunAsync_scipy_bvp_poisson_source_superposition_passes_end_to_end`
- ✅ `RunAsync_scipy_bvp_poisson_mesh_richardson_passes_end_to_end`

openmoc 端到端 + smoke：
- ✅ `RunAsync_ray_track_convergence_passes_end_to_end_with_default_phases`
- ✅ `RunAsync_ray_track_convergence_writes_calibrated_phase_inputs`
- ✅ openmoc runner/adapter/parser smoke 系列

openmc 端到端 + smoke：
- ✅ `RunAsync_particle_count_convergence_passes_end_to_end_with_default_factor`
- ✅ `RunAsync_particle_count_convergence_observed_followup_stderr_is_smaller_than_source`
- ✅ `Runner_solves_sample_pincell_and_writes_keff_json` 等

跨程序 BDD（openmoc × openmc 同 MR）：
- ✅ `ScaleNuSigmaF increases k_eff regardless of solver`（solver=openmoc / openmc）
- ✅ `ScaleFuelSigmaA decreases k_eff regardless of solver`（solver=openmoc / openmc）

**运行时类全部 Passed，0 skipped 0 failed。**

## 4. 6 个 skip 全为范围外（非运行时类，SP1 不负责）

| skip 测试 | 数 | 门控原因 | 归属 |
|---|---|---|---|
| `McpThreeCaseAcceptanceTests.Acceptance_1/2/3` | 3 | 需 `METBENCH_MCP_ACCEPTANCE_*` 指向**实时 MCP server** | MCP 三用例验收（`2026-06-12-mcp-three-case-acceptance-vm-evidence/`）已单独覆盖 |
| `MinimumMrSubsetBGroupExternalSourceSmokeTests`（External_*） | 3 | 需**外部 P3/P8 源** + pytest；外部 P8 依赖已移除的 `np.trapz` | 状态账本记 BLOCKED，属外部源接入议题，非 MetBench 自有运行时 |

这 6 个与 scipy/openmoc/openmc 运行时无关，装运行时也不会让它们转真跑——故不计入 SP1 的"运行时类 0 skip"判据。

## 5. CI 边界

本次未改 `.github/workflows/dotnet-test.yml`。CI（ubuntu 不装 venv）上运行时类测试继续 skip、继续绿。
"0 skip 全真跑"是容器内事实，作为归档证据，不进 CI 必跑门禁（符合 CLAUDE.md §8）。

## 6. 结论

SP1 达成：catalog 全部运行时类 MR（scipy 4 + openmoc 3 + openmc 3）的端到端测试 + 3 个新异步作业
路径测试在真实运行时下全部真跑通过，全套 0 failed。system 类 32 MR 本就无条件真跑。
距"全部 SUT/MR 真实异步跑通"仅余范围外的外部源 smoke（BLOCKED）与需实时 server 的 MCP 验收（已另证）。
