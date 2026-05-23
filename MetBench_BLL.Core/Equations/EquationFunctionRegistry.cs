namespace MetBench_BLL.Equations;

/// <summary>
/// 方程命名空间函数注册表，keyed by (EquationKey, Name)。
/// 与 <see cref="MetBench_BLL.SystemMT.Transformations.TransformationRegistry"/> 平级。
/// 线程安全：写入在程序启动 / catalog seeding 时单线程完成，读取后只读。
/// </summary>
public sealed class EquationFunctionRegistry
{
    private readonly Dictionary<(string eq, string name), IEquationFunction> _store = new();

    /// <summary>注册函数；(EquationKey, Name) 已存在则覆盖。</summary>
    public void Register(IEquationFunction fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        _store[(fn.EquationKey, fn.Name)] = fn;
    }

    /// <summary>按 (equationKey, name) 查询；未命中返回 false。</summary>
    public bool TryGet(string equationKey, string name, out IEquationFunction? fn)
    {
        if (_store.TryGetValue((equationKey, name), out var found))
        {
            fn = found;
            return true;
        }
        fn = null;
        return false;
    }

    /// <summary>列出指定方程下的全部函数（无序）。</summary>
    public IReadOnlyList<IEquationFunction> List(string equationKey)
        => _store.Values
            .Where(f => f.EquationKey == equationKey)
            .ToList();
}
