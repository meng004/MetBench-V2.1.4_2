using System.Reflection;
namespace MetBench_BLL
{
    public class AbsolutePathResolver
    {
        //用于获取解决方案 .sln文件的路径
        /// <summary>
        /// 获取项目的根目录路径
        /// </summary>
        public string GetSolutionPath(Assembly assembly)
        {
            // 获取执行程序集的文件路径
            string assemblyPath = assembly.Location;

            // 获取解决方案的目录路径
            string solutionDirPath = Path.GetDirectoryName(assemblyPath);

            // 循环向上查找解决方案文件（.sln）
            while (!Directory.GetFiles(solutionDirPath, "*.sln").Any())
            {
                // 获取上级目录路径
                string parentDirPath = Directory.GetParent(solutionDirPath)?.FullName;

                // 如果已经到达根目录，则返回空字符串
                if (parentDirPath == null)
                {
                    return string.Empty;
                }

                solutionDirPath = parentDirPath;
            }
            return solutionDirPath;
        }
    }
}
