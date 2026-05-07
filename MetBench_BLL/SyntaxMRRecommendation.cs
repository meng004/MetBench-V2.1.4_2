using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    // 语法相似推荐
    public class SyntaxMRRecommendation : MRRecommendationBase
    {

        public SyntaxMRRecommendation(IPreprocessor preprocessor, ISyntaxSimilarityDetector similarityDetector, SimilarityResultFilter filter, SupportRateCalculator supportRateCalculator) : base(preprocessor, similarityDetector, filter, supportRateCalculator)
        {
        }

        public override ObservableCollection<MRRecommendationResult> Recommend(TargetProgram targetprogram, ObservableCollection<Application> candidateProgrms)
        {
            // 预处理
           var processResultsDic =  _preprocessor.Process(targetprogram, candidateProgrms);
           
            
            if (!(processResultsDic.TryGetValue("TargetProgram", out object targetProgramObj)))
            {
                
                    return null;
            }
            if (!(processResultsDic.TryGetValue("CandidateProgram", out object candidateProgramsObj)))
            {

                return null;
            }
            // 预处理后的目标程序
            var program = targetProgramObj as FunctionProgram;
            if (program == null)
            {
                return null;
            }
            // 预处理后的候选程序
            var newcandidatePrograms = candidateProgramsObj as ObservableCollection<Application>;
            if (newcandidatePrograms == null)
            {
                return null;
            }
            // 计算相似度
            var similarityResults = _similarityDetector.CalculateSimilarity(program, newcandidatePrograms);
            if (similarityResults==null) 
            {
                return null;
            }
            // 筛选结果
            var newsimilarityResults = _filterStrategy.Filter(similarityResults);
            if (newsimilarityResults == null)
            {
                return null;
            }
            // 计算支持率并生成推荐结果
            //var results = _supportRateCalculator.CalculateSupportRate(newsimilarityResults);
            return Task.Run(() => _supportRateCalculator.CalculateSupportRate(newsimilarityResults)).GetAwaiter().GetResult();
        }
    }
}
