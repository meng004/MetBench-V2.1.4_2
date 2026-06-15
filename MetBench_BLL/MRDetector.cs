namespace MetBench_BLL
{
    public class MRDetector
    {
        private  IRecognitionAlgorithm _algorithms;
        private IResultParser _resultParser;
        private string mrCsvPath;
        public MRDetector(IRecognitionAlgorithm algorithm,IResultParser resultParser) 
        {
            _algorithms = algorithm;
            _resultParser = resultParser;
        }

        public async Task<ParsedResult> Recognize(TargetProgram target)
        {
            // 1. 异步执行算法
            var detectionResult = await Task.Run(() => _algorithms.Execute(target))
                .ConfigureAwait(false);

            // 2. 异步处理中间结果
            var (csvPath, parsedResults) = await Task.Run(() =>
            {
                var path = detectionResult.Result["MRCsvPath"] as string;
                var results = _resultParser.Parse(detectionResult);
                return (path, results);
            }).ConfigureAwait(false);

            // 3. 更新共享变量
            Interlocked.Exchange(ref mrCsvPath, csvPath);

            return parsedResults ?? throw new InvalidOperationException("解析结果为空");
        }

        //将识别到的MR数据的Csv文件复制粘贴到指定目录（原始Csv文件） 需要异步实现
        public bool ExportMRResults(string targetDirectory, string mrCsvPath)
        {
            // 参数校验
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(mrCsvPath))
            {
                return false;
            }

            try
            {

                // 2. 创建FileInfo对象
                FileInfo fileInfo = new FileInfo(mrCsvPath);

                // 3. 验证文件是否存在（可选）
                if (!fileInfo.Exists)
                {
                    Console.WriteLine($"警告：文件不存在 {mrCsvPath}");
                    // 仍然返回FileInfo对象，但Exists属性为false
                }

                // 5. 确保目标目录存在
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                    Console.WriteLine($"创建目标目录: {targetDirectory}");
                }

                // 6. 构建目标文件路径
                string destFilePath = Path.Combine(targetDirectory, fileInfo.Name);

                // 7. 执行文件复制（覆盖已存在的文件）
                File.Copy(fileInfo.FullName, destFilePath, overwrite: true);
                Console.WriteLine($"已复制文件到: {destFilePath}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导出MR结果失败: {ex.Message}");
                return false;
            }

        }

        public IResultParser GetResultParser() 
        {
            return _resultParser;
        }
        public IRecognitionAlgorithm GetRecognitionAlgorithm()
        {
            return _algorithms;
        }
        public string GetMRCsvPath() 
        {
            return mrCsvPath;
        }

    }
}
