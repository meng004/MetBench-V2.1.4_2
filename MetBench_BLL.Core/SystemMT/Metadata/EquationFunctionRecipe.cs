namespace MetBench_BLL.SystemMT.Metadata;

/// <summary>
/// L1 数据驱动方程函数的持久化形态 —— 一行 = 一个用户可加的方程级组合函数。
/// 按 <c>(EquationKey, FunctionName)</c> 唯一。
/// </summary>
/// <remarks>
/// 设计见 docs/design/mr-architecture.md §5.1(L1 行)与 §5.2(Recipe 受控表达力)。
///
/// Recipe 是 L0 通用原子算子的有序组合 + 参数占位绑定;入库时 catalog service 校验:
///   • Recipe 引用的每个 <c>op</c> 必须在 <c>TransformationRegistry</c> 中存在
///   • 占位语法只允许 <c>{name}</c>;<c>name</c> 必须在 <see cref="ParameterSchemaJson"/> 中声明
///   • 参数 schema 必须完整(name / type / required / constraint)
///
/// Recipe 本身不引入 DSL / 反射 / IO / 任意表达式求值,所以"代码在数据"风险被严格
/// 约束在「受控原子算子组合」面上(见 §5.1)。
/// </remarks>
public sealed class EquationFunctionRecipe
{
    /// <summary>LiteDB document identity;repository 在首次 insert 时分配新 Guid。</summary>
    public Guid Id { get; set; }

    /// <summary>所属方程的稳定业务键(参见 <see cref="EquationMetadata.EquationKey"/>)。</summary>
    public string EquationKey { get; set; } = string.Empty;

    /// <summary>方程命名空间内的函数名,同 <see cref="EquationFunctionDescriptor.Name"/>。</summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// 序列化的 Recipe(<c>{ "compose": [...] }</c> 结构),由
    /// <c>RecipeBasedEquationFunction</c>(P2)解析执行。
    /// </summary>
    public string RecipeJson { get; set; } = string.Empty;

    /// <summary>
    /// 序列化的参数 schema(数组形式,每项 <c>{name, type, required, constraint?}</c>),
    /// 运行时按 schema 校验用户传入参数。
    /// </summary>
    public string ParameterSchemaJson { get; set; } = string.Empty;

    /// <summary>记录创建时间(UTC)。</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>创建者标识(用户名 / 服务名)。</summary>
    public string CreatedBy { get; set; } = string.Empty;
}
