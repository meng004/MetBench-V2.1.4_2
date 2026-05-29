using LiteDB;
using MetBench_DAL;
using MetBench_DAL.V2;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Schema;

/// <summary>
/// debt #5 — Anomaly.Status 在真实 LiteDB 上 ① 存为 int ② GetByStatus 按 enum 过滤
/// ③ 旧 string 数据被一次性迁移成 int。走 <see cref="DbConfig"/> 生产路径
/// （注册 int 序列化器 + 迁移都在 DbConfig ctor 内）。
/// </summary>
[Collection("DbConfigGlobal")]
public sealed class AnomalyStatusPersistenceTests : IDisposable
{
    private readonly string _dbPath;

    public AnomalyStatusPersistenceTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchDebt5AnomalyStatus",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        DbConfig.OverrideConnectionString($"Filename={_dbPath}");
    }

    public void Dispose()
    {
        DbConfig.ResetOverride();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var log = _dbPath + "-log";
        if (File.Exists(log)) File.Delete(log);
    }

    [Fact]
    public void Status_is_persisted_as_int_on_disk()
    {
        var repo = new LiteDbAnomalyRepository(); // ctor → DbConfig.Instance 注册序列化器
        var id = Guid.NewGuid();
        Assert.True(repo.Add(new Anomaly
        {
            IdAnomaly = id,
            ResultId = Guid.NewGuid(),
            Status = AnomalyStatus.ConfirmedBug,
        }));

        // 原始 BsonDocument 层验证 on-disk 是 int（而非 enum-name 字符串）。
        // Isolated BsonMapper — must NOT read the shared BsonMapper.Global. DbConfig
        // mutates Global, and LiteDB 5's global-mapper cache is not thread-safe, so a
        // bare `new LiteDatabase` can race with concurrent DbConfig tests (see PR #239).
        using var db = new LiteDatabase($"Filename={_dbPath}", new BsonMapper());
        var raw = db.GetCollection("Anomalies").FindById(id);
        Assert.NotNull(raw);
        Assert.True(raw["Status"].IsInt32, $"Status should be int, was {raw["Status"].Type}");
        Assert.Equal((int)AnomalyStatus.ConfirmedBug, raw["Status"].AsInt32);
    }

    [Fact]
    public void Roundtrip_through_repo_preserves_enum()
    {
        var repo = new LiteDbAnomalyRepository();
        var id = Guid.NewGuid();
        repo.Add(new Anomaly { IdAnomaly = id, ResultId = Guid.NewGuid(), Status = AnomalyStatus.FixedUpstream });

        var fetched = repo.Get(id);
        Assert.NotNull(fetched);
        Assert.Equal(AnomalyStatus.FixedUpstream, fetched!.Status);
    }

    [Fact]
    public void GetByStatus_filters_by_enum_against_int_storage()
    {
        var repo = new LiteDbAnomalyRepository();
        repo.Add(new Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = Guid.NewGuid(), Status = AnomalyStatus.New });
        repo.Add(new Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = Guid.NewGuid(), Status = AnomalyStatus.ConfirmedBug });
        repo.Add(new Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = Guid.NewGuid(), Status = AnomalyStatus.ConfirmedBug });

        var confirmed = repo.GetByStatus(AnomalyStatus.ConfirmedBug);
        Assert.Equal(2, confirmed.Count);
        Assert.All(confirmed, a => Assert.Equal(AnomalyStatus.ConfirmedBug, a.Status));

        Assert.Single(repo.GetByStatus(AnomalyStatus.New));
        Assert.Empty(repo.GetByStatus(AnomalyStatus.FalsePositive));
    }

    [Fact]
    public void Migration_rewrites_legacy_string_status_to_int()
    {
        var idKebab = Guid.NewGuid();
        var idAlreadyInt = Guid.NewGuid();

        // 1) 写入旧数据：一条 Status 为 kebab string，一条已是 int（迁移须幂等跳过）。
        // Isolated BsonMapper — must NOT read the shared BsonMapper.Global (race w/ DbConfig, PR #239).
        using (var db = new LiteDatabase($"Filename={_dbPath}", new BsonMapper()))
        {
            var col = db.GetCollection("Anomalies");
            col.Insert(new BsonDocument
            {
                ["_id"] = idKebab,
                ["ResultId"] = Guid.NewGuid(),
                ["Severity"] = "major",
                ["Status"] = "confirmed-bug", // legacy kebab string
                ["Category"] = "basin",
            });
            col.Insert(new BsonDocument
            {
                ["_id"] = idAlreadyInt,
                ["ResultId"] = Guid.NewGuid(),
                ["Severity"] = "minor",
                ["Status"] = (int)AnomalyStatus.Investigating, // already migrated
                ["Category"] = "single-point",
            });
        }

        // 2) 触发 DbConfig.Instance → 迁移在 ctor 内跑。
        var repo = new LiteDbAnomalyRepository();

        // 3) on-disk：两条都是 int，值正确。
        // Isolated BsonMapper — must NOT read the shared BsonMapper.Global (race w/ DbConfig, PR #239).
        using (var db = new LiteDatabase($"Filename={_dbPath}", new BsonMapper()))
        {
            var col = db.GetCollection("Anomalies");
            var migrated = col.FindById(idKebab);
            Assert.True(migrated["Status"].IsInt32);
            Assert.Equal((int)AnomalyStatus.ConfirmedBug, migrated["Status"].AsInt32);

            var untouched = col.FindById(idAlreadyInt);
            Assert.True(untouched["Status"].IsInt32);
            Assert.Equal((int)AnomalyStatus.Investigating, untouched["Status"].AsInt32);
        }

        // 4) typed 读取也正确（反序列化器认 int）。
        Assert.Equal(AnomalyStatus.ConfirmedBug, repo.Get(idKebab)!.Status);
        Assert.Equal(AnomalyStatus.Investigating, repo.Get(idAlreadyInt)!.Status);
    }
}
