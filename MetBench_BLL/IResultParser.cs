using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    public interface IResultParser
    {

        ParsedResult Parse(DetectionResult detectionResult);
        ObservableCollection<MRDetectorCollection> GetMRDetectorCollections();
    }
}
