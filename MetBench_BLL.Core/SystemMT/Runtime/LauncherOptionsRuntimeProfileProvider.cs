using System;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Runtime;

public sealed class LauncherOptionsRuntimeProfileProvider : IRuntimeProfileProvider
{
    private readonly LauncherOptions _options;

    public LauncherOptionsRuntimeProfileProvider(LauncherOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public RuntimeProfile GetProfile(string? runtimeKey)
    {
        var key = RuntimeProfile.NormalizeRuntimeKey(runtimeKey);
        var executable = _options.ResolvePythonExecutable(key);
        if (executable.StartsWith("docker-mcp://", StringComparison.OrdinalIgnoreCase))
            return CreateDockerProfile(key, executable);

        return new RuntimeProfile(
            key,
            DisplayNameFor(key),
            KindFor(key),
            executable);
    }

    private static RuntimeProfile CreateDockerProfile(string runtimeKey, string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            throw InvalidDockerRuntime(runtimeKey, "uri");
        if (!string.Equals(uri.Scheme, "docker-mcp", StringComparison.OrdinalIgnoreCase))
            throw InvalidDockerRuntime(runtimeKey, "scheme");
        if (string.IsNullOrWhiteSpace(uri.Host)
            || !string.Equals(RuntimeProfile.NormalizeRuntimeKey(uri.Host), runtimeKey, StringComparison.Ordinal))
        {
            throw InvalidDockerRuntime(runtimeKey, "runtime key");
        }

        Dictionary<string, string> query;
        try
        {
            query = ParseQuery(uri.Query);
        }
        catch (UriFormatException ex)
        {
            throw new RuntimeEnvironmentResolutionException(
                $"Docker MCP runtime for key '{runtimeKey}' has an invalid or missing 'endpoint' field: {ex.Message}");
        }
        var image = RequiredQueryValue(runtimeKey, query, "image");
        var python = RequiredQueryValue(runtimeKey, query, "python");
        var endpoint = RequiredQueryValue(runtimeKey, query, "endpoint");
        var tool = RequiredQueryValue(runtimeKey, query, "tool");
        var local = RequiredQueryValue(runtimeKey, query, "local");
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri)
            || (endpointUri.Scheme != Uri.UriSchemeHttp && endpointUri.Scheme != Uri.UriSchemeHttps))
        {
            throw InvalidDockerRuntime(runtimeKey, "endpoint");
        }

        query.TryGetValue("authTokenEnv", out var authTokenEnv);
        authTokenEnv = string.IsNullOrWhiteSpace(authTokenEnv) ? null : authTokenEnv;

        query.TryGetValue("localPython", out var localPython);
        localPython = string.IsNullOrWhiteSpace(localPython) ? null : localPython;

        var pathStyle = DockerMcpPathStyle.None;
        if (query.TryGetValue("pathStyle", out var pathStyleRaw))
        {
            if (!string.Equals(pathStyleRaw, "wsl", StringComparison.OrdinalIgnoreCase))
                throw InvalidDockerRuntime(runtimeKey, "pathStyle");
            pathStyle = DockerMcpPathStyle.Wsl;
        }

        return new RuntimeProfile(
            runtimeKey,
            $"{runtimeKey} Docker MCP",
            RuntimeKind.Docker,
            local,
            dockerMcp: new DockerMcpRuntimeOptions(
                endpoint, image, python, authTokenEnv, localPython, pathStyle, tool, local));
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trimmed = query.StartsWith("?", StringComparison.Ordinal) ? query[1..] : query;
        if (string.IsNullOrWhiteSpace(trimmed))
            return result;

        foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            string key;
            string value;
            key = Uri.UnescapeDataString(pieces[0]);
            value = pieces.Length == 2 ? Uri.UnescapeDataString(pieces[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }

    private static string RequiredQueryValue(
        string runtimeKey,
        IReadOnlyDictionary<string, string> query,
        string name)
    {
        if (!query.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
            throw InvalidDockerRuntime(runtimeKey, name);
        return value;
    }

    private static RuntimeEnvironmentResolutionException InvalidDockerRuntime(
        string runtimeKey,
        string field) =>
        new($"Docker MCP runtime for key '{runtimeKey}' has an invalid or missing '{field}' field.");

    private static RuntimeKind KindFor(string runtimeKey) =>
        string.Equals(runtimeKey, PythonExecutableKinds.OpenMoc, StringComparison.OrdinalIgnoreCase)
        || string.Equals(runtimeKey, PythonExecutableKinds.OpenMc, StringComparison.OrdinalIgnoreCase)
        || string.Equals(runtimeKey, PythonExecutableKinds.Scipy, StringComparison.OrdinalIgnoreCase)
            ? RuntimeKind.PythonVirtualEnvironment
            : RuntimeKind.LocalPython;

    private static string DisplayNameFor(string runtimeKey)
    {
        if (string.Equals(runtimeKey, PythonExecutableKinds.System, StringComparison.OrdinalIgnoreCase))
            return "System Python";
        if (string.Equals(runtimeKey, PythonExecutableKinds.OpenMoc, StringComparison.OrdinalIgnoreCase))
            return "OpenMOC Python";
        if (string.Equals(runtimeKey, PythonExecutableKinds.OpenMc, StringComparison.OrdinalIgnoreCase))
            return "OpenMC Python";
        if (string.Equals(runtimeKey, PythonExecutableKinds.Scipy, StringComparison.OrdinalIgnoreCase))
            return "SciPy Python";

        return $"{runtimeKey} Python";
    }
}
