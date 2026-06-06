using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetBench_BLL.Core.SystemMT.ImportExport.Put;

public sealed class SutImportStagingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Func<DateTime> _utcNow;

    public SutImportStagingService(Func<DateTime>? utcNow = null)
    {
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    public string Stage(SutImportUnit unit, string sourcePackageRoot, string stagingRoot)
    {
        if (unit is null)
            throw new ArgumentNullException(nameof(unit));
        if (string.IsNullOrWhiteSpace(sourcePackageRoot))
            throw new SutImportValidationException("Source package root is required.");
        if (string.IsNullOrWhiteSpace(stagingRoot))
            throw new SutImportValidationException("Staging root is required.");

        var validation = SutImportValidator.Validate(unit);
        if (!validation.IsValid)
            throw new SutImportValidationException(
                $"Cannot stage invalid SUT import unit: {string.Join("; ", validation.Errors)}");

        var stagedAt = _utcNow();
        var stagedPackageRoot = AllocateStagedPackageRoot(
            stagingRoot,
            SanitizePathSegment(unit.Sut.SutId),
            stagedAt.ToString("yyyyMMddHHmmssfffffff'Z'"));
        Directory.CreateDirectory(stagedPackageRoot);
        SutImportPackageExporter.Export(unit, stagedPackageRoot);

        var manifest = new SutImportStagingManifest(
            unit.Sut.SutId,
            BuildPackageId(unit),
            Path.GetFullPath(sourcePackageRoot),
            Path.GetFullPath(stagedPackageRoot),
            stagedAt);
        var manifestPath = Path.Combine(stagedPackageRoot, SutImportStagingManifest.FileName);
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions));
        return manifestPath;
    }

    private static string BuildPackageId(SutImportUnit unit)
    {
        var commit = string.IsNullOrWhiteSpace(unit.Provenance.Commit)
            ? "unknown"
            : unit.Provenance.Commit;
        return $"{unit.Sut.SutId}-{commit}";
    }

    private static string SanitizePathSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new SutImportValidationException("SUT id is required for staging.");

        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        var segment = new string(chars);
        if (segment is "." or "..")
            throw new SutImportValidationException($"Unsafe SUT id for staging: '{value}'.");
        return segment;
    }

    private static string AllocateStagedPackageRoot(string stagingRoot, string sutSegment, string timestampSegment)
    {
        var sutRoot = Path.Combine(stagingRoot, sutSegment);
        for (var suffix = 0; suffix < 1000; suffix++)
        {
            var candidateName = suffix == 0
                ? timestampSegment
                : $"{timestampSegment}-{suffix:D3}";
            var candidate = Path.Combine(sutRoot, candidateName);
            if (!Directory.Exists(candidate))
                return candidate;
        }

        throw new SutImportValidationException(
            $"Unable to allocate a unique staging directory for SUT '{sutSegment}' at '{timestampSegment}'.");
    }
}
