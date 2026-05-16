namespace MetBench_BLL
{
    public class MTReportGeneratorService
    {
        private ReportGenerator _reportGenerator;
        private ReportGenerator _htmlReportGenerator;
        private ReportGenerator _wordReportGenerator;
        private ReportGenerator _pdfReportGenerator;

        public MTReportGeneratorService()
        {
            _reportGenerator = new ReportGenerator(new ExcelTestReport());
            _htmlReportGenerator = new ReportGenerator(new HTMLTestReport());
            _wordReportGenerator = new ReportGenerator(new WordTestReport());
            _pdfReportGenerator = new ReportGenerator(new PdfTestReport());
        }

        public void LoadData(string stcCsvPath, string ftcCsvPath, string verificationCsvPath)
        {
            _reportGenerator.LoadSTCDataFromCsv(stcCsvPath);
            _reportGenerator.LoadFTCDataFromCsv(ftcCsvPath);
            _reportGenerator.LoadVerificationDataFromCsv(verificationCsvPath);

            _htmlReportGenerator.LoadSTCDataFromCsv(stcCsvPath);
            _htmlReportGenerator.LoadFTCDataFromCsv(ftcCsvPath);
            _htmlReportGenerator.LoadVerificationDataFromCsv(verificationCsvPath);

            _wordReportGenerator.LoadSTCDataFromCsv(stcCsvPath);
            _wordReportGenerator.LoadFTCDataFromCsv(ftcCsvPath);
            _wordReportGenerator.LoadVerificationDataFromCsv(verificationCsvPath);

            _pdfReportGenerator.LoadSTCDataFromCsv(stcCsvPath);
            _pdfReportGenerator.LoadFTCDataFromCsv(ftcCsvPath);
            _pdfReportGenerator.LoadVerificationDataFromCsv(verificationCsvPath);
        }

        public void GenerateExcelReport(string outputPath, string applicationName, string rLatex, string RLatex, string testTime)
        {
            _reportGenerator.GenerateReport(outputPath, applicationName, rLatex, RLatex, testTime);
        }

        public void GenerateHtmlReport(string outputPath, string applicationName, string rLatex, string RLatex, string testTime)
        {
            _htmlReportGenerator.GenerateReport(outputPath, applicationName, rLatex, RLatex, testTime);
        }

        public void GenerateWordReport(string outputPath, string applicationName, string rLatex, string RLatex, string testTime)
        {
            _wordReportGenerator.GenerateReport(outputPath, applicationName, rLatex, RLatex, testTime);
        }

        public void GeneratePDfReport(string outputPath, string applicationName, string rLatex, string RLatex, string testTime)
        {
            _pdfReportGenerator.GenerateReport(outputPath, applicationName, rLatex, RLatex, testTime);
        }

        // 新增方法：同时生成两种格式的报告
        //public void GenerateAllReports(string excelOutputPath, string htmlOutputPath, string applicationName, string rLatex, string RLatex)
        //{
        //    GenerateExcelReport(excelOutputPath, applicationName, rLatex, RLatex);
        //    GenerateHtmlReport(htmlOutputPath, applicationName, rLatex, RLatex);
        //}
        public void GenerateAllReports(string pdfOutPath, string wordOutPath, string htmlOutputPath, string excelOutputPath, string applicationName, string rLatex, string RLatex, string testTime)
        {
            GeneratePDfReport(pdfOutPath, applicationName, rLatex, RLatex,testTime);
            GenerateWordReport(wordOutPath, applicationName, rLatex, RLatex, testTime);
            GenerateHtmlReport(htmlOutputPath, applicationName, rLatex, RLatex, testTime);
            GenerateExcelReport(excelOutputPath, applicationName, rLatex, RLatex, testTime);
        }
    }
}
