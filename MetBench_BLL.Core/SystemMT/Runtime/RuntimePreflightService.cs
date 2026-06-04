using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Pipeline;

namespace MetBench_BLL.SystemMT.Runtime;

public sealed class RuntimePreflightService : IRuntimePreflightService
{
    private const string VersionCheckKind = "version";
    private const string DependencyCheckKind = "dependency";
    private const string EnvironmentCheckKind = "environment";
    private const string ExecutableCheckKind = "executable";
    private const string StartupCheckKind = "startup";
    private const string MiddlewareCheckKind = "middleware";

    private readonly IProcessExecutor _processExecutor;

    public RuntimePreflightService(IProcessExecutor processExecutor)
    {
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
    }

    public async Task<RuntimePreflightResult> CheckAsync(
        RuntimeProfile profile,
        CancellationToken cancellationToken = default)
    {
        if (profile is null)
            throw new ArgumentNullException(nameof(profile));

        var diagnostics = new List<RuntimePreflightDiagnostic>();

        if (!profile.IsExecutableInV1)
        {
            var detail = $"Runtime kind {profile.Kind} is a v1 placeholder and has no execution backend.";
            diagnostics.Add(new RuntimePreflightDiagnostic(
                MiddlewareCheckKind,
                profile.Kind.ToString(),
                false,
                RuntimeFailureKind.MiddlewareUnavailable,
                detail));
            return RuntimePreflightResult.Blocked(
                profile,
                RuntimeFailureKind.MiddlewareUnavailable,
                detail,
                diagnostics);
        }

        if (string.IsNullOrWhiteSpace(profile.ExecutablePath)
            || IsConfiguredFilePathMissing(profile.ExecutablePath))
        {
            var detail = $"Runtime executable path is missing for runtime '{profile.RuntimeKey}'.";
            diagnostics.Add(new RuntimePreflightDiagnostic(
                ExecutableCheckKind,
                profile.RuntimeKey,
                false,
                RuntimeFailureKind.RuntimeExecutableMissing,
                detail));
            return RuntimePreflightResult.Blocked(
                profile,
                RuntimeFailureKind.RuntimeExecutableMissing,
                detail,
                diagnostics);
        }

        foreach (var variableName in profile.RequiredEnvironmentVariables)
        {
            if (string.IsNullOrWhiteSpace(variableName)
                || string.IsNullOrEmpty(Environment.GetEnvironmentVariable(variableName)))
            {
                var detail = $"Required environment variable '{variableName}' is missing.";
                diagnostics.Add(new RuntimePreflightDiagnostic(
                    EnvironmentCheckKind,
                    variableName,
                    false,
                    RuntimeFailureKind.PreflightFailed,
                    detail));
                return RuntimePreflightResult.Blocked(
                    profile,
                    RuntimeFailureKind.PreflightFailed,
                    detail,
                    diagnostics);
            }

            diagnostics.Add(new RuntimePreflightDiagnostic(
                EnvironmentCheckKind,
                variableName,
                true,
                RuntimeFailureKind.None,
                "Present."));
        }

        var startupCommand = $"{Quote(profile.ExecutablePath!)} --version";
        var startupResult = await _processExecutor.RunAsync(
            startupCommand,
            Environment.CurrentDirectory,
            TimeoutSeconds(profile.Timeout),
            cancellationToken).ConfigureAwait(false);
        var startupDetail = BuildProcessDetail(profile.DisplayName, startupResult);
        if (startupResult.TimedOut)
        {
            diagnostics.Add(ProcessDiagnostic(StartupCheckKind, profile.DisplayName, false, RuntimeFailureKind.Timeout, startupDetail, startupResult));
            return RuntimePreflightResult.Blocked(profile, RuntimeFailureKind.Timeout, startupDetail, diagnostics);
        }

        if (startupResult.ExitCode != 0)
        {
            var detail = $"Runtime executable '{profile.ExecutablePath}' could not be started. {startupDetail}";
            diagnostics.Add(ProcessDiagnostic(StartupCheckKind, profile.DisplayName, false, RuntimeFailureKind.RuntimeExecutableMissing, detail, startupResult));
            return RuntimePreflightResult.Blocked(profile, RuntimeFailureKind.RuntimeExecutableMissing, detail, diagnostics);
        }

        diagnostics.Add(ProcessDiagnostic(StartupCheckKind, profile.DisplayName, true, RuntimeFailureKind.None, startupDetail, startupResult));

        foreach (var check in profile.VersionChecks)
        {
            if (!TryBuildVersionCommand(profile, check, out var command, out var invalidCommandDetail))
            {
                diagnostics.Add(new RuntimePreflightDiagnostic(
                    VersionCheckKind,
                    check.Name,
                    false,
                    RuntimeFailureKind.PreflightFailed,
                    invalidCommandDetail));
                return RuntimePreflightResult.Blocked(
                    profile,
                    RuntimeFailureKind.PreflightFailed,
                    invalidCommandDetail,
                    diagnostics);
            }

            var result = await _processExecutor.RunAsync(
                command,
                Environment.CurrentDirectory,
                TimeoutSeconds(check.Timeout ?? profile.Timeout),
                cancellationToken).ConfigureAwait(false);

            var detail = BuildProcessDetail(check.Name, result);
            if (result.TimedOut)
            {
                diagnostics.Add(ProcessDiagnostic(VersionCheckKind, check.Name, false, RuntimeFailureKind.Timeout, detail, result));
                return RuntimePreflightResult.Blocked(profile, RuntimeFailureKind.Timeout, detail, diagnostics);
            }

            if (result.ExitCode != 0)
            {
                diagnostics.Add(ProcessDiagnostic(VersionCheckKind, check.Name, false, RuntimeFailureKind.PreflightFailed, detail, result));
                return RuntimePreflightResult.Blocked(profile, RuntimeFailureKind.PreflightFailed, detail, diagnostics);
            }

            diagnostics.Add(ProcessDiagnostic(VersionCheckKind, check.Name, true, RuntimeFailureKind.None, detail, result));
        }

        foreach (var check in profile.DependencyChecks)
        {
            if (!IsValidPythonImportName(check.ImportName))
            {
                var invalidImportDetail = $"Dependency check '{check.Name}' has an invalid Python import name.";
                diagnostics.Add(new RuntimePreflightDiagnostic(
                    DependencyCheckKind,
                    check.Name,
                    false,
                    RuntimeFailureKind.PreflightFailed,
                    invalidImportDetail));
                return RuntimePreflightResult.Blocked(
                    profile,
                    RuntimeFailureKind.PreflightFailed,
                    invalidImportDetail,
                    diagnostics);
            }

            var result = await _processExecutor.RunAsync(
                BuildImportCommand(profile, check),
                Environment.CurrentDirectory,
                TimeoutSeconds(profile.Timeout),
                cancellationToken).ConfigureAwait(false);

            var detail = BuildProcessDetail(check.Name, result);
            if (result.TimedOut)
            {
                diagnostics.Add(ProcessDiagnostic(DependencyCheckKind, check.Name, false, RuntimeFailureKind.Timeout, detail, result));
                return RuntimePreflightResult.Blocked(profile, RuntimeFailureKind.Timeout, detail, diagnostics);
            }

            if (result.ExitCode != 0)
            {
                var failureKind = check.Required
                    ? RuntimeFailureKind.DependencyMissing
                    : RuntimeFailureKind.DependencyMissing;
                diagnostics.Add(ProcessDiagnostic(
                    DependencyCheckKind,
                    check.Name,
                    false,
                    failureKind,
                    detail,
                    result,
                    blocking: check.Required));

                if (check.Required)
                    return RuntimePreflightResult.Blocked(profile, RuntimeFailureKind.DependencyMissing, detail, diagnostics);
            }
            else
            {
                diagnostics.Add(ProcessDiagnostic(DependencyCheckKind, check.Name, true, RuntimeFailureKind.None, detail, result));
            }
        }

        return RuntimePreflightResult.Pass(
            profile,
            $"Runtime preflight passed for runtime '{profile.RuntimeKey}' with {diagnostics.Count} check(s).",
            diagnostics);
    }

