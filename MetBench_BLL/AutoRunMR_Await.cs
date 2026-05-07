using System.Diagnostics;
using System.Reflection;

namespace MetBench_BLL
{
    public class AutoRunMR_Await
    {
        private static readonly object _lock = new object(); // 用于确保线程安全  

        // 获取Python的环境变量路径  
        /// <summary>
        /// 获取Python解释器的路径
        /// </summary>
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

        /// <summary>
        /// 运行蜕变测试执行脚本
        /// </summary>
        public async Task<string> AutorunMR2Async(string r_list, string R_list, string File_name, string MR_Input_dimension, string MR_Output_dimension, string random_data_min_value, string random_data_max_value, string random_data_count, string threshold)
        {
            Assembly assembly = Assembly.GetEntryAssembly();
            AbsolutePathResolver absolutePathResolver = new AbsolutePathResolver();
            string solutionPath = absolutePathResolver.GetSolutionPath(assembly);
            // 使用引号括起整个路径  
            string escapedPath = $"\"{solutionPath}\"";
            // 获取Python路径  
            string pythonInPath = FindFirstPythonInPath();
            if (string.IsNullOrEmpty(pythonInPath)) return string.Empty;
            // 创建一个进程对象  
            Process process = new Process();
            string output;
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
                return output; // 返回输出结果  
            }
            catch (Exception ex)
            {
                lock (_lock) // 保护对 Console 的访问  
                {
                    Console.WriteLine("发生异常：" + ex.Message);
                }
                return string.Empty; // 返回空字符串表示发生异常  
            }
            finally
            {
                // 关闭进程  
                process.Close();
            }
        }


    }
}
