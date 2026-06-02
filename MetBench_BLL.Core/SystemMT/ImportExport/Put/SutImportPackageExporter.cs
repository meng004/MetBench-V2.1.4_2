using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetBench_BLL.Core.SystemMT.ImportExport.Put;

public static class SutImportPackageExporter
{
    private const string PackageFileName = "sut-import-unit.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string ResolvePackageFile(string packageRoot, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(packageRoot))
        {
            throw new SutImportValidationException("Package root is required.");
        }

        if (string.IsNullOrWhiteSpace(relativePath)
            || Path.IsPathRooted(relativePath)
            || relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(segment => segment == ".."))
        {
            throw new SutImportValidationException($"Rejected path traversal attempt: '{relativePath}'.");
        }

        var root = Path.GetFullPath(packageRoot);
        var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
        var normalizedRoot = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(normalizedRoot, StringComparison.Ordinal))
        {
            throw new SutImportValidationException($"Rejected path traversal attempt: '{relativePath}'.");
        }

        return candidate;
    }

    public static string Export(SutImportUnit unit, string packageRoot)
    {
        ThrowIfInvalid(unit, "export");
        Directory.CreateDirectory(packageRoot);

        var packageFile = ResolvePackageFile(packageRoot, PackageFileName);
        var json = JsonSerializer.Serialize(unit, JsonOptions);
        File.WriteAllText(packageFile, json);
        return packageFile;
    }

    public static SutImportUnit Import(string packageRoot)
    {
        var packageFile = ResolvePackageFile(packageRoot, PackageFileName);
        var json = File.ReadAllText(packageFile);
        var unit = JsonSerializer.Deserialize<SutImportUnit>(json, JsonOptions)
            ?? throw new SutImportValidationException("Import package did not contain a SUT import unit.");

        ThrowIfInvalid(unit, "import");
        return unit;
    }

    private static void ThrowIfInvalid(SutImportUnit unit, string operation)
    {
        var validation = SutImportValidator.Validate(unit);
        if (!validation.IsValid)
        {
            throw new SutImportValidationException($"Cannot {operation} invalid SUT import unit: {string.Join("; ", validation.Errors)}");
        }
    }
}
