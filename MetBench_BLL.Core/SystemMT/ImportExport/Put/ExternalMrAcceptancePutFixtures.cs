namespace MetBench_BLL.Core.SystemMT.ImportExport.Put;

public static class ExternalMrAcceptancePutFixtures
{
    private const string MinimumMrSubsetUrl = "https://github.com/meng004/Minimum-MR-SubSet.git";
    private const string MinimumMrSubsetCommit = "8944822054e18a1d80ec8c90f56a1214a8fd1665";
    private const string DomainValidityUrl = "https://github.com/meng004/Domain-Validity-Gated-MR-for-SciML.git";
    private const string DomainValidityCommit = "c2956e8eb42b81b216a4cf31720c98e1e035f2e8";

    public static SutImportUnit CreateBatchAToyClassic()
    {
        var sut = new SutAsset(
            "minimum-mr-subset-toy-classic",
            "Minimum-MR-SubSet classic toy PUTs",
            "sorting / matrix multiplication / quadratic roots",
            ProgramKind.NumericalSolver,
            new AdapterSpec(
                "minimum-mr-subset-toy-classic",
                "Imported descriptor for classic toy PUT MR catalog; no live MetBench adapter is bound in Batch A.",
                "experiments/toy_put/classic_mt_catalog.json",
                new[] { "sorting", "matmul", "quadratic" }),
            new[]
            {
                new ObservableSpec("sorted_output", ObservableKind.Vector, "value", "Sorting output sequence"),
                new ObservableSpec("matrix_product", ObservableKind.Vector, "value", "Matrix multiplication output"),
                new ObservableSpec("quadratic_roots", ObservableKind.Vector, "value", "Quadratic root pair"),
                new ObservableSpec("residual", ObservableKind.Summary, "value", "Program-specific residual summary")
            },
            Metadata("batch", "A", "package_id", "metbench-import-minmr-toy-classic-v1"));

        var mrs = new[]
        {
            ToyMr("sort_MR1_permute", "sorting", "Sorting permutation invariance", "sorted_output"),
            ToyMr("sort_MR2_concat_dup", "sorting", "Sorting duplicate concatenation relation", "sorted_output"),
            ToyMr("sort_MR3_remove_max", "sorting", "Sorting remove-maximum relation", "sorted_output"),
            ToyMr("matmul_MR1_transpose", "matmul", "Matrix multiplication transpose relation", "matrix_product"),
            ToyMr("matmul_MR2_scalar", "matmul", "Matrix multiplication scalar relation", "matrix_product"),
            ToyMr("quad_MR1_swap_roots", "quadratic", "Quadratic root swap relation", "quadratic_roots", "residual"),
            ToyMr("quad_MR2_scale", "quadratic", "Quadratic coefficient scaling relation", "quadratic_roots", "residual")
        };
        var ioGroups = new[]
        {
            new IoGroup("toy-sorting-catalog", sut.SutId, "Sorting toy catalog cases", new[] { "sorting-source" }, new[] { "sorting-followup" }, Metadata("program", "sorting")),
            new IoGroup("toy-matmul-catalog", sut.SutId, "Matrix multiplication toy catalog cases", new[] { "matmul-source" }, new[] { "matmul-followup" }, Metadata("program", "matmul")),
            new IoGroup("toy-quadratic-catalog", sut.SutId, "Quadratic toy catalog cases", new[] { "quadratic-source" }, new[] { "quadratic-followup" }, Metadata("program", "quadratic"))
        };
        var mutations = new[]
        {
            new MutationAsset("toy-mut-sorting", sut.SutId, "Sorting toy operator faults", "toy-sorting", MutationRepresentationKind.OperatorClassOnly, Metadata("program", "sorting")),
            new MutationAsset("toy-mut-matmul", sut.SutId, "Matrix multiplication toy operator faults", "toy-matmul", MutationRepresentationKind.OperatorClassOnly, Metadata("program", "matmul")),
            new MutationAsset("toy-mut-quadratic", sut.SutId, "Quadratic toy operator faults", "toy-quadratic", MutationRepresentationKind.OperatorClassOnly, Metadata("program", "quadratic"))
        };
        var detections = mrs.Select((mr, index) =>
        {
            var program = mr.Metadata["program"];
            var mutation = mutations.Single(m => m.Metadata["program"] == program);
            var io = ioGroups.Single(g => g.Metadata["program"] == program);
            return new DetectionRecord(
                $"toy-detection-{index + 1:D2}",
                mr.MrId,
                mutation.MutationId,
                io.IoGroupId,
                DetectionResult.Inconclusive,
                EvidenceKind.ImportedResearchEvidence,
                "Classic toy catalog imported for acceptance-package coverage; detection execution is not replayed in Batch A.");
        }).ToArray();

        return Unit(
            sut,
            mrs,
            ioGroups,
            mutations,
            detections,
            new[] { "experiments/toy_put/classic_mt_catalog.json" },
            "codex-batch-a-toy-classic",
            new CompatibilityProfile(RuntimeReadiness.ImportedOnly, new[] { "Classic toy MRs are imported catalog evidence until runtime adapters are bound." }));
    }

