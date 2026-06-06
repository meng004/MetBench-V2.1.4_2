using MetBench_Domain;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace MetBench_BLL
{
    public class SemanticSimilarityDetector : ISemanticSimilarityDetector
    {
        private ApplicationService _applicationSerive;
        public SemanticSimilarityDetector(ApplicationService applicationSerive)
        {
            _applicationSerive = applicationSerive;
        }

        public ObservableCollection<SimilarityResult> CalculateSimilarity(TargetProgram targetProgram, ObservableCollection<Application> candidateProgrms)
        {
            // 实现语义相似度计算逻辑
            if (targetProgram == null || candidateProgrms == null)
            {
                return null;
            }
            var program = targetProgram as FunctionProgram;
            if (program == null)
            {
                // 目前只支持函数程序代码
                return null;
            }
            var filePath = program.FilePath;
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
                var hasTargetProgram = CopyFileToDestination(filePath);
                var haveCandidatePrograms = true;
                var applicationList = candidateProgrms.ToList();
                var CandidateProgramcount = applicationList.Count;
                var count = 0;//用于计数的
                foreach (var application in applicationList)
                {
                    var haveCandidateProgram = RunSync(() => CopyCandidateProgrmsToDestination(application));
                    if (haveCandidateProgram) { count++; };
                }
                if (count == 0)
                {
                    haveCandidatePrograms = false;
                }


                return RunSync(() => ExcuteSimilarityDetection(targetProgram, candidateProgrms, hasTargetProgram, haveCandidatePrograms));
            }
            catch (Exception ex)
            {
                // 日志记录（实际项目中替换为正式日志）
                Debug.WriteLine($"Parse failed: {ex.Message}");
                return null;
            }
        }

        private async Task<ObservableCollection<SimilarityResult>> ExcuteSimilarityDetection(TargetProgram targetProgram, ObservableCollection<Application> candidateProgrms, bool hasTargetProgram, bool haveCandidatePrograms)
        {
            if (!(hasTargetProgram && haveCandidatePrograms))
            {
                return null;
            }
            var solutionPath = new AbsolutePathResolver().GetSolutionPath(Assembly.GetEntryAssembly());
            var escapedPath = $"\"{solutionPath}\"";
            var pythonExePath = FindFirstPythonInPath() + "\\python.exe";

            string similarityResultCsvPath = string.Empty;
            using (var process = new Process())
            {


                process.StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExePath,
                    Arguments = $"{escapedPath}\\MetBench_Python\\graphclone\\baselines\\graph_test.py ",
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
                const string ScriptCompletionFlag = "保存语义相似检测结果文件路径是"; // 根据实际情况调整标志
                using (StreamReader reader = process.StandardOutput)
                {
                    while (!reader.EndOfStream)
                    {
                        string line = await reader.ReadLineAsync();
                        if (line.Contains(ScriptCompletionFlag))
                        {
                            lastRelevantLine = line;
                            relevantOutput.Clear();
                            relevantOutput.AppendLine(line);
                            var resultstr = !string.IsNullOrEmpty(lastRelevantLine)
                                ? lastRelevantLine
                                : relevantOutput.ToString();

                            string[] parts = line.Split(new[] { ScriptCompletionFlag }, StringSplitOptions.None);
                            // 保存相似结果Csv文件路径
                            similarityResultCsvPath = parts.Length > 1 ? parts[1] : string.Empty;

                            if (similarityResultCsvPath == string.Empty)
                            {
                                return null;
                            }

                        }
                    }
                }
            }
            // 转换成 SimilarityResult对象

            if (!File.Exists(similarityResultCsvPath))
            {
                return null;
            }
            var results = new ObservableCollection<SimilarityResult>();
            var lines = File.ReadAllLines(similarityResultCsvPath).Skip(1); // 跳过标题行
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 3) continue;

                string targetProgramFileFullName = parts[0].Trim('"');
                string candidateProgramFileFullName = parts[1].Trim('"');
                //string similarity = parts[2].Trim('"');
                float similarity = float.Parse(parts[2].Trim('"'));

                string applicationName = Path.GetFileNameWithoutExtension(candidateProgramFileFullName);

                var applicationId = _applicationSerive.GetApplicationId(applicationName);
                var application = _applicationSerive.GetApplication(applicationId);

                var result = new SimilarityResult()
                {
                    TargetProgram = targetProgram,
                    Application = application,
                    Similarity = similarity,
                };
                results.Add(result);
            }
            return results;
        }

        // 将目标函数程序文件复制粘贴到目标目录（MetBench_PythonAutoMR/graphclone/baselines/original）
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
                //Console.WriteLine($"当前基础目录: {baseDirectory}");

                string projectRoot = FindSolutionRoot();
                if (projectRoot == null)
                {
                    Console.WriteLine("无法找到项目根目录！");
                    return false;
                }
                //Console.WriteLine($"找到项目根目录: {projectRoot}");

                // 构建目标目录路径
                string destinationDirectory = Path.Combine(
                    projectRoot,
                    "MetBench_Python",
                    "graphclone",
                    "baselines",
                    "original"
                    );

                //// 确保目标目录存在
                //if (!Directory.Exists(destinationDirectory))
                //{
                //    // 不存在则创建该文件目录
                //    Directory.CreateDirectory(destinationDirectory);
                //}

                // 确保目标目录存在且为空
                if (Directory.Exists(destinationDirectory))
                {
                    try
                    {
                        // 删除整个目录（包括所有内容和子目录）
                        Directory.Delete(destinationDirectory, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"删除目录时出错: {ex.Message}");
                    }
                }

                // 重新创建目录（无论之前是否存在）
                Directory.CreateDirectory(destinationDirectory);
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

        // 将候选程序传输到候选文件目录
        // 注：调用方走 RunSync(Task<bool>) 包装，需保留 Task<bool> 返回类型；
        // 本方法体内无 await，故移除 async 关键字（修 CS1998 假异步）。
        private Task<bool> CopyCandidateProgrmsToDestination(Application application)
        {
            if (application == null || application.Code == null)
            {
                return Task.FromResult(false);
            }

            string projectRoot = FindSolutionRoot();
            if (projectRoot == null)
            {
                Console.WriteLine("无法找到项目根目录！");
                return Task.FromResult(false);
            }
            //Console.WriteLine($"找到项目根目录: {projectRoot}");

            // 构建目标目录路径
            string destinationDirectory = Path.Combine(
                projectRoot,
                "MetBench_Python",
                "graphclone",
                "baselines",
                "candidate"
                );

            var isExtract = ExtractApplicationZip(application, destinationDirectory);
            return Task.FromResult(isExtract);
        }

        private bool ExtractApplicationZip(Application application, string destinationDirectory)
        {
            // 确保目标目录存在
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            try
            {
                // 程序压缩文件字节
                var zipFileData = _applicationSerive.GetZipCodeFileData(application);
                // 转换成内存流
                using (var memoryStream = new MemoryStream(zipFileData))
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
                {
                    //// 首先检查所有文件是否已存在
                    //foreach (ZipArchiveEntry entry in archive.Entries)
                    //{
                    //    if (string.IsNullOrEmpty(entry.Name)) // 跳过目录项
                    //        continue;

                    //    string filePath = Path.Combine(destinationDirectory, entry.FullName);
                    //    if (!File.Exists(filePath))
                    //    {
                    //        allFilesExist = false;
                    //        break; // 只要有一个文件不存在就需要解压
                    //    }
                    //}

                    //// 如果所有文件都已存在，直接返回true
                    //if (allFilesExist)
                    //{
                    //    return true;
                    //}

                    //// 否则执行解压
                    //foreach (ZipArchiveEntry entry in archive.Entries)
                    //{
                    //    if (string.IsNullOrEmpty(entry.Name)) // 跳过目录项
                    //        continue;

                    //    var name = application.Name;
                    //    //var extension = Path.GetExtension(entry.FullName);
                    //    //var fileFullName = Path.Combine(name,extension);
                    //    string fileName = Path.GetFileName(entry.FullName);
                    //    var extension = Path.GetExtension(fileName);
                    //    var newfileName = $"{name}{extension}";
                    //    string filePath = Path.Combine(destinationDirectory, newfileName);

                    //    // 确保目标目录存在
                    //    string directoryPath = Path.GetDirectoryName(filePath);
                    //    if (!Directory.Exists(directoryPath))
                    //    {
                    //        Directory.CreateDirectory(directoryPath);
                    //    }

                    //    // 解压文件
                    //    entry.ExtractToFile(filePath, overwrite: true);
                    //}
                    bool needUnzip = false;
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))// 跳过目录项
                            continue;

                        // 使用与解压时相同的文件名逻辑
                        string fileName = Path.GetFileName(entry.FullName);
                        var extension = Path.GetExtension(fileName);
                        var newfileName = $"{application.Name}{extension}";
                        string filePath = Path.Combine(destinationDirectory, newfileName);

                        if (!File.Exists(filePath))
                        {
                            needUnzip = true;
                            break;
                        }
                    }

                    // 执行解压
                    if (needUnzip)
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            if (string.IsNullOrEmpty(entry.Name))
                                continue;

                            string fileName = Path.GetFileName(entry.FullName);
                            var extension = Path.GetExtension(fileName);
                            var newfileName = $"{application.Name}{extension}";
                            string filePath = Path.Combine(destinationDirectory, newfileName);

                            // 确保目标目录存在
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                            // 解压文件(覆盖已存在的)
                            entry.ExtractToFile(filePath, overwrite: true);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                // CLAUDE.md §6 显式报错：从 ExtractApplicationZip 失败必须留下可见痕迹，
                // 不得静默吞掉（旧实现 declared-but-unused ex 触发 CS0168）。
                Debug.WriteLine($"ExtractApplicationZip failed: {ex.Message}");
                return false;
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
    }
}
