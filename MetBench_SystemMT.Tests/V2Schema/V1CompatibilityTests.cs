using LiteDB;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Schema;

/// <summary>
/// TDD 验证 v1 兼容性：v1 既有读写行为不退化（含 Obsolete 字段读取）。
/// </summary>
public sealed class V1CompatibilityTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ILiteDatabase _db;

    public V1CompatibilityTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchV1CompatTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _db = new LiteDatabase(_dbPath, new BsonMapper());
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var logFile = _dbPath + "-log";
        if (File.Exists(logFile)) File.Delete(logFile);
    }

    [Fact]
    public void V1_MetamorphicRelation_legacy_ApplicationName_still_writable()
    {
        _db.Mapper.Entity<MetamorphicRelation>().Id(x => x.IdMR);
        var col = _db.GetCollection<MetamorphicRelation>("MetamorphicRelations");

        // 模拟 v1 行：只填 v1 字段
#pragma warning disable CS0618 // ApplicationName Obsolete
        var entity = new MetamorphicRelation
        {
            Description = "v1 row",
            InputPattern = "x → 2x",
            OutputPattern = "f(x) < f(2x)",
            Operator = "scale",
            Hierarchy = "method-level",
            ApplicationName = "AppA:AppB:AppC"   // v1 多值字符串保留写入
        };
#pragma warning restore CS0618

        col.Insert(entity);
        var fetched = col.FindById(entity.IdMR);
        Assert.NotNull(fetched);
#pragma warning disable CS0618
        Assert.Equal("AppA:AppB:AppC", fetched.ApplicationName);
#pragma warning restore CS0618
        Assert.Equal("v1 row", fetched.Description);
        // v2 字段在 v1 行上是默认值；注意 LiteDB 既有约定"不存储空字符串"，
        // 所以未设置的空 string 字段从 DB 取回为 null。
        Assert.True(string.IsNullOrEmpty(fetched.Code));
        Assert.Equal("method-level", fetched.Kind);   // 非空默认值不丢失
    }

    [Fact]
    public void V1_Application_legacy_DomainName_still_writable()
    {
        _db.Mapper.Entity<Application>().Id(x => x.IdApplication);
        var col = _db.GetCollection<Application>("Applications");

#pragma warning disable CS0618 // DomainName Obsolete
        var entity = new Application
        {
            Name = "v1-app",
            ProgrammingLanguage = "Java",
            LinesOfCode = 1000,
            DomainName = "Physics:Math"   // v1 多值字符串保留
        };
#pragma warning restore CS0618

        col.Insert(entity);
        var fetched = col.FindById(entity.IdApplication);
        Assert.NotNull(fetched);
#pragma warning disable CS0618
        Assert.Equal("Physics:Math", fetched.DomainName);
#pragma warning restore CS0618
        Assert.Equal("method-level", fetched.Kind);  // 默认 v1 kind
        Assert.Null(fetched.Version);                // v2 字段默认 null
        Assert.Null(fetched.RuntimeId);
    }

    [Fact]
    public void V1_and_V2_rows_can_coexist_in_same_collection()
    {
        _db.Mapper.Entity<MetamorphicRelation>().Id(x => x.IdMR);
        var col = _db.GetCollection<MetamorphicRelation>("MetamorphicRelations");

#pragma warning disable CS0618
        col.Insert(new MetamorphicRelation
        {
            Description = "v1 row",
            ApplicationName = "AppX",
            Kind = "method-level"
        });
#pragma warning restore CS0618

        col.Insert(new MetamorphicRelation
        {
            Description = "v2 row",
            Code = "MR-T",
            MetaPatternCode = "m_mono",
            Kind = "system-level"
        });

        // 通过 Kind 字段过滤
        var v1Rows = col.Find(x => x.Kind == "method-level").ToList();
        var v2Rows = col.Find(x => x.Kind == "system-level").ToList();

        Assert.Single(v1Rows);
        Assert.Single(v2Rows);
        Assert.Equal("v1 row", v1Rows[0].Description);
        Assert.Equal("MR-T", v2Rows[0].Code);
    }
}
