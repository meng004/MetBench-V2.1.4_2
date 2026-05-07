using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    // 相似度检测接口
    public interface ISimilarityDetector
    {
        ObservableCollection<SimilarityResult> CalculateSimilarity(TargetProgram targetProgram,ObservableCollection<Application> candidateProgrms);
    }

    public class SimilarityResult 
    {
        public float Similarity { get; set; }
        public TargetProgram TargetProgram { get; set; }
        public  Application Application { get; set; }
    }
}
