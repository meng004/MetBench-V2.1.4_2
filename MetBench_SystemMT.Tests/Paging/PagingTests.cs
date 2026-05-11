using MetBench_BLL.Paging;
using Xunit;

namespace MetBench_SystemMT.Tests.Paging;

public sealed class PagingTests
{
    [Fact]
    public void PageRequest_First_returns_index_zero()
    {
        var request = PageRequest.First(25);
        Assert.Equal(0, request.PageIndex);
        Assert.Equal(25, request.PageSize);
        Assert.Equal(0, request.Skip);
    }

    [Fact]
    public void PageRequest_Skip_is_index_times_size()
    {
        Assert.Equal(0, new PageRequest(0, 10).Skip);
        Assert.Equal(10, new PageRequest(1, 10).Skip);
        Assert.Equal(75, new PageRequest(3, 25).Skip);
    }

    [Fact]
    public void PageRequest_Validate_rejects_negative_index()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageRequest(-1, 10).Validate());
    }

    [Fact]
    public void PageRequest_Validate_rejects_non_positive_size()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageRequest(0, 0).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageRequest(0, -1).Validate());
    }

    [Fact]
    public void PageRequest_Validate_returns_self_when_valid()
    {
        var request = new PageRequest(2, 50);
        Assert.Same(request, request.Validate());
    }

    [Fact]
    public void PagedResult_TotalPages_rounds_up()
    {
        Assert.Equal(3, new PagedResult<int>(Array.Empty<int>(), TotalCount: 25, PageIndex: 0, PageSize: 10).TotalPages);
        Assert.Equal(2, new PagedResult<int>(Array.Empty<int>(), TotalCount: 20, PageIndex: 0, PageSize: 10).TotalPages);
        Assert.Equal(1, new PagedResult<int>(Array.Empty<int>(), TotalCount: 1,  PageIndex: 0, PageSize: 10).TotalPages);
        Assert.Equal(0, new PagedResult<int>(Array.Empty<int>(), TotalCount: 0,  PageIndex: 0, PageSize: 10).TotalPages);
    }

    [Fact]
    public void PagedResult_TotalPages_zero_when_size_is_zero()
    {
        var result = new PagedResult<int>(Array.Empty<int>(), TotalCount: 25, PageIndex: 0, PageSize: 0);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void PagedResult_HasPrevious_and_HasNext_navigate_correctly()
    {
        var first = new PagedResult<int>(Array.Empty<int>(), TotalCount: 25, PageIndex: 0, PageSize: 10);
        Assert.False(first.HasPrevious);
        Assert.True(first.HasNext);

        var middle = first with { PageIndex = 1 };
        Assert.True(middle.HasPrevious);
        Assert.True(middle.HasNext);

        var last = first with { PageIndex = 2 };
        Assert.True(last.HasPrevious);
        Assert.False(last.HasNext);

        var single = new PagedResult<int>(Array.Empty<int>(), TotalCount: 5, PageIndex: 0, PageSize: 10);
        Assert.False(single.HasPrevious);
        Assert.False(single.HasNext);
    }

    [Fact]
    public void PagedResult_Empty_factory_returns_zero_total()
    {
        var empty = PagedResult<int>.Empty(pageIndex: 0, pageSize: 25);
        Assert.Empty(empty.Items);
        Assert.Equal(0, empty.TotalCount);
        Assert.Equal(0, empty.PageIndex);
        Assert.Equal(25, empty.PageSize);
        Assert.Equal(0, empty.TotalPages);
        Assert.False(empty.HasPrevious);
        Assert.False(empty.HasNext);
    }
}


