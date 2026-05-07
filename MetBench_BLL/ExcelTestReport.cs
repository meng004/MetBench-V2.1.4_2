using ClosedXML.Excel;
namespace MetBench_BLL
{
    public class ExcelTestReport : ITestReportTemplate
    {
        private string applicationName;
        private string rLatex;
        private string RLatex;
        private string MTTime;
        public void GenerateReport(MTTestResultData testData, string outputPath, string applicationname, string rlatex, string Rlatex, string testTime)
        {
            using (var workbook = new XLWorkbook())
            {
                applicationName = applicationname;
                rLatex = rlatex;
                RLatex = Rlatex;
                MTTime = testTime;
                // 添加STC数据工作表
                var stcSheet = workbook.Worksheets.Add("STC Data");
                GenerateDataSheet(stcSheet, testData.STCData, "STC Data");

                // 添加FTC数据工作表
                var ftcSheet = workbook.Worksheets.Add("FTC Data");
                GenerateDataSheet(ftcSheet, testData.FTCData, "FTC Data");

                // 添加验证数据工作表
                var verificationSheet = workbook.Worksheets.Add("Verification Data");
                GenerateVerificationSheet(verificationSheet, testData.VerificationData, testData.IsPassResults);

                // 添加汇总工作表
                var summarySheet = workbook.Worksheets.Add("Summary");
                GenerateSummarySheet(summarySheet, testData);

                workbook.SaveAs(outputPath);
            }
        }

        private void GenerateDataSheet(IXLWorksheet worksheet, Dictionary<string, List<double>> data, string title)
        {
            // 写入标题
            worksheet.Cell(1, 1).Value = title;
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, data.Count + 1).Merge();

            // 写入列标题
            int col = 1;
            foreach (var column in data.Keys)
            {
                worksheet.Cell(2, col).Value = column;
                worksheet.Cell(2, col).Style.Font.Bold = true;
                col++;
            }

            // 写入数据
            int row = 3;
            int maxRows = data.Values.Max(x => x.Count);
            for (int i = 0; i < maxRows; i++)
            {
                col = 1;
                foreach (var column in data.Keys)
                {
                    if (i < data[column].Count)
                    {
                        worksheet.Cell(row, col).Value = data[column][i];
                    }
                    col++;
                }
                row++;
            }

            // 应用样式
            worksheet.RangeUsed().Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.RangeUsed().Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.Columns().AdjustToContents();
        }

        private void GenerateVerificationSheet(IXLWorksheet worksheet, Dictionary<string, List<double>> verificationData, List<bool> isPassResults)
        {
            // 写入标题
            worksheet.Cell(1, 1).Value = "Verification Data";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, verificationData.Count + 2).Merge();

            // 写入列标题
            int col = 1;
            foreach (var column in verificationData.Keys)
            {
                worksheet.Cell(2, col).Value = column;
                worksheet.Cell(2, col).Style.Font.Bold = true;
                col++;
            }
            worksheet.Cell(2, col).Value = "IsPass";
            worksheet.Cell(2, col).Style.Font.Bold = true;

            // 写入数据
            int row = 3;
            int maxRows = verificationData.Values.Max(x => x.Count);
            for (int i = 0; i < maxRows; i++)
            {
                col = 1;
                foreach (var column in verificationData.Keys)
                {
                    if (i < verificationData[column].Count)
                    {
                        worksheet.Cell(row, col).Value = verificationData[column][i];
                    }
                    col++;
                }

                // 写入IsPass结果
                if (i < isPassResults.Count)
                {
                    worksheet.Cell(row, col).Value = isPassResults[i] ? "Pass" : "Fail";
                    worksheet.Cell(row, col).Style.Fill.BackgroundColor = isPassResults[i]
                        ? XLColor.LightGreen
                        : XLColor.LightPink;
                }

                row++;
            }

            // 应用样式
            worksheet.RangeUsed().Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.RangeUsed().Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.Columns().AdjustToContents();
        }
        private void GenerateSummarySheet(IXLWorksheet worksheet, MTTestResultData testData)
        {
            // 写入标题
            worksheet.Cell(1, 1).Value = "Test Summary";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(1, 1, 1, 5).Merge();

            // 计算统计数据
            int passCount = testData.IsPassResults.Count(x => x);
            int failCount = testData.IsPassResults.Count(x => !x);
            int totalCount = testData.IsPassResults.Count;
            double passRate = totalCount > 0 ? (double)passCount / totalCount * 100 : 0;
            double failRate = totalCount > 0 ? (double)failCount / totalCount * 100 : 0;


            // 写入统计数据
            worksheet.Cell(3, 1).Value = "Test Case Statistics";
            worksheet.Cell(3, 1).Style.Font.Bold = true;

            worksheet.Cell(4, 1).Value = "Program Name";
            worksheet.Cell(4, 2).Value = "r_Latex"; // 新增列
            worksheet.Cell(4, 3).Value = "R_Latex"; // 新增列
            worksheet.Cell(4, 4).Value = "Time"; // 新增列
            worksheet.Cell(4, 5).Value = "Pass Count";
            worksheet.Cell(4, 6).Value = "Fail Count";
            worksheet.Cell(4, 7).Value = "Pass Rate (%)";
            worksheet.Cell(4, 8).Value = "Fail Rate (%)";
            worksheet.Cell(4, 9).Value = "Total Test Cases";

            // 设置列标题样式
            for (int i = 1; i <= 9; i++)
            {
                worksheet.Cell(4, i).Style.Font.Bold = true;
                worksheet.Cell(4, i).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // 写入数据行
            worksheet.Cell(5, 1).Value = $"{applicationName}";
            worksheet.Cell(5, 2).Value = $"{rLatex}";
            worksheet.Cell(5, 3).Value = $"{RLatex}";
            worksheet.Cell(5, 4).Value = MTTime; // 填入当前时间
            worksheet.Cell(5, 5).Value = passCount;
            worksheet.Cell(5, 6).Value = failCount;
            worksheet.Cell(5, 7).Value = Math.Round(passRate, 2);
            worksheet.Cell(5, 8).Value = Math.Round(failRate, 2);
            worksheet.Cell(5, 9).Value = totalCount;

            // 应用样式
            worksheet.Range(4, 1, 5, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(4, 1, 5, 9).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.Columns().AdjustToContents();
        }
    }
}
