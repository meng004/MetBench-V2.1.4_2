namespace MetBench_IDAL
{
    //应用程序查询结果类
    public class Applications_QueryResultData
    {
        public int IdApplication { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProgrammingLanguage { get; set; } = string.Empty;
        public int LinesOfCode { get; set; }
        //程序源码或二进制文件
        public byte[] Code { get; set; } = Array.Empty<byte>();
        public string CodeName { get; set; } = string.Empty;
        //测试用例
        public byte[] SourceTestCase { get; set; } = Array.Empty<byte>();
        public string DOI { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string DomainName { get; set; } = string.Empty;
    }
}
