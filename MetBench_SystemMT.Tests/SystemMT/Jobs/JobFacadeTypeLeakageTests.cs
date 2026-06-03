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
            AssertAllowed(Unwrap(m.ReturnType), $"{m.Name} return");
            foreach (var p in m.GetParameters())
                AssertAllowed(Unwrap(p.ParameterType), $"{m.Name} param {p.Name}");
        }
    }

    private static Type Unwrap(Type t)
    {
        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            if (def == typeof(Task<>) || def == typeof(Nullable<>) ||
                def == typeof(IReadOnlyDictionary<,>) || def == typeof(IReadOnlyList<>))
                return Unwrap(t.GetGenericArguments()[^1]);
        }
        return t;
    }

    private static void AssertAllowed(Type t, string where)
        => Assert.True(Allowed.Contains(t),
            $"{where} exposes disallowed type {t.FullName} through job facade (CLAUDE.md §6).");
}
