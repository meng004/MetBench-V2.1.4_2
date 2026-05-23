using MetBench_BLL.SystemMT.Metadata;
using MetBench_BLL.SystemMT.Transformations;
using MetBench_Domain;
using MetBench_IDAL;
using System.Text.Json;

namespace MetBench_BLL.SystemMT.Catalog;

/// <summary>
/// 系统级 MT 目录的 CRUD 服务 —— 把 v1 共享实体 (Application / MetamorphicRelation)
/// 按 Kind="system-level" 过滤暴露 + 提供 v2 专属的唯一性校验与删除安全检查。
/// </summary>
/// <remarks>
/// 与 v1 方法级 MT 的 CRUD 不冲突 —— v1 行 Kind="method-level",本服务的列表方法
/// 与查重方法均按 Kind 过滤,互不可见。
///
/// 删除安全:
///   • Delete SUT:若有依赖的 MRBinding,拒绝(返回 Blocked);需先解绑。
///   • Delete MR:若有依赖的 MRBinding,拒绝。
/// 创建/更新:
///   • SUT:Name 必填、Kind 强制 system-level、同 Kind 下 Name 唯一。
///   • MR:Code 必填、Kind 强制 system-level、同 Kind 下 Code 唯一。
/// 绑定:
///   • BindMrToSut:同 (MRId, ApplicationId) 唯一;返回新建/已存在的 MRBinding。
///
/// 操作均写 AuditLog。
/// </remarks>
public sealed class SystemMtCatalogService
{
    private const string SystemLevel = "system-level";

    private readonly IApplicationRepository _apps;
    private readonly IMetamorphicRelationRepository _mrs;
    private readonly IMRBindingRepository _bindings;
    private readonly IAuditLogRepository _audit;
    private readonly ISystemMtMetadataRepository? _meta;

    public SystemMtCatalogService(
        IApplicationRepository apps,
        IMetamorphicRelationRepository mrs,
        IMRBindingRepository bindings,
        IAuditLogRepository audit,
        ISystemMtMetadataRepository? metadataRepo = null)
    {
        _apps = apps ?? throw new ArgumentNullException(nameof(apps));
        _mrs = mrs ?? throw new ArgumentNullException(nameof(mrs));
        _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _meta = metadataRepo;
    }

    // ==================== SUT (Application Kind=system-level) ====================

    public IReadOnlyList<Application> ListSuts() =>
        _apps.GetAll().Where(IsSystemLevel).ToList();

    public Application? FindSutByName(string name) =>
        _apps.GetAll().FirstOrDefault(a => IsSystemLevel(a)
            && string.Equals(a.Name, name, StringComparison.Ordinal));

    public Application? GetSut(int sutId)
    {
        var apps = _apps.GetAll();
        var sut = apps.FirstOrDefault(a => a.IdApplication == sutId);
        return (sut is not null && IsSystemLevel(sut)) ? sut : null;
    }

    /// <summary>新建一个系统级 SUT;Name 必填、唯一(同 Kind 下)。Kind 强制为 system-level。</summary>
    public int CreateSut(Application sut, string actor = "user")
    {
        ArgumentNullException.ThrowIfNull(sut);
        if (string.IsNullOrWhiteSpace(sut.Name))
            throw new ArgumentException("SUT Name is required", nameof(sut));
        sut.Kind = SystemLevel;
        if (FindSutByName(sut.Name) is not null)
            throw new InvalidOperationException(
                $"system-level SUT with Name='{sut.Name}' already exists");
        _apps.Add(sut);
        Audit(actor, "sut.create", "Application", sut.IdApplication.ToString(), sut.Name);
        return sut.IdApplication;
    }

    public bool UpdateSut(Application sut, string actor = "user")
    {
        ArgumentNullException.ThrowIfNull(sut);
        if (!IsSystemLevel(sut))
            throw new ArgumentException("SUT.Kind must be 'system-level'", nameof(sut));
        var existing = GetSut(sut.IdApplication);
        if (existing is null)
            throw new InvalidOperationException(
                $"system-level SUT id={sut.IdApplication} not found");
        // 撞名检查:任一同名的系统级 SUT,排除自身。
        if (FindSutByName(sut.Name) is { } collision
            && collision.IdApplication != sut.IdApplication)
        {
            throw new InvalidOperationException(
                $"another SUT with Name='{sut.Name}' already exists");
        }
        var ok = _apps.Modify(sut);
        if (ok) Audit(actor, "sut.update", "Application", sut.IdApplication.ToString(), sut.Name);
        return ok;
    }

