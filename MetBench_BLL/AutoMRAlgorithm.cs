using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace MetBench_BLL
{
    // AutoMR算法实现
    public class AutoMRAlgorithm : IRecognitionAlgorithm
    {
        // 确认识别mr流程脚本运行结束的常量字符串 string型
        private const string ScriptCompletionFlag = "所有脚本执行完成";
        //返回的识别到的MR数据Csv文件路径
        //public DetectionResult Execute(TargetProgram program)
        //{
        //    //if (!program.ProgramType.Equals("Function"))
        //    //{
        //    //    return new DetectionResult()
        //    //    {
        //    //        Result = null,
        //    //        IsCompleted = false,
        //    //        AlgorithmType = "AutoMR"
        //    //    };
        //    //}
        //    //var targetProgram = program as FunctionProgram;
        //    //var a = targetProgram.ProgramType;
        //    //if (targetProgram == null)
        //    //{
        //    //    return new DetectionResult()
        //    //    {
        //    //        Result = null,
        //    //        IsCompleted = false,
        //    //        AlgorithmType = "AutoMR"
        //    //    };
        //    //}
        //    if (!program.ProgramType.Equals("Function") || !(program is FunctionProgram targetProgram))
        //    {
        //        return new DetectionResult()
        //        {
        //            Result = null,
        //            IsCompleted = false,
        //            AlgorithmType = "AutoMR"
        //        };
        //    }

        //    // 预处理 将目标程序复制粘贴到目标目录中
        //    var programPath = targetProgram.FilePath;
        //    var isCopy = CopyFileToDestination(programPath);
        //    if (!isCopy) 
        //    {
        //        return new DetectionResult()
        //        {
        //            Result = null,
        //            IsCompleted = false,
        //            AlgorithmType = "AutoMR"
        //        };
        //    }

        //    //获取项目根目录路径
        //    Assembly assembly = Assembly.GetEntryAssembly();
        //    AbsolutePathResolver absolutePathResolver = new AbsolutePathResolver();
        //    string solutionPath = absolutePathResolver.GetSolutionPath(assembly);


        //    // 使用引号括起整个路径
        //    string escapedPath = $"\"{solutionPath}\"";

        //    // 创建一个进程对象
        //    Process process = new Process();
        //    string pythonInPath = FindFirstPythonInPath();
        //    try
        //    {

        //        string input_program_filename = targetProgram.FunctionName;
        //        int input_param_count = targetProgram.ParameterCount;
        //        string frontend_input_ranges = targetProgram.ParameterRanges;
        //        string frontend_input_datatypes = targetProgram.ParameterTypes;

        //        // 设置进程启动信息
        //        ProcessStartInfo startInfo = new ProcessStartInfo();
        //        /*   startInfo.FileName = "python";*/// 指定Python解释器的路径（如果在环境变量中已配置，则可直接使用"python"）
        //        startInfo.FileName = pythonInPath + "\\python.exe";
        //        //构建Arguments属性
        //        startInfo.Arguments = $"{escapedPath}\\MetBench_Python\\AutoMRDetector\\auto_mr_detector-3.py " +
        //                              $"\"{input_program_filename}\" " +
        //                              $"\"{input_param_count}\" " +
        //                              $"\"{frontend_input_ranges}\" " +
        //                              $"\"{frontend_input_datatypes}\" " ;

        //        // 设置进程启动信息的一些属性
        //        startInfo.UseShellExecute = false; // 不使用操作系统shell启动进程
        //        startInfo.CreateNoWindow = true; // 不创建窗口
        //        startInfo.RedirectStandardOutput = true; // 重定向输出流，以便从Python脚本中获取结果
        //        // 将启动信息应用到进程对象
        //        process.StartInfo = startInfo;

        //        // 启动进程
        //        process.Start();

        //        // 等待进程执行完毕
        //        process.WaitForExit();

        //        StringBuilder relevantOutput = new StringBuilder();
        //        string lastRelevantLine = string.Empty;
        //        string mrCsvPath = string.Empty;
        //        //var string1 = process.StandardOutput.ReadToEnd();
        //        //Console.WriteLine(string1 );    
        //        using (StreamReader reader = process.StandardOutput)
        //        {
        //            while (!reader.EndOfStream)
        //            {
        //                string line = reader.ReadLine();
        //                Console.WriteLine(line);

        //                // 实时处理逻辑（示例）
        //                if (line.Contains(ScriptCompletionFlag))
        //                {
        //                    lastRelevantLine = line; // 始终记录最后一次出现的标记行
        //                    relevantOutput.Clear();  // 可选项：清除之前缓存的非关键输出
        //                    relevantOutput.AppendLine(line);
        //                    var resultstr = !string.IsNullOrEmpty(lastRelevantLine)
        //                        ? lastRelevantLine
        //                        : relevantOutput.ToString(); 
        //                    var fuctionName = Path.GetFileNameWithoutExtension(input_program_filename);
        //                    mrCsvPath = FindMRResultsCsv(fuctionName);

        //                }

        //            }
        //        }
        //        return new DetectionResult()
        //                    {
        //                        Result = new Dictionary<string, object> 
        //                        {
        //                           {"MRCsvPath",mrCsvPath},
        //                           {"Program",targetProgram}
        //                        },
        //                        IsCompleted = true,
        //                        AlgorithmType = "AutoMR"
        //                    };
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("发生异常：" + ex.Message);
        //        return new DetectionResult()
        //        {
        //            Result = null,
        //            IsCompleted = false,
        //            AlgorithmType = "AutoMR"
        //        };
        //    }
        //    finally
        //    {
        //        // 关闭进程
        //        process.Close();
        //    }
        //}


        // 实际执行逻辑的私有异步方法

        public DetectionResult Execute(TargetProgram program)
        {
            if (!program.ProgramType.Equals("Function") || !(program is FunctionProgram targetProgram))
            {
                return new DetectionResult()
                {
                    Result = null,
                    IsCompleted = false,
                    AlgorithmType = "AutoMR"
                };
            }

            //// 同步阻塞调用异步核心逻辑
            //return ExecuteCoreAsync(targetProgram).GetAwaiter().GetResult();
            // 启动独立任务上下文避免死锁
            return Task.Run(() => ExecuteCoreAsync(targetProgram)).GetAwaiter().GetResult();
        }
        private async Task<DetectionResult> ExecuteCoreAsync(FunctionProgram targetProgram)
        {
            try
            {
                // 保持原有的预处理逻辑
                if (!CopyFileToDestination(targetProgram.FilePath))
                {
                    return new DetectionResult()
                    {
                        Result = null,
                        IsCompleted = false,
                        AlgorithmType = "AutoMR"
                    };
                }

                // 调用完整的异步Python脚本执行
                var mrCsvPath = await RunPythonScriptAsync(targetProgram);

                // 保持原有的返回结构
                return new DetectionResult()
                {
                    Result = new Dictionary<string, object>
            {
                {"MRCsvPath", mrCsvPath},
                {"Program", targetProgram}
            },
                    IsCompleted = true,
                    AlgorithmType = "AutoMR"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生异常：" + ex.Message);
                return new DetectionResult()
                {
                    Result = null,
                    IsCompleted = false,
                    AlgorithmType = "AutoMR"
                };
            }
        }

        // 异步运行Python脚本
        private async Task<string> RunPythonScriptAsync(FunctionProgram targetProgram)
        {
            var solutionPath = new AbsolutePathResolver().GetSolutionPath(Assembly.GetEntryAssembly());
            var escapedPath = $"\"{solutionPath}\"";
            var pythonExePath = FindFirstPythonInPath() + "\\python.exe";

            using (var process = new Process())
            {
                // 保持原有参数构造逻辑
                string input_program_filename = targetProgram.FunctionName;
                int input_param_count = targetProgram.ParameterCount;
                string frontend_input_ranges = targetProgram.ParameterRanges;
                string frontend_input_datatypes = targetProgram.ParameterTypes;

                process.StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExePath,
                    Arguments = $"{escapedPath}\\MetBench_Python\\AutoMRDetector\\auto_mr_detector-3.py " +
                               $"\"{input_program_filename}\" " +
                               $"\"{input_param_count}\" " +
                               $"\"{frontend_input_ranges}\" " +
                               $"\"{frontend_input_datatypes}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                process.Start();

                // 异步等待进程退出
                await process.WaitForExitAsync();

                // 完全保留原有的输出解析逻辑
                StringBuilder relevantOutput = new StringBuilder();
                string lastRelevantLine = string.Empty;
                string mrCsvPath = string.Empty;
                const string ScriptCompletionFlag = "脚本执行完成"; // 根据实际情况调整标志

                using (StreamReader reader = process.StandardOutput)
                {
                    while (!reader.EndOfStream)
                    {
                        string line = await reader.ReadLineAsync();
                        Console.WriteLine(line); // 保持实时输出

                        if (line.Contains(ScriptCompletionFlag))
                        {
                            lastRelevantLine = line;
                            relevantOutput.Clear();
                            relevantOutput.AppendLine(line);

                            var resultstr = !string.IsNullOrEmpty(lastRelevantLine)
                                ? lastRelevantLine
                                : relevantOutput.ToString();

                            // 保持原有的路径查找逻辑
                            var fuctionName = Path.GetFileNameWithoutExtension(input_program_filename);
                            mrCsvPath = FindMRResultsCsv(fuctionName);
                        }
                    }
                }

                return mrCsvPath;
            }
        }

        // 寻找项目.sln文件所在路径，即项目跟目录
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

        // 获取本机的Python解释器的环境变量路径  
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
                //Console.WriteLine($"第一个 Python 可执行文件目录：{pythonPath}");
                return pythonPath;
            }
            else
            {
                Console.WriteLine($"没有 Python 可执行文件目录");
                return null;
            }
        }

        // 将目标函数程序文件复制粘贴到目标目录（MetBench_PythonAutoMR/AutoMRDetector/fuction）
        private bool CopyFileToDestination(string sourceFilePath)
        {
            try
            {
                // 检查源文件是否存在
                if (!File.Exists(sourceFilePath))
                {
                    Console.WriteLine("源文件不存在！");
                    return false;
                }

                // 获取当前应用程序的基础目录
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"当前基础目录: {baseDirectory}");

                string projectRoot = FindSolutionRoot();
                if (projectRoot == null)
                {
                    Console.WriteLine("无法找到项目根目录！");
                    return false;
                }
                Console.WriteLine($"找到项目根目录: {projectRoot}");

                // 构建目标目录路径
                string destinationDirectory = Path.Combine(
                    projectRoot,
                    "MetBench_Python",
                    "AutoMRDetector",
                    "function");

                // 确保目标目录存在
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                // 获取文件名
                string fileName = Path.GetFileName(sourceFilePath);

                // 拼接目标路径
                string destinationPath = Path.Combine(destinationDirectory, fileName);

                // 复制文件（覆盖已存在的文件）
                File.Copy(sourceFilePath, destinationPath, true);

                Console.WriteLine($"文件已成功复制到：{destinationPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"复制文件时出错：{ex.Message}");
                return false;
            }
        }

        // 获取目标程序识别到的MR数据的Csv文件路径
        public string FindMRResultsCsv(string functionName)
        {
            // 参数校验
            if (string.IsNullOrWhiteSpace(functionName))
            {
                throw new ArgumentException("程序函数名称不能为空", nameof(functionName));
            }
            try
            {
                // 1. 确定基础路径
                string basePath = FindSolutionRoot();

                // 2. 构建完整源路径
                string sourcePath = Path.Combine(
                    basePath,
                    "Metbench_Python",
                    "AutoMRDetector",
                    "output",
                    "MR",
                    $"{functionName}(x)",
                    "successed_mr");

                // 3. 获取所有匹配的非LaTeX CSV文件
                var allFiles = Directory.GetFiles(sourcePath, "*.csv")
                    .Where(f => !f.EndsWith("_latex.csv", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (allFiles.Count == 0)
                {
                    throw new FileNotFoundException($"找不到{functionName}的MR结果文件");
                }

                // 4. 根据文件最后修改时间获取最新文件
                var latestFile = allFiles
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
               
                Console.WriteLine($"使用最新文件(按修改时间): {latestFile.Name} (修改时间: {latestFile.LastWriteTime})");
                var latestFilePath = latestFile.FullName;
                return latestFilePath;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"导出MR结果文件失败: {ex.Message}");
                return string.Empty;
            }

        }

        // 获取目标程序识别到的MR数据的Csv文件路径 异步实现
        public async Task<string> FindMRResultsCsvAsync(string functionName)
        {
            if (string.IsNullOrWhiteSpace(functionName))
                throw new ArgumentException("程序函数名称不能为空", nameof(functionName));

            try
            {
                string basePath = FindSolutionRoot();
                string sourcePath = Path.Combine(
                    basePath, "Metbench_Python", "AutoMRDetector", "output", "MR",
                    $"{functionName}(x)", "successed_mr");

                // 异步获取文件列表
                var allFiles = (await Task.Run(() => Directory.GetFiles(sourcePath, "*.csv")))
                    .Where(f => !f.EndsWith("_latex.csv", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (allFiles.Count == 0)
                    throw new FileNotFoundException($"找不到{functionName}的MR结果文件");

                // 异步获取文件信息并排序
                var latestFile = await Task.Run(() =>
                    allFiles.Select(f => new FileInfo(f))
                           .OrderByDescending(f => f.LastWriteTime)
                           .FirstOrDefault()
                );

                Console.WriteLine($"使用最新文件: {latestFile.Name} (修改时间: {latestFile.LastWriteTime})");
                return latestFile.FullName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导出MR结果文件失败: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
