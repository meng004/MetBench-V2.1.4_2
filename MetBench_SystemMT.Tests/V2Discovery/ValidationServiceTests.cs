using MetBench_BLL.Discovery;
using MetBench_BLL.Discovery.Validators;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

public sealed class ValidationServiceTests
{
    [Fact]
    public async Task ValidateAsync_two_passing_validators_promotes_candidate()
    {
        var (svc, fakes) = MakeService();
        var candidate = SeedCandidate(fakes);
        var validators = new IMRValidator[]
        {
            new StubValidator("empirical", true),
            new StubValidator("theoretical-llm", true),
        };

        var summary = await svc.ValidateAsync(candidate.IdCandidate, validators, actor: "alice");

        Assert.Equal(2, summary.PassedValidators);
        Assert.True(summary.Promoted);
        Assert.NotNull(summary.PromotedMRId);
        Assert.Equal("promoted", candidate.Status);
        Assert.Equal(summary.PromotedMRId, candidate.PromotedToMRId);
        Assert.Single(fakes.Mrs.Data);
        Assert.Equal("MR-T-001", fakes.Mrs.Data[0].Code);
    }

    [Fact]
    public async Task ValidateAsync_one_passing_does_not_promote_but_marks_validated()
    {
        var (svc, fakes) = MakeService();
        var candidate = SeedCandidate(fakes);
        var validators = new IMRValidator[]
        {
            new StubValidator("empirical", true),
            new StubValidator("theoretical-llm", false),
        };

        var summary = await svc.ValidateAsync(candidate.IdCandidate, validators, actor: "alice");

        Assert.Equal(1, summary.PassedValidators);
        Assert.False(summary.Promoted);
        Assert.Null(summary.PromotedMRId);
        Assert.Equal("validated", candidate.Status);
        Assert.Empty(fakes.Mrs.Data);
    }

    [Fact]
    public async Task ValidateAsync_writes_one_validation_run_per_validator()
    {
        var (svc, fakes) = MakeService();
        var candidate = SeedCandidate(fakes);
        var validators = new IMRValidator[]
        {
            new StubValidator("empirical", true),
            new StubValidator("theoretical-llm", false),
            new StubValidator("adversarial-mutmut", true),
        };

        await svc.ValidateAsync(candidate.IdCandidate, validators, actor: "alice");

        Assert.Equal(3, fakes.Validations.Data.Count);
        Assert.Contains(fakes.Validations.Data, v => v.ValidatorName == "empirical" && v.Passed);
        Assert.Contains(fakes.Validations.Data, v => v.ValidatorName == "theoretical-llm" && !v.Passed);
    }

    [Fact]
    public async Task ValidateAsync_throwing_validator_recorded_as_failed_does_not_crash()
    {
        var (svc, fakes) = MakeService();
        var candidate = SeedCandidate(fakes);
        var validators = new IMRValidator[]
        {
            new ThrowingValidator("empirical"),
            new StubValidator("theoretical-llm", true),
        };

        var summary = await svc.ValidateAsync(candidate.IdCandidate, validators, actor: "alice");

        Assert.Equal(1, summary.PassedValidators);
        Assert.False(summary.Promoted);
        Assert.Equal(2, fakes.Validations.Data.Count);
        Assert.Contains(fakes.Validations.Data, v => v.ValidatorName == "empirical" && !v.Passed);
    }

    [Fact]
    public async Task ValidateAsync_writes_audit_when_promoted()
    {
        var (svc, fakes) = MakeService();
        var candidate = SeedCandidate(fakes);
        var validators = new IMRValidator[]
        {
            new StubValidator("v1", true), new StubValidator("v2", true),
        };

        await svc.ValidateAsync(candidate.IdCandidate, validators, "alice");

        Assert.Contains(fakes.Audit.Logs, l => l.Action == "candidate.promote");
    }

    [Fact]
    public async Task ValidateAsync_missing_candidate_throws()
    {
        var (svc, _) = MakeService();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ValidateAsync(Guid.NewGuid(), new IMRValidator[]
            {
                new StubValidator("v", true)
            }, "alice"));
    }

    private static (ValidationService, FakeBundle) MakeService()
    {
        var bundle = new FakeBundle();
        var svc = new ValidationService(bundle.Candidates, bundle.Validations, bundle.Mrs, bundle.Audit);
        return (svc, bundle);
    }

    private static CandidateMR SeedCandidate(FakeBundle bundle)
    {
        var c = new CandidateMR
        {
            IdCandidate = Guid.NewGuid(),
            DiscoveryRunId = Guid.NewGuid(),
            ProposedCode = "MR-T-001",
            ProposedName = "scale fuel σ_f",
            ProposedValueName = "k_eff",
            SuggestedAssertionTypeCode = "less",
            Status = "pending",
            Confidence = 0.8,
            Rationale = "monotone",
        };
        bundle.Candidates.Add(c);
        return c;
    }
}

internal sealed class StubValidator : IMRValidator
{
    public StubValidator(string name, bool passed)
    {
        Name = name;
        _passed = passed;
    }
    public string Name { get; }
    private readonly bool _passed;
    public Task<ValidationOutcome> ValidateAsync(CandidateMR c, CancellationToken ct = default)
        => Task.FromResult(new ValidationOutcome(_passed, $"{{\"validator\":\"{Name}\"}}"));
}

internal sealed class ThrowingValidator : IMRValidator
{
    public ThrowingValidator(string name) { Name = name; }
    public string Name { get; }
    public Task<ValidationOutcome> ValidateAsync(CandidateMR c, CancellationToken ct = default)
        => throw new InvalidOperationException("boom");
}

internal sealed class FakeBundle
{
    public FakeCandidateMRRepository Candidates { get; } = new();
    public FakeValidationRunRepository Validations { get; } = new();
    public FakeMetamorphicRelationRepository Mrs { get; } = new();
    public FakeAuditLogRepository Audit { get; } = new();
}