    /// <summary>删除 SUT;若有 MRBinding 依赖,返回 Blocked。</summary>
    public DeleteOutcome DeleteSut(int sutId, string actor = "user")
    {
        var sut = GetSut(sutId);
        if (sut is null) return DeleteOutcome.NotFound;
        var deps = _bindings.GetByApplication(sutId);
        if (deps.Any())
            return DeleteOutcome.Blocked(
                $"SUT id={sutId} has {deps.Count} MR binding(s); unbind first");
        var ok = _apps.Remove(sut);
        if (ok) Audit(actor, "sut.delete", "Application", sutId.ToString(), sut.Name);
        return ok ? DeleteOutcome.Deleted : DeleteOutcome.Blocked("repository refused delete");
    }

    // ==================== MR (MetamorphicRelation Kind=system-level) ====================

    public IReadOnlyList<MetamorphicRelation> ListMrs() =>
        _mrs.GetAll().Where(IsSystemLevel).ToList();

    public MetamorphicRelation? FindMrByCode(string code) =>
        _mrs.GetAll().FirstOrDefault(m => IsSystemLevel(m)
            && string.Equals(m.Code, code, StringComparison.Ordinal));

    public MetamorphicRelation? GetMr(int mrId)
    {
        var mr = _mrs.GetAll().FirstOrDefault(m => m.IdMR == mrId);
        return (mr is not null && IsSystemLevel(mr)) ? mr : null;
    }

    public int CreateMr(MetamorphicRelation mr, string actor = "user")
    {
        ArgumentNullException.ThrowIfNull(mr);
        if (string.IsNullOrWhiteSpace(mr.Code))
            throw new ArgumentException("MR Code is required", nameof(mr));
        mr.Kind = SystemLevel;
        if (FindMrByCode(mr.Code) is not null)
            throw new InvalidOperationException(
                $"system-level MR with Code='{mr.Code}' already exists");
        if (mr.CreatedAt == default) mr.CreatedAt = DateTime.UtcNow;
        if (string.IsNullOrEmpty(mr.CreatedBy)) mr.CreatedBy = actor;
        _mrs.Add(mr);
        Audit(actor, "mr.create", "MetamorphicRelation", mr.IdMR.ToString(), mr.Code);
        return mr.IdMR;
    }

    public bool UpdateMr(MetamorphicRelation mr, string actor = "user")
    {
        ArgumentNullException.ThrowIfNull(mr);
        if (!IsSystemLevel(mr))
            throw new ArgumentException("MR.Kind must be 'system-level'", nameof(mr));
        var existing = GetMr(mr.IdMR);
        if (existing is null)
            throw new InvalidOperationException(
                $"system-level MR id={mr.IdMR} not found");
        // 撞 Code 检查:排除自身。
        if (FindMrByCode(mr.Code) is { } collision
            && collision.IdMR != mr.IdMR)
        {
            throw new InvalidOperationException(
                $"another MR with Code='{mr.Code}' already exists");
        }
        var ok = _mrs.Modify(mr);
        if (ok) Audit(actor, "mr.update", "MetamorphicRelation", mr.IdMR.ToString(), mr.Code);
        return ok;
    }

    public DeleteOutcome DeleteMr(int mrId, string actor = "user")
    {
        var mr = GetMr(mrId);
        if (mr is null) return DeleteOutcome.NotFound;
        var deps = _bindings.GetByMR(mrId);
        if (deps.Any())
            return DeleteOutcome.Blocked(
                $"MR id={mrId} has {deps.Count} SUT binding(s); unbind first");
        var ok = _mrs.Remove(mr);
        if (ok) Audit(actor, "mr.delete", "MetamorphicRelation", mrId.ToString(), mr.Code);
        return ok ? DeleteOutcome.Deleted : DeleteOutcome.Blocked("repository refused delete");
    }

    // ==================== MR × SUT binding ====================

    public IReadOnlyList<MRBinding> ListBindingsForSut(int sutId) =>
        _bindings.GetByApplication(sutId).ToList();

    public IReadOnlyList<MRBinding> ListBindingsForMr(int mrId) =>
        _bindings.GetByMR(mrId).ToList();

    /// <summary>
    /// 把指定 MR 绑定到指定 SUT;若已存在 (MRId, ApplicationId) 同对绑定,返回原行,
    /// 不创建重复(幂等)。
    /// </summary>
    public BindOutcome BindMrToSut(int mrId, int sutId, string sampleCasePath, string actor = "user")
    {
        var mr = GetMr(mrId);
        if (mr is null)
            return BindOutcome.Failure($"system-level MR id={mrId} not found");
        var sut = GetSut(sutId);
        if (sut is null)
            return BindOutcome.Failure($"system-level SUT id={sutId} not found");

        var existing = _bindings.GetAll()
            .FirstOrDefault(b => b.MRId == mrId && b.ApplicationId == sutId);
        if (existing is not null)
            return BindOutcome.Existed(existing.IdMRBinding);

        var binding = new MRBinding
        {
            MRId = mrId,
            ApplicationId = sutId,
            DefaultSampleCasePath = sampleCasePath ?? string.Empty,
            Status = "active",
            BoundAt = DateTime.UtcNow,
            BoundBy = actor,
        };
        _bindings.Add(binding);
        Audit(actor, "binding.create", "MRBinding",
            binding.IdMRBinding.ToString(),
            $"mr={mr.Code} sut={sut.Name}");
        return BindOutcome.Created(binding.IdMRBinding);
    }

