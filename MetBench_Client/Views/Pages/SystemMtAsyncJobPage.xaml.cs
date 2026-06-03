using MetBench_Client.ViewModels;
using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages;

public partial class SystemMtAsyncJobPage : INavigableView<SystemMtAsyncJobViewModel>
{
    public SystemMtAsyncJobViewModel ViewModel { get; }

    public SystemMtAsyncJobPage(SystemMtAsyncJobViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