    public static SutImportUnit CreateBatchAP1Heat()
    {
        var sut = new SutAsset(
            "minimum-mr-subset-p1-heat",
            "Minimum-MR-SubSet P1 heat equation",
            "parabolic PDE / finite difference heat solver",
            ProgramKind.NumericalSolver,
            new AdapterSpec(
                "minimum-mr-subset-p1-heat",
                "Imported descriptor for p1_heat.py and its MR/detection matrix; runtime execution awaits launcher adapter binding.",
                "experiments/puts/p1_heat.py",
                new[] { "solve_heat" }),
            new[]
            {
                new ObservableSpec("x", ObservableKind.Vector, "position", "Spatial grid"),
                new ObservableSpec("u", ObservableKind.Vector, "temperature", "Final temperature field"),
                new ObservableSpec("l2_norm", ObservableKind.Scalar, "temperature", "L2 norm of the final field"),
                new ObservableSpec("extrema", ObservableKind.Summary, "temperature", "Minimum and maximum field values"),
                new ObservableSpec("mass", ObservableKind.Scalar, "temperature", "Discrete mass summary")
            },
            Metadata("batch", "A", "package_id", "metbench-import-minmr-p1-heat-v1", "external_family", "P1"));

        var mrs = new[]
        {
            HeatMr("p1-heat-MR01", "MR1", "Alpha scale invariance", "ScaleAlphaTransform", "ScaledEqualityPredicate", "u", "mut_C"),
            HeatMr("p1-heat-MR02", "MR2", "Time-step doubling relation", "DoubleTimeStepTransform", "AllClosePredicate", "u", "mut_T"),
            HeatMr("p1-heat-MR03", "MR3", "Grid refinement convergence", "GridRefinementTransform", "ConvergencePredicate", "l2_norm", "mut_G"),
            HeatMr("p1-heat-MR04", "MR4", "Additional steps eigenvalue scaling", "AdditionalStepsTransform", "ScaledEqualityPredicate", "u", "mut_T"),
            HeatMr("p1-heat-MR05", "MR5", "Refinement exact-solution convergence", "GridRefinementTransform", "ConvergencePredicate", "l2_norm", "mut_G"),
            HeatMr("p1-heat-MR06", "MR6", "Alternative alpha invariance", "ScaleAlphaPrimeTransform", "ScaledEqualityPredicate", "u", "mut_F"),
            HeatMr("p1-heat-MR07", "MR7", "Additional steps q-power allclose", "AdditionalStepsTransform", "AllClosePredicate", "u", "mut_T"),
            HeatMr("p1-heat-MR08", "MR8", "Refinement norm relation", "GridRefinementTransform", "ConvergencePredicate", "l2_norm", "mut_G"),
            HeatMr("p1-heat-MR09", "MR9", "Discrete mass balance", "MassBalanceTransform", "MassBalancePredicate", "mass", "mut_C"),
            HeatMr("p1-heat-MR10", "MR10", "Monotone diffusion", "MonotoneDiffusionTransform", "MonotonePredicate", "extrema", "mut_M")
        };
        var ioGroups = new[]
        {
            new IoGroup(
                "p1-heat-detection-matrix",
                sut.SutId,
                "P1 heat source/follow-up cases from detection_matrix.csv",
                new[] { "data/raw/p1_heat/source" },
                new[] { "data/raw/p1_heat/followup" },
                Metadata("source", "data/raw/p1_heat/detection_matrix.csv"))
        };
        var mutations = new[]
        {
            HeatMutation(sut.SutId, "mut_C", "coefficient perturbation"),
            HeatMutation(sut.SutId, "mut_F", "forcing perturbation"),
            HeatMutation(sut.SutId, "mut_G", "grid perturbation"),
            HeatMutation(sut.SutId, "mut_M", "mass/update perturbation"),
            HeatMutation(sut.SutId, "mut_T", "time-step perturbation")
        };
        var detections = BuildHeatDetectionMatrix(mrs, mutations, ioGroups[0]);

        return Unit(
            sut,
            mrs,
            ioGroups,
            mutations,
            detections,
            new[] { "experiments/puts/p1_heat.py", "data/raw/p1_heat/mrs.json", "data/raw/p1_heat/detection_matrix.csv" },
            "codex-batch-a-p1-heat",
            new CompatibilityProfile(RuntimeReadiness.ImportedOnly, new[] { "P1 heat evidence matrix is imported; runtime adapter binding is still pending." }));
    }

