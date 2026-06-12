using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MetBench_BLL.SystemMT.Runtime;

public sealed record DockerMcpRuntimeProfileDraft(
    string RuntimeKey,
    string Endpoint,
    string Image,
    string PythonExecutable,
    string? AuthTokenEnvironmentVariable = null)
{
    public string NormalizedRuntimeKey => RuntimeProfile.NormalizeRuntimeKey(RuntimeKey);

    public string ToRuntimePythonValue()
    {
        Validate();

        var query = new List<string>
        {
            $"image={Uri.EscapeDataString(Image.Trim())}",
            $"python={Uri.EscapeDataString(PythonExecutable.Trim())}",
            $"endpoint={Uri.EscapeDataString(Endpoint.Trim())}",
        };
        if (!string.IsNullOrWhiteSpace(AuthTokenEnvironmentVariable))
            query.Add($"authTokenEnv={Uri.EscapeDataString(AuthTokenEnvironmentVariable.Trim())}");

        return $"docker-mcp://{NormalizedRuntimeKey}?{string.Join("&", query)}";
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RuntimeKey))
            throw new ArgumentException("Runtime key is required.", nameof(RuntimeKey));
        if (string.IsNullOrWhiteSpace(Endpoint)
            || !Uri.TryCreate(Endpoint.Trim(), UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Endpoint must be an absolute http or https URI.", nameof(Endpoint));
        }

        if (string.IsNullOrWhiteSpace(Image))
            throw new ArgumentException("Docker image is required.", nameof(Image));
        if (string.IsNullOrWhiteSpace(PythonExecutable))
            throw new ArgumentException("Python executable is required.", nameof(PythonExecutable));
    }
}

public interface IDockerMcpRuntimeProfileStore
{
    IReadOnlyDictionary<string, string> LoadRuntimePythons();

    void Save(DockerMcpRuntimeProfileDraft draft);
}

public sealed class LocalDockerMcpRuntimeProfileStore : IDockerMcpRuntimeProfileStore
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _path;

    public LocalDockerMcpRuntimeProfileStore(string path)
    {
        _path = string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("Settings path is required.", nameof(path))
            : path;
    }

    public IReadOnlyDictionary<string, string> LoadRuntimePythons()
    {
        var root = LoadRoot();
        var runtimePythons = root["LauncherOptions"]?["RuntimePythons"]?.AsObject();
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (runtimePythons is null)
            return result;

        foreach (var pair in runtimePythons)
        {
            if (pair.Value is not null && pair.Value.GetValueKind() == JsonValueKind.String)
                result[pair.Key] = pair.Value.GetValue<string>();
        }

        return result;
    }

    public void Save(DockerMcpRuntimeProfileDraft draft)
    {
        if (draft is null)
            throw new ArgumentNullException(nameof(draft));
        draft.Validate();

        var root = LoadRoot();
        var launcherOptions = EnsureObject(root, "LauncherOptions");
        var runtimePythons = EnsureObject(launcherOptions, "RuntimePythons");
        runtimePythons[draft.NormalizedRuntimeKey] = draft.ToRuntimePythonValue();

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = $"{_path}.tmp";
        File.WriteAllText(tempPath, root.ToJsonString(WriteOptions));
        if (File.Exists(_path))
        {
            File.Copy(tempPath, _path, overwrite: true);
            File.Delete(tempPath);
        }
        else
        {
            File.Move(tempPath, _path);
        }
    }

    private JsonObject LoadRoot()
    {
        if (!File.Exists(_path) || new FileInfo(_path).Length == 0)
            return new JsonObject();

        var node = JsonNode.Parse(File.ReadAllText(_path));
        return node as JsonObject
            ?? throw new InvalidOperationException("appsettings.local.json root must be a JSON object.");
    }

    private static JsonObject EnsureObject(JsonObject parent, string name)
    {
        if (parent[name] is JsonObject existing)
            return existing;

        var created = new JsonObject();
        parent[name] = created;
        return created;
    }
}
