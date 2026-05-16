using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

namespace MetBench_BLL
{
    // 验证支持率计算接口
    public class SupportRateCalculator
    {
        private MetamorphicRelationService _metamorphicRelationSerive;

        public SupportRateCalculator(MetamorphicRelationService metamorphicRelationSerive)
        {
            _metamorphicRelationSerive = metamorphicRelationSerive;
        }

        public async Task<ObservableCollection<MRRecommendationResult>> CalculateSupportRate(ObservableCollection<SimilarityResult> similarityResults)
        {
            if (similarityResults == null || similarityResults.Count == 0)
            {
                return null;
            }
            var targetProgram = similarityResults[0].TargetProgram;
            if (targetProgram == null)
            {
                return null;
            }
            FunctionProgram functionProgram = targetProgram as FunctionProgram;
            if (functionProgram == null)
            {
                return null;
            }
            // 目标程序复制到指定目录
            var isCopy = CopyFileToDestination(functionProgram);
            if (!isCopy)
            {
                return null;
            }
            // 相似度字典
            var similarityDic = new Dictionary<string, float>();
            var similarityResultsList = similarityResults.ToList();
            foreach (var similarityresult in similarityResultsList)
            {
                var similarity = similarityresult.Similarity;
                var applicationName = similarityresult.Application.Name;

                similarityDic.Add(applicationName, similarity);

            }

            var programName = functionProgram.FunctionName;
            var parameterRanges = functionProgram.ParameterRanges; // 如单参数(-10,10) 两参数((-20,20),(-10,20)) 三次参数((-20,20),(-10,20),(-30,30))
            var globalMinMax = GetGlobalMinMax(parameterRanges);
            var globalMin = globalMinMax.Min;
            var globalMax = globalMinMax.Max;

            // 处理应用程序对应的MR

            var threshold = "0.80";
            var applications = similarityResults.Select(s => s.Application).ToList();
            var allMrs = _metamorphicRelationSerive.GetAllMRs();
            var results = new ObservableCollection<MRRecommendationResult>();
            var syncLock = new object(); // 用于线程安全操作集合


            //var appmrsDicList = new List<Dictionary<string, (Application, List<MetamorphicRelation>)>>();
            //// 将候选程序及其蜕变关系转换成字典
            //foreach (var application in applications)
            //{
            //    var name = application.Name;
            //    // 要将代码解压到指定目录 InputPatternSympy
            //    var mrsList = mrs.Where(s => s.ApplicationName == name).ToList();
            //    // 创建新的字典并添加到列表中

            //    var newEntry = new Dictionary<string, (Application, List<MetamorphicRelation>)>
            //    {
            //        { name,(application, mrsList) }
            //    };

            //    appmrsDicList.Add(newEntry);
            //}

            // 循环验证MR支持率
            try
            {
                // Parallel.ForEachAsync(applications, async (app, cancellationToken) =>
                //{
                //    var appMrs = allMrs.Where(mr => mr.ApplicationName == app.Name).ToList();

                //    var mrs = new ObservableCollection<MRWithDecimal>();
                //    // 并行处理每个MR（异步）
                //    Parallel.ForEachAsync(appMrs, async (mr, ct) =>
                //    {
                //        try
                //        {
                //            var r_list = mr.InputPatterntosympy;
                //            var R_list = mr.OutputPatterntosympy;
                //            var File_name = programName;
                //            var MR_Input_dimension = mr.DimensionOfInputPattern;
                //            var MR_Output_dimension = mr.DimensionOfOutputPattern;
                //            var random_data_min_value = globalMin.ToString();
                //            var random_data_max_value = globalMax.ToString();
                //            var random_data_count = 1000.ToString();
                //            var MTthreshold = "1e-3";
                //            // 调用异步验证方法
                //            decimal supportRate = await AutorunMR2Async(
                //                r_list,
                //                R_list,
                //                File_name,
                //                MR_Input_dimension,
                //                MR_Output_dimension,
                //                random_data_min_value,
                //                random_data_max_value,
                //                random_data_count,
                //                MTthreshold
                //            );
                //            var issucess = supportRate - 0.80m >= 0.00m ? true : false;
                //            if (issucess)
                //            {
                //                lock (syncLock)
                //                {
                //                    var mrWithDecimal = new MRWithDecimal()
                //                    {
                //                        Relation = mr,
                //                        Value = supportRate
                //                    };
                //                    mrs.Add(mrWithDecimal);
                //                }
                //            }
                //        }
                //        catch (Exception ex)
                //        {
                //            Debug.WriteLine($"MR验证失败: {mr.ApplicationName}, 错误: {ex.Message}");
                //        }
                //    });
                //    results.Add(new MRRecommendationResult
                //    {
                //        Application = app,
                //        MetamorphicRelations = mrs,
                //        TargetProgram = targetProgram,
                //        Similarity = similarityDic[app.Name]

                //    });
                //});
                foreach (var app in applications)
                {
                    var appMrs = allMrs.Where(mr => mr.ApplicationName == app.Name).ToList();
                    var mrs = new ObservableCollection<MRWithDecimal>();

                    // 改为普通循环处理每个MR
                    foreach (var mr in appMrs)
                    {
                        try
                        {
                            var r_list = mr.InputPatterntosympy;
                            var R_list = mr.OutputPatterntosympy;
                            var File_name = programName;
                            var MR_Input_dimension = mr.DimensionOfInputPattern;
                            var MR_Output_dimension = mr.DimensionOfOutputPattern;
                            var random_data_min_value = globalMin.ToString();
                            var random_data_max_value = globalMax.ToString();
                            var random_data_count = 1000.ToString();
                            var MTthreshold = "1e-3";

                            // 调用异步验证方法（仍需await）
                            decimal supportRate = await AutorunMR2Async(
                                r_list,
                                R_list,
                                File_name,
                                MR_Input_dimension,
                                MR_Output_dimension,
                                random_data_min_value,
                                random_data_max_value,
                                random_data_count,
                                MTthreshold
                            );

                            var issucess = supportRate - 0.80m >= 0.00m;
                            if (issucess)
                            {
                                // 移除lock（单线程不再需要同步）
                                var mrWithDecimal = new MRWithDecimal()
                                {
                                    Relation = mr,
                                    Value = supportRate
                                };
                                mrs.Add(mrWithDecimal);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"MR验证失败: {mr.ApplicationName}, 错误: {ex.Message}");
                        }
                    }

                    if (mrs.Count > 0)
                    {
                        results.Add(new MRRecommendationResult
                        {
                            Application = app,
                            MetamorphicRelations = mrs,
                            TargetProgram = targetProgram,
                            Similarity = similarityDic[app.Name]
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"并行处理异常: {ex.Message}");
                return null;
            }


            return results;
        }

        // 运行蜕变测试执行脚本
        private async Task<decimal> AutorunMR2Async(string r_list, string R_list, string File_name, string MR_Input_dimension, string MR_Output_dimension, string random_data_min_value, string random_data_max_value, string random_data_count, string threshold)
        {
            var outputDimension = int.Parse(MR_Output_dimension);
            if (outputDimension > 1)
            {
                return 0.00m;
            }
            Assembly assembly = Assembly.GetEntryAssembly();
            AbsolutePathResolver absolutePathResolver = new AbsolutePathResolver();
            string solutionPath = absolutePathResolver.GetSolutionPath(assembly);

            // 使用引号括起整个路径  
            string escapedPath = $"\"{solutionPath}\"";

            // 获取Python路径  
            string pythonInPath = FindFirstPythonInPath();
            if (string.IsNullOrEmpty(pythonInPath)) return 0.00m;

            // 创建一个进程对象  
            Process process = new Process();
            string output;

            var passRate = 0.00m;
            try
            {
                // 设置进程启动信息  
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = $"{pythonInPath}\\python.exe",
                    Arguments = $"{escapedPath}\\MetBench_Python\\AutoRunBinaryMR.py " +
                                $"\"{r_list}\" " +
                                $"\"{R_list}\" " +
                                $"\"{File_name}\" " +
                                $"\"{MR_Input_dimension}\" " +
                                $"\"{MR_Output_dimension}\" " +
                                $"\"{random_data_min_value}\" " +
                                $"\"{random_data_max_value}\" " +
                                $"\"{random_data_count}\" " +
                                $"\"{threshold}\" ",
                    UseShellExecute = false, // 不使用操作系统shell启动进程  
                    CreateNoWindow = true, // 不创建窗口  
                    RedirectStandardOutput = true // 重定向输出流，以便从Python脚本中获取结果  
                };

                // 将启动信息应用到进程对象  
                process.StartInfo = startInfo;

                // 启动进程  
                process.Start();

                // 异步读取输出  
                output = await process.StandardOutput.ReadToEndAsync();

                // 等待进程执行完毕  
                await Task.Run(() => process.WaitForExit());

                string[] splitStrings = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (splitStrings != null && splitStrings.Length >0)
                {
                    // 通过比率字符串
                    string passrate = splitStrings[3];
                    // 失败比率字符串
                    string failurerate = splitStrings[4];
                    // 通过比率字符串
                    decimal passRateDecimal = decimal.Parse(passrate.TrimEnd('%')) / 100m; //如 1.00m
                    // 通过比率字符串
                    decimal failureRateDecimal = decimal.Parse(failurerate.TrimEnd('%')) / 100m;//如 0.00m

                    passRate = passRateDecimal;
                }

                return passRate; // 返回输出结果  
            }
            catch (Exception ex)
            {
                return 0.00m; // 返回空字符串表示发生异常  
            }
            finally
            {
                // 关闭进程  
                process.Close();
            }
        }

        // 将目标函数程序文件复制粘贴到目标目录
        private bool CopyFileToDestination(FunctionProgram functionProgram)
        {
            try
            {
                var sourceFilePath = functionProgram.FilePath;
                var fileName = functionProgram.FunctionName;
                // 检查源文件是否存在
                if (!File.Exists(sourceFilePath))
                {
                    Console.WriteLine("源文件不存在！");
                    return false;
                }

                //// 获取当前应用程序的基础目录
                //string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                //Console.WriteLine($"当前基础目录: {baseDirectory}");

                // 解压文件的存储路径（默认系统的Temp路径）
                string SystemTemppath = Path.GetTempPath() + "\\MetBench";

                // 存储路径下创建的存储文件夹名
                string folderunderpath = "MR_SUT";
                // 创建缓存目录
                string cacheFolderPath = Path.Combine(SystemTemppath, folderunderpath);
                Directory.CreateDirectory(cacheFolderPath);

                // 拼接目标路径
                string destinationPath = Path.Combine(cacheFolderPath, fileName);

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

        //寻找根目录的路径
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
        public string FindFirstPythonInPath()
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

        // 取参数范围的交集
        private (int Min, int Max) GetGlobalMinMax(string parameterRanges)
        {
            // 移除所有空白字符
            string cleanInput = parameterRanges.Replace(" ", "");

            // 处理单参数情况：(-10,10)
            if (!cleanInput.StartsWith("(("))
            {
                var singleRange = ParseSingleRange(cleanInput);
                return (singleRange.Min, singleRange.Max);
            }

            // 处理多参数情况：((-20,20),(-10,20),(-30,30))
            // 1. 移除最外层的括号
            string innerRanges = cleanInput.Substring(1, cleanInput.Length - 2);

            // 2. 分割各个参数范围
            var ranges = innerRanges.Split(new[] { "),(" }, StringSplitOptions.None)
                                   .Select(s => s.Trim('(', ')'))
                                   .Where(s => !string.IsNullOrEmpty(s))
                                   .Select(ParseSingleRange)
                                   .ToList();

            if (ranges.Count == 0)
                throw new ArgumentException("No valid parameter ranges found");

            // 3. 计算全局最小值和最大值（取交集）
            int globalMin = ranges.Max(r => r.Min); // 所有Min中的最大值
            int globalMax = ranges.Min(r => r.Max); // 所有Max中的最小值

            // 检查是否存在有效交集
            if (globalMin > globalMax)
            {
                return (-10, 10); // 用特殊的范围
                                  //throw new ArgumentException("No valid intersection exists for the given parameter ranges");
            }


            return (globalMin, globalMax);
        }

        private (int Min, int Max) ParseSingleRange(string range)
        {
            // 移除所有括号
            string cleanRange = range.Replace("(", "").Replace(")", "");

            var parts = cleanRange.Split(',');
            if (parts.Length != 2)
                throw new FormatException($"Invalid range format: {range}");

            if (!int.TryParse(parts[0], out int min))
                throw new FormatException($"Invalid min value: {parts[0]}");

            if (!int.TryParse(parts[1], out int max))
                throw new FormatException($"Invalid max value: {parts[1]}");

            if (min > max)
                throw new ArgumentException($"Invalid range: min ({min}) cannot be greater than max ({max})");

            return (min, max);
        }
    }
}
