using MetBench_BLL.SystemMT.ControlPlane;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MetBench_Api;

public static class SystemMtApiEndpoints
{
    public static IEndpointRouteBuilder MapSystemMtApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/systemmt");
        group.MapPost("/jobs", SubmitRunAsync);
        group.MapGet("/jobs/{jobId:guid}", GetJobAsync);
        group.MapGet("/jobs/{jobId:guid}/result", GetResultAsync);
        group.MapGet("/jobs/{jobId:guid}/evidence", GetEvidenceAsync);
        group.MapDelete("/jobs/{jobId:guid}", CancelJobAsync);
        return endpoints;
    }

    public static async Task<Results<Accepted<SystemMtJobReceiptResponse>, BadRequest<SystemMtApiError>>>
        SubmitRunAsync(
            ISystemMtControlPlaneService controlPlane,
            SystemMtSubmitRunRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            var receipt = await controlPlane.SubmitRunAsync(
                new SystemMtControlPlaneRunRequest(request.MrId, request.ParameterOverrides),
                cancellationToken).ConfigureAwait(false);
            return TypedResults.Accepted(
                $"/api/v1/systemmt/jobs/{receipt.JobId}",
                new SystemMtJobReceiptResponse(receipt.JobId, receipt.AcceptedAtUtc));
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new SystemMtApiError("bad_request", ex.Message));
        }
    }

    public static async Task<Results<Ok<SystemMtControlPlaneJobSnapshot>, NotFound>> GetJobAsync(
        ISystemMtControlPlaneService controlPlane,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await controlPlane.GetJobAsync(jobId, cancellationToken).ConfigureAwait(false);
        return job is null ? TypedResults.NotFound() : TypedResults.Ok(job);
    }

    public static async Task<Results<Ok<SystemMtControlPlaneRunResult>, NotFound>> GetResultAsync(
        ISystemMtControlPlaneService controlPlane,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var result = await controlPlane.GetResultAsync(jobId, cancellationToken).ConfigureAwait(false);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    public static async Task<Results<Ok<SystemMtControlPlaneEvidenceSnapshot>, NotFound>> GetEvidenceAsync(
        ISystemMtControlPlaneService controlPlane,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var evidence = await controlPlane.GetEvidenceAsync(jobId, cancellationToken).ConfigureAwait(false);
        return evidence is null ? TypedResults.NotFound() : TypedResults.Ok(evidence);
    }

    public static async Task<NoContent> CancelJobAsync(
        ISystemMtControlPlaneService controlPlane,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        await controlPlane.CancelAsync(jobId, cancellationToken).ConfigureAwait(false);
        return TypedResults.NoContent();
    }
}
