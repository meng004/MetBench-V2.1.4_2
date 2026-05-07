using MetBench_Domain;

namespace MetBench_BLL
{
    public class MRRecommendationVIsualResult
    {
        // 目标程序
        public FunctionProgram TargetProgram { get; set; }

        // 推荐的MR
        public MetamorphicRelation MetamorphicRelation { get; set; }

        // r图片路径
        public string InputPatternImagepath { get; set; }
        // R图片路径
        public string OutputPatternImagepath { get; set; }
        // 支持率
        public decimal Value { get; set; }

        // 相似程序
        public Application Application { get; set; }

        // 相似度
        public float Similarity { get; set; }

        // 相似程序对应的应用领域名称集
        public string DomainNames { get; set; }

    }
}
