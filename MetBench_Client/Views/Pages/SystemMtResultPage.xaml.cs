using MetBench_Client.ViewModels;
using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages;

public partial class SystemMtResultPage : INavigableView<SystemMtResultViewModel>
{
    public SystemMtResultViewModel ViewModel { get; }

    public SystemMtResultPage(SystemMtResultViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