    private static bool IsConfiguredFilePathMissing(string executablePath)
    {
        if (!Path.IsPathFullyQualified(executablePath))
            return false;

        return !File.Exists(executablePath);
    }

    private static bool TryBuildVersionCommand(
        RuntimeProfile profile,
        RuntimeVersionCheck check,
        out string command,
        out string invalidDetail)
    {
        command = string.Empty;
        invalidDetail = string.Empty;

        var executable = string.IsNullOrWhiteSpace(check.Command)
            ? profile.ExecutablePath!
            : check.Command;

        if (!IsSafeShellExecutable(executable))
        {
            invalidDetail = $"Version check '{check.Name}' has an unsafe executable command.";
            return false;
        }
        if (!IsSafeShellArguments(check.Arguments))
        {
            invalidDetail = $"Version check '{check.Name}' has unsafe command arguments.";
            return false;
        }

        command = string.IsNullOrWhiteSpace(check.Arguments)
            ? Quote(executable)
            : $"{Quote(executable)} {check.Arguments}";
        return true;
    }

    private static string BuildImportCommand(RuntimeProfile profile, RuntimeDependencyCheck check) =>
        $"{Quote(profile.ExecutablePath!)} -c \"import {check.ImportName}\"";

    private static int TimeoutSeconds(TimeSpan timeout) =>
        Math.Max(1, (int)Math.Ceiling(timeout.TotalSeconds));

