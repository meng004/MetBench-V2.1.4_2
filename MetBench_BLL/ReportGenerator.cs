namespace MetBench_BLL
{
    public class ReportGenerator
    {
        private MTTestResultData _testData;
        private ITestReportTemplate _reportTemplate;

        public ReportGenerator(ITestReportTemplate reportTemplate)
        {
            _testData = new MTTestResultData();
            _reportTemplate = reportTemplate;
        }

        public void LoadSTCDataFromCsv(string csvFilePath)
        {
            LoadDataFromCsv(csvFilePath, _testData.STCData);
        }

        public void LoadFTCDataFromCsv(string csvFilePath)
        {
            LoadDataFromCsv(csvFilePath, _testData.FTCData);
        }

        public void LoadVerificationDataFromCsv(string csvFilePath)
        {
            var lines = File.ReadAllLines(csvFilePath);
            if (lines.Length == 0) return;

            // 解析CSV文件头
            var headers = lines[0].Split(',').Select(x => x.Trim()).ToList();

            // 初始化数据结构
            foreach (var header in headers)
            {
                if (header.Equals("IsPass", StringComparison.OrdinalIgnoreCase))
                {
                    _testData.IsPassResults = new List<bool>();
                }
                else
                {
                    if (!_testData.VerificationData.ContainsKey(header))
                    {
                        _testData.VerificationData[header] = new List<double>();
                    }
                }
            }

            // 解析数据行
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',').Select(x => x.Trim()).ToList();
                for (int j = 0; j < headers.Count && j < values.Count; j++)
                {
                    string header = headers[j];
                    string value = values[j];

                    if (header.Equals("IsPass", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(value, out int isPass))
                        {
                            _testData.IsPassResults.Add(isPass == 1);
                        }
                    }
                    else if (double.TryParse(value, out double numericValue))
                    {
                        _testData.VerificationData[header].Add(numericValue);
                    }
                }
            }
        }

        private void LoadDataFromCsv(string csvFilePath, Dictionary<string, List<double>> targetDictionary)
        {
            var lines = File.ReadAllLines(csvFilePath);
            if (lines.Length == 0) return;

            // 解析CSV文件头
            var headers = lines[0].Split(',').Select(x => x.Trim()).ToList();

            // 初始化数据结构
            foreach (var header in headers)
            {
                if (!targetDictionary.ContainsKey(header))
                {
                    targetDictionary[header] = new List<double>();
                }
            }

            // 解析数据行
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',').Select(x => x.Trim()).ToList();
                for (int j = 0; j < headers.Count && j < values.Count; j++)
                {
                    string header = headers[j];
                    string value = values[j];

                    if (double.TryParse(value, out double numericValue))
                    {
                        targetDictionary[header].Add(numericValue);
                    }
                }
            }
        }

        public void GenerateReport(string outputPath, string applicationName, string rLatex, string RLatex,string testTime)
        {
            _reportTemplate.GenerateReport(_testData, outputPath, applicationName, rLatex, RLatex, testTime);
        }
    }
}
