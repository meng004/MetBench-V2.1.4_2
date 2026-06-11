using MetBench_BLL.SystemMT.Runtime;

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

    public static SutImportUnit CreateBatchBExistingRuntimeReconcile()
    {
        var sut = new SutAsset(
            "minimum-mr-subset-existing-runtime-reconcile",
            "Minimum-MR-SubSet existing runtime reconciliation",
            "existing MetBench runtime slices for P3/P4/P5/P8/P9",
            ProgramKind.NumericalSolver,
            new AdapterSpec(
                "minimum-mr-subset-existing-runtime-reconcile",
                "Reconciliation descriptor only: attaches Minimum-MR-SubSet provenance to existing MetBench runtime SUT IDs without creating duplicate launchable SUTs.",
                "experiments/puts/p3_lorenz.py;experiments/puts/p4_pendulum.py;experiments/puts/p5_pke.py;experiments/puts/p8_schrodinger.py;experiments/puts/p9_openmc.py",
                new[] { "p3_lorenz", "p4_pendulum", "p5_pke", "p8_schrodinger", "p9_openmc_surrogate" }),
            new[]
            {
                new ObservableSpec("separation", ObservableKind.Scalar, "state", "P3 Lorenz trajectory separation"),
                new ObservableSpec("energy_drift", ObservableKind.Scalar, "energy", "P4 pendulum energy drift"),
                new ObservableSpec("max_power", ObservableKind.Scalar, "power", "P5 point kinetics power response"),
                new ObservableSpec("norm_drift", ObservableKind.Scalar, "probability", "P8 Schrodinger norm drift"),
                new ObservableSpec("sigma_k", ObservableKind.Scalar, "uncertainty", "P9 OpenMC surrogate k-effective standard error")
            },
            Metadata("batch", "B", "package_id", "metbench-import-minmr-existing-runtime-reconcile-v1", "mode", "reconcile-existing-runtime-suts"));

        var mrs = new[]
        {
            ReconcileMr("p3-trajectory-sensitivity", "minimum-mr-subset-p3", "P3", "Lorenz trajectory sensitivity", "separation", "ScaleField", "GreaterThan", "greater", "pure-stdlib runtime; dependency gate remains explicit in existing catalog"),
            ReconcileMr("p4-energy-invariant", "minimum-mr-subset-p4", "P4", "Pendulum energy invariant", "energy_drift", "ScaleField", "LessThan", "less", "pure-stdlib runtime"),
            ReconcileMr("p5-power-response", "minimum-mr-subset-p5", "P5", "Point kinetics power response", "max_power", "ScaleField", "GreaterThan", "greater", "pure-stdlib runtime"),
            ReconcileMr("p8-norm-conservation", "minimum-mr-subset-p8", "P8", "Schrodinger norm conservation", "norm_drift", "ScaleField", "LessThan", "less", "dependency gate explicit; external np.trapz compatibility risk is recorded"),
            ReconcileMr("p9-k-eff-noise-aware", "minimum-mr-subset-p9-surrogate", "P9", "OpenMC surrogate k-effective noise-aware relation", "sigma_k", "ScaleField", "VarianceRatio", "variance-ratio", "deterministic OpenMC surrogate; not a real OpenMC execution")
        };
        var ioGroups = mrs.Select(m => new IoGroup(
            $"{m.Metadata["external_family"].ToLowerInvariant()}-existing-runtime-sample",
            sut.SutId,
            $"{m.Metadata["external_family"]} existing runtime sample",
            new[] { $"SUT/{ExistingSutDirectory(m.Metadata["existing_sut_id"])}/sample/standard.json" },
            new[] { $"SUT/{ExistingSutDirectory(m.Metadata["existing_sut_id"])}/runtime-output" },
            Metadata(
                "existing_sut_id", m.Metadata["existing_sut_id"],
                "existing_sut_directory", ExistingSutDirectory(m.Metadata["existing_sut_id"]),
                "sample_case_relative_path", $"SUT/{ExistingSutDirectory(m.Metadata["existing_sut_id"])}/sample/standard.json")))
            .ToArray();
        var mutations = mrs.Select(m => new MutationAsset(
            $"{m.Metadata["external_family"].ToLowerInvariant()}-operator-class",
            sut.SutId,
            $"{m.Metadata["external_family"]} imported operator-class compatibility record",
            $"{m.Metadata["external_family"]}_operator_class",
            MutationRepresentationKind.OperatorClassOnly,
            Metadata("existing_sut_id", m.Metadata["existing_sut_id"], "source", m.Metadata["source"])))
            .ToArray();
        var detections = mrs.Select((m, index) => new DetectionRecord(
            $"batch-b-reconcile-{m.Metadata["external_family"].ToLowerInvariant()}",
            m.MrId,
            mutations[index].MutationId,
            ioGroups[index].IoGroupId,
            DetectionResult.Inconclusive,
            EvidenceKind.ImportedResearchEvidence,
            $"Reconciles external {m.Metadata["external_family"]} provenance with stable MetBench SUT id {m.Metadata["existing_sut_id"]}; no duplicate SUT is created."))
            .ToArray();

        return Unit(
            sut,
            mrs,
            ioGroups,
            mutations,
            detections,
            new[]
            {
                "experiments/puts/p3_lorenz.py",
                "experiments/puts/p4_pendulum.py",
                "experiments/puts/p5_pke.py",
                "experiments/puts/p8_schrodinger.py",
                "experiments/puts/p9_openmc.py"
            },
            "codex-batch-b-existing-runtime-reconcile",
            new CompatibilityProfile(
                RuntimeReadiness.RuntimeCandidate,
                new[]
                {
                    "Existing MetBench SUT IDs remain stable; this package attaches provenance and compatibility only.",
                    "minimum-mr-subset-p3 and minimum-mr-subset-p8 dependency gates remain explicit in the live runtime catalogs.",
                    "P8 external np.trapz compatibility risk is recorded as an environment issue unless patched upstream or pinned."
                }));
    }

    public static SutImportUnit CreateBatchCLocalRemaining()
    {
        var sut = new SutAsset(
            "minimum-mr-subset-local-remaining",
            "Minimum-MR-SubSet remaining local numerical PUTs",
            "P2 wave / P6 Poisson / P7 Burgers / P10 PINN-HNN smoke",
            ProgramKind.NumericalSolver,
            new AdapterSpec(
                "external-acceptance-minmr-local-remaining",
                "Pure-stdlib local acceptance runner for remaining Minimum-MR-SubSet numerical PUT smoke MRs.",
                "SUT/external_acceptance_minmr/external_acceptance_minmr.py",
                new[] { "p2_wave", "p6_poisson", "p7_burgers", "p10_pinn_hnn" }),
            new[]
            {
                new ObservableSpec("wave_peak", ObservableKind.Scalar, "amplitude", "P2 wave peak amplitude"),
                new ObservableSpec("poisson_center", ObservableKind.Scalar, "solution", "P6 Poisson center value"),
                new ObservableSpec("burgers_shock", ObservableKind.Scalar, "gradient", "P7 Burgers shock indicator"),
                new ObservableSpec("pinn_hnn_loss", ObservableKind.Scalar, "loss", "P10 PINN/HNN smoke loss")
            },
            Metadata("batch", "C", "package_id", "metbench-import-minmr-local-remaining-v1", "runtime", "pure-stdlib-local"));

        var mrs = new[]
        {
            LocalRemainingMr("minmr-p2-wave-amplitude-linearity", "P2", "P2 wave amplitude linearity smoke", "wave_peak", "ScaleField", "GreaterThan", "greater", "sample/p2_wave.json"),
            LocalRemainingMr("minmr-p6-poisson-source-linearity", "P6", "P6 Poisson source linearity smoke", "poisson_center", "ScaleField", "GreaterThan", "greater", "sample/p6_poisson.json"),
            LocalRemainingMr("minmr-p7-burgers-viscosity-damping", "P7", "P7 Burgers viscosity damping smoke", "burgers_shock", "ScaleField", "LessThan", "less", "sample/p7_burgers.json"),
            LocalRemainingMr("minmr-p10-pinn-hnn-loss-smoke", "P10", "P10 PINN/HNN training-loss smoke", "pinn_hnn_loss", "ScaleField", "LessThan", "less", "sample/p10_pinn_hnn.json")
        };
        var ioGroups = mrs.Select(m => new IoGroup(
            $"{m.Metadata["external_family"].ToLowerInvariant()}-local-smoke-case",
            sut.SutId,
            $"{m.Metadata["external_family"]} local smoke sample",
            new[] { m.Metadata["sample_case_relative_path"] },
            new[] { $"outputs/{m.MrId}" },
            Metadata(
                "external_family", m.Metadata["external_family"],
                "sample_case_relative_path", m.Metadata["sample_case_relative_path"],
                "runner", "SUT/external_acceptance_minmr/external_acceptance_minmr.py",
                "output_parser", "SUT/external_acceptance_minmr/external_acceptance_minmr_output_parser.py")))
            .ToArray();
        var mutations = mrs.Select(m => new MutationAsset(
            $"{m.Metadata["external_family"].ToLowerInvariant()}-smoke-mutant",
            sut.SutId,
            $"{m.Metadata["external_family"]} smoke mutation class",
            $"{m.Metadata["external_family"]}_smoke_operator",
            MutationRepresentationKind.OperatorClassOnly,
            Metadata("external_family", m.Metadata["external_family"], "source", m.Metadata["source"])))
            .ToArray();
        var detections = mrs.Select((m, index) => new DetectionRecord(
            $"batch-c-{m.Metadata["external_family"].ToLowerInvariant()}-smoke",
            m.MrId,
            mutations[index].MutationId,
            ioGroups[index].IoGroupId,
            DetectionResult.Inconclusive,
            EvidenceKind.LocalExecutionEvidence,
            $"Local smoke MR for {m.Metadata["external_family"]}; broad external detection claims are not imported in Batch C."))
            .ToArray();

        return Unit(
            sut,
            mrs,
            ioGroups,
            mutations,
            detections,
            new[]
            {
                "experiments/puts/p2_wave.py",
                "experiments/puts/p6_poisson.py",
                "experiments/puts/p7_burgers.py",
                "experiments/puts/p10_pinn_hnn.py",
                "SUT/external_acceptance_minmr/acceptance-catalog.json"
            },
            "codex-batch-c-local-remaining",
            new CompatibilityProfile(
                RuntimeReadiness.RuntimeCandidate,
                new[] { "Batch C uses pure-stdlib local smoke runners; unsupported external predicates remain imported-only until typed bindings are added." }));
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
        var docker = RuntimeBackendContract.Docker(
            "sciml-mgn-docker",
            "metbench/sciml-mgn:cpu",
            runtimeKey: "docker-sciml-mgn",
            displayName: "SciML MGN Docker runtime",
            resourceHints: new RuntimeResourceHints(CpuCores: 8, MemoryMegabytes: 32768));
        var ssh = RuntimeBackendContract.SshRemote(
            "sciml-mgn-ssh",
            "configured-by-operator",
            "/path/to/mgn/cylinder-flow",
            runtimeKey: "ssh-sciml-mgn",
            displayName: "SciML MGN SSH runtime",
            resourceHints: new RuntimeResourceHints(CpuCores: 16, MemoryMegabytes: 65536, RequiresGpu: true));

        var sut = new SutAsset(
            "sciml-mgn-runtime",
            "Domain-validity gated MGN SciML real-runtime package",
            "mesh graph neural surrogate / cylinder-flow runtime pilots",
            ProgramKind.Surrogate,
            new AdapterSpec(
                "domain-validity-mgn-runtime",
                "Imported runtime extension descriptor for MGN real-SUT pilots. Docker/SSH execution is blocked until MetBench runtime executors are implemented.",
                "research_assets/runs",
                new[] { "docker-runtime-contract", "ssh-runtime-contract", "mgn_cylinder_flow_pilots" }),
            new[]
            {
                new ObservableSpec("node_prediction", ObservableKind.Vector, "state", "Per-node predicted physical state"),
                new ObservableSpec("mirror_y_prediction", ObservableKind.Vector, "state", "Mirrored-y prediction field"),
                new ObservableSpec("discrete_divergence", ObservableKind.Scalar, "residual", "Discrete divergence diagnostic"),
                new ObservableSpec("checkpoint_id", ObservableKind.Summary, "checkpoint", "MGN checkpoint identity"),
                new ObservableSpec("seeded_fault_metric", ObservableKind.Summary, "rate", "Seeded-fault detection metric")
            },
            Metadata(
                "batch", "E",
                "package_id", "metbench-import-sciml-mgn-runtime-v1",
                "runtime_backends", $"{docker.BackendKey};{ssh.BackendKey}",
                "docker_image", docker.Settings["image"],
                "ssh_host", ssh.Settings["host"],
                "ssh_remote_root", ssh.Settings["remote_root"]));

        var mrs = new[]
        {
            MgnRuntimeMr(
                "mgn-runtime-node-permutation-real-pilot",
                sut.SutId,
                "MGN node permutation real-SUT pilot",
                new[] { "node_prediction", "checkpoint_id", "seeded_fault_metric" },
                "research_assets/runs/real-sut-node-permutation-pilot/manifest.yml"),
            MgnRuntimeMr(
                "mgn-runtime-mirror-y-rate-upgrade",
                sut.SutId,
                "MGN mirror-y rate-upgrade pilot",
                new[] { "mirror_y_prediction", "checkpoint_id", "seeded_fault_metric" },
                "research_assets/runs/mirror-y-rate-upgrade/manifest.yml"),
            MgnRuntimeMr(
                "mgn-runtime-mirror-y-symmetric-mesh",
                sut.SutId,
                "MGN mirror-y synthetic symmetric-mesh pilot",
                new[] { "mirror_y_prediction", "checkpoint_id" },
                "research_assets/runs/mirror-y-symmetric-mesh/manifest.yml"),
            MgnRuntimeMr(
                "mgn-runtime-conservation-diagnostic",
                sut.SutId,
                "MGN conservation diagnostic pilot",
                new[] { "discrete_divergence", "checkpoint_id" },
                "research_assets/runs/conservation-diagnostic-pilot/manifest.yml"),
            MgnRuntimeMr(
                "mgn-runtime-seeded-fault-detection",
                sut.SutId,
                "MGN seeded-fault multicheckpoint detection run",
                new[] { "seeded_fault_metric", "checkpoint_id" },
                "research_assets/runs/seeded-fault-detection/manifest.yml")
        };
        var ioGroups = new[]
        {
            new IoGroup(
                "sciml-mgn-runtime-real-sut-artifacts",
                sut.SutId,
                "MGN real-SUT checkpoint and dataset artifacts",
                new[] { "runtime/checkpoint/cylinder-flow.pt", "runtime/dataset/cylinder-flow" },
                new[] { "runtime/outputs/node-permutation", "runtime/outputs/mirror-y", "runtime/outputs/conservation" },
                Metadata(
                    "docker_backend", docker.BackendKey,
                    "ssh_backend", ssh.BackendKey,
                    "runtime_status", "blocked-until-executor")),
            new IoGroup(
                "sciml-mgn-runtime-seeded-fault-ledger",
                sut.SutId,
                "MGN seeded-fault multicheckpoint ledger",
                new[] { "research_assets/runs/seeded-fault-detection/raw/metric_ledger.json" },
                new[] { "runtime/reports/seeded-fault-detection" },
                Metadata("runtime_status", "imported-external-evidence"))
        };
        var mutations = new[]
        {
            ScimlMutation(sut.SutId, "runtime_BC_zero_inflow", "boundary_condition_fault"),
            ScimlMutation(sut.SutId, "runtime_MA_drop_edges", "mesh_adjacency_fault"),
            ScimlMutation(sut.SutId, "runtime_NS_skip_denorm", "normalization_scale_fault"),
            ScimlMutation(sut.SutId, "runtime_TR_sign_flip", "time_reversal_fault"),
            ScimlMutation(sut.SutId, "runtime_PC_swap_xy", "physical_channel_fault")
        };
        var detections = mrs.Select((mr, index) => new DetectionRecord(
            $"batch-e-{mr.MrId}",
            mr.MrId,
            mutations[index % mutations.Length].MutationId,
            ioGroups[index == mrs.Length - 1 ? 1 : 0].IoGroupId,
            DetectionResult.Inconclusive,
            EvidenceKind.ImportedResearchEvidence,
            "Batch E preserves external real-runtime evidence boundaries; MetBench Docker/SSH execution is not claimed."))
            .ToArray();

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
                    "research_assets/runs/seeded-fault-detection/manifest.yml",
                    "research_assets/runs/seeded-fault-detection/raw/metric_ledger.json"
                },
                "codex-batch-e-sciml-mgn-runtime",
                new DateTimeOffset(2026, 6, 11, 0, 0, 0, TimeSpan.Zero)),
            new CompatibilityProfile(
                RuntimeReadiness.ImportedOnly,
                new[]
                {
                    "Docker runtime contract is recorded, but MetBench has no production Docker executor for Batch E.",
                    "SSH runtime contract is recorded, but MetBench has no production SSH executor for Batch E.",
                    "Batch E remains imported-only with external evidence until a runtime executor records end-to-end MetBench execution."
                }));
    }

    private static MrAsset ReconcileMr(
        string mrId,
        string existingSutId,
        string externalFamily,
        string name,
        string observable,
        string transformKind,
        string assertionName,
        string predicateKind,
        string compatibilityRisk)
    {
        return new MrAsset(
            mrId,
            "minimum-mr-subset-existing-runtime-reconcile",
            name,
            new[] { observable },
            new TransformBinding(CompatibilityStatus.MappedSupported, transformKind, ShapeSpec.Scalar(), $"Already bound by existing MetBench SUT {existingSutId}."),
            new AssertionBinding(CompatibilityStatus.MappedSupported, predicateKind, observable, ToleranceSpec.Relative(1e-6), $"Existing runtime assertion {assertionName} is preserved."),
            Metadata(
                "external_family", externalFamily,
                "existing_sut_id", existingSutId,
                "source", $"experiments/puts/{externalFamily.ToLowerInvariant()}_*",
                "compatibility_risk", compatibilityRisk));
    }

    private static string ExistingSutDirectory(string existingSutId) => existingSutId.Replace('-', '_');

    private static MrAsset LocalRemainingMr(
        string mrId,
        string externalFamily,
        string name,
        string observable,
        string transformKind,
        string assertionName,
        string predicateKind,
        string sampleCaseRelativePath)
    {
        return new MrAsset(
            mrId,
            "minimum-mr-subset-local-remaining",
            name,
            new[] { observable },
            new TransformBinding(CompatibilityStatus.MappedSupported, transformKind, ShapeSpec.Scalar(), "Bound to the Batch C pure-stdlib local acceptance runner."),
            new AssertionBinding(CompatibilityStatus.MappedSupported, predicateKind, observable, ToleranceSpec.Relative(1e-6), $"Bound to runtime assertion {assertionName}."),
            Metadata(
                "external_family", externalFamily,
                "source", $"experiments/puts/{externalFamily.ToLowerInvariant()}_*",
                "sample_case_relative_path", sampleCaseRelativePath,
                "runtime_status", "local-smoke"));
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

    private static MrAsset MgnRuntimeMr(
        string mrId,
        string sutId,
        string name,
        IReadOnlyList<string> observables,
        string manifestPath)
    {
        return new MrAsset(
            mrId,
            sutId,
            name,
            observables,
            TransformBinding.ImportedOnly("Requires MetBench Docker or SSH runtime executor support before source/follow-up execution can be claimed."),
            AssertionBinding.ImportedOnly("Requires runtime artifact retrieval and parser binding before pass/fail can be claimed in MetBench."),
            Metadata(
                "source_manifest", manifestPath,
                "runtime_status", "blocked-until-docker-or-ssh-executor",
                "evidence_boundary", "external-runtime-evidence-imported-only"));
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
            ["mgn-discrete-divergence-boundedness"] = new HashSet<string>(StringComparer.Ordinal) { "BC_zero_inflow", "BC_nonzero_wall", "NS_skip_denorm" }
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
