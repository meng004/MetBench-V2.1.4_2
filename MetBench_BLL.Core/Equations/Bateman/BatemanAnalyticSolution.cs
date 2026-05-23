using MetBench_BLL.SystemMT.ParameterMapping;
using SysMath = System.Math;

namespace MetBench_BLL.Equations.Bateman;

/// <summary>
/// L2 方程函数：Bateman 三核素衰变链 A→B→C 解析解（C 稳定）。
/// </summary>
/// <remarks>
/// 公式（λ_A ≠ λ_B，λ_C = 0 即 C 稳定）：
/// <code>
///   N_A(t) = N_A0 · e^{-λ_A·t}
///   N_B(t) = N_A0 · λ_A/(λ_B-λ_A) · (e^{-λ_A·t} - e^{-λ_B·t}) + N_B0·e^{-λ_B·t}
///   N_C(t) = total - N_A(t) - N_B(t)      (守恒，total = N_A0+N_B0+N_C0)
/// </code>
/// 仅使用 <c>System.Math.Exp</c>，不引入数值求解器（mr-architecture §5.2 L2 纪律）。
/// 输入字段路径对应 decay_chain SUT 的 JSON 格式（/initial/ + /params/）。
/// </remarks>
public sealed class BatemanAnalyticSolution : IEquationFunction
{
    private static readonly JsonPointerResolver Resolver = new();

    public string EquationKey => "bateman";
    public string Name => "AnalyticSolution";
    public string Kind => "solution";

    public Dictionary<string, object?> Execute(
        IReadOnlyDictionary<string, object?> sourceData,
        IReadOnlyDictionary<string, string> parameters)
    {
        var nA0 = GetDouble(sourceData, "/initial/N_A");
        var nB0 = GetDouble(sourceData, "/initial/N_B");
        var nC0 = GetDouble(sourceData, "/initial/N_C");
        var lA  = GetDouble(sourceData, "/params/lambda_A");
        var lB  = GetDouble(sourceData, "/params/lambda_B");
        var t   = GetDouble(sourceData, "/params/t_final");

        if (SysMath.Abs(lB - lA) < 1e-15)
            throw new InvalidOperationException(
                $"BatemanAnalyticSolution requires λ_A ≠ λ_B; got λ_A={lA}, λ_B={lB}");

        var nA = nA0 * SysMath.Exp(-lA * t);
        var nB = nA0 * lA / (lB - lA) * (SysMath.Exp(-lA * t) - SysMath.Exp(-lB * t))
                 + nB0 * SysMath.Exp(-lB * t);
        var nC = (nA0 + nB0 + nC0) - nA - nB;

        return new Dictionary<string, object?> { ["N_A"] = nA, ["N_B"] = nB, ["N_C"] = nC };
    }

    private static double GetDouble(IReadOnlyDictionary<string, object?> data, string path)
    {
        var v = Resolver.Get(data, path);
        return v switch
        {
            double d  => d,
            float f   => (double)f,
            int i     => (double)i,
            long l    => (double)l,
            decimal m => (double)m,
            null      => throw new InvalidOperationException($"Field '{path}' is null"),
            _         => throw new InvalidOperationException(
                             $"Field '{path}' is {v.GetType().Name}; expected numeric"),
        };
    }
}
