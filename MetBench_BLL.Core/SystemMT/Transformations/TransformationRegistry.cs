using MetBench_BLL.SystemMT.Transformations.Math;

namespace MetBench_BLL.SystemMT.Transformations;

/// <summary>
/// IMRTransformation 注册表 — 按 Name 字符串分派到具体实现。
/// </summary>
/// <remarks>
/// Pipeline 拿到 MetamorphicRelation.TransformationName 字符串后通过本类解析对应实现。
///
/// Day-1 内置 5 个单步变换：Identity / ScaleField / TranslateField /
/// PermuteIndices / MirrorAxis。CompositeTransform 由调用方按需构造，不在 Registry。
/// P1 新增 17 个 L0 数学基元（MathExp / … / Min）。
///
/// 设计上不用 DI 容器 — 这些变换无状态、无副作用、不依赖外部服务，
/// 简单静态字典分派最直接。如需扩展，调用 <see cref="Register"/> 或 <see cref="RegisterIfMissing"/>。
/// </remarks>
public static class TransformationRegistry
{
    private static readonly Dictionary<string, Func<IMRTransformation>> _factories = new()
    {
        // ── 既有变换 ──────────────────────────────────────────────────────
        ["Identity"]             = () => new IdentityTransform(),
        ["ScaleField"]           = () => new ScaleField(),
        ["TranslateField"]       = () => new TranslateField(),
        ["PermuteIndices"]       = () => new PermuteIndices(),
        ["MirrorAxis"]           = () => new MirrorAxis(),
        ["ScaleFuelAbsorption"]  = () => new ScaleFuelAbsorption(),
        ["RefineRayTracks"]     = () => new RefineRayTracks(),

        // ── P1 L0 数学基元 ─────────────────────────────────────────────────
        ["MathExp"]     = () => new MathExp(),
        ["MathLog"]     = () => new MathLog(),
        ["MathSin"]     = () => new MathSin(),
        ["MathCos"]     = () => new MathCos(),
        ["MathSqrt"]    = () => new MathSqrt(),
        ["MathAbs"]     = () => new MathAbs(),
        ["MathPow"]     = () => new MathPow(),
        ["MathAdd"]     = () => new MathAdd(),
        ["MathSub"]     = () => new MathSub(),
        ["MathMul"]     = () => new MathMul(),
        ["MathDiv"]     = () => new MathDiv(),
        ["MathLinComb"] = () => new MathLinComb(),
        ["Linspace"]    = () => new Linspace(),
        ["Sum"]         = () => new Sum(),
        ["Mean"]        = () => new Mean(),
        ["Max"]         = () => new Max(),
        ["Min"]         = () => new Min(),
    };

    /// <summary>取得名为 <paramref name="name"/> 的变换实例。</summary>
    /// <exception cref="KeyNotFoundException">未知名称。</exception>
    public static IMRTransformation Get(string name)
    {
        if (!_factories.TryGetValue(name, out var factory))
            throw new KeyNotFoundException(
                $"Unknown transformation '{name}'. Registered: [{string.Join(", ", _factories.Keys)}]");
        return factory();
    }

    /// <summary>注册新变换（重复 Name 抛 ArgumentException）。</summary>
    public static void Register(string name, Func<IMRTransformation> factory)
    {
        if (_factories.ContainsKey(name))
            throw new ArgumentException($"Transformation '{name}' already registered");
        _factories[name] = factory;
    }

    /// <summary>注册但允许覆盖（测试 / 实验用）。</summary>
    public static void RegisterIfMissing(string name, Func<IMRTransformation> factory)
    {
        if (!_factories.ContainsKey(name))
            _factories[name] = factory;
    }

    /// <summary>已注册的全部 Name 列表（UI 下拉框用）。</summary>
    public static IReadOnlyCollection<string> AvailableNames => _factories.Keys;
}
