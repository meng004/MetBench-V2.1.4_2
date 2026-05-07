
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace MetBench_BLL
{
    public class PdfTestReport : ITestReportTemplate
    {
        private string applicationName;
        private string rLatex;
        private string RLatex;
        private string MTTime;

        public void GenerateReport(MTTestResultData testData, string outputPath, string applicationname, string rlatex, string Rlatex,string testTime)
        {
            applicationName = applicationname;
            rLatex = rlatex;
            RLatex = Rlatex;
            MTTime = testTime;

            // 创建PDF文档（A4大小，边距36单位）
            Document document = new Document(PageSize.A4, 36, 36, 36, 36);
            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(outputPath, FileMode.Create));

            document.Open();

            // 1. 添加主标题（Test Summary）
            Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.Black);
            Paragraph title = new Paragraph("Test Summary", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 20f;
            document.Add(title);

            // 2. 添加生成时间
            Font timeFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.Black);
            Paragraph time = new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", timeFont);
            time.Alignment = Element.ALIGN_RIGHT;
            time.SpacingAfter = 20f;
            document.Add(time);

            // 3. 添加合并数据表格（STC + FTC + Verification）
            if (testData.STCData.Count > 0 || testData.FTCData.Count > 0 ||
                (testData.VerificationData.Count > 0 && testData.IsPassResults.Count > 0))
            {
                AddSectionTitle(document, "Test Data");
                AddCombinedDataTable(document, testData);
            }

            // 4. 添加测试总结表格（完全匹配图片格式）
            AddSectionTitle(document, "Test Case Statistics");
            AddTestSummaryTable(document, testData);

            document.Close();
        }

        private void AddSectionTitle(Document document, string titleText)
        {
            Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.Black);
            Paragraph title = new Paragraph(titleText, titleFont);
            title.SpacingAfter = 10f;
            document.Add(title);
        }

        private void AddCombinedDataTable(Document document, MTTestResultData testData)
        {
            // 计算总列数：STC列 + FTC列 + Verification列 + IsPass列
            int totalColumns = testData.STCData.Count + testData.FTCData.Count +
                             testData.VerificationData.Count + 1; // +1 for IsPass

            PdfPTable table = new PdfPTable(totalColumns);
            table.WidthPercentage = 100;
            table.DefaultCell.Border = PdfPCell.BOTTOM_BORDER | PdfPCell.TOP_BORDER |
                                     PdfPCell.LEFT_BORDER | PdfPCell.RIGHT_BORDER;
            table.DefaultCell.BorderWidth = 1f;

            // 添加STC列标题
            foreach (var column in testData.STCData.Keys)
            {
                PdfPCell cell = new PdfPCell(new Phrase($"STC_{column}",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                cell.BackgroundColor = new BaseColor(200, 230, 255); // 浅蓝色背景
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.Padding = 5f;
                table.AddCell(cell);
            }

            // 添加FTC列标题
            foreach (var column in testData.FTCData.Keys)
            {
                PdfPCell cell = new PdfPCell(new Phrase($"FTC_{column}",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                cell.BackgroundColor = new BaseColor(255, 230, 200); // 浅橙色背景
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.Padding = 5f;
                table.AddCell(cell);
            }

            // 添加Verification列标题
            foreach (var column in testData.VerificationData.Keys)
            {
                PdfPCell cell = new PdfPCell(new Phrase(column,
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                cell.BackgroundColor = new BaseColor(230, 255, 200); // 浅绿色背景
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.Padding = 5f;
                table.AddCell(cell);
            }

            // 添加IsPass列标题
            PdfPCell passHeader = new PdfPCell(new Phrase("IsPass",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
            passHeader.BackgroundColor = new BaseColor(255, 200, 200); // 浅红色背景
            passHeader.HorizontalAlignment = Element.ALIGN_CENTER;
            passHeader.Padding = 5f;
            table.AddCell(passHeader);

            // 计算最大行数
            int maxRows = Math.Max(
                Math.Max(
                    testData.STCData.Values.Max(x => x.Count),
                    testData.FTCData.Values.Max(x => x.Count)
                ),
                Math.Max(
                    testData.VerificationData.Values.Max(x => x.Count),
                    testData.IsPassResults.Count
                )
            );

            List<int> allLengths = testData.STCData.Values.Select(list => list.Count).ToList();

            int rows =  testData.STCData.Values.Max(list => list.Count);
            // 添加数据行
            for (int i = 0; i < maxRows; i++)
            {
                // 添加STC数据
                foreach (var column in testData.STCData.Keys)
                {
                    string value = i < testData.STCData[column].Count ?
                        testData.STCData[column][i].ToString("0.######") : "-";
                    PdfPCell cell = new PdfPCell(new Phrase(value));
                    cell.Padding = 5f;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                }

                // 添加FTC数据
                foreach (var column in testData.FTCData.Keys)
                {
                    string value = i < testData.FTCData[column].Count ?
                        testData.FTCData[column][i].ToString("0.######") : "-";
                    PdfPCell cell = new PdfPCell(new Phrase(value));
                    cell.Padding = 5f;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                }

                // 添加Verification数据
                foreach (var column in testData.VerificationData.Keys)
                {
                    string value = i < testData.VerificationData[column].Count ?
                        testData.VerificationData[column][i].ToString("0.######") : "-";
                    PdfPCell cell = new PdfPCell(new Phrase(value));
                    cell.Padding = 5f;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                }

                // 添加IsPass结果
                string passValue = i < testData.IsPassResults.Count ?
                    (testData.IsPassResults[i] ? "Pass" : "Fail") : "-";
                BaseColor passColor = i < testData.IsPassResults.Count ?
                    (testData.IsPassResults[i] ? BaseColor.Green : BaseColor.Red) : BaseColor.White;

                PdfPCell passCell = new PdfPCell(new Phrase(passValue));
                passCell.Padding = 5f;
                passCell.HorizontalAlignment = Element.ALIGN_CENTER;
                passCell.BackgroundColor = passColor;
                table.AddCell(passCell);
            }

            document.Add(table);
            document.Add(new Paragraph(" ")); // 添加空行作为间距
        }

        private void AddTestSummaryTable(Document document, MTTestResultData testData)
        {
            // 创建9列表格（完全匹配图片中的列数）
            PdfPTable table = new PdfPTable(9);
            table.WidthPercentage = 100;
            table.DefaultCell.Border = PdfPCell.BOTTOM_BORDER | PdfPCell.TOP_BORDER |
                                     PdfPCell.LEFT_BORDER | PdfPCell.RIGHT_BORDER;
            table.DefaultCell.BorderWidth = 1f;

            // 添加表头（完全匹配图片中的列名）
            string[] headers = { "Program Name","r_Latex","R_Latex","Time", "Pass Count", "Fail Count",
                           "Pass Rate (%)", "Fail Rate (%)", "Total Test Cases" };

            foreach (var header in headers)
            {
                PdfPCell cell = new PdfPCell(new Phrase(header,
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8)));
                cell.BackgroundColor = new BaseColor(217, 217, 217); // 灰色背景
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.Padding = 5f;
                table.AddCell(cell);
            }

            // 计算统计数据（与图片中的示例数据格式一致）
            int passCount = testData.IsPassResults.Count(x => x);
            int failCount = testData.IsPassResults.Count(x => !x);
            int totalCount = testData.IsPassResults.Count;
            double passRate = totalCount > 0 ? (double)passCount / totalCount * 100 : 0;
            double failRate = totalCount > 0 ? (double)failCount / totalCount * 100 : 0;

            // 添加数据行（完全匹配图片中的格式）
            string[] dataValues = {
             $"{applicationName}",
            $"{rLatex}",
            $"{RLatex}",
            $"{MTTime}",
            passCount.ToString(),
            failCount.ToString(),
            passRate.ToString("0.00"),
            failRate.ToString("0.00"),
            totalCount.ToString()
        };

            foreach (var value in dataValues)
            {
                PdfPCell cell = new PdfPCell(new Phrase(value));
                cell.Padding = 5f;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                table.AddCell(cell);
            }

            document.Add(table);
        }
    }
}
