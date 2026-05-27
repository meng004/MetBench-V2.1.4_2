using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Catalog;

/// <summary>
/// <see cref="IMrCatalogProvider"/> implementation that reads <c>catalog.json</c> manifests
/// from per-SUT directories under <see cref="LauncherOptions.SutRoot"/>.
/// </summary>
/// <remarks>
/// Manifest discovery: each direct subdirectory of <c>SutRoot</c> is scanned for
/// <c>catalog.json</c>. Each found file is deserialized to <see cref="SystemMtCatalogDocument"/>,
/// validated, and projected to <see cref="MrCatalogEntry"/> instances. Script paths declared
/// in the manifest are RELATIVE to the manifest's own directory; the provider resolves them
/// to absolute paths using <see cref="LauncherOptions.SutRoot"/> + the on-disk directory name.
/// </remarks>
public sealed class ManifestMrCatalogProvider : IMrCatalogProvider
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly LauncherOptions _options;
    private readonly IReadOnlyList<string> _manifestFilePaths;

    public ManifestMrCatalogProvider(LauncherOptions options, IReadOnlyList<string>? manifestFilePaths = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _manifestFilePaths = manifestFilePaths ?? DiscoverManifests(options.SutRoot);
    }

    private static IReadOnlyList<string> DiscoverManifests(string sutRoot)
    {
        if (string.IsNullOrWhiteSpace(sutRoot) || !Directory.Exists(sutRoot))
            return Array.Empty<string>();
        return Directory.GetDirectories(sutRoot)
            .Select(dir => Path.Combine(dir, "catalog.json"))
            .Where(File.Exists)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    public string SourceDescription => $"Manifest:{_options.SutRoot}";

    /// <summary>
    /// Manifest authors write JSON paths with forward slashes (cross-platform convention);
    /// the hardcoded launcher catalog builds paths via <c>Path.Combine</c>, which uses the
    /// platform's <see cref="Path.DirectorySeparatorChar"/>. Normalize the JSON-loaded relative
    /// path to the platform's separator so manifest-derived strings match the hardcoded
    /// strings byte-for-byte on every OS (Windows parity regression discovered post PR #91).
    /// </summary>
    private static string NormalizeRelativePath(string relativePath) =>
        string.IsNullOrEmpty(relativePath)
            ? relativePath
            : relativePath.Replace('/', Path.DirectorySeparatorChar)
                          .Replace('\\', Path.DirectorySeparatorChar);

    public IReadOnlyList<MrCatalogEntry> Load()
    {
        var entries = new List<MrCatalogEntry>();
        foreach (var manifestPath in _manifestFilePaths)
        {
            var sutDir = Path.GetFileName(Path.GetDirectoryName(manifestPath)!);
            var doc = LoadDocument(manifestPath);
            doc.Validate();
            if (doc.Program is null)
                throw new CatalogValidationException(
                    $"Manifest '{manifestPath}' requires a 'program' block at the document root");

            foreach (var binding in doc.Mrs)
            {
                entries.Add(MapToEntry(doc.Program, binding, sutDir));
            }
        }
        return entries;
    }

    private SystemMtCatalogDocument LoadDocument(string path)
    {
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new CatalogValidationException($"Cannot read manifest '{path}': {ex.Message}", ex);
        }

        try
        {
            return JsonSerializer.Deserialize<SystemMtCatalogDocument>(json, JsonOpts)
                ?? throw new CatalogValidationException($"Manifest '{path}' deserialized to null");
        }
        catch (JsonException ex)
        {
            throw new CatalogValidationException($"Manifest '{path}' JSON parse failed: {ex.Message}", ex);
        }
    }

    private MrCatalogEntry MapToEntry(ProgramDefinition program, MrBindingDefinition binding, string sutDir)
    {
        var inputAdapterRel = !string.IsNullOrEmpty(binding.InputAdapterScriptRelativePath)
            ? binding.InputAdapterScriptRelativePath
            : program.InputAdapterScriptRelativePath;
        var outputAdapterRel = !string.IsNullOrEmpty(binding.OutputAdapterScriptRelativePath)
            ? binding.OutputAdapterScriptRelativePath
            : program.OutputAdapterScriptRelativePath;

        // PR-1 T1: route runtime key through the generic resolver. Built-in keys
        // (system / openmoc / openmc / scipy) preserve their existing fallback shape;
        // unknown non-system keys fail closed with a clear diagnostic naming the missing key.
        // Adding a new runtime family (fenics, fipy, ...) is now a config-only change —
        // no switch arm here, no new LauncherOptions field.
        var pythonExecutable = _options.ResolvePythonExecutable(program.PythonExecutableKind);

        var tolerance = (binding.NoiseAware || binding.ToleranceRel > 0 || binding.ToleranceAbs > 0)
            ? new AssertionTolerance(
                NoiseAware: binding.NoiseAware,
                ToleranceRel: binding.ToleranceRel,
                ToleranceAbs: binding.ToleranceAbs,
                NoiseMultiplier: binding.NoiseMultiplier)
            : null;

        var mr = new MrSummary(
            Id: binding.MrId,
            DisplayName: binding.DisplayName,
            SutName: binding.SutName,
            TransformationName: binding.TransformationName,
            AssertionName: binding.AssertionName,
            ValueName: binding.ValueName,
            DefaultParameters: binding.DefaultParameters,
            Description: binding.Description,
            MrFamily: binding.MrFamily);

        return new MrCatalogEntry(
            Mr: mr,
            SampleCaseRelativePath: Path.Combine(sutDir, NormalizeRelativePath(binding.SampleCaseRelativePath)),
            RunnerScriptPath: Path.Combine(_options.SutRoot, sutDir, NormalizeRelativePath(program.RunnerScriptRelativePath)),
            InputAdapterScriptPath: Path.Combine(_options.SutRoot, sutDir, NormalizeRelativePath(inputAdapterRel)),
            OutputAdapterScriptPath: Path.Combine(_options.SutRoot, sutDir, NormalizeRelativePath(outputAdapterRel)),
            PythonExecutable: pythonExecutable,
            WorkRootName: binding.WorkRootName,
            Timeout: TimeSpan.FromSeconds(binding.TimeoutSeconds),
            InputParserScriptPath: Path.Combine(_options.SutRoot, sutDir, NormalizeRelativePath(program.InputParserScriptRelativePath)),
            OutputParserScriptPath: Path.Combine(_options.SutRoot, sutDir, NormalizeRelativePath(program.OutputParserScriptRelativePath)),
            TransformSteps: binding.TransformSteps
                .Select(s => new MrCatalogTransformStep(s.TransformationName, s.TargetFieldPath))
                .ToList(),
            AssertionTypeCode: binding.AssertionTypeCode,
            EquationKey: binding.EquationKey,
            Tolerance: tolerance,
            // PR-Bol-2A: project manifest refinement_phases → runtime RefinementPhase list
            RefinementPhases: binding.RefinementPhases is { Count: > 0 } defs
                ? defs.Select(d => new MetBench_BLL.SystemMT.Pipeline.RefinementPhase(
                    d.Role,
                    new Dictionary<string, string>(d.Parameters, StringComparer.Ordinal))).ToList()
                : null)
        {
            // PR-T3-7: surface manifest meta_pattern so the coverage auditor can
            // build the (equation × meta-pattern) matrix without re-parsing JSON.
            MetaPattern = binding.MetaPattern,
        };
    }
}
