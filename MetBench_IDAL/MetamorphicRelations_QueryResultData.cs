using MetBench_Domain;

namespace MetBench_IDAL
{
    //蜕变关系查询结果类
    public class MetamorphicRelations_QueryResultData
    {
        public int IdMR { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string Constraint { get; set; } = string.Empty;
        public string OrderOfMR { get; set; } = string.Empty;
        //public HierarchyType HierarchyType { get; set; }
        public string Granularity { get; set; } = string.Empty;
        public string Hierarchy { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;

        public string InputPattern { get; set; } = string.Empty;
        public string OutputPattern { get; set; } = string.Empty;
        public string DimensionOfInputPattern { get; set; } = string.Empty;
        public string DimensionOfOutputPattern { get; set; } = string.Empty;
        #region
        //InputPatternimage属性
        public byte[] InputPatternImageData { get; set; } = Array.Empty<byte>();
        //OutputPatternimage属性
        public byte[] OutputPatternImageData { get; set; } = Array.Empty<byte>();
        //InputPatternimagepath属性
        public string InputPatternImagepath { get; set; } = string.Empty;
        //OutputPatternimagepath属性
        public string OutputPatternImagepath { get; set; } = string.Empty;
        #endregion
        public string ApplicationName { get; set; } = string.Empty;
        public string CodeName { get; set; } = string.Empty; //Code的Name
        public string DomainName { get; set; } = string.Empty;

        /// <summary>
        /// MRBinding-derived 状态聚合（F19 / PR-VM-6）。取值: active / deprecated / archived / experimental。
        /// 默认 "active" — 该 MR 无 binding 行 / VM 未注入 IMRBindingRepository 时回退为 active。
        /// 聚合规则: 任一 binding active → "active"；否则取第一个 binding 的 Status；都没 binding → "active"。
        /// </summary>
        public string Status { get; set; } = "active";
    }
}