    public static SutImportUnit CreateBatchDScimlDomainValidity()
    {
        var sut = new SutAsset(
            "sciml-domain-validity-mgn",
            "Domain-validity gated MGN SciML fixture",
            "mesh graph neural surrogate / domain validity",
            ProgramKind.Surrogate,
            new AdapterSpec(
                "domain-validity-mgn",
                "Imported SciML domain-validity evidence package. Runtime replay requires the original MGN environment and is not bound in Batch D.",
                "domain_validity_gated_mr/evidence/mr_cards",
                new[] { "node_permutation_fixture", "seeded_fault_metric_ledger" }),
            new[]
            {
                new ObservableSpec("node_prediction", ObservableKind.Vector, "state", "Per-node predicted physical state"),
                new ObservableSpec("edge_index", ObservableKind.Vector, "graph", "Graph connectivity"),
                new ObservableSpec("mirror_y_prediction", ObservableKind.Vector, "state", "Mirrored-y prediction field"),
                new ObservableSpec("discrete_divergence", ObservableKind.Scalar, "residual", "Discrete divergence diagnostic"),
                new ObservableSpec("seeded_fault_metric", ObservableKind.Summary, "rate", "Seeded-fault detection metric")
            },
            Metadata("batch", "D", "package_id", "metbench-import-sciml-domain-validity-fixture-v1"));

        var mrs = new[]
        {
            new MrAsset(
                "mgn-node-permutation-equivariance",
                sut.SutId,
                "MGN node permutation equivariance",
                new[] { "node_prediction", "edge_index", "seeded_fault_metric" },
                new TransformBinding(CompatibilityStatus.RequiresAdapter, "NodePermutationTransform", Shape: ShapeSpec.ScalarSeries(), Notes: "Requires MGN graph fixture and tensor adapter."),
                new AssertionBinding(CompatibilityStatus.MappedSupported, "RelativeL2LessOrEqualPredicate", "permutation_relative_l2_error", ToleranceSpec.Relative(1e-6)),
                Metadata("status", "design-time-retained", "evidence", "mr_card")),
            new MrAsset(
                "mgn-mirror-y-equivariance",
                sut.SutId,
                "MGN mirror-y equivariance",
                new[] { "mirror_y_prediction", "seeded_fault_metric" },
                new TransformBinding(CompatibilityStatus.RequiresAdapter, "MirrorYTransform", Shape: ShapeSpec.ScalarSeries(), Notes: "Requires geometry-aware MGN fixture."),
                new AssertionBinding(CompatibilityStatus.MappedSupported, "RelativeL2LessOrEqualPredicate", "mirror_y_relative_l2_error", ToleranceSpec.Relative(1e-6)),
                Metadata("status", "design-time-retained-ood-stress", "evidence", "mr_card")),
            new MrAsset(
                "mgn-discrete-divergence-boundedness",
                sut.SutId,
                "MGN discrete divergence boundedness",
                new[] { "discrete_divergence", "seeded_fault_metric" },
                TransformBinding.ImportedOnly("Deferred by upstream: threshold is blocked until calibration."),
                AssertionBinding.ImportedOnly("Allowed verdicts are skip, out-of-relation-domain, or inconclusive; this is diagnostic evidence only."),
                Metadata("status", "design-time-deferred", "allowed_verdicts", "skip;out-of-relation-domain;inconclusive", "threshold", "blocked_until_calibrated"))
        };
        var ioGroups = new[]
        {
            new IoGroup(
                "sciml-mgn-node-permutation-fixture",
                sut.SutId,
                "Node permutation and seeded-fault evidence fixture",
                new[] { "fixtures/node_permutation/source_graph" },
                new[] { "fixtures/node_permutation/followup_graph", "reports/seeded_fault_metric_ledger" },
                Metadata("source", "domain-validity evidence fixture"))
        };
        var mutations = new[]
        {
            ScimlMutation(sut.SutId, "BC_zero_inflow", "boundary_condition_fault"),
            ScimlMutation(sut.SutId, "BC_nonzero_wall", "boundary_condition_fault"),
            ScimlMutation(sut.SutId, "MA_drop_edges", "mesh_adjacency_fault"),
            ScimlMutation(sut.SutId, "MA_permute_edges", "mesh_adjacency_fault"),
            ScimlMutation(sut.SutId, "NS_skip_denorm", "normalization_scale_fault"),
            ScimlMutation(sut.SutId, "NS_double_scale", "normalization_scale_fault"),
            ScimlMutation(sut.SutId, "TR_sign_flip", "time_reversal_fault"),
            ScimlMutation(sut.SutId, "TR_double_step", "time_reversal_fault"),
            ScimlMutation(sut.SutId, "PC_swap_xy", "physical_channel_fault"),
            ScimlMutation(sut.SutId, "PC_zero_vy", "physical_channel_fault")
        };
        var detections = BuildScimlDetectionMatrix(mrs, mutations, ioGroups[0]);

        return new SutImportUnit(
            SutImportUnit.CurrentSchemaVersion,
            sut,
            mrs,
            ioGroups,
            mutations,
            detections,
            new Provenance(
                DomainValidityUrl,
                DomainValidityCommit,
                new[] { "README.md", "reports/seeded_fault_metric_ledger.json", "mr_cards" },
                "codex-batch-d-sciml-domain-validity",
                new DateTimeOffset(2026, 6, 11, 0, 0, 0, TimeSpan.Zero)),
            new CompatibilityProfile(
                RuntimeReadiness.ImportedOnly,
                new[] { "Batch D preserves domain-validity evidence; graph/tensor adapters and calibrated divergence threshold are required before runtime promotion." }));
    }