    private static RuntimePreflightDiagnostic ProcessDiagnostic(
        string checkKind,
        string name,
        bool passed,
        RuntimeFailureKind failureKind,
        string detail,
        ProcessResult result,
        bool blocking = true) =>
        new(
            checkKind,
            name,
            passed,
            failureKind,
            detail,
            blocking,
            result.ExitCode,
            result.TimedOut,
            result.Stdout ?? string.Empty,
            result.Stderr ?? string.Empty);

    private static string BuildProcessDetail(string checkName, ProcessResult result)
    {
        if (result.TimedOut)
            return $"{checkName} timed out. {OutputSummary(result)}";

        if (result.ExitCode != 0)
            return $"{checkName} failed with exit code {result.ExitCode}. {OutputSummary(result)}";

        return $"{checkName} passed. {OutputSummary(result)}";
    }

    private static string OutputSummary(ProcessResult result)
    {
        var stdout = Trim(result.Stdout);
        var stderr = Trim(result.Stderr);

        if (stdout.Length == 0 && stderr.Length == 0)
            return string.Empty;
        if (stderr.Length == 0)
            return $"stdout: {stdout}";
        if (stdout.Length == 0)
            return $"stderr: {stderr}";

        return $"stdout: {stdout}; stderr: {stderr}";
    }

    private static string Trim(string? value)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= 500
            ? text
            : text[..500];
    }

    private static string Quote(string value)
    {
        if (value.StartsWith("\"", StringComparison.Ordinal) && value.EndsWith("\"", StringComparison.Ordinal))
            return value;

        return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    private static bool IsSafeShellExecutable(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.IndexOfAny(new[] { '"', '&', '|', ';', '<', '>', '`', '\r', '\n' }) < 0;
    }

    private static bool IsSafeShellArguments(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch)
                || ch is '-' or '_' or '.' or ':' or '/' or '\\' or '=' or '+' or ' ')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool IsValidPythonImportName(string importName)
    {
        if (string.IsNullOrWhiteSpace(importName))
            return false;

        foreach (var part in importName.Split('.'))
        {
            if (!IsValidPythonIdentifier(part))
                return false;
        }

        return true;
    }

    private static bool IsValidPythonIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        if (!(char.IsLetter(value[0]) || value[0] == '_'))
            return false;

        for (var i = 1; i < value.Length; i++)
        {
            if (!(char.IsLetterOrDigit(value[i]) || value[i] == '_'))
                return false;
        }

        return true;
    }
}
