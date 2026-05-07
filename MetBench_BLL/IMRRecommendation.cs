using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    public interface IMRRecommendation
    {
        // 基础接口
        ObservableCollection<MRRecommendationResult> Recommend(TargetProgram program,ObservableCollection<Application> applications);
    }
}