    public static SutImportUnit CreateBatchEScimlMgnRuntime()
    {
        var sut = new SutAsset(
            "sciml-mgn-runtime",
            "Domain-validity MGN real-SUT runtime assets",
            "mesh graph neural surrogate / cylinder flow",
            ProgramKind.Surrogate,
            new AdapterSpec(
                "domain-validity-mgn-runtime",
                "Imported real-SUT runtime descriptor for Docker/SSH Batch E. MetBench can preflight Docker, but tensor/checkpoint adapters are not bound yet.",
                "research_assets/runs",
                new[] { "run_real_sut_node_permutation", "run_mirror_y", "run_conservation_diagnostic", "run_seeded_fault_detection" }),
            new[]
            {
                new ObservableSpec("node_permutation_relative_l2", ObservableKind.Scalar, "relative_error", "Node permutation relative L2 replay metric"),
                new ObservableSpec("mirror_y_relative_l2", ObservableKind.Scalar, "relative_error", "Mirror-y real/synthetic replay metric"),
                new ObservableSpec("discrete_divergence", ObservableKind.Scalar, "residual", "Discrete divergence diagnostic replay metric"),
                new ObservableSpec("checkpoint_id", ObservableKind.Summary, "id", "MGN checkpoint identifier"),
                new ObservableSpec("seeded_fault_detection", ObservableKind.Summary, "rate", "Seeded-fault runtime detection summary")
            },
            Metadata(
                "batch", "E",
                "package_id", "metbench-import-sciml-mgn-runtime-v1",
                "runtime", "docker",
                "runtime_key", "docker-sciml",
                "ssh_fallback", "blocked-until-executor"));

        var mrs = new[]
        {
            new MrAsset(
                "mgn-runtime-node-permutation-equivariance",
                sut.SutId,
                "MGN runtime node permutation equivariance",
                new[] { "node_permutation_relative_l2", "checkpoint_id", "seeded_fault_detection" },
                new TransformBinding(CompatibilityStatus.RequiresAdapter, "NodePermutationTensorTransform", ShapeSpec.ScalarSeries(), "Requires graph/tensor adapter and checkpoint-mounted Docker runtime."),
                new AssertionBinding(CompatibilityStatus.MappedSupported, "RelativeL2LessOrEqualPredicate", "node_permutation_relative_l2", ToleranceSpec.Relative(1e-6), "Predicate is known, but metric extraction is not bound to MetBench yet."),
                Metadata("source_run", "real-sut-node-permutation-pilot", "runtime", "docker-or-ssh")),
            new MrAsset(
                "mgn-runtime-mirror-y-equivariance",
                sut.SutId,
                "MGN runtime mirror-y equivariance",
                new[] { "mirror_y_relative_l2", "checkpoint_id", "seeded_fault_detection" },
                new TransformBinding(CompatibilityStatus.RequiresAdapter, "MirrorYTensorTransform", ShapeSpec.ScalarSeries(), "Requires geometry-aware tensor adapter and checkpoint-mounted Docker runtime."),
                new AssertionBinding(CompatibilityStatus.MappedSupported, "RelativeL2LessOrEqualPredicate", "mirror_y_relative_l2", ToleranceSpec.Relative(1e-6), "Predicate is known, but metric extraction is not bound to MetBench yet."),
                Metadata("source_run", "mirror-y-rate-upgrade;mirror-y-symmetric-mesh", "runtime", "docker-or-ssh")),
            new MrAsset(
                "mgn-runtime-discrete-divergence-diagnostic",
                sut.SutId,
                "MGN runtime discrete divergence diagnostic",
                new[] { "discrete_divergence", "checkpoint_id" },
                TransformBinding.ImportedOnly("Upstream threshold remains diagnostic until calibrated."),
                AssertionBinding.ImportedOnly("Allowed verdicts are skip, out-of-relation-domain, or inconclusive; no pass/fail runtime assertion is claimed."),
                Metadata("source_run", "conservation-diagnostic-pilot", "status", "diagnostic-only")),
        };
        var ioGroups = new[]
        {
            new IoGroup(
                "sciml-mgn-runtime-checkpoint-dataset",
                sut.SutId,
                "Cylinder-flow checkpoint and dataset roots",
                new[] { "datasets/cylinder_flow/source_graphs", "checkpoints/mgn_cylinder_flow" },
                new[] { "runs/real_sut_metrics", "runs/seeded_fault_detection/raw/metric_ledger.json" },
                Metadata("runtime", "docker-or-ssh", "artifact_policy", "external-paths-not-vendored"))
        };
        var mutations = new[]
        {
            ScimlMutation(sut.SutId, "BC_zero_inflow", "boundary_condition_fault"),
            ScimlMutation(sut.SutId, "BC_nonzero_wall", "boundary_condition_fault"),
            ScimlMutation(sut.SutId, "MA_drop_edges", "mesh_adjacency_fault"),
            ScimlMutation(sut.SutId, "MA_permute_edges", "mesh_adjacency_fault"),
            ScimlMutation(sut.SutId, "NS_skip_denorm", "normalization_scale_fault"),
            ScimlMutation(sut.SutId, "NS_double_scale", "normalization_scale_fault"),
            ScimlMutation(sut.SutId, "TR_sign_flip", "time_reversal_fault"),
            ScimlMutation(sut.SutId, "TR_double_step", "time_reversal_fault"),
            ScimlMutation(sut.SutId, "PC_swap_xy", "physical_channel_fault"),
            ScimlMutation(sut.SutId, "PC_zero_vy", "physical_channel_fault")
        };
        var detections = BuildScimlDetectionMatrix(mrs, mutations, ioGroups[0]);

        return new SutImportUnit(
            SutImportUnit.CurrentSchemaVersion,
            sut,
            mrs,
            ioGroups,
            mutations,
            detections,
            new Provenance(
                DomainValidityUrl,
                DomainValidityCommit,
                new[]
                {
                    "research_assets/runs/real-sut-node-permutation-pilot/manifest.yml",
                    "research_assets/runs/mirror-y-rate-upgrade/manifest.yml",
                    "research_assets/runs/mirror-y-symmetric-mesh/manifest.yml",
                    "research_assets/runs/conservation-diagnostic-pilot/manifest.yml",
                    "research_assets/runs/seeded-fault-detection/manifest.yml"
                },
                "codex-batch-e-sciml-mgn-runtime",
                new DateTimeOffset(2026, 6, 12, 0, 0, 0, TimeSpan.Zero)),
            new CompatibilityProfile(
                RuntimeReadiness.ImportedOnly,
                new[] { "Batch E records Docker/SSH runtime assets; Docker preflight is available, but MGN tensor/checkpoint adapters and artifact mounts are required before MetBench replay." }));
    }

