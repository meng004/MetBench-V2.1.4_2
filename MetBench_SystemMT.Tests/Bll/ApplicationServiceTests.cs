using System.Collections.ObjectModel;
using MetBench_BLL;
using MetBench_Domain;
using MetBench_IDAL;
using Xunit;

namespace MetBench_SystemMT.Tests.Bll;

/// <summary>
/// Regression tests for <see cref="ApplicationService"/> covering bugs found by
/// Windows UAT round-1 (limeng 2026-05-18) — see
/// <c>docs/uat/reports/round-1-limeng-2026-05-18/README.md</c>.
/// </summary>
public sealed class ApplicationServiceTests
{
    /// <summary>
    /// UC-A2 (Major bug, round-1): editing an existing Application while keeping
    /// its Name unchanged must succeed; the IsDuplicate check must exclude the
    /// record under edit. Pre-fix the BLL passed excludeSelf=false, causing every
    /// edit to be misclassified as duplicate and rejected.
    /// </summary>
    [Fact]
    public void UpdateService_editing_same_name_record_should_call_IsDuplicate_with_excludeSelf_true()
    {
        var fakeRepo = new RecordingApplicationRepository();
        var service = new ApplicationService(fakeRepo);

        var existing = new Application
        {
            IdApplication = 7,
            Name = "UAT-App-1",
            ProgrammingLanguage = "Python",
            LinesOfCode = 100,
            Description = "v2 — edited description",
        };

        var result = service.UpdateService(existing);

        Assert.Equal(2, result); // 2 = success per docstring
        Assert.True(fakeRepo.IsDuplicateCalled, "UpdateService must call IsDuplicate");
        Assert.True(fakeRepo.LastExcludeSelf,
            "UpdateService must pass excludeSelf=true so the record under edit is not flagged as its own duplicate");
        Assert.True(fakeRepo.ModifyCalled, "UpdateService must call Modify when not duplicate");
    }

    /// <summary>
    /// Sanity: when a TRUE duplicate exists (different IdApplication, same Name),
    /// UpdateService must still return 1 (duplicate) so the fix does not weaken
    /// the duplicate check.
    /// </summary>
    [Fact]
    public void UpdateService_returns_duplicate_when_another_record_has_same_name()
    {
        var fakeRepo = new RecordingApplicationRepository(reportDuplicate: true);
        var service = new ApplicationService(fakeRepo);

        var attempt = new Application
        {
            IdApplication = 7,
            Name = "Collision",
            ProgrammingLanguage = "Python",
            LinesOfCode = 100,
        };

        var result = service.UpdateService(attempt);

        Assert.Equal(1, result); // 1 = duplicate per docstring
        Assert.False(fakeRepo.ModifyCalled, "Modify must NOT be called when IsDuplicate returns true");
    }

    private sealed class RecordingApplicationRepository : IApplicationRepository
    {
        private readonly bool _reportDuplicate;

        public RecordingApplicationRepository(bool reportDuplicate = false)
        {
            _reportDuplicate = reportDuplicate;
        }

        public bool IsDuplicateCalled { get; private set; }
        public bool LastExcludeSelf { get; private set; }
        public bool ModifyCalled { get; private set; }

        public bool IsDuplicate(Application app, bool excludeSelf)
        {
            IsDuplicateCalled = true;
            LastExcludeSelf = excludeSelf;
            return _reportDuplicate;
        }

        public bool Modify(Application entity)
        {
            ModifyCalled = true;
            return true;
        }

        // ----- IRepository<Application> remainder: unused, return defaults -----
        public ObservableCollection<Application> GetAll() => new();
        public Application Get(int id) => null!;
        public ObservableCollection<Application> Get(Application entity) => new();
        public bool Add(Application entity) => true;
        public bool Remove(Application entity) => true;

        // ----- IApplicationRepository remainder -----
        public int Get(string Name) => 0;
        public ObservableCollection<Applications_QueryResultData> GetAll_MIX() => new();
        public ObservableCollection<Applications_QueryResultData> GetByName(string Name) => new();
    }
}

/// <summary>
/// UC-A5 / UC-B1 (Major bug, round-1) regression: <see cref="Application.ToString"/>
/// must return the human-readable Name so WPF ComboBox bindings display correctly
/// instead of the fully-qualified type name.
/// </summary>
public sealed class ApplicationToStringTests
{
    [Fact]
    public void ToString_returns_Name_for_combobox_display()
    {
        var app = new Application { IdApplication = 1, Name = "amax.py" };

        Assert.Equal("amax.py", app.ToString());
    }

    [Fact]
    public void ToString_returns_empty_string_when_Name_is_null()
    {
        var app = new Application { IdApplication = 1, Name = null! };

        Assert.Equal(string.Empty, app.ToString());
    }
}
