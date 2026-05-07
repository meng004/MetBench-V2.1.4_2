namespace MetBench_BLL
{
    public interface ITestReportTemplate
    {
        void GenerateReport(MTTestResultData testData, string outputPath, string applicationName, string rLatex, string RLatex, string testTime);
    }
}