    private static MrAsset ToyMr(string mrId, string program, string name, params string[] observables)
    {
        return new MrAsset(
            mrId,
            "minimum-mr-subset-toy-classic",
            name,
            observables,
            TransformBinding.ImportedOnly("Classic toy catalog transform is imported but not bound to a MetBench runtime adapter."),
            AssertionBinding.ImportedOnly("Classic toy catalog assertion is imported but not bound to a typed predicate."),
            Metadata("program", program, "source", "classic_mt_catalog.json"));
    }

    private static MrAsset HeatMr(
        string mrId,
        string externalMrId,
        string name,
        string transformKind,
        string predicateKind,
        string metric,
        string detectionOperatorClass)
    {
        return new MrAsset(
            mrId,
            "minimum-mr-subset-p1-heat",
            name,
            new[] { "x", "u", "l2_norm", "extrema", "mass" },
            new TransformBinding(CompatibilityStatus.RequiresAdapter, transformKind, ShapeSpec.ScalarSeries(), $"Mapped from external {externalMrId}; MetBench launcher adapter is not bound yet."),
            new AssertionBinding(CompatibilityStatus.MappedSupported, predicateKind, metric, ToleranceSpec.Relative(1e-6), $"Mapped from external {externalMrId}."),
            Metadata("external_mr_id", externalMrId, "detected_operator_class", detectionOperatorClass, "source", "data/raw/p1_heat/mrs.json"));
    }

