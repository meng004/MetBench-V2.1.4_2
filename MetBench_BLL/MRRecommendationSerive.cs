using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    public class MRRecommendationSerive
    {
        private IPreprocessor _preprocessor;
        private ISyntaxSimilarityDetector _syntaxDetector;
        private ISemanticSimilarityDetector _semanticDetector;
        private SimilarityResultFilter _filter;
        private SupportRateCalculator _supportRateCalculator;
        private SyntaxMRRecommendation syntaxMRRecommendation;
        private SemanticMRRecommendation semanticMRRecommendation;

        public MRRecommendationSerive(
       IPreprocessor preprocessor,
       ISyntaxSimilarityDetector syntaxDetector,
       ISemanticSimilarityDetector semanticDetector,
       SimilarityResultFilter filter,
       SupportRateCalculator supportRateCalculator)
        {
            _preprocessor = preprocessor;
            _syntaxDetector = syntaxDetector;
            _semanticDetector = semanticDetector;
            _filter = filter;
            _supportRateCalculator = supportRateCalculator;
            syntaxMRRecommendation = CreateSyntaxRecommender();
            semanticMRRecommendation = CreateSemanticRecommender();
        }

        private SyntaxMRRecommendation CreateSyntaxRecommender()
        {
            return new SyntaxMRRecommendation(
                _preprocessor,
                _syntaxDetector,
                _filter,
                _supportRateCalculator);
        }

        private SemanticMRRecommendation CreateSemanticRecommender()
        {
            return new SemanticMRRecommendation(
                _preprocessor,
                _semanticDetector,
                _filter,
                _supportRateCalculator);
        }

        public async Task<ObservableCollection< MRRecommendationResult>> SyntaxMRRRecommend(TargetProgram targetProgram, ObservableCollection<Application> candidatePrograms)
        {
            if (targetProgram==null||candidatePrograms==null)
            {
                return null;
            }
            var results = await Task.Run(() =>
           syntaxMRRecommendation.Recommend(targetProgram, candidatePrograms))
           .ConfigureAwait(false);
            return results;
        }
        public async Task<ObservableCollection<MRRecommendationResult>> SemanticMRRRecommend(TargetProgram targetProgram, ObservableCollection<Application> candidatePrograms)
        {
            if (targetProgram == null || candidatePrograms == null)
            {
                return null;
            }
            var results = await Task.Run(() =>
           semanticMRRecommendation.Recommend(targetProgram, candidatePrograms))
           .ConfigureAwait(false);
            return results;
        }
    }
}
