using MetBench_Domain;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace MetBench_BLL
{
    public class AutoMRParser : IResultParser
    {
        private ObservableCollection<MRDetectorCollection> mRDetectorCollections = new ObservableCollection<MRDetectorCollection>();
        public ObservableCollection<MRDetectorCollection> GetMRDetectorCollections() { return mRDetectorCollections; }
        // 寻找根目录的路径
        private static string FindSolutionRoot()
        {
            // 从当前程序集位置向上查找，直到找到解决方案根目录
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                if (Directory.GetFiles(directory.FullName, "*.sln").Any())
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("无法找到解决方案根目录");
        }
        // 获取Python的环境变量路径  
        private string FindFirstPythonInPath()
        {
            // 在 PATH 中查找包含 "Python" 的目录  
            string pathVariable = Environment.GetEnvironmentVariable("PATH");
            // 以分号分隔 PATH 中的各个目录  
            string[] directories = pathVariable.Split(';');
            string pythonPath = null;

            // 在每个目录中查找包含 "Python" 的子目录  
            foreach (var directory in directories)
            {
                if (directory.Contains("Python", StringComparison.OrdinalIgnoreCase))
                {
                    pythonPath = directory;
                    break;
                }
            }

            if (pythonPath != null)
            {
                Console.WriteLine($"第一个 Python 可执行文件目录：{pythonPath}");
                return pythonPath;
            }
            else
            {
                Console.WriteLine($"没有 Python 可执行文件目录");
                return null;
            }
        }

        //Result = new Dictionary<string, object> 
        //    {
        //    {"MRCsvPath",mrCsvPath}
        //    },
        //IsCompleted = false,
        //AlgorithmType = "AutoMR"

        // 解析MR数据
        public ParsedResult Parse(DetectionResult detectionResult)
        {
            //var algorithmType = detectionResult.AlgorithmType;

            //if (!algorithmType.Equals("AutoMR"))
            //{
            //    return null;
            //}

            //if (!detectionResult.Result.TryGetValue("MRCsvPath", out var mrCsvPath))
            //{
            //    return null;
            //}

            //var csvPath = detectionResult.Result["MRCsvPath"] as string;
            //var targetProgram = detectionResult.Result["Program"] as FunctionProgram;
            ////如sin.py
            //var programName = targetProgram.FunctionName;


            ////采用异步函数实现
            //// 格式转换
            //var mrLatexCsvPath = MRStringToLatexAsync(csvPath).GetAwaiter().GetResult();
            //// 解析提取Csv文件中MR数据
            //var mrDetectorCollection = ReadMRResultsFromFileAsync(mrLatexCsvPath).GetAwaiter().GetResult();
            //// 转换符合存储格式的MR数据
            //var results = ProduceMRsAsync(mrDetectorCollection, targetProgram).GetAwaiter().GetResult();

            //return results;
            // 参数校验（线程安全）
            if (detectionResult == null ||
                !"AutoMR".Equals(detectionResult.AlgorithmType, StringComparison.OrdinalIgnoreCase) ||
                !detectionResult.Result.TryGetValue("MRCsvPath", out var csvObj) ||
                csvObj is not string csvPath ||
                !detectionResult.Result.TryGetValue("Program", out var programObj) ||
                programObj is not FunctionProgram targetProgram)
            {
                return null;
            }
            // 同步调用异步方法（安全封装）
            static TResult RunSync<TResult>(Func<Task<TResult>> asyncFunc)
            {
                return Task.Run(async () => await asyncFunc().ConfigureAwait(false))
                         .GetAwaiter()
                         .GetResult();
            }

            try
            {
                // 执行异步操作流水线
                var mrLatexCsvPath = RunSync(() => MRStringToLatexAsync(csvPath));
                var mrDetectorCollection = RunSync(() => ReadMRResultsFromFileAsync(mrLatexCsvPath));
                return RunSync(() => ProduceMRsAsync(mrDetectorCollection, targetProgram));
            }
            catch (Exception ex)
            {
                // 日志记录（实际项目中替换为正式日志）
                Debug.WriteLine($"Parse failed: {ex.Message}");
                return null;
            }
        }

        // 将识别到的MR数据的Csv文件进行Latex格式转换
        private string MRStringToLatex(string csvPath)
        {
            Assembly assembly = Assembly.GetEntryAssembly();
            AbsolutePathResolver absolutePathResolver = new AbsolutePathResolver();
            string solutionPath = absolutePathResolver.GetSolutionPath(assembly);

            // 使用引号括起整个路径
            string escapedPath = $"\"{solutionPath}\"";

            // 创建一个进程对象
            Process process = new Process();
            string pythonInPath = FindFirstPythonInPath();
            try
            {
                // 设置进程启动信息
                ProcessStartInfo startInfo = new ProcessStartInfo();
                /*   startInfo.FileName = "python";*/// 指定Python解释器的路径（如果在环境变量中已配置，则可直接使用"python"）
                startInfo.FileName = pythonInPath + "\\python.exe";
                //构建Arguments属性
                startInfo.Arguments = $"{escapedPath}\\MetBench_Python\\AutoMRDetector\\6-expressionlatex.py " +
                                $"\"{csvPath}\" "
                                     ;

                // 设置进程启动信息的一些属性
                startInfo.UseShellExecute = false; // 不使用操作系统shell启动进程
                startInfo.CreateNoWindow = true; // 不创建窗口
                startInfo.RedirectStandardOutput = true; // 重定向输出流，以便从Python脚本中获取结果
                // 将启动信息应用到进程对象
                process.StartInfo = startInfo;

                // 启动进程
                process.Start();

                // 等待进程执行完毕
                process.WaitForExit();

                StringBuilder relevantOutput = new StringBuilder();
                string generatedCsvPath = string.Empty;
                string lastRelevantLine = string.Empty;
                const string COMPLETION_FLAG = "已生成新文件: ";
                const string SUCCESS_FLAG = "成功处理 2 个表达式（r和R列）";
                bool successFlagFound = false;

                using (StreamReader reader = process.StandardOutput)
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();

                        // 检测成功标志
                        if (line.Contains(SUCCESS_FLAG))
                        {
                            successFlagFound = true;
                        }

                        // 捕获最终生成的CSV路径
                        if (line.StartsWith(COMPLETION_FLAG))
                        {
                            generatedCsvPath = line.Substring(COMPLETION_FLAG.Length).Trim();

                        }
                    }
                }
                //返回生成的Latex格式的MR数据的Csv文件
                return generatedCsvPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生异常：" + ex.Message);
                return string.Empty; // 返回空字符串表示发生异常
            }
            finally
            {
                // 关闭进程
                process.Close();
            }
        }

        // 将识别到的MR数据的Csv文件进行Latex格式转换（异步实现）
        private async Task<string> MRStringToLatexAsync(string csvPath)
        {
            Assembly assembly = Assembly.GetEntryAssembly();
            AbsolutePathResolver absolutePathResolver = new AbsolutePathResolver();
            string solutionPath = absolutePathResolver.GetSolutionPath(assembly);

            string escapedPath = $"\"{solutionPath}\"";
            Process process = new Process();
            string pythonInPath = FindFirstPythonInPath();

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = pythonInPath + "\\python.exe";
                startInfo.Arguments = $"{escapedPath}\\MetBench_Python\\AutoMRDetector\\6-expressionlatex.py \"{csvPath}\"";
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardOutput = true;

                process.StartInfo = startInfo;
                process.Start();

                // 使用异步等待进程退出
                await process.WaitForExitAsync();

                StringBuilder relevantOutput = new StringBuilder();
                string generatedCsvPath = string.Empty;
                const string COMPLETION_FLAG = "已生成新文件: ";
                const string SUCCESS_FLAG = "成功处理 2 个表达式（r和R列）";
                bool successFlagFound = false;

                using (StreamReader reader = process.StandardOutput)
                {
                    while (!reader.EndOfStream)
                    {
                        string line = await reader.ReadLineAsync(); // 异步读取行

                        if (line.Contains(SUCCESS_FLAG))
                        {
                            successFlagFound = true;
                        }

                        if (line.StartsWith(COMPLETION_FLAG))
                        {
                            generatedCsvPath = line.Substring(COMPLETION_FLAG.Length).Trim();
                        }
                    }
                }

                return generatedCsvPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生异常：" + ex.Message);
                return string.Empty;
            }
            finally
            {
                process.Close();
            }
        }

        //解析提取Csv文件中MR数据 MRDetectorCollection为中间类型
        private ObservableCollection<MRDetectorCollection> ReadMRResultsFromFile(string filePath)
        {
            var results = new ObservableCollection<MRDetectorCollection>();
            var lines = File.ReadAllLines(filePath).Skip(1); // 跳过标题行

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 6) continue;

                // 处理目标程序的(x)字符
                string appName = parts[0].Trim('"').TrimEnd();
                if (appName.EndsWith("(x)"))
                {
                    appName = appName.Substring(0, appName.Length - 3);
                }

                var result = new MRDetectorCollection
                {
                    ApplicationName = appName,
                    InputPattern = parts[1].Trim('"'),
                    OutputPattern = parts[2].Trim('"'),
                    DimensionOfInputPattern = parts[3].Trim('"'),
                    TrueRatio = parts[5]
                };

                var a = result.TrueRatio;
                results.Add(result);
            }
            return results;
        }

        //解析提取Csv文件中MR数据 MRDetectorCollection为中间类型（异步实现）
        private async Task<ObservableCollection<MRDetectorCollection>> ReadMRResultsFromFileAsync(string filePath)
        {
            var results = new ObservableCollection<MRDetectorCollection>();
            var lines = (await File.ReadAllLinesAsync(filePath)).Skip(1); // 异步读取文件

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 6) continue;

                string appName = parts[0].Trim('"').TrimEnd();
                if (appName.EndsWith("(x)"))
                {
                    appName = appName.Substring(0, appName.Length - 3);
                }

                var result = new MRDetectorCollection
                {
                    ApplicationName = appName,
                    InputPattern = parts[1].Trim('"'),
                    OutputPattern = parts[2].Trim('"'),
                    DimensionOfInputPattern = parts[3].Trim('"'),
                    TrueRatio = parts[5]
                };
                var latextosympy_Await = new Latextosympy_Await();
                string inputPatternimagepath = await latextosympy_Await.LatextoImageAsync(result.InputPattern);
                result.InputPatternimagepath = inputPatternimagepath;

                string outputPatternimagepath = await latextosympy_Await.LatextoImageAsync(result.OutputPattern);
                result.OutputPatternimagepath = outputPatternimagepath;
                results.Add(result);
            }
            mRDetectorCollections = results;
            return results;
        }

        // 获取关系中的元数
        private string GetRelationType(TargetProgram targetProgram)
        {
            if (targetProgram==null) 
            {
                return string.Empty;
            }
            if (!(targetProgram is FunctionProgram)) 
            {
                return string.Empty;
            }

            FunctionProgram program = targetProgram as FunctionProgram;

            if (program.FunctionName != string.Empty)
            {
                var paramcount = program.ParameterCount;
                if (paramcount == 1)
                {
                    return "二元关系";
                }
                else if (paramcount == 2)
                {
                    return "三元关系";
                }
                else
                {
                    return "多元关系";
                }
            }
            else
            {
                return "二元关系";
            }
        }

        // 转换为解析数据类型的对象ParsedResult
        private ParsedResult ProduceMRs(ObservableCollection<MRDetectorCollection> mrDetectorCollection,TargetProgram targetProgram)
        {
            if (targetProgram == null) 
            {
                return null;
            }
            var result = new ParsedResult();
            var program = targetProgram as FunctionProgram;
            if (mrDetectorCollection != null && program != null)
            {
                var mrs = new ObservableCollection<MetamorphicRelation>();
                var mrDetectorList = mrDetectorCollection.ToList();
                var language = program.GetLanguageName();
                foreach (var mrDetector in mrDetectorCollection)
                {
                    var orderOfMR = GetRelationType(targetProgram);
                    var inputPattern = mrDetector.InputPattern;
                    var outputPattern = mrDetector.OutputPattern;
                    var appNameel = mrDetector.ApplicationName;
                    var applicationName = $"{appNameel}-{language.ToLower()}";
                    var dimensionOfInputPattern = mrDetector.DimensionOfInputPattern;
                    var dimensionOfOutputPattern = "1";//默认为1
                    Latextosympy latextosympy = new Latextosympy();
                    var inputPatterntosympy =  latextosympy.latextosympy(inputPattern);
                    var outputPatterntosympy = latextosympy.latextosympy(outputPattern);

                    var mr = new MetamorphicRelation
                    {
                        OrderOfMR = orderOfMR,
                        InputPattern = inputPattern,
                        OutputPattern = outputPattern,
                        InputPatterntosympy = inputPatterntosympy,
                        OutputPatterntosympy = outputPatterntosympy,
                        ApplicationName = applicationName,
                        DimensionOfInputPattern = dimensionOfInputPattern,
                        DimensionOfOutputPattern = dimensionOfOutputPattern,
                        Granularity="Unit",
                        Hierarchy="Code",
                        Operator="Equation",
                        Expression="Linear"
                    };
                    mrs.Add(mr);
                }
                string fuctionName = program.FunctionName.Trim('"').TrimEnd();
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fuctionName);
                string appName = $"{fileNameWithoutExtension}-{language.ToLower()}";
                var code = CompressFileToBytes(program.FilePath);
                var application = new Application() 
                {
                    Name = appName,
                    ProgrammingLanguage = language,
                    Code = code,
                    CodeName = program.FunctionName,
                };
                result = new ParsedResult()
                {
                    ProgramName = appName,
                    Application = application,
                    MetamorphicRelations = mrs
                };
            }
            return result;
        }

        // 转换为解析数据类型的对象ParsedResult （异步实现）
        private async Task<ParsedResult> ProduceMRsAsync(ObservableCollection<MRDetectorCollection> mrDetectorCollection, TargetProgram targetProgram)
        {
            if (targetProgram == null)
            {
                return null;
            }

            var result = new ParsedResult();
            var program = targetProgram as FunctionProgram;

            if (mrDetectorCollection != null && program != null)
            {
                var mrs = new ObservableCollection<MetamorphicRelation>();
                var language = program.GetLanguageName();

                foreach (var mrDetector in mrDetectorCollection)
                {
                    var orderOfMR = GetRelationType(targetProgram);
                    var inputPattern = mrDetector.InputPattern;
                    var outputPattern = mrDetector.OutputPattern;
                    var appNameel = mrDetector.ApplicationName;
                    var applicationName = $"{appNameel}-{language.ToLower()}";
                    var dimensionOfInputPattern = mrDetector.DimensionOfInputPattern;
                    var dimensionOfOutputPattern = "1";

                    Latextosympy_Await latextosympy = new Latextosympy_Await();
                    var inputPatterntosympy = await latextosympy.LatextosympyAsync(inputPattern);
                    var outputPatterntosympy = await latextosympy.LatextosympyAsync(outputPattern);

                    var mr = new MetamorphicRelation
                    {
                        OrderOfMR = orderOfMR,
                        InputPattern = inputPattern,
                        OutputPattern = outputPattern,
                        InputPatterntosympy = inputPatterntosympy,
                        OutputPatterntosympy = outputPatterntosympy,
                        ApplicationName = applicationName,
                        DimensionOfInputPattern = dimensionOfInputPattern,
                        DimensionOfOutputPattern = dimensionOfOutputPattern,
                    };
                    mrs.Add(mr);
                }

                string fuctionName = program.FunctionName.Trim('"').TrimEnd();
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fuctionName);
                string appName = $"{fileNameWithoutExtension}-{language.ToLower()}";
                var code =  CompressFileToBytes(program.FilePath); // 异步压缩文件

                var application = new Application()
                {
                    Name = appName,
                    ProgrammingLanguage = language,
                    Code = code,
                    CodeName = program.FunctionName,
                };

                result = new ParsedResult()
                {
                    ProgramName = appName,
                    Application = application,
                    MetamorphicRelations = mrs
                };
            }
            return result;
        }

        // 压缩目标程序
        private byte[] CompressFileToBytes(string filePath)
        {
            // 参数验证
            if (!File.Exists(filePath))
                throw new FileNotFoundException("源文件不存在", filePath);

            // 创建内存流存放压缩数据
            using (var memoryStream = new MemoryStream())
            {
                // 创建Zip存档到内存流
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // 添加文件到Zip（保持原文件名）
                    var entry = zipArchive.CreateEntry(Path.GetFileName(filePath));

                    // 写入文件内容
                    using (var entryStream = entry.Open())
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        fileStream.CopyTo(entryStream);
                    }
                }

                // 返回内存流中的字节数组
                return memoryStream.ToArray();
            }
        }

    }
}
