namespace MetBench_IDAL
{
    //应用程序查询结果类
    public class Applications_QueryResultData
    {
        public int IdApplication { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProgrammingLanguage { get; set; }
        public int LinesOfCode { get; set; }
        //程序源码或二进制文件
        public byte[] Code { get; set; }
        public string CodeName { get; set; }
        //测试用例
        public byte[] SourceTestCase { get; set; }
        public string DOI { get; set; }
        public string Url { get; set; }
        public string DomainName { get; set; }
    }
}
