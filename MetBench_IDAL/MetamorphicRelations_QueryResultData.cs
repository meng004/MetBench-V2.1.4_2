using MetBench_Domain;

namespace MetBench_IDAL
{
    //蜕变关系查询结果类
    public class MetamorphicRelations_QueryResultData
    {
        public int IdMR { get; set; }
        public string Description { get; set; }
        public string Context { get; set; }
        public string Constraint { get; set; }
        public string OrderOfMR { get; set; }
        //public HierarchyType HierarchyType { get; set; }
        public string Granularity { get; set; }
        public string Hierarchy { get; set; }
        public string Operator { get; set; }
        public string Expression { get; set; }

        public string InputPattern { get; set; }
        public string OutputPattern { get; set; }
        public string DimensionOfInputPattern { get; set; }
        public string DimensionOfOutputPattern { get; set; }
        #region
        //InputPatternimage属性
        public byte[] InputPatternImageData { get; set; }
        //OutputPatternimage属性
        public byte[] OutputPatternImageData { get; set; }
        //InputPatternimagepath属性
        public string InputPatternImagepath { get; set; }
        //OutputPatternimagepath属性
        public string OutputPatternImagepath { get; set; }
        #endregion
        public string ApplicationName { get; set; }
        public string CodeName { get; set; } //Code的Name
        public string DomainName { get; set; }
    }
}
