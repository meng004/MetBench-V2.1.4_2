using LiteDB;
using MetBench_DAL;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Schema;

/// <summary>
/// F18 — DbConfig 跨平台修复验证 (cold-start C5)。
///
/// 验证 <see cref="DbConfig._conn"/> 三级优先级：
///   1) <see cref="DbConfig.OverrideConnectionString"/> 静态 override
///   2) 环境变量 <c>METBENCH_DB_PATH</c>
///   3) legacy ConfigurationManager + .sln 走查（Windows app.config 路径）
///
/// 这些测试不依赖 app.config，可在 Linux / CI 上运行 ——
/// 每个测试以 try/finally 重置 static state (env var + override) 避免污染其他测试。
/// </summary>
[Collection("DbConfigGlobal")]
public sealed class DbConfigTests : IDisposable
{
    private const string EnvVar = "METBENCH_DB_PATH";
    private readonly string? _previousEnv;
    private readonly string _scratchDir;

    public DbConfigTests()
    {
        _previousEnv = Environment.GetEnvironmentVariable(EnvVar);
        Environment.SetEnvironmentVariable(EnvVar, null);
        DbConfig.ResetOverride();

        _scratchDir = Path.Combine(
            Path.GetTempPath(),
            "MetBenchDbConfigTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_scratchDir);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(EnvVar, _previousEnv);
        DbConfig.ResetOverride();
        if (Directory.Exists(_scratchDir))
        {
            try { Directory.Delete(_scratchDir, recursive: true); } catch { /* best-effort */ }
        }
    }

    // ===== Test 1: OverrideConnectionString 优先级最高 =====

    [Fact]
    public void OverrideConnectionString_returns_override_value_via_conn_getter()
    {
        var dbPath = Path.Combine(_scratchDir, "override.litedb");
        var expected = $"Filename={dbPath};Connection=shared";

        DbConfig.OverrideConnectionString($"Filename={dbPath}");

        var conn = DbConfig.Instance._conn;
        Assert.Equal(expected, conn);
    }

    // ===== Test 2: 环境变量优先于 legacy 路径 =====

    [Fact]
    public void Env_var_METBENCH_DB_PATH_resolves_to_Filename_connection_string()
    {
        var dbPath = Path.Combine(_scratchDir, "from-env.litedb");
        Environment.SetEnvironmentVariable(EnvVar, dbPath);

        // 不要走 Instance 单例（它的 ctor 会试着开 LiteDB），只验证 _conn getter 逻辑。
        // 但 _conn 是 instance 属性，所以需要一个 DbConfig 实例 —— 用 reflection 拿 Instance
        // 但避免触发 ctor 的 LiteDatabase 物理读：override 一个有效 path 让 ctor 也能跑过。
        DbConfig.OverrideConnectionString($"Filename={dbPath}");
        var via_override = DbConfig.Instance._conn;
        DbConfig.ResetOverride();

        // 现在只剩 env var → _conn 应当从 env 解析
        var via_env = DbConfig.Instance._conn;

        Assert.Equal($"Filename={dbPath};Connection=shared", via_env);
        Assert.Equal(via_override, via_env);
    }

    // ===== Test 3: Override 比 env var 优先级高 =====

    [Fact]
    public void Override_takes_precedence_over_env_var()
    {
        var envPath = Path.Combine(_scratchDir, "from-env.litedb");
        var overridePath = Path.Combine(_scratchDir, "from-override.litedb");
        Environment.SetEnvironmentVariable(EnvVar, envPath);
        DbConfig.OverrideConnectionString($"Filename={overridePath}");

        var conn = DbConfig.Instance._conn;

        Assert.Equal($"Filename={overridePath};Connection=shared", conn);
        Assert.DoesNotContain("from-env", conn);
    }

    // ===== Test 4: ResetOverride 后回落到 env var =====

    [Fact]
    public void ResetOverride_falls_back_to_env_var()
    {
        var envPath = Path.Combine(_scratchDir, "after-reset.litedb");
        Environment.SetEnvironmentVariable(EnvVar, envPath);
        DbConfig.OverrideConnectionString("Filename=ignored.litedb");

        // 重置后应当回落到 env var
        DbConfig.ResetOverride();

        var conn = DbConfig.Instance._conn;
        Assert.Equal($"Filename={envPath};Connection=shared", conn);
    }

    // ===== Test 5: Path.Combine 跨平台 — 不含 Windows 反斜杠硬路径 =====

    [Fact]
    public void Env_var_path_with_subdirectory_creates_directory_and_uses_platform_separator()
    {
        var nested = Path.Combine(_scratchDir, "nested", "deep", "db.litedb");
        Environment.SetEnvironmentVariable(EnvVar, nested);

        var conn = DbConfig.Instance._conn;

        // 路径分隔符必须是当前平台的（Linux: '/', Windows: '\\'）
        var nestedDir = Path.GetDirectoryName(nested)!;
        Assert.True(Directory.Exists(nestedDir),
            $"env-var resolution should create the parent directory; expected '{nestedDir}'");

        // conn 中不应当混入异平台分隔符
        Assert.Contains(Path.DirectorySeparatorChar.ToString(), nested);
        Assert.Equal($"Filename={nested};Connection=shared", conn);
    }

    [Fact]
    public void Explicit_connection_mode_is_preserved()
    {
        var dbPath = Path.Combine(_scratchDir, "explicit-direct.litedb");
        var expected = $"Filename={dbPath};Connection=direct";

        DbConfig.OverrideConnectionString(expected);

        var conn = DbConfig.Instance._conn;
        Assert.Equal(expected, conn);
    }

    // ===== Test 6: Override + 真实 LiteDB 集成 — Instance ctor 可用新串建库 =====

    [Fact]
    public void Override_followed_by_Instance_access_opens_new_litedb_file()
    {
        var dbPath = Path.Combine(_scratchDir, "instance-init.litedb");
        DbConfig.OverrideConnectionString($"Filename={dbPath}");

        // 触发 Instance ctor — 它会用新 conn 跑实体映射 + 各 collection EnsureIndex
        var instance = DbConfig.Instance;

        Assert.NotNull(instance);
        Assert.True(File.Exists(dbPath),
            $"DbConfig.Instance ctor should physically create the LiteDB file at '{dbPath}'");
    }

    // ===== Test 7: OverrideConnectionString 空字符串应抛 ArgumentException =====

    [Fact]
    public void OverrideConnectionString_rejects_empty_value()
    {
        Assert.Throws<ArgumentException>(() => DbConfig.OverrideConnectionString(""));
        Assert.Throws<ArgumentException>(() => DbConfig.OverrideConnectionString("   "));
    }
}
