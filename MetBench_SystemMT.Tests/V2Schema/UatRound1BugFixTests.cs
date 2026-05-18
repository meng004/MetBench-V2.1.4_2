using System.Collections.ObjectModel;
using MetBench_BLL;
using MetBench_Domain;
using MetBench_IDAL;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Schema;

/// <summary>
/// UAT round-1 (2026-05-18, PR #70) 发现的 2 个 bug 的回归测试。
///
/// 1. ApplicationService.UpdateService / DomainService.UpdateService 调用 IsDuplicate 时未传 excludeSelf=true,
///    导致"修改同名记录"被误判为"新建重名" → "该应用程序已存在！" Tips。
/// 2. Application / Domain / ApplicationEx 缺 ToString override → WPF ComboBox 显示类全名 (如
///    "MetBench_Domain.Application") 而非 Name 属性。
/// </summary>
public sealed class UatRound1BugFixTests
{
    // ============================================================
    // Bug 1 — UpdateService 必须传 excludeSelf=true
    // ============================================================

    [Fact]
    public void ApplicationService_UpdateService_passes_excludeSelf_true_to_IsDuplicate()
    {
        var repo = new FakeApplicationRepository();
        var service = new ApplicationService(repo);
        var app = new Application { IdApplication = 1, Name = "UAT-App-1" };

        var res = service.UpdateService(app);

        Assert.True(repo.LastIsDuplicateExcludeSelf,
            "UpdateService 必须以 excludeSelf=true 调 IsDuplicate, 否则修改同名记录会被自身命中而误判为重复");
        Assert.Equal(2, res); // 2 = success (因 fake 不会判重 + Modify 返回 true)
    }

    [Fact]
    public void ApplicationService_AddService_still_passes_excludeSelf_false_to_IsDuplicate()
    {
        // 防止过度修复 — Add 路径仍要保留 excludeSelf=false (新建不能命中现有同名)
        var repo = new FakeApplicationRepository();
        var service = new ApplicationService(repo);
        var app = new Application { Name = "UAT-App-new" };

        service.AddService(app);

        Assert.False(repo.LastIsDuplicateExcludeSelf,
            "AddService 必须保持 excludeSelf=false");
    }

    [Fact]
    public void DomainService_UpdateService_passes_excludeSelf_true_to_IsDuplicate()
    {
        var repo = new FakeDomainRepository();
        var service = new DomainService(repo);
        var domain = new Domain { IdDomain = 1, Name = "Neutronics" };

        var res = service.UpdateService(domain);

        Assert.True(repo.LastIsDuplicateExcludeSelf,
            "DomainService.UpdateService 同 ApplicationService — 必须 excludeSelf=true");
        Assert.Equal(2, res);
    }

    // ============================================================
    // Bug 2 — ToString override 必须返回 Name (供 WPF ComboBox 默认渲染)
    // ============================================================

    [Fact]
    public void Application_ToString_returns_Name_property()
    {
        var app = new Application { IdApplication = 1, Name = "UAT-App-1" };
        Assert.Equal("UAT-App-1", app.ToString());
    }

    [Fact]
    public void Application_ToString_returns_empty_when_Name_null()
    {
        var app = new Application { IdApplication = 0, Name = null! };
        Assert.Equal(string.Empty, app.ToString());
    }

    [Fact]
    public void Domain_ToString_returns_Name_property()
    {
        var domain = new Domain { IdDomain = 1, Name = "Neutronics" };
        Assert.Equal("Neutronics", domain.ToString());
    }

    [Fact]
    public void Domain_ToString_returns_empty_when_Name_null()
    {
        var domain = new Domain { IdDomain = 0, Name = null! };
        Assert.Equal(string.Empty, domain.ToString());
    }

    // ============================================================
    // Fakes
    // ============================================================

    private sealed class FakeApplicationRepository : IApplicationRepository
    {
        public bool LastIsDuplicateExcludeSelf { get; private set; }

        public bool IsDuplicate(Application app, bool excludeSelf)
        {
            LastIsDuplicateExcludeSelf = excludeSelf;
            return false;
        }

        public bool Add(Application entity) => true;
        public bool Modify(Application entity) => true;
        public bool Remove(Application entity) => true;
        public Application Get(int id) => new();
        public int Get(string Name) => 0;
        public ObservableCollection<Application> Get(Application entity) => new();
        public ObservableCollection<Application> GetAll() => new();
        public ObservableCollection<Applications_QueryResultData> GetAll_MIX() => new();
        public ObservableCollection<Applications_QueryResultData> GetByName(string Name) => new();
    }

    private sealed class FakeDomainRepository : IDomainRepository
    {
        public bool LastIsDuplicateExcludeSelf { get; private set; }

        public bool IsDuplicate(Domain domain, bool excludeSelf)
        {
            LastIsDuplicateExcludeSelf = excludeSelf;
            return false;
        }

        public bool Add(Domain entity) => true;
        public bool Modify(Domain entity) => true;
        public bool Remove(Domain entity) => true;
        public Domain Get(int id) => new();
        public int Get(string Name) => 0;
        public ObservableCollection<Domain> Get(Domain entity) => new();
        public ObservableCollection<Domain> GetAll() => new();
        public ObservableCollection<Domain> GetByName(string Name) => new();
    }
}