    public bool UnbindMrFromSut(int mrId, int sutId, string actor = "user")
    {
        var existing = _bindings.GetAll()
            .FirstOrDefault(b => b.MRId == mrId && b.ApplicationId == sutId);
        if (existing is null) return false;
        var ok = _bindings.Remove(existing);
        if (ok) Audit(actor, "binding.delete", "MRBinding",
            existing.IdMRBinding.ToString(), $"mr={mrId} sut={sutId}");
        return ok;
    }

    // ==================== EquationFunction CRUD (P3) ====================

    /// <summary>
    /// 校验并持久化一个 L1 Recipe。校验：EquationKey/FunctionName 非空、RecipeJson 合法、
    /// 每个 op 存在于 <see cref="TransformationRegistry"/>。
    /// </summary>
    public async Task CreateEquationFunctionAsync(
        EquationFunctionRecipe recipe, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        if (string.IsNullOrWhiteSpace(recipe.EquationKey))
            throw new ArgumentException("EquationKey is required", nameof(recipe));
        if (string.IsNullOrWhiteSpace(recipe.FunctionName))
            throw new ArgumentException("FunctionName is required", nameof(recipe));

        ValidateRecipeJson(recipe.RecipeJson);

        var repo = _meta ?? throw new InvalidOperationException(
            "CreateEquationFunctionAsync requires ISystemMtMetadataRepository");
        await repo.UpsertRecipeAsync(recipe, ct);
    }

    /// <summary>按 (equationKey, functionName) 取 Recipe，缺失返回 null。</summary>
    public async Task<EquationFunctionRecipe?> GetEquationFunctionAsync(
        string equationKey, string functionName, CancellationToken ct = default)
    {
        if (_meta is null || string.IsNullOrWhiteSpace(equationKey) ||
            string.IsNullOrWhiteSpace(functionName))
            return null;
        return await _meta.GetRecipeAsync(equationKey, functionName, ct);
    }

    /// <summary>列出指定方程下的全部 L1 Recipe，按 FunctionName 升序。</summary>
    public async Task<IReadOnlyList<EquationFunctionRecipe>> ListEquationFunctionsAsync(
        string equationKey, CancellationToken ct = default)
    {
        if (_meta is null || string.IsNullOrWhiteSpace(equationKey))
            return Array.Empty<EquationFunctionRecipe>();
        return await _meta.ListRecipesByEquationAsync(equationKey, ct);
    }

    private static void ValidateRecipeJson(string recipeJson)
    {
        List<string> ops;
        try
        {
            using var doc = JsonDocument.Parse(recipeJson);
            var compose = doc.RootElement.GetProperty("compose");
            ops = compose.EnumerateArray()
                .Select(s => s.GetProperty("op").GetString() ?? string.Empty)
                .ToList();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new ArgumentException($"Invalid RecipeJson: {ex.Message}", ex);
        }

        foreach (var op in ops)
        {
            if (!TransformationRegistry.AvailableNames.Contains(op))
                throw new ArgumentException(
                    $"Unknown op '{op}' in recipe. " +
                    $"Registered: [{string.Join(", ", TransformationRegistry.AvailableNames)}]");
        }
    }

    // ==================== helpers ====================

    private static bool IsSystemLevel(Application a) =>
        string.Equals(a.Kind, SystemLevel, StringComparison.Ordinal);

    private static bool IsSystemLevel(MetamorphicRelation m) =>
        string.Equals(m.Kind, SystemLevel, StringComparison.Ordinal);

    private void Audit(string actor, string action, string entityType, string entityId, string detail)
    {
        _audit.Add(new AuditLog
        {
            IdLog = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Actor = actor,
            Action = action,
            TargetEntityType = entityType,
            TargetEntityId = entityId,
            DetailsJson = detail,
        });
    }
}

/// <summary>SUT / MR 删除结果(三态)。</summary>
public sealed record DeleteOutcome(string Status, string? Reason)
{
    public static readonly DeleteOutcome Deleted = new("deleted", null);
    public static readonly DeleteOutcome NotFound = new("not-found", null);
    public static DeleteOutcome Blocked(string reason) => new("blocked", reason);
}

/// <summary>绑定结果(三态)。</summary>
public sealed record BindOutcome(string Status, int? BindingId, string? Error)
{
    public static BindOutcome Created(int id) => new("created", id, null);
    public static BindOutcome Existed(int id) => new("existed", id, null);
    public static BindOutcome Failure(string error) => new("error", null, error);
}
