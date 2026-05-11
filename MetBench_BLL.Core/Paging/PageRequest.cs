namespace MetBench_BLL.Paging;

/// <summary>
/// Request for one page of results from a paged query. Zero-based
/// <see cref="PageIndex"/>; <see cref="PageSize"/> must be positive.
/// </summary>
public sealed record PageRequest(int PageIndex, int PageSize)
{
    public static PageRequest First(int pageSize) => new(0, pageSize);

    public int Skip => checked(PageIndex * PageSize);

    /// <summary>
    /// Throws if the request is malformed; returns the request otherwise.
    /// Repository implementations call this at the boundary so callers get
    /// consistent exception types regardless of backend.
    /// </summary>
    public PageRequest Validate()
    {
        if (PageIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PageIndex), PageIndex, "PageIndex must be non-negative");
        }
        if (PageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PageSize), PageSize, "PageSize must be positive");
        }
        return this;
    }
}
