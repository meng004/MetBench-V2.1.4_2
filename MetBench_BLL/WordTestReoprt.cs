using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace MetBench_BLL
{
    public class WordTestReport : ITestReportTemplate
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
            using (WordprocessingDocument doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document))
            {
                // 创建文档主体
                MainDocumentPart mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // 1. 添加主标题
                AddTitle(body, "MT Test Report", 32, true);

                // 2. 添加生成时间
                AddParagraph(body, $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                             alignment: JustificationValues.Right, spacingAfter: 600);

                // 3. 添加合并数据表格（STC + FTC + Verification）
                if (testData.STCData.Count > 0 || testData.FTCData.Count > 0 ||
                    (testData.VerificationData.Count > 0 && testData.IsPassResults.Count > 0))
                {
                    AddTitle(body, "Test Data", 24, true);
                    AddCombinedDataTable(body, testData);
                }

                // 4. 添加测试总结表格（完全匹配图片格式）
                AddTitle(body, "Test Summary", 24, true);
                AddTitle(body, "Test Case Statistics", 18, true, spacingAfter: 200);
                AddTestSummaryTable(body, testData);
            }
        }

        private void AddTitle(Body body, string text, int fontSize, bool isBold, uint spacingAfter = 200)
        {
            Paragraph para = body.AppendChild(new Paragraph());
            Run run = para.AppendChild(new Run());
            run.AppendChild(new Text(text));

            RunProperties runProps = new RunProperties();
            if (isBold) runProps.Append(new Bold());
            runProps.Append(new FontSize() { Val = fontSize.ToString() });
            run.RunProperties = runProps;

            para.ParagraphProperties = new ParagraphProperties(
                new Justification() { Val = JustificationValues.Center },
                new SpacingBetweenLines() { After = spacingAfter.ToString() });
        }

        private void AddParagraph(Body body, string text, JustificationValues alignment,
                                 int fontSize = 12, uint spacingAfter = 200)
        {
            Paragraph para = body.AppendChild(new Paragraph());
            Run run = para.AppendChild(new Run());
            run.AppendChild(new Text(text));

            run.RunProperties = new RunProperties(
                new FontSize() { Val = fontSize.ToString() });

            para.ParagraphProperties = new ParagraphProperties(
                new Justification() { Val = alignment },
                new SpacingBetweenLines() { After = spacingAfter.ToString() });
        }

        //private void AddCombinedDataTable(Body body, MTTestResultData testData)
        //{
        //    // 计算总列数：STC列 + FTC列 + Verification列 + IsPass列
        //    int totalColumns = testData.STCData.Count + testData.FTCData.Count +
        //                     testData.VerificationData.Count + 1; // +1 for IsPass

        //    Table table = new Table();

        //    // 设置表格样式
        //    TableProperties tableProps = new TableProperties(
        //        new TableBorders(
        //            new TopBorder() { Val = BorderValues.Single, Size = 4 },
        //            new BottomBorder() { Val = BorderValues.Single, Size = 4 },
        //            new LeftBorder() { Val = BorderValues.Single, Size = 4 },
        //            new RightBorder() { Val = BorderValues.Single, Size = 4 },
        //            new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 4 },
        //            new InsideVerticalBorder() { Val = BorderValues.Single, Size = 4 }
        //        ),
        //        new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct }
        //    );
        //    table.AppendChild(tableProps);

        //    // 添加表头行
        //    TableRow headerRow = new TableRow();

        //    // 添加STC列标题
        //    foreach (var column in testData.STCData.Keys)
        //    {
        //        TableCell cell = CreateTableCell($"STC_{column}", true);
        //        cell.TableCellProperties.Append(new Shading() { Fill = "D9E5FF" }); // 浅蓝色背景
        //        headerRow.AppendChild(cell);
        //    }

        //    // 添加FTC列标题
        //    foreach (var column in testData.FTCData.Keys)
        //    {
        //        TableCell cell = CreateTableCell($"FTC_{column}", true);
        //        cell.TableCellProperties.Append(new Shading() { Fill = "FFE5D9" }); // 浅橙色背景
        //        headerRow.AppendChild(cell);
        //    }

        //    // 添加Verification列标题
        //    foreach (var column in testData.VerificationData.Keys)
        //    {
        //        TableCell cell = CreateTableCell(column, true);
        //        cell.TableCellProperties.Append(new Shading() { Fill = "E5FFD9" }); // 浅绿色背景
        //        headerRow.AppendChild(cell);
        //    }

        //    // 添加IsPass列标题
        //    TableCell passHeader = CreateTableCell("IsPass", true);
        //    passHeader.TableCellProperties.Append(new Shading() { Fill = "FFD9D9" }); // 浅红色背景
        //    headerRow.AppendChild(passHeader);

        //    table.AppendChild(headerRow);

        //    // 计算最大行数
        //    int maxRows = Math.Max(
        //        Math.Max(
        //            testData.STCData.Values.Max(x => x.Count),
        //            testData.FTCData.Values.Max(x => x.Count)
        //        ),
        //        Math.Max(
        //            testData.VerificationData.Values.Max(x => x.Count),
        //            testData.IsPassResults.Count
        //        )
        //    );

        //    // 添加数据行
        //    for (int i = 0; i < maxRows; i++)
        //    {
        //        TableRow dataRow = new TableRow();

        //        // 添加STC数据
        //        foreach (var column in testData.STCData.Keys)
        //        {
        //            string value = i < testData.STCData[column].Count ?
        //                testData.STCData[column][i].ToString("0.######") : "-";
        //            dataRow.AppendChild(CreateTableCell(value, false));
        //        }

        //        // 添加FTC数据
        //        foreach (var column in testData.FTCData.Keys)
        //        {
        //            string value = i < testData.FTCData[column].Count ?
        //                testData.FTCData[column][i].ToString("0.######") : "-";
        //            dataRow.AppendChild(CreateTableCell(value, false));
        //        }

        //        // 添加Verification数据
        //        foreach (var column in testData.VerificationData.Keys)
        //        {
        //            string value = i < testData.VerificationData[column].Count ?
        //                testData.VerificationData[column][i].ToString("0.######") : "-";
        //            dataRow.AppendChild(CreateTableCell(value, false));
        //        }

        //        // 添加IsPass结果
        //        string passValue = i < testData.IsPassResults.Count ?
        //            (testData.IsPassResults[i] ? "Pass" : "Fail") : "-";
        //        TableCell passCell = CreateTableCell(passValue, false);
        //        passCell.TableCellProperties.Append(new Shading()
        //        {
        //            Fill = testData.IsPassResults[i] ? "00FF00" : "FF0000"
        //        }); // 绿色/红色背景
        //        dataRow.AppendChild(passCell);

        //        table.AppendChild(dataRow);
        //    }

        //    body.AppendChild(table);
        //    AddParagraph(body, "", JustificationValues.Left, spacingAfter: 400); // 添加间距
        //}

        private void AddCombinedDataTable(Body body, MTTestResultData testData)
        {
            // 数据验证
            if (testData == null || testData.STCData == null || testData.FTCData == null ||
                testData.VerificationData == null || testData.IsPassResults == null)
            {
                AddParagraph(body, "测试数据不完整，无法生成表格", JustificationValues.Center);
                return;
            }

            // 计算总列数（安全方式）
            int totalColumns = (testData.STCData?.Count ?? 0) +
                              (testData.FTCData?.Count ?? 0) +
                              (testData.VerificationData?.Count ?? 0) + 1;

            Table table = new Table();

            // 设置表格样式
            TableProperties tableProps = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder() { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder() { Val = BorderValues.Single, Size = 4 },
                    new RightBorder() { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 4 }
                ),
                new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct }
            );
            table.AppendChild(tableProps);

            // 添加表头行
            TableRow headerRow = new TableRow();

            // 添加STC列标题（安全方式）
            if (testData.STCData?.Count > 0)
            {
                foreach (var column in testData.STCData.Keys)
                {
                    var cell = CreateTableCell($"STC_{column}", true);
                    cell.TableCellProperties.Append(new Shading() { Fill = "D9E5FF" });
                    headerRow.AppendChild(cell);
                }
            }

            // 添加FTC列标题（安全方式）
            if (testData.FTCData?.Count > 0)
            {
                foreach (var column in testData.FTCData.Keys)
                {
                    var cell = CreateTableCell($"FTC_{column}", true);
                    cell.TableCellProperties.Append(new Shading() { Fill = "FFE5D9" });
                    headerRow.AppendChild(cell);
                }
            }

            // 添加Verification列标题（安全方式）
            if (testData.VerificationData?.Count > 0)
            {
                foreach (var column in testData.VerificationData.Keys)
                {
                    var cell = CreateTableCell(column, true);
                    cell.TableCellProperties.Append(new Shading() { Fill = "E5FFD9" });
                    headerRow.AppendChild(cell);
                }
            }

            // 添加IsPass列标题
            TableCell passHeader = CreateTableCell("IsPass", true);
            passHeader.TableCellProperties.Append(new Shading() { Fill = "FFD9D9" });
            headerRow.AppendChild(passHeader);

            table.AppendChild(headerRow);

            // 安全计算最大行数（处理可能的空集合）
            int maxRows = 0;
            try
            {
                maxRows = new[]
                {
            testData.STCData?.Values.Select(x => x?.Count ?? 0).Max() ?? 0,
            testData.FTCData?.Values.Select(x => x?.Count ?? 0).Max() ?? 0,
            testData.VerificationData?.Values.Select(x => x?.Count ?? 0).Max() ?? 0,
            testData.IsPassResults?.Count ?? 0
        }.Max();
            }
            catch (InvalidOperationException) // 当所有集合为空时
            {
                maxRows = 0;
            }

            // 添加数据行
            for (int i = 0; i < maxRows; i++)
            {
                TableRow dataRow = new TableRow();

                // 添加STC数据（安全方式）
                if (testData.STCData?.Count > 0)
                {
                    foreach (var column in testData.STCData.Keys)
                    {
                        string value = "-";
                        if (testData.STCData.TryGetValue(column, out var stcList) &&
                            stcList != null && i < stcList.Count)
                        {
                            value = stcList[i].ToString("0.######");
                        }
                        dataRow.AppendChild(CreateTableCell(value, false));
                    }
                }

                // 添加FTC数据（安全方式）
                if (testData.FTCData?.Count > 0)
                {
                    foreach (var column in testData.FTCData.Keys)
                    {
                        string value = "-";
                        if (testData.FTCData.TryGetValue(column, out var ftcList) &&
                            ftcList != null && i < ftcList.Count)
                        {
                            value = ftcList[i].ToString("0.######");
                        }
                        dataRow.AppendChild(CreateTableCell(value, false));
                    }
                }

                // 添加Verification数据（安全方式）
                if (testData.VerificationData?.Count > 0)
                {
                    foreach (var column in testData.VerificationData.Keys)
                    {
                        string value = "-";
                        if (testData.VerificationData.TryGetValue(column, out var verifyList) &&
                            verifyList != null && i < verifyList.Count)
                        {
                            value = verifyList[i].ToString("0.######");
                        }
                        dataRow.AppendChild(CreateTableCell(value, false));
                    }
                }

                // 添加IsPass结果（完全安全的方式）
                string passValue = "-";
                string shadingColor = "FFFFFF"; // 默认白色

                if (i < testData.IsPassResults?.Count)
                {
                    bool isPass = testData.IsPassResults[i];
                    passValue = isPass ? "Pass" : "Fail";
                    shadingColor = isPass ? "00FF00" : "FF0000";
                }

                TableCell passCell = CreateTableCell(passValue, false);
                passCell.TableCellProperties.Append(new Shading() { Fill = shadingColor });
                dataRow.AppendChild(passCell);

                table.AppendChild(dataRow);
            }

            body.AppendChild(table);
            AddParagraph(body, "", JustificationValues.Left, spacingAfter: 400);
        }
        private void AddTestSummaryTable(Body body, MTTestResultData testData)
        {
            Table table = new Table();

            // 设置表格样式
            TableProperties tableProps = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder() { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder() { Val = BorderValues.Single, Size = 4 },
                    new RightBorder() { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 4 }
                ),
                new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct }
            );
            table.AppendChild(tableProps);

            // 添加表头行（与图片完全一致）
            string[] headers = { "Program Name","r_Latex","R_Latex","Time", "Pass Count", "Fail Count",
                           "Pass Rate (%)", "Fail Rate (%)", "Total Test Cases" };

            TableRow headerRow = new TableRow();
            foreach (var header in headers)
            {
                headerRow.Append(CreateTableCell(header, true));
            }
            table.Append(headerRow);

            // 计算统计数据
            int passCount = testData.IsPassResults.Count(x => x);
            int failCount = testData.IsPassResults.Count(x => !x);
            int totalCount = testData.IsPassResults.Count;
            double passRate = totalCount > 0 ? (double)passCount / totalCount * 100 : 0;
            double failRate = totalCount > 0 ? (double)failCount / totalCount * 100 : 0;

            // 添加数据行（与图片完全一致）
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

            TableRow dataRow = new TableRow();
            foreach (var value in dataValues)
            {
                dataRow.Append(CreateTableCell(value, false));
            }
            table.Append(dataRow);

            body.Append(table);
        }

        private TableCell CreateTableCell(string text, bool isHeader)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(new Text(text))));

            var cellProps = new TableCellProperties();
            if (isHeader)
            {
                cellProps.Append(new Shading() { Fill = "D9D9D9" }); // 灰色背景
            }
            cellProps.Append(new TableCellBorders(
                new TopBorder() { Val = BorderValues.Single, Size = 4 },
                new BottomBorder() { Val = BorderValues.Single, Size = 4 },
                new LeftBorder() { Val = BorderValues.Single, Size = 4 },
                new RightBorder() { Val = BorderValues.Single, Size = 4 }
            ));

            cell.TableCellProperties = cellProps;
            return cell;
        }
    }
}
