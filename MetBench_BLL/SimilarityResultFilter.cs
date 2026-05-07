using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    // 相似度筛选策略
    public class SimilarityResultFilter
    {
        // 语法相似筛选的相似度阈值为 0.1  语义相似筛选的相似度阈值为 0.5 
        public ObservableCollection<SimilarityResult> Filter(ObservableCollection<SimilarityResult> similarityResults,bool isSyntaxSimilarityDetect=true) 
        {
            var results = new ObservableCollection<SimilarityResult>();
            if (isSyntaxSimilarityDetect)
            {
                var threshold = 0.1f;
                var similarityResultsList = similarityResults.ToList();
                foreach (var similarityResult in similarityResultsList) 
                {
                    var similarity = similarityResult.Similarity;
                    if (similarity > threshold)
                    {
                        results.Add(similarityResult);
                    }
                }
            }
            else 
            {
                var threshold = 0.5f;
                var similarityResultsList = similarityResults.ToList();
                foreach (var similarityResult in similarityResultsList)
                {
                    var similarity = similarityResult.Similarity;
                    if (similarity > threshold)
                    {
                        results.Add(similarityResult);
                    }
                }
            }
            return results;
        }


    }
}
