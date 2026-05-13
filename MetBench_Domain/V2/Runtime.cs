using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// 运行时定义：SUT 执行所需的环境（Python venv / MATLAB / C++ binary / Java JVM / Fortran binary）。
/// 每个 Application 通过 RuntimeId 引用一个 Runtime。
/// </summary>
/// <remarks>
/// InvokeTemplate 是参数化命令字符串，placeholders 由 Pipeline 替换：
///   {python}  → Python 解释器路径
///   {binary}  → 编译好的二进制路径
///   {script}  → SUT runner 脚本路径
///   {input}   → 输入文件路径
///   {output}  → 输出文件路径
///
/// 例子：
///   Python: "{python} {script} --input {input} --output {output}"
///   MATLAB: "matlab -batch \"runMR('{input}','{output}')\""
///   C++:    "{binary} {input} {output}"
///   Java:   "java -jar {binary} {input} {output}"
///   Fortran: "{binary} <{input} >{output}"
/// </remarks>
public class Runtime
{
    [BsonId] public int IdRuntime { get; set; }

    /// <summary>运行时显示名（如 "python-openmoc-venv"）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>运行时种类："python" | "matlab" | "cpp" | "java" | "fortran" | "other"。</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>命令模板，placeholder 见 remarks。</summary>
    public string InvokeTemplate { get; set; } = string.Empty;

    /// <summary>调用时附加的环境变量。</summary>
    public Dictionary<string, string> EnvVars { get; set; } = new();

    /// <summary>可选的健康检查命令（部署时用）。</summary>
    public string? HealthCheckCommand { get; set; }

    /// <summary>说明（中文）。</summary>
    public string? Description { get; set; }
}
