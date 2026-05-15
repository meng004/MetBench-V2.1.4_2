using System.Collections.ObjectModel;
using MetBench_Domain;

namespace MetBench_IDAL;

/// <summary>
/// MetaPattern CRUD —— string PK (Code 字段)。
/// </summary>
/// <remarks>
/// 跟 v2 其他实体（int / Guid PK）不一样，MetaPattern 用稳定语义 code 当 PK
/// （"m_inv" / "m_mono" 等），避免 int → human-readable 间接层。
/// </remarks>
public interface IMetaPatternRepository
{
    /// <summary>列出全部 MetaPattern（含 out-of-scope）。</summary>
    ObservableCollection<MetaPattern> GetAll();

    /// <summary>按 Code 取一行。找不到返回 null。</summary>
    MetaPattern? Get(string code);

    /// <summary>按 Status 列出（active / deprecated / experimental / out-of-scope）。</summary>
    ObservableCollection<MetaPattern> GetByStatus(string status);

    /// <summary>只列 active 的（最常用，scaffold 工具 / WPF dropdown 用）。</summary>
    ObservableCollection<MetaPattern> GetActive();

    /// <summary>按 Block 列出（NOETHER B1-B8）。</summary>
    ObservableCollection<MetaPattern> GetByBlock(string block);

    /// <summary>插入。Code 重复 → throw。</summary>
    bool Add(MetaPattern entity);

    /// <summary>更新。Code 不存在 → 返回 false。</summary>
    bool Modify(MetaPattern entity);

    /// <summary>删除。Code 不存在 → 返回 false。生产中应改 Status 而非删。</summary>
    bool Remove(MetaPattern entity);

    /// <summary>总行数。</summary>
    int Count();
}
