namespace MetBench_BLL.Core.SystemMT.ImportExport.Put;

public static class BGroupPutFixtures
{
    private const string SourceRepositoryUrl = "https://github.com/meng004/Minimum-MR-SubSet.git";
    private const string SourceCommit = "b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f";

    public static SutImportUnit Create(string sutId)
    {
        return sutId switch
        {
            "P3" => CreateP3(),
            "P8" => CreateP8(),
            _ => throw new ArgumentOutOfRangeException(nameof(sutId), sutId, "Only B-group SUT ids P3 and P8 are supported.")
        };
    }

    private static SutImportUnit CreateP3()
    {
        var sut = new SutAsset(
            "P3",
            "Lorenz trajectory solver",
            "chaotic ODE / RK",
            ProgramKind.NumericalSolver,
            new AdapterSpec(
                "minimum-mr-subset-p3",
                "Imported staging descriptor for p3_lorenz.py Lorenz trajectory fixture; external source imports NumPy and SciPy.",
                "experiments/puts/p3_lorenz.py",
                new[] { "run_canonical" }),
            new[]
            {
                new ObservableSpec("t", ObservableKind.Series, "s", "Time samples"),
                new ObservableSpec("trajectory", ObservableKind.Vector, "state", "Lorenz state trajectory"),
                new ObservableSpec("centroid", ObservableKind.Vector, "state", "Mean trajectory coordinate")
            },
            Metadata("priority", "B", "external_dependencies", "numpy;scipy"));

        return Unit(
            sut,
            new MrAsset(
                "p3-trajectory-sensitivity",
                "P3",
                "Lorenz trajectory-sensitivity imported MR",
                new[] { "t", "trajectory", "centroid" },
                TransformBinding.ImportedOnly("P3 has no MetBench runtime transform binding in the staging package."),
                AssertionBinding.ImportedOnly("P3 has no MetBench typed assertion binding in the staging package."),
                Metadata("source", "tests/puts/test_smoke.py")),
            new IoGroup(
                "p3-canonical",
                "P3",
                "P3 canonical imported smoke IO",
                new[] { "canonical-input" },
                new[] { "canonical-output" },
                Metadata("source", "run_canonical")),
            new MutationAsset(
                "p3-mut-initial-condition",
                "P3",
                "P3 initial-condition perturbation class",
                "initial-condition-perturbation",
                MutationRepresentationKind.OperatorClassOnly,
                Metadata("representation", "research-imported")),
            "p3-detection-001",
            "No P3 detection matrix was observed in the external source tree; detection remains inconclusive.");
    }

    private static SutImportUnit CreateP8()
    {
        var sut = new SutAsset(
            "P8",
            "Schrodinger spectral solver",
            "complex PDE / spectral",
            ProgramKind.NumericalSolver,
            new AdapterSpec(
                "minimum-mr-subset-p8",
                "Imported staging descriptor for p8_schrodinger.py spectral Schrodinger fixture; external source imports NumPy FFT.",
                "experiments/puts/p8_schrodinger.py",
                new[] { "run_canonical" }),
            new[]
            {
                new ObservableSpec("x", ObservableKind.Vector, "position", "Spatial grid"),
                new ObservableSpec("probability_density", ObservableKind.Vector, "probability", "Final probability density"),
                new ObservableSpec("norm", ObservableKind.Series, "probability", "Norm trajectory")
            },
            Metadata("priority", "B", "external_dependencies", "numpy"));

        return Unit(
            sut,
            new MrAsset(
                "p8-norm-conservation",
                "P8",
                "Schrodinger norm-conservation imported MR",
                new[] { "x", "probability_density", "norm" },
                TransformBinding.ImportedOnly("P8 has no MetBench runtime transform binding in the staging package."),
                AssertionBinding.ImportedOnly("P8 has no MetBench typed assertion binding in the staging package."),
                Metadata("source", "tests/puts/test_smoke.py")),
            new IoGroup(
                "p8-canonical",
                "P8",
                "P8 canonical imported smoke IO",
                new[] { "canonical-input" },
                new[] { "canonical-output" },
                Metadata("source", "run_canonical")),
            new MutationAsset(
                "p8-mut-time-step",
                "P8",
                "P8 time-step perturbation class",
                "time-step-perturbation",
                MutationRepresentationKind.OperatorClassOnly,
                Metadata("representation", "research-imported")),
            "p8-detection-001",
            "No P8 detection matrix was observed in the external source tree; detection remains inconclusive.");
    }

    private static SutImportUnit Unit(
        SutAsset sut,
        MrAsset mr,
        IoGroup io,
        MutationAsset mutation,
        string detectionId,
        string detectionNote)
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
                    DetectionResult.Inconclusive,
                    EvidenceKind.ImportedResearchEvidence,
                    detectionNote)
            },
            new Provenance(
                SourceRepositoryUrl,
                SourceCommit,
                new[] { sut.Adapter.SourcePath, "tests/puts/test_smoke.py" },
                "codex-b-group-put-import",
                new DateTimeOffset(2026, 6, 4, 0, 0, 0, TimeSpan.Zero)),
            new CompatibilityProfile(
                RuntimeReadiness.ImportedOnly,
                new[] { "B-group imported fixture is staged only until transform and assertion bindings are reviewed." }));
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
