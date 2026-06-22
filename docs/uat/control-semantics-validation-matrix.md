# Control Semantics Validation Matrix

This matrix binds each System MT control semantic to at least one executable
test in each environment: API, Business MCP, and Runtime MCP.

| Semantic | Environment | Expected behavior | Executed test |
|---|---|---|---|
| `workflow` | API | API does not expose workflow as a public resource. | `SystemMtControlPlaneBoundaryTests.Semantic_validation_matrix_has_environment_specific_guard` |
| `workflow` | Business MCP | Business MCP does not expose workflow as a public tool/resource. | `SystemMtControlPlaneBoundaryTests.Semantic_validation_matrix_has_environment_specific_guard` |
| `workflow` | Runtime MCP | Runtime MCP does not expose workflow as a runtime command/resource. | `SystemMtControlPlaneBoundaryTests.Semantic_validation_matrix_has_environment_specific_guard` |
| `job` | API | API exposes durable job routes under `/jobs`. | `SystemMtApiEndpointsTests.GetJobAsync_returns_snapshot_from_control_plane` |
| `job` | Business MCP | Business MCP `get_job` targets `/api/v1/systemmt/jobs/{job_id}`. | `MetBenchBusinessServerTests.test_dispatch_get_job_uses_job_oriented_url` |
| `job` | Runtime MCP | Runtime MCP does not accept or create MetBench job ids. | `SystemMtControlPlaneBoundaryTests.Semantic_validation_matrix_has_environment_specific_guard` |
| `operation / job kind` | API | API/core records use `SystemMtJobKind` as internal job classification. | `SystemMtJobServiceTests.SubmitAsync_old_mr_request_persists_run_mr_job_kind` |
| `operation / job kind` | Business MCP | Business MCP does not expose a separate operation resource hierarchy. | `SystemMtControlPlaneBoundaryTests.Semantic_validation_matrix_has_environment_specific_guard` |
| `operation / job kind` | Runtime MCP | Runtime MCP does not expose business job kind. | `SystemMtControlPlaneBoundaryTests.Semantic_validation_matrix_has_environment_specific_guard` |
| `submit_run` | API | API `POST /jobs` returns an accepted job receipt. | `SystemMtApiEndpointsTests.SubmitRunAsync_returns_accepted_and_forwards_control_plane_request` |
| `submit_run` | Business MCP | Business MCP `submit_run` posts to `/api/v1/systemmt/jobs`. | `MetBenchBusinessServerTests.test_dispatch_submit_run_posts_business_job_request_to_rest_api` |
| `submit_run` | Runtime MCP | Runtime MCP does not expose `submit_run`; it exposes backend tool execution only. | `SystemMtControlPlaneBoundaryTests.Semantic_validation_matrix_has_environment_specific_guard` |
| `execution` | API | API result/evidence projection reads persisted `ExecutionId`. | `SystemMtControlPlaneServiceTests.GetEvidenceAsync_reads_evidence_through_completed_job_execution_id` |
| `execution` | Business MCP | Business MCP reads result/evidence by job id and does not create executions. | `MetBenchBusinessServerTests.test_design_doc_lists_current_business_mcp_tools` |
| `execution` | Runtime MCP | Runtime MCP does not create or expose `ExecutionId`. | `SystemMtControlPlaneBoundaryTests.Semantic_validation_matrix_has_environment_specific_guard` |
| `runtime run` | API | API evidence projection exposes runtime source/follow-up run ids. | `SystemMtApiEndpointsTests.GetEvidenceAsync_returns_runtime_evidence_from_control_plane` |
| `runtime run` | Business MCP | Business MCP does not accept runtime `run_id` as a control-plane identifier. | `MetBenchBusinessServerTests.test_business_tools_do_not_expose_runtime_plane_terms` |
| `runtime run` | Runtime MCP | Runtime MCP `run_sut_command` generates and stores runtime `run_id`. | `DockerRuntimeServerTests.test_dispatch_run_sut_command_stores_result_for_get_run_result` |
| `cancel` | API | API cancel is `POST /jobs/{jobId}/cancel`. | `SystemMtApiEndpointsTests.Api_maps_cancel_as_job_action_not_delete_resource` |
| `cancel` | Business MCP | Business MCP `cancel_job` posts to the API cancel action. | `MetBenchBusinessServerTests.test_dispatch_cancel_job_posts_cancel_action` |
| `cancel` | Runtime MCP | Runtime MCP does not expose business `cancel_job`. | `SystemMtControlPlaneBoundaryTests.Semantic_validation_matrix_has_environment_specific_guard` |
| `kill` | API | API does not expose runtime `kill_run`. | `SystemMtControlPlaneBoundaryTests.Semantic_validation_matrix_has_environment_specific_guard` |
| `kill` | Business MCP | Business MCP does not expose runtime `kill_run`. | `SystemMtControlPlaneBoundaryTests.Semantic_kill_is_runtime_only_and_not_business_mcp_tool` |
| `kill` | Runtime MCP | Runtime MCP exposes `kill_run` for runtime run ids. | `DockerRuntimeServerTests.test_dispatch_kill_run_returns_not_found_for_unknown_run` |

Verification commands:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtControlPlaneBoundaryTests"
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtApi|FullyQualifiedName~ControlPlane"
python -m unittest discover infra/mcp/metbench-business/tests
python -m unittest discover infra/mcp/docker-runtime/tests
```