    private static MutationAsset HeatMutation(string sutId, string operatorClass, string name)
    {
        return new MutationAsset(
            $"p1-heat-{operatorClass}",
            sutId,
            $"P1 heat {name}",
            operatorClass,
            MutationRepresentationKind.OperatorClassOnly,
            Metadata("source", "data/raw/p1_heat/detection_matrix.csv"));
    }

    private static MutationAsset ScimlMutation(string sutId, string mutationId, string operatorClass)
    {
        return new MutationAsset(
            mutationId,
            sutId,
            mutationId.Replace('_', ' '),
            operatorClass,
            MutationRepresentationKind.OperatorClassOnly,
            Metadata("source", "seeded_fault_metric_ledger", "representation", "seeded-fault"));
    }

    private static IReadOnlyList<DetectionRecord> BuildHeatDetectionMatrix(
        IReadOnlyList<MrAsset> mrs,
        IReadOnlyList<MutationAsset> mutations,
        IoGroup ioGroup)
    {
        var detections = new List<DetectionRecord>();
        foreach (var mr in mrs)
        {
            var detectedOperatorClass = mr.Metadata["detected_operator_class"];
            foreach (var mutation in mutations)
            {
                var result = string.Equals(mutation.OperatorClass, detectedOperatorClass, StringComparison.Ordinal)
                    ? DetectionResult.Detected
                    : DetectionResult.Survived;
                detections.Add(new DetectionRecord(
                    $"p1-{mr.Metadata["external_mr_id"]}-{mutation.OperatorClass}",
                    mr.MrId,
                    mutation.MutationId,
                    ioGroup.IoGroupId,
                    result,
                    EvidenceKind.ImportedResearchEvidence,
                    $"Imported from P1 heat detection_matrix.csv: {mr.Metadata["external_mr_id"]} vs {mutation.OperatorClass} => {result}."));
            }
        }

        return detections;
    }

