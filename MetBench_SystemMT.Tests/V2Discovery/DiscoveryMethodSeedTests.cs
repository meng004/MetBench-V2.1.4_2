using MetBench_BLL.Discovery;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

public sealed class DiscoveryMethodSeedTests
{
    [Fact]
    public void Canonical_set_contains_three_methods()
    {
        Assert.Equal(3, DiscoveryMethodSeed.CanonicalSet.Count);
        var names = DiscoveryMethodSeed.CanonicalSet.Select(d => d.Name).ToHashSet();
        Assert.Contains("MetaPattern-Structural", names);
        Assert.Contains("LLM-Native", names);
        Assert.Contains("SCG-Heuristic", names);
    }

    [Fact]
    public void EnsureSeeded_inserts_all_when_repo_empty()
    {
        var repo = new FakeDiscoveryMethodRepo();
        var inserted = DiscoveryMethodSeed.EnsureSeeded(repo);
        Assert.Equal(3, inserted);
        Assert.Equal(3, repo.Items.Count);
    }

    [Fact]
    public void EnsureSeeded_is_idempotent()
    {
        var repo = new FakeDiscoveryMethodRepo();
        DiscoveryMethodSeed.EnsureSeeded(repo);
        var second = DiscoveryMethodSeed.EnsureSeeded(repo);
        Assert.Equal(0, second);
        Assert.Equal(3, repo.Items.Count);
    }

    [Fact]
    public void EnsureSeeded_only_adds_missing_when_partial()
    {
        var repo = new FakeDiscoveryMethodRepo();
        // Pre-populate with MetaPattern only
        repo.Add(new DiscoveryMethod { Name = "MetaPattern-Structural", Version = "NOETHER-v1" });
        var inserted = DiscoveryMethodSeed.EnsureSeeded(repo);
        Assert.Equal(2, inserted);
        Assert.Equal(3, repo.Items.Count);
    }

    [Fact]
    public void SCG_Heuristic_method_has_w11_config_and_enabled()
    {
        var scg = DiscoveryMethodSeed.CanonicalSet.Single(d => d.Name == "SCG-Heuristic");
        Assert.Equal("scg-v1", scg.Version);
        Assert.True(scg.Enabled);
        Assert.Contains("direct-cause", scg.ConfigJson);
        Assert.Contains("mediator", scg.ConfigJson);
        Assert.Contains("confounder", scg.ConfigJson);
    }

    // ===== fake repo =====

    private sealed class FakeDiscoveryMethodRepo : IDiscoveryMethodRepository
    {
        public List<DiscoveryMethod> Items { get; } = new();

        public ObservableCollection<DiscoveryMethod> GetEnabled()
            => new(Items.Where(d => d.Enabled).ToList());

        public DiscoveryMethod? GetByNameVersion(string name, string version)
            => Items.FirstOrDefault(d => d.Name == name && d.Version == version);

        public ObservableCollection<DiscoveryMethod> GetAll() => new(Items);
        public ObservableCollection<DiscoveryMethod> Get(DiscoveryMethod template) => GetAll();
        public DiscoveryMethod? Get(int id) => Items.FirstOrDefault(d => d.IdMethod == id);
        public bool Add(DiscoveryMethod entity)
        {
            entity.IdMethod = Items.Count == 0 ? 1 : Items.Max(d => d.IdMethod) + 1;
            Items.Add(entity);
            return true;
        }
        public bool Modify(DiscoveryMethod entity)
        {
            var idx = Items.FindIndex(d => d.IdMethod == entity.IdMethod);
            if (idx < 0) return false;
            Items[idx] = entity;
            return true;
        }
        public bool Remove(DiscoveryMethod entity) => Items.Remove(entity);
        public ObservableCollection<DiscoveryMethod> GetPage(int pageIndex, int pageSize)
            => new(Items.Skip(pageIndex * pageSize).Take(pageSize).ToList());
        public int Count() => Items.Count;
    }
}
