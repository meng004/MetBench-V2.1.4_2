using LiteDB;
using MetBench_Domain.V2;
using MetBench_Domain.V2.Enums;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

/// <summary>
/// V3 MR 5D-tag schema LiteDB 仓库实现。继承 Guid PK 基类，加 5D 标签维度的
/// 二级查询入口（直接走 LiteDB Query 索引）。
/// </summary>
public sealed class LiteDbMetamorphicRelationV3Repository
    : LiteDbGuidPkRepositoryBase<MetamorphicRelationV3>, IMetamorphicRelationV3Repository
{
    public LiteDbMetamorphicRelationV3Repository() : base() { }

    public LiteDbMetamorphicRelationV3Repository(DbConfig dbConfig, string conn)
        : base(dbConfig, conn) { }

    protected override string CollectionKey => "v3_metamorphic_relations";

    public MetamorphicRelationV3? GetByCode(string mrCode)
    {
        if (string.IsNullOrEmpty(mrCode)) return null;
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MetamorphicRelationV3>(CollectionKey);
        return col.FindOne(m => m.MrCode == mrCode);
    }

    public ObservableCollection<MetamorphicRelationV3> GetByEquation(EquationKind equation)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MetamorphicRelationV3>(CollectionKey);
        var rows = col.Query()
            .Where(m => m.Equation == equation)
            .OrderBy(m => m.MrCode)
            .ToList();
        return new ObservableCollection<MetamorphicRelationV3>(rows);
    }

    public ObservableCollection<MetamorphicRelationV3> GetByMetaPattern(MetaPatternKind pattern)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MetamorphicRelationV3>(CollectionKey);
        var rows = col.Query()
            .Where(m => m.MetaPattern == pattern)
            .OrderBy(m => m.MrCode)
            .ToList();
        return new ObservableCollection<MetamorphicRelationV3>(rows);
    }

    public ObservableCollection<MetamorphicRelationV3> GetByEquationAndPattern(
        EquationKind equation, MetaPatternKind pattern)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MetamorphicRelationV3>(CollectionKey);
        var rows = col.Query()
            .Where(m => m.Equation == equation && m.MetaPattern == pattern)
            .OrderBy(m => m.MrCode)
            .ToList();
        return new ObservableCollection<MetamorphicRelationV3>(rows);
    }
}
