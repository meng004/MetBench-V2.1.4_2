using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    public class MRRecommendationResult
    {
        // 目标程序
        public TargetProgram TargetProgram { get; set; }

        // 推荐的MR及其支持率 集合
        public ObservableCollection<MRWithDecimal> MetamorphicRelations { get; set; }
        //// 推荐的MR集合
        //public ObservableCollection<MetamorphicRelation> MetamorphicRelations { get; set; }
        // 相似程序
        public Application Application { get; set; }
        // 相似度
        public float Similarity { get; set; }

    }
    public class MRWithDecimal
    {
        // MR
        public MetamorphicRelation Relation { get; set; }
        // 支持率
        public decimal Value { get; set; }
    }
}
