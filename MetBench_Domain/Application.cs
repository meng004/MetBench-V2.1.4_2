using LiteDB;
using System.Collections.ObjectModel;

namespace MetBench_Domain
{
    //Application
    public class Application
    {
        [BsonId]
        public int IdApplication { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProgrammingLanguage { get; set; }
        public int LinesOfCode { get; set; }//非空字段

        // 输入参数列表
        public List<ApplicationParameter> InputParameters { get; set; } = new List<ApplicationParameter>();
        // 输出参数列表
        public List<ApplicationParameter> OutputParameters { get; set; } = new List<ApplicationParameter>();

        //程序源码或二进制文件 压缩包转换为字节数组
        public byte[] Code { get; set; }
        #region
        public string CodeName { get; set; }
        #endregion
        //测试用例
        public byte[] SourceTestCase { get; set; }
        public string SourceTestCaseName { get; set; }
        public string DOI { get; set; }
        public string Url { get; set; }

        //Domain的Name 作为外键
        public string DomainName { get; set; } = String.Empty; // DomainName之间以:为分隔符
    }

    // 应用程序参数类
    public class ApplicationParameter
    {
        // 参数名称
        public string Name { get; set; }

        // 参数类型（如 "float", "int", "string" 等）
        public string Type { get; set; }

        // 参数描述
        public string Description { get; set; }

        // 参数约束或取值范围（如 ">0", "[0,100]","(0,20)" "A|B|C" 等）
        public string Constraints { get; set; }

        // 是否必需参数
        public bool IsRequired { get; set; }=false;

    }
}
