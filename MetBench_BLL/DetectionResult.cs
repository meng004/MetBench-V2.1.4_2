namespace MetBench_BLL
{
    public class DetectionResult
    {
        public Dictionary<string, object> Result { get; set; }
        public bool IsCompleted { get; set; }=false;
        public string AlgorithmType { get; set; }
    }
}
