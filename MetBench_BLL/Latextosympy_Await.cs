using System.Diagnostics;
using System.Reflection;

namespace MetBench_BLL
{
    [Obsolete("Legacy LaTeX→SymPy async execution path. New MRs use MethodTransformationRegistry + EquationFunction (mr-architecture §5). This class is retained for v1 method-level MT read compatibility only.")]
    public class Latextosympy_Await
    {
        private static readonly object lockObject = new object();

        // 解压文件的存储路径（默认系统的Temp路径）  
        string SystemTemppath = Path.GetTempPath();
        // 存储路径下创建的存储文件夹名  
        string folderunderpath = "temp_image";

        // 获取python的环境变量路径  
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
                    if (directory.Contains("Python", StringComparison.OrdinalIgnoreCase)) // 忽略大小写  
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

        // 异步方法，转换latex为sympy表达式  
        public async Task<string> LatextosympyAsync(string latex)
        {
            Assembly assembly = Assembly.GetEntryAssembly();
            AbsolutePathResolver absolutePathResolver = new AbsolutePathResolver();
            string solutionPath = absolutePathResolver.GetSolutionPath(assembly);
            string escapedPath = $"\"{solutionPath}\"";
            string pythonInPath = FindFirstPythonInPath();

            return await Task.Run(() =>
            {
                lock (lockObject) // 加锁，确保线程安全  
                {
                    using (Process process = new Process())
                    {
                        try
                        {
                            // 设置进程启动信息  
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = Path.Combine(pythonInPath, "python.exe"),
                                Arguments = $"{escapedPath}\\MetBench_Python\\LatexToSympy.py \"{latex}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardOutput = true
                            };

                            process.StartInfo = startInfo;

                            // 启动进程  
                            process.Start();
                            // 等待进程执行完毕  
                            process.WaitForExit();

                            // 读取并返回Python脚本的输出结果  
                            string output = process.StandardOutput.ReadToEnd();
                            return output.Trim();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("发生异常：" + ex.Message);
                            return string.Empty;
                        }
                    }
                }
            });
        }

        // 异步方法，将latex渲染成图片，并返回图片路径  
        public async Task<string> LatextoImageAsync(string latex)
        {
            Assembly assembly = Assembly.GetEntryAssembly();
            AbsolutePathResolver absolutePathResolver = new AbsolutePathResolver();
            string solutionPath = absolutePathResolver.GetSolutionPath(assembly);
            string escapedPath = $"\"{solutionPath}\"";
            string pythonInPath = FindFirstPythonInPath();

            return await Task.Run(() =>
            {
                lock (lockObject) // 加锁，确保线程安全  
                {
                    using (Process process = new Process())
                    {
                        try
                        {
                            // 设置进程启动信息  
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = Path.Combine(pythonInPath, "python.exe"),
                                Arguments = $"{escapedPath}\\MetBench_Python\\LatextoImage.py \"{latex}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardOutput = true
                            };

                            process.StartInfo = startInfo;

                            // 启动进程  
                            process.Start();
                            // 等待进程执行完毕  
                            process.WaitForExit();

                            // 读取并返回Python脚本的输出结果  
                            string output = process.StandardOutput.ReadToEnd();
                            return output.Trim();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("发生异常：" + ex.Message);
                            return string.Empty;
                        }
                    }
                }
            });
        }

        // 将图像转为字节数组  
        public byte[] ConvertImageToByteArray(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                Console.WriteLine("Image file does not exist.");
                return null;
            }

            byte[] imageData = File.ReadAllBytes(imagePath);

            if (imageData.Length == 0)
            {
                Console.WriteLine("Image data is empty.");
            }

            return imageData;
        }
    }
}
