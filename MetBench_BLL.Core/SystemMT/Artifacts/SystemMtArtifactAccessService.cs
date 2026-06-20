using System.Text.Json;
using MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;

namespace MetBench_BLL.Core.SystemMT.Artifacts;

public sealed class SystemMtArtifactAccessService : ISystemMtArtifactAccessService
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private readonly string[] _allowedRoots;

    public SystemMtArtifactAccessService(params string[] allowedArtifactRootPaths)
    {
        if (allowedArtifactRootPaths is null)
            throw new ArgumentNullException(nameof(allowedArtifactRootPaths));

        _allowedRoots = allowedArtifactRootPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(NormalizeDirectoryRoot)
            .Distinct(PathComparer)
            .ToArray();

        if (_allowedRoots.Length == 0)
            throw new ArgumentException("At least one allowed artifact root path is required.", nameof(allowedArtifactRootPaths));
    }

    public async Task<IReadOnlyList<SystemMtArtifactDescriptor>> ListAsync(
        string manifestPath,
        CancellationToken cancellationToken = default)
    {
        var context = await LoadManifestAsync(manifestPath, cancellationToken).ConfigureAwait(false);
        var descriptors = new List<SystemMtArtifactDescriptor>(context.Manifest.Files.Length);

        foreach (var entry in context.Manifest.Files)
        {
            var artifact = ResolveListedArtifact(context.ManifestDirectory, entry);
            var info = new FileInfo(artifact.FullPath);
            descriptors.Add(new SystemMtArtifactDescriptor(
                artifact.ArtifactId,
                Path.GetFileName(artifact.FullPath),
                info.Length,
                GetContentType(artifact.FullPath)));
        }

        return descriptors;
    }

    public async Task<SystemMtArtifactContent> ReadAsync(
        string manifestPath,
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artifactId))
            throw new ArgumentException("Artifact id must be non-blank.", nameof(artifactId));

        var context = await LoadManifestAsync(manifestPath, cancellationToken).ConfigureAwait(false);
        var artifact = context.Manifest.Files
            .Select(entry => ResolveListedArtifact(context.ManifestDirectory, entry))
            .FirstOrDefault(candidate => string.Equals(candidate.ArtifactId, artifactId, StringComparison.Ordinal));

        if (artifact is null)
            throw new KeyNotFoundException($"Artifact '{artifactId}' is not listed by the execution-artifact manifest.");

        return new SystemMtArtifactContent(
            artifact.ArtifactId,
            Path.GetFileName(artifact.FullPath),
            GetContentType(artifact.FullPath),
            await File.ReadAllBytesAsync(artifact.FullPath, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<ManifestContext> LoadManifestFileAsync(
        string manifestPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
            throw new ArgumentException("Manifest path must be non-blank.", nameof(manifestPath));

        var manifestFullPath = Path.GetFullPath(manifestPath);
        if (!File.Exists(manifestFullPath))
            throw new FileNotFoundException("Execution-artifact manifest file was not found.", manifestFullPath);

        var manifestDirectory = Path.GetDirectoryName(manifestFullPath)
            ?? throw new InvalidOperationException("Execution-artifact manifest path has no containing directory.");

        await using var stream = File.OpenRead(manifestFullPath);
        var manifest = await JsonSerializer.DeserializeAsync<ExecutionArtifactExportManifest>(
                stream,
                ExecutionArtifactExporter.JsonOptions,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidDataException("Execution-artifact manifest could not be deserialized.");

        return new ManifestContext(manifestFullPath, NormalizeDirectoryRoot(manifestDirectory), manifest);
    }

    private async Task<ManifestContext> LoadManifestAsync(
        string manifestPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
            throw new ArgumentException("Manifest path must be non-blank.", nameof(manifestPath));

        var manifestFullPath = Path.GetFullPath(manifestPath);
        if (!IsUnderAllowedRoot(manifestFullPath))
            throw new UnauthorizedAccessException("Execution-artifact manifest path is outside the configured artifact roots.");

        return await LoadManifestFileAsync(manifestFullPath, cancellationToken).ConfigureAwait(false);
    }

    private static ResolvedArtifact ResolveListedArtifact(string manifestDirectory, string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
            throw new InvalidDataException("Execution-artifact manifest contains a blank file entry.");
        if (IsRootedOnAnySupportedPlatform(entry))
            throw new UnauthorizedAccessException("Execution-artifact manifest file entries must be relative paths.");

        var relativePath = entry.Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(manifestDirectory, relativePath));
        if (!IsUnderDirectory(fullPath, manifestDirectory))
            throw new UnauthorizedAccessException("Execution-artifact manifest file entry escapes the manifest directory.");
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Execution-artifact file listed by manifest was not found.", fullPath);

        return new ResolvedArtifact(NormalizeArtifactId(entry), fullPath);
    }

    private bool IsUnderAllowedRoot(string path) =>
        _allowedRoots.Any(root => IsUnderDirectory(path, root));

    private static bool IsUnderDirectory(string path, string directoryRoot)
    {
        var fullPath = Path.GetFullPath(path);
        var root = NormalizeDirectoryRoot(directoryRoot);
        return string.Equals(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), root, PathComparison)
            || fullPath.StartsWith(root + Path.DirectorySeparatorChar, PathComparison)
            || fullPath.StartsWith(root + Path.AltDirectorySeparatorChar, PathComparison);
    }

    private static bool IsRootedOnAnySupportedPlatform(string path) =>
        Path.IsPathRooted(path)
        || path.StartsWith("/", StringComparison.Ordinal)
        || path.StartsWith("\\", StringComparison.Ordinal)
        || (path.Length >= 3
            && char.IsLetter(path[0])
            && path[1] == ':'
            && (path[2] == '\\' || path[2] == '/'));

    private static string NormalizeDirectoryRoot(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static string NormalizeArtifactId(string entry) =>
        entry.Replace('\\', '/');

    private static string GetContentType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".json" => "application/json",
            ".html" => "text/html",
            ".md" => "text/markdown",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream",
        };

    private static IEqualityComparer<string> PathComparer { get; } =
        StringComparer.FromComparison(PathComparison);

    private sealed record ManifestContext(
        string ManifestPath,
        string ManifestDirectory,
        ExecutionArtifactExportManifest Manifest);

    private sealed record ResolvedArtifact(string ArtifactId, string FullPath);
}
