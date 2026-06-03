namespace MetBench_BLL.Core.SystemMT.ImportExport.Put;

public static class AGroupPutFixtures
{
    private const string SourceRepositoryUrl = "https://github.com/meng004/Minimum-MR-SubSet.git";
    private const string SourceCommit = "b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f";

    public static SutImportUnit Create(string sutId)
    {
        return sutId switch
        {
            "P5" => CreateP5(),
            "P4" => CreateP4(),
            "P9" => CreateP9(),
            _ => throw new ArgumentOutOfRangeException(nameof(sutId), sutId, "Only A-group SUT ids P5, P4, and P9 are supported.")
        };
    }

    private static SutImportUnit CreateP5()
    {
        var sut = new SutAsset(
            "P5",
            "Point kinetics equation",
            "stiff ODE / point kinetics",
            ProgramKind.NumericalSolver,
            new AdapterSpec(
                "minimum-mr-subset-p5",
                "Imported staging descriptor for p5_pke.py point kinetics smoke fixture.",
                "experiments/puts/p5_pke.py",
                new[] { "run_canonical" }),
            new[]
            {
                new ObservableSpec("t", ObservableKind.Series, "s", "Time samples"),
                new ObservableSpec("power", ObservableKind.Series, "relative", "Reactor power trajectory"),
                new ObservableSpec("precursor", ObservableKind.Series, "relative", "Delayed neutron precursor trajectory"),
                new ObservableSpec("power_extrema", ObservableKind.Summary, "relative", "Power extrema summary")
            },
            Metadata("priority", "A"));

        return Unit(
            sut,
            new MrAsset(
                "p5-power-response",
                "P5",
                "Point-kinetics power response imported MR",
                new[] { "power", "precursor", "power_extrema" },
                TransformBinding.ImportedOnly("minimum-mr-subset fixture has no MetBench runtime transform binding yet."),
                AssertionBinding.ImportedOnly("minimum-mr-subset fixture has no MetBench typed assertion binding yet."),
                Metadata("source", "tests/puts/test_smoke.py")),
            new IoGroup(
                "p5-canonical",
                "P5",
                "P5 canonical imported smoke IO",
                new[] { "canonical-input" },
                new[] { "canonical-output" },
                Metadata("source", "run_canonical")),
            new MutationAsset(
                "p5-mut-reactivity-sign",
                "P5",
                "P5 reactivity sign perturbation class",
                "reactivity-sign-flip",
                MutationRepresentationKind.OperatorClassOnly,
                Metadata("representation", "research-imported")),
            "p5-detection-001");
    }

    private static SutImportUnit CreateP4()
    {
        var sut = new SutAsset(
            "P4",
            "Hamiltonian pendulum",
            "Hamiltonian ODE / symplectic integration",
            ProgramKind.NumericalSolver,
            new AdapterSpec(
                "minimum-mr-subset-p4",
                "Imported staging descriptor for p4_pendulum.py Hamiltonian pendulum fixture.",
                "experiments/puts/p4_pendulum.py",
                new[] { "run_canonical" }),
            new[]
            {
                new ObservableSpec("q", ObservableKind.Series, "rad", "Pendulum generalized coordinate"),
                new ObservableSpec("p", ObservableKind.Series, "rad/s", "Pendulum generalized momentum"),
                new ObservableSpec("energy", ObservableKind.Series, "relative", "Hamiltonian energy trajectory")
            },
            Metadata("priority", "A"));

        return Unit(
            sut,
            new MrAsset(
                "p4-energy-invariant",
                "P4",
                "Hamiltonian energy-invariant imported MR",
                new[] { "q", "p", "energy" },
                TransformBinding.ImportedOnly("minimum-mr-subset fixture has no MetBench runtime transform binding yet."),
                AssertionBinding.ImportedOnly("minimum-mr-subset fixture has no MetBench typed assertion binding yet."),
                Metadata("source", "tests/puts/test_smoke.py")),
            new IoGroup(
                "p4-canonical",
                "P4",
                "P4 canonical imported smoke IO",
                new[] { "canonical-input" },
                new[] { "canonical-output" },
                Metadata("source", "run_canonical")),
            new MutationAsset(
                "p4-mut-integrator-drift",
                "P4",
                "P4 integrator drift perturbation class",
                "integrator-drift",
                MutationRepresentationKind.OperatorClassOnly,
                Metadata("representation", "research-imported")),
            "p4-detection-001");
    }

