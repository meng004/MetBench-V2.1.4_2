using System.Text;

namespace MetBench_BLL
{
    public class HTMLTestReport : ITestReportTemplate
    {
        private string applicationName;
        private string rLatex;
        private string RLatex;
        private string MTTime;
        public void GenerateReport(MTTestResultData testData, string outputPath, string applicationname, string rlatex, string Rlatex, string testTime)
        {
            applicationName = applicationname;
            rLatex = rlatex;
            RLatex = Rlatex;
            MTTime = testTime;
            string htmlContent = GenerateHTMLContent(testData);
            File.WriteAllText(outputPath, htmlContent, Encoding.UTF8);
        }

        private string GenerateHTMLContent(MTTestResultData testData)
        {
            var sb = new StringBuilder();
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            // HTML文档开始
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("    <title>MT Test Report</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
            sb.AppendLine("        h1 { color: #2c3e50; }");
            sb.AppendLine("        h2 { color: #3498db; margin-top: 30px; }");
            sb.AppendLine("        table { border-collapse: collapse; width: 100%; margin-bottom: 20px; }");
            sb.AppendLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            sb.AppendLine("        th { background-color: #f2f2f2; }");
            sb.AppendLine("        tr:nth-child(even) { background-color: #f9f9f9; }");
            sb.AppendLine("        .pass { background-color: #d4edda; }");
            sb.AppendLine("        .fail { background-color: #f8d7da; }");
            sb.AppendLine("        .summary { background-color: #e2e3e5; padding: 15px; border-radius: 5px; }");
            sb.AppendLine("        .section { margin-bottom: 30px; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // 报告标题
            sb.AppendLine("    <h1>MT Test Report</h1>");
            sb.AppendLine($"    <p>Generated on: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}</p>");

            // STC数据部分
            sb.AppendLine("    <div class=\"section\">");
            sb.AppendLine("        <h2>STC Data</h2>");
            sb.AppendLine(GenerateDataTable(testData.STCData));
            sb.AppendLine("    </div>");

            // FTC数据部分
            sb.AppendLine("    <div class=\"section\">");
            sb.AppendLine("        <h2>FTC Data</h2>");
            sb.AppendLine(GenerateDataTable(testData.FTCData));
            sb.AppendLine("    </div>");

            // 验证数据部分
            if (testData.VerificationData.Count > 0)
            {
                sb.AppendLine("    <div class=\"section\">");
                sb.AppendLine("        <h2>Verification Data</h2>");
                sb.AppendLine(GenerateVerificationTable(testData.VerificationData, testData.IsPassResults));
                sb.AppendLine("    </div>");
            }

            // 汇总部分
            sb.AppendLine("    <div class=\"section summary\">");
            sb.AppendLine("        <h2>Test Summary</h2>");
            sb.AppendLine(GenerateSummaryContent(testData));
            sb.AppendLine("    </div>");

            // HTML文档结束
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private string GenerateDataTable(Dictionary<string, List<double>> data)
        {
            if (data.Count == 0)
                return "<p>No data available.</p>";

            var sb = new StringBuilder();
            sb.AppendLine("<table>");

            // 表头
            sb.AppendLine("<tr>");
            foreach (var column in data.Keys)
            {
                sb.AppendLine($"<th>{column}</th>");
            }
            sb.AppendLine("</tr>");

            // 数据行
            int maxRows = data.Values.Max(x => x.Count);
            for (int i = 0; i < maxRows; i++)
            {
                sb.AppendLine("<tr>");
                foreach (var column in data.Keys)
                {
                    if (i < data[column].Count)
                    {
                        sb.AppendLine($"<td>{data[column][i]}</td>");
                    }
                    else
                    {
                        sb.AppendLine("<td>-</td>");
                    }
                }
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            return sb.ToString();
        }

        private string GenerateVerificationTable(Dictionary<string, List<double>> verificationData, List<bool> isPassResults)
        {
            if (verificationData.Count == 0)
                return "<p>No verification data available.</p>";

            var sb = new StringBuilder();
            sb.AppendLine("<table>");

            // 表头
            sb.AppendLine("<tr>");
            foreach (var column in verificationData.Keys)
            {
                sb.AppendLine($"<th>{column}</th>");
            }
            sb.AppendLine("<th>IsPass</th>");
            sb.AppendLine("</tr>");

            // 数据行
            int maxRows = verificationData.Values.Max(x => x.Count);
            for (int i = 0; i < maxRows; i++)
            {
                sb.AppendLine("<tr>");
                foreach (var column in verificationData.Keys)
                {
                    if (i < verificationData[column].Count)
                    {
                        sb.AppendLine($"<td>{verificationData[column][i]}</td>");
                    }
                    else
                    {
                        sb.AppendLine("<td>-</td>");
                    }
                }

                // IsPass列
                if (i < isPassResults.Count)
                {
                    string status = isPassResults[i] ? "Pass" : "Fail";
                    string statusClass = isPassResults[i] ? "pass" : "fail";
                    sb.AppendLine($"<td class=\"{statusClass}\">{status}</td>");
                }
                else
                {
                    sb.AppendLine("<td>-</td>");
                }

                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            return sb.ToString();
        }

        private string GenerateSummaryContent(MTTestResultData testData)
        {
            var sb = new StringBuilder();
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 计算统计数据
            int passCount = testData.IsPassResults.Count(x => x);
            int failCount = testData.IsPassResults.Count(x => !x);
            int totalCount = testData.IsPassResults.Count;
            double passRate = totalCount > 0 ? (double)passCount / totalCount * 100 : 0;
            double failRate = totalCount > 0 ? (double)failCount / totalCount * 100 : 0;

            sb.AppendLine("<h3>Test Case Statistics</h3>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Program Name</th>");
            sb.AppendLine("<th>r_Latex</th>");
            sb.AppendLine("<th>R_Latex</th>");
            sb.AppendLine(" <th>Time</th>");
            sb.AppendLine("<th>Pass Count</th>");
            sb.AppendLine("<th>Fail Count</th>");
            sb.AppendLine("<th>Pass Rate (%)</th>");
            sb.AppendLine("<th>Fail Rate (%)</th>");
            sb.AppendLine("<th>Total Test Cases</th>");
            sb.AppendLine("</tr>");

            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{applicationName}</td>");
            sb.AppendLine($"<td>{rLatex}</td>"); // 填入r
            sb.AppendLine($"<td>{RLatex}</td>"); // 填入R
            sb.AppendLine($"<td>{MTTime}</td>"); // 填入当前时间
            sb.AppendLine($"<td>{passCount}</td>");
            sb.AppendLine($"<td>{failCount}</td>");
            sb.AppendLine($"<td>{passRate:F2}</td>");
            sb.AppendLine($"<td>{failRate:F2}</td>");
            sb.AppendLine($"<td>{totalCount}</td>");
            sb.AppendLine("</tr>");
            sb.AppendLine("</table>");

            return sb.ToString();
        }
    }
}
