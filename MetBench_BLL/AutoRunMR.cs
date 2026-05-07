using System.Diagnostics;
using System.Reflection;

namespace MetBench_BLL
{
    public class AutoRunMR
    {
        //获取python的环境变量路径
        public string FindFirstPythonInPath()
        {
            // 在 PATH 中查找包含 "Python" 的目录，你也可以查找其他关键字
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
                string parentDirectory = pythonPath;
                Console.WriteLine($"第一个 Python 可执行文件目录：{parentDirectory}");
                return parentDirectory;
            }
            else
            {
                Console.WriteLine($"没有 Python 可执行文件目录");
                return null;
            }
        }
        public string AutorunMR2(string r_list, string R_list, string File_name, string MR_Input_dimension ,string MR_Output_dimension, string random_data_min_value, string random_data_max_value, string random_data_count,string threshold)
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
                startInfo.Arguments = $"{escapedPath}\\MetBench_Python\\AutoRunBinaryMR.py " +
                                      $"\"{r_list}\" " +
                                      $"\"{R_list}\" " +
                                      $"\"{File_name}\" " +
                                      $"\"{MR_Input_dimension}\" " +
                                      $"\"{MR_Output_dimension}\" " +
                                      $"\"{random_data_min_value}\" " +
                                      $"\"{random_data_max_value}\" " +
                                      $"\"{random_data_count}\" " +
                                      $"\"{threshold}\" ";

                // 设置进程启动信息的一些属性
                startInfo.UseShellExecute = false; // 不使用操作系统shell启动进程
                startInfo.CreateNoWindow = false; // 不创建窗口
                startInfo.RedirectStandardOutput = true; // 重定向输出流，以便从Python脚本中获取结果
                // 将启动信息应用到进程对象
                process.StartInfo = startInfo;
       
                // 启动进程
                process.Start();

                // 等待进程执行完毕
                process.WaitForExit();

                // 读取并返回Python脚本的输出结果
                string output = process.StandardOutput.ReadToEnd();
                return output; // 去除字符串两端的空格和换行符

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
    }
}
