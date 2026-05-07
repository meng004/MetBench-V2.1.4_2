using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    public abstract class MRRecommendationBase:IMRRecommendation
    {
        protected  IPreprocessor _preprocessor;
        protected  ISimilarityDetector _similarityDetector;
        protected  SimilarityResultFilter _filterStrategy;
        protected SupportRateCalculator _supportRateCalculator;

        protected MRRecommendationBase(
       IPreprocessor preprocessor,
       ISimilarityDetector similarityDetector,
       SimilarityResultFilter filterStrategy,
       SupportRateCalculator supportRateCalculator)
        {
            _preprocessor = preprocessor;
            _similarityDetector = similarityDetector;
            _filterStrategy = filterStrategy;
            _supportRateCalculator = supportRateCalculator;
        }
        public virtual ObservableCollection<MRRecommendationResult> Recommend(TargetProgram targetprogram, ObservableCollection<Application> candidateProgrms)
        {
            // 预处理
           
            // 计算相似度

            // 筛选结果


            // 计算支持率并生成推荐结果
            return null;
        }


    }
}
