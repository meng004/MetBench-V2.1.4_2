using MetBench_Client.ViewModels;

namespace MetBench_Client.Views.Pages;

public partial class SystemMtAsyncJobPage
{
    public SystemMtAsyncJobViewModel ViewModel { get; }

    public SystemMtAsyncJobPage(SystemMtAsyncJobViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
