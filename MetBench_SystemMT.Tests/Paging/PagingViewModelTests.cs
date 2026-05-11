using MetBench_BLL.Paging;
using Xunit;

namespace MetBench_SystemMT.Tests.Paging;

public sealed class PagingViewModelTests
{
    /// <summary>
    /// Test double: returns deterministic slices of a fixed in-memory source.
    /// </summary>
    private sealed class FakePagingViewModel : PagingViewModel<string>
    {
        private readonly IReadOnlyList<string> _source;
        public int LoadCallCount { get; private set; }
        public PageRequest? LastRequest { get; private set; }

        public FakePagingViewModel(int totalItems)
        {
            _source = Enumerable.Range(0, totalItems).Select(i => $"item-{i}").ToList();
        }

        protected override Task<PagedResult<string>> LoadPageAsync(
            PageRequest request, CancellationToken cancellationToken)
        {
            LoadCallCount++;
            LastRequest = request;
            cancellationToken.ThrowIfCancellationRequested();

            var slice = _source.Skip(request.Skip).Take(request.PageSize).ToList();
            return Task.FromResult(new PagedResult<string>(
                slice, _source.Count, request.PageIndex, request.PageSize));
        }
    }

    [Fact]
    public void Initial_state_has_empty_items_and_default_page_size()
    {
        var vm = new FakePagingViewModel(0);

        Assert.Empty(vm.Items);
        Assert.Equal(0, vm.PageIndex);
        Assert.Equal(25, vm.PageSize);
        Assert.Equal(0, vm.TotalCount);
        Assert.Equal(0, vm.TotalPages);
        Assert.False(vm.HasPrevious);
        Assert.False(vm.HasNext);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadAsync_with_empty_source_yields_empty_items()
    {
        var vm = new FakePagingViewModel(0);

        await vm.LoadAsync(new PageRequest(0, 10));

        Assert.Empty(vm.Items);
        Assert.Equal(0, vm.TotalCount);
        Assert.Equal(0, vm.TotalPages);
        Assert.False(vm.HasNext);
    }

    [Fact]
    public async Task LoadAsync_first_page_populates_items_and_metadata()
    {
        var vm = new FakePagingViewModel(57);

        await vm.LoadAsync(new PageRequest(0, 25));

        Assert.Equal(25, vm.Items.Count);
        Assert.Equal("item-0", vm.Items[0]);
        Assert.Equal("item-24", vm.Items[24]);
        Assert.Equal(57, vm.TotalCount);
        Assert.Equal(3, vm.TotalPages);
        Assert.False(vm.HasPrevious);
        Assert.True(vm.HasNext);
    }

    [Fact]
    public async Task LoadAsync_with_null_request_refreshes_current_page()
    {
        var vm = new FakePagingViewModel(57);
        await vm.LoadAsync(new PageRequest(1, 25));

        var beforeCalls = vm.LoadCallCount;
        await vm.LoadAsync();

        Assert.Equal(beforeCalls + 1, vm.LoadCallCount);
        Assert.Equal(1, vm.LastRequest!.PageIndex);
        Assert.Equal(25, vm.LastRequest.PageSize);
    }

    [Fact]
    public async Task NextPage_command_advances_index_and_loads()
    {
        var vm = new FakePagingViewModel(57);
        await vm.LoadAsync(new PageRequest(0, 25));

        await vm.NextPageCommand.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, vm.PageIndex);
        Assert.Equal(25, vm.Items.Count);
        Assert.Equal("item-25", vm.Items[0]);
    }

    [Fact]
    public async Task PreviousPage_command_decrements_index_and_loads()
    {
        var vm = new FakePagingViewModel(57);
        await vm.LoadAsync(new PageRequest(2, 25));
        Assert.Equal(7, vm.Items.Count); // last page partial

        await vm.PreviousPageCommand.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, vm.PageIndex);
        Assert.Equal(25, vm.Items.Count);
    }

    [Fact]
    public async Task FirstPage_command_jumps_to_index_zero()
    {
        var vm = new FakePagingViewModel(57);
        await vm.LoadAsync(new PageRequest(2, 25));

        await vm.FirstPageCommand.ExecuteAsync(CancellationToken.None);

        Assert.Equal(0, vm.PageIndex);
        Assert.False(vm.HasPrevious);
    }

    [Fact]
    public async Task LastPage_command_jumps_to_total_pages_minus_one()
    {
        var vm = new FakePagingViewModel(57);
        await vm.LoadAsync(new PageRequest(0, 25));

        await vm.LastPageCommand.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, vm.PageIndex);
        Assert.Equal(7, vm.Items.Count);
        Assert.False(vm.HasNext);
    }

    [Fact]
    public async Task Navigation_command_can_execute_reflects_navigation_availability()
    {
        var vm = new FakePagingViewModel(57);
        await vm.LoadAsync(new PageRequest(0, 25));

        Assert.False(vm.PreviousPageCommand.CanExecute(null));
        Assert.False(vm.FirstPageCommand.CanExecute(null));
        Assert.True(vm.NextPageCommand.CanExecute(null));
        Assert.True(vm.LastPageCommand.CanExecute(null));

        await vm.NextPageCommand.ExecuteAsync(CancellationToken.None);

        Assert.True(vm.PreviousPageCommand.CanExecute(null));
        Assert.True(vm.NextPageCommand.CanExecute(null));

        await vm.LastPageCommand.ExecuteAsync(CancellationToken.None);

        Assert.True(vm.PreviousPageCommand.CanExecute(null));
        Assert.False(vm.NextPageCommand.CanExecute(null));
        Assert.False(vm.LastPageCommand.CanExecute(null));
    }

    [Fact]
    public async Task RefreshCommand_reissues_current_page_request()
    {
        var vm = new FakePagingViewModel(57);
        await vm.LoadAsync(new PageRequest(1, 25));
        var beforeCalls = vm.LoadCallCount;

        await vm.RefreshCommand.ExecuteAsync(CancellationToken.None);

        Assert.Equal(beforeCalls + 1, vm.LoadCallCount);
        Assert.Equal(1, vm.LastRequest!.PageIndex);
    }

    [Fact]
    public async Task LoadAsync_validates_request_and_throws_on_negative_index()
    {
        var vm = new FakePagingViewModel(10);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            vm.LoadAsync(new PageRequest(-1, 10)));
    }

    [Fact]
    public async Task LoadAsync_rejects_zero_page_size()
    {
        var vm = new FakePagingViewModel(10);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            vm.LoadAsync(new PageRequest(0, 0)));
    }

    [Fact]
    public async Task Items_collection_reference_is_preserved_across_loads()
    {
        var vm = new FakePagingViewModel(57);
        await vm.LoadAsync(new PageRequest(0, 25));
        var collectionRef = vm.Items;

        await vm.LoadAsync(new PageRequest(1, 25));

        Assert.Same(collectionRef, vm.Items);
        Assert.Equal("item-25", vm.Items[0]);
    }
}
