using System.Reflection;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

/// <summary>
/// §6 守护：<see cref="ISystemMtJobService"/> 公开签名不得泄漏引擎内部类型。
/// 只允许 primitives / Guid / 本命名空间 DTO / 既有 facade <see cref="MrRunResult"/>。
/// 泄漏 <c>SystemMtTask</c> / <c>PipelineContext</c> / <c>IMrAssertion</c> / <c>SystemMtResult</c> 等即红。
/// </summary>
public class JobFacadeTypeLeakageTests
{
    private static readonly HashSet<Type> Allowed = new()
    {
        typeof(void), typeof(Task), typeof(string), typeof(Guid), typeof(bool), typeof(int),
        typeof(CancellationToken),
        typeof(SystemMtJobRequest), typeof(SystemMtJobHandle),
        typeof(SystemMtJobStatus), typeof(SystemMtJobState),
        typeof(MrRunResult),
    };

    [Fact]
    public void ISystemMtJobService_does_not_leak_engine_internal_types()
    {
        foreach (var m in typeof(ISystemMtJobService).GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            AssertAllowed(m.ReturnType, $"{m.Name} return");
            foreach (var p in m.GetParameters())
                AssertAllowed(p.ParameterType, $"{m.Name} param {p.Name}");
        }
    }

    /// <summary>
    /// All leaf types of a (possibly generic) type. Recurses through EVERY generic argument —
    /// including the KEY of a dictionary — so a disallowed type in any position is caught,
    /// not just the last argument.
    /// </summary>
    private static IEnumerable<Type> LeafTypes(Type t)
    {
        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            if (def == typeof(Task<>) || def == typeof(Nullable<>) ||
                def == typeof(IReadOnlyDictionary<,>) || def == typeof(IReadOnlyList<>) ||
                def == typeof(IEnumerable<>) || def == typeof(IReadOnlyCollection<>))
                return t.GetGenericArguments().SelectMany(LeafTypes);
        }
        return new[] { t };
    }

    private static void AssertAllowed(Type type, string where)
    {
        foreach (var leaf in LeafTypes(type))
            Assert.True(Allowed.Contains(leaf),
                $"{where} exposes disallowed type {leaf.FullName} through job facade (CLAUDE.md §6).");
    }
}
