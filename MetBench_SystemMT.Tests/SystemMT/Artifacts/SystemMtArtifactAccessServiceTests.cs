using System.Text.Json;
using MetBench_BLL.Core.SystemMT.Artifacts;
using MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Artifacts;

public sealed class SystemMtArtifactAccessServiceTests
{
    [Fact]
    public async Task ListAsync_returns_public_descriptors_for_manifest_files()
    {
        using var temp = TempDirectory.Create();
        var exportRoot = temp.PathOf("exports", "run-1");
        WriteArtifact(exportRoot, "execution-result.json", """{"passed":true}""");
        WriteArtifact(exportRoot, "reports/report.html", "<html>ok</html>");
        var manifestPath = WriteManifest(exportRoot, "execution-result.json", "reports/report.html");
        var service = new SystemMtArtifactAccessService(temp.PathOf("exports"));

        var descriptors = await service.ListAsync(manifestPath, CancellationToken.None);

        Assert.Collection(
            descriptors,
            first =>
            {
                Assert.Equal("execution-result.json", first.ArtifactId);
                Assert.Equal("execution-result.json", first.FileName);
                Assert.Equal("application/json", first.ContentType);
                Assert.Equal(new FileInfo(Path.Combine(exportRoot, "execution-result.json")).Length, first.Length);
            },
            second =>
            {
                Assert.Equal("reports/report.html", second.ArtifactId);
                Assert.Equal("report.html", second.FileName);
                Assert.Equal("text/html", second.ContentType);
            });
    }

    [Fact]
    public async Task ReadAsync_returns_content_for_listed_artifact_id()
    {
        using var temp = TempDirectory.Create();
        var exportRoot = temp.PathOf("exports", "run-1");
        WriteArtifact(exportRoot, "reports/summary.md", "# summary");
        var manifestPath = WriteManifest(exportRoot, "reports/summary.md");
        var service = new SystemMtArtifactAccessService(temp.PathOf("exports"));

        var content = await service.ReadAsync(manifestPath, "reports/summary.md", CancellationToken.None);

        Assert.Equal("reports/summary.md", content.ArtifactId);
        Assert.Equal("summary.md", content.FileName);
        Assert.Equal("text/markdown", content.ContentType);
        Assert.Equal("# summary", System.Text.Encoding.UTF8.GetString(content.Content));
    }

    [Fact]
    public async Task ListAsync_rejects_manifest_outside_allowed_root()
    {
        using var temp = TempDirectory.Create();
        var allowedRoot = temp.PathOf("allowed");
        Directory.CreateDirectory(allowedRoot);
        var outsideRoot = temp.PathOf("outside");
        WriteArtifact(outsideRoot, "report.html", "<html>outside</html>");
        var manifestPath = WriteManifest(outsideRoot, "report.html");
        var service = new SystemMtArtifactAccessService(allowedRoot);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ListAsync(manifestPath, CancellationToken.None));
    }

    [Fact]
    public async Task ListAsync_rejects_manifest_outside_allowed_root_before_reading_file()
    {
        using var temp = TempDirectory.Create();
        var allowedRoot = temp.PathOf("allowed");
        Directory.CreateDirectory(allowedRoot);
        var outsideRoot = temp.PathOf("outside");
        Directory.CreateDirectory(outsideRoot);
        var manifestPath = Path.Combine(outsideRoot, ExecutionArtifactExportManifest.FileName);
        File.WriteAllText(manifestPath, "not-json");
        var service = new SystemMtArtifactAccessService(allowedRoot);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ListAsync(manifestPath, CancellationToken.None));
    }

    [Fact]
    public async Task ListAsync_rejects_manifest_file_entry_that_escapes_manifest_directory()
    {
        using var temp = TempDirectory.Create();
        var exportRoot = temp.PathOf("exports", "run-1");
        WriteArtifact(temp.PathOf("exports"), "secret.json", """{"secret":true}""");
        var manifestPath = WriteManifest(exportRoot, "../secret.json");
        var service = new SystemMtArtifactAccessService(temp.PathOf("exports"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ListAsync(manifestPath, CancellationToken.None));
    }

    [Fact]
    public async Task ListAsync_rejects_rooted_manifest_file_entry()
    {
        using var temp = TempDirectory.Create();
        var exportRoot = temp.PathOf("exports", "run-1");
        var manifestPath = WriteManifest(exportRoot, "/tmp/secret.json");
        var service = new SystemMtArtifactAccessService(temp.PathOf("exports"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ListAsync(manifestPath, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_rejects_unlisted_artifact_id()
    {
        using var temp = TempDirectory.Create();
        var exportRoot = temp.PathOf("exports", "run-1");
        WriteArtifact(exportRoot, "report.html", "<html>ok</html>");
        var manifestPath = WriteManifest(exportRoot, "report.html");
        var service = new SystemMtArtifactAccessService(temp.PathOf("exports"));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ReadAsync(manifestPath, "execution-result.json", CancellationToken.None));
    }

    [Fact]
    public async Task ListAsync_descriptors_do_not_expose_absolute_paths()
    {
        using var temp = TempDirectory.Create();
        var exportRoot = temp.PathOf("exports", "run-1");
        WriteArtifact(exportRoot, "nested/report.pdf", "pdf");
        var manifestPath = WriteManifest(exportRoot, "nested/report.pdf");
        var service = new SystemMtArtifactAccessService(temp.PathOf("exports"));

        var descriptor = Assert.Single(await service.ListAsync(manifestPath, CancellationToken.None));

        Assert.False(Path.IsPathRooted(descriptor.ArtifactId));
        Assert.False(Path.IsPathRooted(descriptor.FileName));
        Assert.DoesNotContain(exportRoot, descriptor.ArtifactId, StringComparison.Ordinal);
        Assert.DoesNotContain(exportRoot, descriptor.FileName, StringComparison.Ordinal);
        Assert.Equal("application/pdf", descriptor.ContentType);
    }

    [Fact]
    public async Task ListAsync_maps_document_spreadsheet_and_default_content_types()
    {
        using var temp = TempDirectory.Create();
        var exportRoot = temp.PathOf("exports", "run-1");
        WriteArtifact(exportRoot, "report.docx", "docx");
        WriteArtifact(exportRoot, "table.xlsx", "xlsx");
        WriteArtifact(exportRoot, "raw.bin", "bin");
        var manifestPath = WriteManifest(exportRoot, "report.docx", "table.xlsx", "raw.bin");
        var service = new SystemMtArtifactAccessService(temp.PathOf("exports"));

        var descriptors = await service.ListAsync(manifestPath, CancellationToken.None);

        Assert.Equal(
            new[]
            {
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/octet-stream",
            },
            descriptors.Select(descriptor => descriptor.ContentType).ToArray());
    }

    private static string WriteManifest(string directory, params string[] files)
    {
        Directory.CreateDirectory(directory);
        var manifest = new ExecutionArtifactExportManifest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc),
            files);
        var manifestPath = Path.Combine(directory, ExecutionArtifactExportManifest.FileName);
        File.WriteAllText(
            manifestPath,
            JsonSerializer.Serialize(manifest, ExecutionArtifactExporter.JsonOptions));
        return manifestPath;
    }

    private static void WriteArtifact(string directory, string relativePath, string content)
    {
        var path = Path.Combine(directory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), $"metbench-artifacts-{Guid.NewGuid():N}");

        public static TempDirectory Create()
        {
            var temp = new TempDirectory();
            Directory.CreateDirectory(temp.Root);
            return temp;
        }

        public string PathOf(params string[] parts) =>
            Path.Combine(new[] { Root }.Concat(parts).ToArray());

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
