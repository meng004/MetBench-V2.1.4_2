using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    public class UnitLevelPreprocessor :IPreprocessor
    {
        private ApplicationSerive _applicationSerive ;

        public UnitLevelPreprocessor(ApplicationSerive applicationSerive) 
        {
            _applicationSerive = applicationSerive ;
        }

        // 返回 "Program",program "ObservableCollection<Application>"

        public Dictionary<string, object> Process(TargetProgram targetProgram, ObservableCollection<Application> candidatePrograms)
        {
            var newtargetProgram = ProcessTargetProgram(targetProgram);
            var newcandidatePrograms = ProcessCandidateProgram(targetProgram, candidatePrograms);
            var results = new Dictionary<string, object>();

            results.Add("TargetProgram", newtargetProgram);
            results.Add("CandidateProgram", newcandidatePrograms);
            return results;
        }

        // 筛选候选程序
        public ObservableCollection<Application> ProcessCandidateProgram(TargetProgram targetProgram, ObservableCollection<Application> candidatePrograms)
        {
            //  处理空值情况
            if (targetProgram == null || candidatePrograms == null)
            {
                return candidatePrograms ?? new ObservableCollection<Application>();
            }

            //  安全转型
            FunctionProgram functionProgram = targetProgram as FunctionProgram;
            if (functionProgram == null)
            {
                return candidatePrograms;
            }

            //  获取目标程序特征
            var targetName = Path.GetFileNameWithoutExtension(functionProgram.FunctionName);//如 sin
            var targetLanguage = targetProgram.Language.ToString();
            var targetParamCount = functionProgram.ParameterCount;

            // 4. 转换为列表以便处理
            var applications = candidatePrograms.ToList();

            // 5. 按优先级顺序应用筛选条件
            var filteredResults = applications.AsEnumerable();

            //  筛选编程语言
            if (!string.IsNullOrWhiteSpace(targetLanguage))
            {
                filteredResults = filteredResults
                    .Where(app => string.Equals(app.ProgrammingLanguage, targetLanguage, StringComparison.OrdinalIgnoreCase));
            }
            //  如果经过语言筛选后没有结果，返回所有候选
            if (!filteredResults.Any())
            {
                return candidatePrograms;
            }

            ////  筛选函数名称（模糊匹配）
            //if (!string.IsNullOrWhiteSpace(targetName))
            //{
            //    filteredResults = filteredResults
            //        .Where(app => FuzzyNameMatch(app.Name, targetName))
            //        .OrderByDescending(app => CalculateNameSimilarity(app.Name, targetName));
            //}
            //// 如果经过名称筛选后没有结果，返回之前的结果
            //if (!filteredResults.Any())
            //{
            //    return new ObservableCollection<Application>(candidatePrograms
            //        .Where(app => string.Equals(app.ProgrammingLanguage, targetLanguage, StringComparison.OrdinalIgnoreCase))
            //        .ToList());
            //}
            // 5.3 筛选参数个数 暂时不实现
            //if (targetParamCount > 0)
            //{
            //    filteredResults = filteredResults
            //        .Where(app => app.InputParameters.Count == targetParamCount);
            //}

            // 6. 返回结果
            var resultList = filteredResults.ToList();
            return new ObservableCollection<Application>(resultList.Any() ? resultList : applications);

        }
        #region 辅助方法 - 名称模糊匹配
        // 名称模糊匹配（基于包含关系）
        private bool FuzzyNameMatch(string candidateName, string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName)) return true;
            if (string.IsNullOrWhiteSpace(candidateName)) return false;

            // 简单实现：检查是否包含（忽略大小写和特殊字符）
            var cleanCandidate = NormalizeName(candidateName);
            var cleanTarget = NormalizeName(targetName);

            return cleanCandidate.Contains(cleanTarget) || cleanTarget.Contains(cleanCandidate);
        }

        // 计算名称相似度分数（0-1）
        private double CalculateNameSimilarity(string name1, string name2)
        {
            if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
                return 0;

            if (string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase))
                return 1.0;

            // 简单相似度计算：基于共同字符比例
            var set1 = new HashSet<char>(NormalizeName(name1));
            var set2 = new HashSet<char>(NormalizeName(name2));

            int intersection = set1.Intersect(set2).Count();
            int union = set1.Union(set2).Count();

            return union == 0 ? 0 : (double)intersection / union;
        }

        // 规范化名称用于比较
        private string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            return new string(name
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }
        #endregion
        public TargetProgram ProcessTargetProgram(TargetProgram targetProgram)
        {
            if (targetProgram == null)
            {
                return null;
            }

            var program = targetProgram as FunctionProgram;
            if (program == null)
            {
                return null;
            }

            var language = program.Language;
            var filePath = program.FilePath;


            // 根据编程语言处理注释并处理空白行
            ProcessAndSaveToFile(filePath, language);

            return program;
        }

        private string ProcessAndSaveToFile(string sourceFilePath, ProgramLanguage language)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                throw new ArgumentException("无效的文件路径");
            }

            string sourceCode = File.ReadAllText(sourceFilePath);
            string processedCode;

            // 多语言处理选择
            switch (language)
            {
                case ProgramLanguage.Python:
                    processedCode = ProcessPythonCode(sourceCode);
                    break;

                case ProgramLanguage.Java:
                    processedCode = ProcessJavaCode(sourceCode);
                    break;

                case ProgramLanguage.C:
                    // TODO: 未来实现
                    processedCode = sourceCode;
                    break;

                case ProgramLanguage.CSharp:
                    // TODO: 未来实现
                    processedCode = sourceCode;
                    break;

                default:
                    processedCode = sourceCode;
                    break;
            }

            File.WriteAllText(sourceFilePath, processedCode);
            return sourceFilePath;
        }

        /// <summary>
        /// 处理Python代码（仅去除#注释）
        /// </summary>
        private  string ProcessPythonCode(string sourceCode)
        {
            var lines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var resultLines = new List<string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.Trim())) continue;

                int commentIndex = line.IndexOf('#');
                string processedLine = (commentIndex >= 0)
                    ? line.Substring(0, commentIndex).TrimEnd()
                    : line;

                if (!string.IsNullOrWhiteSpace(processedLine))
                {
                    resultLines.Add(processedLine);
                }
            }

            return string.Join(Environment.NewLine, resultLines);
        }

        /// <summary>
        /// 处理Java代码（去除//和/* */注释）
        /// </summary>
        private string ProcessJavaCode(string sourceCode)
        {
            var lines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var resultLines = new List<string>();
            bool inBlockComment = false;

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                string processedLine = line;

                if (!inBlockComment)
                {
                    // 处理行注释
                    int lineCommentIndex = line.IndexOf("//");
                    if (lineCommentIndex >= 0)
                    {
                        processedLine = line.Substring(0, lineCommentIndex).TrimEnd();
                    }

                    // 处理块注释开始
                    int blockStartIndex = line.IndexOf("/*");
                    if (blockStartIndex >= 0)
                    {
                        inBlockComment = true;
                        processedLine = line.Substring(0, blockStartIndex).TrimEnd();

                        // 检查块注释是否在同一行结束
                        int blockEndIndex = line.IndexOf("*/", blockStartIndex + 2);
                        if (blockEndIndex >= 0)
                        {
                            inBlockComment = false;
                            processedLine += line.Substring(blockEndIndex + 2);
                        }
                    }
                }
                else
                {
                    // 在块注释中，查找结束标记
                    int blockEndIndex = line.IndexOf("*/");
                    if (blockEndIndex >= 0)
                    {
                        inBlockComment = false;
                        processedLine = line.Substring(blockEndIndex + 2).TrimStart();
                    }
                    else
                    {
                        processedLine = string.Empty;
                    }
                }

                if (!string.IsNullOrWhiteSpace(processedLine))
                {
                    resultLines.Add(processedLine);
                }
            }

            return string.Join(Environment.NewLine, resultLines);
        }
    }
}