    private static IReadOnlyList<DetectionRecord> BuildScimlDetectionMatrix(
        IReadOnlyList<MrAsset> mrs,
        IReadOnlyList<MutationAsset> mutations,
        IoGroup ioGroup)
    {
        var detectedByMr = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["mgn-node-permutation-equivariance"] = new HashSet<string>(StringComparer.Ordinal),
            ["mgn-mirror-y-equivariance"] = new HashSet<string>(StringComparer.Ordinal) { "MA_drop_edges", "PC_swap_xy" },
            ["mgn-discrete-divergence-boundedness"] = new HashSet<string>(StringComparer.Ordinal) { "BC_zero_inflow", "BC_nonzero_wall", "NS_skip_denorm" },
            ["mgn-runtime-node-permutation-equivariance"] = new HashSet<string>(StringComparer.Ordinal),
            ["mgn-runtime-mirror-y-equivariance"] = new HashSet<string>(StringComparer.Ordinal) { "MA_drop_edges", "PC_swap_xy" },
            ["mgn-runtime-discrete-divergence-diagnostic"] = new HashSet<string>(StringComparer.Ordinal) { "BC_zero_inflow", "BC_nonzero_wall", "NS_skip_denorm" }
        };
        var detections = new List<DetectionRecord>();
        foreach (var mr in mrs)
        {
            foreach (var mutation in mutations)
            {
                var result = detectedByMr[mr.MrId].Contains(mutation.MutationId)
                    ? DetectionResult.Detected
                    : DetectionResult.Survived;
                detections.Add(new DetectionRecord(
                    $"sciml-{mr.MrId}-{mutation.MutationId}",
                    mr.MrId,
                    mutation.MutationId,
                    ioGroup.IoGroupId,
                    result,
                    EvidenceKind.ImportedResearchEvidence,
                    $"Imported seeded-fault metric ledger row: {mr.MrId} vs {mutation.MutationId} => {result}."));
            }
        }

        return detections;
    }

    private static SutImportUnit Unit(
        SutAsset sut,
        IReadOnlyList<MrAsset> mrs,
        IReadOnlyList<IoGroup> ioGroups,
        IReadOnlyList<MutationAsset> mutations,
        IReadOnlyList<DetectionRecord> detections,
        IReadOnlyList<string> sourcePaths,
        string capturedBy,
        CompatibilityProfile compatibility)
    {
        return new SutImportUnit(
            SutImportUnit.CurrentSchemaVersion,
            sut,
            mrs,
            ioGroups,
            mutations,
            detections,
            new Provenance(
                MinimumMrSubsetUrl,
                MinimumMrSubsetCommit,
                sourcePaths,
                capturedBy,
                new DateTimeOffset(2026, 6, 11, 0, 0, 0, TimeSpan.Zero)),
            compatibility);
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
