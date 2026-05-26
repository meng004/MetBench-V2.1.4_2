using MetBench_BLL.SystemMT.Assertions;

namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Extracted (Phase B) hardcoded MR catalog factory.
/// Was previously <c>SystemMtLauncher.BuildMrCatalog</c>; lifted here so
/// <see cref="MetBench_BLL.SystemMT.Catalog.IMrCatalogProvider"/> implementations
/// can consume the same blueprints without launcher self-reference. Behavior is
/// byte-identical to the pre-Phase-B inline version.
/// </summary>
internal static class LegacyCatalogFactory
{
    internal static IEnumerable<MrBlueprint> Build(LauncherOptions options)
    {
        yield return new MrBlueprint(
            new MrSummary(
                Id: "openmoc-pincell-nu-sigma-f",
                DisplayName: "OpenMOC pin-cell — ScaleNuSigmaF (k_eff increases)",
                SutName: "openmoc",
                TransformationName: "ScaleNuSigmaF",
                AssertionName: "GreaterThan",
                ValueName: "k_eff",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "1.5" },
                Description:
                    "Scaling the fuel material's nu-sigma-f cross section by factor > 1 must " +
                    "monotonically increase the dominant eigenvalue k_eff of the OpenMOC " +
                    "neutron-transport solution.",
                MrFamily: "NeutronTransport.Scaling.NuSigmaF"),
            SampleCaseRelativePath: Path.Combine("openmoc", "sample", "pincell.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_runner.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_output_adapter.py"),
            PythonExecutable: options.OpenMocPython,
            WorkRootName: "MetBenchOpenMocNuSigmaF",
            Timeout: TimeSpan.FromMinutes(2),
            InputParserScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/materials/fuel/nu_sigma_f") },
            AssertionTypeCode: "greater");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "openmoc-pincell-sigma-a",
                DisplayName: "OpenMOC pin-cell — ScaleFuelSigmaA (k_eff decreases)",
                SutName: "openmoc",
                TransformationName: "ScaleFuelSigmaA",
                AssertionName: "LessThan",
                ValueName: "k_eff",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "1.5" },
                Description:
                    "Scaling the fuel material's absorption cross section by factor > 1 must " +
                    "monotonically decrease the dominant eigenvalue k_eff.",
                MrFamily: "NeutronTransport.Scaling.SigmaA"),
            SampleCaseRelativePath: Path.Combine("openmoc", "sample", "pincell.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_runner.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_input_adapter_sigma_a.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_output_adapter.py"),
            PythonExecutable: options.OpenMocPython,
            WorkRootName: "MetBenchOpenMocSigmaA",
            Timeout: TimeSpan.FromMinutes(2),
            InputParserScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleFuelAbsorption", "/materials/fuel") },
            AssertionTypeCode: "less");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "openmc-pincell-nu-sigma-f",
                DisplayName: "OpenMC pin-cell — ScaleNuSigmaF (k_eff increases)",
                SutName: "openmc",
                TransformationName: "ScaleNuSigmaF",
                AssertionName: "GreaterThan",
                ValueName: "k_eff",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "1.5" },
                Description:
                    "Monte Carlo counterpart of the OpenMOC ScaleNuSigmaF MR. Scaling the " +
                    "fuel material's nu-sigma-f cross section by factor > 1 must monotonically " +
                    "increase k_eff in OpenMC's multi-group eigenvalue solve, just as it does " +
                    "in OpenMOC's deterministic transport solve. Same MR, different solver.",
                MrFamily: "NeutronTransport.Scaling.NuSigmaF"),
            SampleCaseRelativePath: Path.Combine("openmc", "sample", "pincell.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_runner.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_output_adapter.py"),
            PythonExecutable: options.EffectiveOpenMcPython,
            WorkRootName: "MetBenchOpenMcNuSigmaF",
            Timeout: TimeSpan.FromMinutes(5),
            InputParserScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/materials/fuel/nu_sigma_f") },
            AssertionTypeCode: "greater");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "openmc-pincell-sigma-a",
                DisplayName: "OpenMC pin-cell — ScaleFuelSigmaA (k_eff decreases)",
                SutName: "openmc",
                TransformationName: "ScaleFuelSigmaA",
                AssertionName: "LessThan",
                ValueName: "k_eff",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "1.5" },
                Description:
                    "Monte Carlo counterpart of the OpenMOC ScaleFuelSigmaA MR. Scaling fuel " +
                    "absorption by factor > 1 must monotonically decrease k_eff in OpenMC's " +
                    "multi-group eigenvalue solve.",
                MrFamily: "NeutronTransport.Scaling.SigmaA"),
            SampleCaseRelativePath: Path.Combine("openmc", "sample", "pincell.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_runner.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_input_adapter_sigma_a.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_output_adapter.py"),
            PythonExecutable: options.EffectiveOpenMcPython,
            WorkRootName: "MetBenchOpenMcSigmaA",
            Timeout: TimeSpan.FromMinutes(5),
            InputParserScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleFuelAbsorption", "/materials/fuel") },
            AssertionTypeCode: "less");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "heat-equation-amplitude",
                DisplayName: "1D heat equation — ScaleAmplitude (linearity)",
                SutName: "heat-equation",
                TransformationName: "ScaleAmplitude",
                AssertionName: "GreaterThan",
                ValueName: "max_u",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "1D heat equation with homogeneous Dirichlet BCs is linear in the initial " +
                    "profile. Scaling the initial amplitude by factor > 1 must scale max_u at " +
                    "t_final by the same factor.",
                MrFamily: "Diffusion.Scaling.Amplitude"),
            SampleCaseRelativePath: Path.Combine("heat_equation", "sample", "gaussian.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchHeatEq",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/initial/amplitude") },
            AssertionTypeCode: "greater");

        // S8-P2: Fourier MR 库扩展（复用 heat-equation SUT）
        yield return new MrBlueprint(
            new MrSummary(
                Id: "fourier-timestep-convergence",
                DisplayName: "1D heat equation — TimestepConvergence (forward-Euler refinement)",
                SutName: "heat-equation",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "max_u",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Time-step convergence MP_conv: doubling num_steps (halving the forward-Euler " +
                    "time-step dt) must leave max_u(t_final) within Euler truncation tolerance — " +
                    "the integrator is already at the fine-grid plateau given the chosen alpha.",
                MrFamily: "Fourier.Convergence.Timestep"),
            SampleCaseRelativePath: Path.Combine("heat_equation", "sample", "gaussian.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchHeatEq",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/params/num_steps") },
            AssertionTypeCode: "approx",
            EquationKey: "heat-equation-1d",
            // forward-Euler 1 阶 O(dt)；步长翻倍后 max_u 差 ~1e-3 量级；ToleranceRel=1e-2 → 充裕
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-2, ToleranceAbs: 1e-6));

        yield return new MrBlueprint(
            new MrSummary(
                Id: "fourier-alpha-monotonic",
                DisplayName: "1D heat equation — ScaleAlpha (diffusion monotonicity)",
                SutName: "heat-equation",
                TransformationName: "ScaleField",
                AssertionName: "LessThan",
                ValueName: "max_u",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Diffusion coefficient monotonicity MP_mono: at fixed t_final, larger alpha " +
                    "causes faster diffusive smoothing of the initial profile, so scaling alpha " +
                    "by factor > 1 must strictly decrease max_u(t_final).",
                MrFamily: "Fourier.Scaling.Alpha"),
            SampleCaseRelativePath: Path.Combine("heat_equation", "sample", "gaussian.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchHeatEq",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "heat_equation", "heat_equation_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/params/alpha") },
            AssertionTypeCode: "less",
            EquationKey: "heat-equation-1d");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "decay-chain-scale-initial",
                DisplayName: "Decay chain — ScaleInitial (linearity)",
                SutName: "decay-chain",
                TransformationName: "ScaleInitial",
                AssertionName: "GreaterThan",
                ValueName: "N_C_final",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "The 3-nuclide Bateman decay chain A→B→C is linear in the initial " +
                    "quantities. Scaling every initial N by factor > 1 must scale the " +
                    "accumulated end-nuclide N_C_final by the same factor.",
                MrFamily: "Bateman.Scaling.Initial"),
            SampleCaseRelativePath: Path.Combine("decay_chain", "sample", "three_nuclide.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchDecayChain",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_output_parser.py"),
            // P4: 使用 L1 Recipe（EquationFunctionRegistry["bateman","ScaleInitial"]）
            // 单步声明；路径由 Recipe 内部各 ScaleField step 管理
            TransformSteps: new[] { new MrTransformStep("ScaleInitial", "") },
            AssertionTypeCode: "greater",
            EquationKey: "bateman");

        // S8-P1: Bateman MR 库扩展（复用 decay-chain SUT）
        yield return new MrBlueprint(
            new MrSummary(
                Id: "bateman-mass-conservation",
                DisplayName: "Bateman — MassConservation (lambda invariance)",
                SutName: "decay-chain",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "total",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Mass conservation MP_inv: the Bateman chain A→B→C conserves total " +
                    "nuclide count (no production/absorption). Scaling lambda_A must not " +
                    "change the conserved total = N_A+N_B+N_C at t_final.",
                MrFamily: "Bateman.Invariance.MassConservation"),
            SampleCaseRelativePath: Path.Combine("decay_chain", "sample", "three_nuclide.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchDecayChain",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/params/lambda_A") },
            AssertionTypeCode: "approx",
            EquationKey: "bateman",
            // 守恒 total ≈ N_A0+N_B0+N_C0=1000；RK4 累积截断误差 ~1e-6 量级；ToleranceRel=1e-6 → bound ≈ 1e-3
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-6, ToleranceAbs: 1e-9));

        yield return new MrBlueprint(
            new MrSummary(
                Id: "bateman-timestep-cauchy",
                DisplayName: "Bateman — TimestepCauchy (RK4 convergence)",
                SutName: "decay-chain",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "N_C_final",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Time-step Cauchy convergence MP_conv: doubling num_steps (halving the " +
                    "RK4 step size) must leave N_C_final within RK4 truncation tolerance — " +
                    "the integrator is already at the fine-grid plateau.",
                MrFamily: "Bateman.Convergence.Timestep"),
            SampleCaseRelativePath: Path.Combine("decay_chain", "sample", "three_nuclide.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchDecayChain",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "decay_chain", "decay_chain_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/params/num_steps") },
            AssertionTypeCode: "approx",
            EquationKey: "bateman",
            // RK4 4 阶截断 O(dt^4)；步长翻倍后 max_u 差 ~1e-4 量级；ToleranceRel=1e-3 → 充裕
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-3, ToleranceAbs: 1e-6));

        yield return new MrBlueprint(
            new MrSummary(
                Id: "damped-oscillator-scale-state",
                DisplayName: "Damped oscillator — ScaleInitialState (linearity)",
                SutName: "damped-oscillator",
                TransformationName: "ScaleInitialState",
                AssertionName: "GreaterThan",
                ValueName: "max_abs_displacement",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "The damped harmonic oscillator is linear and homogeneous in the " +
                    "initial state (x0, v0). Scaling the initial state by factor > 1 must " +
                    "scale the peak absolute displacement by the same factor.",
                MrFamily: "Oscillator.Scaling.InitialState"),
            SampleCaseRelativePath: Path.Combine("damped_oscillator", "sample", "underdamped.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "damped_oscillator", "damped_oscillator.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "damped_oscillator", "damped_oscillator_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "damped_oscillator", "damped_oscillator_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchDampedOsc",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "damped_oscillator", "damped_oscillator_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "damped_oscillator", "damped_oscillator_output_parser.py"),
            TransformSteps: new[]
            {
                new MrTransformStep("ScaleField", "/initial/x0"),
                new MrTransformStep("ScaleField", "/initial/v0"),
            },
            AssertionTypeCode: "greater");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "lotka-volterra-scale-gamma",
                DisplayName: "Lotka-Volterra — ScaleGamma (mean-prey identity)",
                SutName: "lotka-volterra",
                TransformationName: "ScaleGamma",
                AssertionName: "GreaterThan",
                ValueName: "mean_prey",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "By the Lotka-Volterra time-average identity ⟨prey⟩ = gamma / delta, " +
                    "scaling the predator death rate gamma by factor > 1 must increase " +
                    "the time-averaged prey population mean_prey.",
                MrFamily: "LotkaVolterra.Scaling.Gamma"),
            SampleCaseRelativePath: Path.Combine("lotka_volterra", "sample", "classic.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "lotka_volterra", "lotka_volterra.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "lotka_volterra", "lotka_volterra_input_adapter.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "lotka_volterra", "lotka_volterra_output_adapter.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchLotkaVolterra",
            Timeout: TimeSpan.FromSeconds(60),
            InputParserScriptPath: Path.Combine(options.SutRoot, "lotka_volterra", "lotka_volterra_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "lotka_volterra", "lotka_volterra_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/params/gamma") },
            AssertionTypeCode: "greater");

        // T3C-IVP: SciPy `solve_ivp`-backed Lotka-Volterra (first external-library-dependent SUT).
        // Same equation `lotka-volterra` re-used; SUT id is `scipy-ivp-lotka-volterra`.
        // Uses options.EffectiveScipyPython (env METBENCH_SCIPY_PYTHON; falls back to SystemPython).
        yield return new MrBlueprint(
            new MrSummary(
                Id: "scipy-ivp-lv-prey-growth-monotone",
                DisplayName: "SciPy IVP Lotka-Volterra — ScaleGamma (mean-prey identity)",
                SutName: "scipy-ivp-lotka-volterra",
                TransformationName: "ScaleGamma",
                AssertionName: "GreaterThan",
                ValueName: "mean_prey",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "By the Lotka-Volterra time-average identity ⟨prey⟩ = gamma / delta, " +
                    "scaling the predator death rate gamma by factor > 1 must strictly increase " +
                    "the time-averaged prey population mean_prey, even when the integrator is " +
                    "SciPy's adaptive RK45.",
                MrFamily: "ScipyIvp.LotkaVolterra.Scaling.Gamma"),
            SampleCaseRelativePath: Path.Combine("scipy_ivp_lotka_volterra", "sample", "standard.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "scipy_ivp_lotka_volterra", "scipy_ivp_lotka_volterra.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "scipy_ivp_lotka_volterra", "scipy_ivp_lotka_volterra_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "scipy_ivp_lotka_volterra", "scipy_ivp_lotka_volterra_output_parser.py"),
            PythonExecutable: options.EffectiveScipyPython,
            WorkRootName: "MetBenchScipyIvpLotkaVolterra",
            Timeout: TimeSpan.FromSeconds(120),
            InputParserScriptPath: Path.Combine(options.SutRoot, "scipy_ivp_lotka_volterra", "scipy_ivp_lotka_volterra_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "scipy_ivp_lotka_volterra", "scipy_ivp_lotka_volterra_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/params/gamma") },
            AssertionTypeCode: "greater",
            EquationKey: "lotka-volterra");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "scipy-ivp-lv-step-convergence",
                DisplayName: "SciPy IVP Lotka-Volterra — EvalGridRefinement (trapezoidal-mean convergence)",
                SutName: "scipy-ivp-lotka-volterra",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "mean_prey",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "SciPy adaptive RK45 decouples solution accuracy from num_eval_points " +
                    "(which only controls the output sampling grid). Doubling num_eval_points " +
                    "must leave mean_prey within tolerance because the trapezoidal time-average " +
                    "over a denser eval grid converges to the same continuous time-average " +
                    "⟨prey⟩ = gamma/delta.",
                MrFamily: "ScipyIvp.LotkaVolterra.Convergence.EvalGrid"),
            SampleCaseRelativePath: Path.Combine("scipy_ivp_lotka_volterra", "sample", "standard.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "scipy_ivp_lotka_volterra", "scipy_ivp_lotka_volterra.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "scipy_ivp_lotka_volterra", "scipy_ivp_lotka_volterra_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "scipy_ivp_lotka_volterra", "scipy_ivp_lotka_volterra_output_parser.py"),
            PythonExecutable: options.EffectiveScipyPython,
            WorkRootName: "MetBenchScipyIvpLotkaVolterra",
            Timeout: TimeSpan.FromSeconds(120),
            InputParserScriptPath: Path.Combine(options.SutRoot, "scipy_ivp_lotka_volterra", "scipy_ivp_lotka_volterra_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "scipy_ivp_lotka_volterra", "scipy_ivp_lotka_volterra_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/params/num_eval_points") },
            AssertionTypeCode: "approx",
            EquationKey: "lotka-volterra",
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-3, ToleranceAbs: 1e-6));

        // T3C-BVP: SciPy `solve_bvp`-backed 1D Poisson (second external-library-dependent SUT).
        // Same equation `poisson` re-used; SUT id is `scipy-bvp-poisson-1d`.
        // Uses options.EffectiveScipyPython (env METBENCH_SCIPY_PYTHON; falls back to SystemPython).
        yield return new MrBlueprint(
            new MrSummary(
                Id: "scipy-bvp-poisson-source-superposition",
                DisplayName: "SciPy BVP 1D Poisson — ScaleSource (linearity)",
                SutName: "scipy-bvp-poisson-1d",
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "u_max",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Superposition / linearity MP_mono: the 1D Poisson equation -u'' = f is " +
                    "linear in the source f. Scaling f by factor > 1 must strictly increase " +
                    "the peak amplitude u_max — analytically by exactly the same factor — " +
                    "even when the integrator is SciPy's adaptive solve_bvp.",
                MrFamily: "ScipyBvp.Poisson.Scaling.Source"),
            SampleCaseRelativePath: Path.Combine("scipy_bvp_poisson_1d", "sample", "standard.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "scipy_bvp_poisson_1d", "scipy_bvp_poisson_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "scipy_bvp_poisson_1d", "scipy_bvp_poisson_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "scipy_bvp_poisson_1d", "scipy_bvp_poisson_1d_output_parser.py"),
            PythonExecutable: options.EffectiveScipyPython,
            WorkRootName: "MetBenchScipyBvpPoisson1d",
            Timeout: TimeSpan.FromSeconds(120),
            InputParserScriptPath: Path.Combine(options.SutRoot, "scipy_bvp_poisson_1d", "scipy_bvp_poisson_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "scipy_bvp_poisson_1d", "scipy_bvp_poisson_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/source/strength") },
            AssertionTypeCode: "greater",
            EquationKey: "poisson");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "scipy-bvp-poisson-seed-mesh-insensitivity",
                DisplayName: "SciPy BVP 1D Poisson — InitialMeshRefinement (adaptive-BVP convergence)",
                SutName: "scipy-bvp-poisson-1d",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "u_max",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "SciPy solve_bvp adaptively refines its own mesh until residual is below " +
                    "tolerance; the user-supplied num_points only seeds the initial mesh. " +
                    "Doubling the seed num_points must leave u_max within tolerance because " +
                    "both runs converge to the same continuous solution u(x)=f·x(L−x)/2, " +
                    "whose peak value u_max = f·L²/8 occurs at x = L/2.",
                MrFamily: "ScipyBvp.Poisson.Convergence.SeedMesh"),
            SampleCaseRelativePath: Path.Combine("scipy_bvp_poisson_1d", "sample", "standard.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "scipy_bvp_poisson_1d", "scipy_bvp_poisson_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "scipy_bvp_poisson_1d", "scipy_bvp_poisson_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "scipy_bvp_poisson_1d", "scipy_bvp_poisson_1d_output_parser.py"),
            PythonExecutable: options.EffectiveScipyPython,
            WorkRootName: "MetBenchScipyBvpPoisson1d",
            Timeout: TimeSpan.FromSeconds(120),
            InputParserScriptPath: Path.Combine(options.SutRoot, "scipy_bvp_poisson_1d", "scipy_bvp_poisson_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "scipy_bvp_poisson_1d", "scipy_bvp_poisson_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/geometry/num_points") },
            AssertionTypeCode: "approx",
            EquationKey: "poisson",
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-3, ToleranceAbs: 1e-6));

        yield return new MrBlueprint(
            new MrSummary(
                Id: "projectile-scale-v0",
                DisplayName: "Projectile range — ScaleV0 (R ∝ v0² monotonic)",
                SutName: "projectile",
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "range",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "By the projectile-motion identity R = v0²·sin(2θ)/g, scaling the " +
                    "initial speed v0 by factor > 1 must monotonically increase the " +
                    "horizontal range (in fact, by factor²).",
                MrFamily: "Projectile.Scaling.V0"),
            SampleCaseRelativePath: Path.Combine("projectile", "sample", "standard.txt"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "projectile", "projectile.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "projectile", "projectile_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "projectile", "projectile_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchProjectile",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "projectile", "projectile_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "projectile", "projectile_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/v0") },
            AssertionTypeCode: "greater");

        // S8-P3: 1D subchannel SUT + navier-stokes 方程接入
        yield return new MrBlueprint(
            new MrSummary(
                Id: "subchannel-flow-temperature-monotone",
                DisplayName: "1D subchannel — ScaleMassFlux (flow ↑ ⇒ ΔT ↓)",
                SutName: "subchannel-1d",
                TransformationName: "ScaleField",
                AssertionName: "LessThan",
                ValueName: "delta_T",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Energy conservation MP_mono: at fixed wall heat flux q'' and inlet " +
                    "temperature, ΔT = q''·P_h·L/(G·A_xs·c_p) is inversely proportional to " +
                    "mass flux G — higher flow strictly decreases the outlet temperature rise.",
                MrFamily: "Subchannel.Scaling.MassFlux"),
            SampleCaseRelativePath: Path.Combine("subchannel_1d", "sample", "pwr_channel.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchSubchannel",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/boundary/mass_flux") },
            AssertionTypeCode: "less",
            EquationKey: "navier-stokes");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "subchannel-heat-flux-linearity",
                DisplayName: "1D subchannel — ScaleHeatFlux (linearity in q'')",
                SutName: "subchannel-1d",
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "delta_T",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Energy conservation MP_mono (linearity in heat input): doubling the wall " +
                    "heat flux q'' must strictly increase ΔT (in fact, by the same factor) " +
                    "because energy balance is linear in q'' at constant flow.",
                MrFamily: "Subchannel.Scaling.HeatFlux"),
            SampleCaseRelativePath: Path.Combine("subchannel_1d", "sample", "pwr_channel.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchSubchannel",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "subchannel_1d", "subchannel_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/boundary/heat_flux") },
            AssertionTypeCode: "greater",
            EquationKey: "navier-stokes");

        // S8-P4: 1D diffusion SUT + diffusion 方程接入
        yield return new MrBlueprint(
            new MrSummary(
                Id: "diffusion-source-linearity",
                DisplayName: "1D diffusion — ScaleSource (linearity)",
                SutName: "diffusion-1d",
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "phi_max",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Linearity MP_mono: the steady-state diffusion equation -D·φ″ + Σ_a·φ = S " +
                    "is linear in the source S. Scaling S by factor > 1 must strictly increase " +
                    "the peak flux φ_max (in fact, by the same factor).",
                MrFamily: "Diffusion.Scaling.Source"),
            SampleCaseRelativePath: Path.Combine("diffusion_1d", "sample", "slab.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchDiffusion1d",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/source/strength") },
            AssertionTypeCode: "greater",
            EquationKey: "diffusion");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "diffusion-mesh-richardson",
                DisplayName: "1D diffusion — MeshRichardson (FD convergence)",
                SutName: "diffusion-1d",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "phi_max",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Mesh-refinement Richardson convergence MP_conv: doubling num_points (halving " +
                    "the FD spacing dx) must leave φ_max within FD truncation tolerance — the " +
                    "second-order scheme is already at the fine-mesh plateau.",
                MrFamily: "Diffusion.Convergence.Mesh"),
            SampleCaseRelativePath: Path.Combine("diffusion_1d", "sample", "slab.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchDiffusion1d",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "diffusion_1d", "diffusion_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/geometry/num_points") },
            AssertionTypeCode: "approx",
            EquationKey: "diffusion",
            // FD 2 阶 O(dx²)；网格加密后 phi_max 差 ~1e-4 量级（已 plateau）；ToleranceRel=1e-3 → 充裕
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-3, ToleranceAbs: 1e-6));

        // T3 expansion: 1D Poisson SUT + Poisson 方程（broader elliptic PDE coverage
        // beyond the 5 reactor anchors）.
        yield return new MrBlueprint(
            new MrSummary(
                Id: "poisson-source-superposition",
                DisplayName: "1D Poisson — ScaleSource (linearity)",
                SutName: "poisson-1d",
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "u_max",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Superposition / linearity MP_mono: the 1D Poisson equation -u'' = f " +
                    "is linear in the source f. Scaling f by factor > 1 must strictly increase " +
                    "the peak amplitude u_max (and in fact scale it by the same factor exactly).",
                MrFamily: "Poisson.Scaling.Source"),
            SampleCaseRelativePath: Path.Combine("poisson_1d", "sample", "standard.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "poisson_1d", "poisson_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "poisson_1d", "poisson_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "poisson_1d", "poisson_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchPoisson1d",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "poisson_1d", "poisson_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "poisson_1d", "poisson_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/source/strength") },
            AssertionTypeCode: "greater",
            EquationKey: "poisson");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "poisson-mesh-richardson",
                DisplayName: "1D Poisson — MeshRichardson (FD convergence)",
                SutName: "poisson-1d",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "u_max",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Mesh-refinement Richardson convergence MP_conv: doubling num_points " +
                    "(halving the FD spacing dx) must leave u_max within O(dx²) FD truncation " +
                    "tolerance — the second-order central-difference scheme is already at the " +
                    "fine-mesh plateau.",
                MrFamily: "Poisson.Convergence.Mesh"),
            SampleCaseRelativePath: Path.Combine("poisson_1d", "sample", "standard.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "poisson_1d", "poisson_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "poisson_1d", "poisson_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "poisson_1d", "poisson_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchPoisson1d",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "poisson_1d", "poisson_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "poisson_1d", "poisson_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/geometry/num_points") },
            AssertionTypeCode: "approx",
            EquationKey: "poisson",
            // 2nd-order central FD with constant source resolves the quadratic analytic
            // solution exactly to machine precision; halving dx changes u_max by ~1e-15.
            // ToleranceRel=1e-3 + Atol=1e-6 is far more than needed; keep parity with diffusion-mesh-richardson.
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-3, ToleranceAbs: 1e-6));

        // T3 expansion: 1D linear advection SUT + Advection 方程接入（first hyperbolic-PDE
        // SUT after Poisson; pure-stdlib first-order upwind FD with periodic boundaries）.
        yield return new MrBlueprint(
            new MrSummary(
                Id: "advection-amplitude-linearity",
                DisplayName: "1D advection — ScaleInitial (amplitude linearity)",
                SutName: "advection-1d",
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "peak_amplitude",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Linearity MP_mono: the 1D advection equation u_t + v·u_x = 0 is linear " +
                    "in the initial condition. Scaling the initial Gaussian amplitude by " +
                    "factor > 1 must strictly increase the final peak amplitude (and in fact " +
                    "scale it by the same factor exactly, since the conservative upwind scheme " +
                    "preserves linearity).",
                MrFamily: "Advection.Scaling.Amplitude"),
            SampleCaseRelativePath: Path.Combine("advection_1d", "sample", "standard.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "advection_1d", "advection_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "advection_1d", "advection_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "advection_1d", "advection_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchAdvection1d",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "advection_1d", "advection_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "advection_1d", "advection_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/initial/amplitude") },
            AssertionTypeCode: "greater",
            EquationKey: "advection");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "advection-mesh-conservation",
                DisplayName: "1D advection — MeshRefinement (mass conservation)",
                SutName: "advection-1d",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "mass_integral",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Mass conservation MP_inv: the conservative first-order upwind scheme " +
                    "with periodic boundaries preserves the integral ∫u dx to floating-point " +
                    "precision. Doubling num_points changes the initial-condition sampling " +
                    "by O(dx²) but the per-step mass conservation is exact; the final " +
                    "mass_integral must agree within tight FD tolerance.",
                MrFamily: "Advection.Invariance.Mass"),
            SampleCaseRelativePath: Path.Combine("advection_1d", "sample", "standard.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "advection_1d", "advection_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "advection_1d", "advection_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "advection_1d", "advection_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchAdvection1d",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "advection_1d", "advection_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "advection_1d", "advection_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/geometry/num_points") },
            AssertionTypeCode: "approx",
            EquationKey: "advection",
            // 一阶迎风格式严格守恒 ∫u dx；网格加密 200→400 时 mass_integral 仅因初始 Gaussian
            // 采样差异变化 O(dx²) ~ 4e-9（实测）；ToleranceRel=1e-3 + Atol=1e-6 充裕。
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-3, ToleranceAbs: 1e-6));

        // T3 expansion: 1D linear wave SUT + Wave 方程接入（second hyperbolic-PDE SUT after
        // Advection; pure-stdlib second-order leapfrog FD with Dirichlet boundaries）.
        yield return new MrBlueprint(
            new MrSummary(
                Id: "wave-amplitude-linearity",
                DisplayName: "1D wave — ScaleInitial (amplitude linearity)",
                SutName: "wave-1d",
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "peak_amplitude",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Linearity MP_mono: the 1D linear wave equation u_tt = c²·u_xx is linear " +
                    "in the initial condition. Scaling the initial Gaussian amplitude by " +
                    "factor > 1 must strictly increase the final peak amplitude (and in fact " +
                    "scale it by the same factor exactly under the linear leapfrog discretisation).",
                MrFamily: "Wave.Scaling.Amplitude"),
            SampleCaseRelativePath: Path.Combine("wave_1d", "sample", "standard.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "wave_1d", "wave_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "wave_1d", "wave_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "wave_1d", "wave_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchWave1d",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "wave_1d", "wave_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "wave_1d", "wave_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/initial/amplitude") },
            AssertionTypeCode: "greater",
            EquationKey: "wave");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "wave-mesh-energy-convergence",
                DisplayName: "1D wave — MeshRefinement (energy proxy convergence)",
                SutName: "wave-1d",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "energy_proxy",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Mesh-refinement convergence MP_conv: the second-order leapfrog scheme " +
                    "converges as O(dx², dt²). Doubling num_points (with internal dt halving to " +
                    "keep Courant = 0.5) must leave the integrated energy proxy 0.5·∫u² dx " +
                    "within FD truncation tolerance — for a smooth Gaussian initial pulse the " +
                    "L² invariant is preserved to near machine precision under refinement.",
                MrFamily: "Wave.Convergence.Energy"),
            SampleCaseRelativePath: Path.Combine("wave_1d", "sample", "standard.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "wave_1d", "wave_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "wave_1d", "wave_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "wave_1d", "wave_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchWave1d",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "wave_1d", "wave_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "wave_1d", "wave_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/geometry/num_points") },
            AssertionTypeCode: "approx",
            EquationKey: "wave",
            // 二阶 leapfrog 在平滑 Gaussian 上 L² 不变量到机器精度；num_points 翻倍（dt 内部
            // 同步减半保持 Courant=0.5）后 energy_proxy 实测变化 ~1e-12；ToleranceRel=1e-3 + Atol=1e-6 充裕。
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-3, ToleranceAbs: 1e-6));

        // T3 expansion: 1D inviscid Burgers SUT + Burgers 方程接入（first nonlinear-PDE
        // SUT after the linear hyperbolic family Advection / Wave; pure-stdlib Lax-Friedrichs
        // conservative flux differencing with periodic boundaries）.
        yield return new MrBlueprint(
            new MrSummary(
                Id: "burgers-amplitude-peak-monotone",
                DisplayName: "1D Burgers — ScaleInitial (amplitude → peak monotone)",
                SutName: "burgers-1d",
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "peak_amplitude",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Amplitude-monotonicity MP_mono: the inviscid Burgers equation " +
                    "u_t + (u²/2)_x = 0 is nonlinear, but strictly-positive scaling of the " +
                    "initial Gaussian pulse must strictly increase the final peak amplitude. " +
                    "Lax-Friedrichs is dissipative (peak ratio < 2 due to shock smearing), so " +
                    "the assertion is GreaterThan rather than equal scaling.",
                MrFamily: "Burgers.Scaling.Amplitude"),
            SampleCaseRelativePath: Path.Combine("burgers_1d", "sample", "standard.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "burgers_1d", "burgers_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "burgers_1d", "burgers_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "burgers_1d", "burgers_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchBurgers1d",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "burgers_1d", "burgers_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "burgers_1d", "burgers_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/initial/amplitude") },
            AssertionTypeCode: "greater",
            EquationKey: "burgers");

        yield return new MrBlueprint(
            new MrSummary(
                Id: "burgers-mesh-conservation",
                DisplayName: "1D Burgers — MeshRefinement (mass conservation)",
                SutName: "burgers-1d",
                TransformationName: "ScaleField",
                AssertionName: "ApproxEqual",
                ValueName: "mass_integral",
                DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
                Description:
                    "Mass conservation MP_inv: the conservative Lax-Friedrichs flux " +
                    "differencing with periodic boundaries preserves ∫u dx exactly per step " +
                    "regardless of the shock structure. Doubling num_points changes the " +
                    "initial Gaussian sampling by O(dx²) but per-step mass conservation is " +
                    "exact; the final mass_integral must agree within tight FD tolerance.",
                MrFamily: "Burgers.Invariance.Mass"),
            SampleCaseRelativePath: Path.Combine("burgers_1d", "sample", "standard.json"),
            RunnerScriptPath: Path.Combine(options.SutRoot, "burgers_1d", "burgers_1d.py"),
            InputAdapterScriptPath: Path.Combine(options.SutRoot, "burgers_1d", "burgers_1d_input_parser.py"),
            OutputAdapterScriptPath: Path.Combine(options.SutRoot, "burgers_1d", "burgers_1d_output_parser.py"),
            PythonExecutable: options.SystemPython,
            WorkRootName: "MetBenchBurgers1d",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: Path.Combine(options.SutRoot, "burgers_1d", "burgers_1d_input_parser.py"),
            OutputParserScriptPath: Path.Combine(options.SutRoot, "burgers_1d", "burgers_1d_output_parser.py"),
            TransformSteps: new[] { new MrTransformStep("ScaleField", "/geometry/num_points") },
            AssertionTypeCode: "approx",
            EquationKey: "burgers",
            // LxF 守恒流差分严格保 ∫u dx；num_points 翻倍后实测 mass_integral 变化 ~6e-10
            // （初始 Gaussian 采样误差 O(dx²)）；ToleranceRel=1e-3 + Atol=1e-6 充裕。
            Tolerance: new AssertionTolerance(ToleranceRel: 1e-3, ToleranceAbs: 1e-6));
    }
}
