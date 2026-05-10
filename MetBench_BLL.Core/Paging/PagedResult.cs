namespace MetBench_BLL.Paging;

/// <summary>
/// One page of items together with the totals needed to render a pagination
/// control. <see cref="TotalPages"/>, <see cref="HasPrevious"/>, and
/// <see cref="HasNext"/> are derived; the persisted state is <see cref="Items"/>,
/// <see cref="TotalCount"/>, <see cref="PageIndex"/>, and <see cref="PageSize"/>.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageIndex,
    int PageSize)
{
    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPrevious => PageIndex > 0;

    public bool HasNext => PageIndex + 1 < TotalPages;

    public static PagedResult<T> Empty(int pageIndex, int pageSize) =>
        new(Array.Empty<T>(), TotalCount: 0, pageIndex, pageSize);
}
