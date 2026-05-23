using MetBench_Domain.V2;
using MetBench_Domain.V2.Enums;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>
/// V3 MR 5D-tag schema 仓库接口。继承泛型 Guid PK 仓库，再加 5D 标签维度上的过滤
/// 查询便于 Coverage / 报表系统按维度统计。
/// </summary>
/// <remarks>
/// V3 是与 v1/v2 <see cref="IMetamorphicRelationRepository"/> 并存的索引视图；
/// MR 业务唯一键是 <see cref="MetamorphicRelationV3.MrCode"/>。
/// </remarks>
public interface IMetamorphicRelationV3Repository : IGuidRepository<MetamorphicRelationV3>
{
    /// <summary>按 MrCode 业务键查（找不到返回 null）。</summary>
    MetamorphicRelationV3? GetByCode(string mrCode);

    /// <summary>按方程维度过滤。</summary>
    ObservableCollection<MetamorphicRelationV3> GetByEquation(EquationKind equation);

    /// <summary>按元模式维度过滤。</summary>
    ObservableCollection<MetamorphicRelationV3> GetByMetaPattern(MetaPatternKind pattern);

    /// <summary>按 (方程, 元模式) 联合维度过滤。</summary>
    ObservableCollection<MetamorphicRelationV3> GetByEquationAndPattern(
        EquationKind equation, MetaPatternKind pattern);
}
