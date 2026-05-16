using System.Collections.ObjectModel;
using MetBench_BLL.Discovery;
using MetBench_Domain;
using MetBench_IDAL;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

/// <summary>
/// F16 测试：MRPairingService — m_cmp 配对 binding 的创建 + 一致性 + 反查。
/// 闭环 cold-start C1 (m_cmp binding 模型缺失)。
/// </summary>
public sealed class MRPairingServiceTests
{
    [Fact]
    public void CreatePair_creates_two_bindings_with_complementary_roles()
    {
        var repo = new FakePairingBindingRepo();
        var svc = new MRPairingService(repo);

        var (primaryId, partnerId) = svc.CreatePair(
            mrId: 14, primaryApplicationId: 1, partnerApplicationId: 2,
            defaultSampleCasePath: "SUT/cross-program/pincell.json",
            boundBy: "test");

        Assert.NotEqual(primaryId, partnerId);
        var primary = repo.Get(primaryId);
        var partner = repo.Get(partnerId);
        Assert.Equal("primary", primary.Role);
        Assert.Equal("partner", partner.Role);
        Assert.Equal(partnerId, primary.PartnerBindingId);
        Assert.Equal(primaryId, partner.PartnerBindingId);
        Assert.Equal(14, primary.MRId);
        Assert.Equal(14, partner.MRId);
        Assert.Equal(1, primary.ApplicationId);
        Assert.Equal(2, partner.ApplicationId);
    }

    [Fact]
    public void CreatePair_refuses_same_application_on_both_sides()
    {
        var svc = new MRPairingService(new FakePairingBindingRepo());
        var ex = Assert.Throws<InvalidOperationException>(() =>
            svc.CreatePair(mrId: 14, primaryApplicationId: 1, partnerApplicationId: 1));
        Assert.Contains("distinct", ex.Message);
    }

    [Fact]
    public void GetPartner_returns_other_side_of_pair()
    {
        var repo = new FakePairingBindingRepo();
        var svc = new MRPairingService(repo);
        var (primaryId, partnerId) = svc.CreatePair(14, 1, 2);

        var fromPrimary = svc.GetPartner(primaryId);
        var fromPartner = svc.GetPartner(partnerId);

        Assert.NotNull(fromPrimary);
        Assert.NotNull(fromPartner);
        Assert.Equal(partnerId, fromPrimary!.IdMRBinding);
        Assert.Equal(primaryId, fromPartner!.IdMRBinding);
    }

    [Fact]
    public void GetPartner_returns_null_for_non_paired_binding()
    {
        var repo = new FakePairingBindingRepo();
        var standalone = new MRBinding { MRId = 1, ApplicationId = 1 };  // Role empty + PartnerBindingId null
        repo.Add(standalone);
        var svc = new MRPairingService(repo);
        Assert.Null(svc.GetPartner(standalone.IdMRBinding));
    }

    [Fact]
    public void ValidatePairs_returns_empty_when_all_pairs_consistent()
    {
        var repo = new FakePairingBindingRepo();
        var svc = new MRPairingService(repo);
        svc.CreatePair(14, 1, 2);
        svc.CreatePair(15, 1, 2);
        Assert.Empty(svc.ValidatePairs());
    }

    [Fact]
    public void ValidatePairs_flags_pair_with_mismatched_MRId()
    {
        var repo = new FakePairingBindingRepo();
        var svc = new MRPairingService(repo);
        var (primaryId, partnerId) = svc.CreatePair(14, 1, 2);

        // 人为破坏：把 partner 的 MRId 改成另一个
        var partner = repo.Get(partnerId);
        partner.MRId = 99;
        repo.Modify(partner);

        var bad = svc.ValidatePairs();
        Assert.Contains(primaryId, bad);
        Assert.Contains(partnerId, bad);
    }

    [Fact]
    public void ValidatePairs_flags_pair_with_missing_partner_link()
    {
        var repo = new FakePairingBindingRepo();
        var svc = new MRPairingService(repo);
        var (primaryId, _) = svc.CreatePair(14, 1, 2);

        // 人为破坏：清空 primary 的 PartnerBindingId
        var primary = repo.Get(primaryId);
        primary.PartnerBindingId = null;
        repo.Modify(primary);

        var bad = svc.ValidatePairs();
        Assert.Contains(primaryId, bad);
    }

    [Fact]
    public void ValidatePairs_skips_standalone_bindings()
    {
        var repo = new FakePairingBindingRepo();
        repo.Add(new MRBinding { MRId = 1, ApplicationId = 1 });
        repo.Add(new MRBinding { MRId = 1, ApplicationId = 2 });
        var svc = new MRPairingService(repo);
        Assert.Empty(svc.ValidatePairs());
    }

    [Fact]
    public void GetPairsForMR_returns_primary_first_then_partner()
    {
        var repo = new FakePairingBindingRepo();
        var svc = new MRPairingService(repo);
        svc.CreatePair(14, 1, 2);
        var pairs = svc.GetPairsForMR(14);
        Assert.Equal(2, pairs.Count);
        Assert.Equal("primary", pairs[0].Role);
        Assert.Equal("partner", pairs[1].Role);
    }

    [Fact]
    public void GetPairsForMR_excludes_standalone_bindings_for_same_MR()
    {
        var repo = new FakePairingBindingRepo();
        var svc = new MRPairingService(repo);
        svc.CreatePair(14, 1, 2);
        repo.Add(new MRBinding { MRId = 14, ApplicationId = 3 });  // standalone

        var pairs = svc.GetPairsForMR(14);
        Assert.Equal(2, pairs.Count);  // 不含 standalone
        Assert.All(pairs, p => Assert.False(string.IsNullOrEmpty(p.Role)));
    }

    [Fact]
    public void ExpectedComplementaryRole_maps_correctly()
    {
        Assert.Equal("partner", MRPairingService.ExpectedComplementaryRole("primary"));
        Assert.Equal("primary", MRPairingService.ExpectedComplementaryRole("partner"));
        Assert.Equal("", MRPairingService.ExpectedComplementaryRole(""));
        Assert.Equal("", MRPairingService.ExpectedComplementaryRole("invalid"));
    }
}

internal sealed class FakePairingBindingRepo : IMRBindingRepository
{
    public List<MRBinding> Data { get; } = new();
    private int _nextId = 1;

    public ObservableCollection<MRBinding> GetAll() => new(Data);
    public MRBinding Get(int id) => Data.First(b => b.IdMRBinding == id);
    public ObservableCollection<MRBinding> Get(MRBinding e) => new(Data);
    public bool Add(MRBinding e) { e.IdMRBinding = _nextId++; Data.Add(e); return true; }
    public bool Modify(MRBinding e)
    {
        var i = Data.FindIndex(x => x.IdMRBinding == e.IdMRBinding);
        if (i < 0) return false;
        Data[i] = e; return true;
    }
    public bool Remove(MRBinding e) => Data.Remove(e);
    public ObservableCollection<MRBinding> GetByMR(int mrId) => new(Data.Where(b => b.MRId == mrId).ToList());
    public ObservableCollection<MRBinding> GetByApplication(int appId) => new(Data.Where(b => b.ApplicationId == appId).ToList());
    public ObservableCollection<MRBinding> GetActive() => new(Data.Where(b => b.Status == "active").ToList());
    public ObservableCollection<MRBinding> GetByStatus(string status) => new(Data.Where(b => b.Status == status).ToList());
}