    private static SutImportUnit CreateP9()
    {
        var sut = new SutAsset(
            "P9",
            "OpenMC criticality surrogate",
            "criticality / two-group surrogate",
            ProgramKind.Surrogate,
            new AdapterSpec(
                "minimum-mr-subset-p9",
                "Imported staging descriptor for p9_openmc.py deterministic two-group surrogate; not a live OpenMC runtime binding.",
                "experiments/puts/p9_openmc.py",
                new[] { "run_canonical" }),
            new[]
            {
                new ObservableSpec("k_eff", ObservableKind.Scalar, "dimensionless", "Effective multiplication factor"),
                new ObservableSpec("sigma_k", ObservableKind.Scalar, "dimensionless", "Estimated uncertainty of k_eff"),
                new ObservableSpec("reaction_balance", ObservableKind.Summary, "dimensionless", "Reaction balance summary")
            },
            Metadata("priority", "A", "classification", "surrogate"));

        return Unit(
            sut,
            new MrAsset(
                "p9-k-eff-noise-aware",
                "P9",
                "OpenMC criticality surrogate noise-aware imported MR",
                new[] { "k_eff", "sigma_k", "reaction_balance" },
                TransformBinding.ImportedOnly("minimum-mr-subset fixture has no MetBench runtime transform binding yet."),
                AssertionBinding.ImportedOnly("P9 requires explicit sigma_k/noise-aware assertion semantics before runtime promotion."),
                Metadata("source", "tests/puts/test_smoke.py")),
            new IoGroup(
                "p9-canonical",
                "P9",
                "P9 canonical imported smoke IO",
                new[] { "canonical-input" },
                new[] { "canonical-output" },
                Metadata("source", "run_canonical")),
            new MutationAsset(
                "p9-mut-particle-count",
                "P9",
                "P9 particle-count perturbation class",
                "particle-count-scale",
                MutationRepresentationKind.OperatorClassOnly,
                Metadata("representation", "research-imported")),
            "p9-detection-001");
    }

    private static SutImportUnit Unit(
        SutAsset sut,
        MrAsset mr,
        IoGroup io,
        MutationAsset mutation,
        string detectionId)
    {
        return new SutImportUnit(
            SutImportUnit.CurrentSchemaVersion,
            sut,
            new[] { mr },
            new[] { io },
            new[] { mutation },
            new[]
            {
                new DetectionRecord(
                    detectionId,
                    mr.MrId,
                    mutation.MutationId,
                    io.IoGroupId,
                    DetectionResult.Detected,
                    EvidenceKind.ImportedResearchEvidence,
                    "Detection row imported from minimum-mr-subset research fixture metadata; not re-executed by MetBench runtime.")
            },
            new Provenance(
                SourceRepositoryUrl,
                SourceCommit,
                new[] { sut.Adapter.SourcePath, "tests/puts/test_smoke.py" },
                "codex-a-group-put-import",
                new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero)),
            new CompatibilityProfile(
                RuntimeReadiness.ImportedOnly,
                new[] { "Imported fixture is staged only until transform and assertion bindings are reviewed." }));
    }

    private static IReadOnlyDictionary<string, string> Metadata(params string[] pairs)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i + 1 < pairs.Length; i += 2)
        {
            dict[pairs[i]] = pairs[i + 1];
        }

        return dict;
    }
}
