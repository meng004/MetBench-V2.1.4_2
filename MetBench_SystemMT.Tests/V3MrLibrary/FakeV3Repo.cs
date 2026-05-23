using MetBench_Domain.V2;
using MetBench_Domain.V2.Enums;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_SystemMT.Tests.V3MrLibrary;

/// <summary>In-memory fake of <see cref="IMetamorphicRelationV3Repository"/> for migration tests.</summary>
internal sealed class FakeV3Repo : IMetamorphicRelationV3Repository
{
    public List<MetamorphicRelationV3> Data { get; } = new();

    public ObservableCollection<MetamorphicRelationV3> GetAll() => new(Data);

    public MetamorphicRelationV3? Get(Guid id) => Data.FirstOrDefault(m => m.IdV3 == id);

    public ObservableCollection<MetamorphicRelationV3> Get(MetamorphicRelationV3 template) => new(Data);

    public bool Add(MetamorphicRelationV3 entity)
    {
        Data.Add(entity);
        return true;
    }

    public bool Modify(MetamorphicRelationV3 entity)
    {
        var idx = Data.FindIndex(m => m.IdV3 == entity.IdV3);
        if (idx < 0) return false;
        Data[idx] = entity;
        return true;
    }

    public bool Remove(MetamorphicRelationV3 entity) => Data.Remove(entity);

    public ObservableCollection<MetamorphicRelationV3> GetPage(int pageIndex, int pageSize)
        => new(Data.Skip(pageIndex * pageSize).Take(pageSize).ToList());

    public int Count() => Data.Count;

    public MetamorphicRelationV3? GetByCode(string mrCode) =>
        Data.FirstOrDefault(m => m.MrCode == mrCode);

    public ObservableCollection<MetamorphicRelationV3> GetByEquation(EquationKind equation)
        => new(Data.Where(m => m.Equation == equation).OrderBy(m => m.MrCode).ToList());

    public ObservableCollection<MetamorphicRelationV3> GetByMetaPattern(MetaPatternKind pattern)
        => new(Data.Where(m => m.MetaPattern == pattern).OrderBy(m => m.MrCode).ToList());

    public ObservableCollection<MetamorphicRelationV3> GetByEquationAndPattern(
        EquationKind equation, MetaPatternKind pattern)
        => new(Data.Where(m => m.Equation == equation && m.MetaPattern == pattern)
            .OrderBy(m => m.MrCode).ToList());
}
