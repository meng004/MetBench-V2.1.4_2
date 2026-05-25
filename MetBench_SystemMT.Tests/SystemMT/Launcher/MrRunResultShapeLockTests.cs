using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// Locks the launcher facade DTO surface as part of ExecutionEvidence v2
/// (§9.1 of the design). PR-C0 adds a typed verification persistence block,
/// but the launcher's <see cref="MrRunResult"/> must not gain any new field
/// — typed engine internals stay inside the typed catalog and evidence.
///
/// If a future change is intentional, the golden list below must be updated
/// in the same PR that changes the shape, and the CLAUDE.md §6 launcher
/// type-leakage rule must be re-evaluated.
/// </summary>
public sealed class MrRunResultShapeLockTests
{
    private static readonly (string Name, string Type)[] Expected = new (string, string)[]
    {
        ("RecordId", "System.String"),
        ("MrId", "System.String"),
        ("Passed", "System.Boolean"),
        ("FailureReason", "System.String"),
        ("ValueName", "System.String"),
        ("SourceValue", "System.Double"),
        ("FollowUpValue", "System.Double"),
        ("SourceElapsed", "System.TimeSpan"),
        ("FollowUpElapsed", "System.TimeSpan"),
    };

    [Fact]
    public void MrRunResult_public_properties_match_locked_shape()
    {
        var actual = typeof(MrRunResult)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.DeclaringType == typeof(MrRunResult)
                || p.DeclaringType?.Namespace == typeof(MrRunResult).Namespace)
            // Records expose a synthesized EqualityContract — exclude it from the shape lock.
            .Where(p => p.Name != "EqualityContract")
            .Select(p => (p.Name, p.PropertyType.FullName ?? p.PropertyType.Name))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        var expected = Expected
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .Select(t => (t.Name, t.Type))
            .ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    [Fact]
    public void MrRunResult_remains_a_sealed_record()
    {
        Assert.True(typeof(MrRunResult).IsSealed,
            "MrRunResult must remain sealed so external code cannot subtype it across the facade.");
        // Records expose the synthesized <Clone>$ method; presence is the canonical record test.
        var clone = typeof(MrRunResult)
            .GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(clone);
    }
}
