namespace MetBench_BLL
{
    public class MTTestResultData
    {
        public Dictionary<string, List<double>> STCData { get; set; } = new Dictionary<string, List<double>>();
        public Dictionary<string, List<double>> FTCData { get; set; } = new Dictionary<string, List<double>>();
        public Dictionary<string, List<double>> VerificationData { get; set; } = new Dictionary<string, List<double>>();
        public List<bool> IsPassResults { get; set; } = new List<bool>();
    }
}
