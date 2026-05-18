using LiteDB;
using System.Collections.ObjectModel;

namespace MetBench_Domain
{
    //Application — v1 方法级被测程序 / v2 系统级 SUT 共用同一实体，由 Kind 字段区分
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

        // ===== v1 反模式（保留读取兼容，标 Obsolete；v2 由 ApplicationDomains junction 取代） =====
        //Domain的Name 作为外键，DomainName 之间以:为分隔符
        [Obsolete("Use ApplicationDomains junction collection instead. Kept for v1 read compatibility only.")]
        public string DomainName { get; set; } = String.Empty;

        // ===== v2 新增字段（系统级 SUT 专用；v1 行默认值为空/null） =====

        /// <summary>SUT 自身版本（git sha / release tag），支持多版本并存。</summary>
        public string? Version { get; set; }

        /// <summary>运行时引用（→ Runtimes.IdRuntime）。null 表示用默认 Python。</summary>
        public int? RuntimeId { get; set; }

        /// <summary>SUT runner 脚本路径（如 "SUT/openmoc/openmoc_runner.py"）。</summary>
        public string? RunnerEntryPath { get; set; }

        /// <summary>Input parser 脚本路径（如 "SUT/openmoc/openmoc_input_parser.py"）。</summary>
        public string? InputParserPath { get; set; }

        /// <summary>Output parser 脚本路径（如 "SUT/openmoc/openmoc_output_parser.py"）。</summary>
        public string? OutputParserPath { get; set; }

        /// <summary>默认超时秒数（系统级执行）。</summary>
        public int? DefaultTimeoutSeconds { get; set; } = 60;

        /// <summary>并发上限（如 OpenMC MPI 4 进程版只允许 1 个并发）。</summary>
        public int? MaxConcurrentRuns { get; set; } = 1;

        /// <summary>"method-level"（v1）或 "system-level"（v2）。</summary>
        public string Kind { get; set; } = "method-level";

        // 让 WPF ComboBox 默认按 Name 显示, 而不是 fallback 到 "MetBench_Domain.Application" 类名 (UAT round-1 UC-A5/B1 bug)
        public override string ToString() => Name ?? string.Empty;
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
