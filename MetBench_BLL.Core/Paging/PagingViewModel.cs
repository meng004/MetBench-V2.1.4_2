using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MetBench_BLL.Paging;

/// <summary>
/// Reusable INotifyPropertyChanged + commands base for any UI that displays a
/// paged collection. Subclass it, implement <see cref="LoadPageAsync"/>, and the
/// derived ViewModel gets <c>First</c>/<c>Previous</c>/<c>Next</c>/<c>Last</c>/
/// <c>Refresh</c> commands plus <see cref="Items"/>/<see cref="PageIndex"/>/
/// <see cref="PageSize"/>/<see cref="TotalCount"/>/<see cref="TotalPages"/>/
/// <see cref="HasPrevious"/>/<see cref="HasNext"/>/<see cref="IsLoading"/>
/// state for free.
/// </summary>
/// <remarks>
/// Lives in <c>MetBench_BLL.Core</c> on purpose: WPF-free so it can be unit-
/// tested on Linux. CommunityToolkit.Mvvm is the only runtime dependency and
/// is platform-neutral. The class is <c>partial</c> because the
/// <c>[ObservableProperty]</c> and <c>[RelayCommand]</c> source generators
/// emit the backing members at compile time.
/// </remarks>
public abstract partial class PagingViewModel<T> : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<T> _items = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(HasPrevious))]
    [NotifyPropertyChangedFor(nameof(HasNext))]
    [NotifyCanExecuteChangedFor(nameof(FirstPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPrevious))]
    [NotifyPropertyChangedFor(nameof(HasNext))]
    [NotifyCanExecuteChangedFor(nameof(FirstPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
    private int _pageIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(HasNext))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
    private int _pageSize = 25;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FirstPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool _isLoading;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPrevious => PageIndex > 0;

    public bool HasNext => PageIndex + 1 < TotalPages;

    /// <summary>
    /// Fetch one page of data. Implementations should be cancellable.
    /// </summary>
    protected abstract Task<PagedResult<T>> LoadPageAsync(PageRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Load a page, replacing <see cref="Items"/> and updating
    /// <see cref="PageIndex"/>/<see cref="PageSize"/>/<see cref="TotalCount"/>.
    /// If <paramref name="request"/> is <c>null</c>, the current
    /// <see cref="PageIndex"/>/<see cref="PageSize"/> are used (refresh in
    /// place). Reentrant calls while a load is in flight are no-ops.
    /// </summary>
    public async Task LoadAsync(PageRequest? request = null, CancellationToken cancellationToken = default)
    {
        if (IsLoading) return;
        var effective = (request ?? new PageRequest(PageIndex, PageSize)).Validate();

        IsLoading = true;
        try
        {
            var result = await LoadPageAsync(effective, cancellationToken).ConfigureAwait(false);
            PageIndex = result.PageIndex;
            PageSize = result.PageSize;
            TotalCount = result.TotalCount;

            // Mutate the existing ObservableCollection in place so UI bindings
            // attached to Items keep their reference and incremental change
            // events fire correctly.
            Items.Clear();
            foreach (var item in result.Items)
            {
                Items.Add(item);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoFirst))]
    private Task FirstPageAsync(CancellationToken ct) =>
        LoadAsync(new PageRequest(0, PageSize), ct);

    private bool CanGoFirst() => !IsLoading && HasPrevious;

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private Task PreviousPageAsync(CancellationToken ct) =>
        LoadAsync(new PageRequest(PageIndex - 1, PageSize), ct);

    private bool CanGoPrevious() => !IsLoading && HasPrevious;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private Task NextPageAsync(CancellationToken ct) =>
        LoadAsync(new PageRequest(PageIndex + 1, PageSize), ct);

    private bool CanGoNext() => !IsLoading && HasNext;

    [RelayCommand(CanExecute = nameof(CanGoLast))]
    private Task LastPageAsync(CancellationToken ct) =>
        LoadAsync(new PageRequest(Math.Max(0, TotalPages - 1), PageSize), ct);

    private bool CanGoLast() => !IsLoading && HasNext;

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private Task RefreshAsync(CancellationToken ct) =>
        LoadAsync(cancellationToken: ct);

    private bool CanRefresh() => !IsLoading;
}
